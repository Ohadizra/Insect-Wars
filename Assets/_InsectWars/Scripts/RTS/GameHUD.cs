using InsectWars.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        [SerializeField] Sprite colonyCapacityIcon;

        // ── Organic Palette ──
        static readonly Color ColAmber     = new(0.96f, 0.90f, 0.78f); // Parchment/Amber
        static readonly Color ColSub       = new(0.83f, 0.69f, 0.44f); // Copper
        static readonly Color ColWhite     = Color.white;

        Text _calorieLabel;
        Text _ccLabel;
        Text _selectionLabel;
        const string SelectionHint = "LMB SELECT · RMB COMMAND";

        GameObject _settingsPanel;
        Text _pauseBtnLabel;
        public static bool SettingsPanelOpen { get; private set; }

        void Awake()
        {
            BuildHud();
        }

        void EnsureEventSystem()
        {
            var existingES = FindFirstObjectByType<EventSystem>();
            InputSystemUIInputModule mod;

            if (existingES != null)
            {
                mod = existingES.GetComponent<InputSystemUIInputModule>();
                if (mod != null) return;
                var legacy = existingES.GetComponent<BaseInputModule>();
                if (legacy != null) Destroy(legacy);
                mod = existingES.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                mod = es.AddComponent<InputSystemUIInputModule>();
            }

            var asset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            #if UNITY_EDITOR
            if (asset == null)
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            #endif
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
                    if (c.name == "DemoHUD") Destroy(c.gameObject);
                }
            }

            if (barMechanicalSprite == null) barMechanicalSprite = LoadSpriteFromResources("UI/Extracted/top_bar_frame");
            if (frameSquareSprite == null) frameSquareSprite = LoadSpriteFromResources("UI/Extracted/frame_square_panel");
            if (portraitFrameSprite == null) portraitFrameSprite = LoadSpriteFromResources("UI/Extracted/frame_portrait");
            if (buttonRoundSprite == null) buttonRoundSprite = LoadSpriteFromResources("UI/Extracted/btn_menu");
            if (larvaIcon == null) larvaIcon = LoadSpriteFromResources("UI/Extracted/icon_larva");
            if (eggIcon == null) eggIcon = LoadSpriteFromResources("UI/Extracted/icon_egg");
            if (crystalIcon == null) crystalIcon = LoadSpriteFromResources("UI/Extracted/icon_crystal");
            if (appleIcon == null) appleIcon = LoadSpriteFromResources("UI/icon_apple");
            if (colonyCapacityIcon == null) colonyCapacityIcon = LoadSpriteFromResources("UI/icon_colony_capacity");

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

            // --- Top Left: Resource Display (Icon + Text Only) ---
            var container = new GameObject("CalorieDisplay").AddComponent<RectTransform>();
            container.SetParent(HudCanvasRect, false);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0, 1);
            container.anchoredPosition = new Vector2(30, -25.5f);
            container.sizeDelta = new Vector2(170, 42.5f);
            
            _calorieLabel = CreateResourceItem(container, appleIcon != null ? appleIcon : larvaIcon, "0");

            // --- Colony Capacity display just below calories ---
            var ccContainer = new GameObject("CCDisplay").AddComponent<RectTransform>();
            ccContainer.SetParent(HudCanvasRect, false);
            ccContainer.anchorMin = ccContainer.anchorMax = ccContainer.pivot = new Vector2(0, 1);
            ccContainer.anchoredPosition = new Vector2(30, -70f);
            ccContainer.sizeDelta = new Vector2(170, 42.5f);

            Sprite ccIcon = colonyCapacityIcon != null ? colonyCapacityIcon : eggIcon;
            _ccLabel = CreateResourceItem(ccContainer, ccIcon, "0 / 0");

            // --- Top Right: Settings Button ---
            var menuBtnGo = CreatePanel("SettingsBtn", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -25.5f), new Vector2(68, 68), buttonRoundSprite);
            menuBtnGo.gameObject.AddComponent<Button>().onClick.AddListener(ToggleSettingsPanel);

            BuildSettingsPanel();
        }

        void BuildSettingsPanel()
        {
            // Full-screen dimmed backdrop that also catches clicks to close
            _settingsPanel = new GameObject("SettingsPanel");
            _settingsPanel.transform.SetParent(HudCanvasRect, false);
            var panelRt = _settingsPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var backdrop = _settingsPanel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.55f);
            var backdropBtn = _settingsPanel.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(CloseSettingsPanel);

            // Center card
            var card = new GameObject("Card");
            card.transform.SetParent(panelRt, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(320, 260);
            var cardImg = card.AddComponent<Image>();
            cardImg.sprite = frameSquareSprite;
            cardImg.color = new Color(0.08f, 0.1f, 0.06f, 0.95f);
            cardImg.type = Image.Type.Sliced;

            // Title
            var title = CreateText("Title", cardRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(10, -50), new Vector2(-10, -15), "SETTINGS", 28, ColAmber);
            title.alignment = TextAnchor.MiddleCenter;

            // Pause button
            var pauseBtn = CreateSettingsButton(cardRt, new Vector2(0, -90),
                PauseController.IsPaused ? "RESUME" : "PAUSE", () =>
            {
                PauseController.TogglePause();
                RefreshPauseLabel();
            });
            _pauseBtnLabel = pauseBtn.GetComponentInChildren<Text>();

            // Quit button
            CreateSettingsButton(cardRt, new Vector2(0, -160), "QUIT TO MENU", () =>
            {
                PauseController.ForceUnpause();
                SceneLoader.LoadHome();
            });

            _settingsPanel.SetActive(false);
            SettingsPanelOpen = false;
        }

        GameObject CreateSettingsButton(RectTransform parent, Vector2 offset, string label, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(label + "Btn");
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(250, 50);

            var img = btnGo.AddComponent<Image>();
            img.sprite = buttonRoundSprite;
            img.color = new Color(0.15f, 0.18f, 0.12f, 0.92f);
            img.type = Image.Type.Sliced;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.85f, 1f, 0.85f);
            colors.pressedColor = new Color(0.6f, 0.8f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(action);

            var txt = CreateText("Label", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, label, 22, ColAmber);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btnGo;
        }

        void ToggleSettingsPanel()
        {
            if (_settingsPanel == null) return;
            bool opening = !_settingsPanel.activeSelf;
            _settingsPanel.SetActive(opening);
            SettingsPanelOpen = opening;
            if (opening) RefreshPauseLabel();
        }

        void CloseSettingsPanel()
        {
            if (_settingsPanel == null) return;
            _settingsPanel.SetActive(false);
            SettingsPanelOpen = false;
        }

        void RefreshPauseLabel()
        {
            if (_pauseBtnLabel != null)
                _pauseBtnLabel.text = PauseController.IsPaused ? "RESUME" : "PAUSE";
        }

        Text CreateResourceItem(Transform parent, Sprite icon, string initialVal)
        {
            var go = new GameObject("ResItem");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            
            var img = new GameObject("Icon").AddComponent<Image>();
            img.transform.SetParent(rt, false);
            img.sprite = icon;
            img.rectTransform.anchorMin = new Vector2(0, 0.5f); img.rectTransform.anchorMax = new Vector2(0, 0.5f);
            img.rectTransform.pivot = new Vector2(0, 0.5f); img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = new Vector2(44, 44);
            img.preserveAspect = true;
            
            var val = CreateText("Val", rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(55, 0), Vector2.zero, initialVal, 22, ColAmber);
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
            ColonyCapacity.OnPlayerCCChanged += OnCCChanged;
            OnCCChanged();
        }

        void OnDestroy()
        {
            if (PlayerResources.Instance != null)
            {
                PlayerResources.Instance.OnCaloriesChanged -= OnCalories;
            }
            ColonyCapacity.OnPlayerCCChanged -= OnCCChanged;
            HudCanvasRect = null;
        }

        void OnCalories(int n)
        {
            if (_calorieLabel != null)
                _calorieLabel.text = $"{n:N0}";
        }

        void OnCCChanged()
        {
            if (_ccLabel == null) return;
            int used = ColonyCapacity.GetUsed(Team.Player);
            int cap = ColonyCapacity.GetCap(Team.Player);
            _ccLabel.text = $"{used} / {cap}";
            _ccLabel.color = used >= cap ? new Color(1f, 0.35f, 0.3f) : ColAmber;
        }

        void Update()
        {
            if (_selectionLabel != null && SelectionController.Instance != null)
            {
                var c = 0;
                foreach (var _ in SelectionController.Instance.SelectedPlayerUnits()) c++;
                _selectionLabel.text = c > 0 ? $"SELECTED: {c} UNITS" : SelectionHint;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && SettingsPanelOpen)
                CloseSettingsPanel();
        }

        internal static Sprite LoadSpriteFromResources(string path)
        {
            var s = Resources.Load<Sprite>(path);
            if (s != null) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return null;
        }
    }
}
