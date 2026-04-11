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

        [SerializeField] GameObject hudCanvasPrefab;

        [Header("Sketch Style Assets")]
        [SerializeField] Sprite whitePanelSprite;
        [SerializeField] Sprite separatorLineSprite;
        [SerializeField] Sprite larvaIcon;
        [SerializeField] Sprite eggIcon;
        [SerializeField] Sprite crystalIcon;

        // ── Sketch Palette ──
        static readonly Color ColGold      = new(0.91f, 0.85f, 0.63f);
        static readonly Color ColWhite     = Color.white;
        static readonly Color ColActionBg  = new(0.95f, 0.95f, 0.95f, 0.90f);

        Text _calorieLabel;
        Text _selectionLabel;
        const string SelectionHint = "LMB select · RMB command";

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
                es.AddComponent<InputSystemUIInputModule>();
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
                    // If it's the one we named before, destroy it
                    if (c.name == "DemoHUD") DestroyImmediate(c.gameObject);
                }
            }

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

#if UNITY_EDITOR
            string p = "Assets/_InsectWars/Sprites/UI/Extracted/";
            if (whitePanelSprite == null) whitePanelSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (separatorLineSprite == null) separatorLineSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "bar_hp_xp.png");
            if (larvaIcon == null) larvaIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_larva.png");
            if (eggIcon == null) eggIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_egg.png");
            if (crystalIcon == null) crystalIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_crystal.png");
#endif

            // --- Top Center: Resource Bar ---
            var topBar = CreatePanel("TopBar", HudCanvasRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 60), whitePanelSprite);
            topBar.GetComponent<Image>().color = ColWhite;

            var resIcons = new GameObject("Icons").AddComponent<RectTransform>();
            resIcons.SetParent(topBar, false);
            resIcons.anchorMin = Vector2.zero; resIcons.anchorMax = Vector2.one; resIcons.offsetMin = new Vector2(40, 5); resIcons.offsetMax = new Vector2(-40, -5);
            var hl = resIcons.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 30; hl.childControlWidth = false;

            _calorieLabel = CreateResourceItem(resIcons, larvaIcon, "0");

            // --- Top Right: Menu Button ---
            var menuBtnGo = CreatePanel("MenuBtn", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(100, 50), whitePanelSprite);
            menuBtnGo.GetComponent<Image>().color = ColWhite;
            menuBtnGo.gameObject.AddComponent<Button>().onClick.AddListener(() => SceneLoader.LoadHome());
            CreateText("Label", menuBtnGo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "MENU", 16, ColGold).alignment = TextAnchor.MiddleCenter;

            // --- Bottom Left: Map ---
            MapPanel = CreatePanel("MapFrame", HudCanvasRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(240, 240), whitePanelSprite);
            MapPanel.GetComponent<Image>().color = ColWhite;

            // --- Bottom Center: Selection ---
            SelectionPanel = CreatePanel("SelectionPanel", HudCanvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(500, 160), whitePanelSprite);
            SelectionPanel.GetComponent<Image>().color = ColWhite;
            _selectionLabel = CreateText("Hint", SelectionPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20, 20), new Vector2(-20, -20), SelectionHint, 14, ColGold);
            _selectionLabel.alignment = TextAnchor.MiddleCenter;

            // --- Bottom Right: Actions ---
            ActionPanel = CreatePanel("ActionPanel", HudCanvasRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(300, 240), whitePanelSprite);
            ActionPanel.GetComponent<Image>().color = ColWhite;
        }

        Text CreateResourceItem(Transform parent, Sprite icon, string initialVal)
        {
            var go = new GameObject("ResItem");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 40);
            
            var img = new GameObject("Icon").AddComponent<Image>();
            img.transform.SetParent(rt, false);
            img.sprite = icon;
            img.rectTransform.anchorMin = new Vector2(0, 0.5f); img.rectTransform.anchorMax = new Vector2(0, 0.5f);
            img.rectTransform.pivot = new Vector2(0, 0.5f); img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = new Vector2(30, 30);
            img.color = ColGold;
            
            var val = CreateText("Val", rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(40, 0), Vector2.zero, initialVal, 16, ColGold);
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
            img.type = Image.Type.Sliced;
            img.color = ColWhite;
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
