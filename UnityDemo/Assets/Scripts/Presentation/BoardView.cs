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
    public sealed class BoardView : MonoBehaviour
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

        // --- Text visual scale (adjust if the count looks too big or too small) ---
        private const int CountFontSize = 100;
        private const float CountCharacterSize = 0.06f;

        // --- Colors ---
        private static readonly Color BackgroundColor = new Color(0.10f, 0.16f, 0.28f);  // Soft dark blue.
        private static readonly Color BoardPlateColor = Color.white;
        private static readonly Color FallbackCellColor = new Color(0.30f, 0.42f, 0.62f);
        private static readonly Color FallbackPlateColor = new Color(0.14f, 0.20f, 0.34f, 0.92f);
        private static readonly Color FallbackBackpackColor = new Color(0.62f, 0.47f, 0.26f);

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

        // --- Draw order within the default sorting layer ---
        private const int BackgroundOrder = -100;
        private const int BoardPlateOrder = 0;
        private const int BaseSlotOrder = 1;
        private const int PieceOrder = 2;
        private const int TopPanelOrder = 50;
        private const int TopPanelOverlayOrder = 60;

        private BoardState _board;
        private bool _rendered;

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

            _rendered = true;
            _board = board;

            LoadAssets();

            BuildBackground();

            // Measure the top panel first, then fit the board below it.
            float panelHeight = BuildTopPanel();
            float boardCenterY = ComputeBoardCenterY(panelHeight);

            GameObject boardRoot = CreateChildObject("Board");
            boardRoot.transform.localPosition = new Vector3(0f, boardCenterY, 0f);
            BuildBoardPlate(boardRoot);
            BuildBaseSlots(boardRoot);
            BuildPieces(boardRoot);
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
            SpriteRenderer background = CreateChildSprite("Background", BackgroundOrder);
            background.sprite = WhiteSprite();
            background.color = BackgroundColor;

            float height = 2f * Camera.main.orthographicSize;
            float width = height * Camera.main.aspect;
            // Oversize by a full screen so the background still covers the view when the camera or aspect changes.
            background.transform.localScale = new Vector3(width * 2f, height * 2f, 1f);
        }

        private float ComputeBoardCenterY(float panelHeight)
        {
            float halfViewHeight = Camera.main.orthographicSize;
            float boardSpanY = (_board.Height - 1) * CellSpacing + CellWorldSize;
            float panelBottom = halfViewHeight - TopPanelMargin - panelHeight;

            // The gap is measured to the top of the plate (not the top slot) so the plate never
            // sits flush against the panel; the plate extends BoardPlatePadding above the slots.
            return panelBottom - BoardPanelGap - (boardSpanY * 0.5f + BoardPlatePadding);
        }

        private void BuildBoardPlate(GameObject boardRoot)
        {
            float width = (_board.Width - 1) * CellSpacing + CellWorldSize + 2f * BoardPlatePadding;
            float height = (_board.Height - 1) * CellSpacing + CellWorldSize + 2f * BoardPlatePadding;

            SpriteRenderer plate = CreateChildSprite("BoardPlate", BoardPlateOrder, boardRoot);
            if (_plateSprite != null)
            {
                plate.sprite = _plateSprite;
                plate.color = BoardPlateColor;
                SetSpriteSize(plate, new Vector2(width, height));
            }
            else
            {
                plate.sprite = WhiteSprite();
                plate.color = FallbackPlateColor;
                plate.transform.localScale = new Vector3(width, height, 1f);
            }
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

        private float BuildTopPanel()
        {
            GameObject panelRoot = CreateChildObject("TopPanel");

            float height;
            if (_panelSprite != null)
            {
                SpriteRenderer background = CreateChildSprite("PanelBackground", TopPanelOrder, panelRoot);
                background.sprite = _panelSprite;
                background.color = Color.white;

                Vector2 natural = SpriteNaturalSize(_panelSprite);
                float scale = natural.x > 0f ? TopPanelWorldWidth / natural.x : 1f;
                height = natural.y * scale;
                background.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                height = 1.4f; // Placeholder height so the reserved area is still occupied.
            }

            panelRoot.transform.localPosition = new Vector3(0f, Camera.main.orthographicSize - TopPanelMargin - height * 0.5f, 0f);

            BuildGoalIcon(panelRoot);
            BuildCountText(panelRoot);
            return height;
        }

        private void BuildGoalIcon(GameObject panelRoot)
        {
            SpriteRenderer icon = CreateChildSprite("GoalIcon", TopPanelOverlayOrder, panelRoot);
            icon.transform.localPosition = new Vector3(-2.4f, 0f, 0f);
            if (_goalSprite != null)
            {
                icon.sprite = _goalSprite;
                icon.color = Color.white;
                SetSpriteSize(icon, new Vector2(GoalIconWorldSize, GoalIconWorldSize));
            }
            else
            {
                icon.sprite = WhiteSprite();
                icon.color = FallbackBackpackColor;
                icon.transform.localScale = new Vector3(GoalIconWorldSize, GoalIconWorldSize, 1f);
            }
        }

        private void BuildCountText(GameObject panelRoot)
        {
            int count = _board.CountBackpacks();

            var textObject = CreateChildObject("BackpackCount", panelRoot);
            textObject.transform.localPosition = new Vector3(-1.6f, 0f, 0f);

            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = count.ToString();
            textMesh.anchor = TextAnchor.MiddleLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = CountFontSize;
            textMesh.characterSize = CountCharacterSize;
            textMesh.color = Color.white;
            if (_font != null)
            {
                textMesh.font = _font;
            }

            MeshRenderer mesh = textObject.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.sortingOrder = TopPanelOverlayOrder;
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