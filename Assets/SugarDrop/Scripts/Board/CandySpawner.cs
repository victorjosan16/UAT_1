using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Creează grămada de bomboane (cercuri cu fizică 2D) deasupra tablei. Bomboanele stau pe capace
    /// și cad prin găurile care apar după alinieri.
    /// </summary>
    public class CandySpawner : MonoBehaviour
    {
        [SerializeField] private float radius = 0.24f;
        [SerializeField] private float gravityScale = 1.6f;
        [SerializeField] private PhysicsMaterial2D material;
        [Tooltip("Tabla: bomboanele care stau pe capace sunt împinse ușor spre cea mai apropiată gaură.")]
        [SerializeField] private Board board;
        [Tooltip("Forța care împinge bomboanele spre găuri (ca un nisip care curge).")]
        [SerializeField] private float flowForce = 3f;
        [SerializeField] private float flowMaxSpeed = 1.4f;

        private readonly List<GameObject> _candies = new();
        private readonly List<Rigidbody2D> _bodies = new();
        private readonly List<Candy> _candyInfo = new();
        private int _flowTick;

        /// <summary>Așază „count” bomboane în dreptunghiul dat, rând cu rând de jos în sus.</summary>
        public void Spawn(int count, Rect area)
        {
            Clear();
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

        /// <summary>
        /// Un mic „ghiont” pentru bomboanele care stau pe loc (după fiecare mutare): cele rămase pe marginea
        /// unui capac lângă o gaură cad în ea, în loc să rămână agățate.
        /// </summary>
        public void Nudge(float strength = 0.6f)
        {
            foreach (GameObject candy in _candies)
            {
                if (candy == null || !candy.TryGetComponent(out Rigidbody2D body)) continue;
                if (body.GetPointVelocity(body.position).sqrMagnitude > 0.05f) continue; // merge și în Unity 6, și în versiunile vechi
                if (candy.TryGetComponent(out Candy c) && c.Counted) continue;
                body.AddForce(new Vector2(Random.Range(-1f, 1f) * strength, Random.Range(0.2f, 0.5f) * strength), ForceMode2D.Impulse);
            }
        }

        /// <summary>
        /// „Curgerea”: bomboanele care stau pe capace plate alunecă încet spre cea mai apropiată gaură
        /// de pe aceeași podea, în loc să rămână blocate (ca zahărul care curge). Rulează la fiecare
        /// al doilea pas de fizică; bomboanele deja în mișcare sau din borcan nu sunt atinse.
        /// </summary>
        private void FixedUpdate()
        {
            if (board == null || _bodies.Count == 0) return;
            if (++_flowTick % 2 != 0) return;
            for (int i = 0; i < _bodies.Count; i++)
            {
                Rigidbody2D body = _bodies[i];
                if (body == null || _candyInfo[i].Counted) continue;
                Vector2 position = body.position;
                Vector2 velocity = body.GetPointVelocity(position);
                if (Mathf.Abs(velocity.y) > 1.5f) continue; // cade deja
                if (!board.TryFindDropX(position, out float targetX)) continue;
                float dx = targetX - position.x;
                if (Mathf.Abs(dx) < 0.12f) continue;
                float direction = Mathf.Sign(dx);
                if (velocity.x * direction > flowMaxSpeed) continue;
                body.AddForce(new Vector2(direction * flowForce * body.mass, 0f));
            }
        }

        public void Clear()
        {
            foreach (GameObject candy in _candies)
                if (candy != null) Destroy(candy);
            _candies.Clear();
            _bodies.Clear();
            _candyInfo.Clear();
        }

        private GameObject CreateCandy(Vector2 position)
        {
            // Trei forme de bomboane (spirală, ambalată, inimioară), în culori pastelate, rotite la întâmplare.
            Sprite sprite = SugarSprites.CandyVariant(Random.Range(0, 3));
            var go = new GameObject("Candy");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            float scale = radius * 2f / sprite.bounds.size.x;
            go.transform.localScale = Vector3.one * scale;

            Color color = Palette.RandomCandy();
            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.color = color;
            body.sortingOrder = 5;

            var rigidbody = go.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = gravityScale;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            // Bomboanele nu „adorm”: când dispare un capac de sub ele, încep imediat să cadă.
            rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = sprite.bounds.extents.x * 0.92f; // în spațiul local (obiectul e scalat)
            if (material != null) collider.sharedMaterial = material;

            var info = go.AddComponent<Candy>();
            info.Color = color;
            _bodies.Add(rigidbody);
            _candyInfo.Add(info);
            return go;
        }
    }
}
