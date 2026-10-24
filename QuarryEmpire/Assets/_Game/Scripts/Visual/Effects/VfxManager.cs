using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Punct unic pentru efectele vizuale. Fiecare efect e UN singur ParticleSystem refolosit
    /// (Emit la poziția dorită), deci nu se creează obiecte noi în timpul jocului — e cea mai ieftină
    /// formă de pooling pentru particule. Textul plutitor vine din ObjectPool-ul existent.
    /// Sistemele de particule sunt create de VisualSetup ca copii și pot fi reglate din Inspector.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class VfxManager : MonoBehaviour
    {
        public static VfxManager Instance { get; private set; }

        [Header("Minat")]
        [SerializeField] private ParticleSystem impactChips;
        [SerializeField] private ParticleSystem impactDust;
        [SerializeField] private ParticleSystem breakChunks;
        [SerializeField] private ParticleSystem breakDust;

        [Header("Colectare / vânzare / upgrade")]
        [SerializeField] private ParticleSystem collectSparkle;
        [SerializeField] private ParticleSystem sellCoins;
        [SerializeField] private ParticleSystem upgradeSparkle;

        [Header("Cantități (înmulțite cu densitatea profilului de performanță)")]
        [SerializeField, Min(0)] private int impactChipCount = 6;
        [SerializeField, Min(0)] private int impactDustCount = 2;
        [SerializeField, Min(0)] private int breakChunkCount = 10;
        [SerializeField, Min(0)] private int breakDustCount = 5;
        [SerializeField, Min(0)] private int collectCount = 5;
        [SerializeField, Min(0)] private int sellCoinCount = 12;
        [SerializeField, Min(0)] private int upgradeCount = 24;

        [Header("Text plutitor")]
        [SerializeField] private FloatingText floatingTextPrefab;
        [Tooltip("Limită de texte vizibile simultan (protejează FPS-ul la colectări rapide).")]
        [SerializeField, Min(1)] private int maxFloatingTexts = 10;

        /// <summary>0..1, setat de PerformanceManager (profilul 30 FPS emite mai puține particule).</summary>
        public float Density { get; set; } = 1f;

        internal int ActiveFloatingTexts { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayImpact(Vector3 position, Color color)
        {
            Emit(impactChips, position, impactChipCount, color);
            Emit(impactDust, position, impactDustCount, null);
        }

        public void PlayBreak(Vector3 position, Color color)
        {
            Emit(breakChunks, position, breakChunkCount, color);
            Emit(breakDust, position, breakDustCount, null);
        }

        public void PlayCollect(Vector3 position, Color color) => Emit(collectSparkle, position, collectCount, color);

        /// <summary>Efect de vânzare (pentru zona de Sell din Etapa 2): ploaie de monede + text.</summary>
        public void PlaySell(Vector3 position, int amount)
        {
            Emit(sellCoins, position, sellCoinCount, null);
            if (amount > 0) ShowFloatingText(position + Vector3.up * 1.5f, $"+{amount}$", new Color(1f, 0.84f, 0.25f), 1.3f);
        }

        /// <summary>Feedback la upgrade (pentru Etapa 3): scântei care urcă în jurul țintei.</summary>
        public void PlayUpgrade(Vector3 position)
        {
            Emit(upgradeSparkle, position, upgradeCount, null);
            ShowFloatingText(position + Vector3.up * 2.2f, "UPGRADE!", new Color(0.55f, 0.85f, 1f), 1.2f);
        }

        public void ShowFloatingText(Vector3 position, string text, Color color, float scale = 1f)
        {
            if (floatingTextPrefab == null || ObjectPool.Instance == null) return;
            if (ActiveFloatingTexts >= maxFloatingTexts) return;
            FloatingText instance = ObjectPool.Instance.Spawn(floatingTextPrefab, position, Quaternion.identity);
            instance.Play(this, text, color, scale);
        }

        private void Emit(ParticleSystem system, Vector3 position, int count, Color? color)
        {
            if (system == null) return;
            int scaled = Mathf.RoundToInt(count * Mathf.Clamp01(Density));
            if (scaled <= 0) return;

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true
            };
            if (color.HasValue) emitParams.startColor = color.Value;
            system.Emit(emitParams, scaled);
        }

        [ContextMenu("Test: Sell")]
        private void TestSell() => PlaySell(transform.position, 25);

        [ContextMenu("Test: Upgrade")]
        private void TestUpgrade() => PlayUpgrade(transform.position);
    }
}
