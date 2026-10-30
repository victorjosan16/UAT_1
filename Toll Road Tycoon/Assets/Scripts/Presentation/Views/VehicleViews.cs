using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Traffic;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>One pooled vehicle visual (top-down body, cabin glass, optional cargo, shadow).</summary>
    public sealed class VehicleView : MonoBehaviour
    {
        public string TypeId { get; private set; }
        public Vehicle Vehicle { get; set; }
        private SpriteRenderer _paint;
        private SpriteRenderer _beacon;

        public static VehicleView Create(Transform parent, VehicleTypeConfig type)
        {
            var go = new GameObject("Vehicle_" + type.Id);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<VehicleView>();
            view.TypeId = type.Id;
            view.BuildShape(type);
            return view;
        }

        private void BuildShape(VehicleTypeConfig type)
        {
            var t = transform;
            float w = (float)type.Width;
            float l = (float)type.Length;
            Shapes.Sprite(t, "Shadow", SpriteFactory.SoftShadow, Palette.Shadow, new Vector2(0.06f, -0.08f), new Vector2(w * 1.5f, l * 1.25f), Layers.VehicleShadow);
            switch (type.Id)
            {
                case "truck":
                case "heavy_truck":
                {
                    float cab = 0.55f;
                    var cargoColor = type.Id == "truck" ? Palette.Hex(0x2F8FE0) : Palette.Hex(0xD9DEE3);
                    Shapes.Rounded(t, "Cargo", cargoColor, new Vector2(0, -cab * 0.5f + 0.02f), new Vector2(w, l - cab), Layers.Vehicle);
                    _paint = Shapes.Rounded(t, "Cab", Palette.Red, new Vector2(0, l * 0.5f - cab * 0.5f), new Vector2(w * 0.92f, cab), Layers.Vehicle + 1);
                    Shapes.Rect(t, "Glass", Palette.Glass, new Vector2(0, l * 0.5f - 0.14f), new Vector2(w * 0.7f, 0.12f), Layers.Vehicle + 2);
                    break;
                }
                case "bus":
                {
                    _paint = Shapes.Rounded(t, "Body", Palette.Gold, Vector2.zero, new Vector2(w, l), Layers.Vehicle);
                    Shapes.Rect(t, "Windshield", Palette.Glass, new Vector2(0, l * 0.5f - 0.16f), new Vector2(w * 0.78f, 0.14f), Layers.Vehicle + 1);
                    Shapes.Rect(t, "Roof", Palette.Hex(0xFFE08A), new Vector2(0, -0.05f), new Vector2(w * 0.6f, l * 0.7f), Layers.Vehicle + 1);
                    break;
                }
                default:
                {
                    _paint = Shapes.Rounded(t, "Body", Palette.White, Vector2.zero, new Vector2(w, l), Layers.Vehicle);
                    float cabinLen = l * (type.Id == "van" ? 0.6f : 0.45f);
                    Shapes.Rounded(t, "Cabin", Palette.Glass, new Vector2(0, type.Id == "van" ? l * 0.08f : -l * 0.02f), new Vector2(w * 0.78f, cabinLen), Layers.Vehicle + 1);
                    Shapes.Rounded(t, "RoofPanel", Palette.WithAlpha(Palette.White, 0.85f), new Vector2(0, type.Id == "van" ? -l * 0.02f : -l * 0.06f), new Vector2(w * 0.66f, cabinLen * 0.6f), Layers.Vehicle + 2);
                    if (type.IsEmergency)
                    {
                        Shapes.Rect(t, "Stripe", Palette.Red, new Vector2(0, -l * 0.33f), new Vector2(w, 0.1f), Layers.Vehicle + 3);
                        _beacon = Shapes.Rect(t, "Beacon", Palette.Red, new Vector2(0, 0.02f), new Vector2(w * 0.5f, 0.1f), Layers.Vehicle + 4);
                    }
                    break;
                }
            }
            // Headlights.
            Shapes.Rect(t, "LightL", Palette.Hex(0xFFF4C2), new Vector2(-w * 0.32f, l * 0.5f - 0.03f), new Vector2(0.1f, 0.05f), Layers.Vehicle + 3);
            Shapes.Rect(t, "LightR", Palette.Hex(0xFFF4C2), new Vector2(w * 0.32f, l * 0.5f - 0.03f), new Vector2(0.1f, 0.05f), Layers.Vehicle + 3);
        }

        public void SetPaint(Color color, bool isEmergency)
        {
            if (_paint != null && !isEmergency && TypeId != "bus") _paint.color = color;
        }

        private void Update()
        {
            if (_beacon != null) _beacon.color = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? Palette.Red : Palette.Blue;
        }
    }

    /// <summary>Keeps one pooled view per simulated vehicle and interpolates poses between fixed steps.</summary>
    public sealed class VehicleViewManager : MonoBehaviour
    {
        private readonly Dictionary<Vehicle, VehicleView> _active = new Dictionary<Vehicle, VehicleView>();
        private readonly Dictionary<string, Stack<VehicleView>> _pool = new Dictionary<string, Stack<VehicleView>>();
        private GameSimulation _sim;

        public void Bind(GameSimulation sim)
        {
            Unbind();
            _sim = sim;
            _sim.Traffic.VehicleSpawned += OnSpawned;
            _sim.Traffic.VehicleDespawned += OnDespawned;
            foreach (var v in _sim.Traffic.ActiveVehicles) OnSpawned(v);
        }

        private void Unbind()
        {
            if (_sim == null) return;
            _sim.Traffic.VehicleSpawned -= OnSpawned;
            _sim.Traffic.VehicleDespawned -= OnDespawned;
            foreach (var kv in _active) Release(kv.Value);
            _active.Clear();
            _sim = null;
        }

        private void OnDestroy() => Unbind();

        private void OnSpawned(Vehicle v)
        {
            if (!_pool.TryGetValue(v.Type.Id, out var stack))
            {
                stack = new Stack<VehicleView>();
                _pool[v.Type.Id] = stack;
            }
            var view = stack.Count > 0 ? stack.Pop() : VehicleView.Create(transform, v.Type);
            view.gameObject.SetActive(true);
            view.Vehicle = v;
            view.SetPaint(Palette.CarColors[(int)(v.TripId % Palette.CarColors.Length)], v.Type.IsEmergency);
            _active[v] = view;
            Apply(view, 1f);
        }

        private void OnDespawned(Vehicle v)
        {
            if (!_active.TryGetValue(v, out var view)) return;
            _active.Remove(v);
            Release(view);
        }

        private void Release(VehicleView view)
        {
            view.Vehicle = null;
            view.gameObject.SetActive(false);
            if (!_pool.TryGetValue(view.TypeId, out var stack))
            {
                stack = new Stack<VehicleView>();
                _pool[view.TypeId] = stack;
            }
            stack.Push(view);
        }

        private void LateUpdate()
        {
            if (_sim == null) return;
            float alpha = Mathf.Clamp01((float)_sim.InterpolationAlpha);
            foreach (var kv in _active) Apply(kv.Value, alpha);
        }

        private static void Apply(VehicleView view, float alpha)
        {
            var v = view.Vehicle;
            var p = Vec2.Lerp(v.PreviousPosition, v.Position, alpha);
            var h = Vec2.Lerp(v.PreviousHeading, v.Heading, alpha);
            float angle = Mathf.Atan2((float)h.Y, (float)h.X) * Mathf.Rad2Deg - 90f;
            view.transform.localPosition = new Vector3((float)p.X, (float)p.Y, 0);
            view.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
