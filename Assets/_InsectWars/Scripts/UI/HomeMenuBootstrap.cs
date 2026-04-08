using System.Text;
using InsectWars.Core;
using InsectWars.Data;
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

        // ───────── How To Play with Unit Codex ─────────

        Text _unitDetailText;
        readonly UnitArchetype[] _codexOrder =
            { UnitArchetype.Worker, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
        int _codexIndex;

        void BuildHowToPlayPanel(Transform parent, Font font)
        {
            AddTitle(parent, "How To Play", font, 32);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.02f, 0.04f);
            scrollRt.anchorMax = new Vector2(0.98f, 0.88f);
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(20, 20, 10, 20);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            AddSectionLabel(contentGo.transform, font, "CONTROLS", 20, new Color(1f, 0.95f, 0.55f));
            AddBodyText(contentGo.transform, font,
                "LMB / drag — select units\n" +
                "RMB ground — move selected\n" +
                "RMB enemy — attack\n" +
                "RMB fruit (with workers) — gather nectar\n" +
                "Workers auto-return nectar to hive\n" +
                "Edge-pan / MMB drag — scroll camera · Scroll — zoom\n" +
                "Escape — pause/unpause\n" +
                "Hotkeys: M move · A attack-move · S stop · H hold · P patrol · B build menu\n" +
                "Win: eliminate all enemies · Lose: all your units die",
                15, new Color(0.88f, 0.9f, 0.92f));

            AddSectionLabel(contentGo.transform, font, "UNIT CODEX", 20, new Color(1f, 0.95f, 0.55f));

            var tabRow = new GameObject("UnitTabs");
            tabRow.transform.SetParent(contentGo.transform, false);
            var tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;
            tabLayout.spacing = 8f;
            var tabFitter = tabRow.AddComponent<LayoutElement>();
            tabFitter.preferredHeight = 42f;

            foreach (var arch in _codexOrder)
            {
                var archCopy = arch;
                var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
                AddTabButton(tabRow.transform, font, def.displayName.ToUpperInvariant(),
                    () => ShowCodexUnit(archCopy));
            }

            var detailGo = new GameObject("UnitDetail");
            detailGo.transform.SetParent(contentGo.transform, false);
            _unitDetailText = detailGo.AddComponent<Text>();
            _unitDetailText.font = font;
            _unitDetailText.fontSize = 15;
            _unitDetailText.lineSpacing = 1.1f;
            _unitDetailText.supportRichText = true;
            _unitDetailText.color = new Color(0.92f, 0.93f, 0.95f);
            _unitDetailText.alignment = TextAnchor.UpperLeft;
            _unitDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _unitDetailText.verticalOverflow = VerticalWrapMode.Overflow;
            var dlayout = detailGo.AddComponent<LayoutElement>();
            dlayout.preferredHeight = 440f;
            dlayout.flexibleWidth = 1f;

            ShowCodexUnit(UnitArchetype.Worker);

            var backY = -1f;
            var backParent = parent;
            var backGo = new GameObject("Back");
            backGo.transform.SetParent(backParent, false);
            var backRt = backGo.AddComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 12f);
            backRt.sizeDelta = new Vector2(280, 44);
            var bimg = backGo.AddComponent<Image>();
            bimg.color = new Color(0.2f, 0.35f, 0.22f, 0.95f);
            var bbtn = backGo.AddComponent<Button>();
            bbtn.onClick.AddListener(() => ShowMain());
            var btx = new GameObject("T").AddComponent<Text>();
            btx.transform.SetParent(backGo.transform, false);
            btx.font = font;
            btx.fontSize = 20;
            btx.color = Color.white;
            btx.alignment = TextAnchor.MiddleCenter;
            btx.text = "Back";
            var btrt = btx.rectTransform;
            btrt.anchorMin = Vector2.zero;
            btrt.anchorMax = Vector2.one;
            btrt.offsetMin = btrt.offsetMax = Vector2.zero;
        }

        void ShowCodexUnit(UnitArchetype arch)
        {
            if (_unitDetailText == null) return;
            var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
            var sb = new StringBuilder(1024);

            sb.AppendLine($"<b><size=22><color=#8CFFA8>{def.displayName.ToUpperInvariant()}</color></size></b>");
            sb.AppendLine($"<i>{ArchetypeFlavorText(arch)}</i>");
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>VISION</color></b>");
            sb.AppendLine($"  Sight radius .............. {def.visionRadius:F1} m");
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>VITALITY</color></b>");
            sb.AppendLine($"  Max HP .................... {def.maxHealth:F0}");
            sb.AppendLine($"  Move speed ................ {def.moveSpeed:F1}");
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>COMBAT</color></b>");
            sb.AppendLine($"  Damage / hit .............. {def.attackDamage:F1}");
            sb.AppendLine($"  Attack cooldown ........... {def.attackCooldown:F2} s");
            sb.AppendLine($"  DPS ....................... ~{def.attackDamage / Mathf.Max(0.05f, def.attackCooldown):F1}");
            sb.AppendLine($"  Range ..................... {def.attackRange:F1} m  ({(arch == UnitArchetype.BasicRanged ? "ranged projectile" : "melee")})");
            sb.AppendLine($"  Can gather ................ {(def.canGather ? "Yes" : "No")}");
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>HIT BOX</color></b>");
            AppendHitboxInfo(arch, sb);
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>ACTIONS</color></b>");
            AppendActions(arch, sb);
            sb.AppendLine();

            sb.AppendLine("<b><color=#FFE87A>ANIMATIONS</color></b>");
            AppendAnimations(arch, sb);

            _unitDetailText.text = sb.ToString();
        }

        static string ArchetypeFlavorText(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker =>
                "Tireless forager. Gathers nectar from rotting fruit and returns it to the hive. Weak in combat but essential for your economy.",
            UnitArchetype.BasicFighter =>
                "Armored melee brawler. Closes distance fast and locks onto targets with scythe-arms. High damage up close, no ranged capability.",
            UnitArchetype.BasicRanged =>
                "Ranged bombardier. Launches homing acid projectiles from distance. Fragile if flanked, but deadly in groups behind a frontline.",
            _ => ""
        };

        static void AppendHitboxInfo(UnitArchetype arch, StringBuilder sb)
        {
            switch (arch)
            {
                case UnitArchetype.Worker:
                    sb.AppendLine("  Capsule: center (0, 0.45, 0)  r=0.32  h=0.95");
                    sb.AppendLine("  NavAgent: h=0.92  r=0.30");
                    break;
                case UnitArchetype.BasicFighter:
                    sb.AppendLine("  Capsule: center (0, 0.22, 0)  r=0.38  h=0.55");
                    sb.AppendLine("  NavAgent: h=0.50  r=0.42");
                    break;
                case UnitArchetype.BasicRanged:
                    sb.AppendLine("  Capsule: center (0, 0.55, 0)  r=0.28  h=1.10");
                    sb.AppendLine("  NavAgent: h=1.12  r=0.27");
                    break;
            }
            sb.AppendLine("  Layer: Units");
        }

        static void AppendActions(UnitArchetype arch, StringBuilder sb)
        {
            sb.AppendLine("  Move (RMB ground)");
            sb.AppendLine("  Attack-move (A + LMB)");
            sb.AppendLine("  Attack unit (RMB on enemy)");
            sb.AppendLine("  Stop (S) / Hold position (H)");
            sb.AppendLine("  Patrol (P, click start then end)");
            if (arch == UnitArchetype.Worker)
            {
                sb.AppendLine("  <color=#FFD966>Gather</color> (RMB on Rotting Fruit)");
                sb.AppendLine("  Auto-returns nectar to hive when full");
            }
        }

        static void AppendAnimations(UnitArchetype arch, StringBuilder sb)
        {
            sb.AppendLine("  Pipeline: procedural (or Animator if prefab has controller)");
            sb.AppendLine("  Move: sinusoidal Y bob while walking");
            sb.AppendLine("  Attack: 0.35s lunge + squash/stretch + arm pitch");
            switch (arch)
            {
                case UnitArchetype.BasicFighter:
                    sb.AppendLine("  Idle: 30s mantis loop — head look, scythe maintenance L/R, head dip, tail sway");
                    sb.AppendLine("  Bones driven: frontleg, R_frontleg, chest, head, tail");
                    break;
                case UnitArchetype.Worker:
                case UnitArchetype.BasicRanged:
                    sb.AppendLine("  Idle: subtle chest-breath scale pulse");
                    break;
            }
            sb.AppendLine("  Death: scale-to-zero shrink (or Animator Death trigger)");
            sb.AppendLine("  Animator params (when controller assigned):");
            sb.AppendLine("    Speed (float) · IsMoving (bool) · Gathering (bool)");
            sb.AppendLine("    Attack (trigger) · Death (trigger)");
        }

        void AddSectionLabel(Transform parent, Font font, string text, int size, Color color)
        {
            var go = new GameObject("Section");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.text = text;
            t.supportRichText = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = size + 12;
        }

        void AddBodyText(Transform parent, Font font, string text, int size, Color color)
        {
            var go = new GameObject("Body");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.UpperLeft;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = text.Split('\n').Length * (size + 4) + 10;
            le.flexibleWidth = 1f;
        }

        void AddTabButton(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.3f, 0.2f, 0.92f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;
            le.flexibleWidth = 1f;
            var tx = new GameObject("T").AddComponent<Text>();
            tx.transform.SetParent(go.transform, false);
            tx.font = font;
            tx.fontSize = 17;
            tx.fontStyle = FontStyle.Bold;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = label;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
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
                if (c.name is "MainPanel" or "PlayPanel" or "HowPanel" or "SettingsPanel" or "AboutPanel" or "UnitCodexPanel")
                    c.gameObject.SetActive(c.gameObject == on);
            }
        }

        void SetDiff(DemoDifficulty d)
        {
            GameSession.SetDifficulty(d);
        }
    }
}
