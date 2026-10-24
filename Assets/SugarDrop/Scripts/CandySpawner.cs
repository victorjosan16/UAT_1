using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Creează grămada de bomboane (cercuri cu fizică 2D) deasupra tablei. Bomboanele stau pe capace
    /// și cad prin găurile care apar după alinieri.
    /// </summary>
    public class CandySpawner : MonoBehaviour
    {
        [SerializeField] private float radius = 0.16f;
        [SerializeField] private float gravityScale = 1.6f;
        [SerializeField] private PhysicsMaterial2D material;

        private readonly List<GameObject> _candies = new();
        private Sprite _sprite;

        /// <summary>Așază „count” bomboane în dreptunghiul dat, rând cu rând de jos în sus.</summary>
        public void Spawn(int count, Rect area)
        {
            Clear();
            _sprite ??= ProceduralSprites.Circle;
            float diameter = radius * 2f * 1.06f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((area.width - radius * 2f) / diameter) + 1);

            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int column = i % perRow;
                float offset = row % 2 == 0 ? 0f : diameter * 0.5f; // rânduri decalate, ca o grămadă naturală
                var position = new Vector2(
                    area.xMin + radius + column * diameter + offset + Random.Range(-0.02f, 0.02f),
                    area.yMin + radius + row * diameter * 0.9f);
                position.x = Mathf.Min(position.x, area.xMax - radius);
                _candies.Add(CreateCandy(position));
            }
        }

        public void Clear()
        {
            foreach (GameObject candy in _candies)
                if (candy != null) Destroy(candy);
            _candies.Clear();
        }

        private GameObject CreateCandy(Vector2 position)
        {
            var go = new GameObject("Candy");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            float scale = radius * 2f / _sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * scale;

            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = _sprite;
            body.color = Palette.RandomCandy();
            body.sortingOrder = 5;

            // Reflexie mică, ca bomboana să pară lucioasă.
            var shine = new GameObject("Shine");
            shine.transform.SetParent(go.transform, false);
            shine.transform.localPosition = new Vector3(-0.22f, 0.22f, 0f) * _sprite.bounds.size.x;
            shine.transform.localScale = Vector3.one * 0.3f;
            var shineRenderer = shine.AddComponent<SpriteRenderer>();
            shineRenderer.sprite = _sprite;
            shineRenderer.color = new Color(1f, 1f, 1f, 0.7f);
            shineRenderer.sortingOrder = 6;

            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = gravityScale;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            // Bomboanele nu „adorm”: când dispare un capac de sub ele, încep imediat să cadă.
            rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = _sprite.bounds.extents.x; // în spațiul local (obiectul e scalat)
            if (material != null) collider.sharedMaterial = material;

            go.AddComponent<Candy>();
            return go;
        }
    }
}
