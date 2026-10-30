using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Traffic;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>Floating "+$5" labels above a booth whenever a toll is actually collected.</summary>
    public sealed class CoinPopups : MonoBehaviour
    {
        private sealed class Popup
        {
            public TextMesh Text;
            public SpriteRenderer Coin;
            public float Age;
            public Vector3 Origin;
        }

        private const float Lifetime = 1.1f;
        private readonly List<Popup> _live = new List<Popup>();
        private readonly Stack<Popup> _free = new Stack<Popup>();
        private GameSimulation _sim;
        private WorldView _world;

        public void Bind(GameSimulation sim, WorldView world)
        {
            if (_sim != null) _sim.Payments.TollCollected -= OnToll;
            _sim = sim;
            _world = world;
            _sim.Payments.TollCollected += OnToll;
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.Payments.TollCollected -= OnToll;
        }

        private void OnToll(TollBooth booth, Vehicle vehicle, long cents)
        {
            var p = _free.Count > 0 ? _free.Pop() : CreatePopup();
            p.Age = 0;
            float x = (float)_sim.Lanes.BoothX(booth.SlotIndex);
            p.Origin = new Vector3(x, (float)_sim.Config.Layout.StopLineY + 0.9f, 0);
            p.Text.text = cents > 0 ? "+" + Money.Format(cents) : "0";
            p.Text.gameObject.SetActive(true);
            _live.Add(p);
        }

        private Popup CreatePopup()
        {
            var text = Shapes.Label(transform, "Popup", "", Palette.Gold, Vector2.zero, 0.42f, Layers.Popup);
            var coin = Shapes.Circle(text.transform, "Coin", Palette.Gold, new Vector2(-0.55f, 0), 0.32f, Layers.Popup);
            return new Popup { Text = text, Coin = coin };
        }

        private void Update()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Age += Time.deltaTime;
                float t = p.Age / Lifetime;
                p.Text.transform.localPosition = p.Origin + new Vector3(0, t * 1.4f, 0);
                float a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
                p.Text.color = Palette.WithAlpha(Palette.Gold, a);
                p.Coin.color = Palette.WithAlpha(Palette.Gold, a);
                if (p.Age >= Lifetime)
                {
                    p.Text.gameObject.SetActive(false);
                    _live.RemoveAt(i);
                    _free.Push(p);
                }
            }
        }
    }
}
