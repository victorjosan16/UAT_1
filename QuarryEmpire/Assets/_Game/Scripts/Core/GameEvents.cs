using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// „Panou de anunțuri” global. Sistemele anunță ce s-a întâmplat (Raise...),
    /// iar cine e interesat ascultă (+=). Așa sistemele nu se cunosc direct între ele.
    /// Regulă: abonează-te în OnEnable, dezabonează-te în OnDisable.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Un bloc a fost spart complet.</summary>
        public static event Action<MineBlock> BlockDestroyed;

        /// <summary>O resursă a ajuns la jucător (resursa, poziția unde a fost prinsă).</summary>
        public static event Action<ResourceData, Vector3> ResourceCollected;

        public static void RaiseBlockDestroyed(MineBlock block) => BlockDestroyed?.Invoke(block);

        public static void RaiseResourceCollected(ResourceData resource, Vector3 position) =>
            ResourceCollected?.Invoke(resource, position);

        // Curățăm abonații la pornirea Play Mode (util când „Domain Reload” e dezactivat în Editor).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            BlockDestroyed = null;
            ResourceCollected = null;
        }
    }
}
