using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Lista tuturor resurselor, ca să le putem găsi după id la încărcarea salvării.</summary>
    [DefaultExecutionOrder(-450)]
    public class ResourceCatalog : MonoBehaviour
    {
        private static readonly Dictionary<string, ResourceData> ById = new();

        [SerializeField] private List<ResourceData> resources = new();

        private void Awake()
        {
            ById.Clear();
            foreach (ResourceData resource in resources)
                if (resource != null && !string.IsNullOrEmpty(resource.Id)) ById[resource.Id] = resource;
        }

        public static ResourceData Find(string id) =>
            id != null && ById.TryGetValue(id, out ResourceData resource) ? resource : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ById.Clear();
    }
}
