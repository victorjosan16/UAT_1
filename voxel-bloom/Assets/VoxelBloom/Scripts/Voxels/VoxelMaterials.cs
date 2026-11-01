using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Shared materials (one voxel material for all models keeps batching friendly).</summary>
    public static class VoxelMaterials
    {
        static Material voxel, fragment;

        public static Shader VoxelShader
        {
            get
            {
                var s = Resources.Load<Shader>("VoxelBloom/Shaders/VoxelLit");
                if (s == null) s = Shader.Find("VoxelBloom/VoxelLit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s == null) s = Shader.Find("Sprites/Default");
                return s;
            }
        }

        public static Material Voxel => voxel != null ? voxel : (voxel = new Material(VoxelShader) { name = "VoxelLit (runtime)" });

        public static Material Fragment
        {
            get
            {
                if (fragment != null) return fragment;
                fragment = new Material(VoxelShader) { name = "VoxelFragment (runtime)", enableInstancing = true };
                return fragment;
            }
        }
    }
}
