using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SugarDrop
{
    /// <summary>
    /// Tabla nivelurilor „Salvează-l pe Bobo”. Sus, o țeavă din rezervorul cu Sirop Fermecat picură deasupra unei
    /// coloane; jos, o săgeată arată unde trebuie să ajungă siropul (deasupra pâlniei). Atingi bomboane ca să sapi
    /// drumul; siropul curge animat prin goluri, pătrățel cu pătrățel. Când ajunge jos, se varsă pe pâlnie în borcan,
    /// cușca lui Bobo se sparge, iar Bobo (până atunci o siluetă) își recapătă culorile.
    /// </summary>
    public class RescueBoard : MonoBehaviour
    {
        private static readonly Color SyrupColor = new(0.96f, 0.36f, 0.56f, 0.92f);
        private static readonly Color SyrupLight = new(1f, 0.62f, 0.76f, 0.9f);
        private const float FlowStep = 0.055f;

        public event Action MoveMade;
        public event Action InvalidTap;
        public event Action Touched;
        public event Action<int, Vector3> TileCleared;
        public event Action<int, Vector3, int> GroupBlasted;
        public event Action<int> Points;
        /// <summary>Siropul a ajuns la Bobo și animația de salvare s-a terminat.</summary>
        public event Action Rescued;

        public bool Interactable { get; set; }
        public bool Busy => _busyUntil > Time.time || _rescuing;
        public bool IsBuilt => _engine != null;
        public bool IsRescued => _engine != null && _engine.Rescued;

        private RescueEngine _engine;
        private float _cell;
        private Vector2 _bottomCenter;
        private SpriteRenderer[,] _pieces;
        private SpriteRenderer[,] _overlays;
        private SpriteRenderer[,] _syrup;
        private float[,] _syrupAt;
        private float[,] _popAt;
        private float[,] _shakeAt;
        private float[,] _growAt;
        private float _hintUntil;
        private System.Collections.Generic.List<Vector2Int> _hint = new();
        private float _busyUntil;
        private bool _rescuing;
        private Transform _pipe;
        private SpriteRenderer _drop;
        private Transform _exitArrow;
        private SpriteRenderer _cage;
        private Mascot _mascot;
        private Vector3 _jarMouth, _jarCenter;
        private float _jarHalfWidth;
        private Camera _camera;

        // ------------------------------------------------------------ Construcție

        public void Build(RescueLayout layout, Vector2 bottomCenter, float cellSize, int chapter, Mascot mascot, Vector3 jarMouth, Vector3 jarCenter, float jarHalfWidth)
        {
            Clear();
            _camera = Camera.main;
            _engine = new RescueEngine(layout);
            _cell = cellSize;
            _bottomCenter = bottomCenter;
            _mascot = mascot;
            _jarMouth = jarMouth;
            _jarCenter = jarCenter;
            _jarHalfWidth = jarHalfWidth;
            int w = _engine.Width, h = _engine.Height;
            _pieces = new SpriteRenderer[w, h];
            _overlays = new SpriteRenderer[w, h];
            _syrup = new SpriteRenderer[w, h];
            _syrupAt = new float[w, h];
            _popAt = new float[w, h];
            _shakeAt = new float[w, h];
            _growAt = new float[w, h];
            ChapterTheme theme = ChapterTheme.For(chapter);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var c = new Vector2Int(x, y);
                    SpriteRenderer back = Sprite("Cell", SugarSprites.CellMask, Center(c), _cell * 0.97f, 2);
                    back.color = (x + y) % 2 == 0 ? theme.CellA : theme.CellB;
                    _syrup[x, y] = Sprite("Syrup", SugarSprites.Circle, Center(c), _cell * 1.18f, 3);
                    _syrup[x, y].color = SyrupColor;
                    _syrup[x, y].gameObject.SetActive(false);
                    _pieces[x, y] = Sprite("Piece", SugarSprites.Circle, Center(c), _cell * 0.9f, 6);
                    _overlays[x, y] = Sprite("Overlay", SugarSprites.Cage, Center(c), _cell * 0.96f, 7);
                    _popAt[x, y] = _shakeAt[x, y] = _growAt[x, y] = -1f;
                    _syrupAt[x, y] = -1f;
                    Refresh(c);
                }

            // Țeava rezervorului deasupra coloanei de intrare și picătura care cade din ea.
            Vector3 inlet = Center(_engine.Inlet) + Vector3.up * _cell * 0.95f;
            SpriteRenderer pipe = Panel("Pipe", inlet, new Vector2(_cell * 0.7f, _cell * 0.75f), Palette.WallStripe, 9);
            _pipe = pipe.transform;
            SpriteRenderer rim = Panel("Rim", inlet + Vector3.down * _cell * 0.36f, new Vector2(_cell * 0.92f, _cell * 0.22f), Color.white, 10);
            rim.transform.SetParent(_pipe, true);
            SpriteRenderer pool = Sprite("Syrup", SugarSprites.Circle, inlet + Vector3.up * _cell * 0.05f, _cell * 0.42f, 10);
            pool.color = SyrupColor;
            pool.transform.SetParent(_pipe, true);
            _drop = Sprite("Drop", SugarSprites.Drop, inlet, _cell * 0.3f, 11);
            _drop.color = SyrupColor;

            // Săgeata de jos (unde trebuie să ajungă siropul) și cușca peste Bobo, care e doar o siluetă.
            _exitArrow = Sprite("Exit", SugarSprites.Chevron, Center(_engine.Outlet) + Vector3.down * _cell * 0.66f, _cell * 0.5f, 9).transform;
            _exitArrow.GetComponent<SpriteRenderer>().color = SyrupLight;
            if (_mascot != null)
            {
                _mascot.SetSilhouette(true);
                _cage = Sprite("BoboCage", SugarSprites.Cage, _mascot.transform.position + Vector3.up * 0.15f, 2.3f, 25);
                _cage.color = new Color(0.55f, 0.4f, 0.3f);
            }
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (Transform child in transform) Destroy(child.gameObject);
            _engine = null;
            _rescuing = false;
            _busyUntil = 0f;
            _hint.Clear();
            if (_mascot != null) _mascot.SetSilhouette(false);
            _mascot = null;
        }

        private SpriteRenderer Sprite(string name, Sprite sprite, Vector3 position, float worldSize, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private SpriteRenderer Panel(string name, Vector3 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = QuarryEmpire.ProceduralSprites.RoundedRect;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Vector3 Center(Vector2Int c) => new(
            _bottomCenter.x + (c.x - (_engine.Width - 1) * 0.5f) * _cell,
            _bottomCenter.y + (c.y + 0.5f) * _cell, 0f);

        private bool TryGetCell(Vector2 world, out Vector2Int cell)
        {
            int x = Mathf.FloorToInt((world.x - _bottomCenter.x) / _cell + _engine.Width * 0.5f);
            int y = Mathf.FloorToInt((world.y - _bottomCenter.y) / _cell);
            cell = new Vector2Int(x, y);
            return x >= 0 && y >= 0 && x < _engine.Width && y < _engine.Height;
        }

        /// <summary>Ce arată celula: bomboana, piatra, cușca (cu bomboana în ea) sau slime-ul.</summary>
        private void Refresh(Vector2Int c)
        {
            SpriteRenderer piece = _pieces[c.x, c.y], overlay = _overlays[c.x, c.y];
            RescueCell kind = _engine.Kind(c.x, c.y);
            piece.gameObject.SetActive(kind != RescueCell.Empty);
            overlay.gameObject.SetActive(kind == RescueCell.Cage);
            switch (kind)
            {
                case RescueCell.Piece:
                case RescueCell.Cage:
                    SetSprite(piece, PieceSprites.Piece(_engine.Color(c.x, c.y)), 0.9f);
                    piece.color = kind == RescueCell.Cage ? new Color(0.8f, 0.8f, 0.85f) : Color.white;
                    overlay.color = _engine.Hits(c.x, c.y) >= 2 ? new Color(0.85f, 0.6f, 0.25f) : new Color(0.95f, 0.8f, 0.5f, 0.85f);
                    break;
                case RescueCell.Stone:
                    SetSprite(piece, SugarSprites.Stone, 0.96f);
                    piece.color = Color.white;
                    break;
                case RescueCell.Slime:
                    SetSprite(piece, SugarSprites.Slime, 0.95f);
                    piece.color = Color.white;
                    break;
            }
        }

        private void SetSprite(SpriteRenderer renderer, Sprite sprite, float size)
        {
            renderer.sprite = sprite;
            renderer.transform.localScale = Vector3.one * (_cell * size / sprite.bounds.size.x);
        }

        // ------------------------------------------------------------ Joc

        private void Update()
        {
            if (_engine == null) return;
            HandleInput();
            Animate();
        }

        private void HandleInput()
        {
            Pointer pointer = Pointer.current;
            if (!Interactable || pointer == null || _camera == null || !pointer.press.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 world = _camera.ScreenToWorldPoint(pointer.position.ReadValue());
            if (!TryGetCell(world, out Vector2Int cell)) return;
            Touched?.Invoke();
            Tap(cell);
        }

        private void Tap(Vector2Int cell)
        {
            RescueStep step = _engine.Tap(cell);
            if (step == null)
            {
                _shakeAt[cell.x, cell.y] = Time.time;
                InvalidTap?.Invoke();
                return;
            }
            _hint.Clear();
            float now = Time.time;
            MoveMade?.Invoke();

            Vector3 sum = Vector3.zero;
            foreach (Vector2Int c in step.Removed)
            {
                Vector3 position = transform.TransformPoint(Center(c));
                sum += position;
                _popAt[c.x, c.y] = now;
                TileCleared?.Invoke(step.Color, position);
            }
            if (step.Removed.Count > 0)
                GroupBlasted?.Invoke(step.Removed.Count, sum / step.Removed.Count, step.Color);
            Points?.Invoke(step.Removed.Count * ScoreRules.TilePoints + ScoreRules.GroupBonus(step.Removed.Count)
                + (step.CagesOpened.Count + step.SlimeCleared.Count) * ScoreRules.Obstacle + step.Syrup.Count * 10);

            foreach (Vector2Int c in step.CageHits)
            {
                _shakeAt[c.x, c.y] = now;
                Refresh(c);
            }
            foreach (Vector2Int c in step.CagesOpened)
            {
                if (SugarVfx.Instance != null) SugarVfx.Instance.IceShatter(transform.TransformPoint(Center(c)));
                Refresh(c);
            }
            foreach (Vector2Int c in step.SlimeCleared)
            {
                if (SugarVfx.Instance != null) SugarVfx.Instance.WaterSplash(transform.TransformPoint(Center(c)));
                _popAt[c.x, c.y] = now;
            }
            foreach (Vector2Int c in step.SlimeGrown)
            {
                _growAt[c.x, c.y] = now + 0.25f;
                Refresh(c);
            }

            // Siropul curge pătrățel cu pătrățel, după distanța de la frontul vechi.
            int longest = 0;
            foreach ((Vector2Int c, int distance) in step.Syrup)
            {
                _syrupAt[c.x, c.y] = now + 0.18f + distance * FlowStep;
                longest = Mathf.Max(longest, distance);
            }
            if (step.Syrup.Count > 0 && SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Candy, 0.5f, 0.8f);
            _busyUntil = Mathf.Max(_busyUntil, now + 0.35f + (step.Syrup.Count > 0 ? 0.18f + longest * FlowStep + 0.3f : 0f));
            if (step.Rescued) StartCoroutine(RescueRoutine(0.18f + longest * FlowStep + 0.3f));
        }

        /// <summary>Indiciu: grupul cel mai bun pulsează puțin.</summary>
        public void ShowHint()
        {
            if (_engine == null || _engine.Rescued) return;
            Vector2Int best = _engine.BestTap();
            if (best.x < 0) return;
            _hint = _engine.Kind(best.x, best.y) == RescueCell.Piece ? _engine.GroupAt(best) : new System.Collections.Generic.List<Vector2Int> { best };
            _hintUntil = Time.time + 2.5f;
        }

        // ------------------------------------------------------------ Animații

        private void Animate()
        {
            float now = Time.time;
            int w = _engine.Width, h = _engine.Height;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    SpriteRenderer piece = _pieces[x, y];
                    Transform pt = piece.transform;
                    Vector3 home = Center(new Vector2Int(x, y));

                    // Bomboana spartă: crește puțin și dispare.
                    if (_popAt[x, y] >= 0f)
                    {
                        float t = (now - _popAt[x, y]) / 0.22f;
                        if (t >= 1f)
                        {
                            _popAt[x, y] = -1f;
                            Refresh(new Vector2Int(x, y));
                            pt.localPosition = home;
                        }
                        else
                        {
                            piece.gameObject.SetActive(true);
                            float k = t < 0.3f ? 1f + t : (1f - t) * 1.85f;
                            pt.localScale = Vector3.one * (_cell * 0.9f / piece.sprite.bounds.size.x) * k;
                        }
                        continue;
                    }

                    float shake = _shakeAt[x, y] >= 0f ? Mathf.Clamp01(1f - (now - _shakeAt[x, y]) / 0.3f) : 0f;
                    float scale = 1f;
                    if (_growAt[x, y] >= 0f)
                    {
                        float g = Mathf.Clamp01((now - _growAt[x, y]) / 0.35f);
                        scale = UiEase.OutBack(g, 2f);
                        if (g >= 1f) _growAt[x, y] = -1f;
                    }
                    RescueCell kind = _engine.Kind(x, y);
                    if (kind == RescueCell.Slime) scale *= 1f + Mathf.Sin(now * 3f + x + y) * 0.04f;
                    bool hinted = _hint.Count > 0 && now < _hintUntil && _hint.Contains(new Vector2Int(x, y));
                    if (hinted) scale *= 1f + Mathf.Abs(Mathf.Sin(now * 6f)) * 0.12f;
                    pt.localPosition = home + new Vector3(Mathf.Sin(now * 60f) * 0.06f * shake, 0f, 0f);
                    if (piece.sprite != null && kind != RescueCell.Empty)
                    {
                        float size = kind == RescueCell.Stone ? 0.96f : kind == RescueCell.Slime ? 0.95f : 0.9f;
                        pt.localScale = Vector3.one * (_cell * size / piece.sprite.bounds.size.x) * scale;
                    }

                    // Siropul: apare cu un mic „boing” și apoi se unduiește.
                    SpriteRenderer syrup = _syrup[x, y];
                    if (_syrupAt[x, y] >= 0f && now >= _syrupAt[x, y])
                    {
                        if (!syrup.gameObject.activeSelf) syrup.gameObject.SetActive(true);
                        float s = Mathf.Clamp01((now - _syrupAt[x, y]) / 0.25f);
                        float wobble = 1f + Mathf.Sin(now * 2.5f + x * 0.7f + y * 1.1f) * 0.035f;
                        syrup.transform.localScale = Vector3.one * (_cell * 1.18f / syrup.sprite.bounds.size.x) * UiEase.OutBack(s, 1.6f) * wobble;
                        syrup.color = Color.Lerp(SyrupLight, SyrupColor, s);
                    }
                }

            // Picătura din țeavă cade în buclă până ajunge siropul jos; săgeata de jos se mișcă în sus-jos.
            if (_drop != null && !_engine.Rescued)
            {
                float t = Mathf.Repeat(now * 0.9f, 1f);
                Vector3 inlet = Center(_engine.Inlet) + Vector3.up * _cell * 0.55f;
                _drop.transform.localPosition = inlet + Vector3.down * t * _cell * 0.8f;
                _drop.color = new Color(SyrupColor.r, SyrupColor.g, SyrupColor.b, 1f - t);
            }
            if (_exitArrow != null)
                _exitArrow.localPosition = Center(_engine.Outlet) + Vector3.down * _cell * (0.66f + Mathf.Abs(Mathf.Sin(now * 3f)) * 0.12f);
            if (_pipe != null) _pipe.localScale = new Vector3(1f + Mathf.Sin(now * 4f) * 0.03f, 1f, 1f);
            if (_cage != null && !_rescuing)
                _cage.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 5f) * 2f);
        }

        /// <summary>Siropul se varsă pe pâlnie în borcan, cușca sare în bucăți și Bobo își recapătă culorile.</summary>
        private IEnumerator RescueRoutine(float delay)
        {
            _rescuing = true;
            yield return new WaitForSeconds(delay);
            if (_exitArrow != null) _exitArrow.gameObject.SetActive(false);

            Vector3 from = transform.TransformPoint(Center(_engine.Outlet)) + Vector3.down * _cell * 0.45f;
            Vector3 to = _jarMouth;
            SpriteRenderer stream = Panel("Stream", from, new Vector2(_cell * 0.42f, 0.01f), SyrupColor, 9);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
            {
                // Firul de sirop: dintr-o celulă de jos până la gura borcanului (prin pâlnie).
                Vector3 end = Vector3.Lerp(from, to, t);
                stream.transform.position = (from + end) * 0.5f;
                Vector3 delta = end - from;
                stream.size = new Vector2(_cell * 0.42f, Mathf.Max(0.01f, delta.magnitude));
                stream.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f);
                yield return null;
            }
            // Borcanul se umple cu sirop în spatele lui Bobo.
            float jarHeight = (to.y - _jarCenter.y) * 2f;
            SpriteRenderer fill = Panel("JarSyrup", _jarCenter + Vector3.down * jarHeight * 0.5f, new Vector2(_jarHalfWidth * 1.9f, 0.01f), SyrupColor, 3);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
            {
                float height = jarHeight * 0.55f * UiEase.OutBack(t, 1.2f);
                fill.size = new Vector2(_jarHalfWidth * 1.9f, Mathf.Max(0.01f, height));
                fill.transform.position = _jarCenter + Vector3.down * (jarHeight * 0.5f - height * 0.5f);
                yield return null;
            }
            stream.gameObject.SetActive(false);

            // Cușca se sparge și Bobo își recapătă culorile.
            if (_cage != null)
            {
                Vector3 cagePosition = _cage.transform.position;
                if (SugarVfx.Instance != null)
                {
                    SugarVfx.Instance.IceShatter(cagePosition);
                    SugarVfx.Instance.Confetti(cagePosition + Vector3.up * 1.5f, 4f, 3f);
                }
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
                {
                    _cage.transform.localScale *= 1.04f;
                    _cage.color = new Color(_cage.color.r, _cage.color.g, _cage.color.b, 1f - t);
                    yield return null;
                }
                _cage.gameObject.SetActive(false);
            }
            if (_mascot != null) _mascot.SetSilhouette(false);
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Win, 0.8f);
            yield return new WaitForSeconds(0.4f);
            _rescuing = false;
            Rescued?.Invoke();
        }
    }
}
