using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Arată starea unui bloc: o bară mică de viață (după prima lovitură) și un lacăt
    /// dacă blocul e prea dur pentru târnăcopul actual al jucătorului.
    /// </summary>
    public class BlockStatusVisual : MonoBehaviour
    {
        [SerializeField] private MineBlock block;
        [Tooltip("Obiectul cu bara de viață (ascuns cât blocul e întreg).")]
        [SerializeField] private GameObject healthBar;
        [Tooltip("Pivotul umplerii (scalat pe X de la 0 la 1, ancorat în stânga).")]
        [SerializeField] private Transform healthFill;
        [SerializeField] private GameObject lockIcon;

        private void OnEnable()
        {
            block.Damaged += OnDamaged;
            block.Initialized += OnInitialized;
            GameEvents.Upgraded += OnUpgraded;
            Refresh();
        }

        private void OnDisable()
        {
            block.Damaged -= OnDamaged;
            block.Initialized -= OnInitialized;
            GameEvents.Upgraded -= OnUpgraded;
        }

        private void OnInitialized(MineBlock _) => Refresh();
        private void OnUpgraded(UpgradeDefinition _, int __) => RefreshLock();

        private void OnDamaged(MineBlock _)
        {
            if (healthBar != null && !healthBar.activeSelf) healthBar.SetActive(true);
            if (healthFill != null) healthFill.localScale = new Vector3(Mathf.Max(0.02f, block.Health01), 1f, 1f);
        }

        private void Refresh()
        {
            if (healthBar != null) healthBar.SetActive(false);
            if (healthFill != null) healthFill.localScale = Vector3.one;
            RefreshLock();
        }

        private void RefreshLock()
        {
            if (lockIcon == null) return;
            PlayerRoot player = PlayerRoot.Instance;
            bool tooHard = player != null && player.Stats != null && block.Data != null && !block.CanBeMinedBy(player.Stats.MiningTier);
            if (lockIcon.activeSelf != tooHard) lockIcon.SetActive(tooHard);
        }
    }
}
