using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Bomboanele care ajung în borcan: când o bomboană-ingredient iese de pe tablă, aici devine o bomboană
    /// cu fizică 2D care cade pe pâlnie și se așază în borcan.
    /// </summary>
    public class CandySpawner : MonoBehaviour
    {
        [SerializeField] private float radius = 0.3f;
        [SerializeField] private float gravityScale = 1.6f;
        [SerializeField] private PhysicsMaterial2D material;

        private readonly List<GameObject> _candies = new();

        [Tooltip("Câte bomboane cu fizică pot exista deodată (cele mai vechi dispar), ca fizica să rămână ieftină.")]
        [SerializeField] private int maxActive = 50;

        /// <summary>O bomboană nouă care cade din poziția dată (sub ieșirea de pe tablă).</summary>
        public void SpawnFalling(Vector3 position, int colorIndex) =>
            SpawnFalling(position, Palette.Candy(colorIndex), SugarSprites.CandyWrapped, radius);

        /// <summary>
        /// Bomboană decorativă (din culorile strânse): cade prin tablă spre borcan și se adună acolo.
        /// Obiectivele nu depind de ea — e doar pentru senzația de „colecție”.
        /// </summary>
        public void SpawnDecorative(Vector3 position, Color color) =>
            SpawnFalling(position, color, SugarSprites.CandyVariant(Random.Range(0, 3)), radius * 0.7f);

        private void SpawnFalling(Vector3 position, Color color, Sprite sprite, float size)
        {
            _candies.RemoveAll(c => c == null);
            while (_candies.Count >= maxActive)
            {
                Destroy(_candies[0]);
                _candies.RemoveAt(0);
            }
            var go = new GameObject("Candy");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f)));
            go.transform.localScale = Vector3.one * (size * 2f / sprite.bounds.size.x);
            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = color;
            body.sortingOrder = 5;

            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = gravityScale;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.AddForce(new Vector2(Random.Range(-0.4f, 0.4f), -0.5f), ForceMode2D.Impulse);
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = sprite.bounds.extents.x * 0.8f; // în spațiul local (obiectul e scalat)
            if (material != null) collider.sharedMaterial = material;

            go.AddComponent<Candy>().Color = color;
            _candies.Add(go);
        }

        public void Clear()
        {
            foreach (GameObject candy in _candies)
                if (candy != null) Destroy(candy);
            _candies.Clear();
        }
    }
}
