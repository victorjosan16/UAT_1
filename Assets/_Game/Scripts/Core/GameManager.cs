using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Setări globale la pornire și starea jocului: meniu (pauză) sau joc activ.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("FPS țintă. Pe Android implicit e 30, noi vrem 60 pentru fluiditate.")]
        [SerializeField] private int targetFrameRate = 60;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = targetFrameRate;
            // Ecranul nu se stinge cât timp jucătorul se joacă.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Până la apăsarea „JOACĂ” lumea e oprită.
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        /// <summary>Pornește jocul (apelat de meniul principal).</summary>
        public void StartGame()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            Time.timeScale = 1f;
            GameEvents.RaiseGameStarted();
        }

        /// <summary>Pune pauză sau reia jocul (panoul de setări).</summary>
        public void SetPaused(bool paused)
        {
            if (!IsPlaying) return;
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
