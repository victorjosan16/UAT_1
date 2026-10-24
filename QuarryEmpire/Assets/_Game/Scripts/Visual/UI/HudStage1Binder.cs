using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// TEMPORAR (până la Etapa 2/5): alimentează HUD-ul din evenimentele existente, fără să adauge economie.
    /// - Progres = blocuri sparte / blocuri din carieră (date reale).
    /// - Bani = valoarea resurselor colectate (SellPrice), doar AFIȘAT; nu există încă bani reali.
    /// - Misiune = obiective de afișare din listă (fără recompense).
    /// Când apar sistemele reale, dezactivează acest component și apelează direct HudController.
    /// </summary>
    public class HudStage1Binder : MonoBehaviour
    {
        [Serializable]
        private class DisplayMission
        {
            public string description = "Sparge 10 blocuri";
            [Tooltip("Gol = orice bloc spart. Setat = se numără resursele colectate de acest tip.")]
            public ResourceData resource;
            [Min(1)] public int target = 10;
        }

        [SerializeField] private HudController hud;
        [SerializeField, Min(1)] private int displayLevel = 1;
        [Tooltip("Afișează valoarea resurselor colectate în pastila de bani (placeholder Etapa 1).")]
        [SerializeField] private bool showCollectedValueAsMoney = true;
        [SerializeField] private List<DisplayMission> missions = new()
        {
            new DisplayMission { description = "Sparge 10 blocuri", target = 10 },
            new DisplayMission { description = "Sparge 25 de blocuri", target = 25 },
        };

        private int _totalBlocks;
        private int _brokenBlocks;
        private long _collectedValue;
        private int _missionIndex;
        private int _missionProgress;

        private void OnEnable()
        {
            GameEvents.BlockDestroyed += HandleBlockDestroyed;
            GameEvents.ResourceCollected += HandleResourceCollected;
        }

        private void OnDisable()
        {
            GameEvents.BlockDestroyed -= HandleBlockDestroyed;
            GameEvents.ResourceCollected -= HandleResourceCollected;
        }

        private IEnumerator Start()
        {
            if (hud == null) yield break;
            hud.SetLevel(displayLevel);
            hud.SetMoney(0, false);
            RefreshMission();
            // DebugBlockGrid construiește blocurile în Start; numărăm după primul frame.
            yield return null;
            _totalBlocks = FindObjectsByType<MineBlock>(FindObjectsSortMode.None).Length + _brokenBlocks;
            RefreshProgress();
        }

        private void HandleBlockDestroyed(MineBlock block)
        {
            _brokenBlocks++;
            RefreshProgress();
            DisplayMission mission = CurrentMission;
            if (mission != null && mission.resource == null) AdvanceMission(1);
        }

        private void HandleResourceCollected(ResourceData resource, Vector3 _)
        {
            if (resource == null) return;
            if (showCollectedValueAsMoney && hud != null)
            {
                _collectedValue += resource.SellPrice;
                hud.SetMoney(_collectedValue);
            }
            DisplayMission mission = CurrentMission;
            if (mission != null && mission.resource == resource) AdvanceMission(1);
        }

        private DisplayMission CurrentMission =>
            missions.Count > 0 ? missions[Mathf.Min(_missionIndex, missions.Count - 1)] : null;

        private void AdvanceMission(int amount)
        {
            DisplayMission mission = CurrentMission;
            if (mission == null) return;
            _missionProgress += amount;
            if (_missionProgress >= mission.target && _missionIndex < missions.Count - 1)
            {
                // Misiune îndeplinită: puls + următoarea din listă.
                _missionIndex++;
                _missionProgress = 0;
                if (hud != null) hud.PunchMission();
            }
            RefreshMission();
        }

        private void RefreshMission()
        {
            if (hud == null) return;
            DisplayMission mission = CurrentMission;
            if (mission == null) hud.SetMission("Explorează cariera", 0, 1);
            else hud.SetMission(mission.description, _missionProgress, mission.target);
        }

        private void RefreshProgress()
        {
            if (hud == null) return;
            float value = _totalBlocks > 0 ? (float)_brokenBlocks / _totalBlocks : 0f;
            hud.SetProgress(value, $"Carieră {_brokenBlocks}/{Mathf.Max(_totalBlocks, _brokenBlocks)}");
        }
    }
}
