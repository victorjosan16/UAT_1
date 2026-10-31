using System;
using Veliqora.Core.Save;
using Veliqora.UI;

namespace Veliqora.Settings
{
    /// <summary>Owns SettingsData mutations; every change is persisted (via onChanged → autosave) and applied.</summary>
    public sealed class SettingsService
    {
        private readonly SettingsData _data;
        private readonly Action _onChanged;

        public event Action Changed;
        public SettingsData Data => _data;

        public SettingsService(SettingsData data, Action onChanged)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _onChanged = onChanged;
            ApplyMotion();
        }

        public void SetMusic(bool on) { _data.Music = on; Commit(); }
        public void SetSound(bool on) { _data.Sound = on; Commit(); }
        public void SetHaptics(bool on) { _data.Haptics = on; Commit(); }
        public void SetReducedMotion(bool on) { _data.ReducedMotion = on; Commit(); }
        public void SetColorblind(bool on) { _data.ColorblindSymbols = on; Commit(); }
        public void SetLightGameplay(bool on) { _data.LightGameplayBackground = on; Commit(); }
        public void SetSimulateAdFailure(bool on) { _data.DebugSimulateAdFailure = on; Commit(); }
        public void SetLanguage(string code) { _data.Language = code ?? ""; Commit(); }

        /// <summary>Cycles animation speed Relaxed (0.75) → Normal (1) → Fast (1.5).</summary>
        public void CycleAnimationSpeed()
        {
            _data.AnimationSpeed = _data.AnimationSpeed < 0.9f ? 1f : (_data.AnimationSpeed < 1.2f ? 1.5f : 0.75f);
            Commit();
        }

        public string AnimationSpeedLabel => _data.AnimationSpeed < 0.9f ? "Relaxed" : (_data.AnimationSpeed < 1.2f ? "Normal" : "Fast");

        private void Commit()
        {
            ApplyMotion();
            _onChanged?.Invoke();
            Changed?.Invoke();
        }

        private void ApplyMotion()
        {
            MotionPrefs.Reduced = _data.ReducedMotion;
            MotionPrefs.Speed = _data.AnimationSpeed;
        }
    }
}
