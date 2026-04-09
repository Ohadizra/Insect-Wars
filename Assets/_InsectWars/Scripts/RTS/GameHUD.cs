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

        [Header("Insectoid UI Skin")]
        [SerializeField] Sprite barSprite;
        [SerializeField] Sprite frameSprite;
        [SerializeField] Sprite actionGridSprite;
        [SerializeField] Sprite buttonSprite;

        Text _calorieLabel;
        Text _selectionLabel;
        const string SelectionHint =
            "LMB select · RMB move/attack/gather · M/S/H/P/A commands (see bottom bar)";

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
            // Auto-wire the new Chitin art if null
            if (barSprite == null) barSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Bar_Chitin_Horizontal.png");
            if (frameSprite == null) frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Frame_Chitin_Square.png");
            if (actionGridSprite == null) actionGridSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_ActionPanel_Chitin.png");
            if (buttonSprite == null) buttonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Button_Chitin.png");
#endif

            // --- Top Left: Resources ---
            var resourceContainer = new GameObject("Resources").AddComponent<RectTransform>();
            resourceContainer.SetParent(HudCanvasRect, false);
            resourceContainer.anchorMin = resourceContainer.anchorMax = resourceContainer.pivot = new Vector2(0, 1);
            resourceContainer.anchoredPosition = new Vector2(30, -30);
            resourceContainer.sizeDelta = new Vector2(360, 50);

            var calPanel = CreatePanel("CaloriePanel", resourceContainer, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, barSprite);
            _calorieLabel = CreateText("Text", calPanel.transform, Vector2.zero, Vector2.one, new Vector2(15, 0), new Vector2(-15, 0), new Vector2(0.5f, 0.5f), "Calories: 0", 18, Color.white);
            _calorieLabel.alignment = TextAnchor.MiddleCenter;

            // --- Top Right: Menu ---
            var menuPanel = CreatePanel("MenuButton", HudCanvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(140, 50), buttonSprite);
            var menuBtn = menuPanel.gameObject.AddComponent<Button>();
            menuBtn.onClick.AddListener(() => SceneLoader.LoadHome());
            CreateText("Label", menuPanel.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "MENU", 18, Color.white).alignment = TextAnchor.MiddleCenter;

            // --- Mid Left: Assistant ---
            var assistantFrame = CreatePanel("AssistantFrame", HudCanvasRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 80), new Vector2(100, 100), frameSprite);
            CreateText("Label", assistantFrame.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(100, 20), "ASSISTANT", 10, new Color(0.7f, 0.7f, 0.7f)).alignment = TextAnchor.MiddleCenter;

            // --- Bottom Left: Map ---
            MapPanel = CreatePanel("MapFrame", HudCanvasRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(280, 280), frameSprite);

            // --- Bottom Center: Selection ---
            SelectionPanel = CreatePanel("SelectionPanel", HudCanvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(640, 140), frameSprite);
            _selectionLabel = CreateText("SelectionText", SelectionPanel.transform, Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-30, -30), new Vector2(0.5f, 0.5f), SelectionHint, 16, Color.white);
            _selectionLabel.alignment = TextAnchor.MiddleCenter;

            // --- Bottom Right: Actions ---
            ActionPanel = CreatePanel("ActionPanel", HudCanvasRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(360, 260), actionGridSprite);
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
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
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
