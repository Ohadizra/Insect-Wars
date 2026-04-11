using InsectWars.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public class GameHUD : MonoBehaviour
    {
        public static RectTransform HudCanvasRect { get; private set; }
        public static RectTransform MapPanel { get; private set; }
        public static RectTransform SelectionPanel { get; private set; }
        public static RectTransform ActionPanel { get; private set; }

        [Header("Natural Ornate Assets")]
        [SerializeField] Sprite barMechanicalSprite;
        [SerializeField] Sprite frameSquareSprite;
        [SerializeField] Sprite portraitFrameSprite;
        [SerializeField] Sprite buttonRoundSprite;
        [SerializeField] Sprite larvaIcon;
        [SerializeField] Sprite eggIcon;
        [SerializeField] Sprite crystalIcon;
        [SerializeField] Sprite appleIcon;

        // ── Organic Palette ──
        static readonly Color ColAmber     = new(0.96f, 0.90f, 0.78f); // Parchment/Amber
        static readonly Color ColSub       = new(0.83f, 0.69f, 0.44f); // Copper
        static readonly Color ColWhite     = Color.white;

        Text _calorieLabel;
        Text _selectionLabel;
        const string SelectionHint = "LMB SELECT · RMB COMMAND";

        void Awake()
        {
            BuildHud();
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                var mod = es.AddComponent<InputSystemUIInputModule>();
                
                // Manual Link Actions for responsiveness
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                if (asset != null)
                {
                    mod.actionsAsset = asset;
                    var uiMap = asset.FindActionMap("UI");
                    if (uiMap != null)
                    {
                        mod.point = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("Point"));
                        mod.leftClick = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("Click"));
                        mod.rightClick = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("RightClick"));
                        mod.middleClick = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("MiddleClick"));
                        mod.scrollWheel = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("ScrollWheel"));
                        mod.move = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("Navigate"));
                        mod.submit = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("Submit"));
                        mod.cancel = UnityEngine.InputSystem.InputActionReference.Create(uiMap.FindAction("Cancel"));
                    }
                }
            }
        }

        void BuildHud()
        {
            EnsureEventSystem();

            // Clear ALL existing Canvases to ensure replacement
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name.Contains("HUD") || c.name.Contains("Canvas") || c.name.Contains("UI"))
                {
                    c.gameObject.SetActive(false);
                    if (c.name == "DemoHUD") DestroyImmediate(c.gameObject);
                }
            }

        #if UNITY_EDITOR
            string p = "Assets/_InsectWars/Sprites/UI/Extracted/";
            if (barMechanicalSprite == null) barMechanicalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "top_bar_frame.png");
            if (frameSquareSprite == null) frameSquareSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            if (portraitFrameSprite == null) portraitFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_portrait.png");
            if (buttonRoundSprite == null) buttonRoundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_menu.png");
            if (larvaIcon == null) larvaIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_larva.png");
            if (eggIcon == null) eggIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_egg.png");
            if (crystalIcon == null) crystalIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_crystal.png");
            if (appleIcon == null) appleIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/icon_apple.png");
        #endif

            var canvasGo = new GameObject("DemoHUD");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            HudCanvasRect = canvasGo.GetComponent<RectTransform>();

            // --- Top Center: Small Resource Bar ---
            var topBar = CreatePanel("TopBar", HudCanvasRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(220, 60), barMechanicalSprite);
            
            var resIcons = new GameObject("Icons").AddComponent<RectTransform>();
            resIcons.SetParent(topBar, false);
            resIcons.anchorMin = Vector2.zero; resIcons.anchorMax = Vector2.one; 
            resIcons.offsetMin = new Vector2(20, 5); resIcons.offsetMax = new Vector2(-20, -5);
            var hl = resIcons.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 15; hl.childControlWidth = false;

            _calorieLabel = CreateResourceItem(resIcons, appleIcon != null ? appleIcon : larvaIcon, "0");


            // --- Top Right: Menu Button ---
            var menuBtnGo = CreatePanel("MenuBtn", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(80, 80), buttonRoundSprite);
            menuBtnGo.gameObject.AddComponent<Button>().onClick.AddListener(() => SceneLoader.LoadHome());

            // --- Bottom Left: Map ---
            // Removed MapFrame, SelectionPanel, ActionPanel as they are handled by BottomBar

            // MapPanel = CreatePanel("MapFrame", HudCanvasRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(300, 300), frameSquareSprite);

            // --- Bottom Center: Selection ---
            // SelectionPanel = CreatePanel("SelectionPanel", HudCanvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(600, 200), frameSquareSprite);
            // _selectionLabel = CreateText("Hint", SelectionPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(40, 40), new Vector2(-40, -40), SelectionHint, 18, ColAmber);
            // _selectionLabel.alignment = TextAnchor.MiddleCenter;

            // --- Bottom Right: Actions ---
            // ActionPanel = CreatePanel("ActionPanel", HudCanvasRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(360, 300), frameSquareSprite);
            }

        Text CreateResourceItem(Transform parent, Sprite icon, string initialVal)
        {
            var go = new GameObject("ResItem");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 50);
            
            var img = new GameObject("Icon").AddComponent<Image>();
            img.transform.SetParent(rt, false);
            img.sprite = icon;
            img.rectTransform.anchorMin = new Vector2(0, 0.5f); img.rectTransform.anchorMax = new Vector2(0, 0.5f);
            img.rectTransform.pivot = new Vector2(0, 0.5f); img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = new Vector2(40, 40);
            img.preserveAspect = true;
            
            var val = CreateText("Val", rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(50, 0), Vector2.zero, initialVal, 20, ColAmber);
            val.alignment = TextAnchor.MiddleLeft;
            return val;
        }

        RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = ColWhite;
            img.type = Image.Type.Sliced;
            return rt;
        }

        Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, string content, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFontHelper.GetFont();
            t.fontSize = size;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        void Start()
        {
            if (PlayerResources.Instance != null)
            {
                PlayerResources.Instance.OnCaloriesChanged += OnCalories;
                OnCalories(PlayerResources.Instance.Calories);
            }
        }

        void OnDestroy()
        {
            if (PlayerResources.Instance != null)
            {
                PlayerResources.Instance.OnCaloriesChanged -= OnCalories;
            }
            HudCanvasRect = null;
        }

        void OnCalories(int n)
        {
            if (_calorieLabel != null)
                _calorieLabel.text = $"{n:N0}";
        }

        void Update()
        {
            if (_selectionLabel != null && SelectionController.Instance != null)
            {
                var c = 0;
                foreach (var _ in SelectionController.Instance.SelectedPlayerUnits()) c++;
                _selectionLabel.text = c > 0 ? $"SELECTED: {c} UNITS" : SelectionHint;
            }
        }
    }
}
