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
    /// <summary>
    /// Builds main menu at runtime: optional video loop, navigation, settings, difficulty.
    /// </summary>
    public class HomeMenuBootstrap : MonoBehaviour
    {
        [SerializeField] string streamingVideoName = "MenuLoop.mp4";
        [SerializeField] UnitVisualLibrary visualLibrary;

        Canvas _canvas;
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
            var menuScaler = go.AddComponent<CanvasScaler>();
            menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            menuScaler.referenceResolution = new Vector2(1920, 1080);
            menuScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            menuScaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildVideoBackground()
        {
            var bg = new GameObject("VideoBackground");
            bg.transform.SetParent(_canvas.transform, false);
            var rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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
                vp.url = Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor
                    ? "file://" + path
                    : path;
                vp.Play();
            }
            else
            {
                raw.color = new Color(0.12f, 0.18f, 0.28f);
                vp.enabled = false;
            }

            var dim = new GameObject("Dim");
            dim.transform.SetParent(bg.transform, false);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
        }

        void BuildMainMenu()
        {
            _panelMain = Panel("MainPanel", _canvas.transform);
            var v = GetFont();
            AddTitle(_panelMain.transform, "INSECT WARS", v, 42);
            float y = -120f;
            AddMenuButton(_panelMain.transform, "Play Demo", v, ref y, () => ShowPlay());
            AddMenuButton(_panelMain.transform, "How To Play", v, ref y, () => ShowHow());
            AddMenuButton(_panelMain.transform, "Settings", v, ref y, () => ShowSettings());
            AddMenuButton(_panelMain.transform, "About", v, ref y, () => ShowAbout());
            AddMenuButton(_panelMain.transform, "Quit", v, ref y, () => Application.Quit());
        }

        void BuildSubPanels()
        {
            var v = GetFont();
            _panelPlay = Panel("PlayPanel", _canvas.transform);
            AddTitle(_panelPlay.transform, "Select Difficulty", v, 32);
            AddLabel(_panelPlay.transform, "Difficulty affects enemy durability and AI speed.", v, -90f);
            float y = -140f;
            AddMenuButton(_panelPlay.transform, "Easy", v, ref y, () => { SetDiff(DemoDifficulty.Easy); ShowMapSelect(); });
            AddMenuButton(_panelPlay.transform, "Normal", v, ref y, () => { SetDiff(DemoDifficulty.Normal); ShowMapSelect(); });
            AddMenuButton(_panelPlay.transform, "Hard", v, ref y, () => { SetDiff(DemoDifficulty.Hard); ShowMapSelect(); });
            AddMenuButton(_panelPlay.transform, "Back", v, ref y, () => ShowMain());

            BuildMapSelectPanel(v);

            _panelHow = Panel("HowPanel", _canvas.transform);
            BuildHowToPlayPanel(_panelHow.transform, v);

            _panelSettings = Panel("SettingsPanel", _canvas.transform);
            AddTitle(_panelSettings.transform, "Settings", v, 32);
            AddVolumeRow(_panelSettings.transform, v, -100f);
            _fullToggle = AddToggle(_panelSettings.transform, "Fullscreen", GameSession.GetSavedFullscreen(), v, -180f, x => GameSession.SetFullscreen(x));
            AddLabel(_panelSettings.transform, $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}", v, -240f);
            y = -280f;
            AddMenuButton(_panelSettings.transform, "Cycle Quality", v, ref y, CycleQuality);
            y -= 8f;
            AddMenuButton(_panelSettings.transform, "Back", v, ref y, () => ShowMain());

            _panelAbout = Panel("AboutPanel", _canvas.transform);
            AddTitle(_panelAbout.transform, "About", v, 32);
            AddMultiline(_panelAbout.transform, "Insect Wars — Demo 0\n\nUnity 6 RTS vertical slice: NavMesh units, economy, fog of war, skirmish flow.\nProcedural primitives are the default when no UnitVisualLibrary prefabs are assigned.\n\nOptional: assign a Skirmish Map Definition on SkirmishDirector for authored layouts.", v, -70f);
            y = -280f;
            AddMenuButton(_panelAbout.transform, "Back", v, ref y, () => ShowMain());
        }

        // ───────── How To Play with 3D Unit Codex ─────────

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

        void BuildHowToPlayPanel(Transform parent, Font font)
        {
            AddTitle(parent, "How To Play", font, 26);

            // ── Controls strip below title ──
            var ctrlGo = new GameObject("Controls");
            ctrlGo.transform.SetParent(parent, false);
            var ctrlRt = ctrlGo.AddComponent<RectTransform>();
            ctrlRt.anchorMin = new Vector2(0.03f, 0.89f);
            ctrlRt.anchorMax = new Vector2(0.97f, 0.95f);
            ctrlRt.offsetMin = ctrlRt.offsetMax = Vector2.zero;
            var ctrlBg = ctrlGo.AddComponent<Image>();
            ctrlBg.color = new Color(0.06f, 0.08f, 0.14f, 0.85f);
            ctrlBg.raycastTarget = false;
            var ctrlTx = new GameObject("T").AddComponent<Text>();
            ctrlTx.transform.SetParent(ctrlGo.transform, false);
            ctrlTx.font = font;
            ctrlTx.fontSize = 13;
            ctrlTx.color = new Color(0.82f, 0.84f, 0.88f);
            ctrlTx.alignment = TextAnchor.MiddleCenter;
            ctrlTx.supportRichText = true;
            ctrlTx.text = "LMB select · RMB move/attack/gather · A atk-move · S stop · H hold · P patrol · B build · Esc pause · Scroll zoom · Win: kill all foes";
            var crt = ctrlTx.rectTransform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(12f, 2f); crt.offsetMax = new Vector2(-12f, -2f);

            // ── Unit tab row ──
            var tabGo = new GameObject("Tabs");
            tabGo.transform.SetParent(parent, false);
            var tabRt = tabGo.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.03f, 0.82f);
            tabRt.anchorMax = new Vector2(0.97f, 0.88f);
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
                _tabButtons[i] = BuildCodexTab(tabGo.transform, font, def.displayName.ToUpperInvariant(),
                    () => ShowCodexUnit(_codexOrder[idx], idx));
            }

            // ── Left half: 3D preview ──
            var leftGo = new GameObject("PreviewArea");
            leftGo.transform.SetParent(parent, false);
            var leftRt = leftGo.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0.03f, 0.08f);
            leftRt.anchorMax = new Vector2(0.42f, 0.81f);
            leftRt.offsetMin = leftRt.offsetMax = Vector2.zero;

            var previewBg = leftGo.AddComponent<Image>();
            previewBg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
            previewBg.raycastTarget = false;

            var rawGo = new GameObject("PreviewRaw");
            rawGo.transform.SetParent(leftGo.transform, false);
            var rawRt = rawGo.AddComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0.02f, 0.15f);
            rawRt.anchorMax = new Vector2(0.98f, 0.98f);
            rawRt.offsetMin = rawRt.offsetMax = Vector2.zero;
            _previewImage = rawGo.AddComponent<RawImage>();
            _previewImage.raycastTarget = false;

            SetupPreviewCamera();

            // ── Anim buttons below preview ──
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
            BuildAnimBtn(animBar.transform, font, "Idle", () => PreviewSetAnim(0));
            BuildAnimBtn(animBar.transform, font, "Walk", () => PreviewSetAnim(1));
            BuildAnimBtn(animBar.transform, font, "Attack", () => PreviewSetAnim(2));

            // ── Right half: stats scroll ──
            var rightGo = new GameObject("StatsArea");
            rightGo.transform.SetParent(parent, false);
            var rightRt = rightGo.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.44f, 0.08f);
            rightRt.anchorMax = new Vector2(0.97f, 0.81f);
            rightRt.offsetMin = rightRt.offsetMax = Vector2.zero;

            var rightBg = rightGo.AddComponent<Image>();
            rightBg.color = new Color(0.05f, 0.07f, 0.12f, 0.90f);
            rightBg.raycastTarget = false;

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
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.01f);
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
            _unitDetailText.font = font;
            _unitDetailText.fontSize = 14;
            _unitDetailText.lineSpacing = 1.15f;
            _unitDetailText.supportRichText = true;
            _unitDetailText.color = new Color(0.92f, 0.93f, 0.95f);
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

            // ── Back button at bottom ──
            float backY = 0f;
            AddMenuButton(parent, "Back", font, ref backY, () => ShowMain());
            var backObj = parent.Find("Back");
            if (backObj != null)
            {
                var brt = backObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0f);
                brt.anchorMax = new Vector2(0.5f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(0f, 10f);
            }

            ShowCodexUnit(UnitArchetype.Worker, 0);
        }

        GameObject _previewLightFill;

        void SetupPreviewCamera()
        {
            _previewRT = new RenderTexture(512, 512, 24);
            _previewRT.antiAliasing = 4;
            _previewImage.texture = _previewRT;

            var camGo = new GameObject("CodexPreviewCam");
            _previewCam = camGo.AddComponent<Camera>();
            _previewCam.targetTexture = _previewRT;
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0.07f, 0.09f, 0.14f, 1f);
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
            var camPos = unitCenter + new Vector3(0f, 0.4f, -2.2f);
            _previewCam.transform.position = camPos;
            _previewCam.transform.LookAt(unitCenter);
        }

        int _previewAnimMode; // 0=idle 1=walk 2=attack
        float _walkPhase;
        float _attackPhase = -1f;
        Vector3 _previewBasePos;

        void PreviewSetAnim(int mode)
        {
            _previewAnimMode = mode;
            _walkPhase = 0f;
            _attackPhase = mode == 2 ? 0.35f : -1f;
            if (_previewModelRoot != null)
            {
                _previewModelRoot.transform.position = _previewBasePos;
                _previewModelRoot.transform.localScale = Vector3.one;

                var driver = _previewModelRoot.GetComponentInChildren<UnitAnimationDriver>();
                if (driver != null)
                {
                    driver.previewSpeed = (mode == 1) ? 3.5f : 0f;
                    if (mode == 2) driver.NotifyAttack();
                }
            }
        }

        void LateUpdate()
        {
            UpdatePreviewAnim();
        }

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
                case 0: // idle breathe
                    _previewBob += Time.unscaledDeltaTime;
                    var breathe = 1f + Mathf.Sin(_previewBob * 2f) * 0.02f;
                    t.localScale = new Vector3(breathe, 1f, breathe);
                    t.position = _previewBasePos;
                    break;
                case 1: // walk bob
                    _walkPhase += Time.unscaledDeltaTime * 10f;
                    var yBob = Mathf.Sin(_walkPhase) * 0.04f;
                    t.position = _previewBasePos + new Vector3(0f, yBob, 0f);
                    t.localScale = Vector3.one;
                    break;
                case 2: // attack lunge
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

            GameObject prefab = (visualLibrary != null) ? visualLibrary.GetUnitPrefab(arch) : null;
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

                // Ensure actual skirmish Animator is active and smooth in menus
                foreach (var anim in inst.GetComponentsInChildren<Animator>(true))
                {
                    anim.enabled = true;
                    anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                }

                var block = new MaterialPropertyBlock();
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.gameObject.name == "TeamStrap") continue;
                    r.GetPropertyBlock(block);
                    if (r.sharedMaterial != null)
                    {
                        if (r.sharedMaterial.HasProperty("_BaseColor")) block.SetColor("_BaseColor", skin);
                        else if (r.sharedMaterial.HasProperty("_Color")) block.SetColor("_Color", skin);
                    }
                    r.SetPropertyBlock(block);
                }

                var strap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strap.name = "TeamStrap";
                strap.transform.SetParent(inst.transform, false);
                strap.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                strap.transform.localScale = new Vector3(0.85f, 0.02f, 0.85f);
                Destroy(strap.GetComponent<Collider>());
                var sr = strap.GetComponent<Renderer>();
                if (sr != null)
                {
                    var m = sr.material;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", accent);
                    else if (m.HasProperty("_Color")) m.color = accent;
                    sr.material = m;
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

            Debug.Log($"[CodexPreview] Spawned {arch}, children={_previewModelRoot.transform.childCount}, " +
                      $"library={(visualLibrary != null ? "Yes" : "No")}, " +
                      $"shader={_previewModelRoot.GetComponentInChildren<Renderer>()?.material?.shader?.name}");
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
            var camPos = target + new Vector3(0f, 0.6f, -dist);
            _previewCam.transform.position = camPos;
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

            // Use the property block on the existing material to tint — avoids all shader/material issues
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
            sb.AppendLine($"<i><size=12><color=#AAB0BB>{ArchetypeFlavorText(arch)}</color></size></i>");
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

        static void Stat(StringBuilder sb, string heading)
        {
            sb.AppendLine($"<b><color=#FFE87A>{heading}</color></b>");
        }

        static void Row(StringBuilder sb, string label, string value)
        {
            sb.AppendLine($"  {label}: <b>{value}</b>");
        }

        void HighlightTab(int active)
        {
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == active
                        ? new Color(0.2f, 0.52f, 0.3f, 1f)
                        : new Color(0.14f, 0.22f, 0.16f, 0.85f);
            }
        }

        static string ArchetypeFlavorText(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker =>
                "Tireless forager. Gathers nectar from rotting fruit and returns it to the hive. Weak in combat but essential for economy.",
            UnitArchetype.BasicFighter =>
                "Armored melee brawler. Closes distance fast and locks onto targets with scythe-arms. High damage up close.",
            UnitArchetype.BasicRanged =>
                "Ranged bombardier. Launches homing acid projectiles from distance. Fragile if flanked, deadly in groups.",
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

        Button BuildCodexTab(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.22f, 0.16f, 0.85f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = new GameObject("T").AddComponent<Text>();
            tx.transform.SetParent(go.transform, false);
            tx.font = font;
            tx.fontSize = 16;
            tx.fontStyle = FontStyle.Bold;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = label;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return btn;
        }

        void BuildAnimBtn(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = new GameObject("T").AddComponent<Text>();
            tx.transform.SetParent(go.transform, false);
            tx.font = font;
            tx.fontSize = 13;
            tx.color = new Color(0.8f, 0.9f, 1f);
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = label;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (_previewModelRoot != null) Destroy(_previewModelRoot);
            if (_previewCam != null) Destroy(_previewCam.gameObject);
            if (_previewLight != null) Destroy(_previewLight.gameObject);
            if (_previewLightFill != null) Destroy(_previewLightFill);
            if (_previewRT != null) _previewRT.Release();
        }

        static Font GetFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        static GameObject Panel(string name, Transform parent)
        {
            var p = new GameObject(name);
            p.transform.SetParent(parent, false);
            var rt = p.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            p.SetActive(false);
            return p;
        }

        void AddTitle(Transform parent, string text, Font font, int size)
        {
            var t = new GameObject("Title").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.95f, 0.55f);
            t.alignment = TextAnchor.UpperCenter;
            t.text = text;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -40f);
            rt.sizeDelta = new Vector2(800, 64);
        }

        void AddLabel(Transform parent, string text, Font font, float y)
        {
            var t = new GameObject("Lbl").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = 18;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperCenter;
            t.text = text;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(700, 40);
        }

        void AddMultiline(Transform parent, string text, Font font, float yTop)
        {
            var t = new GameObject("Body").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = 17;
            t.color = new Color(0.92f, 0.92f, 0.92f);
            t.alignment = TextAnchor.UpperCenter;
            t.text = text;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yTop);
            rt.sizeDelta = new Vector2(640, 360);
        }

        void AddMenuButton(Transform parent, string label, Font font, ref float y, UnityEngine.Events.UnityAction onClick)
        {
            var b = new GameObject(label);
            b.transform.SetParent(parent, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(280, 44);
            y -= 52f;
            var img = b.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.22f, 0.95f);
            var btn = b.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var tx = new GameObject("T").AddComponent<Text>();
            tx.transform.SetParent(b.transform, false);
            tx.font = font;
            tx.fontSize = 20;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = label;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        void AddVolumeRow(Transform parent, Font font, float y)
        {
            AddLabel(parent, "Master volume", font, y);
            var row = new GameObject("VolRow");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y - 36f);
            rt.sizeDelta = new Vector2(400, 40);
            float v = GameSession.GetSavedMasterVolume();
            void RefreshLabel()
            {
                if (_volValueLabel != null)
                    _volValueLabel.text = $"{Mathf.RoundToInt(v * 100)}%";
            }
            var minusBtn = new GameObject("VolMinus");
            minusBtn.transform.SetParent(row.transform, false);
            var mrt = minusBtn.AddComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0f, 0f);
            mrt.anchorMax = new Vector2(0.3f, 1f);
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;
            minusBtn.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.22f, 0.95f);
            var mbtn = minusBtn.AddComponent<Button>();
            mbtn.onClick.AddListener(() =>
            {
                v = Mathf.Clamp01(v - 0.1f);
                GameSession.SetMasterVolume(v);
                RefreshLabel();
            });
            var mtx = new GameObject("T").AddComponent<Text>();
            mtx.transform.SetParent(minusBtn.transform, false);
            mtx.font = font;
            mtx.fontSize = 20;
            mtx.color = Color.white;
            mtx.alignment = TextAnchor.MiddleCenter;
            mtx.text = "-";
            mtx.rectTransform.anchorMin = Vector2.zero;
            mtx.rectTransform.anchorMax = Vector2.one;
            mtx.rectTransform.offsetMin = Vector2.zero;
            mtx.rectTransform.offsetMax = Vector2.zero;
            var lblGo = new GameObject("VolPct");
            lblGo.transform.SetParent(row.transform, false);
            _volValueLabel = lblGo.AddComponent<Text>();
            _volValueLabel.font = font;
            _volValueLabel.fontSize = 22;
            _volValueLabel.color = Color.white;
            _volValueLabel.alignment = TextAnchor.MiddleCenter;
            var lrt = _volValueLabel.rectTransform;
            lrt.anchorMin = new Vector2(0.35f, 0f);
            lrt.anchorMax = new Vector2(0.65f, 1f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            RefreshLabel();
            var plusBtn = new GameObject("VolPlus");
            plusBtn.transform.SetParent(row.transform, false);
            var prt = plusBtn.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.7f, 0f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var pimg = plusBtn.AddComponent<Image>();
            pimg.color = new Color(0.2f, 0.35f, 0.22f, 0.95f);
            var pbtn = plusBtn.AddComponent<Button>();
            pbtn.onClick.AddListener(() =>
            {
                v = Mathf.Clamp01(v + 0.1f);
                GameSession.SetMasterVolume(v);
                RefreshLabel();
            });
            var ptx = new GameObject("T").AddComponent<Text>();
            ptx.transform.SetParent(plusBtn.transform, false);
            ptx.font = font;
            ptx.fontSize = 20;
            ptx.color = Color.white;
            ptx.alignment = TextAnchor.MiddleCenter;
            ptx.text = "+";
            var ptrt = ptx.rectTransform;
            ptrt.anchorMin = Vector2.zero;
            ptrt.anchorMax = Vector2.one;
            ptrt.offsetMin = Vector2.zero;
            ptrt.offsetMax = Vector2.zero;
        }

        Toggle AddToggle(Transform parent, string label, bool on, Font font, float y, System.Action<bool> onSet)
        {
            var row = new GameObject(label);
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(400, 32);
            var tgl = row.AddComponent<Toggle>();
            var bg = new GameObject("Bg").AddComponent<Image>();
            bg.transform.SetParent(row.transform, false);
            bg.color = new Color(0.25f, 0.25f, 0.3f);
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = new Vector2(0f, 0.5f);
            bgrt.anchorMax = new Vector2(0f, 0.5f);
            bgrt.pivot = new Vector2(0f, 0.5f);
            bgrt.anchoredPosition = new Vector2(0, 0);
            bgrt.sizeDelta = new Vector2(28, 28);
            var ch = new GameObject("Check").AddComponent<Image>();
            ch.transform.SetParent(bg.transform, false);
            ch.color = new Color(0.5f, 0.85f, 0.4f);
            var crt = ch.rectTransform;
            crt.anchorMin = new Vector2(0.1f, 0.1f);
            crt.anchorMax = new Vector2(0.9f, 0.9f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            tgl.targetGraphic = bg;
            tgl.graphic = ch;
            tgl.isOn = on;
            tgl.onValueChanged.AddListener(v => onSet(v));
            var tx = new GameObject("Txt").AddComponent<Text>();
            tx.transform.SetParent(row.transform, false);
            tx.font = font;
            tx.fontSize = 18;
            tx.color = Color.white;
            tx.text = label;
            tx.rectTransform.anchorMin = new Vector2(0f, 0f);
            tx.rectTransform.anchorMax = new Vector2(1f, 1f);
            tx.rectTransform.offsetMin = new Vector2(40f, 0f);
            tx.rectTransform.offsetMax = Vector2.zero;
            return tgl;
        }

        void CycleQuality()
        {
            int n = QualitySettings.names.Length;
            int next = (QualitySettings.GetQualityLevel() + 1) % n;
            GameSession.SetQualityLevel(next);
        }

        void BuildMapSelectPanel(Font font)
        {
            _panelMapSelect = Panel("MapSelectPanel", _canvas.transform);

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_panelMapSelect.transform, false);
            var bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
            bdImg.raycastTarget = false;
            var bdRt = backdrop.GetComponent<RectTransform>();
            bdRt.anchorMin = new Vector2(0.15f, 0.05f);
            bdRt.anchorMax = new Vector2(0.85f, 0.95f);
            bdRt.offsetMin = bdRt.offsetMax = Vector2.zero;

            AddTitle(_panelMapSelect.transform, "Select Map", font, 32);

            var diffLabel = new GameObject("DiffLabel").AddComponent<Text>();
            diffLabel.transform.SetParent(_panelMapSelect.transform, false);
            diffLabel.font = font;
            diffLabel.fontSize = 16;
            diffLabel.color = new Color(0.7f, 0.85f, 0.7f);
            diffLabel.alignment = TextAnchor.UpperCenter;
            diffLabel.text = $"Difficulty: {GameSession.Difficulty}";
            var dlRt = diffLabel.rectTransform;
            dlRt.anchorMin = new Vector2(0.5f, 1f);
            dlRt.anchorMax = new Vector2(0.5f, 1f);
            dlRt.pivot = new Vector2(0.5f, 1f);
            dlRt.anchoredPosition = new Vector2(0, -80f);
            dlRt.sizeDelta = new Vector2(400, 30);
            _diffLabelInMapSelect = diffLabel;

            var maps = SkirmishMapPresets.GetAll();
            float y = -120f;

            AddLabel(_panelMapSelect.transform, "Click a map to start:", font, y);
            y -= 40f;

            for (int i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                AddMapCard(_panelMapSelect.transform, font, map, ref y);
            }

            y -= 12f;
            AddMenuButton(_panelMapSelect.transform, "Back", font, ref y, () => ShowPlay());
        }

        void AddMapCard(Transform parent, Font font, SkirmishMapDefinition map, ref float y)
        {
            var card = new GameObject(map.displayName);
            card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(420, 110);
            y -= 122f;

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.32f, 0.20f, 0.95f);
            var btn = card.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color(0.28f, 0.48f, 0.30f);
            btnColors.pressedColor = new Color(0.35f, 0.55f, 0.35f);
            btn.colors = btnColors;
            btn.onClick.AddListener(() =>
            {
                GameSession.SetSelectedMap(map);
                SceneLoader.LoadSkirmishDemo();
            });

            var nameGo = new GameObject("MapName").AddComponent<Text>();
            nameGo.transform.SetParent(card.transform, false);
            nameGo.font = font;
            nameGo.fontSize = 22;
            nameGo.fontStyle = FontStyle.Bold;
            nameGo.color = new Color(1f, 0.95f, 0.55f);
            nameGo.alignment = TextAnchor.UpperLeft;
            nameGo.text = map.displayName;
            var nameRt = nameGo.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(16f, 50f);
            nameRt.offsetMax = new Vector2(-16f, -10f);

            var descGo = new GameObject("MapDesc").AddComponent<Text>();
            descGo.transform.SetParent(card.transform, false);
            descGo.font = font;
            descGo.fontSize = 14;
            descGo.color = new Color(0.88f, 0.90f, 0.92f);
            descGo.alignment = TextAnchor.UpperLeft;
            descGo.text = map.description;
            descGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            var descRt = descGo.rectTransform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.55f);
            descRt.offsetMin = new Vector2(16f, 8f);
            descRt.offsetMax = new Vector2(-16f, 0f);

            var sizeGo = new GameObject("MapSize").AddComponent<Text>();
            sizeGo.transform.SetParent(card.transform, false);
            sizeGo.font = font;
            sizeGo.fontSize = 14;
            sizeGo.fontStyle = FontStyle.Bold;
            sizeGo.color = new Color(0.6f, 0.9f, 0.65f);
            sizeGo.alignment = TextAnchor.UpperRight;
            string sizeLabel = map.mapHalfExtent < 65f ? "Small" : map.mapHalfExtent < 85f ? "Medium" : "Large";
            sizeGo.text = $"{sizeLabel} — {(int)(map.mapHalfExtent * 2)}x{(int)(map.mapHalfExtent * 2)}";
            var sizeRt = sizeGo.rectTransform;
            sizeRt.anchorMin = new Vector2(0f, 0f);
            sizeRt.anchorMax = new Vector2(1f, 1f);
            sizeRt.offsetMin = new Vector2(16f, 50f);
            sizeRt.offsetMax = new Vector2(-16f, -10f);
        }

        void ShowMapSelect()
        {
            if (_diffLabelInMapSelect != null)
                _diffLabelInMapSelect.text = $"Difficulty: {GameSession.Difficulty}";
            SetActivePanels(_panelMapSelect);
        }

        void ShowMain()
        {
            SetActivePanels(_panelMain);
        }

        void ShowPlay()
        {
            SetActivePanels(_panelPlay);
        }

        void ShowHow()
        {
            SetActivePanels(_panelHow);
        }

        void ShowSettings()
        {
            SetActivePanels(_panelSettings);
        }

        void ShowAbout()
        {
            SetActivePanels(_panelAbout);
        }

        static void SetActivePanels(GameObject on)
        {
            var parent = on.transform.parent;
            foreach (Transform c in parent)
            {
                if (c.name is "MainPanel" or "PlayPanel" or "MapSelectPanel" or "HowPanel" or "SettingsPanel" or "AboutPanel" or "UnitCodexPanel")
                    c.gameObject.SetActive(c.gameObject == on);
            }
        }

        void SetDiff(DemoDifficulty d)
        {
            GameSession.SetDifficulty(d);
        }
    }
}
