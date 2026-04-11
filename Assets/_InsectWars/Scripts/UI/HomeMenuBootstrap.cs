using System.Text;
using InsectWars.Core;
using InsectWars.Data;
using InsectWars.RTS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace InsectWars.UI
{
    public class HomeMenuBootstrap : MonoBehaviour
    {
        [SerializeField] string streamingVideoName = "MenuLoop.mp4";
        [SerializeField] UnitVisualLibrary visualLibrary;

        // ── Earthy / organic palette ──
        static readonly Color ColTitle        = new(0.95f, 0.88f, 0.65f);       // warm honey
        static readonly Color ColSubtitle     = new(0.72f, 0.68f, 0.55f);       // dry straw
        static readonly Color ColBodyText     = new(0.88f, 0.85f, 0.78f);       // parchment
        static readonly Color ColLabel        = new(0.70f, 0.66f, 0.55f);       // weathered bark
        static readonly Color ColPanelBg      = new(0.08f, 0.07f, 0.05f, 0.88f);// deep soil
        static readonly Color ColBtnNormal    = new(0.28f, 0.22f, 0.14f, 0.92f);// warm earth
        static readonly Color ColBtnHighlight = new(0.40f, 0.32f, 0.18f, 0.95f);// sunlit soil
        static readonly Color ColBtnPressed   = new(0.48f, 0.38f, 0.20f, 1f);   // clay press
        static readonly Color ColBtnText      = new(0.92f, 0.88f, 0.72f);       // cream
        static readonly Color ColDim          = new(0f, 0f, 0f, 0.50f);
        static readonly Color ColAccent       = new(0.55f, 0.72f, 0.35f);       // moss green
        static readonly Color ColAccentDim    = new(0.35f, 0.48f, 0.22f);       // darker moss
        static readonly Color ColSeparator    = new(0.40f, 0.35f, 0.25f, 0.50f);// thin line
        static readonly Color ColCardBg       = new(0.14f, 0.12f, 0.08f, 0.90f);// dark bark
        static readonly Color ColCardHover    = new(0.22f, 0.18f, 0.12f, 0.95f);

        const float BtnW = 380f, BtnH = 56f, BtnGap = 66f;
        const int BtnFontSize = 24;

        Canvas _canvas;
        Font _font;
        GameObject _panelMain, _panelPlay, _panelMapSelect, _panelHow, _panelSettings, _panelAbout;
        Text _volValueLabel, _diffLabelInMapSelect;
        Toggle _fullToggle;

        void Awake()
        {
            GameSession.LoadPrefs();
            AudioListener.volume = GameSession.GetSavedMasterVolume();
            Screen.fullScreen = GameSession.GetSavedFullscreen();
            _font = UiFontHelper.GetFont();

            SetupEventSystem();
            BuildCanvas();
            BuildVideoBackground();
            BuildMainMenu();
            BuildSubPanels();
            ShowMain();
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
                    ? "file://" + path : path;
                vp.Play();
            }
            else
            {
                raw.color = new Color(0.06f, 0.06f, 0.04f);
                vp.enabled = false;
                BuildFallbackGradient(bg.transform);
            }

            // soft dim overlay
            var dim = new GameObject("Dim");
            dim.transform.SetParent(bg.transform, false);
            Stretch(dim.AddComponent<RectTransform>());
            dim.AddComponent<Image>().color = ColDim;
        }

        void BuildFallbackGradient(Transform parent)
        {
            var tex = new Texture2D(1, 8, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.01f));
            tex.SetPixel(0, 1, new Color(0.04f, 0.04f, 0.02f));
            tex.SetPixel(0, 2, new Color(0.07f, 0.06f, 0.03f));
            tex.SetPixel(0, 3, new Color(0.10f, 0.09f, 0.05f));
            tex.SetPixel(0, 4, new Color(0.12f, 0.11f, 0.06f));
            tex.SetPixel(0, 5, new Color(0.10f, 0.09f, 0.05f));
            tex.SetPixel(0, 6, new Color(0.06f, 0.06f, 0.03f));
            tex.SetPixel(0, 7, new Color(0.03f, 0.03f, 0.02f));
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var grad = new GameObject("Gradient");
            grad.transform.SetParent(parent, false);
            Stretch(grad.AddComponent<RectTransform>());
            grad.AddComponent<RawImage>().texture = tex;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Main Menu
        // ═══════════════════════════════════════════════════════════════

        void BuildMainMenu()
        {
            _panelMain = MakePanel("MainPanel");

            // frosted panel behind menu content
            var panel = new GameObject("MenuBg");
            panel.transform.SetParent(_panelMain.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(520, 680);
            panel.AddComponent<Image>().color = ColPanelBg;

            // title
            var title = Txt(panel.transform, "INSECT WARS", 62, ColTitle, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            AnchorTopCenter(title.rectTransform, new Vector2(0, -40f), new Vector2(480, 80));

            // subtitle
            var sub = Txt(panel.transform, "Command your colony. Conquer the garden.",
                18, ColSubtitle, TextAnchor.MiddleCenter);
            sub.fontStyle = FontStyle.Italic;
            AnchorTopCenter(sub.rectTransform, new Vector2(0, -120f), new Vector2(420, 30));

            // thin separator line
            MakeSeparator(panel.transform, -160f, 300f);

            // buttons
            float y = -185f;
            EarthButton(panel.transform, "Play Demo", ref y, () => ShowPlay());
            EarthButton(panel.transform, "How To Play", ref y, () => ShowHow());
            EarthButton(panel.transform, "Settings", ref y, () => ShowSettings());
            EarthButton(panel.transform, "About", ref y, () => ShowAbout());

            MakeSeparator(panel.transform, y + 20f, 300f);

            y -= 10f;
            EarthButton(panel.transform, "Quit", ref y, () => Application.Quit());

            // version watermark
            var ver = Txt(_panelMain.transform, "Insect Wars — Demo", 13,
                new Color(0.50f, 0.46f, 0.38f, 0.5f), TextAnchor.LowerRight);
            var vrt = ver.rectTransform;
            vrt.anchorMin = new Vector2(1f, 0f);
            vrt.anchorMax = new Vector2(1f, 0f);
            vrt.pivot = new Vector2(1f, 0f);
            vrt.anchoredPosition = new Vector2(-16f, 10f);
            vrt.sizeDelta = new Vector2(280, 24);
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
            var box = EarthBox(_panelPlay.transform, 460, 460);

            PanelHeader(box.transform, "Select Difficulty", -30f);

            var hint = Txt(box.transform, "Affects enemy durability and AI aggressiveness.",
                16, ColSubtitle, TextAnchor.MiddleCenter);
            AnchorTopCenter(hint.rectTransform, new Vector2(0, -90f), new Vector2(400, 28));

            MakeSeparator(box.transform, -125f, 260f);

            float y = -150f;
            EarthButton(box.transform, "Easy", ref y,
                () => { SetDiff(DemoDifficulty.Easy); ShowMapSelect(); });
            EarthButton(box.transform, "Normal", ref y,
                () => { SetDiff(DemoDifficulty.Normal); ShowMapSelect(); });
            EarthButton(box.transform, "Hard", ref y,
                () => { SetDiff(DemoDifficulty.Hard); ShowMapSelect(); });

            MakeSeparator(box.transform, y + 20f, 260f);
            y -= 10f;
            EarthButton(box.transform, "Back", ref y, () => ShowMain());
        }

        void BuildSettingsPanel()
        {
            _panelSettings = MakePanel("SettingsPanel");
            var box = EarthBox(_panelSettings.transform, 500, 500);

            PanelHeader(box.transform, "Settings", -30f);
            MakeSeparator(box.transform, -80f, 280f);

            SettingsLabel(box.transform, "Master Volume", -100f);
            AddVolumeRow(box.transform, -135f);

            _fullToggle = AddToggle(box.transform, "Fullscreen",
                GameSession.GetSavedFullscreen(), -210f, x => GameSession.SetFullscreen(x));

            MakeSeparator(box.transform, -260f, 280f);

            SettingsLabel(box.transform,
                $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}", -280f);

            float y = -320f;
            EarthButton(box.transform, "Cycle Quality", ref y, CycleQuality);
            y -= 10f;
            EarthButton(box.transform, "Back", ref y, () => ShowMain());
        }

        void BuildAboutPanel()
        {
            _panelAbout = MakePanel("AboutPanel");
            var box = EarthBox(_panelAbout.transform, 560, 440);

            PanelHeader(box.transform, "About", -30f);
            MakeSeparator(box.transform, -78f, 320f);

            var body = Txt(box.transform,
                "Insect Wars — Demo\n\n" +
                "A Unity 6 RTS vertical slice set at insect scale.\n" +
                "NavMesh units, single-resource economy, fog of war,\n" +
                "skirmish AI, and procedural terrain generation.\n\n" +
                "Built with URP and the New Input System.",
                17, ColBodyText, TextAnchor.UpperCenter);
            var brt = body.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, -95f);
            brt.sizeDelta = new Vector2(480, 220);

            float y = -350f;
            EarthButton(box.transform, "Back", ref y, () => ShowMain());
        }

        // ═══════════════════════════════════════════════════════════════
        //  How To Play with 3D Unit Codex
        // ═══════════════════════════════════════════════════════════════

        Text _unitDetailText;
        RawImage _previewImage;
        RenderTexture _previewRT;
        Camera _previewCam;
        Light _previewLight;
        GameObject _previewModelRoot, _previewLightFill;
        UnitArchetype _currentPreviewArch;
        readonly UnitArchetype[] _codexOrder =
            { UnitArchetype.Worker, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
        readonly Button[] _tabButtons = new Button[3];
        float _previewYaw, _previewBob;

        void BuildHowToPlayPanel()
        {
            _panelHow = MakePanel("HowPanel");
            var parent = _panelHow.transform;

            // full-screen frosted bg for this panel
            var bg = new GameObject("HowBg");
            bg.transform.SetParent(parent, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.03f, 0.02f);
            bgRt.anchorMax = new Vector2(0.97f, 0.98f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = ColPanelBg;

            PanelHeader(parent, "How To Play", -22f);

            // controls strip
            var ctrlGo = new GameObject("Controls");
            ctrlGo.transform.SetParent(parent, false);
            var ctrlRt = ctrlGo.AddComponent<RectTransform>();
            ctrlRt.anchorMin = new Vector2(0.05f, 0.90f);
            ctrlRt.anchorMax = new Vector2(0.95f, 0.955f);
            ctrlRt.offsetMin = ctrlRt.offsetMax = Vector2.zero;
            ctrlGo.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.07f, 0.85f);
            var ctrlTx = Txt(ctrlGo.transform,
                "LMB select · RMB command · A atk-move · S stop · H hold · P patrol · B build · Esc pause · Scroll zoom",
                13, ColLabel, TextAnchor.MiddleCenter);
            var cTrt = ctrlTx.rectTransform;
            cTrt.anchorMin = Vector2.zero; cTrt.anchorMax = Vector2.one;
            cTrt.offsetMin = new Vector2(10f, 0f); cTrt.offsetMax = new Vector2(-10f, 0f);

            // unit tabs
            var tabGo = new GameObject("Tabs");
            tabGo.transform.SetParent(parent, false);
            var tabRt = tabGo.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.05f, 0.83f);
            tabRt.anchorMax = new Vector2(0.95f, 0.895f);
            tabRt.offsetMin = tabRt.offsetMax = Vector2.zero;
            var tabHL = tabGo.AddComponent<HorizontalLayoutGroup>();
            tabHL.childAlignment = TextAnchor.MiddleCenter;
            tabHL.childControlWidth = true; tabHL.childControlHeight = true;
            tabHL.childForceExpandWidth = true; tabHL.childForceExpandHeight = true;
            tabHL.spacing = 8f;

            for (var i = 0; i < _codexOrder.Length; i++)
            {
                var idx = i;
                var arch = _codexOrder[i];
                var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
                _tabButtons[i] = BuildCodexTab(tabGo.transform, def.displayName.ToUpperInvariant(),
                    () => ShowCodexUnit(_codexOrder[idx], idx));
            }

            // preview area
            var leftGo = new GameObject("PreviewArea");
            leftGo.transform.SetParent(parent, false);
            var leftRt = leftGo.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0.05f, 0.08f);
            leftRt.anchorMax = new Vector2(0.42f, 0.82f);
            leftRt.offsetMin = leftRt.offsetMax = Vector2.zero;
            leftGo.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.92f);

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
            BuildSmallBtn(animBar.transform, "Idle", () => PreviewSetAnim(0));
            BuildSmallBtn(animBar.transform, "Walk", () => PreviewSetAnim(1));
            BuildSmallBtn(animBar.transform, "Attack", () => PreviewSetAnim(2));

            // stats scroll area
            var rightGo = new GameObject("StatsArea");
            rightGo.transform.SetParent(parent, false);
            var rightRt = rightGo.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.44f, 0.08f);
            rightRt.anchorMax = new Vector2(0.95f, 0.82f);
            rightRt.offsetMin = rightRt.offsetMax = Vector2.zero;
            rightGo.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.90f);

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
            cntRt.sizeDelta = Vector2.zero;
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

            textGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(0, 0, 4, 4);

            // back button anchored at bottom
            float backY = 0f;
            EarthButton(parent, "Back", ref backY, () => ShowMain());
            var backObj = parent.Find("Back");
            if (backObj != null)
            {
                var brt2 = backObj.GetComponent<RectTransform>();
                brt2.anchorMin = new Vector2(0.5f, 0f);
                brt2.anchorMax = new Vector2(0.5f, 0f);
                brt2.pivot = new Vector2(0.5f, 0f);
                brt2.anchoredPosition = new Vector2(0f, 12f);
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
            bdRt.anchorMin = new Vector2(0.15f, 0.04f);
            bdRt.anchorMax = new Vector2(0.85f, 0.96f);
            bdRt.offsetMin = bdRt.offsetMax = Vector2.zero;
            backdrop.AddComponent<Image>().color = ColPanelBg;

            PanelHeader(_panelMapSelect.transform, "Select Map", -36f);

            var diffLabel = Txt(_panelMapSelect.transform, $"Difficulty: {GameSession.Difficulty}",
                17, ColSubtitle, TextAnchor.UpperCenter);
            AnchorTopCenter(diffLabel.rectTransform, new Vector2(0, -88f), new Vector2(360, 28));
            _diffLabelInMapSelect = diffLabel;

            MakeSeparator(_panelMapSelect.transform, -120f, 320f);

            var maps = SkirmishMapPresets.GetAll();
            float y = -140f;
            var hint = Txt(_panelMapSelect.transform, "Click a map to begin:",
                16, ColLabel, TextAnchor.UpperCenter);
            AnchorTopCenter(hint.rectTransform, new Vector2(0, y), new Vector2(360, 26));
            y -= 36f;

            for (int i = 0; i < maps.Length; i++)
                AddMapCard(_panelMapSelect.transform, maps[i], ref y);

            MakeSeparator(_panelMapSelect.transform, y + 12f, 320f);
            y -= 8f;
            EarthButton(_panelMapSelect.transform, "Back", ref y, () => ShowPlay());
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
            rt.sizeDelta = new Vector2(480, 110);
            y -= 122f;

            var bg = card.AddComponent<Image>();
            bg.color = ColCardBg;

            var btn = card.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = ColCardBg;
            cols.highlightedColor = ColCardHover;
            cols.pressedColor = ColBtnPressed;
            btn.colors = cols;
            btn.onClick.AddListener(() =>
            {
                GameSession.SetSelectedMap(map);
                if (!string.IsNullOrEmpty(map.name) && Application.CanStreamedLevelBeLoaded(map.name))
                    SceneLoader.LoadSkirmishDemo(map.name);
                else
                    SceneLoader.LoadSkirmishDemo();
            });

            var nameGo = Txt(card.transform, map.displayName, 22, ColTitle, TextAnchor.UpperLeft);
            nameGo.fontStyle = FontStyle.Bold;
            var nameRt = nameGo.rectTransform;
            nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(18f, 48f); nameRt.offsetMax = new Vector2(-18f, -10f);

            var descGo = Txt(card.transform, map.description, 14, ColBodyText, TextAnchor.UpperLeft);
            descGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            var descRt = descGo.rectTransform;
            descRt.anchorMin = Vector2.zero; descRt.anchorMax = new Vector2(1f, 0.52f);
            descRt.offsetMin = new Vector2(18f, 8f); descRt.offsetMax = new Vector2(-18f, 0f);

            string sizeLabel = map.mapHalfExtent < 65f ? "Small" : map.mapHalfExtent < 85f ? "Medium" : "Large";
            var sizeGo = Txt(card.transform,
                $"{sizeLabel} — {(int)(map.mapHalfExtent * 2)}x{(int)(map.mapHalfExtent * 2)}",
                14, ColAccent, TextAnchor.UpperRight);
            sizeGo.fontStyle = FontStyle.Bold;
            var sizeRt = sizeGo.rectTransform;
            sizeRt.anchorMin = Vector2.zero; sizeRt.anchorMax = Vector2.one;
            sizeRt.offsetMin = new Vector2(18f, 48f); sizeRt.offsetMax = new Vector2(-18f, -10f);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Preview camera & animation
        // ═══════════════════════════════════════════════════════════════

        void SetupPreviewCamera()
        {
            _previewRT = new RenderTexture(512, 512, 24) { antiAliasing = 4 };
            _previewImage.texture = _previewRT;

            var camGo = new GameObject("CodexPreviewCam");
            _previewCam = camGo.AddComponent<Camera>();
            _previewCam.targetTexture = _previewRT;
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0.06f, 0.05f, 0.04f, 1f);
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
            _previewLight.color = new Color(1f, 0.95f, 0.85f);
            _previewLight.intensity = 1.6f;

            var fillGo = new GameObject("CodexFillLight");
            fillGo.transform.rotation = Quaternion.Euler(15f, -40f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.6f, 0.65f, 0.8f);
            fill.intensity = 0.6f;
            _previewLightFill = fillGo;
        }

        void FrameCameraOnUnit()
        {
            if (_previewCam == null) return;
            var c = new Vector3(500f, 0.45f, 500f);
            _previewCam.transform.position = c + new Vector3(0f, 0.4f, -2.2f);
            _previewCam.transform.LookAt(c);
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

            if (_previewModelRoot.GetComponentInChildren<UnitAnimationDriver>() != null) return;

            switch (_previewAnimMode)
            {
                case 0:
                    _previewBob += Time.unscaledDeltaTime;
                    var b = 1f + Mathf.Sin(_previewBob * 2f) * 0.02f;
                    t.localScale = new Vector3(b, 1f, b);
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
                        t.position = _previewBasePos + t.forward * (Mathf.Sin(p * Mathf.PI) * 0.3f);
                        var sq = 1f + 0.12f * Mathf.Sin(p * Mathf.PI * 2f);
                        t.localScale = new Vector3(sq, 1f / sq, sq);
                    }
                    break;
            }
        }

        void SpawnPreviewUnit(UnitArchetype arch)
        {
            if (_previewModelRoot != null) Destroy(_previewModelRoot);
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
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.24f, 0.52f), Quaternion.identity, skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 0.58f, 0f), Vector3.one * 0.3f, Quaternion.identity,
                            Color.Lerp(skin, Color.white, 0.2f));
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.01f, 0f), new Vector3(0.85f, 0.02f, 0.85f), Quaternion.identity, accent);
                        break;
                    case UnitArchetype.BasicFighter:
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0f, 0.35f, -0.1f), new Vector3(0.45f, 0.6f, 0.45f),
                            Quaternion.Euler(45f, 0f, 0f), skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 0.85f, 0.3f), Vector3.one * 0.35f, Quaternion.identity, skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(-0.15f, 0.95f, 0.45f), Vector3.one * 0.12f, Quaternion.identity, accent);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0.15f, 0.95f, 0.45f), Vector3.one * 0.12f, Quaternion.identity, accent);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(-0.25f, 0.45f, 0.35f), new Vector3(0.15f, 0.35f, 0.15f),
                            Quaternion.Euler(60f, 0f, 0f), skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0.25f, 0.45f, 0.35f), new Vector3(0.15f, 0.35f, 0.15f),
                            Quaternion.Euler(60f, 0f, 0f), skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Cylinder,
                            new Vector3(0f, 0.01f, 0f), new Vector3(1.1f, 0.02f, 1.1f), Quaternion.identity, accent);
                        break;
                    case UnitArchetype.BasicRanged:
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Capsule,
                            new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.5f, 0.42f), Quaternion.identity, skin);
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Sphere,
                            new Vector3(0f, 1.02f, 0f), Vector3.one * 0.3f, Quaternion.identity,
                            Color.Lerp(skin, Color.white, 0.2f));
                        PreviewPrim(_previewModelRoot.transform, PrimitiveType.Cylinder,
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
            float cy = arch switch { UnitArchetype.Worker => 0.5f, UnitArchetype.BasicFighter => 0.8f,
                UnitArchetype.BasicRanged => 0.9f, _ => 0.45f };
            float d = arch switch { UnitArchetype.Worker => 3.8f, UnitArchetype.BasicFighter => 4.6f,
                UnitArchetype.BasicRanged => 5.2f, _ => 4.0f };
            var target = new Vector3(500f, cy, 500f);
            _previewCam.transform.position = target + new Vector3(0f, 0.6f, -d);
            _previewCam.transform.LookAt(target);
        }

        static void PreviewPrim(Transform parent, PrimitiveType prim, Vector3 lp, Vector3 ls,
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

            sb.AppendLine($"<b><size=20><color=#E8D9A0>{def.displayName.ToUpperInvariant()}</color></size></b>");
            sb.AppendLine($"<i><size=13><color=#A09880>{ArchFlavorText(arch)}</color></size></i>");
            sb.AppendLine();

            StatH(sb, "VISION");
            Row(sb, "Sight radius", $"{def.visionRadius:F0} m"); sb.AppendLine();
            StatH(sb, "VITALITY");
            Row(sb, "Max HP", $"{def.maxHealth:F0}");
            Row(sb, "Move speed", $"{def.moveSpeed:F1}"); sb.AppendLine();
            StatH(sb, "COMBAT");
            Row(sb, "Damage / hit", $"{def.attackDamage:F1}");
            Row(sb, "Cooldown", $"{def.attackCooldown:F2} s");
            Row(sb, "DPS", $"~{def.attackDamage / Mathf.Max(0.05f, def.attackCooldown):F1}");
            Row(sb, "Range", $"{def.attackRange:F1} m ({(arch == UnitArchetype.BasicRanged ? "ranged" : "melee")})");
            Row(sb, "Gathers", def.canGather ? "<color=#C8B060>Yes</color>" : "No"); sb.AppendLine();
            StatH(sb, "ACTIONS");
            sb.AppendLine("  Move · Attack-move · Attack");
            sb.AppendLine("  Stop · Hold · Patrol");
            if (arch == UnitArchetype.Worker)
                sb.AppendLine("  <color=#C8B060>Gather</color> fruit, auto-return nectar");

            _unitDetailText.text = sb.ToString();
        }

        static void StatH(StringBuilder sb, string h) =>
            sb.AppendLine($"<b><color=#C8B868>{h}</color></b>");

        static void Row(StringBuilder sb, string l, string v) =>
            sb.AppendLine($"  {l}: <b>{v}</b>");

        void HighlightTab(int active)
        {
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == active
                        ? new Color(0.35f, 0.28f, 0.16f, 1f)
                        : new Color(0.16f, 0.14f, 0.10f, 0.85f);
            }
        }

        static string ArchFlavorText(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker =>
                "Tireless forager. Gathers nectar from rotting fruit and returns it to the hive. Weak in combat but essential for economy.",
            UnitArchetype.BasicFighter =>
                "Armored melee brawler. Closes distance fast and locks onto targets with scythe-arms. High damage up close.",
            UnitArchetype.BasicRanged =>
                "Turns its abdomen and unleashes a scalding chemical spray in a cone. Short range, but punishes clumped enemies.",
            _ => ""
        };

        // ═══════════════════════════════════════════════════════════════
        //  Reusable UI helpers
        // ═══════════════════════════════════════════════════════════════

        GameObject MakePanel(string name)
        {
            var p = new GameObject(name);
            p.transform.SetParent(_canvas.transform, false);
            Stretch(p.AddComponent<RectTransform>());
            p.SetActive(false);
            return p;
        }

        GameObject EarthBox(Transform parent, float w, float h)
        {
            var go = new GameObject("Box");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = ColPanelBg;
            return go;
        }

        void PanelHeader(Transform parent, string text, float yPos)
        {
            var t = Txt(parent, text, 38, ColTitle, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            AnchorTopCenter(t.rectTransform, new Vector2(0, yPos), new Vector2(600, 50));
        }

        void EarthButton(Transform parent, string label, ref float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var b = new GameObject(label);
            b.transform.SetParent(parent, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(BtnW, BtnH);
            y -= BtnGap;

            b.AddComponent<Image>().color = ColBtnNormal;

            var btn = b.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = ColBtnNormal;
            cols.highlightedColor = ColBtnHighlight;
            cols.pressedColor = ColBtnPressed;
            cols.selectedColor = ColBtnNormal;
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var tx = Txt(b.transform, label, BtnFontSize, ColBtnText, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            Stretch(tx.rectTransform);
        }

        void SettingsLabel(Transform parent, string text, float y)
        {
            var t = Txt(parent, text, 19, ColLabel, TextAnchor.MiddleCenter);
            AnchorTopCenter(t.rectTransform, new Vector2(0, y), new Vector2(400, 30));
        }

        void MakeSeparator(Transform parent, float y, float width)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(width, 1.5f);
            go.AddComponent<Image>().color = ColSeparator;
        }

        Button BuildCodexTab(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.16f, 0.14f, 0.10f, 0.85f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = Txt(go.transform, label, 16, ColBtnText, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            Stretch(tx.rectTransform);
            return btn;
        }

        void BuildSmallBtn(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.10f, 0.9f);
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var tx = Txt(go.transform, label, 13, ColBtnText, TextAnchor.MiddleCenter);
            Stretch(tx.rectTransform);
        }

        // ── Volume & toggle ──

        void AddVolumeRow(Transform parent, float y)
        {
            var row = new GameObject("VolRow");
            row.transform.SetParent(parent, false);
            AnchorTopCenter(row.AddComponent<RectTransform>(), new Vector2(0, y), new Vector2(380, 48));

            float v = GameSession.GetSavedMasterVolume();
            void Refresh() { if (_volValueLabel != null) _volValueLabel.text = $"{Mathf.RoundToInt(v * 100)}%"; }

            VolBtn(row.transform, "−", new Vector2(0, 0), new Vector2(0.28f, 1f), () =>
            { v = Mathf.Clamp01(v - 0.1f); GameSession.SetMasterVolume(v); Refresh(); });

            var lbl = new GameObject("VolPct");
            lbl.transform.SetParent(row.transform, false);
            _volValueLabel = lbl.AddComponent<Text>();
            _volValueLabel.font = _font;
            _volValueLabel.fontSize = 24;
            _volValueLabel.fontStyle = FontStyle.Bold;
            _volValueLabel.color = ColTitle;
            _volValueLabel.alignment = TextAnchor.MiddleCenter;
            var lrt = _volValueLabel.rectTransform;
            lrt.anchorMin = new Vector2(0.32f, 0f); lrt.anchorMax = new Vector2(0.68f, 1f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            Refresh();

            VolBtn(row.transform, "+", new Vector2(0.72f, 0f), new Vector2(1f, 1f), () =>
            { v = Mathf.Clamp01(v + 0.1f); GameSession.SetMasterVolume(v); Refresh(); });
        }

        void VolBtn(Transform parent, string label, Vector2 aMin, Vector2 aMax,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Vol{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = ColBtnNormal;
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var tx = Txt(go.transform, label, 24, ColBtnText, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            Stretch(tx.rectTransform);
        }

        Toggle AddToggle(Transform parent, string label, bool on, float y, System.Action<bool> onSet)
        {
            var row = new GameObject(label);
            row.transform.SetParent(parent, false);
            AnchorTopCenter(row.AddComponent<RectTransform>(), new Vector2(0, y), new Vector2(380, 38));

            var tgl = row.AddComponent<Toggle>();
            var bg = new GameObject("Bg").AddComponent<Image>();
            bg.transform.SetParent(row.transform, false);
            bg.color = new Color(0.22f, 0.20f, 0.16f);
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = new Vector2(0f, 0.5f); bgrt.anchorMax = new Vector2(0f, 0.5f);
            bgrt.pivot = new Vector2(0f, 0.5f);
            bgrt.anchoredPosition = Vector2.zero; bgrt.sizeDelta = new Vector2(32, 32);

            var ch = new GameObject("Check").AddComponent<Image>();
            ch.transform.SetParent(bg.transform, false);
            ch.color = ColAccent;
            var crt = ch.rectTransform;
            crt.anchorMin = new Vector2(0.12f, 0.12f); crt.anchorMax = new Vector2(0.88f, 0.88f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;

            tgl.targetGraphic = bg;
            tgl.graphic = ch;
            tgl.isOn = on;
            tgl.onValueChanged.AddListener(val => onSet(val));

            var tx = Txt(row.transform, label, 19, ColBodyText, TextAnchor.MiddleLeft);
            tx.rectTransform.anchorMin = Vector2.zero; tx.rectTransform.anchorMax = Vector2.one;
            tx.rectTransform.offsetMin = new Vector2(44f, 0f); tx.rectTransform.offsetMax = Vector2.zero;
            return tgl;
        }

        // ── Utilities ──

        Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var t = new GameObject("T").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = _font; t.fontSize = size; t.color = color;
            t.alignment = anchor; t.supportRichText = true; t.text = text;
            return t;
        }

        static void AnchorTopCenter(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

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
            int next = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            GameSession.SetQualityLevel(next);
        }

        static void SetActivePanels(GameObject on)
        {
            foreach (Transform c in on.transform.parent)
            {
                if (c.name is "MainPanel" or "PlayPanel" or "MapSelectPanel"
                    or "HowPanel" or "SettingsPanel" or "AboutPanel")
                    c.gameObject.SetActive(c.gameObject == on);
            }
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
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
