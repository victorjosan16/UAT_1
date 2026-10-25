using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SugarDrop
{
    /// <summary>
    /// Atingerea pe tablă: pui degetul pe o bomboană (se ridică, grupul ei strălucește) și îl ridici pe aceeași
    /// celulă ca să spargi grupul sau să activezi piesa specială. Dacă tragi degetul pe altă celulă, nu se întâmplă
    /// nimic (poți răzgândi). În modul „țintire” (ajutorul bombă), atingerea alege celula pentru bombă.
    /// Merge cu degetul pe telefon și cu mouse-ul în Editor (New Input System).
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private Camera worldCamera;

        /// <summary>Jucătorul a atins tabla (pentru indiciul care apare după o pauză lungă).</summary>
        public event Action Touched;

        /// <summary>Dacă e setat, următoarea atingere pe tablă alege o celulă (ex. bomba) în loc de atingerea normală.</summary>
        public Action<Vector2Int> TapTarget { get; set; }

        private bool _tracking;
        private bool _pressOnUi;
        private Vector2Int _startCell;

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
                if (_tracking && TapTarget == null) board.Press(_startCell);
                Touched?.Invoke();
            }
            if (_pressOnUi) return;

            if (_tracking && pointer.press.isPressed && TapTarget == null)
            {
                // Degetul a plecat de pe celulă: renunțăm la evidențiere (atingerea nu se mai face).
                if (!board.TryGetCell(world, out Vector2Int current) || current != _startCell) board.Press(null);
            }

            if (!pointer.press.wasReleasedThisFrame) return;
            bool sameCell = _tracking && board.TryGetCell(world, out Vector2Int cell) && cell == _startCell;
            _tracking = false;
            board.Press(null);
            if (!sameCell) return;

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
