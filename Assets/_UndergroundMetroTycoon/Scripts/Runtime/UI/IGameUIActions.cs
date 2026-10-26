namespace UndergroundMetroTycoon.Game
{
    /// <summary>Commands the UI can issue; implemented by the game bootstrap so the UI never mutates game state directly.</summary>
    public interface IGameUIActions
    {
        void AcceptContract(string contractId);
        void ClaimContract(string contractId);
        void OpenContracts();
        void OpenSettings();
        void SetSoundEnabled(bool enabled);
        void SetGraphicsQuality(int quality);
        void ResetProgress();
    }
}
