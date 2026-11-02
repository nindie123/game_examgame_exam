using System.Collections;
using System.Collections.Generic;
using BagElimination.Rules.Board;
using UnityEngine;

namespace BagElimination.Presentation
{
    /// <summary>
    /// Passive board renderer. <see cref="GameBootstrap"/> builds the authoritative
    /// <see cref="BoardState"/> and passes it into <see cref="Render"/>; this class only reads
    /// that board and displays it, so it never creates a second gameplay state.
    ///
    /// Draw order (same default sorting layer): background, board plate, base slots ("底格"),
    /// pieces on top, then the top panel with the backpack icon and remaining count.
    /// Board dimensions are read from the passed-in board, not hardcoded here.
    /// </summary>
    public sealed partial class BoardView : MonoBehaviour
    {
        // --- Layout config (visual scale; the board SIZE comes from BoardState at render time) ---
        private const float CellSpacing = 0.8f;         // World distance between neighbouring cell centers.
        private const float CellWorldSize = 0.7f;       // Base slot ("底格") width/height.
        private const float PieceScaleFactor = 0.88f;   // Piece size relative to the base slot, so the slot peeks out.
        private const float BoardPlatePadding = 0.35f;  // Plate extends this far beyond the outermost slots.
        private const float TopPanelMargin = 0.3f;      // Gap between the top panel and the screen top edge.
        private const float BoardPanelGap = 0.4f;       // Gap between the top panel bottom and the board plate top.
        private const float TopPanelWorldWidth = 6.5f;  // Target width of the top panel (height keeps aspect).
        private const float GoalIconWorldSize = 0.85f;  // Target size of the backpack icon.
        private const float GoalIconOffsetX = -0.35f;   // Icon center, horizontal offset from the panel center.
        private const float CountTextOffsetX = 0.10f;   // Count text start (MiddleLeft), horizontal offset from the panel center.

        // --- Text visual scale (adjust if the count looks too big or too small) ---
        private const int CountFontSize = 100;
        private const float CountCharacterSize = 0.06f;

        // --- Selection & swap feedback ---
        private const float SelectionScale = 1.5f;                 // Highlight size relative to the base slot.
        public const float SwapDuration = 0.18f;                   // Seconds for one swap-then-rollback leg.
        public const float HintHoldSeconds = 1.0f;                 // How long the valid-swap hint stays visible.
        public const string SpecialSwapHint = "特殊匹配尚未接入";
        public const string RoundCompleteHint = "本轮消除完成，下落待接入";
        private const int HintFontSize = 48;
        private const float HintCharacterSize = 0.05f;

        // --- Colors ---
        private static readonly Color BackgroundColor = new Color(0.10f, 0.16f, 0.28f);  // Soft dark blue.
        private static readonly Color BoardPlateColor = Color.white;
        private static readonly Color FallbackCellColor = new Color(0.30f, 0.42f, 0.62f);
        private static readonly Color FallbackPlateColor = new Color(0.14f, 0.20f, 0.34f, 0.92f);
        private static readonly Color FallbackBackpackColor = new Color(0.62f, 0.47f, 0.26f);
        private static readonly Color SelectionHighlightColor = new Color(1f, 0.86f, 0.30f, 1f);

        // --- Resource paths (relative to any Resources folder) ---
        private const string TileSpritePath = "Presentation/tileA";
        private const string PlateSpritePath = "Presentation/UIpanel_outside";
        private const string PanelSpritePath = "Presentation/UIpanel_top_Minigame";
        private const string GoalSpritePath = "Presentation/goal_suitcase";
        private const string FontPath = "Presentation/BPreplay";
        private const string RedSpritePath = "Presentation/candy_1_1_1";
        private const string YellowSpritePath = "Presentation/candy_1_2_1";
        private const string BlueSpritePath = "Presentation/candy_1_3_1";
        private const string GreenSpritePath = "Presentation/candy_1_4_1";
        private const string BackpackSpritePath = "Presentation/candy_7_0_1";
        private const string RocketHorizontalSpritePath = "Presentation/boost_item_hor";
        private const string RocketVerticalSpritePath = "Presentation/boost_item_ver";
        private const string BombSpritePath = "Presentation/boost_candy_bomb";
        private const string PropellerSpritePath = "Presentation/boost_item_xcross";

        // --- Draw order within the default sorting layer ---
        private const int BackgroundOrder = -100;
        private const int BoardPlateOrder = 0;
        private const int BaseSlotOrder = 1;
        private const int SelectionHighlightOrder = 2; // A ring behind the selected piece, so the candy stays visible.
        private const int PieceOrder = 3;
        private const int TopPanelOrder = 50;
        private const int TopPanelOverlayOrder = 60;

        private BoardState _board;
        private bool _rendered;
        private GameObject _boardRoot;
        private Transform[,] _pieceTransforms;
        private SpriteRenderer _selectionHighlight;
        private TextMesh _hintText;
        private UnityEngine.UI.Text _countText;

        private Sprite _whiteSprite;
        private Sprite _cellSprite;
        private Sprite _plateSprite;
        private Sprite _panelSprite;
        private Sprite _goalSprite;
        private Sprite _redSprite;
        private Sprite _yellowSprite;
        private Sprite _blueSprite;
        private Sprite _greenSprite;
        private Sprite _backpackSprite;
        private Sprite _backgroundSprite;
        private Sprite _selectionRingSprite;
        private Coroutine _selectionPulse;
        private Font _font;

        public void Render(BoardState board)
        {
            if (board == null)
            {
                Debug.LogError("[BoardView] Render 收到空的棋盘，无法绘制。");
                return;
            }

            if (_rendered)
            {
                Debug.LogWarning("[BoardView] Render 已执行过，忽略重复调用。");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("[BoardView] 需要场景中存在标记为 MainCamera 的相机才能定位棋盘。");
                return;
            }

            Camera.main.orthographicSize = Mathf.Max(5.3f, 4.1f / Camera.main.aspect);
            _rendered = true;
            _board = board;

            LoadAssets();

            BuildBackground();

            // UI occupies the top and bottom safe areas; the world board stays centered.
            float boardCenterY = 0f;

            GameObject boardRoot = CreateChildObject("Board");
            boardRoot.transform.localPosition = new Vector3(0f, boardCenterY, 0f);
            _boardRoot = boardRoot;
            BuildBoardPlate(boardRoot);
            BuildBaseSlots(boardRoot);
            BuildPieces(boardRoot);
            BuildSelectionHighlight(boardRoot);
            BuildHint();
            BuildHud();
        }

        private void LoadAssets()
        {
            _cellSprite = LoadRequiredSprite(TileSpritePath, "底格");
            _plateSprite = LoadRequiredSprite(PlateSpritePath, "棋盘底板");
            _panelSprite = LoadRequiredSprite(PanelSpritePath, "顶部面板");
            _goalSprite = LoadRequiredSprite(GoalSpritePath, "背包图标");
            _redSprite = LoadRequiredSprite(RedSpritePath, "红色棋子");
            _yellowSprite = LoadRequiredSprite(YellowSpritePath, "黄色棋子");
            _blueSprite = LoadRequiredSprite(BlueSpritePath, "蓝色棋子");
            _greenSprite = LoadRequiredSprite(GreenSpritePath, "绿色棋子");
            _backpackSprite = LoadRequiredSprite(BackpackSpritePath, "背包棋子");
            _font = LoadFont(FontPath);
        }

        private void BuildBackground()
        {
            BuildScenery();
        }
        private void BuildBoardPlate(GameObject root)
        {
            Vector2 size = new Vector2((_board.Width-1)*CellSpacing+CellWorldSize+.24f,(_board.Height-1)*CellSpacing+CellWorldSize+.24f);
            var frame=CreateChildSprite("BoardFrame",-2,root);
            frame.sprite=RoundedUiSprite(); frame.drawMode=SpriteDrawMode.Sliced; frame.size=size+Vector2.one*.08f;
            frame.color=new Color(.22f,.43f,.55f);
            var plate=CreateChildSprite("BoardPlate",-1,root);
            plate.sprite=RoundedUiSprite(); plate.drawMode=SpriteDrawMode.Sliced; plate.size=size;
            plate.color=new Color(.045f,.12f,.19f);
        }
        private void BuildBaseSlots(GameObject boardRoot)
        {
            GameObject gridRoot = CreateChildObject("Grid", boardRoot);

            for (int row = 0; row < _board.Height; row++)
            {
                for (int col = 0; col < _board.Width; col++)
                {
                    // Base slot ("底格") is drawn for every cell, occupied or not, so the board shape
                    // is fully visible. Row 0 is at the top, column 0 at the left (BoardPosition order).
                    SpriteRenderer slot = CreateChildSprite(string.Format("Slot_{0}_{1}", row, col), BaseSlotOrder, gridRoot);
                    slot.transform.localPosition = CellCenter(row, col);
                    if (_cellSprite != null)
                    {
                        slot.sprite = _cellSprite;
                        slot.color = Color.white;
                        SetSpriteSize(slot, new Vector2(CellWorldSize, CellWorldSize));
                    }
                    else
                    {
                        slot.sprite = WhiteSprite();
                        slot.color = FallbackCellColor;
                        slot.transform.localScale = new Vector3(CellWorldSize, CellWorldSize, 1f);
                    }
                }
            }
        }

        private void BuildPieces(GameObject boardRoot)
        {
            GameObject piecesRoot = CreateChildObject("Pieces", boardRoot);
            float pieceSize = CellWorldSize * PieceScaleFactor;
            _pieceTransforms = new Transform[_board.Height, _board.Width];

            for (int row = 0; row < _board.Height; row++)
            {
                for (int col = 0; col < _board.Width; col++)
                {
                    BoardCell cell = _board.GetCell(row, col);
                    if (!cell.IsOccupied)
                    {
                        continue;
                    }

                    Sprite sprite = PieceSpriteFor(cell);

                    SpriteRenderer piece = CreateChildSprite(string.Format("Piece_{0}_{1}", row, col), PieceOrder, piecesRoot);
                    piece.transform.localPosition = CellCenter(row, col);
                    _pieceTransforms[row, col] = piece.transform;
                    if (sprite != null)
                    {
                        piece.sprite = sprite;
                        piece.color = Color.white;
                        SetSpriteSize(piece, new Vector2(pieceSize, pieceSize));
                    }
                    else
                    {
                        // A missing asset was already reported by LoadAssets; still draw a placeholder so
                        // the object is visible and never silently dropped.
                        piece.sprite = WhiteSprite();
                        piece.color = cell.Type == ElementType.Backpack ? FallbackBackpackColor : FallbackCellColor;
                        piece.transform.localScale = new Vector3(pieceSize, pieceSize, 1f);
                    }
                }
            }
        }

        private Sprite PieceSpriteFor(BoardCell cell)
        {
            if (cell.Type == ElementType.Backpack)
            {
                return _backpackSprite;
            }

            if (cell.Type == ElementType.NormalPiece)
            {
                switch (cell.Color)
                {
                    case PieceColor.Red:
                        return _redSprite;
                    case PieceColor.Yellow:
                        return _yellowSprite;
                    case PieceColor.Blue:
                        return _blueSprite;
                    case PieceColor.Green:
                        return _greenSprite;
                }
            }

            if (cell.Type == ElementType.Rocket) return PropSprite(cell);
            if (cell.Type == ElementType.Bomb || cell.Type == ElementType.Propeller) return PropSprite(cell);
            // Anything else (a prop, or an unknown color) has no mapped sprite; log and render a placeholder.
            Debug.LogWarning("[BoardView] 无法确定格子显示的素材: " + cell + "，使用占位色块。");
            return null;
        }

        private Vector3 CellCenter(int row, int col)
        {
            float x = (col - (_board.Width - 1) * 0.5f) * CellSpacing;
            float y = ((_board.Height - 1) * 0.5f - row) * CellSpacing;
            return new Vector3(x, y, 0f);
        }

        private void BuildSelectionHighlight(GameObject boardRoot)
        {
            _selectionHighlight = CreateChildSprite("SelectionHighlight", SelectionHighlightOrder, boardRoot);
            _selectionHighlight.sprite = SelectionRingSprite();
            _selectionHighlight.color = SelectionHighlightColor;
            float size = CellWorldSize * SelectionScale;
            _selectionHighlight.transform.localScale = new Vector3(size, size, 1f);
            _selectionHighlight.gameObject.SetActive(false);
        }

        private void BuildHint()
        {
            var hintObject = CreateChildObject("SwapHint");
            _hintText = hintObject.AddComponent<TextMesh>();
            _hintText.anchor = TextAnchor.MiddleCenter;
            _hintText.alignment = TextAlignment.Center;
            _hintText.fontSize = HintFontSize;
            _hintText.characterSize = HintCharacterSize;
            _hintText.color = SelectionHighlightColor;
            if (_font != null)
            {
                _hintText.font = _font;
                hintObject.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            }

            MeshRenderer mesh = hintObject.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.sortingOrder = TopPanelOverlayOrder;
            }

            // Below the board plate, out of the way of both the board and the top panel.
            float boardSpanY = (_board.Height - 1) * CellSpacing + CellWorldSize;
            float boardBottom = _boardRoot.transform.position.y - boardSpanY * 0.5f;
            hintObject.transform.localPosition = new Vector3(0f, boardBottom - 0.5f, 0f);
            hintObject.SetActive(false);
        }

        /// <summary>
        /// Inverse of <see cref="CellCenter"/> using the same <see cref="CellSpacing"/> constant and
        /// board root, so click picking never duplicates the layout as a second set of numbers.
        /// </summary>
        public bool TryGetCell(Vector3 worldPosition, out int row, out int col)
        {
            row = 0;
            col = 0;
            if (_board == null || _boardRoot == null)
            {
                return false;
            }

            Vector3 local = worldPosition - _boardRoot.transform.position;
            float colFloat = local.x / CellSpacing + (_board.Width - 1) * 0.5f;
            float rowFloat = (_board.Height - 1) * 0.5f - local.y / CellSpacing;
            int candidateCol = Mathf.RoundToInt(colFloat);
            int candidateRow = Mathf.RoundToInt(rowFloat);
            if (candidateRow < 0 || candidateRow >= _board.Height || candidateCol < 0 || candidateCol >= _board.Width)
            {
                return false;
            }

            row = candidateRow;
            col = candidateCol;
            return true;
        }

        public void ShowSelection(int row, int col)
        {
            if (_selectionHighlight == null)
            {
                return;
            }

            _selectionHighlight.transform.localPosition = CellCenter(row, col);
            _selectionHighlight.gameObject.SetActive(true);
            if (_selectionPulse != null)
            {
                StopCoroutine(_selectionPulse);
            }
            _selectionPulse = StartCoroutine(PulseSelection());
        }

        public void ClearSelection()
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.gameObject.SetActive(false);
            }

            if (_selectionPulse != null)
            {
                StopCoroutine(_selectionPulse);
                _selectionPulse = null;
            }
        }

        private IEnumerator PulseSelection()
        {
            float baseSize = CellWorldSize * SelectionScale;
            while (true)
            {
                // Gentle "breathing" so the selected piece stays obvious without a flashing block.
                float wave = 1f + 0.12f * Mathf.Sin(Time.time * 3.2f);
                if (_selectionHighlight != null)
                {
                    _selectionHighlight.transform.localScale = new Vector3(baseSize * wave, baseSize * wave, 1f);
                }
                yield return null;
            }
        }

        public void ShowHint(string message)
        {
            if (_hintText != null)
            {
                _hintText.text = message;
                _hintText.gameObject.SetActive(true);
            }
        }

        public void HideHint()
        {
            if (_hintText != null)
            {
                _hintText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Exchanges the two pieces' screen positions over <paramref name="duration"/> seconds.
        /// <see cref="_pieceTransforms"/> stays keyed by the original cell, so calling this again with
        /// the same two coordinates swaps them back — the authoritative board data is never touched.
        /// </summary>
        public IEnumerator AnimateSwap(int firstRow, int firstCol, int secondRow, int secondCol, float duration)
        {
            if (_pieceTransforms == null)
            {
                yield break;
            }

            if (firstRow < 0 || firstRow >= _board.Height || firstCol < 0 || firstCol >= _board.Width
                || secondRow < 0 || secondRow >= _board.Height || secondCol < 0 || secondCol >= _board.Width)
            {
                yield break;
            }

            Transform first = _pieceTransforms[firstRow, firstCol];
            Transform second = _pieceTransforms[secondRow, secondCol];
            if (first == null || second == null)
            {
                // An empty cell has no piece transform; there is nothing to animate. The swap itself
                // should never have been submitted for a non-movable cell, so log it as a diagnostic.
                Debug.LogWarning("[BoardView] 交换动画收到空棋子格子，已跳过。");
                yield break;
            }

            Vector3 firstStart = first.localPosition;
            Vector3 secondStart = second.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                first.localPosition = Vector3.Lerp(firstStart, secondStart, t);
                second.localPosition = Vector3.Lerp(secondStart, firstStart, t);
                yield return null;
            }

            first.localPosition = secondStart;
            second.localPosition = firstStart;
        }

        /// <summary>
        /// Re-keys the piece look-up after a committed swap so that _pieceTransforms[row, col]
        /// names the piece that now occupies that cell. Call after <see cref="AnimateSwap"/> has
        /// moved the two pieces, before any clear step reads the array again.
        /// </summary>
        public void FinalizeSwap(int firstRow, int firstCol, int secondRow, int secondCol)
        {
            if (_pieceTransforms == null)
            {
                return;
            }

            Transform swap = _pieceTransforms[firstRow, firstCol];
            _pieceTransforms[firstRow, firstCol] = _pieceTransforms[secondRow, secondCol];
            _pieceTransforms[secondRow, secondCol] = swap;
        }

        /// <summary>
        /// Hides the piece at each listed position. The look-up must already reflect the committed
        /// board (see <see cref="FinalizeSwap"/>); this only changes the view, never the data.
        /// </summary>
        public void RemoveOccupants(IReadOnlyList<BoardPosition> positions)
        {
            if (_pieceTransforms == null)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                BoardPosition position = positions[i];
                Transform piece = _pieceTransforms[position.Row, position.Column];
                if (piece != null)
                {
                    piece.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Adopts the post-elimination snapshot as the board this view reads (backpack count,
        /// picking). It never rebuilds background/plate/panel; those are created once in Render.
        /// </summary>
        public void ApplyCommittedBoard(BoardState board)
        {
            if (board == null)
            {
                return;
            }

            _board = board;
            RefreshBackpackCount();
        }

        private void RefreshBackpackCount()
        {
            if (_countText != null && _board != null)
            {
                _countText.text = _board.CountBackpacks().ToString();
            }
        }

        private static Vector2 SpriteNaturalSize(Sprite sprite)
        {
            if (sprite == null)
            {
                return Vector2.zero;
            }

            Bounds bounds = sprite.bounds;
            return new Vector2(bounds.size.x, bounds.size.y);
        }

        private static void SetSpriteSize(SpriteRenderer renderer, Vector2 targetSize)
        {
            Vector2 natural = renderer.sprite != null
                ? new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
                : Vector2.one;
            if (natural.x <= 0f || natural.y <= 0f)
            {
                return;
            }

            // Scale x/y only; a sprite has no meaningful thickness along z.
            renderer.transform.localScale = new Vector3(targetSize.x / natural.x, targetSize.y / natural.y, 1f);
        }

        private SpriteRenderer CreateChildSprite(string name, int sortingOrder, GameObject parent = null)
        {
            GameObject child = CreateChildObject(name, parent);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private GameObject CreateChildObject(string name, GameObject parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent != null ? parent.transform : transform, worldPositionStays: false);
            return child;
        }

        private Sprite BackgroundSprite()
        {
            if (_backgroundSprite != null)
            {
                return _backgroundSprite;
            }

            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Color top = new Color(0.035f, 0.20f, 0.24f);     // 柔和深蓝，顶部略亮。
            Color bottom = new Color(0.018f, 0.035f, 0.10f);  // 底部略微加深。
            Color glow = new Color(0.06f, 0.34f, 0.39f);    // 棋盘后方轻微提亮。
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    Color color = Color.Lerp(bottom, top, v);

                    // 靠近中上方的柔光，衬托棋盘，不产生生硬分界。
                    float dx = u - 0.5f;
                    float dy = v - 0.55f;
                    float center = Mathf.Clamp01(1f - (dx * dx * 5f + dy * dy * 6f));
                    color = Color.Lerp(color, glow, center * 0.48f);

                    // 柔和暗角，把视线留在棋盘上。
                    float edge = new Vector2(u - 0.5f, v - 0.5f).magnitude;
                    float vignette = Mathf.Clamp01(1f - Mathf.Max(0f, edge - 0.32f) * 1.4f);
                    color *= Mathf.Lerp(0.82f, 1f, vignette);

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            _backgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _backgroundSprite.name = "BackgroundGradient";
            return _backgroundSprite;
        }

        private Sprite SelectionRingSprite()
        {
            if (_selectionRingSprite != null)
            {
                return _selectionRingSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = new Vector2(x - (size - 1) * 0.5f, y - (size - 1) * 0.5f).magnitude;
                    // 内、外缘都柔和衰减，形成描边光环而非方块。
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - 26f) / 6f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _selectionRingSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _selectionRingSprite.name = "SelectionRing";
            return _selectionRingSprite;
        }

        private Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.name = "White1x1";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _whiteSprite.name = "White1x1";
            return _whiteSprite;
        }

        private static Sprite LoadRequiredSprite(string resourcesPath, string label)
        {
            Sprite sprite = LoadSpriteOrNull(resourcesPath);
            if (sprite == null)
            {
                Debug.LogError("[BoardView] 缺少素材: " + resourcesPath + " (" + label + ")，将用占位色块显示该对象。");
            }

            return sprite;
        }

        private static Font LoadFont(string resourcesPath)
        {
            Font font = Resources.Load<Font>(resourcesPath);
            if (font == null)
            {
                Debug.LogError("[BoardView] 缺少字体: " + resourcesPath + "，将回退到内置字体。");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static Sprite LoadSpriteOrNull(string resourcesPath)
        {
            // Handles both Single import (sprite named after the texture) and Multiple import
            // (sub-sprite named "<name>_0"); LoadAll covers the latter.
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            Sprite single = Resources.Load<Sprite>(resourcesPath);
            if (single != null)
            {
                return single;
            }

            // Fallback: the texture was not imported as a Sprite, reconstruct one from the Texture2D.
            Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture != null)
            {
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }

            return null;
        }
    }
}
