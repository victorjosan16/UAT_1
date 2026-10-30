using TollRoadTycoon.Core;
using TollRoadTycoon.Localization;
using TollRoadTycoon.TollSystem;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>Booth house, island, barrier arm, worker and status light for one toll booth.</summary>
    public sealed class BoothView : MonoBehaviour
    {
        private TollBooth _booth;
        private Transform _barrierPivot;
        private SpriteRenderer _light;
        private SpriteRenderer _worker;
        private SpriteRenderer _selection;
        private SpriteRenderer _robot;
        private TextMesh _queueLabel;
        private float _bump;
        private long _lastServed;

        public TollBooth Booth => _booth;
        public Rect HitRect { get; private set; }
        public Vector2 PayPoint { get; private set; }

        public void Build(TollBooth booth, float x, RoadLayoutConfig layout)
        {
            _booth = booth;
            _lastServed = booth.VehiclesServed;
            var t = transform;
            float stop = (float)layout.StopLineY;
            float islandX = x + 0.8f;
            PayPoint = new Vector2(x, stop);
            HitRect = new Rect(x - 0.8f, stop - 4f, 1.6f, 6f);

            _selection = Shapes.Rounded(t, "Selection", Palette.WithAlpha(Palette.Gold, 0.0f), new Vector2(x, stop - 1.2f), new Vector2(1.55f, 5.5f), Layers.Island - 1);

            // Concrete island on the right of the booth lane.
            Shapes.Rounded(t, "Island", Palette.Cream, new Vector2(islandX, stop - 0.6f), new Vector2(0.32f, 3.4f), Layers.Island);
            Shapes.Rect(t, "IslandNose", Palette.Gold, new Vector2(islandX, stop - 2.2f), new Vector2(0.3f, 0.2f), Layers.Island + 1);

            // Booth house.
            Shapes.Rounded(t, "BoothShadow", Palette.Shadow, new Vector2(islandX + 0.08f, stop - 0.15f), new Vector2(0.5f, 0.8f), Layers.Booth - 1);
            Shapes.Rounded(t, "BoothBody", Palette.Cream, new Vector2(islandX, stop - 0.05f), new Vector2(0.48f, 0.78f), Layers.Booth);
            Shapes.Rounded(t, "BoothRoof", Palette.Blue, new Vector2(islandX, stop + 0.08f), new Vector2(0.54f, 0.42f), Layers.Booth + 1);
            Shapes.Rect(t, "BoothWindow", Palette.Glass, new Vector2(islandX - 0.2f, stop - 0.2f), new Vector2(0.08f, 0.3f), Layers.Booth + 1);
            _worker = Shapes.Circle(t, "Worker", Palette.Gold, new Vector2(islandX + 0.02f, stop - 0.22f), 0.2f, Layers.Booth + 2);
            _robot = Shapes.Rounded(t, "AutoReader", Palette.Green, new Vector2(islandX - 0.1f, stop - 0.55f), new Vector2(0.14f, 0.2f), Layers.Booth + 2);
            _light = Shapes.Circle(t, "Light", Palette.Green, new Vector2(islandX, stop + 0.32f), 0.16f, Layers.Booth + 3);

            // Barrier arm pivoting on the island; spans the booth lane.
            _barrierPivot = new GameObject("BarrierPivot").transform;
            _barrierPivot.SetParent(t, false);
            _barrierPivot.localPosition = new Vector3(islandX - 0.12f, (float)layout.BarrierY, 0);
            Shapes.Rect(_barrierPivot, "Arm", Palette.White, new Vector2(-0.7f, 0), new Vector2(1.4f, 0.1f), Layers.Barrier);
            for (int i = 0; i < 3; i++)
            {
                Shapes.Rect(_barrierPivot, "Stripe", Palette.Red, new Vector2(-0.3f - i * 0.4f, 0), new Vector2(0.18f, 0.1f), Layers.Barrier + 1);
            }
            Shapes.Circle(t, "BarrierPost", Palette.DarkBlue, new Vector2(islandX - 0.12f, (float)layout.BarrierY), 0.2f, Layers.Barrier + 2);

            _queueLabel = Shapes.Label(t, "Queue", "", Palette.White, new Vector2(x, stop - 13.4f), 0.32f, Layers.Popup - 1);
        }

        public void SetSelected(bool selected)
        {
            _selection.color = Palette.WithAlpha(Palette.Gold, selected ? 0.35f : 0f);
        }

        private void Update()
        {
            if (_booth == null) return;
            _barrierPivot.localRotation = Quaternion.Euler(0, 0, -80f * (float)_booth.BarrierOpen);

            bool auto = _booth.Mode == BoothMode.Automatic;
            _robot.enabled = auto;
            _worker.enabled = !auto && _booth.AssignedWorker != null;
            if (!_booth.IsStaffed) _light.color = Palette.Red;
            else if (_booth.PendingClose) _light.color = Palette.Gold;
            else if (_booth.State == BoothState.ProcessingPayment) _light.color = Color.Lerp(Palette.Gold, Palette.White, Mathf.PingPong(Time.time * 4f, 1f));
            else _light.color = Palette.Green;

            if (_booth.VehiclesServed != _lastServed)
            {
                _lastServed = _booth.VehiclesServed;
                _bump = 1f;
            }
            _bump = Mathf.MoveTowards(_bump, 0, Time.deltaTime * 4f);
            _worker.transform.localScale = Vector3.one * (0.2f * (1 + 0.25f * _bump));

            _queueLabel.text = _booth.IsStaffed
                ? $"{_booth.AssignedVehicles.Count}/{_booth.QueueCapacity}"
                : Loc.T("booth.unstaffed");
        }
    }
}
