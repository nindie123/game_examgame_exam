using System.Collections;
using UnityEngine;

namespace BagElimination.Presentation
{
    public sealed partial class BoardView
    {
        private SpriteRenderer _scenery;
        private SpriteRenderer _victoryShade;
        private Transform _rewardIdle;
        private Transform _haloIdle;
        private readonly SpriteRenderer[] _dust = new SpriteRenderer[24];
        private float _sceneryTime;

        private void BuildScenery()
        {
            _rewardIdle = null;
            _haloIdle = null;
            _victoryShade = null;
            _sceneryTime = 0;
            _scenery = CreateChildSprite("Scenery", BackgroundOrder);
            _scenery.sprite = BackgroundSprite();
            // Exact viewport fit: the old scale multiplied the texture's natural size,
            // cropping almost the whole gradient and making it look like a flat fill.
            FitScenery();
            for (int i = 0; i < _dust.Length; i++)
            {
                _dust[i] = CreateChildSprite("AmbientLight_" + i, -80);
                _dust[i].sprite = RingSprite();
            }
        }

        private void FitScenery()
        {
            if (_scenery == null || Camera.main == null) return;
            float height = Camera.main.orthographicSize * 2;
            SetSpriteSize(_scenery, new Vector2(height * Camera.main.aspect + .05f, height + .05f));
            _scenery.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0);
            if (_victoryShade != null)
            {
                SetSpriteSize(_victoryShade, new Vector2(height * Camera.main.aspect + .05f, height + .05f));
                _victoryShade.transform.position = _scenery.transform.position;
            }
        }

        private void LateUpdate()
        {
            if (!_rendered || Camera.main == null) return;
            LayoutUi();
            FitScenery();
            _sceneryTime += Time.deltaTime;
            float halfHeight = Camera.main.orthographicSize;
            float halfWidth = halfHeight * Camera.main.aspect;
            for (int i = 0; i < _dust.Length; i++)
            {
                if (_dust[i] == null) continue;
                float phase = i * 2.39996f;
                float x = Mathf.Sin(phase) * halfWidth * .94f + Mathf.Sin(_sceneryTime * .12f + phase) * .12f;
                float y = Mathf.Repeat(i * .731f + _sceneryTime * (.035f + i % 3 * .015f), 2 * halfHeight) - halfHeight;
                _dust[i].transform.localPosition = new Vector3(x, y, 0);
                _dust[i].transform.localScale = Vector3.one * (.035f + i % 4 * .025f);
                _dust[i].color = new Color(.6f, .86f, 1, .12f + .1f * Mathf.Sin(phase + _sceneryTime * .6f));
            }
            if (_rewardIdle != null)
            {
                _rewardIdle.localPosition = new Vector3(0, -.15f + Mathf.Sin(_sceneryTime * 1.6f) * .07f, 0);
                _rewardIdle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_sceneryTime) * 2);
            }
            if (_haloIdle != null) _haloIdle.Rotate(0, 0, Time.deltaTime * 8);
        }

        private TextMesh VictoryText(string name, string message, Vector3 position, float size, Color color, GameObject root)
        {
            var go = CreateChildObject(name, root);
            go.transform.localPosition = position;
            var text = go.AddComponent<TextMesh>();
            text.text = message;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 72;
            text.characterSize = size;
            text.color = color;
            if (_font != null)
            {
                text.font = _font;
                go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            }
            go.GetComponent<MeshRenderer>().sortingOrder = 77;
            return text;
        }

        private IEnumerator VictorySequence()
        {
            yield return new WaitForSeconds(.4f);
            ClearSelection();
            HideHint();
            _victoryShade = CreateChildSprite("VictoryShade", 65);
            _victoryShade.sprite = WhiteSprite();
            _victoryShade.color = new Color(.015f, .035f, .09f, 0);
            FitScenery();
            Vector3 position = _boardRoot.transform.localPosition;
            Vector3 scale = _boardRoot.transform.localScale;
            Transform panel = _goalPanel;
            Vector3 panelPosition = panel != null ? panel.localPosition : Vector3.zero;
            for (float elapsed = 0; elapsed < .55f; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / .55f);
                _boardRoot.transform.localPosition = position + Vector3.down * t * .8f;
                _boardRoot.transform.localScale = scale * (1 - .12f * t);
                _victoryShade.color = new Color(.015f, .035f, .09f, t * .94f);
                SetGoalExit(t);
                yield return null;
            }
            _boardRoot.SetActive(false);
            if (panel != null) panel.gameObject.SetActive(false);
            var root = CreateChildObject("VictoryPresentation");
            var title = VictoryText("Title", "Great!", new Vector3(0, 2.15f, 0), .105f, new Color(1, .81f, .31f), root);
            var subtitle = VictoryText("Subtitle", "ALL BAGS COLLECTED", new Vector3(0, 1.42f, 0), .027f, new Color(.65f, .84f, .91f), root);
            var caption = VictoryText("Caption", "Tap Restart to play again", new Vector3(0, -2.05f, 0), .027f, new Color(.65f, .84f, .91f), root);
            var halo = CreateChildSprite("RewardHalo", 68, root);
            halo.sprite = RingSprite();
            halo.color = new Color(1, .75f, .25f, .3f);
            halo.transform.localPosition = new Vector3(0, -.15f, 0);
            halo.transform.localScale = Vector3.one * 3.3f;
            var reward = CreateChildSprite("Reward", 75, root);
            reward.sprite = _goalSprite;
            Vector2 natural = SpriteNaturalSize(_goalSprite);
            float factor = 2.2f / Mathf.Max(natural.x, natural.y);
            Vector3 rewardScale = Vector3.one * factor;
            for (float elapsed = 0; elapsed < .85f; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / .85f);
                float pop = 1 + 2.7f * Mathf.Pow(t - 1, 3) + 1.7f * Mathf.Pow(t - 1, 2);
                reward.transform.localScale = rewardScale * pop;
                reward.transform.localPosition = new Vector3(0, -.15f + .45f * (1 - t), 0);
                reward.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2) * 8 * (1 - t));
                title.transform.localScale = Vector3.one * pop;
                subtitle.color = new Color(.65f, .84f, .91f, t);
                caption.color = new Color(.65f, .84f, .91f, t);
                halo.transform.localScale = Vector3.one * (2.6f + t * .7f);
                yield return null;
            }
            title.transform.localScale = Vector3.one;
            reward.transform.localScale = rewardScale;
            _rewardIdle = reward.transform;
            _haloIdle = halo.transform;
            SetHudLabel("auto", "Auto");
            yield return VictoryConfetti(root);
        }

        private IEnumerator VictoryConfetti(GameObject root)
        {
            const int count = 42;
            var pieces = new SpriteRenderer[count];
            Color[] palette = { new Color(1, .75f, .2f), new Color(.25f, .85f, .95f), new Color(1, .43f, .56f) };
            for (int i = 0; i < count; i++)
            {
                pieces[i] = CreateChildSprite("Confetti", 78, root);
                pieces[i].sprite = WhiteSprite();
            }
            for (float elapsed = 0; elapsed < 2.1f; elapsed += Time.deltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = Mathf.Clamp01((elapsed - i % 7 * .035f) / 1.8f);
                    float side = i % 2 == 0 ? -1 : 1;
                    Vector3 start = new Vector3(side * 2.9f, .3f, 0);
                    Vector3 velocity = new Vector3(-side * (1.2f + i % 5 * .28f), 3 + i % 6 * .3f, 0);
                    pieces[i].transform.localPosition = start + velocity * t + Vector3.down * 4 * t * t;
                    pieces[i].transform.localScale = new Vector3(.045f * Mathf.Cos(t * 13 + i), .11f, 1);
                    pieces[i].transform.localRotation = Quaternion.Euler(0, 0, i * 29 + t * 260);
                    Color color = palette[i % palette.Length];
                    color.a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, t));
                    pieces[i].color = color;
                }
                yield return null;
            }
            foreach (var piece in pieces) Destroy(piece.gameObject);
        }
    }
}
