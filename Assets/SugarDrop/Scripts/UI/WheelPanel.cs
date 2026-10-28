using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Roata norocului: o rotire gratuită pe zi (două în weekend) și până la 3 rotiri cu reclamă. Roata se învârte
    /// câteva secunde (cu „tic” la fiecare felie) și se oprește pe premiul ales; premiul se dă imediat.
    /// </summary>
    public class WheelPanel : MonoBehaviour
    {
        private const float WheelSize = 760f;
        private const float SpinSeconds = 4.2f;

        /// <summary>S-a câștigat un premiu (ecranele își reîmprospătează monedele și viețile).</summary>
        public event Action PrizeWon;

        private GameObject _root;
        private UiLabel _title;
        private UiLabel _info;
        private UiLabel _result;
        private PunchScale _resultPunch;
        private RectTransform _spinner;
        private RectTransform _pointer;
        private Button _spin;
        private UiLabel _spinLabel;
        private Image _spinFace;
        private UiLabel _closeLabel;
        private UIParticleController _bursts;

        private bool _spinning;
        private float _from, _to, _t;
        private int _slice;
        private int _lastTick;

        public bool IsOpen => _root != null && _root.activeSelf;
        public bool Spinning => _spinning;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(940f, 1560f));

            _title = SugarUi.Outlined("Title", box, "", 76, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(880f, 100f));
            _info = UiFactory.CreateText("Info", box, "", 34, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.25f));
            UiFactory.Place(_info.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(860f, 60f));

            // Roata (se rotește) cu iconițele premiilor, butucul din mijloc și indicatorul de sus (fix).
            RectTransform holder = UiFactory.CreateRect("Wheel", box);
            UiFactory.Place(holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -620f), new Vector2(WheelSize, WheelSize));
            Image shadow = UiFactory.CreateImage("Shadow", holder, SugarSprites.Circle, QuestStyle.Shadow);
            UiFactory.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), Vector2.one * WheelSize);
            Image wheel = UiFactory.CreateImage("Spinner", holder, SugarSprites.Wheel(WheelRules.Slices.Length), Color.white);
            _spinner = UiFactory.Place(wheel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * WheelSize);
            float slice = 360f / WheelRules.Slices.Length;
            for (int i = 0; i < WheelRules.Slices.Length; i++) PrizeIcon(WheelRules.Slices[i], i * slice + slice * 0.5f);
            Image hub = UiFactory.CreateImage("Hub", holder, SugarSprites.Circle, Color.white);
            UiFactory.Place(hub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            Image hubCandy = UiFactory.CreateImage("HubGem", hub.transform, PieceSprites.Piece(3), Color.white);
            UiFactory.Place(hubCandy.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            Image pointer = UiFactory.CreateImage("Pointer", holder, SugarSprites.Chevron, theme.NegativeColor);
            _pointer = UiFactory.Place(pointer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(130f, 130f));
            var outline = pointer.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(4f, -4f);

            _result = SugarUi.Outlined("Result", box, "", 56, theme.AccentColor, 4f);
            UiFactory.Place(_result.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(880f, 90f));
            _resultPunch = _result.gameObject.AddComponent<PunchScale>();

            _spin = SugarUi.Button("Spin", box, "", theme.PositiveColor, OnSpinClicked, 50);
            UiFactory.Place((RectTransform)_spin.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(620f, 140f));
            _spinLabel = UiFactory.GetButtonLabel(_spin);
            _spinFace = _spin.transform.Find("Face").GetComponent<Image>();
            Button close = SugarUi.Button("Close", box, "", theme.PanelColor, () =>
            {
                if (_spinning) return;
                SugarUi.Click();
                Hide();
            }, 40);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(480f, 110f));
            _closeLabel = UiFactory.GetButtonLabel(close);

            RectTransform particles = UiFactory.CreateRect("Bursts", box);
            UiFactory.Stretch(particles);
            _bursts = particles.gameObject.AddComponent<UIParticleController>();
            _bursts.Initialize(60, SugarSprites.Sparkle, null, 0f);

            _root.SetActive(false);
        }

        private void PrizeIcon(WheelPrize prize, float angle)
        {
            float radius = WheelSize * 0.33f;
            float rad = angle * Mathf.Deg2Rad;
            RectTransform slot = UiFactory.CreateRect("Prize" + prize, _spinner);
            UiFactory.Place(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius, new Vector2(130f, 130f));
            slot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            Sprite icon = prize switch
            {
                WheelPrize.Life or WheelPrize.FullLives => SugarSprites.LifeHeart,
                WheelPrize.Coins50 or WheelPrize.Coins150 => ProceduralSprites.Coin,
                WheelPrize.Bomb => SugarSprites.Bomb,
                WheelPrize.StartRocket => SugarSprites.Rocket,
                WheelPrize.Shuffle => SugarSprites.ShuffleIcon,
                _ => null,
            };
            if (icon != null)
            {
                Image image = UiFactory.CreateImage("Icon", slot, icon, prize == WheelPrize.Shuffle ? UiRoot.Theme.TextDarkColor : Color.white);
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(96f, 96f));
            }
            string caption = prize switch
            {
                WheelPrize.FullLives => "MAX",
                WheelPrize.ExtraMoves => "+5",
                WheelPrize.Coins50 => "50",
                WheelPrize.Coins150 => "150",
                WheelPrize.Life => "+1",
                _ => "",
            };
            if (caption.Length == 0) return;
            UiLabel label = SugarUi.Outlined("Caption", slot, caption, prize == WheelPrize.ExtraMoves ? 64 : 36, Color.white, 3f);
            UiFactory.Place(label.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, prize == WheelPrize.ExtraMoves ? 10f : -40f), new Vector2(140f, 70f));
        }

        public void Show()
        {
            _title.Text = SugarLoc.T("Roata norocului");
            _closeLabel.Text = SugarLoc.T("ÎNCHIDE");
            _result.Text = "";
            Refresh();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_spinning) return;
            _root.SetActive(false);
        }

        private void Refresh()
        {
            int free = SugarWheel.FreeSpinsLeft, ads = SugarWheel.AdSpinsLeft;
            _info.Text = SugarLoc.F("Gratis azi: {0}  ·  Cu reclamă: {1}", free, ads);
            UiTheme theme = UiRoot.Theme;
            if (free > 0)
            {
                _spinLabel.Text = SugarLoc.T("ROTEȘTE GRATIS");
                _spinFace.color = theme.PositiveColor;
            }
            else if (ads > 0)
            {
                _spinLabel.Text = SugarLoc.T("ROTEȘTE (reclamă)");
                _spinFace.color = theme.AdColor;
            }
            else
            {
                _spinLabel.Text = SugarLoc.T("REVINO MÂINE");
                _spinFace.color = theme.PanelColor;
            }
        }

        private void OnSpinClicked()
        {
            if (_spinning) return;
            SugarUi.Click();
            if (SugarWheel.FreeSpinsLeft > 0) StartSpin(true);
            else if (SugarWheel.AdSpinsLeft > 0) SugarUi.RewardedAd("wheel", () => StartSpin(false));
        }

        private void StartSpin(bool free)
        {
            if (_spinning || !IsOpen) return;
            SugarWheel.UseSpin(free);
            _slice = WheelRules.PickSlice(UnityEngine.Random.value);
            float slice = 360f / WheelRules.Slices.Length;
            float center = _slice * slice + slice * 0.5f;
            float current = -_spinner.localEulerAngles.z;
            // Rotim în sensul acelor de ceasornic până când mijlocul feliei ajunge sus (plus câteva ture întregi).
            float wobble = UnityEngine.Random.Range(-slice * 0.3f, slice * 0.3f);
            float target = Mathf.Repeat(360f - center + wobble, 360f);
            _from = current;
            _to = current + 5f * 360f + Mathf.Repeat(target - Mathf.Repeat(current, 360f), 360f);
            _t = 0f;
            _lastTick = Mathf.FloorToInt(current / slice);
            _spinning = true;
            _result.Text = "";
            Refresh();
        }

        private void Update()
        {
            if (!_spinning) return;
            _t += Time.unscaledDeltaTime / SpinSeconds;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(_t), 3.2f);
            float angle = Mathf.Lerp(_from, _to, eased);
            _spinner.localRotation = Quaternion.Euler(0f, 0f, -angle);

            // „Tic” la fiecare felie: indicatorul tresare și se aude un clic.
            float slice = 360f / WheelRules.Slices.Length;
            int tick = Mathf.FloorToInt(angle / slice);
            if (tick != _lastTick)
            {
                _lastTick = tick;
                _pointer.localRotation = Quaternion.Euler(0f, 0f, 14f);
                if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Click, 0.5f, 1.2f);
            }
            _pointer.localRotation = Quaternion.Slerp(_pointer.localRotation, Quaternion.identity, Time.unscaledDeltaTime * 12f);

            if (_t < 1f) return;
            _spinning = false;
            WheelPrize prize = WheelRules.Slices[_slice];
            SugarWheel.Grant(prize);
            _result.Text = SugarLoc.T(SugarWheel.PrizeKey(prize));
            _resultPunch.Punch(2f);
            _bursts.BurstAt(_result.RectTransform, 24, SugarSprites.Sparkle, new[] { Color.white, Palette.TileLight(3), Palette.TileLight(0) });
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Win);
            SugarAudio.Buzz(1);
            Refresh();
            PrizeWon?.Invoke();
        }
    }
}
