using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Un bloc din carieră: are viață, primește lovituri și se sparge.
    /// Efectele vizuale și drop-ul sunt în alte scripturi, care ascultă evenimentele de aici.
    /// Ține și o listă statică cu toate blocurile active (pentru muncitori), fără Find().
    /// </summary>
    public class MineBlock : MonoBehaviour, IPoolable
    {
        private static readonly List<MineBlock> ActiveBlocks = new();

        /// <summary>Toate blocurile active din scenă.</summary>
        public static IReadOnlyList<MineBlock> Active => ActiveBlocks;

        [SerializeField] private BlockData data;
        [Tooltip("Renderer-ul cubului vizual (copilul „Visual”).")]
        [SerializeField] private Renderer blockRenderer;

        /// <summary>Blocul a primit o lovitură (dar poate fi încă întreg).</summary>
        public event Action<MineBlock> Damaged;

        /// <summary>Blocul s-a spart. Anunțat înainte ca blocul să dispară.</summary>
        public event Action<MineBlock> Broken;

        public BlockData Data => data;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health01 => MaxHealth > 0f ? Health / MaxHealth : 0f;
        public bool IsAlive => Health > 0f && gameObject.activeInHierarchy;
        public int Hardness => data != null ? data.Hardness : int.MaxValue;

        /// <summary>Cine „a rezervat” blocul (un muncitor), ca să nu lucreze doi la același.</summary>
        public UnityEngine.Object ClaimedBy { get; set; }

        private float _healthMultiplier = 1f;

        private void Awake()
        {
            if (data != null) ApplyVisual();
        }

        private void OnEnable()
        {
            ActiveBlocks.Add(this);
            ResetHealth();
        }

        private void OnDisable()
        {
            ActiveBlocks.Remove(this);
            ClaimedBy = null;
        }

        /// <summary>Configurează blocul cu un tip de date și un multiplicator de viață (generatorul de straturi).</summary>
        public void Init(BlockData newData, float healthMultiplier = 1f)
        {
            data = newData;
            _healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
            ApplyVisual();
            ResetHealth();
        }

        /// <summary>Poate fi spart de o unealtă cu acest nivel?</summary>
        public bool CanBeMinedBy(int tier) => data != null && data.Hardness <= tier;

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(this);
            GameEvents.RaiseBlockHit(this);

            if (Health <= 0f) Break();
        }

        private void Break()
        {
            Broken?.Invoke(this);
            GameEvents.RaiseBlockDestroyed(this);
            if (ObjectPool.Instance != null && TryGetComponent(out PooledObject _)) ObjectPool.Instance.Despawn(gameObject);
            else gameObject.SetActive(false);
        }

        private void ResetHealth()
        {
            MaxHealth = data != null ? data.MaxHealth * _healthMultiplier : 0f;
            Health = MaxHealth;
        }

        private void ApplyVisual()
        {
            if (blockRenderer != null && data != null && data.BlockMaterial != null)
                blockRenderer.sharedMaterial = data.BlockMaterial;
        }

        public void OnSpawned() => ClaimedBy = null;
        public void OnDespawned() => ClaimedBy = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveBlocks.Clear();
    }
}
