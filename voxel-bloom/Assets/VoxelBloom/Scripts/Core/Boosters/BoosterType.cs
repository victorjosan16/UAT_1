namespace VoxelBloom.Core
{
    public enum BoosterType
    {
        /// <summary>Destroys a configurable number of voxels around a tapped voxel, any colour.</summary>
        Hammer = 0,
        /// <summary>Rearranges the board blocks, preserving a playable move when possible.</summary>
        Shuffle = 1,
        /// <summary>Destroys a large group of voxels of one selected colour.</summary>
        ColorBomb = 2,
        /// <summary>"Eliminare": removes one queued attack, freeing its slot (un-jams the queue).</summary>
        Eliminate = 3,
    }
}
