using InsectWars.Core;
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

        Canvas _canvas;
        GameObject _panelMain;
        GameObject _panelPlay;
        GameObject _panelHow;
        GameObject _panelSettings;
        GameObject _panelAbout;
        Text _volValueLabel;
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
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
            AddTitle(_panelPlay.transform, "Play Demo", v, 32);
            AddLabel(_panelPlay.transform, "Difficulty affects enemy durability.", v, -90f);
            float y = -140f;
            AddMenuButton(_panelPlay.transform, "Easy", v, ref y, () => SetDiff(DemoDifficulty.Easy));
            AddMenuButton(_panelPlay.transform, "Normal", v, ref y, () => SetDiff(DemoDifficulty.Normal));
            AddMenuButton(_panelPlay.transform, "Hard", v, ref y, () => SetDiff(DemoDifficulty.Hard));
            AddMenuButton(_panelPlay.transform, "Start Skirmish", v, ref y, () => SceneLoader.LoadSkirmishDemo());
            AddMenuButton(_panelPlay.transform, "Back", v, ref y, () => ShowMain());

            _panelHow = Panel("HowPanel", _canvas.transform);
            AddTitle(_panelHow.transform, "How To Play", v, 32);
            AddMultiline(_panelHow.transform, "Select your units with left-click or drag a box.\nRight-click ground to move.\nRight-click enemies to attack.\nSelect workers and right-click Rotting Fruit to gather nectar.\nWorkers auto-return to the hive when full.\nEdge-pan or MMB drag to move the camera. Scroll to zoom.\nEscape returns to this menu from the demo.", v, -70f);
            y = -320f;
            AddMenuButton(_panelHow.transform, "Back", v, ref y, () => ShowMain());

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
            AddMultiline(_panelAbout.transform, "Insect Wars — Demo 0\n\nUnity 6 RTS vertical slice.\nPlaceholder units and economy.\n\nFuture: ECS, flow fields, lockstep.", v, -70f);
            y = -280f;
            AddMenuButton(_panelAbout.transform, "Back", v, ref y, () => ShowMain());
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
                if (c.name is "MainPanel" or "PlayPanel" or "HowPanel" or "SettingsPanel" or "AboutPanel")
                    c.gameObject.SetActive(c.gameObject == on);
            }
        }

        void SetDiff(DemoDifficulty d)
        {
            GameSession.SetDifficulty(d);
        }
    }
}
