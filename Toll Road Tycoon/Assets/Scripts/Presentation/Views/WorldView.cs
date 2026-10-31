using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.Traffic;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Draws the road, plaza, markings and scenery from the simulation geometry, and rebuilds them
    /// whenever lanes or booths are added so the visible road always matches the routing.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        private const float LaneWidth = 1.5f;

        private GameSimulation _sim;
        private Transform _road;
        private Transform _scenery;
        private readonly List<BoothView> _booths = new List<BoothView>();
        private SpriteRenderer _ghost;
        private TextMesh _ghostLabel;
        private bool _dirty;

        public IReadOnlyList<BoothView> Booths => _booths;

        /// <summary>World rect of the next purchasable booth slot, if any.</summary>
        public Rect? NextSlotRect { get; private set; }

        public float MinX { get; private set; }
        public float MaxX { get; private set; }

        public void Bind(GameSimulation sim)
        {
            if (_sim != null)
            {
                _sim.Lanes.GeometryChanged -= MarkDirty;
            }
            _sim = sim;
            _sim.Lanes.GeometryChanged += MarkDirty;
            if (_scenery == null) BuildScenery();
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_sim == null) return;
            _sim.Lanes.GeometryChanged -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (_dirty)
            {
                _dirty = false;
                Rebuild();
            }
            UpdateGhost();
        }

        private void Rebuild()
        {
            if (_road != null) Destroy(_road.gameObject);
            _booths.Clear();
            _road = new GameObject("Road").transform;
            _road.SetParent(transform, false);

            var layout = _sim.Config.Layout;
            var lanes = _sim.Lanes;
            int laneCount = lanes.ActiveLaneCount;
            int boothCount = _sim.Booths.Count;

            // Horizontal extent of the tolled carriageway.
            float minLane = float.MaxValue, maxLane = float.MinValue;
            for (int i = 0; i < laneCount; i++)
            {
                minLane = Mathf.Min(minLane, (float)lanes.LaneX(i));
                maxLane = Mathf.Max(maxLane, (float)lanes.LaneX(i));
            }
            float minBooth = minLane, maxBooth = maxLane;
            int shownSlots = Mathf.Min(boothCount + 1, _sim.Config.MaxBooths);
            for (int s = 0; s < shownSlots; s++)
            {
                minBooth = Mathf.Min(minBooth, (float)lanes.BoothX(s));
                maxBooth = Mathf.Max(maxBooth, (float)lanes.BoothX(s));
            }
            MinX = Mathf.Min((float)layout.OncomingLaneX, minBooth) - 1.5f;
            MaxX = maxBooth + 1.5f;

            float half = LaneWidth * 0.5f;
            float spawnY = (float)layout.SpawnY - 4;
            float despawnY = (float)layout.DespawnY + 4;
            float decisionY = (float)layout.DecisionY;
            float exitY = (float)layout.ExitStartY;

            // Carriageways before and after the plaza.
            float roadL = minLane - half, roadR = maxLane + half;
            Box("Approach", Palette.Asphalt, roadL, roadR, spawnY, decisionY + 0.5f, Layers.Road);
            Box("Exit", Palette.Asphalt, roadL, roadR, exitY - 0.5f, despawnY, Layers.Road);
            EdgeLines(roadL, roadR, spawnY, decisionY);
            EdgeLines(roadL, roadR, exitY, despawnY);
            for (int i = 0; i < laneCount; i++)
            {
                for (int j = 0; j < laneCount; j++)
                {
                    float a = (float)lanes.LaneX(i), b = (float)lanes.LaneX(j);
                    if (b > a && b - a < LaneWidth + 0.2f)
                    {
                        Dashes((a + b) / 2, spawnY, decisionY);
                        Dashes((a + b) / 2, exitY, despawnY);
                    }
                }
            }

            // Plaza: widened apron covering every active booth lane and the next slot preview.
            float plazaL = Mathf.Min(roadL, minBooth - half - 0.1f);
            float plazaR = Mathf.Max(roadR, maxBooth + half + 0.1f);
            Box("Plaza", Palette.Asphalt, plazaL, plazaR, decisionY, exitY, Layers.Road);
            Box("PlazaEdgeL", Palette.Cream, plazaL - 0.12f, plazaL, decisionY, exitY, Layers.Marking);
            Box("PlazaEdgeR", Palette.Cream, plazaR, plazaR + 0.12f, decisionY, exitY, Layers.Marking);
            Box("StopLine", Palette.WithAlpha(Palette.White, 0.85f), plazaL, plazaR, (float)layout.StopLineY - 0.06f, (float)layout.StopLineY + 0.06f, Layers.Marking);

            // Oncoming (untolled) carriageway with a grass median.
            float ox = (float)layout.OncomingLaneX;
            Box("Oncoming", Palette.Asphalt, ox - half, ox + half, spawnY, despawnY, Layers.Road);
            EdgeLines(ox - half, ox + half, spawnY, despawnY);
            Arrows(ox, 180f, spawnY, despawnY);
            for (int i = 0; i < laneCount; i++)
            {
                Arrows((float)lanes.LaneX(i), 0f, spawnY, decisionY - 2);
            }

            // Booths: islands between booth lanes, booth houses, barriers.
            for (int i = 0; i < boothCount; i++)
            {
                var booth = _sim.Booths[i];
                float x = (float)lanes.BoothX(booth.SlotIndex);
                var go = new GameObject("Booth" + booth.SlotIndex);
                go.transform.SetParent(_road, false);
                var view = go.AddComponent<BoothView>();
                view.Build(booth, x, layout);
                _booths.Add(view);
            }
            if (boothCount > 0)
            {
                float bl = float.MaxValue, br = float.MinValue;
                foreach (var b in _sim.Booths)
                {
                    bl = Mathf.Min(bl, (float)lanes.BoothX(b.SlotIndex) - half - 0.1f);
                    br = Mathf.Max(br, (float)lanes.BoothX(b.SlotIndex) + half + 0.1f);
                }
                Box("CanopyShadow", Palette.Shadow, bl, br, 1.05f, 1.55f, Layers.Canopy - 1);
                var canopy = ArtLibrary.Get("TollBooths/canopy");
                if (canopy != null)
                {
                    // 9-sliced so one image stretches over any number of booths.
                    var sr = Shapes.Sprite(_road, "CanopyArt", canopy, Color.white, new Vector2((bl + br) / 2, 1.48f), Vector2.one, Layers.Canopy);
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(br - bl, 0.75f);
                }
                else
                {
                    Box("Canopy", Palette.Cream, bl, br, 1.2f, 1.75f, Layers.Canopy);
                    Box("CanopyStripe", Palette.Blue, bl, br, 1.62f, 1.75f, Layers.Canopy + 1);
                }
                Shapes.Label(_road, "TollSign", "TOLL", Palette.DarkBlue, new Vector2((bl + br) / 2, 1.42f), 0.5f, Layers.Canopy + 2);
            }

            // Placement preview for the next booth slot.
            NextSlotRect = null;
            _ghost = null;
            _ghostLabel = null;
            if (boothCount < _sim.Config.MaxBooths)
            {
                float gx = (float)lanes.BoothX(boothCount);
                var rect = new Rect(gx - half, (float)layout.BoothLaneStartY, LaneWidth, (float)(layout.BoothLaneEndY - layout.BoothLaneStartY));
                NextSlotRect = rect;
                _ghost = Shapes.Rounded(_road, "NextSlot", Palette.WithAlpha(Palette.White, 0.25f), rect.center, rect.size, Layers.Island);
                _ghostLabel = Shapes.Label(_road, "NextSlotPrice", "", Palette.White, new Vector2(gx, -4f), 0.45f, Layers.Canopy + 3);
            }

            // Toll office next to the plaza.
            Building(new Vector2(plazaR + 2.2f, -2.5f), new Vector2(2.4f, 1.8f), Palette.Red);
        }

        private void UpdateGhost()
        {
            if (_ghost == null || _sim == null) return;
            var block = _sim.Expansion.CanBuyBooth();
            string price = Money.Format(_sim.Expansion.NextBoothCost);
            if (block == ExpansionBlock.NeedsMoreLanes)
            {
                _ghostLabel.text = "+\n" + Localization.Loc.F("booth.needs_lanes", _sim.Expansion.LanesRequiredForNextBooth);
                _ghostLabel.color = Palette.Cream;
            }
            else
            {
                _ghostLabel.text = "+\n" + price;
                _ghostLabel.color = block == ExpansionBlock.None ? Palette.Gold : Palette.Cream;
            }
            float pulse = 0.18f + 0.1f * Mathf.Sin(Time.time * 3f);
            _ghost.color = Palette.WithAlpha(Palette.White, pulse);
        }

        private void Box(string name, Color color, float x0, float x1, float y0, float y1, int order)
        {
            Shapes.Rect(_road, name, color, new Vector2((x0 + x1) / 2, (y0 + y1) / 2), new Vector2(x1 - x0, y1 - y0), order);
        }

        private void EdgeLines(float left, float right, float y0, float y1)
        {
            Box("EdgeL", Palette.WithAlpha(Palette.White, 0.9f), left + 0.06f, left + 0.14f, y0, y1, Layers.Marking);
            Box("EdgeR", Palette.WithAlpha(Palette.White, 0.9f), right - 0.14f, right - 0.06f, y0, y1, Layers.Marking);
        }

        private void Dashes(float x, float y0, float y1)
        {
            for (float y = y0; y < y1 - 0.8f; y += 2f)
            {
                Shapes.Rect(_road, "Dash", Palette.WithAlpha(Palette.White, 0.85f), new Vector2(x, y + 0.5f), new Vector2(0.1f, 1f), Layers.Marking);
            }
        }

        private void Arrows(float x, float rotation, float y0, float y1)
        {
            for (float y = y0 + 6; y < y1 - 2; y += 12)
            {
                Shapes.Rect(_road, "ArrowStem", Palette.WithAlpha(Palette.White, 0.75f), new Vector2(x, y), new Vector2(0.14f, 0.8f), Layers.Marking, rotation);
                var dir = rotation == 0 ? 1 : -1;
                Shapes.Rect(_road, "ArrowHeadL", Palette.WithAlpha(Palette.White, 0.75f), new Vector2(x - 0.12f, y + 0.32f * dir), new Vector2(0.12f, 0.45f), Layers.Marking, rotation - 35);
                Shapes.Rect(_road, "ArrowHeadR", Palette.WithAlpha(Palette.White, 0.75f), new Vector2(x + 0.12f, y + 0.32f * dir), new Vector2(0.12f, 0.45f), Layers.Marking, rotation + 35);
            }
        }

        private void Building(Vector2 pos, Vector2 size, Color roof)
        {
            if (Shapes.Art(_road, "OfficeArt", "Buildings/toll_office", 0, pos, size.x * 1.2f, Layers.Decor) != null) return;
            Shapes.Rounded(_road, "BuildingShadow", Palette.Shadow, pos + new Vector2(0.15f, -0.15f), size, Layers.Decor - 1);
            Shapes.Rounded(_road, "Building", Palette.Cream, pos, size, Layers.Decor);
            Shapes.Rounded(_road, "Roof", roof, pos + new Vector2(0, size.y * 0.18f), new Vector2(size.x * 1.05f, size.y * 0.62f), Layers.Decor + 1);
            Shapes.Rect(_road, "Door", Palette.DarkBlue, pos + new Vector2(0, -size.y * 0.38f), new Vector2(0.35f, 0.3f), Layers.Decor + 2);
        }

        /// <summary>Grass, trees and houses; generated once from a fixed seed.</summary>
        private void BuildScenery()
        {
            _scenery = new GameObject("Scenery").transform;
            _scenery.SetParent(transform, false);
            var grass = ArtLibrary.Get("Environment/grass");
            if (grass != null)
            {
                var sr = Shapes.Sprite(_scenery, "GrassArt", grass, Color.white, Vector2.zero, Vector2.one, Layers.Ground);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(80, 140);
            }
            else
            {
                Shapes.Rect(_scenery, "Grass", Palette.Grass, new Vector2(0, 0), new Vector2(80, 140), Layers.Ground);
            }
            var rng = new System.Random(1234);
            var layout = _sim.Config.Layout;
            float maxRoadX = 0;
            foreach (var x in layout.BoothSlotX) maxRoadX = Mathf.Max(maxRoadX, (float)x);
            float onc = (float)layout.OncomingLaneX;
            for (int i = 0; i < 260; i++)
            {
                float x = (float)(rng.NextDouble() * 34 - 17);
                float y = (float)(rng.NextDouble() * 110 - 55);
                bool onRoad = x > onc - 1.4f && x < maxRoadX + 1.6f;
                bool nearOffice = x > maxRoadX + 0.6f && x < maxRoadX + 5f && y > -5 && y < 0.5f;
                if (onRoad || nearOffice) continue;
                float size = 0.9f + (float)rng.NextDouble() * 0.9f;
                var p = new Vector2(x, y);
                if (rng.NextDouble() < 0.06)
                {
                    if (Shapes.Art(_scenery, "HouseArt", "Buildings/house", i, p, 2.0f, Layers.Decor) != null) continue;
                    var houseColor = rng.NextDouble() < 0.5 ? Palette.Red : Palette.Blue;
                    Shapes.Rounded(_scenery, "HouseShadow", Palette.Shadow, p + new Vector2(0.15f, -0.15f), new Vector2(1.8f, 1.5f), Layers.Decor - 1);
                    Shapes.Rounded(_scenery, "House", Palette.Cream, p, new Vector2(1.8f, 1.5f), Layers.Decor);
                    Shapes.Rounded(_scenery, "HouseRoof", houseColor, p + new Vector2(0, 0.25f), new Vector2(1.9f, 0.9f), Layers.Decor + 1);
                    continue;
                }
                if (Shapes.Art(_scenery, "TreeArt", "Environment/tree", i, p, size * 1.25f, Layers.Tree) != null) continue;
                Shapes.Sprite(_scenery, "TreeShadow", SpriteFactory.SoftShadow, Palette.Shadow, p + new Vector2(0.2f, -0.25f), new Vector2(size * 1.3f, size * 1.3f), Layers.Tree - 2);
                Shapes.Circle(_scenery, "Tree", Palette.TreeDark, p, size, Layers.Tree - 1);
                Shapes.Circle(_scenery, "TreeTop", Palette.TreeLight, p + new Vector2(-size * 0.12f, size * 0.12f), size * 0.7f, Layers.Tree);
            }
            // Darker grass strip as a median between the carriageways.
            Shapes.Rect(_scenery, "Median", Palette.GrassDark, new Vector2(onc + 1.6f, 0), new Vector2(1.2f, 140), Layers.Ground + 1);
        }

        /// <summary>Returns the booth view under a world point, if any.</summary>
        public BoothView PickBooth(Vector2 world)
        {
            foreach (var b in _booths)
            {
                if (b.HitRect.Contains(world)) return b;
            }
            return null;
        }
    }
}
