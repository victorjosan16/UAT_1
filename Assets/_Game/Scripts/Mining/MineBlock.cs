using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Un bloc din carieră: are viață, primește lovituri și se sparge.
    /// Efectele vizuale și drop-ul sunt în alte scripturi, care ascultă evenimentele de aici.
    /// </summary>
    public class MineBlock : MonoBehaviour
    {
        [SerializeField] private BlockData data;
        [Tooltip("Renderer-ul cubului vizual (copilul „Visual”).")]
        [SerializeField] private Renderer blockRenderer;

        /// <summary>Blocul a primit o lovitură (dar poate fi încă întreg).</summary>
        public event Action<MineBlock> Damaged;

        /// <summary>Blocul s-a spart. Anunțat înainte ca blocul să fie dezactivat.</summary>
        public event Action<MineBlock> Broken;

        public BlockData Data => data;
        public float Health { get; private set; }
        public float Health01 => data != null ? Health / data.MaxHealth : 0f;
        public bool IsAlive => Health > 0f && gameObject.activeInHierarchy;

        private void Awake()
        {
            if (data != null) ApplyVisual();
        }

        private void OnEnable() => ResetHealth();

        /// <summary>Configurează blocul cu un alt tip de date (folosit de generatorul de grilă).</summary>
        public void Init(BlockData newData)
        {
            data = newData;
            ApplyVisual();
            ResetHealth();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(this);

            if (Health <= 0f) Break();
        }

        private void Break()
        {
            Broken?.Invoke(this);
            GameEvents.RaiseBlockDestroyed(this);
            gameObject.SetActive(false);
        }

        private void ResetHealth() => Health = data != null ? data.MaxHealth : 0f;

        private void ApplyVisual()
        {
            if (blockRenderer != null && data.BlockMaterial != null)
                blockRenderer.sharedMaterial = data.BlockMaterial;
        }
    }
}
