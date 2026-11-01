using System.Collections;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using BagElimination.Rules.Simulation;
using UnityEngine;
namespace BagElimination.Presentation
{
    public sealed partial class BoardView
    {
        private readonly Dictionary<string,Sprite> _propSprites=new Dictionary<string,Sprite>();
        private Sprite PropSprite(BoardCell cell)
        {
            string key = cell.Type + "_" + cell.Direction;
            if (_propSprites.TryGetValue(key, out var cached)) return cached;

            // Prefer the real prop art from the asset library; only fall back to the generated
            // placeholder when that specific asset is missing, so nothing is silently dropped.
            Sprite sprite = LoadPropSprite(cell.Type, cell.Direction) ?? GeneratePropSprite(cell);
            _propSprites[key] = sprite;
            return sprite;
        }

        private Sprite LoadPropSprite(ElementType type, RocketDirection direction)
        {
            string path = PropSpritePathFor(type, direction);
            return path == null ? null : LoadSpriteOrNull(path);
        }

        private static string PropSpritePathFor(ElementType type, RocketDirection direction)
        {
            switch (type)
            {
                case ElementType.Rocket:
                    return direction == RocketDirection.Horizontal ? RocketHorizontalSpritePath : RocketVerticalSpritePath;
                case ElementType.Bomb:
                    return BombSpritePath;
                case ElementType.Propeller:
                    return PropellerSpritePath;
                default:
                    return null;
            }
        }

        private Sprite GeneratePropSprite(BoardCell cell)
        {
            var tex = new Texture2D(64, 64) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f; Color color = Color.clear;
                if (cell.Type == ElementType.Bomb)
                { if (dx * dx + dy * dy < 700) color = dy > 10 ? new Color(1, .3f, .3f) : new Color(.3f, .15f, .5f); if (Mathf.Abs(dy) < 6 && Mathf.Abs(dx) < 27) color = Color.yellow; }
                else if (cell.Type == ElementType.Propeller)
                { if (Mathf.Abs(dx - dy) < 9 || Mathf.Abs(dx + dy) < 9) color = new Color(1, .75f, .1f); if (dx * dx + dy * dy < 90) color = Color.red; }
                else
                {
                    float along = cell.Direction == RocketDirection.Horizontal ? dx : dy, across = cell.Direction == RocketDirection.Horizontal ? dy : dx;
                    if (Mathf.Abs(across) < 11 && Mathf.Abs(along) < 27) color = ((int)(along + 32) / 9) % 2 == 0 ? Color.red : Color.yellow;
                    if (along > 20 && Mathf.Abs(across) < 32 - along) color = new Color(.25f, .55f, 1);
                }
                tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
        }
        public void ResetBoard()
        {
            StopAllCoroutines();
            foreach(Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _rendered=false; _board=null;
        }
        private const float SwapSeconds = .18f;
        private const float ClearSeconds = .26f;
        private const float CollectSeconds = .56f;
        private const float LandSeconds = .10f;
        private Sprite _ringSprite;

        private sealed class Motion
        {
            public Transform Transform;
            public readonly List<Vector3> Points = new List<Vector3>();
            public Vector3 Scale;
            public float Delay;
        }

        public IEnumerator PlayFalls(IReadOnlyList<SimulationStep> steps)
        {
            var tracks = new Dictionary<int, Motion>();
            foreach (var p in _board.EnumeratePositions())
            {
                var cell = _board.GetCell(p);
                if (!cell.IsOccupied) continue;
                var tr = _pieceTransforms[p.Row, p.Column];
                var track = new Motion { Transform = tr, Scale = tr.localScale };
                track.Points.Add(tr.localPosition);
                tracks.Add(cell.ObjectId, track);
            }
            var spawnDepth = new int[_board.Width];
            foreach (var step in steps)
                foreach (var p in step.Board.EnumeratePositions())
                {
                    var cell = step.Board.GetCell(p);
                    if (!cell.IsOccupied) continue;
                    Vector3 position = CellCenter(p.Row, p.Column);
                    if (!tracks.TryGetValue(cell.ObjectId, out var track))
                    {
                        var sr = CreateChildSprite("Piece_" + cell.ObjectId, PieceOrder, _boardRoot);
                        sr.sprite = PieceSpriteFor(cell);
                        SetSpriteSize(sr, Vector2.one * CellWorldSize * PieceScaleFactor);
                        track = new Motion { Transform = sr.transform, Scale = sr.transform.localScale };
                        // Later arrivals in the same column start higher, preserving refill order.
                        track.Points.Add(CellCenter(0, p.Column) + Vector3.up * CellSpacing * (++spawnDepth[p.Column]));
                        sr.transform.localPosition = track.Points[0];
                        tracks.Add(cell.ObjectId, track);
                    }
                    if ((track.Points[track.Points.Count - 1] - position).sqrMagnitude > .0001f)
                        track.Points.Add(position);
                }
            var final = steps[steps.Count - 1].Board;
            var next = new Transform[final.Height, final.Width];
            int maxSegments = 1;
            foreach (var p in final.EnumeratePositions())
            {
                var cell = final.GetCell(p);
                if (!cell.IsOccupied) continue;
                var track = tracks[cell.ObjectId];
                next[p.Row, p.Column] = track.Transform;
                track.Delay = p.Column * .012f;
                maxSegments = Mathf.Max(maxSegments, track.Points.Count - 1);
            }
            float duration = Mathf.Clamp(.16f + maxSegments * .045f, .25f, .65f);
            _board = final;
            for (float elapsed = 0; elapsed < duration + .1f + LandSeconds; elapsed += Time.deltaTime)
            {
                foreach (var track in tracks.Values)
                {
                    if (track.Points.Count < 2) continue;
                    float t = Mathf.Clamp01((elapsed - track.Delay) / duration);
                    // Accelerate into the fall; retain all recorded turns around obstacles.
                    float progress = t * t * (track.Points.Count - 1);
                    int segment = Mathf.Min(Mathf.FloorToInt(progress), track.Points.Count - 2);
                    track.Transform.localPosition = Vector3.Lerp(track.Points[segment], track.Points[segment + 1], progress - segment);
                    float landing = Mathf.Clamp01((elapsed - track.Delay - duration) / LandSeconds);
                    float squash = t >= 1 ? Mathf.Sin(landing * Mathf.PI) * .13f : 0;
                    track.Transform.localScale = Vector3.Scale(track.Scale, new Vector3(1 + squash, 1 - squash, 1));
                }
                yield return null;
            }
            foreach (var track in tracks.Values)
            {
                track.Transform.localPosition = track.Points[track.Points.Count - 1];
                track.Transform.localScale = track.Scale;
            }
            _pieceTransforms = next;
        }

        public IEnumerator PlayStep(SimulationStep step)
        {
            if (step.Kind == "Ready" || step.Kind == "Victory") yield break;
            var before = _board;
            var old = new Dictionary<int, Transform>();
            var oldCells = new Dictionary<int, BoardCell>();
            foreach (var p in before.EnumeratePositions())
            {
                var cell = before.GetCell(p);
                var tr = _pieceTransforms[p.Row, p.Column];
                if (cell.IsOccupied && tr != null) { old[cell.ObjectId] = tr; oldCells[cell.ObjectId] = cell; }
            }
            var next = new Transform[step.Board.Height, step.Board.Width];
            var moving = new List<Motion>();
            var created = new HashSet<int>();
            var retained = new HashSet<int>();
            foreach (var p in step.Board.EnumeratePositions())
            {
                var cell = step.Board.GetCell(p);
                if (!cell.IsOccupied) continue;
                retained.Add(cell.ObjectId);
                Vector3 target = CellCenter(p.Row, p.Column);
                if (!old.TryGetValue(cell.ObjectId, out var tr))
                {
                    var sr = CreateChildSprite("Piece_" + cell.ObjectId, PieceOrder, _boardRoot);
                    sr.sprite = PieceSpriteFor(cell);
                    SetSpriteSize(sr, Vector2.one * CellWorldSize * PieceScaleFactor);
                    tr = sr.transform;
                    tr.localPosition = target;
                    created.Add(cell.ObjectId);
                    StartCoroutine(SparkBurst(tr.position, new Color(1, .8f, .25f), 10));
                }
                next[p.Row, p.Column] = tr;
                var motion = new Motion { Transform = tr, Scale = tr.localScale, Delay = created.Contains(cell.ObjectId) ? 1 : 0 };
                motion.Points.Add(tr.localPosition); motion.Points.Add(target);
                moving.Add(motion);
            }
            var removed = new List<Motion>();
            var bags = new List<bool>();
            foreach (var entry in old)
            {
                if (retained.Contains(entry.Key)) continue;
                bool bag = oldCells[entry.Key].Type == ElementType.Backpack;
                var track = new Motion { Transform = entry.Value, Scale = entry.Value.localScale, Delay = bag ? (removed.Count % 5) * .025f : 0 };
                track.Points.Add(entry.Value.position);
                removed.Add(track); bags.Add(bag);
                entry.Value.GetComponent<SpriteRenderer>().sortingOrder = bag ? 80 : 10;
                StartCoroutine(SparkBurst(entry.Value.position, bag ? new Color(1, .7f, .15f) : Color.white, bag ? 8 : 5));
            }
            if (step.Origin >= 0)
            {
                var prop = before.GetCell(step.Origin / before.Width, step.Origin % before.Width);
                StartCoroutine(EffectPulse(step, prop, before));
            }
            _board = step.Board;
            RefreshBackpackCount();
            bool hasBags = bags.Contains(true);
            float duration = step.Kind == "Swap" ? SwapSeconds : hasBags ? CollectSeconds + .1f : step.Origin >= 0 ? .45f : ClearSeconds;
            Vector3 countScale = _countText.transform.localScale;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0, 1, t);
                foreach (var motion in moving)
                {
                    motion.Transform.localPosition = Vector3.Lerp(motion.Points[0], motion.Points[1], ease);
                    if (motion.Delay > 0)
                    {
                        float pop = Mathf.Clamp01(elapsed / ClearSeconds);
                        motion.Transform.localScale = motion.Scale * (pop + Mathf.Sin(pop * Mathf.PI) * .35f);
                    }
                }
                for (int i = 0; i < removed.Count; i++)
                {
                    var motion = removed[i];
                    float p = Mathf.Clamp01((elapsed - motion.Delay) / (bags[i] ? CollectSeconds : ClearSeconds));
                    if (bags[i])
                    {
                        Vector3 start = motion.Points[0], end = CollectionTargetWorld();
                        Vector3 control = (start + end) * .5f + Vector3.right * (i % 2 == 0 ? 1.1f : -1.1f) + Vector3.up * 1.2f;
                        float fly = p * p;
                        motion.Transform.position = (1 - fly) * (1 - fly) * start + 2 * (1 - fly) * fly * control + fly * fly * end;
                        motion.Transform.localScale = motion.Scale * Mathf.Lerp(1.08f, .18f, fly);
                        motion.Transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(p * Mathf.PI) * 18);
                    }
                    else
                    {
                        float scale = p < .22f ? Mathf.Lerp(1, 1.22f, p / .22f) : Mathf.Lerp(1.22f, 0, (p - .22f) / .78f);
                        motion.Transform.localScale = motion.Scale * scale;
                    }
                    var sr = motion.Transform.GetComponent<SpriteRenderer>();
                    sr.color = new Color(1, 1, 1, 1 - Mathf.InverseLerp(.7f, 1, p));
                }
                if (hasBags) _countText.transform.localScale = countScale * (1 + .14f * Mathf.Sin(t * Mathf.PI * 2) * (1 - t));
                yield return null;
            }
            foreach (var motion in moving) { motion.Transform.localPosition = motion.Points[1]; motion.Transform.localScale = motion.Scale; }
            foreach (var motion in removed) Destroy(motion.Transform.gameObject);
            _countText.transform.localScale = countScale;
            _pieceTransforms = next;
        }

        private Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            var tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float distance = new Vector2(x - 31.5f, y - 31.5f).magnitude;
                float alpha = Mathf.Clamp01(1 - Mathf.Abs(distance - 26) / 3);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
            return _ringSprite;
        }

        private IEnumerator SparkBurst(Vector3 origin, Color color, int count)
        {
            var sparks = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                sparks[i] = CreateChildSprite("Spark", 85);
                sparks[i].sprite = WhiteSprite();
                sparks[i].transform.position = origin;
            }
            const float lifetime = .4f;
            for (float elapsed = 0; elapsed < lifetime; elapsed += Time.deltaTime)
            {
                float t = elapsed / lifetime;
                for (int i = 0; i < count; i++)
                {
                    // Purely visual deterministic spread: never consumes gameplay random state.
                    float angle = i * Mathf.PI * 2 / count;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    sparks[i].transform.position = origin + direction * (.18f + .55f * t) + Vector3.down * t * t * .3f;
                    sparks[i].transform.localScale = Vector3.one * (.07f * (1 - t));
                    sparks[i].transform.localRotation = Quaternion.Euler(0, 0, i * 43 + t * 180);
                    sparks[i].color = new Color(color.r, color.g, color.b, 1 - t);
                }
                yield return null;
            }
            foreach (var spark in sparks) Destroy(spark.gameObject);
        }

        private IEnumerator EffectPulse(SimulationStep step, BoardCell prop, BoardState before)
        {
            Vector3 origin = _boardRoot.transform.TransformPoint(CellCenter(step.Origin / _board.Width, step.Origin % _board.Width));
            var flash = CreateChildSprite("Effect", 45);
            flash.transform.position = origin;
            bool rocket = step.Kind == "Rocket";
            flash.sprite = rocket ? WhiteSprite() : RingSprite();
            bool horizontal = prop.Direction == RocketDirection.Horizontal;
            if (rocket)
            {
                // Center the beam on the board, not on an off-center trigger cell.
                Vector3 local = CellCenter(step.Origin / _board.Width, step.Origin % _board.Width);
                if (horizontal) local.x = 0;
                else local.y = (CellCenter(0, 0).y + CellCenter(_board.Height - 1, 0).y) * .5f;
                flash.transform.position = _boardRoot.transform.TransformPoint(local);
            }
            Vector3 target = origin;
            if (step.Kind == "Propeller")
            {
                // Remote target follows the rules' row-major/right-to-left selection,
                // excluding backpacks already hit by the local four-neighbour effect.
                bool found = false;
                int row = step.Origin / before.Width, column = step.Origin % before.Width;
                for (int r = 0; r < before.Height && !found; r++)
                    for (int c = before.Width - 1; c >= 0; c--)
                        if (before.GetCell(r, c).Type == ElementType.Backpack
                            && Mathf.Abs(r - row) + Mathf.Abs(c - column) > 1
                            && !step.Board.GetCell(r, c).IsOccupied)
                        { target = _boardRoot.transform.TransformPoint(CellCenter(r, c)); found = true; break; }
                flash.sprite = PropSprite(prop);
            }
            const float seconds = .42f;
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
            {
                float t = elapsed / seconds;
                if (rocket)
                {
                    float length = (horizontal ? _board.Width : _board.Height) * CellSpacing;
                    flash.transform.localScale = horizontal ? new Vector3(length, .24f * (1 - t), 1) : new Vector3(.24f * (1 - t), length, 1);
                }
                else if (step.Kind == "Propeller")
                {
                    flash.transform.position = Vector3.Lerp(origin, target, Mathf.SmoothStep(0, 1, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .7f;
                    flash.transform.localScale = Vector3.one * .85f;
                    flash.transform.localRotation = Quaternion.Euler(0, 0, t * 1080);
                }
                else flash.transform.localScale = Vector3.one * Mathf.Lerp(.4f, 5 * CellSpacing, 1 - (1 - t) * (1 - t));
                flash.color = new Color(1, .85f, .35f, 1 - t);
                yield return null;
            }
            Destroy(flash.gameObject);
        }

        public IEnumerator Celebrate() { yield return VictorySequence(); }
    }
}
