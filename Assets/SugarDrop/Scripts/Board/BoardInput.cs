using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SugarDrop
{
    /// <summary>
    /// Mutările pe tablă: pui degetul pe o bomboană și o tragi spre o vecină (sus/jos/stânga/dreapta) ca să le schimbi.
    /// O atingere fără tragere selectează piesa; a doua atingere pe o vecină face schimbul. În modul „țintire”
    /// (ajutorul bombă), atingerea alege celula pentru bombă. Merge cu degetul pe telefon și cu mouse-ul în Editor.
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Cât trebuie tras degetul (în celule) ca să înceapă schimbul.")]
        [SerializeField] private float dragThreshold = 0.4f;

        /// <summary>Jucătorul a atins tabla (pentru indiciul care apare după o pauză lungă).</summary>
        public event Action Touched;

        /// <summary>Dacă e setat, următoarea atingere pe tablă alege o celulă (ex. bomba) în loc de mutarea normală.</summary>
        public Action<Vector2Int> TapTarget { get; set; }

        private bool _tracking;
        private bool _pressOnUi;
        private bool _swiped;
        private Vector2Int _startCell;
        private Vector2 _startWorld;

        private void OnDisable()
        {
            _tracking = false;
            if (board != null) board.Press(null);
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || board == null || worldCamera == null) return;

            Vector2 world = worldCamera.ScreenToWorldPoint(pointer.position.ReadValue());

            if (pointer.press.wasPressedThisFrame)
            {
                // Atingerile pe butoane (UI) nu se transmit tablei.
                _pressOnUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (_pressOnUi) return;
                _tracking = board.TryGetCell(world, out _startCell);
                _startWorld = world;
                _swiped = false;
                if (_tracking && TapTarget == null) board.Press(_startCell);
                Touched?.Invoke();
            }
            if (_pressOnUi) return;

            if (_tracking && !_swiped && pointer.press.isPressed && TapTarget == null)
            {
                // Tragere: când degetul a mers destul într-o direcție, schimbăm cu vecina din direcția aceea.
                Vector2 delta = world - _startWorld;
                if (delta.magnitude >= dragThreshold * board.CellSize)
                {
                    Vector2Int direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                        ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
                        : new Vector2Int(0, delta.y > 0f ? 1 : -1);
                    _swiped = true;
                    board.Press(null);
                    board.Swap(_startCell, _startCell + direction);
                }
            }

            if (!pointer.press.wasReleasedThisFrame) return;
            bool tap = _tracking && !_swiped && board.TryGetCell(world, out Vector2Int cell) && cell == _startCell;
            _tracking = false;
            board.Press(null);
            if (!tap) return;

            if (TapTarget != null)
            {
                Action<Vector2Int> target = TapTarget;
                TapTarget = null;
                target(_startCell);
                return;
            }
            board.Tap(_startCell);
        }
    }
}
