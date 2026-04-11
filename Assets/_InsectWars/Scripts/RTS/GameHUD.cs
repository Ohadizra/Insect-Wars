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

        [Header("Insectoid UI Skin (Sketch Matching)")]
        [SerializeField] Sprite topBarSprite;
        [SerializeField] Sprite menuBtnSprite;
        [SerializeField] Sprite secondaryBtnSprite;
        [SerializeField] Sprite portraitFrameSprite;
        [SerializeField] Sprite mapFrameSprite;
        [SerializeField] Sprite selectionFrameSprite;
        [SerializeField] Sprite actionFrameSprite;
        [SerializeField] Sprite barBgSprite;
        [SerializeField] Sprite larvaIcon;
        [SerializeField] Sprite eggIcon;
        [SerializeField] Sprite crystalIcon;
        [SerializeField] Sprite portraitSprite;

        [Header("Action Icons")]
        [SerializeField] Sprite iconBranching;
        [SerializeField] Sprite iconBeetle;
        [SerializeField] Sprite iconHammerWrench;
        [SerializeField] Sprite iconRunning;

        Text _calorieLabel, _eggLabel, _crystalLabel;
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
            if (topBarSprite == null) topBarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "top_bar_frame.png");
            if (menuBtnSprite == null) menuBtnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_menu.png");
            if (secondaryBtnSprite == null) secondaryBtnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_secondary.png");
            if (portraitFrameSprite == null) portraitFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_portrait.png");
            if (mapFrameSprite == null) mapFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_ornate.png");
            if (selectionFrameSprite == null) selectionFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            if (actionFrameSprite == null) actionFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_action_grid.png");
            if (barBgSprite == null) barBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "bar_hp_xp.png");
            if (larvaIcon == null) larvaIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_larva.png");
            if (eggIcon == null) eggIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_egg.png");
            if (crystalIcon == null) crystalIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_crystal.png");
            if (portraitSprite == null) portraitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "portrait_stag_beetle.png");
            if (iconBranching == null) iconBranching = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_branching_arrows.png");
            if (iconBeetle == null) iconBeetle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_beetle_action.png");
            if (iconHammerWrench == null) iconHammerWrench = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_hammer_wrench.png");
            if (iconRunning == null) iconRunning = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_running_person.png");
        #endif

            // --- Top Center: Resource Bar ---
            var topBar = CreatePanel("TopBar", HudCanvasRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 80), topBarSprite);
            topBar.GetComponent<Image>().color = Color.white;

            var resContainer = new GameObject("Icons").AddComponent<RectTransform>();
            resContainer.SetParent(topBar, false);
            resContainer.anchorMin = Vector2.zero; resContainer.anchorMax = Vector2.one; resContainer.offsetMin = new Vector2(80, 10); resContainer.offsetMax = new Vector2(-80, -10);
            var hl = resContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 20; hl.childControlWidth = false;

            _calorieLabel = CreateResourceItem(resContainer, larvaIcon, "15");
            _eggLabel = CreateResourceItem(resContainer, eggIcon, "250");
            _crystalLabel = CreateResourceItem(resContainer, crystalIcon, "75");

            // --- Top Right: Menu Buttons ---
            var menuBtn = CreatePanel("MenuBtn", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(70, 70), menuBtnSprite);
            menuBtn.gameObject.AddComponent<Button>().onClick.AddListener(() => SceneLoader.LoadHome());
            menuBtn.GetComponent<Image>().color = Color.white;

            var secondaryBtn = CreatePanel("SecondaryBtn", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -100), new Vector2(70, 50), secondaryBtnSprite);
            secondaryBtn.GetComponent<Image>().color = Color.white;

            // --- Mid Left: Portrait ---
            var portraitPanel = CreatePanel("PortraitPanel", HudCanvasRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(240, 320), portraitFrameSprite);
            portraitPanel.GetComponent<Image>().color = Color.white;
            
            var bugImg = new GameObject("Bug").AddComponent<Image>();
            bugImg.transform.SetParent(portraitPanel, false);
            bugImg.sprite = portraitSprite;
            bugImg.rectTransform.anchorMin = new Vector2(0.1f, 0.35f); bugImg.rectTransform.anchorMax = new Vector2(0.9f, 0.9f); bugImg.rectTransform.offsetMin = bugImg.rectTransform.offsetMax = Vector2.zero;
            
            var hpBar = CreatePanel("HPBar", portraitPanel, new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 12), barBgSprite);
            hpBar.GetComponent<Image>().color = new Color(1, 0.3f, 0.3f, 0.8f);
            var xpBar = CreatePanel("XPBar", portraitPanel, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 10), barBgSprite);
            xpBar.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1, 0.8f);

            // --- Bottom Left: Map ---
            MapPanel = CreatePanel("MapFrame", HudCanvasRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(260, 260), mapFrameSprite);
            MapPanel.GetComponent<Image>().color = Color.white;

            // --- Bottom Center: Selection ---
            SelectionPanel = CreatePanel("SelectionPanel", HudCanvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(400, 240), selectionFrameSprite);
            SelectionPanel.GetComponent<Image>().color = Color.white;
            _selectionLabel = CreateText("Hint", SelectionPanel.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20, 20), new Vector2(-20, -20), SelectionHint, 14, Color.white);
            _selectionLabel.alignment = TextAnchor.MiddleCenter;

            // --- Bottom Right: Actions ---
            ActionPanel = CreatePanel("ActionPanel", HudCanvasRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(320, 240), actionFrameSprite);
            ActionPanel.GetComponent<Image>().color = Color.white;
            
            var grid = new GameObject("Grid").AddComponent<RectTransform>();
            grid.SetParent(ActionPanel, false);
            grid.anchorMin = new Vector2(0.1f, 0.1f); grid.anchorMax = new Vector2(0.9f, 0.9f); grid.offsetMin = grid.offsetMax = Vector2.zero;
            var glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(110, 85); glg.spacing = new Vector2(10, 10); glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; glg.constraintCount = 2;
            
            CreateActionBtn(grid, iconBranching);
            CreateActionBtn(grid, iconBeetle);
            CreateActionBtn(grid, iconHammerWrench);
            CreateActionBtn(grid, iconRunning);
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
            img.rectTransform.sizeDelta = new Vector2(35, 35);
            
            var val = CreateText("Val", rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(40, 0), Vector2.zero, initialVal, 16, Color.white);
            val.alignment = TextAnchor.MiddleLeft;
            return val;
        }

        void CreateActionBtn(Transform parent, Sprite icon)
        {
            var btn = new GameObject("ActionBtn").AddComponent<Image>();
            btn.transform.SetParent(parent, false);
            btn.sprite = icon;
            btn.preserveAspect = true;
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
            img.type = Image.Type.Simple;
            img.color = new Color(1, 1, 1, 0.8f);
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

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(1, -1);

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
                _calorieLabel.text = $"Calories: {n:N0}";
        }

        void Update()
        {
            if (_selectionLabel != null && SelectionController.Instance != null)
            {
                var c = 0;
                foreach (var _ in SelectionController.Instance.SelectedPlayerUnits()) c++;
                _selectionLabel.text = c > 0 ? $"Selected: {c} unit(s)" : SelectionHint;
            }
        }
    }
}
