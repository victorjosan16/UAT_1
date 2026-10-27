using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Pericolul de sub tablă: Bobo stă pe un suport de tort, iar ceva urcă spre el cu fiecare mutare folosită —
    /// gem de căpșuni, slime acru, apă, nisip, suc de struguri (cu un spiriduș al Vrăjitorului deasupra),
    /// ciocolată fierbinte, gheață, caramel, ceață de vată de zahăr (cu fulgere) sau o gaură neagră.
    /// La victorie pericolul se retrage, la înfrângere îl acoperă pe Bobo până la bărbie (comic, nu violent).
    /// </summary>
    public class DangerScene : MonoBehaviour
    {
        private struct Style
        {
            public Color Body;
            public Color Surface;
            public Sprite Puff;
            public Sprite Particle;
            public Color ParticleColor;
            public float Wave;
        }

        private const int PuffCount = 12;
        private const int ParticleCount = 8;

        private SpriteRenderer _body;
        private readonly List<SpriteRenderer> _puffs = new();
        private readonly List<SpriteRenderer> _particles = new();
        private readonly List<SpriteRenderer> _sweat = new();
        private SpriteRenderer _stand;
        private SpriteRenderer _plate;
        private SpriteRenderer _hunter;
        private SpriteRenderer _flash;
        private Style _style;
        private int _theme;
        private float _bottom, _width, _feet, _chin;
        private float _target, _shown;
        private float _caught;
        private bool _rescued;
        private float _rescueT;

        public bool Visible => gameObject.activeSelf;

        private void Awake()
        {
            _body = Renderer("Body", QuarryEmpire.ProceduralSprites.RoundedRect, 30);
            _body.drawMode = SpriteDrawMode.Sliced;
            for (int i = 0; i < PuffCount; i++) _puffs.Add(Renderer("Puff", SugarSprites.Circle, 31));
            for (int i = 0; i < ParticleCount; i++) _particles.Add(Renderer("Particle", SugarSprites.Ring, 32));
            for (int i = 0; i < 2; i++) _sweat.Add(Renderer("Sweat", SugarSprites.Drop, 26));
            _stand = Renderer("Stand", QuarryEmpire.ProceduralSprites.RoundedRect, 12);
            _stand.drawMode = SpriteDrawMode.Sliced;
            _plate = Renderer("Plate", QuarryEmpire.ProceduralSprites.RoundedRect, 13);
            _plate.drawMode = SpriteDrawMode.Sliced;
            _hunter = Renderer("Hunter", SugarSprites.BossFace(0), 33);
            _flash = Renderer("Flash", SugarSprites.SoftDot, 34);
            gameObject.SetActive(false);
        }

        private SpriteRenderer Renderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void Size(SpriteRenderer renderer, float size) =>
            renderer.transform.localScale = Vector3.one * (size / renderer.sprite.bounds.size.x);

        /// <param name="boboFeet">Unde stă Bobo (tălpile); pericolul urcă de la <paramref name="bottom"/> spre el.</param>
        public void Show(int theme, Vector3 boboFeet, float boboHeight, float width, float bottom)
        {
            _theme = theme;
            _style = StyleFor(theme);
            _width = width;
            _bottom = bottom;
            _feet = boboFeet.y;
            _chin = boboFeet.y + boboHeight * 0.55f;
            _target = _shown = 0f;
            _caught = -1f;
            _rescued = false;
            gameObject.SetActive(true);

            // Suportul de tort pe care stă Bobo (deasupra pericolului, la început).
            ChapterTheme colors = ChapterTheme.For(theme);
            _stand.size = new Vector2(0.45f, _feet - bottom);
            _stand.color = Color.Lerp(colors.Stripe, Color.white, 0.55f);
            _stand.transform.position = new Vector3(boboFeet.x, (bottom + _feet) * 0.5f, 0f);
            _plate.size = new Vector2(2.3f, 0.3f);
            _plate.color = Color.white;
            _plate.transform.position = new Vector3(boboFeet.x, _feet - 0.12f, 0f);

            _body.color = _style.Body;
            for (int i = 0; i < _puffs.Count; i++)
            {
                _puffs[i].sprite = _style.Puff;
                _puffs[i].color = i % 2 == 0 ? _style.Surface : _style.Body;
                Size(_puffs[i], 1.1f + (i % 3) * 0.25f);
            }
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i].sprite = _style.Particle;
                _particles[i].color = _style.ParticleColor;
                Size(_particles[i], 0.18f + (i % 3) * 0.08f);
            }
            foreach (SpriteRenderer drop in _sweat)
            {
                drop.color = new Color(0.55f, 0.85f, 1f, 0f);
                Size(drop, 0.22f);
            }
            // Castelul de Struguri: un spiriduș al Vrăjitorului coboară deasupra lui Bobo.
            _hunter.gameObject.SetActive(theme == 4);
            Size(_hunter, 1.3f);
            _flash.gameObject.SetActive(theme == 8);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            Size(_flash, width * 1.4f);
            _flash.transform.position = new Vector3(0f, _feet + 1f, 0f);
            Apply(0f, Time.time);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Cât a înaintat pericolul (0 = departe, 1 = la mijlocul lui Bobo); se mișcă lin spre valoare.</summary>
        public void SetProgress(float progress)
        {
            if (_caught < 0f && !_rescued) _target = Mathf.Clamp01(progress);
        }

        /// <summary>Victorie: pericolul se retrage.</summary>
        public void PlayRescue()
        {
            _rescued = true;
            _rescueT = 0f;
        }

        /// <summary>Înfrângere: pericolul îl ajunge pe Bobo.</summary>
        public void PlayCaught() => _caught = 0f;

        /// <summary>Continuă cu mutări în plus (reclamă): pericolul coboară puțin înapoi.</summary>
        public void Recover(float progress)
        {
            _caught = -1f;
            _target = Mathf.Clamp01(progress);
        }

        private void Update()
        {
            float now = Time.time, dt = Time.deltaTime;
            if (_rescued)
            {
                _rescueT += dt;
                _target = 0f;
                _shown = Mathf.MoveTowards(_shown, -0.3f, dt * 0.8f);
            }
            else if (_caught >= 0f)
            {
                _caught += dt;
                _shown = Mathf.MoveTowards(_shown, 1.25f, dt * 0.9f);
            }
            else _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-dt * 3f));
            Apply(_shown, now);
        }

        /// <summary>Așază nivelul pericolului: corpul, suprafața ondulată, particulele, spiridușul și sudoarea lui Bobo.</summary>
        private void Apply(float progress, float now)
        {
            // progress 1 = jumătatea lui Bobo; 1,25 = la bărbie (prins).
            float top = Mathf.Lerp(_bottom - 0.4f, Mathf.Lerp(_feet, _chin, 0.55f), Mathf.Clamp(progress, -0.3f, 1f));
            if (progress > 1f) top = Mathf.Lerp(Mathf.Lerp(_feet, _chin, 0.55f), _chin, (progress - 1f) / 0.25f);
            float height = Mathf.Max(0.01f, top - _bottom + 0.6f);
            _body.size = new Vector2(_width + 1f, height);
            _body.transform.position = new Vector3(0f, _bottom - 0.6f + height * 0.5f, 0f);

            for (int i = 0; i < _puffs.Count; i++)
            {
                float x = -_width * 0.5f - 0.3f + i * (_width + 0.6f) / (_puffs.Count - 1);
                float y = top + Mathf.Sin(now * 2.2f + i * 0.9f) * _style.Wave;
                _puffs[i].transform.position = new Vector3(x, y, 0f);
                if (_theme == 6) _puffs[i].transform.rotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 0f : 180f);
            }
            for (int i = 0; i < _particles.Count; i++)
            {
                float k = Mathf.Repeat(now * 0.25f + i / (float)_particles.Count, 1f);
                float x = -_width * 0.42f + (i * 0.37f % 1f) * _width * 0.84f + Mathf.Sin(now * 2f + i) * 0.15f;
                float y = Mathf.Lerp(_bottom, top - 0.2f, k);
                _particles[i].transform.position = new Vector3(x, y, 0f);
                _particles[i].gameObject.SetActive(top > _bottom + 0.4f);
            }

            // Bobo transpiră când pericolul se apropie.
            float fear = Mathf.Clamp01((progress - 0.55f) / 0.35f);
            for (int i = 0; i < _sweat.Count; i++)
            {
                float k = Mathf.Repeat(now * 1.4f + i * 0.5f, 1f);
                Vector3 head = new(i == 0 ? -0.75f : 0.8f, _chin + 0.5f - k * 0.5f, 0f);
                _sweat[i].transform.position = head;
                _sweat[i].color = new Color(0.55f, 0.85f, 1f, fear * (1f - k));
            }

            if (_hunter.gameObject.activeSelf)
            {
                float hover = Mathf.Lerp(_chin + 3f, _chin + 1.1f, Mathf.Clamp01(progress));
                _hunter.transform.position = new Vector3(-2.6f + Mathf.Sin(now * 1.3f) * 0.5f, hover + Mathf.Sin(now * 2.6f) * 0.12f, 0f);
                _hunter.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 3f) * 8f);
                _hunter.color = new Color(1f, 1f, 1f, _rescued ? Mathf.Clamp01(1f - _rescueT * 2f) : 1f);
            }
            if (_flash.gameObject.activeSelf)
            {
                // Furtună în Norii de Vată de Zahăr: fulgere mai dese când pericolul e aproape.
                float period = Mathf.Lerp(4f, 1.6f, Mathf.Clamp01(progress));
                float c = Mathf.Repeat(now, period);
                _flash.color = new Color(1f, 1f, 0.85f, _rescued ? 0f : (c < 0.07f || (c > 0.13f && c < 0.18f) ? 0.45f : 0f));
            }
        }

        private static Style StyleFor(int theme) => theme switch
        {
            0 => Liquid("#E8264F", "#FF5C7A", SugarSprites.Circle, SugarSprites.Heart, new Color(1f, 0.75f, 0.8f, 0.8f), 0.12f),
            1 => Liquid("#47B81E", "#7FE03A", SugarSprites.Slime, SugarSprites.Ring, new Color(0.8f, 1f, 0.6f, 0.8f), 0.14f),
            2 => Liquid("#1F6FE0", "#4FA3FF", SugarSprites.Circle, SugarSprites.Ring, new Color(0.85f, 0.95f, 1f, 0.85f), 0.16f),
            3 => Liquid("#E8A82A", "#FFD25C", SugarSprites.Circle, SugarSprites.Crumb, new Color(0.85f, 0.6f, 0.2f, 0.9f), 0.05f),
            4 => Liquid("#6A26C9", "#9B55F5", SugarSprites.Circle, SugarSprites.Ring, new Color(0.9f, 0.75f, 1f, 0.8f), 0.12f),
            5 => Liquid("#5A2C14", "#7C4424", SugarSprites.Circle, SugarSprites.Ring, new Color(0.85f, 0.55f, 0.35f, 0.85f), 0.1f),
            6 => Liquid("#7CD8F5", "#D2F6FF", SugarSprites.Shard, SugarSprites.Sparkle, Color.white, 0.03f),
            7 => Liquid("#D9661A", "#FF9A3C", SugarSprites.Circle, SugarSprites.Ring, new Color(1f, 0.85f, 0.6f, 0.85f), 0.1f),
            8 => Liquid("#B57BE0", "#F2C6FF", SugarSprites.Frosting, SugarSprites.SoftDot, new Color(1f, 1f, 1f, 0.7f), 0.18f),
            _ => Liquid("#1A0F45", "#3A2490", SugarSprites.Circle, SugarSprites.Sparkle, new Color(1f, 0.95f, 0.6f, 1f), 0.12f),
        };

        private static Style Liquid(string body, string surface, Sprite puff, Sprite particle, Color particleColor, float wave)
        {
            ColorUtility.TryParseHtmlString(body, out Color b);
            ColorUtility.TryParseHtmlString(surface, out Color s);
            return new Style { Body = b, Surface = s, Puff = puff, Particle = particle, ParticleColor = particleColor, Wave = wave };
        }

        /// <summary>Numele pericolului (cheie SugarLoc), pentru fereastra de start.</summary>
        public static string DangerKey(int theme) => theme switch
        {
            0 => "Gemul de căpșuni urcă!",
            1 => "Slime-ul acru urcă!",
            2 => "Apa lacului urcă!",
            3 => "Nisipul îl îngroapă pe Bobo!",
            4 => "Spiridușul Vrăjitorului vine!",
            5 => "Ciocolata fierbinte urcă!",
            6 => "Gheața îl îngheață pe Bobo!",
            7 => "Caramelul urcă!",
            8 => "Vine furtuna!",
            _ => "Gaura neagră îl trage!",
        };
    }
}
