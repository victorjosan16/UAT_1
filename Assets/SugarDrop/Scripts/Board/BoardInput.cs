using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SugarDrop
{
    /// <summary>
    /// Glisare pe tablă: pui degetul pe un capac și tragi spre vecin (sus/jos/stânga/dreapta) ca să le schimbi.
    /// În modul „țintire” (bomba), o atingere simplă pe tablă alege celula. Funcționează cu degetul pe telefon
    /// și cu mouse-ul în Editor (New Input System).
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Cât trebuie tras degetul (unități din lume) ca să conteze ca glisare.")]
        [SerializeField] private float dragThreshold = 0.3f;

        /// <summary>Jucătorul a atins tabla (pentru indiciul care apare după o pauză lungă).</summary>
        public event Action Touched;

        /// <summary>Dacă e setat, următoarea atingere pe tablă alege o celulă (ex. bomba) în loc de glisare.</summary>
        public Action<Vector2Int> TapTarget { get; set; }

        private bool _tracking;
        private bool _pressOnUi;
        private Vector2Int _startCell;
        private Vector2 _startWorld;

        private void OnDisable() => _tracking = false;

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
                Touched?.Invoke();
            }
            if (_pressOnUi) return;

            if (TapTarget != null)
            {
                if (pointer.press.wasReleasedThisFrame && _tracking && board.TryGetCell(world, out Vector2Int cell) && cell == _startCell)
                {
                    Action<Vector2Int> target = TapTarget;
                    TapTarget = null;
                    target(cell);
                }
                if (pointer.press.wasReleasedThisFrame) _tracking = false;
                return;
            }

            if (_tracking && pointer.press.isPressed)
            {
                Vector2 delta = world - _startWorld;
                if (delta.magnitude >= dragThreshold)
                {
                    Vector2Int direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
                        : new Vector2Int(0, delta.y > 0f ? 1 : -1);
                    board.TrySwap(_startCell, _startCell + direction);
                    _tracking = false;
                }
            }

            if (pointer.press.wasReleasedThisFrame) _tracking = false;
        }
    }
}
