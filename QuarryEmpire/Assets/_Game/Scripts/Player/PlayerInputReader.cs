using UnityEngine;
using UnityEngine.InputSystem;

namespace QuarryEmpire
{
    /// <summary>
    /// Citește direcția de mișcare din New Input System.
    /// Surse: joystick-ul de pe ecran (simulează un gamepad), un gamepad real, WASD și săgeți.
    /// Celelalte scripturi citesc doar proprietatea Move, fără să știe de unde vine.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>Direcția dorită, cu lungime între 0 și 1.</summary>
        public Vector2 Move { get; private set; }

        private InputAction _moveAction;

        private void Awake()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

            // Joystick-ul on-screen (OnScreenStick) trimite valori pe „<Gamepad>/leftStick”.
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }

        private void OnEnable() => _moveAction.Enable();

        private void OnDisable()
        {
            _moveAction.Disable();
            Move = Vector2.zero;
        }

        private void OnDestroy() => _moveAction.Dispose();

        private void Update()
        {
            Move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
        }
    }
}
