using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Animație procedurală simplă a târnăcopului: la fiecare lovitură rotește pivotul
    /// înainte și înapoi. Va fi înlocuită de un Animator când avem model de personaj.
    /// </summary>
    public class PickaxeSwing : MonoBehaviour
    {
        [SerializeField] private PlayerMiner miner;
        [Tooltip("Obiectul gol (umărul) de care e prins târnăcopul.")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Vector3 restEuler = new(-20f, 0f, 0f);
        [SerializeField] private Vector3 strikeEuler = new(100f, 0f, 0f);
        [Tooltip("Durata unei lovituri. Ține-o mai mică decât HitInterval.")]
        [SerializeField, Min(0.05f)] private float duration = 0.25f;
        [Tooltip("0 = poziție de repaus, 1 = lovitură. Implicit: coboară rapid, revine lent.")]
        [SerializeField] private AnimationCurve curve = new(
            new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));

        private float _progress = -1f; // -1 = nu animăm

        private void OnEnable()
        {
            miner.Swung += HandleSwung;
            pivot.localRotation = Quaternion.Euler(restEuler);
        }

        private void OnDisable() => miner.Swung -= HandleSwung;

        private void HandleSwung(MineBlock _) => _progress = 0f;

        private void Update()
        {
            if (_progress < 0f) return;

            _progress += Time.deltaTime / duration;
            float t = _progress >= 1f ? 0f : curve.Evaluate(_progress);
            pivot.localRotation = Quaternion.Euler(Vector3.LerpUnclamped(restEuler, strikeEuler, t));

            if (_progress >= 1f) _progress = -1f;
        }
    }
}
