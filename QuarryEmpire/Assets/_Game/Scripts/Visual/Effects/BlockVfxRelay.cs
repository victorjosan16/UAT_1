using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Pe prefab-ul Block: transformă evenimentele MineBlock (Damaged / Broken) în particule.
    /// Doar ascultă — nu schimbă viața blocului și nici drop-ul.
    /// </summary>
    [RequireComponent(typeof(MineBlock))]
    public class BlockVfxRelay : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Înălțimea (față de centrul blocului) unde apar așchiile la lovitură.")]
        [SerializeField] private float impactHeight = 0.35f;

        private MineBlock _block;

        private void Awake() => _block = GetComponent<MineBlock>();

        private void OnEnable()
        {
            _block.Damaged += HandleDamaged;
            _block.Broken += HandleBroken;
        }

        private void OnDisable()
        {
            _block.Damaged -= HandleDamaged;
            _block.Broken -= HandleBroken;
        }

        private void HandleDamaged(MineBlock block)
        {
            // Ultima lovitură e acoperită de HandleBroken (spargerea are efectul ei).
            if (block.Health <= 0f || VfxManager.Instance == null) return;
            VfxManager.Instance.PlayImpact(transform.position + Vector3.up * impactHeight, BlockColor(block));
        }

        private void HandleBroken(MineBlock block)
        {
            if (VfxManager.Instance == null) return;
            VfxManager.Instance.PlayBreak(transform.position, BlockColor(block));
        }

        private static Color BlockColor(MineBlock block)
        {
            BlockData data = block.Data;
            if (data == null) return Color.gray;
            Material material = data.BlockMaterial;
            if (material != null && material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            return data.DroppedResource != null ? data.DroppedResource.UiColor : Color.gray;
        }
    }
}
