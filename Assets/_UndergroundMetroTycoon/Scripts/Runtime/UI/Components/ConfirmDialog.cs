using System;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Reusable yes/no confirmation built on the shared modal window.</summary>
    public sealed class ConfirmDialog : ModalPanel
    {
        private Text _message;
        private UIButton _confirm;
        private Action _onConfirm;

        public void Build(SfxPlayer sfx)
        {
            BuildFrame((RectTransform)transform.parent, "Confirmare", 620f, sfx, 900f);

            _message = UIFactory.Text("Message", Body, "", UITheme.SizeLabel, UITheme.TextDark, TextAnchor.MiddleCenter);
            _message.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            _message.rectTransform.anchorMax = Vector2.one;
            _message.rectTransform.offsetMin = Vector2.zero;
            _message.rectTransform.offsetMax = Vector2.zero;

            UIButton cancel = UIButton.Create(Body, "ANULEAZĂ", UIButtonStyle.Light, Close);
            UIFactory.Place(cancel.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(390f, 112f));

            _confirm = UIButton.Create(Body, "DA", UIButtonStyle.Danger, () =>
            {
                Action action = _onConfirm;
                _onConfirm = null;
                Close();
                action?.Invoke();
            });
            UIFactory.Place(_confirm.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(390f, 112f));
        }

        public void Show(string message, string confirmLabel, Action onConfirm)
        {
            _message.text = message;
            _confirm.SetLabel(confirmLabel);
            _onConfirm = onConfirm;
            Open();
        }
    }
}
