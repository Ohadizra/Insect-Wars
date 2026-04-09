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
        /// <summary>Runtime HUD canvas (minimap and other widgets parent here).</summary>
        public static RectTransform HudCanvasRect { get; private set; }

        [SerializeField] GameObject hudCanvasPrefab;

        Text _calorieLabel;
        Text _seedLabel;
        Text _selectionLabel;
        const string SelectionHint =
            "LMB select · RMB move/attack/gather · M/S/H/P/A commands (see bottom bar)";

        void Awake()
        {
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

            var demo = new GameObject("DemoTag").AddComponent<Text>();
            demo.transform.SetParent(canvas.transform, false);
            demo.font = UiFontHelper.GetFont();
            demo.fontSize = 18;
            demo.color = new Color(1f, 0.92f, 0.4f);
            var rt = demo.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(16, -12);
            rt.sizeDelta = new Vector2(400, 32);
            demo.text = "INSECT WARS — DEMO 0";

            var barGo = new GameObject("CalorieBar");
            barGo.transform.SetParent(canvas.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(0, 1);
            barRt.pivot = new Vector2(0, 1);
            barRt.anchoredPosition = new Vector2(12, -46);
            barRt.sizeDelta = new Vector2(270, 44);
            var barBg = barGo.AddComponent<Image>();
            barBg.color = new Color(0.08f, 0.06f, 0.02f, 0.88f);
            barBg.raycastTarget = false;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(barGo.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(10, 0);
            iconRt.sizeDelta = new Vector2(26, 26);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.85f, 0.68f, 0.15f);
            iconImg.raycastTarget = false;

            _calorieLabel = new GameObject("CalorieText").AddComponent<Text>();
            _calorieLabel.transform.SetParent(barGo.transform, false);
            _calorieLabel.font = UiFontHelper.GetFont();
            _calorieLabel.fontSize = 22;
            _calorieLabel.color = new Color(1f, 0.95f, 0.7f);
            var nrt = _calorieLabel.rectTransform;
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(44, 0);
            nrt.offsetMax = new Vector2(-8, 0);
            _calorieLabel.alignment = TextAnchor.MiddleLeft;
            _calorieLabel.text = "Calories: 0";

            var seedBarGo = new GameObject("SeedBar");
            seedBarGo.transform.SetParent(canvas.transform, false);
            var seedBarRt = seedBarGo.AddComponent<RectTransform>();
            seedBarRt.anchorMin = new Vector2(0, 1);
            seedBarRt.anchorMax = new Vector2(0, 1);
            seedBarRt.pivot = new Vector2(0, 1);
            seedBarRt.anchoredPosition = new Vector2(12, -94);
            seedBarRt.sizeDelta = new Vector2(270, 38);
            var seedBarBg = seedBarGo.AddComponent<Image>();
            seedBarBg.color = new Color(0.08f, 0.06f, 0.02f, 0.88f);
            seedBarBg.raycastTarget = false;

            var seedIconGo = new GameObject("SeedIcon");
            seedIconGo.transform.SetParent(seedBarGo.transform, false);
            var seedIconRt = seedIconGo.AddComponent<RectTransform>();
            seedIconRt.anchorMin = new Vector2(0, 0.5f);
            seedIconRt.anchorMax = new Vector2(0, 0.5f);
            seedIconRt.pivot = new Vector2(0, 0.5f);
            seedIconRt.anchoredPosition = new Vector2(10, 0);
            seedIconRt.sizeDelta = new Vector2(22, 22);
            var seedIconImg = seedIconGo.AddComponent<Image>();
            seedIconImg.color = new Color(0.45f, 0.65f, 0.25f);
            seedIconImg.raycastTarget = false;

            _seedLabel = new GameObject("SeedText").AddComponent<Text>();
            _seedLabel.transform.SetParent(seedBarGo.transform, false);
            _seedLabel.font = UiFontHelper.GetFont();
            _seedLabel.fontSize = 20;
            _seedLabel.color = new Color(0.8f, 1f, 0.7f);
            var seedRt = _seedLabel.rectTransform;
            seedRt.anchorMin = new Vector2(0, 0);
            seedRt.anchorMax = new Vector2(1, 1);
            seedRt.offsetMin = new Vector2(40, 0);
            seedRt.offsetMax = new Vector2(-8, 0);
            _seedLabel.alignment = TextAnchor.MiddleLeft;
            _seedLabel.text = "Cacti Seeds: 0";

            _selectionLabel = new GameObject("Selection").AddComponent<Text>();
            _selectionLabel.transform.SetParent(canvas.transform, false);
            _selectionLabel.font = UiFontHelper.GetFont();
            _selectionLabel.fontSize = 16;
            _selectionLabel.color = new Color(0.9f, 0.9f, 0.9f);
            var srt = _selectionLabel.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 0);
            srt.anchorMax = new Vector2(0.5f, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.anchoredPosition = new Vector2(0, 228);
            srt.sizeDelta = new Vector2(720, 56);
            _selectionLabel.alignment = TextAnchor.MiddleCenter;
            _selectionLabel.text = SelectionHint;

            var btnGo = new GameObject("Menu");
            btnGo.transform.SetParent(canvas.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 1);
            btnRt.anchorMax = new Vector2(1, 1);
            btnRt.pivot = new Vector2(1, 1);
            btnRt.anchoredPosition = new Vector2(-16, -16);
            btnRt.sizeDelta = new Vector2(140, 40);
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.15f, 0.12f, 0.2f, 0.92f);
            var btn = btnGo.AddComponent<Button>();
            var bt = new GameObject("Label").AddComponent<Text>();
            bt.transform.SetParent(btnGo.transform, false);
            bt.font = UiFontHelper.GetFont();
            bt.fontSize = 18;
            bt.color = Color.white;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.text = "Main Menu";
            var btr = bt.rectTransform;
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = Vector2.zero;
            btr.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() => SceneLoader.LoadHome());
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
