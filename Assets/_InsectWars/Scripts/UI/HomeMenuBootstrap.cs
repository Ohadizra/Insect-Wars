using System.Text;
using InsectWars.Core;
using InsectWars.Data;
using InsectWars.RTS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsectWars.UI
{
    public class HomeMenuBootstrap : MonoBehaviour
    {
        [SerializeField] string streamingVideoName = "MenuLoop.mp4";
        [SerializeField] UnitVisualLibrary visualLibrary;

        [Header("Menu UI Art")]
        [SerializeField] Sprite titleFrame;
        [SerializeField] Sprite panelFrame;
        [SerializeField] Sprite buttonSprite;
        [SerializeField] Sprite mapCardFrame;

        // ── Palette constants ──
        static readonly Color ColTitleGold    = new(1f, 0.92f, 0.45f);
        static readonly Color ColSubtitle     = new(0.7f, 0.85f, 0.65f);
        static readonly Color ColPanelBg      = new(0.04f, 0.03f, 0.08f, 0.94f);
        static readonly Color ColBtnNormal    = new(0.14f, 0.28f, 0.16f, 0.95f);
        static readonly Color ColBtnHighlight = new(0.22f, 0.42f, 0.24f, 1f);
        static readonly Color ColBtnPressed   = new(0.30f, 0.52f, 0.30f, 1f);
        static readonly Color ColBtnText      = new(0.88f, 1f, 0.88f);
        static readonly Color ColBodyText     = new(0.90f, 0.92f, 0.88f);
        static readonly Color ColLabel        = new(0.7f, 0.8f, 0.7f);
        static readonly Color ColDimOverlay   = new(0f, 0f, 0f, 0.55f);
        static readonly Color ColFallbackBg   = new(0.06f, 0.08f, 0.04f);
        static readonly Color ColMapCardBg    = new(0.10f, 0.20f, 0.12f, 0.95f);
        static readonly Color ColMapCardHover = new(0.18f, 0.36f, 0.20f);

        const float BtnWidth  = 420f;
        const float BtnHeight = 62f;
        const float BtnGap    = 72f;
        const int   BtnFont   = 26;
        const int   TitleFont = 68;

        Canvas _canvas;
        Font _font;
        GameObject _panelMain;
        GameObject _panelPlay;
        GameObject _panelMapSelect;
        GameObject _panelHow;
        GameObject _panelSettings;
        GameObject _panelAbout;
        Text _volValueLabel;
        Text _diffLabelInMapSelect;
        Toggle _fullToggle;

        void Awake()
        {
            GameSession.LoadPrefs();
            AudioListener.volume = GameSession.GetSavedMasterVolume();
            Screen.fullScreen = GameSession.GetSavedFullscreen();

            _font = UiFontHelper.GetFont();
            AutoWireSprites();
            SetupEventSystem();
            BuildCanvas();
            BuildVideoBackground();
            BuildMainMenu();
            BuildSubPanels();
            ShowMain();
        }

        void AutoWireSprites()
        {
        #if UNITY_EDITOR
            if (titleFrame == null) titleFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Frame_Vines_Wide 3.png");
            if (panelFrame == null) panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_ActionPanel_Vines 3.png");
            if (buttonSprite == null) buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Button_Vines 3.png");
            if (mapCardFrame == null) mapCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/UI_Frame_Vines_Square 3.png");
        #endif
        }

        void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        void BuildCanvas()
        {
            var go = new GameObject("MainMenuCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Background
        // ═══════════════════════════════════════════════════════════════

        void BuildVideoBackground()
        {
            var bg = new GameObject("VideoBackground");
            bg.transform.SetParent(_canvas.transform, false);
            Stretch(bg.AddComponent<RectTransform>());

            var raw = bg.AddComponent<RawImage>();
            raw.color = Color.white;

            var vgo = new GameObject("VideoPlayer");
            vgo.transform.SetParent(bg.transform, false);
            var vp = vgo.AddComponent<VideoPlayer>();
            vp.playOnAwake = true;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            var vrt = new RenderTexture(1920, 1080, 0);
            vp.targetTexture = vrt;
            raw.texture = vrt;

            var path = System.IO.Path.Combine(Application.streamingAssetsPath, streamingVideoName);
            if (System.IO.File.Exists(path))
            {
                vp.url = Application.platform == RuntimePlatform.OSXPlayer ||
                         Application.platform == RuntimePlatform.OSXEditor
                    ? "file://" + path
                    : path;
                vp.Play();
            }
            else
            {
                raw.color = ColFallbackBg;
                vp.enabled = false;
                BuildFallbackGradient(bg.transform);
            }

            var dim = new GameObject("Dim");
            dim.transform.SetParent(bg.transform, false);
            Stretch(dim.AddComponent<RectTransform>());
            dim.AddComponent<Image>().color = ColDimOverlay;
        }

        void BuildFallbackGradient(Transform parent)
        {
            var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, new Color(0.02f, 0.04f, 0.02f));
            tex.SetPixel(0, 1, new Color(0.06f, 0.10f, 0.05f));
            tex.SetPixel(0, 2, new Color(0.08f, 0.14f, 0.06f));
            tex.SetPixel(0, 3, new Color(0.04f, 0.07f, 0.03f));
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var grad = new GameObject("Gradient");
            grad.transform.SetParent(parent, false);
            Stretch(grad.AddComponent<RectTransform>());
            var ri = grad.AddComponent<RawImage>();
            ri.texture = tex;
            ri.uvRect = new Rect(0, 0, 1, 1);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Main Menu
        // ═══════════════════════════════════════════════════════════════

        void BuildMainMenu()
        {
            _panelMain = MakePanel("MainPanel");

            var center = new GameObject("CenterGroup");
            center.transform.SetParent(_panelMain.transform, false);
            var crt = center.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0, 30f);
            crt.sizeDelta = new Vector2(600, 620);

            AddArtPanel(center.transform, panelFrame, new Vector2(-40, -40), new Vector2(40, 40), ColPanelBg);

            AddTitleBlock(center.transform, "INSECT WARS", TitleFont, 220f,
                "Command your colony. Conquer the garden.", 20);

            float y = -10f;
            AddStyledButton(center.transform, "Play Demo", ref y, () => ShowPlay());
            AddStyledButton(center.transform, "How To Play", ref y, () => ShowHow());
            AddStyledButton(center.transform, "Settings", ref y, () => ShowSettings());
            AddStyledButton(center.transform, "About", ref y, () => ShowAbout());
            AddStyledButton(center.transform, "Quit", ref y, () => Application.Quit());

            AddVersionLabel(_panelMain.transform);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Sub-panels
        // ═══════════════════════════════════════════════════════════════

        void BuildSubPanels()
        {
            BuildPlayPanel();
            BuildMapSelectPanel();
            BuildHowToPlayPanel();
            BuildSettingsPanel();
            BuildAboutPanel();
        }

        void BuildPlayPanel()
        {
            _panelPlay = MakePanel("PlayPanel");

            var center = CenteredBox(_panelPlay.transform, 500, 520);
            AddArtPanel(center.transform, panelFrame, new Vector2(-30, -30), new Vector2(30, 30), ColPanelBg);

            AddTitleBlock(center.transform, "Select Difficulty", 42, 130f,
                "Difficulty affects enemy durability and AI speed.", 18);

            float y = -20f;
            AddStyledButton(center.transform, "Easy", ref y, () => { SetDiff(DemoDifficulty.Easy); ShowMapSelect(); });
            AddStyledButton(center.transform, "Normal", ref y, () => { SetDiff(DemoDifficulty.Normal); ShowMapSelect(); });
            AddStyledButton(center.transform, "Hard", ref y, () => { SetDiff(DemoDifficulty.Hard); ShowMapSelect(); });
            y -= 10f;
            AddStyledButton(center.transform, "Back", ref y, () => ShowMain());
        }

        void BuildSettingsPanel()
        {
            _panelSettings = MakePanel("SettingsPanel");

            var center = CenteredBox(_panelSettings.transform, 540, 520);
            AddArtPanel(center.transform, panelFrame, new Vector2(-30, -30), new Vector2(30, 30), ColPanelBg);

            AddPanelTitle(center.transform, "Settings", 42, -30f);

            AddSettingsLabel(center.transform, "Master Volume", -110f);
            AddVolumeRow(center.transform, -150f);

            _fullToggle = AddToggle(center.transform, "Fullscreen",
                GameSession.GetSavedFullscreen(), -230f, x => GameSession.SetFullscreen(x));

            AddSettingsLabel(center.transform,
                $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}", -300f);

            float y = -350f;
            AddStyledButton(center.transform, "Cycle Quality", ref y, CycleQuality);
            y -= 10f;
            AddStyledButton(center.transform, "Back", ref y, () => ShowMain());
        }

        void BuildAboutPanel()
        {
            _panelAbout = MakePanel("AboutPanel");

            var center = CenteredBox(_panelAbout.transform, 600, 480);
            AddArtPanel(center.transform, panelFrame, new Vector2(-30, -30), new Vector2(30, 30), ColPanelBg);

            AddPanelTitle(center.transform, "About", 42, -30f);

            var body = MakeText(center.transform, "BodyText",
                "Insect Wars — Demo\n\n" +
                "A Unity 6 RTS vertical slice set at insect scale.\n" +
                "NavMesh units, single-resource economy, fog of war,\n" +
                "skirmish AI, and procedural terrain generation.\n\n" +
                "Built with URP and the New Input System.",
                19, ColBodyText, TextAnchor.UpperCenter);
            var brt = body.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, -110f);
            brt.sizeDelta = new Vector2(520, 240);

            float y = -380f;
            AddStyledButton(center.transform, "Back", ref y, () => ShowMain());
        }

        // ═══════════════════════════════════════════════════════════════
        //  How To Play with 3D Unit Codex
        // ═══════════════════════════════════════════════════════════════

        Text _unitDetailText;
        RawImage _previewImage;
        RenderTexture _previewRT;
        Camera _previewCam;
        Light _previewLight;
        GameObject _previewModelRoot;
        UnitArchetype _currentPreviewArch;
        readonly UnitArchetype[] _codexOrder =
            { UnitArchetype.Worker, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
        readonly Button[] _tabButtons = new Button[3];
        float _previewYaw;
        float _previewBob;

        void BuildHowToPlayPanel()
        {
            _panelHow = MakePanel("HowPanel");
            var parent = _panelHow.transform;

            AddArtPanel(parent, panelFrame,
                new Vector2(40, 20), new Vector2(-40, -20), ColPanelBg);

            AddPanelTitle(parent, "How To Play", 36, -30f);

            var ctrlGo = new GameObject("Controls");
            ctrlGo.transform.SetParent(parent, false);
            var ctrlRt = ctrlGo.AddComponent<RectTransform>();
            ctrlRt.anchorMin = new Vector2(0.05f, 0.89f);
            ctrlRt.anchorMax = new Vector2(0.95f, 0.95f);
            ctrlRt.offsetMin = ctrlRt.offsetMax = Vector2.zero;
            var ctrlBg = ctrlGo.AddComponent<Image>();
            ctrlBg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            ctrlBg.raycastTarget = false;
            var ctrlTx = MakeText(ctrlGo.transform, "T",
                "LMB select · RMB command · A atk-move · S stop · H hold · P patrol · B build · Esc pause · Scroll zoom · Win: destroy enemy hive",
                14, new Color(0.82f, 0.88f, 0.82f), TextAnchor.MiddleCenter);
            var cTrt = ctrlTx.rectTransform;
            cTrt.anchorMin = Vector2.zero; cTrt.anchorMax = Vector2.one;
            cTrt.offsetMin = new Vector2(12f, 2f); cTrt.offsetMax = new Vector2(-12f, -2f);

            var tabGo = new GameObject("Tabs");
            tabGo.transform.SetParent(parent, false);
            var tabRt = tabGo.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.05f, 0.82f);
            tabRt.anchorMax = new Vector2(0.95f, 0.88f);
            tabRt.offsetMin = tabRt.offsetMax = Vector2.zero;
            var tabHL = tabGo.AddComponent<HorizontalLayoutGroup>();
            tabHL.childAlignment = TextAnchor.MiddleCenter;
            tabHL.childControlWidth = true; tabHL.childControlHeight = true;
            tabHL.childForceExpandWidth = true; tabHL.childForceExpandHeight = true;
            tabHL.spacing = 6f;

            for (var i = 0; i < _codexOrder.Length; i++)
            {
                var idx = i;
                var arch = _codexOrder[i];
                var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
                _tabButtons[i] = BuildCodexTab(tabGo.transform, def.displayName.ToUpperInvariant(),
                    () => ShowCodexUnit(_codexOrder[idx], idx));
            }

            var leftGo = new GameObject("PreviewArea");
            leftGo.transform.SetParent(parent, false);
            var leftRt = leftGo.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0.05f, 0.08f);
            leftRt.anchorMax = new Vector2(0.42f, 0.81f);
            leftRt.offsetMin = leftRt.offsetMax = Vector2.zero;
            leftGo.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.92f);

            var rawGo = new GameObject("PreviewRaw");
            rawGo.transform.SetParent(leftGo.transform, false);
            var rawRt = rawGo.AddComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0.02f, 0.15f);
            rawRt.anchorMax = new Vector2(0.98f, 0.98f);
            rawRt.offsetMin = rawRt.offsetMax = Vector2.zero;
            _previewImage = rawGo.AddComponent<RawImage>();
            _previewImage.raycastTarget = false;

            SetupPreviewCamera();

            var animBar = new GameObject("AnimButtons");
            animBar.transform.SetParent(leftGo.transform, false);
            var abRt = animBar.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.04f, 0.02f);
            abRt.anchorMax = new Vector2(0.96f, 0.14f);
            abRt.offsetMin = abRt.offsetMax = Vector2.zero;
            var abHL = animBar.AddComponent<HorizontalLayoutGroup>();
            abHL.childAlignment = TextAnchor.MiddleCenter;
            abHL.childControlWidth = true; abHL.childControlHeight = true;
            abHL.childForceExpandWidth = true; abHL.childForceExpandHeight = true;
            abHL.spacing = 4f;
            BuildAnimBtn(animBar.transform, "Idle", () => PreviewSetAnim(0));
            BuildAnimBtn(animBar.transform, "Walk", () => PreviewSetAnim(1));
            BuildAnimBtn(animBar.transform, "Attack", () => PreviewSetAnim(2));

            var rightGo = new GameObject("StatsArea");
            rightGo.transform.SetParent(parent, false);
            var rightRt = rightGo.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.44f, 0.08f);
            rightRt.anchorMax = new Vector2(0.95f, 0.81f);
            rightRt.offsetMin = rightRt.offsetMax = Vector2.zero;
            rightGo.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(rightGo.transform, false);
            var sRt = scrollGo.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(4f, 4f); sRt.offsetMax = new Vector2(-4f, -4f);
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewportGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpRt.pivot = new Vector2(0f, 1f);
            viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var cntRt = contentGo.AddComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(0f, 1f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.pivot = new Vector2(0f, 1f);
            cntRt.anchoredPosition = Vector2.zero;
            cntRt.sizeDelta = new Vector2(0f, 0f);
            sr.content = cntRt;

            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(contentGo.transform, false);
            var txtRt = textGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0f, 1f);
            txtRt.anchorMax = new Vector2(1f, 1f);
            txtRt.pivot = new Vector2(0f, 1f);
            txtRt.anchoredPosition = new Vector2(8f, -4f);
            txtRt.sizeDelta = new Vector2(-16f, 0f);
            _unitDetailText = textGo.AddComponent<Text>();
            _unitDetailText.font = _font;
            _unitDetailText.fontSize = 15;
            _unitDetailText.lineSpacing = 1.15f;
            _unitDetailText.supportRichText = true;
            _unitDetailText.color = ColBodyText;
            _unitDetailText.alignment = TextAnchor.UpperLeft;
            _unitDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _unitDetailText.verticalOverflow = VerticalWrapMode.Overflow;

            var csf = textGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var cntFitter = contentGo.AddComponent<ContentSizeFitter>();
            cntFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cntFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(0, 0, 4, 4);

            float backY = 0f;
            AddStyledButton(parent, "Back", ref backY, () => ShowMain());
            var backObj = parent.Find("Back");
            if (backObj != null)
            {
                var brt = backObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0f);
                brt.anchorMax = new Vector2(0.5f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(0f, 14f);
            }

            ShowCodexUnit(UnitArchetype.Worker, 0);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Map Select
        // ═══════════════════════════════════════════════════════════════

        void BuildMapSelectPanel()
        {
            _panelMapSelect = MakePanel("MapSelectPanel");

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_panelMapSelect.transform, false);
            var bdRt = backdrop.AddComponent<RectTransform>();
            bdRt.anchorMin = new Vector2(0.12f, 0.04f);
            bdRt.anchorMax = new Vector2(0.88f, 0.96f);
            bdRt.offsetMin = bdRt.offsetMax = Vector2.zero;

            AddArtPanel(backdrop.transform, panelFrame, Vector2.zero, Vector2.zero, ColPanelBg);

            AddPanelTitle(_panelMapSelect.transform, "Select Map", 42, -40f);

            var diffLabel = MakeText(_panelMapSelect.transform, "DiffLabel",
                $"Difficulty: {GameSession.Difficulty}", 18, ColSubtitle, TextAnchor.UpperCenter);
            var dlRt = diffLabel.rectTransform;
            dlRt.anchorMin = new Vector2(0.5f, 1f);
            dlRt.anchorMax = new Vector2(0.5f, 1f);
            dlRt.pivot = new Vector2(0.5f, 1f);
            dlRt.anchoredPosition = new Vector2(0, -95f);
            dlRt.sizeDelta = new Vector2(400, 32);
            _diffLabelInMapSelect = diffLabel;

            var maps = SkirmishMapPresets.GetAll();
            float y = -140f;
            AddSettingsLabel(_panelMapSelect.transform, "Click a map to start:", y);
            y -= 44f;

            for (int i = 0; i < maps.Length; i++)
                AddMapCard(_panelMapSelect.transform, maps[i], ref y);

            y -= 14f;
            AddStyledButton(_panelMapSelect.transform, "Back", ref y, () => ShowPlay());
        }

        void AddMapCard(Transform parent, SkirmishMapDefinition map, ref float y)
        {
            var card = new GameObject(map.displayName);
            card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(520, 120);
            y -= 134f;

            var bg = card.AddComponent<Image>();
            bg.sprite = mapCardFrame;
            bg.type = mapCardFrame != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = mapCardFrame != null ? new Color(1f, 1f, 1f, 0.9f) : ColMapCardBg;

            var btn = card.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.normalColor = mapCardFrame != null ? new Color(0.85f, 0.9f, 0.85f) : ColMapCardBg;
            btnColors.highlightedColor = mapCardFrame != null ? Color.white : ColMapCardHover;
            btnColors.pressedColor = mapCardFrame != null ? new Color(0.7f, 0.85f, 0.7f) : new Color(0.30f, 0.50f, 0.30f);
            btn.colors = btnColors;
            btn.onClick.AddListener(() =>
            {
                GameSession.SetSelectedMap(map);
                if (map.name == "ShazuDen")
                    SceneLoader.LoadSkirmishDemo("ShazuDen");
                else
                    SceneLoader.LoadSkirmishDemo();
            });

            var nameGo = MakeText(card.transform, "MapName", map.displayName,
                24, ColTitleGold, TextAnchor.UpperLeft);
            nameGo.fontStyle = FontStyle.Bold;
            var nameRt = nameGo.rectTransform;
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(20f, 52f);
            nameRt.offsetMax = new Vector2(-20f, -12f);

            var descGo = MakeText(card.transform, "MapDesc", map.description,
                15, ColBodyText, TextAnchor.UpperLeft);
            descGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            var descRt = descGo.rectTransform;
            descRt.anchorMin = Vector2.zero;
            descRt.anchorMax = new Vector2(1f, 0.55f);
            descRt.offsetMin = new Vector2(20f, 10f);
            descRt.offsetMax = new Vector2(-20f, 0f);

            string sizeLabel = map.mapHalfExtent < 65f ? "Small" : map.mapHalfExtent < 85f ? "Medium" : "Large";
            var sizeGo = MakeText(card.transform, "MapSize",
                $"{sizeLabel} — {(int)(map.mapHalfExtent * 2)}x{(int)(map.mapHalfExtent * 2)}",
                15, ColSubtitle, TextAnchor.UpperRight);
            sizeGo.fontStyle = FontStyle.Bold;
            var sizeRt = sizeGo.rectTransform;
            sizeRt.anchorMin = Vector2.zero;
            sizeRt.anchorMax = Vector2.one;
            sizeRt.offsetMin = new Vector2(20f, 52f);
            sizeRt.offsetMax = new Vector2(-20f, -12f);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Preview camera & animation (unchanged logic, cleaned up)
        // ═══════════════════════════════════════════════════════════════

        GameObject _previewLightFill;

        void SetupPreviewCamera()
        {
            _previewRT = new RenderTexture(512, 512, 24) { antiAliasing = 4 };
            _previewImage.texture = _previewRT;

            var camGo = new GameObject("CodexPreviewCam");
            _previewCam = camGo.AddComponent<Camera>();
            _previewCam.targetTexture = _previewRT;
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);
            _previewCam.fieldOfView = 30f;
            _previewCam.nearClipPlane = 0.05f;
            _previewCam.farClipPlane = 30f;
            _previewCam.cullingMask = ~0;
            _previewCam.depth = -10;
            FrameCameraOnUnit();

            var keyGo = new GameObject("CodexKeyLight");
            keyGo.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
            _previewLight = keyGo.AddComponent<Light>();
            _previewLight.type = LightType.Directional;
            _previewLight.color = new Color(1f, 0.97f, 0.92f);
            _previewLight.intensity = 1.6f;

            var fillGo = new GameObject("CodexFillLight");
            fillGo.transform.rotation = Quaternion.Euler(15f, -40f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.6f, 0.7f, 1f);
            fill.intensity = 0.6f;
            _previewLightFill = fillGo;
        }

        void FrameCameraOnUnit()
        {
            if (_previewCam == null) return;
            var unitCenter = new Vector3(500f, 0.45f, 500f);
            _previewCam.transform.position = unitCenter + new Vector3(0f, 0.4f, -2.2f);
            _previewCam.transform.LookAt(unitCenter);
        }

        int _previewAnimMode;
        float _walkPhase;
        float _attackPhase = -1f;
        Vector3 _previewBasePos;

        void PreviewSetAnim(int mode)
        {
            _previewAnimMode = mode;
            _walkPhase = 0f;
            _attackPhase = mode == 2 ? 0.35f : -1f;
            if (_previewModelRoot == null) return;
            _previewModelRoot.transform.position = _previewBasePos;
            _previewModelRoot.transform.localScale = Vector3.one;
            var driver = _previewModelRoot.GetComponentInChildren<UnitAnimationDriver>();
            if (driver != null)
            {
                driver.previewSpeed = (mode == 1) ? 3.5f : 0f;
                if (mode == 2) driver.NotifyAttack();
            }
        }

        void LateUpdate() => UpdatePreviewAnim();

        void UpdatePreviewAnim()
        {
            if (_previewModelRoot == null) return;
            var t = _previewModelRoot.transform;
            _previewYaw += Time.unscaledDeltaTime * 25f;
            t.rotation = Quaternion.Euler(0f, _previewYaw, 0f);

            if (_previewModelRoot.GetComponentInChildren<UnitAnimationDriver>() != null)
                return;

            switch (_previewAnimMode)
            {
                case 0:
                    _previewBob += Time.unscaledDeltaTime;
                    var breathe = 1f + Mathf.Sin(_previewBob * 2f) * 0.02f;
                    t.localScale = new Vector3(breathe, 1f, breathe);
                    t.position = _previewBasePos;
                    break;
                case 1:
                    _walkPhase += Time.unscaledDeltaTime * 10f;
                    t.position = _previewBasePos + new Vector3(0f, Mathf.Sin(_walkPhase) * 0.04f, 0f);
                    t.localScale = Vector3.one;
                    break;
                case 2:
                    if (_attackPhase >= 0f)
                    {
                        _attackPhase -= Time.unscaledDeltaTime;
                        if (_attackPhase <= 0f) _attackPhase = 0.35f;
                        var p = 1f - (_attackPhase / 0.35f);
                        var lunge = Mathf.Sin(p * Mathf.PI) * 0.3f;
                        var squash = 1f + 0.12f * Mathf.Sin(p * Mathf.PI * 2f);
                        t.position = _previewBasePos + t.forward * lunge;
                        t.localScale = new Vector3(squash, 1f / squash, squash);
                    }
                    break;
            }
        }

        void SpawnPreviewUnit(UnitArchetype arch)
        {
            if (_previewModelRoot != null)
                Destroy(_previewModelRoot);

            var pos = new Vector3(500f, 0f, 500f);
            _previewBasePos = pos;
            _previewModelRoot = new GameObject($"CodexPreview_{arch}");
            _previewModelRoot.transform.position = pos;

            var team = Team.Player;
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);

            GameObject prefab = visualLibrary != null ? visualLibrary.GetUnitPrefab(arch) : null;
            if (prefab != null)
            {
                var inst = Instantiate(prefab, _previewModelRoot.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;

                var agent = inst.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                var col = inst.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                var unit = inst.GetComponent<InsectUnit>();
                if (unit == null) unit = inst.AddComponent<InsectUnit>();
                unit.enabled = true;
                unit.Configure(team, UnitDefinition.CreateRuntimeDefault(arch, skin));

                if (inst.TryGetComponent<UnitHealthBar>(out var hb)) hb.enabled = false;
                if (inst.TryGetComponent<SimpleEnemyAi>(out var ai)) ai.enabled = false;

                var driver = inst.GetComponent<UnitAnimationDriver>();
                if (driver == null) driver = inst.AddComponent<UnitAnimationDriver>();
                driver.enabled = true;

                foreach (var anim in inst.GetComponentsInChildren<Animator>(true))
                {
                    anim.enabled = true;
                    anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                }

                var block = new MaterialPropertyBlock();
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    r.GetPropertyBlock(block);
                    if (r.sharedMaterial != null)
                    {
                        if (r.sharedMaterial.HasProperty("_BaseColor")) block.SetColor("_BaseColor", skin);
                        else if (r.sharedMaterial.HasProperty("_Color")) block.SetColor("_Color", skin);
                    }
                    r.SetPropertyBlock(block);
                }
            }
            else
            {
                switch (arch)
                {
                    case UnitArchetype.Worker:
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.24f, 0.52f), Quaternion.identity, skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 0.58f, 0f), Vector3.one * 0.3f, Quaternion.identity,
                            Color.Lerp(skin, Color.white, 0.2f));
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.01f, 0f), new Vector3(0.85f, 0.02f, 0.85f), Quaternion.identity, accent);
                        break;
                    case UnitArchetype.BasicFighter:
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0f, 0.35f, -0.1f), new Vector3(0.45f, 0.6f, 0.45f),
                            Quaternion.Euler(45f, 0f, 0f), skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 0.85f, 0.3f), Vector3.one * 0.35f, Quaternion.identity, skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(-0.15f, 0.95f, 0.45f), Vector3.one * 0.12f, Quaternion.identity, accent);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0.15f, 0.95f, 0.45f), Vector3.one * 0.12f, Quaternion.identity, accent);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(-0.25f, 0.45f, 0.35f), new Vector3(0.15f, 0.35f, 0.15f),
                            Quaternion.Euler(60f, 0f, 0f), skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0.25f, 0.45f, 0.35f), new Vector3(0.15f, 0.35f, 0.15f),
                            Quaternion.Euler(60f, 0f, 0f), skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.01f, 0f), new Vector3(1.1f, 0.02f, 1.1f), Quaternion.identity, accent);
                        break;
                    case UnitArchetype.BasicRanged:
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.5f, 0.42f), Quaternion.identity, skin);
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 1.02f, 0f), Vector3.one * 0.3f, Quaternion.identity,
                            Color.Lerp(skin, Color.white, 0.2f));
                        BuildPreviewPart(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.01f, 0f), new Vector3(0.85f, 0.02f, 0.85f), Quaternion.identity, accent);
                        break;
                }
            }

            _previewYaw = 0f;
            _previewBob = 0f;
            _previewAnimMode = 0;
            _attackPhase = -1f;
            _currentPreviewArch = arch;
            PreviewSetAnim(0);
            FrameCameraForArch(arch);
        }

        void FrameCameraForArch(UnitArchetype arch)
        {
            if (_previewCam == null) return;
            float centerY = arch switch
            {
                UnitArchetype.Worker => 0.5f,
                UnitArchetype.BasicFighter => 0.8f,
                UnitArchetype.BasicRanged => 0.9f,
                _ => 0.45f
            };
            float dist = arch switch
            {
                UnitArchetype.Worker => 3.8f,
                UnitArchetype.BasicFighter => 4.6f,
                UnitArchetype.BasicRanged => 5.2f,
                _ => 4.0f
            };
            var target = new Vector3(500f, centerY, 500f);
            _previewCam.transform.position = target + new Vector3(0f, 0.6f, -dist);
            _previewCam.transform.LookAt(target);
        }

        static void BuildPreviewPart(Transform parent, PrimitiveType prim, Vector3 lp, Vector3 ls,
            Quaternion lr, Color c)
        {
            var p = GameObject.CreatePrimitive(prim);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = lp;
            p.transform.localScale = ls;
            p.transform.localRotation = lr;
            Object.Destroy(p.GetComponent<Collider>());
            var r = p.GetComponent<Renderer>();
            if (r == null) return;
            var m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            r.material = m;
        }

        void ShowCodexUnit(UnitArchetype arch, int tabIdx)
        {
            SpawnPreviewUnit(arch);
            HighlightTab(tabIdx);

            if (_unitDetailText == null) return;
            var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
            var sb = new StringBuilder(1024);

            sb.AppendLine($"<b><size=20><color=#8CFFA8>{def.displayName.ToUpperInvariant()}</color></size></b>");
            sb.AppendLine($"<i><size=13><color=#AAB0BB>{ArchetypeFlavorText(arch)}</color></size></i>");
            sb.AppendLine();

            Stat(sb, "VISION");
            Row(sb, "Sight radius", $"{def.visionRadius:F0} m");
            sb.AppendLine();

            Stat(sb, "VITALITY");
            Row(sb, "Max HP", $"{def.maxHealth:F0}");
            Row(sb, "Move speed", $"{def.moveSpeed:F1}");
            sb.AppendLine();

            Stat(sb, "COMBAT");
            Row(sb, "Damage / hit", $"{def.attackDamage:F1}");
            Row(sb, "Cooldown", $"{def.attackCooldown:F2} s");
            Row(sb, "DPS", $"~{def.attackDamage / Mathf.Max(0.05f, def.attackCooldown):F1}");
            Row(sb, "Range", $"{def.attackRange:F1} m ({(arch == UnitArchetype.BasicRanged ? "ranged" : "melee")})");
            Row(sb, "Gathers", def.canGather ? "<color=#FFD966>Yes</color>" : "No");
            sb.AppendLine();

            Stat(sb, "HIT BOX");
            AppendHitboxInfo(arch, sb);
            sb.AppendLine();

            Stat(sb, "ACTIONS");
            AppendActions(arch, sb);
            sb.AppendLine();

            Stat(sb, "ANIMATIONS");
            AppendAnimations(arch, sb);

            _unitDetailText.text = sb.ToString();
        }

        static void Stat(StringBuilder sb, string heading) =>
            sb.AppendLine($"<b><color=#FFE87A>{heading}</color></b>");

        static void Row(StringBuilder sb, string label, string value) =>
            sb.AppendLine($"  {label}: <b>{value}</b>");

        void HighlightTab(int active)
        {
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == active
                        ? new Color(0.18f, 0.48f, 0.24f, 1f)
                        : new Color(0.10f, 0.18f, 0.12f, 0.85f);
            }
        }

        static string ArchetypeFlavorText(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker =>
                "Tireless forager. Gathers nectar from rotting fruit and returns it to the hive. Weak in combat but essential for economy.",
            UnitArchetype.BasicFighter =>
                "Armored melee brawler. Closes distance fast and locks onto targets with scythe-arms. High damage up close.",
            UnitArchetype.BasicRanged =>
                "Turns its abdomen and unleashes a scalding chemical spray in a cone. Short range, but punishes clumped enemies.",
            _ => ""
        };

        static void AppendHitboxInfo(UnitArchetype arch, StringBuilder sb)
        {
            switch (arch)
            {
                case UnitArchetype.Worker:
                    Row(sb, "Capsule", "(0, 0.45, 0) r=0.32 h=0.95");
                    Row(sb, "Nav agent", "h=0.92 r=0.30");
                    break;
                case UnitArchetype.BasicFighter:
                    Row(sb, "Capsule", "(0, 0.22, 0) r=0.38 h=0.55");
                    Row(sb, "Nav agent", "h=0.50 r=0.42");
                    break;
                case UnitArchetype.BasicRanged:
                    Row(sb, "Capsule", "(0, 0.55, 0) r=0.28 h=1.10");
                    Row(sb, "Nav agent", "h=1.12 r=0.27");
                    break;
            }
            Row(sb, "Layer", "Units");
        }

        static void AppendActions(UnitArchetype arch, StringBuilder sb)
        {
            sb.AppendLine("  Move · Attack-move · Attack");
            sb.AppendLine("  Stop · Hold · Patrol");
            if (arch == UnitArchetype.Worker)
                sb.AppendLine("  <color=#FFD966>Gather</color> fruit, auto-return nectar");
        }

        static void AppendAnimations(UnitArchetype arch, StringBuilder sb)
        {
            sb.AppendLine("  Procedural (or Animator when prefab)");
            sb.AppendLine("  Walk: Y bob · Attack: 0.35s lunge");
            switch (arch)
            {
                case UnitArchetype.BasicFighter:
                    sb.AppendLine("  Idle: mantis 30s loop (look/scythe/dip)");
                    break;
                default:
                    sb.AppendLine("  Idle: chest-breath pulse");
                    break;
            }
            sb.AppendLine("  Death: scale shrink (or Death trigger)");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Reusable UI builders
        // ═══════════════════════════════════════════════════════════════

        GameObject MakePanel(string name)
        {
            var p = new GameObject(name);
            p.transform.SetParent(_canvas.transform, false);
            Stretch(p.AddComponent<RectTransform>());
            p.SetActive(false);
            return p;
        }

        void AddArtPanel(Transform parent, Sprite sprite, Vector2 insetMin, Vector2 insetMax, Color fallback)
        {
            var bg = new GameObject("ArtBg");
            bg.transform.SetParent(parent, false);
            var rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = insetMin;
            rt.offsetMax = insetMax;
            var img = bg.AddComponent<Image>();
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1f, 1f, 1f, 0.95f);
            }
            else
            {
                img.color = fallback;
            }
        }

        void AddTitleBlock(Transform parent, string title, int titleSize, float blockHeight,
            string subtitle, int subSize)
        {
            var block = new GameObject("TitleBlock");
            block.transform.SetParent(parent, false);
            var brt = block.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, -20f);
            brt.sizeDelta = new Vector2(0, blockHeight);

            if (titleFrame != null)
            {
                var frameGo = new GameObject("TitleFrame");
                frameGo.transform.SetParent(block.transform, false);
                var frt = frameGo.AddComponent<RectTransform>();
                frt.anchorMin = new Vector2(0.05f, 0.1f);
                frt.anchorMax = new Vector2(0.95f, 0.9f);
                frt.offsetMin = frt.offsetMax = Vector2.zero;
                var fimg = frameGo.AddComponent<Image>();
                fimg.sprite = titleFrame;
                fimg.type = Image.Type.Sliced;
                fimg.color = new Color(1f, 1f, 1f, 0.8f);
                fimg.raycastTarget = false;
            }

            var t = MakeText(block.transform, "Title", title, titleSize, ColTitleGold, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            var trt = t.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.35f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = MakeText(block.transform, "Subtitle", subtitle, subSize, ColSubtitle, TextAnchor.UpperCenter);
                s.fontStyle = FontStyle.Italic;
                var srt = s.rectTransform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(1f, 0.38f);
                srt.offsetMin = srt.offsetMax = Vector2.zero;
            }
        }

        void AddPanelTitle(Transform parent, string text, int size, float yPos)
        {
            var t = MakeText(parent, "Title", text, size, ColTitleGold, TextAnchor.UpperCenter);
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yPos);
            rt.sizeDelta = new Vector2(800, 64);
        }

        void AddStyledButton(Transform parent, string label, ref float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var b = new GameObject(label);
            b.transform.SetParent(parent, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(BtnWidth, BtnHeight);
            y -= BtnGap;

            var img = b.AddComponent<Image>();
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.85f, 0.92f, 0.85f);
            }
            else
            {
                img.color = ColBtnNormal;
            }

            var btn = b.AddComponent<Button>();
            var cols = btn.colors;
            if (buttonSprite != null)
            {
                cols.normalColor = new Color(0.78f, 0.88f, 0.78f);
                cols.highlightedColor = Color.white;
                cols.pressedColor = new Color(0.65f, 0.80f, 0.65f);
                cols.selectedColor = new Color(0.78f, 0.88f, 0.78f);
            }
            else
            {
                cols.normalColor = ColBtnNormal;
                cols.highlightedColor = ColBtnHighlight;
                cols.pressedColor = ColBtnPressed;
                cols.selectedColor = ColBtnNormal;
            }
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var tx = MakeText(b.transform, "T", label, BtnFont, ColBtnText, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            if (buttonSprite != null)
                tx.color = new Color(0.12f, 0.20f, 0.10f);
        }

        void AddSettingsLabel(Transform parent, string text, float y)
        {
            var t = MakeText(parent, "Lbl", text, 20, ColLabel, TextAnchor.UpperCenter);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(460, 36);
        }

        void AddVersionLabel(Transform parent)
        {
            var v = MakeText(parent, "Version", "Insect Wars — Demo",
                14, new Color(0.5f, 0.55f, 0.45f, 0.6f), TextAnchor.LowerRight);
            var vrt = v.rectTransform;
            vrt.anchorMin = new Vector2(1f, 0f);
            vrt.anchorMax = new Vector2(1f, 0f);
            vrt.pivot = new Vector2(1f, 0f);
            vrt.anchoredPosition = new Vector2(-20f, 12f);
            vrt.sizeDelta = new Vector2(300, 30);
        }

        Text MakeText(Transform parent, string goName, string text, int size, Color color, TextAnchor anchor)
        {
            var t = new GameObject(goName).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.text = text;
            return t;
        }

        GameObject CenteredBox(Transform parent, float w, float h)
        {
            var go = new GameObject("CenterBox");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 20f);
            rt.sizeDelta = new Vector2(w, h);
            return go;
        }

        Button BuildCodexTab(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.10f, 0.18f, 0.12f, 0.85f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = MakeText(go.transform, "T", label, 17, Color.white, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return btn;
        }

        void BuildAnimBtn(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.08f, 0.14f, 0.18f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = MakeText(go.transform, "T", label, 14, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter);
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Volume & Toggle widgets
        // ═══════════════════════════════════════════════════════════════

        void AddVolumeRow(Transform parent, float y)
        {
            var row = new GameObject("VolRow");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(420, 50);

            float v = GameSession.GetSavedMasterVolume();
            void RefreshLabel()
            {
                if (_volValueLabel != null)
                    _volValueLabel.text = $"{Mathf.RoundToInt(v * 100)}%";
            }

            MakeVolBtn(row.transform, "−", new Vector2(0f, 0f), new Vector2(0.28f, 1f), () =>
            {
                v = Mathf.Clamp01(v - 0.1f);
                GameSession.SetMasterVolume(v);
                RefreshLabel();
            });

            var lblGo = new GameObject("VolPct");
            lblGo.transform.SetParent(row.transform, false);
            _volValueLabel = lblGo.AddComponent<Text>();
            _volValueLabel.font = _font;
            _volValueLabel.fontSize = 24;
            _volValueLabel.fontStyle = FontStyle.Bold;
            _volValueLabel.color = ColTitleGold;
            _volValueLabel.alignment = TextAnchor.MiddleCenter;
            var lrt = _volValueLabel.rectTransform;
            lrt.anchorMin = new Vector2(0.32f, 0f);
            lrt.anchorMax = new Vector2(0.68f, 1f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            RefreshLabel();

            MakeVolBtn(row.transform, "+", new Vector2(0.72f, 0f), new Vector2(1f, 1f), () =>
            {
                v = Mathf.Clamp01(v + 0.1f);
                GameSession.SetMasterVolume(v);
                RefreshLabel();
            });
        }

        void MakeVolBtn(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Vol{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.8f, 0.88f, 0.8f);
            }
            else
            {
                img.color = ColBtnNormal;
            }
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var tx = MakeText(go.transform, "T", label, 26, ColBtnText, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            if (buttonSprite != null) tx.color = new Color(0.12f, 0.20f, 0.10f);
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        Toggle AddToggle(Transform parent, string label, bool on, float y, System.Action<bool> onSet)
        {
            var row = new GameObject(label);
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(420, 40);

            var tgl = row.AddComponent<Toggle>();
            var bg = new GameObject("Bg").AddComponent<Image>();
            bg.transform.SetParent(row.transform, false);
            bg.color = new Color(0.20f, 0.20f, 0.24f);
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = new Vector2(0f, 0.5f);
            bgrt.anchorMax = new Vector2(0f, 0.5f);
            bgrt.pivot = new Vector2(0f, 0.5f);
            bgrt.anchoredPosition = Vector2.zero;
            bgrt.sizeDelta = new Vector2(34, 34);

            var ch = new GameObject("Check").AddComponent<Image>();
            ch.transform.SetParent(bg.transform, false);
            ch.color = new Color(0.45f, 0.85f, 0.35f);
            var crt = ch.rectTransform;
            crt.anchorMin = new Vector2(0.1f, 0.1f);
            crt.anchorMax = new Vector2(0.9f, 0.9f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            tgl.targetGraphic = bg;
            tgl.graphic = ch;
            tgl.isOn = on;
            tgl.onValueChanged.AddListener(val => onSet(val));

            var tx = MakeText(row.transform, "Txt", label, 20, ColBodyText, TextAnchor.MiddleLeft);
            tx.rectTransform.anchorMin = Vector2.zero;
            tx.rectTransform.anchorMax = Vector2.one;
            tx.rectTransform.offsetMin = new Vector2(46f, 0f);
            tx.rectTransform.offsetMax = Vector2.zero;
            return tgl;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Navigation & utilities
        // ═══════════════════════════════════════════════════════════════

        void ShowMain() => SetActivePanels(_panelMain);
        void ShowPlay() => SetActivePanels(_panelPlay);
        void ShowHow() => SetActivePanels(_panelHow);
        void ShowSettings() => SetActivePanels(_panelSettings);
        void ShowAbout() => SetActivePanels(_panelAbout);

        void ShowMapSelect()
        {
            if (_diffLabelInMapSelect != null)
                _diffLabelInMapSelect.text = $"Difficulty: {GameSession.Difficulty}";
            SetActivePanels(_panelMapSelect);
        }

        void SetDiff(DemoDifficulty d) => GameSession.SetDifficulty(d);

        void CycleQuality()
        {
            int n = QualitySettings.names.Length;
            int next = (QualitySettings.GetQualityLevel() + 1) % n;
            GameSession.SetQualityLevel(next);
        }

        static void SetActivePanels(GameObject on)
        {
            var parent = on.transform.parent;
            foreach (Transform c in parent)
            {
                if (c.name is "MainPanel" or "PlayPanel" or "MapSelectPanel"
                    or "HowPanel" or "SettingsPanel" or "AboutPanel")
                    c.gameObject.SetActive(c.gameObject == on);
            }
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (_previewModelRoot != null) Destroy(_previewModelRoot);
            if (_previewCam != null) Destroy(_previewCam.gameObject);
            if (_previewLight != null) Destroy(_previewLight.gameObject);
            if (_previewLightFill != null) Destroy(_previewLightFill);
            if (_previewRT != null) _previewRT.Release();
        }
    }
}
