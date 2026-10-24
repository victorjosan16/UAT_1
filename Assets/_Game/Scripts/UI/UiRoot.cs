using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Pe Canvas-ul principal. Oferă tema și Canvas-ul celorlalte scripturi de UI, care își
    /// construiesc singure elementele din cod.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public class UiRoot : MonoBehaviour
    {
        public static UiTheme Theme { get; private set; }
        public static RectTransform CanvasRect { get; private set; }

        [SerializeField] private UiTheme theme;
        [Tooltip("Modulul de input al EventSystem-ului (atingeri pe butoane și joystick).")]
        [SerializeField] private InputSystemUIInputModule inputModule;

        private void Awake()
        {
            Theme = theme != null ? theme : ScriptableObject.CreateInstance<UiTheme>();
            CanvasRect = (RectTransform)transform;
            // Dacă modulul nu are acțiuni (scena e generată din cod), îi dăm acțiunile implicite pentru UI.
            if (inputModule != null && inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
        }
    }
}
