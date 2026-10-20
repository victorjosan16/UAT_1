using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Setări globale ale jocului la pornire. Va crește în etapele următoare
    /// (încărcare salvare, stare joc etc.).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        [Tooltip("FPS țintă. Pe Android implicit e 30, noi vrem 60 pentru fluiditate.")]
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            // Ecranul nu se stinge cât timp jucătorul se joacă.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
