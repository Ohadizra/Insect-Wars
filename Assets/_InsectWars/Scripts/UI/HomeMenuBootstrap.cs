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

        [Header("Steampunk Sketch Assets")]
        [SerializeField] Sprite mainFrameSprite;
        [SerializeField] Sprite buttonSprite;
        [SerializeField] Sprite separatorSprite;
        [SerializeField] Sprite logoSprite;

        // ── Sketch Palette ──
        static readonly Color ColTitle     = new(0.96f, 0.90f, 0.78f); // Light Amber/Parchment
        static readonly Color ColSub       = new(0.83f, 0.69f, 0.44f); // Warm Copper/Gold
        static readonly Color ColDim       = new(0f, 0f, 0f, 0.70f); // Dark Charcoal Dimmer
        static readonly Color ColWhite     = Color.white;

        const float PanelW = 750f, PanelH = 722.5f;
        const float BtnW = 400f, BtnH = 120f, BtnGap = 140f;
        const int TitleSize = 48, SubSize = 15, BtnFontSize = 22;

        Canvas _canvas;
        Font _font;
        GameObject _panelMain, _panelPlay, _panelMapSelect, _panelHow, _panelSettings, _panelAbout;
        Text _volValueLabel, _diffLabelInMapSelect;

        void Awake()
        {
            GameSession.LoadPrefs();
            AudioListener.volume = GameSession.GetSavedMasterVolume();
            Screen.fullScreen = GameSession.GetSavedFullscreen();
            _font = UiFontHelper.GetFont();

        #if UNITY_EDITOR
            string p = "Assets/_InsectWars/Sprites/UI/Extracted/";
            if (mainFrameSprite == null) mainFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            if (buttonSprite == null) buttonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_menu.png");
            if (separatorSprite == null) separatorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_ornate.png");
            if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_WithTitle.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
if (logoSprite == null) logoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/InsectWarsLogo_Raw.png");
#endif

            // Stop NavMesh errors in Home scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneLoader.HomeScene)
            {
                foreach (var agent in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
                    agent.enabled = false;
                foreach (var unit in Object.FindObjectsByType<InsectUnit>(FindObjectsSortMode.None))
                    unit.enabled = false;
            }

            SetupEventSystem();
            BuildCanvas();
            BuildVideoBackground();
            BuildMainMenu();
            BuildSubPanels();
            ShowMain();
        }

        void SetupEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                var mod = existing.GetComponent<InputSystemUIInputModule>();
                if (mod != null)
                {
                    TryAssignInputActions(mod);
                    return;
                }
                DestroyImmediate(existing.gameObject);
            }
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var newMod = es.AddComponent<InputSystemUIInputModule>();
            TryAssignInputActions(newMod);
        }

        void TryAssignInputActions(InputSystemUIInputModule mod)
        {
        #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (asset != null)
            {
                mod.actionsAsset = asset;
                // Manually link the actions to ensure responsiveness if auto-link fails
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
        #endif
        }

        void BuildCanvas()
        {
            var existing = GameObject.Find("MainMenuCanvas");
            if (existing != null) DestroyImmediate(existing);

            var go = new GameObject("MainMenuCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0; // Ensure it's behind anything else if needed, but Overlay is on top anyway
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildVideoBackground()
        {
            var bg = new GameObject("VideoBackground");
            bg.transform.SetParent(_canvas.transform, false);
            bg.transform.SetAsFirstSibling(); // Ensure it's behind all other UI
            Stretch(bg.AddComponent<RectTransform>());

            var raw = bg.AddComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;

            var vrt = new RenderTexture(1920, 1080, 24);
            vrt.Create();
            raw.texture = vrt;

            var vgo = new GameObject("VideoPlayer");
            vgo.transform.SetParent(bg.transform, false);
            var vp = vgo.AddComponent<VideoPlayer>();
            vp.playOnAwake = true;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = vrt;
            vp.aspectRatio = VideoAspectRatio.FitInside;
            vp.source = VideoSource.Url;

            var path = System.IO.Path.Combine(Application.streamingAssetsPath, streamingVideoName);
            if (System.IO.File.Exists(path))
            {
                // Use file:// prefix for absolute paths to ensure compatibility
                vp.url = "file://" + path;
                vp.Prepare();
                vp.prepareCompleted += (p) => {
                    p.Play();
                    Debug.Log("HomeMenu: Video started playing.");
                };
            }
            else
            {
                Debug.LogWarning($"HomeMenu: Video file not found at {path}");
                vp.enabled = false;
            }

            var dim = new GameObject("Dim");
            dim.transform.SetParent(bg.transform, false);
            Stretch(dim.AddComponent<RectTransform>());
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = ColDim;
            dimImg.raycastTarget = false;
        }

        void BuildMainMenu()
        {
            _panelMain = MakePanel("MainPanel");
            var box = DarkBox(_panelMain.transform, PanelW, 850f);

            var titleGo = new GameObject("TitleLogo");
titleGo.transform.SetParent(box.transform, false);
            var titleImg = titleGo.AddComponent<Image>();
            titleImg.sprite = logoSprite;
            titleImg.preserveAspect = true;
            var titleRt = titleGo.GetComponent<RectTransform>();
            AnchorTopCenter(titleRt, new Vector2(0, -30f), new Vector2(650, 260));

            float y = -310f;
            DarkButton(box.transform, "START MISSION", ref y, () => ShowPlay());
DarkButton(box.transform, "CODEX", ref y, () => ShowHow());
            DarkButton(box.transform, "CONFIGURATION", ref y, () => ShowSettings());
            DarkButton(box.transform, "LOGS", ref y, () => ShowAbout());

            MakeSeparator(box.transform, y + 20f, 400f);
            y -= 20f;
            DarkButton(box.transform, "ABANDON", ref y, () => Application.Quit());
        }

        void BuildSubPanels()
        {
            _panelPlay = MakePanel("PlayPanel");
            var boxPlay = DarkBox(_panelPlay.transform, 550, 550);
            PanelHeader(boxPlay.transform, "SELECT DIFFICULTY", -50f);
            float yP = -150f;
            DarkButton(boxPlay.transform, "EASY", ref yP, () => { SetDiff(DemoDifficulty.Easy); ShowMapSelect(); });
            DarkButton(boxPlay.transform, "NORMAL", ref yP, () => { SetDiff(DemoDifficulty.Normal); ShowMapSelect(); });
            DarkButton(boxPlay.transform, "HARD", ref yP, () => { SetDiff(DemoDifficulty.Hard); ShowMapSelect(); });
            DarkButton(boxPlay.transform, "BACK", ref yP, () => ShowMain());

            _panelSettings = MakePanel("SettingsPanel");
            var boxSet = DarkBox(_panelSettings.transform, 550, 550);
            PanelHeader(boxSet.transform, "CONFIGURATION", -50f);
            float yS = -150f;
            DarkButton(boxSet.transform, "TOGGLE FULLSCREEN", ref yS, () => { Screen.fullScreen = !Screen.fullScreen; });
            DarkButton(boxSet.transform, "BACK", ref yS, () => ShowMain());

            _panelAbout = MakePanel("AboutPanel");
            var boxAb = DarkBox(_panelAbout.transform, 650, 500);
            PanelHeader(boxAb.transform, "LOGS", -50f);
            var body = Txt(boxAb.transform, "INSECT WAR\n\nA Unity 6 RTS Vertical Slice.\nBuilt with URP and New Input System.", 20, ColTitle, TextAnchor.UpperCenter);
AnchorTopCenter(body.rectTransform, new Vector2(0, -150f), new Vector2(500, 200));
            float yA = -380f;
            DarkButton(boxAb.transform, "BACK", ref yA, () => ShowMain());

            BuildMapSelectPanel();
            _panelHow = MakePanel("HowPanel");
        }

        void BuildMapSelectPanel()
        {
            _panelMapSelect = MakePanel("MapSelectPanel");
            var box = DarkBox(_panelMapSelect.transform, 750, 750);
            PanelHeader(box.transform, "SELECT MAP", -40f);
            
            var maps = SkirmishMapPresets.GetAll();
            float y = -120f;
            foreach(var m in maps)
            {
                var mapName = m.displayName;
                DarkButton(box.transform, mapName.ToUpper(), ref y, () => {
                    GameSession.SetSelectedMap(m);
                    SceneLoader.LoadSkirmishDemo();
                });
            }
DarkButton(box.transform, "BACK", ref y, () => ShowPlay());
        }

        // ── Helpers ──

        GameObject MakePanel(string name)
        {
            var p = new GameObject(name);
            p.transform.SetParent(_canvas.transform, false);
            Stretch(p.AddComponent<RectTransform>());
            p.SetActive(false);
            return p;
        }

        GameObject DarkBox(Transform parent, float w, float h)
        {
            var go = new GameObject("Box");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            // Image component removed to make the layout container transparent
            return go;
        }

            void PanelHeader(Transform parent, string text, float y)
            {
            var t = Txt(parent, text, 36, ColTitle, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            AnchorTopCenter(t.rectTransform, new Vector2(0, y), new Vector2(500, 50));
        }

        void DarkButton(Transform parent, string label, ref float y, UnityEngine.Events.UnityAction onClick)
        {
            var b = new GameObject(label);
            b.transform.SetParent(parent, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(BtnW, BtnH);
            y -= BtnGap;

            var img = b.AddComponent<Image>();
            img.sprite = buttonSprite;
            img.color = ColWhite;
            img.type = Image.Type.Sliced; // Use Sliced for better button quality
            
            var btn = b.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = new Color(1, 0.9f, 0.7f, 1f);
            cols.pressedColor = new Color(0.8f, 0.7f, 0.5f, 1f);
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var tx = Txt(b.transform, label, BtnFontSize, ColTitle, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            Stretch(tx.rectTransform);
        }

        void MakeSeparator(Transform parent, float y, float width)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(width, 12f);
            var img = go.AddComponent<Image>();
            img.sprite = separatorSprite;
            img.color = ColSub;
        }

        Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.text = text; t.supportRichText = true;
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.06f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
            }

        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        static void AnchorTopCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size; }

        void ShowMain() => SetActivePanels(_panelMain);
        void ShowPlay() => SetActivePanels(_panelPlay);
        void ShowHow() => SetActivePanels(_panelHow);
        void ShowSettings() => SetActivePanels(_panelSettings);
        void ShowAbout() => SetActivePanels(_panelAbout);
        void ShowMapSelect() => SetActivePanels(_panelMapSelect);

        void SetActivePanels(GameObject on)
        {
            if (_panelMain) _panelMain.SetActive(_panelMain == on);
            if (_panelPlay) _panelPlay.SetActive(_panelPlay == on);
            if (_panelMapSelect) _panelMapSelect.SetActive(_panelMapSelect == on);
            if (_panelHow) _panelHow.SetActive(_panelHow == on);
            if (_panelSettings) _panelSettings.SetActive(_panelSettings == on);
            if (_panelAbout) _panelAbout.SetActive(_panelAbout == on);
        }

        void SetDiff(DemoDifficulty d) => GameSession.SetDifficulty(d);
    }
}
