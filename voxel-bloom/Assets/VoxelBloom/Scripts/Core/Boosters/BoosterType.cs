namespace VoxelBloom.Core
{
    public enum BoosterType
    {
        /// <summary>Destroys a configurable number of voxels around a tapped voxel, any colour.</summary>
        Hammer = 0,
        /// <summary>Conveyor: reorders the queued trays while keeping the level solvable.</summary>
        Shuffle = 1,
        /// <summary>Destroys a large group of voxels of one selected colour.</summary>
        ColorBomb = 2,
        /// <summary>"Eliminare": removes one queued attack, freeing its slot (un-jams the queue).</summary>
        Eliminate = 3,
        /// <summary>Conveyor: reverts the last tray placement (state snapshot).</summary>
        Undo = 4,
        /// <summary>Conveyor: opens a green "plus" station (extra placement capacity).</summary>
        ExtraStation = 5,
        /// <summary>Conveyor "Amestecă culorile": reorders the colours of the cubes on the belt and in the feeder.</summary>
        ColorShuffle = 6,
    }
}
