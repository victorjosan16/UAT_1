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

        private Action<bool> _onDone;
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
            LevelPlay.OnInitSuccess += _ => CreateAd();
            LevelPlay.OnInitFailed += _ =>
            {
                Debug.LogWarning("[Ads] LevelPlay nu s-a inițializat; reîncerc în 60 s.");
                StartCoroutine(RetryInit());
            };
            LevelPlay.Init(AppKey);
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
            _ad.OnAdLoadFailed += _ => StartCoroutine(RetryLoad());
            _ad.OnAdDisplayed += _ => _displayed = true;
            _ad.OnAdRewarded += (_, _) => _rewarded = true;
            _ad.OnAdClosed += _ => StartCoroutine(AfterClose());
            _ad.LoadAd();
        }

        private IEnumerator AfterClose()
        {
            // Recompensa poate sosi puțin după închidere (evenimentele sunt asincrone).
            for (float t = 0f; t < 1f && !_rewarded; t += Time.unscaledDeltaTime) yield return null;
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
