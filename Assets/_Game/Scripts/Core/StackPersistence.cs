using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Salvează și restaurează conținutul unei stive (jucător, camion, muncitori), ca resursele
    /// cărate să nu se piardă la închiderea jocului. Se pune lângă ResourceStack, cu o cheie unică.
    /// </summary>
    public class StackPersistence : MonoBehaviour
    {
        [SerializeField] private ResourceStack stack;
        [Tooltip("Cheie unică în salvare (ex. player, truck, worker1).")]
        [SerializeField] private string saveKey = "player";

        private readonly List<ResourceData> _contents = new();
        private readonly List<string> _ids = new();
        private bool _loaded;

        private void Start()
        {
            // În Start: capacitatea stivei a fost deja setată (statistici / definiție muncitor).
            Load();
            stack.Changed += OnChanged;
            _loaded = true;
        }

        private void OnDestroy()
        {
            if (stack != null) stack.Changed -= OnChanged;
        }

        private void Load()
        {
            if (SaveSystem.Instance == null) return;
            List<string> ids = SaveData.GetList(SaveSystem.Instance.Data.stacks, saveKey);
            if (ids == null) return;
            // Copie: TryAdd declanșează Changed, care ar rescrie lista în timp ce o parcurgem.
            var copy = new List<string>(ids);
            foreach (string id in copy)
            {
                ResourceData resource = ResourceCatalog.Find(id);
                if (resource != null) stack.TryAdd(resource);
            }
        }

        private void OnChanged(ResourceStack _)
        {
            if (!_loaded || SaveSystem.Instance == null) return;
            stack.GetContents(_contents);
            _ids.Clear();
            foreach (ResourceData resource in _contents) _ids.Add(resource.Id);
            SaveData.SetList(SaveSystem.Instance.Data.stacks, saveKey, _ids);
            SaveSystem.Instance.MarkDirty();
        }
    }
}
