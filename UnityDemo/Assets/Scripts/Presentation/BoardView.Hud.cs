using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BagElimination.Presentation
{
    public sealed partial class BoardView
    {
        private Canvas _uiCanvas;
        private RectTransform _goalPanel, _goalIcon;
        private CanvasGroup _goalGroup;
        private Font _uiFont;
        private bool _chinese;
        private Sprite _roundedUi;
        private Sprite _buttonPanelSprite;
        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Text> _labels = new Dictionary<string, Text>();
        public event Action<string> HudClicked;
        private const float UiHeight = 76f;
        private static readonly Color Ink = new Color(.26f, .14f, .08f);

        private RectTransform UiRect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
        private Text UiText(string name, Transform parent, string value, int size)
        {
            var rect = UiRect(name, parent, new Vector2(.5f,.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 4); rect.offsetMax = new Vector2(-12,-4);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _uiFont; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter; text.color = Ink; text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
        private void SelectUiFont()
        {
            if (_uiFont != null) return;
            string[] installed = Font.GetOSInstalledFontNames();
            foreach (string candidate in new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC" })
                if (Array.IndexOf(installed, candidate) >= 0)
                { _uiFont = Font.CreateDynamicFontFromOSFont(candidate, 40); _chinese = true; break; }
            if (_uiFont == null)
            { _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); _chinese = false; Debug.LogWarning("Chinese UI font unavailable; using English labels."); }
        }
        public void BuildHud()
        {
            SelectUiFont(); _buttons.Clear(); _labels.Clear();
            var go = new GameObject("GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _uiCanvas = go.GetComponent<Canvas>(); _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1440); scaler.matchWidthOrHeight = .5f;
            if (EventSystem.current == null)
            {
                var events = new GameObject("UIEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            else if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
            {
                foreach (var module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            _goalPanel = UiRect("TopPanel", go.transform, new Vector2(.5f,.9f), new Vector2(400,108), Vector2.zero);
            _goalGroup = _goalPanel.gameObject.AddComponent<CanvasGroup>();
            var panel = _goalPanel.gameObject.AddComponent<Image>(); panel.sprite = _panelSprite; panel.preserveAspect = true; panel.raycastTarget = false;
            _goalIcon = UiRect("GoalIcon", _goalPanel, new Vector2(.5f,.5f), new Vector2(52,58), new Vector2(-35,0));
            var icon = _goalIcon.gameObject.AddComponent<Image>(); icon.sprite = _goalSprite; icon.preserveAspect = true; icon.raycastTarget = false;
            var count = UiRect("Count", _goalPanel, new Vector2(.5f,.5f), new Vector2(90,64), new Vector2(40,0));
            _countText = UiText("Number", count, _board.CountBackpacks().ToString(), 40);
            string[] ids = { "restart", "hint", "auto" };
            for (int i=0;i<ids.Length;i++)
            {
                string id = ids[i];
                var rect = UiRect(id,go.transform,new Vector2(.5f,.09f),new Vector2(200,UiHeight),new Vector2((i-1)*216,0));
                var background = rect.gameObject.AddComponent<Image>(); background.sprite = ButtonPanelSprite(); background.type = Image.Type.Sliced;
                background.color = Color.white;
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
                var colors = button.colors; colors.highlightedColor = new Color(1, .96f, .84f); colors.pressedColor = new Color(.78f,.66f,.5f); colors.disabledColor = new Color(.46f,.46f,.46f,.65f); colors.fadeDuration = .12f; button.colors = colors;
                button.onClick.AddListener(() => HudClicked?.Invoke(id));
                _buttons[id] = button; _labels[id] = UiText("Label",rect,"",27);
                var feedback = rect.gameObject.AddComponent<UiButtonFeedback>(); feedback.Button = button;
            }
            SetHudLabel("restart","Restart"); SetHudLabel("hint","Hint"); SetHudLabel("auto","Auto");
            LayoutUi(); Canvas.ForceUpdateCanvases();
        }
        public void ShowSettlementError(string reason)
        {
            var rect=UiRect("SettlementError",_uiCanvas.transform,new Vector2(.5f,.2f),new Vector2(600,100),Vector2.zero);
            var bg=rect.gameObject.AddComponent<Image>(); bg.sprite=RoundedUiSprite();bg.type=Image.Type.Sliced;
            bg.color=new Color(1,.9f,.78f,.97f);
            var label=UiText("Message",rect,_chinese ? "结算已停止，请重新开始\n详细原因已保存至日志" : "Settlement stopped. Please restart.\nDetails saved in the log.",24);
            label.resizeTextForBestFit=true; label.resizeTextMinSize=12;label.resizeTextMaxSize=24;
        }
        public void SetHudLabel(string id,string value)
        {
            if (!_labels.TryGetValue(id,out var label)) return;
            label.text = !_chinese ? value : id=="restart" ? "重新开始" : id=="hint" ? "提示" : value=="Stop" ? "停止演示" : "自动演示";
        }
        public void SetHudState(bool busy,bool victory,bool demo)
        {
            if (_buttons.Count==0)return;
            _buttons["hint"].interactable = !busy && !victory;
            _buttons["auto"].interactable = !victory && (!busy || demo);
        }
        public bool IsOverUi(Vector2 position)
        {
            if (EventSystem.current==null)return false;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},results);
            return results.Exists(r=>r.module is GraphicRaycaster);
        }
        private void LayoutUi()
        {
            if (_uiCanvas==null || _board==null)return;
            float w = (_board.Width-1)*CellSpacing+CellWorldSize+.5f;
            float h = (_board.Height-1)*CellSpacing+CellWorldSize+.5f;
            Camera.main.orthographicSize = Mathf.Max(h/.62f/2,w/(Camera.main.aspect*.9f)/2);
            var canvasRect=(RectTransform)_uiCanvas.transform;
            float bw=Mathf.Min(200,(canvasRect.rect.width-80)/3-16);
            float height=Mathf.Min(UiHeight,canvasRect.rect.height*.1f);
            string[] ids={"restart","hint","auto"};
            for(int i=0;i<3;i++)
            { var rect=(RectTransform)_buttons[ids[i]].transform; rect.sizeDelta=new Vector2(bw,height); rect.anchoredPosition=new Vector2((i-1)*(bw+16),0); _labels[ids[i]].fontSize=Mathf.RoundToInt(Mathf.Min(27,bw/5.2f)); }
            float ph=Mathf.Min(108,canvasRect.rect.height*.15f);
            float ps=ph/108;
            _goalPanel.localScale=Vector3.one*ps;
        }
        private Vector3 CollectionTargetWorld()
        {
            Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,_goalIcon.position);
            var world=Camera.main.ScreenToWorldPoint(new Vector3(screen.x,screen.y,-Camera.main.transform.position.z));
            world.z=0; return world;
        }
        private void SetGoalExit(float progress)
        { if(_goalGroup!=null){_goalGroup.alpha=1-progress; _goalPanel.anchoredPosition=Vector2.up*progress*80;} }
        private Sprite ButtonPanelSprite()
        {
            if (_buttonPanelSprite != null) return _buttonPanelSprite;
            if (_panelSprite == null) return RoundedUiSprite();
            Rect rect = _panelSprite.rect;
            float border = Mathf.Min(rect.width, rect.height) * .3f;
            _buttonPanelSprite = Sprite.Create(_panelSprite.texture, rect, new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));
            return _buttonPanelSprite;
        }
        private Sprite RoundedUiSprite()
        {
            if(_roundedUi!=null)return _roundedUi;
            const int size=64;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float dx=Mathf.Max(Mathf.Abs(x-31.5f)-19.5f,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-19.5f,0);float alpha=Mathf.Clamp01(12-Mathf.Sqrt(dx*dx+dy*dy));texture.SetPixel(x,y,new Color(1,1,1,alpha));}
            texture.Apply(); _roundedUi=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));return _roundedUi;
        }
    }
    public sealed class UiButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Button Button;
        private bool _pressed;
        public void OnPointerDown(PointerEventData e){_pressed=Button.interactable;}
        public void OnPointerUp(PointerEventData e){_pressed=false;}
        public void OnPointerExit(PointerEventData e){_pressed=false;}
        private void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(_pressed && Button.interactable ? .96f : 1),Time.unscaledDeltaTime*22);}
        private void OnDisable(){_pressed=false;transform.localScale=Vector3.one;}
    }
}
