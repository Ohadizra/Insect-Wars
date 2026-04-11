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

        [Header("Sketch Style Assets")]
        [SerializeField] Sprite whitePanelSprite;
        [SerializeField] Sprite separatorLineSprite;

        // ── Sketch Palette ──
        static readonly Color ColGold      = new(0.65f, 0.45f, 0.10f); // Darker, more saturated gold for readability
        static readonly Color ColWhite     = new(1f, 1f, 1f, 0.92f);   // Slight transparency to panel
        static readonly Color ColDim       = new(0f, 0f, 0f, 0.50f);
        static readonly Color ColBtnHover  = new(0, 0, 0, 0.08f);
        static readonly Color ColBtnPress  = new(0, 0, 0, 0.15f);

        const float PanelW = 700f, PanelH = 820f;
const float BtnW = 400f, BtnH = 60f, BtnGap = 75f;
        const int TitleSize = 72, SubSize = 20, BtnFontSize = 26;

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

#if UNITY_EDITOR
            if (whitePanelSprite == null) whitePanelSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (separatorLineSprite == null) separatorLineSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/Extracted/bar_hp_xp.png");
#endif

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
            // Clear existing Canvases to ensure replacement
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name.Contains("Menu") || c.name.Contains("Canvas") || c.name.Contains("UI"))
                {
                    c.gameObject.SetActive(false);
                    if (c.name == "MainMenuCanvas") DestroyImmediate(c.gameObject);
                }
            }

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
                raw.color = new Color(0.12f, 0.10f, 0.08f);
                vp.enabled = false;
            }

            var dim = new GameObject("Dim");
            dim.transform.SetParent(bg.transform, false);
            Stretch(dim.AddComponent<RectTransform>());
            dim.AddComponent<Image>().color = ColDim;
        }

        void BuildMainMenu()
        {
            _panelMain = MakePanel("MainPanel");
            var box = SketchBox(_panelMain.transform, PanelW, PanelH);

            var title = Txt(box.transform, "STAGBEETLE", TitleSize, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            AnchorTopCenter(title.rectTransform, new Vector2(0, -100f), new Vector2(500, 100));

            var sub = Txt(box.transform, "COMMAND THE COLONY. CONQUER THE DEPTHS.", SubSize, ColGold, TextAnchor.MiddleCenter);
            sub.fontStyle = FontStyle.Bold;
            AnchorTopCenter(sub.rectTransform, new Vector2(0, -155f), new Vector2(460, 30));

            MakeSeparator(box.transform, -185f, 380f);

            float y = -240f;
            SketchButton(box.transform, "START MISSION", ref y, () => ShowPlay());
            SketchButton(box.transform, "CODEX", ref y, () => ShowHow());
            SketchButton(box.transform, "CONFIGURATION", ref y, () => ShowSettings());
            SketchButton(box.transform, "LOGS", ref y, () => ShowAbout());

            MakeSeparator(box.transform, y + 20f, 380f);
            y -= 20f;
            SketchButton(box.transform, "ABANDON", ref y, () => Application.Quit());
        }

        void BuildSubPanels()
        {
            _panelPlay = MakePanel("PlayPanel");
            var boxPlay = SketchBox(_panelPlay.transform, 500, 500);
            PanelHeader(boxPlay.transform, "SELECT DIFFICULTY", -50f);
            float yP = -150f;
            SketchButton(boxPlay.transform, "EASY", ref yP, () => { SetDiff(DemoDifficulty.Easy); ShowMapSelect(); });
            SketchButton(boxPlay.transform, "NORMAL", ref yP, () => { SetDiff(DemoDifficulty.Normal); ShowMapSelect(); });
            SketchButton(boxPlay.transform, "HARD", ref yP, () => { SetDiff(DemoDifficulty.Hard); ShowMapSelect(); });
            SketchButton(boxPlay.transform, "BACK", ref yP, () => ShowMain());

            _panelSettings = MakePanel("SettingsPanel");
            var boxSet = SketchBox(_panelSettings.transform, 500, 500);
            PanelHeader(boxSet.transform, "CONFIGURATION", -50f);
            float yS = -150f;
            SketchButton(boxSet.transform, "TOGGLE FULLSCREEN", ref yS, () => { Screen.fullScreen = !Screen.fullScreen; });
            SketchButton(boxSet.transform, "BACK", ref yS, () => ShowMain());

            _panelAbout = MakePanel("AboutPanel");
            var boxAb = SketchBox(_panelAbout.transform, 600, 500);
            PanelHeader(boxAb.transform, "LOGS", -50f);
            var body = Txt(boxAb.transform, "STAGBEETLE ODYSSEY\n\nA Unity 6 RTS Vertical Slice.\nBuilt with URP and New Input System.", 18, ColGold, TextAnchor.UpperCenter);
            AnchorTopCenter(body.rectTransform, new Vector2(0, -150f), new Vector2(500, 200));
            float yA = -380f;
            SketchButton(boxAb.transform, "BACK", ref yA, () => ShowMain());

            BuildMapSelectPanel();
            _panelHow = MakePanel("HowPanel"); // Placeholder
        }

        void BuildMapSelectPanel()
        {
            _panelMapSelect = MakePanel("MapSelectPanel");
            var box = SketchBox(_panelMapSelect.transform, 700, 700);
            PanelHeader(box.transform, "SELECT MAP", -40f);
            
            var maps = SkirmishMapPresets.GetAll();
            float y = -120f;
            foreach(var m in maps)
            {
                var mapName = m.displayName;
                SketchButton(box.transform, mapName.ToUpper(), ref y, () => {
                    GameSession.SetSelectedMap(m);
                    SceneLoader.LoadSkirmishDemo(m.name);
                });
            }
            SketchButton(box.transform, "BACK", ref y, () => ShowPlay());
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

        GameObject SketchBox(Transform parent, float w, float h)
        {
            var go = new GameObject("Box");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.sprite = whitePanelSprite;
            img.type = Image.Type.Sliced;
            img.color = ColWhite;
            return go;
        }

        void PanelHeader(Transform parent, string text, float y)
        {
            var t = Txt(parent, text, 32, ColGold, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            AnchorTopCenter(t.rectTransform, new Vector2(0, y), new Vector2(500, 50));
        }

        void SketchButton(Transform parent, string label, ref float y, UnityEngine.Events.UnityAction onClick)
        {
            var b = new GameObject(label);
            b.transform.SetParent(parent, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(BtnW, BtnH);
            y -= BtnGap;

            var img = b.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.01f); 
            var btn = b.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = Color.clear;
            cols.highlightedColor = ColBtnHover;
            cols.pressedColor = ColBtnPress;
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var tx = Txt(b.transform, label, BtnFontSize, ColGold, TextAnchor.MiddleCenter);
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
            rt.sizeDelta = new Vector2(width, 10f);
            var img = go.AddComponent<Image>();
            img.sprite = separatorLineSprite;
            img.color = ColGold;
        }

        Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.text = text; t.supportRichText = true;
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.2f);
            outline.effectDistance = new Vector2(1, -1);
            
            return t;
        }

        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        static void AnchorTopCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
        static void AnchorBottomCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = pos; rt.sizeDelta = size; }

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
        void CycleQuality() => GameSession.SetQualityLevel((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
    }
}
