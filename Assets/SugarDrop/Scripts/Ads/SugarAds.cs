using System;
using System.Collections;
using System.Reflection;
using QuarryEmpire;
using UnityEngine;
#if SUGARDROP_LEVELPLAY
using Unity.Services.LevelPlay;
#endif

namespace SugarDrop
{
    /// <summary>
    /// Reclamele cu recompensă din Sugar Drop. Cu pachetul Unity LevelPlay instalat și cheile completate în
    /// <see cref="SugarAdsConfig"/>, arată reclame reale; altfel (și mereu în Editor) folosește reclama de TEST din
    /// AdService. Jucătorul primește recompensa doar dacă a văzut reclama până la capăt.
    /// Consimțământul (GDPR) e întrebat în joc la prima pornire și trimis către LevelPlay înainte de inițializare.
    /// </summary>
    public class SugarAds : MonoBehaviour
    {
        public static SugarAds Instance { get; private set; }

        /// <summary>Nu există nicio reclamă gata acum (fără internet, fără umplere): jocul arată un mesaj.</summary>
        public static event Action Unavailable;

        /// <summary>Ultimul mesaj al SDK-ului (inițializare, reclamă, banner): se arată în fereastra „Nicio reclamă”, ca să se vadă motivul.</summary>
        public string Status { get; private set; } = "";

        /// <summary>Bannerul de jos a apărut sau a dispărut (interfața urcă / coboară cu înălțimea lui).</summary>
        public static event Action BannerChanged;

        private Action<bool> _onDone;
        private bool _bannerWanted;
        private GameObject _testBanner;

        /// <summary>Bannerul e pe ecran acum.</summary>
        public bool BannerShowing { get; private set; }

        /// <summary>Înălțimea bannerului în pixeli de ecran (320×50 dp), 0 dacă nu e afișat.</summary>
        public float BannerHeightPixels => BannerShowing ? 50f * (Screen.dpi > 0f ? Screen.dpi : 320f) / 160f : 0f;
        private bool _rewarded;
        private bool _displayed;
        private bool _initStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => Ensure();

        /// <summary>Serviciul de reclame (creat la prima nevoie, rămâne pe toată durata jocului).</summary>
        public static SugarAds Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SugarAds");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SugarAds>();
            return Instance;
        }

        private static string AppKey => Application.platform == RuntimePlatform.IPhonePlayer ? SugarAdsConfig.IosAppKey : SugarAdsConfig.AndroidAppKey;
        private static string AdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? SugarAdsConfig.IosRewardedAdUnitId : SugarAdsConfig.AndroidRewardedAdUnitId;
        private static string BannerUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? SugarAdsConfig.IosBannerAdUnitId : SugarAdsConfig.AndroidBannerAdUnitId;

        /// <summary>Reclamele reale sunt posibile în acest build (pachet instalat, chei completate, pe telefon).</summary>
        public static bool RealAds
        {
            get
            {
#if SUGARDROP_LEVELPLAY
                return !Application.isEditor && !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(AdUnitId);
#else
                return false;
#endif
            }
        }

        /// <summary>Pornește reclamele (după ce jucătorul a răspuns la întrebarea despre consimțământ).</summary>
        public void Initialize(bool personalizedConsent)
        {
            if (!RealAds || _initStarted) return;
            _initStarted = true;
            ApplyConsent(personalizedConsent);
#if SUGARDROP_LEVELPLAY
            LevelPlay.OnInitSuccess += _ =>
            {
                Status = "init ok";
                // Dacă bibliotecile native lipsesc sau SDK-ul se schimbă, o eroare aici nu trebuie să strice jocul.
                try
                {
                    CreateAd();
                    CreateBanner();
                }
                catch (Exception e)
                {
                    Status = "creare: " + e.Message;
                Debug.LogWarning("[Ads] Nu am putut crea reclamele: " + e.Message);
                }
            };
            LevelPlay.OnInitFailed += error =>
            {
                Status = "init: " + error;
                Debug.LogWarning("[Ads] LevelPlay nu s-a inițializat (" + error + "); reîncerc în 60 s.");
                StartCoroutine(RetryInit());
            };
            try
            {
                LevelPlay.Init(AppKey);
            }
            catch (Exception e)
            {
                Status = "pornire: " + e.Message;
                Debug.LogWarning("[Ads] LevelPlay nu a pornit: " + e.Message);
            }
#endif
        }

        /// <summary>Jucătorul și-a schimbat alegerea din Setări (se aplică pentru reclamele următoare).</summary>
        public void UpdateConsent(bool personalizedConsent)
        {
            if (RealAds) ApplyConsent(personalizedConsent);
        }

        /// <summary>Arată o reclamă cu recompensă. done(true) = văzută până la capăt.</summary>
        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (_onDone != null)
            {
                done?.Invoke(false);
                return;
            }
            if (!RealAds)
            {
                // Reclama de test (Editor, sau înainte de configurarea LevelPlay).
                if (AdService.Instance == null) done?.Invoke(true);
                else AdService.Instance.ShowRewarded(placement, done);
                return;
            }
#if SUGARDROP_LEVELPLAY
            if (_ad == null || !_ad.IsAdReady())
            {
                Unavailable?.Invoke();
                _ad?.LoadAd();
                done?.Invoke(false);
                return;
            }
            _onDone = done;
            _rewarded = false;
            _displayed = false;
            AudioListener.pause = true;
            Debug.Log($"[Ads] Reclamă pentru „{placement}”.");
            _ad.ShowAd();
            StartCoroutine(ShowTimeout());
#endif
        }

        /// <summary>
        /// Cere bannerul de jos (hartă și nivel) sau îl ascunde (meniu, povești). Se apelează des: nu face nimic
        /// dacă starea nu s-a schimbat. Fără reclame reale: în Editor și în build-urile de dezvoltare apare o bară de
        /// TEST de aceeași mărime (ca să se vadă așezarea), iar în build-urile finale nu apare nimic.
        /// </summary>
        public void SetBannerVisible(bool visible)
        {
            if (_bannerWanted == visible) return;
            _bannerWanted = visible;
            ApplyBanner();
        }

        private void ApplyBanner()
        {
            bool show = _bannerWanted;
            if (!RealAds)
            {
                show &= Application.isEditor || Debug.isDebugBuild;
                if (show && _testBanner == null) _testBanner = BuildTestBanner();
                if (_testBanner != null) _testBanner.SetActive(show);
            }
#if SUGARDROP_LEVELPLAY
            else
            {
                show &= _banner != null && _bannerLoaded;
                if (_banner != null)
                {
                    if (show) _banner.ShowAd();
                    else _banner.HideAd();
                }
            }
#endif
            if (show == BannerShowing) return;
            BannerShowing = show;
            if (_testBanner != null) ((RectTransform)_testBanner.transform.GetChild(0)).sizeDelta = new Vector2(0f, BannerHeightPixels);
            BannerChanged?.Invoke();
        }

        /// <summary>Bara gri „BANNER DE TEST” (doar Editor / build de dezvoltare), cât un banner real.</summary>
        private GameObject BuildTestBanner()
        {
            var root = new GameObject("TestBanner", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)bar.transform;
            rect.SetParent(root.transform, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 50f * (Screen.dpi > 0f ? Screen.dpi : 320f) / 160f);
            bar.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.92f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "BANNER DE TEST (320×50)";
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
            return root;
        }

        private IEnumerator ShowTimeout()
        {
            yield return new WaitForSecondsRealtime(SugarAdsConfig.ShowTimeoutSeconds);
            if (_onDone == null || _displayed) yield break;
            Debug.LogWarning("[Ads] Reclama n-a pornit la timp.");
            Unavailable?.Invoke();
            Finish(false);
        }

        private void Finish(bool rewarded)
        {
            AudioListener.pause = false;
            Action<bool> done = _onDone;
            _onDone = null;
            done?.Invoke(rewarded);
        }

        /// <summary>
        /// Trimite consimțământul GDPR către LevelPlay. Numele metodei diferă între versiunile SDK-ului
        /// (9.5+: LevelPlayPrivacySettings.SetGDPRConsent, mai vechi: LevelPlay.SetConsent / IronSource.Agent.setConsent),
        /// așa că o căutăm pe cea existentă.
        /// </summary>
        private static void ApplyConsent(bool consent)
        {
            if (InvokeStatic("Unity.Services.LevelPlay.LevelPlayPrivacySettings", "SetGDPRConsent", consent)) return;
            if (InvokeStatic("Unity.Services.LevelPlay.LevelPlay", "SetConsent", consent)) return;
            Type ironSource = FindType("IronSource");
            object agent = ironSource?.GetProperty("Agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            MethodInfo set = agent?.GetType().GetMethod("setConsent", new[] { typeof(bool) });
            if (set != null)
            {
                set.Invoke(agent, new object[] { consent });
                return;
            }
            Debug.LogWarning("[Ads] Nu am găsit metoda de consimțământ în SDK-ul LevelPlay.");
        }

        private static bool InvokeStatic(string typeName, string method, bool value)
        {
            Type type = FindType(typeName);
            if (type == null) return false;
            foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] p = m.GetParameters();
                if (!string.Equals(m.Name, method, StringComparison.OrdinalIgnoreCase) || p.Length != 1 || p[0].ParameterType != typeof(bool)) continue;
                m.Invoke(null, new object[] { value });
                return true;
            }
            return false;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

#if SUGARDROP_LEVELPLAY
        private LevelPlayRewardedAd _ad;

        private void CreateAd()
        {
            if (_ad != null) return;
            _ad = new LevelPlayRewardedAd(AdUnitId);
            _ad.OnAdLoadFailed += error =>
            {
                Status = "reclamă: " + error;
                Debug.LogWarning("[Ads] Reclama nu s-a încărcat: " + error);
                StartCoroutine(RetryLoad());
            };
            _ad.OnAdDisplayed += _ => _displayed = true;
            _ad.OnAdRewarded += (_, _) => _rewarded = true;
            _ad.OnAdClosed += _ => StartCoroutine(AfterClose());
            _ad.LoadAd();
        }

        private LevelPlayBannerAd _banner;
        private bool _bannerLoaded;

        /// <summary>Bannerul standard 320×50, jos pe mijloc; se încarcă după inițializare, se arată doar la cerere.</summary>
        private void CreateBanner()
        {
            if (_banner != null || string.IsNullOrEmpty(BannerUnitId)) return;
            // SDK 9.x: setările bannerului se dau printr-un obiect Config (constructorul cu 3 argumente nu mai există).
            // Se arată doar la cerere (SetDisplayOnLoad(false)), în zona sigură a ecranului.
            LevelPlayBannerAd.Config config = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false)
                .SetRespectSafeArea(true)
                .Build();
            _banner = new LevelPlayBannerAd(BannerUnitId, config);
            _banner.OnAdLoaded += _ =>
            {
                _bannerLoaded = true;
                ApplyBanner();
            };
            _banner.OnAdLoadFailed += error =>
            {
                Status = "banner: " + error;
                Debug.LogWarning("[Ads] Bannerul nu s-a încărcat: " + error);
                _bannerLoaded = false;
                ApplyBanner();
                StartCoroutine(RetryBanner());
            };
            _banner.LoadAd();
            if (!_bannerWanted) _banner.HideAd();
        }

        private IEnumerator RetryBanner()
        {
            yield return new WaitForSecondsRealtime(30f);
            _banner?.LoadAd();
        }

        private IEnumerator AfterClose()
        {
            // Recompensa poate sosi după închidere (evenimentele SDK-ului sunt asincrone, iar ordinea lor nu e
            // garantată): așteptăm până la 3 s, ca o reclamă văzută până la capăt să nu-și piardă recompensa.
            for (float t = 0f; t < 3f && !_rewarded; t += Time.unscaledDeltaTime) yield return null;
            Finish(_rewarded);
            _ad.LoadAd();
        }

        private IEnumerator RetryLoad()
        {
            yield return new WaitForSecondsRealtime(15f);
            _ad?.LoadAd();
        }

        private IEnumerator RetryInit()
        {
            yield return new WaitForSecondsRealtime(60f);
            LevelPlay.Init(AppKey);
        }
#endif
    }
}
