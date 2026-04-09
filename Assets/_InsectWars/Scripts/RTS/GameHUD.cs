using InsectWars.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public class GameHUD : MonoBehaviour
    {
        /// <summary>Runtime HUD canvas (minimap and other widgets parent here).</summary>
        public static RectTransform HudCanvasRect { get; private set; }

        [SerializeField] GameObject hudCanvasPrefab;

        [Header("Insectoid UI Skin")]
        [SerializeField] Sprite barSprite;
        [SerializeField] Sprite frameSprite;
        [SerializeField] Sprite actionGridSprite;
        [SerializeField] Sprite buttonSprite;

        Text _calorieLabel;
        Text _seedLabel;
        Text _selectionLabel;
        const string SelectionHint =
            "LMB select · RMB move/attack/gather · M/S/H/P/A commands (see bottom bar)";

        void Awake()
        {
            // Load default insectoid sprites if not assigned
            if (barSprite == null) barSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Bar_Slim_Insect.png");
            if (frameSprite == null) frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Frame_Slim_Insect.png");
            if (actionGridSprite == null) actionGridSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_ActionPanel_Slim_Insect.png");
            if (buttonSprite == null) buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Button_Slim_Insect.png");

            if (hudCanvasPrefab != null)
            {
                var root = Instantiate(hudCanvasPrefab, transform);
                var bind = root.GetComponentInChildren<DemoHudBindings>(true);
                if (bind != null && bind.CalorieText != null && bind.SelectionText != null)
                {
                    EnsureEventSystem();
                    _calorieLabel = bind.CalorieText;
                    _selectionLabel = bind.SelectionText;
                    HudCanvasRect = root.GetComponent<RectTransform>();
                    return;
                }
                Destroy(root);
            }
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

            // --- Top Left: Resources ---
            var resourcePanel = CreatePanel("Resources", HudCanvasRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(300, 100), barSprite);
            
            _calorieLabel = CreateText("CalorieText", resourcePanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(40, -10), new Vector2(-10, -45), "Calories: 0", 22, Color.white);
            _calorieLabel.alignment = TextAnchor.MiddleLeft;

            _seedLabel = CreateText("SeedText", resourcePanel.transform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(40, 10), new Vector2(-10, 45), "Seeds: 0", 20, new Color(0.8f, 1f, 0.7f));
            _seedLabel.alignment = TextAnchor.MiddleLeft;

            // --- Top Right: Menu ---
            var menuPanel = CreatePanel("MenuPanel", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(150, 50), buttonSprite);
            var menuBtn = menuPanel.gameObject.AddComponent<Button>();
            menuBtn.onClick.AddListener(() => SceneLoader.LoadHome());
            CreateText("MenuLabel", menuPanel.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "MENU", 18, Color.white).alignment = TextAnchor.MiddleCenter;

            // --- Mid Left: Assistant ---
            var assistantFrame = CreatePanel("AssistantFrame", HudCanvasRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 100), new Vector2(128, 128), frameSprite);
            CreateText("AssistantLabel", assistantFrame.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(120, 20), "ASSISTANT", 12, Color.gray).alignment = TextAnchor.MiddleCenter;

            // --- Bottom Left: Map ---
            var mapFrame = CreatePanel("MapFrame", HudCanvasRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(256, 256), frameSprite);
            CreateText("MapLabel", mapFrame.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(100, 20), "MAP", 14, Color.white).alignment = TextAnchor.MiddleCenter;

            // --- Bottom Center: Selection ---
            var selectionPanel = CreatePanel("SelectionPanel", HudCanvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(600, 160), barSprite);
            _selectionLabel = CreateText("SelectionText", selectionPanel.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20, 20), new Vector2(-20, -20), SelectionHint, 16, Color.white);
            _selectionLabel.alignment = TextAnchor.MiddleCenter;

            // --- Bottom Right: Actions ---
            var actionPanel = CreatePanel("ActionPanel", HudCanvasRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 16), new Vector2(320, 220), actionGridSprite);
            CreateText("ActionLabel", actionPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 5), new Vector2(100, 20), "ACTIONS", 14, Color.white).alignment = TextAnchor.MiddleCenter;
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
            img.color = Color.white;
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
                PlayerResources.Instance.OnCactiSeedsChanged += OnSeeds;
                OnCalories(PlayerResources.Instance.Calories);
                OnSeeds(PlayerResources.Instance.CactiSeeds);
            }
        }

        void OnDestroy()
        {
            if (PlayerResources.Instance != null)
            {
                PlayerResources.Instance.OnCaloriesChanged -= OnCalories;
                PlayerResources.Instance.OnCactiSeedsChanged -= OnSeeds;
            }
            HudCanvasRect = null;
        }

        void OnCalories(int n)
        {
            if (_calorieLabel != null)
                _calorieLabel.text = $"Calories: {n:N0}";
        }

        void OnSeeds(int n)
        {
            if (_seedLabel != null)
                _seedLabel.text = $"Cacti Seeds: {n:N0}";
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
