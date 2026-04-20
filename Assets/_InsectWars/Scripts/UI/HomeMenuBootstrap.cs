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
        [SerializeField] VideoClip backgroundClip;
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
        const float BtnW = 520f, BtnH = 72f, BtnGap = 80f;
        const int TitleSize = 48, SubSize = 15, BtnFontSize = 22;

        Canvas _canvas;
        Font _font;
        GameObject _panelMain, _panelPlay, _panelMapSelect, _panelHow, _panelLearning, _panelSettings;
        SpotlightPanel _spotlightPanel;
        GameObject _loadingScreen;
        CanvasGroup _loadingCanvasGroup;
        Text _volValueLabel, _qualValueLabel, _fsValueLabel, _diffLabelInMapSelect;
        float _fadeTimer = -1f;
        int _revealCountdown = -1;
        const float FadeDuration = 0.5f;
        const int FramesToWaitAfterPlay = 3;

        void Awake()
        {
            GameSession.LoadPrefs();
            AudioListener.volume = GameSession.GetSavedMasterVolume();
            Screen.fullScreen = GameSession.GetSavedFullscreen();
            _font = UiFontHelper.GetFont();

            if (mainFrameSprite == null) mainFrameSprite = RTS.GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");
            if (buttonSprite == null) buttonSprite = RTS.GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");
            if (separatorSprite == null) separatorSprite = RTS.GameHUD.LoadSpriteFromResources("UI/Extracted/frame_ornate");
            if (logoSprite == null) logoSprite = RTS.GameHUD.LoadSpriteFromResources("UI/InsectWarsLogo_WithTitle");
            if (logoSprite == null) logoSprite = RTS.GameHUD.LoadSpriteFromResources("UI/InsectWarsLogo_Raw");

            SetupEventSystem();
            BuildCanvas();
            BuildLoadingScreen();
            BuildVideoBackground();
            BuildMainMenu();
            BuildSubPanels();

            var systems = new GameObject("Systems");
            systems.AddComponent<GameAudio>();
            }


        void Update()
        {
            if (_revealCountdown > 0)
            {
                _revealCountdown--;
                if (_revealCountdown == 0)
                {
                    ShowMain();
                    _fadeTimer = 0f;
                    _revealCountdown = -1;
                }
            }

            if (_fadeTimer < 0f) return;
            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / FadeDuration);
            if (_loadingCanvasGroup != null)
                _loadingCanvasGroup.alpha = 1f - t;
            if (t >= 1f)
            {
                if (_loadingScreen != null) _loadingScreen.SetActive(false);
                _fadeTimer = -1f;
            }
        }

        void OnVideoReady()
        {
            _revealCountdown = FramesToWaitAfterPlay;
        }

        void BuildLoadingScreen()
        {
            _loadingScreen = new GameObject("LoadingScreen");
            _loadingScreen.transform.SetParent(_canvas.transform, false);
            var rt = _loadingScreen.AddComponent<RectTransform>();
            Stretch(rt);
            _loadingCanvasGroup = _loadingScreen.AddComponent<CanvasGroup>();
            _loadingCanvasGroup.blocksRaycasts = true;

            var bg = new GameObject("BG");
            bg.transform.SetParent(_loadingScreen.transform, false);
            Stretch(bg.AddComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            if (logoSprite != null)
            {
                var logo = new GameObject("Logo");
                logo.transform.SetParent(_loadingScreen.transform, false);
                var logoImg = logo.AddComponent<Image>();
                logoImg.sprite = logoSprite;
                logoImg.preserveAspect = true;
                logoImg.raycastTarget = false;
                var logoRt = logo.GetComponent<RectTransform>();
                logoRt.anchorMin = logoRt.anchorMax = logoRt.pivot = new Vector2(0.5f, 0.5f);
                logoRt.anchoredPosition = new Vector2(0, 40f);
                logoRt.sizeDelta = new Vector2(500, 200);
            }

            var loadingTxt = Txt(_loadingScreen.transform, "LOADING...", 24, ColSub, TextAnchor.MiddleCenter);
            var txtRt = loadingTxt.rectTransform;
            txtRt.anchorMin = txtRt.anchorMax = txtRt.pivot = new Vector2(0.5f, 0.5f);
            txtRt.anchoredPosition = new Vector2(0, -100f);
            txtRt.sizeDelta = new Vector2(300, 40);

            _loadingScreen.transform.SetAsLastSibling();
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
                Destroy(existing.gameObject);
            }
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var newMod = es.AddComponent<InputSystemUIInputModule>();
            TryAssignInputActions(newMod);
        }

        void TryAssignInputActions(InputSystemUIInputModule mod)
        {
            var asset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            #if UNITY_EDITOR
            if (asset == null)
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            #endif
            if (asset != null)
            {
                mod.actionsAsset = asset;
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
        }

        void BuildCanvas()
        {
            var existing = GameObject.Find("MainMenuCanvas");
            if (existing != null) Destroy(existing);

            var go = new GameObject("MainMenuCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;
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
            bg.transform.SetAsFirstSibling();
            Stretch(bg.AddComponent<RectTransform>());

            var raw = bg.AddComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;

            var vrt = new RenderTexture(1920, 1080, 24);
            vrt.Create();
            var prev = RenderTexture.active;
            RenderTexture.active = vrt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
            raw.texture = vrt;

            var vgo = new GameObject("VideoPlayer");
            vgo.transform.SetParent(bg.transform, false);
            var vp = vgo.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.skipOnDrop = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = vrt;
            vp.aspectRatio = VideoAspectRatio.FitInside;
            
            // Setup audio output
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vp.controlledAudioTrackCount = 1; 
            var vAsrc = vgo.AddComponent<AudioSource>();
            vAsrc.playOnAwake = false;
            vAsrc.volume = 1f;
            vAsrc.spatialBlend = 0f;
            vp.SetTargetAudioSource(0, vAsrc);

            if (backgroundClip != null)
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = backgroundClip;
                vp.Prepare();
                vp.prepareCompleted += (p) => 
                { 
                    p.EnableAudioTrack(0, true);
                    p.Play(); 
                    OnVideoReady(); 
                };
                vp.loopPointReached += (p) => { p.Pause(); };
                var freeze = vgo.AddComponent<VideoFreezeBeforeEnd>();
                freeze.player = vp;
                freeze.videoAudio = vAsrc;
                freeze.freezeBeforeEnd = 1.0; 
                freeze.fadeDuration = 1.0f;
            }
            else
            {
                Debug.LogWarning("HomeMenu: Video clip is not assigned.");
                vp.enabled = false;
                OnVideoReady();
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
            DarkButton(box.transform, "TUTORIAL", ref y, () =>
            {
                GameSession.SetLearningMode(true);
                GameSession.SetTutorialMode(true);
                GameSession.SetSelectedMap(SkirmishMapPresets.GetTutorialMap());
                SceneLoader.LoadSkirmishDemo();
            });
            DarkButton(box.transform, "START MISSION", ref y, () => ShowPlay());
            DarkButton(box.transform, "PLAY-GROUND", ref y, () => ShowLearning());
            DarkButton(box.transform, "SETTINGS", ref y, () => ShowSettings());

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
            var boxSet = DarkBox(_panelSettings.transform, 600, 580);
            PanelHeader(boxSet.transform, "SETTINGS", -50f);

            float yS = -130f;
            const float rowH = 55f;
            const float rowGap = 70f;

            float curVol = GameSession.GetSavedMasterVolume();
            _volValueLabel = SettingsRow(boxSet.transform, "MASTER VOLUME", Mathf.RoundToInt(curVol * 100) + "%", ref yS, rowH, rowGap,
                () => { AdjustVolume(-0.05f); },
                () => { AdjustVolume(0.05f); });

            bool curFs = GameSession.GetSavedFullscreen();
            _fsValueLabel = SettingsRow(boxSet.transform, "FULLSCREEN", curFs ? "ON" : "OFF", ref yS, rowH, rowGap,
                () => { ToggleFullscreen(); },
                () => { ToggleFullscreen(); });

            int curQ = GameSession.GetSavedQuality();
            string qName = QualitySettings.names.Length > curQ ? QualitySettings.names[curQ] : curQ.ToString();
            _qualValueLabel = SettingsRow(boxSet.transform, "QUALITY", qName.ToUpper(), ref yS, rowH, rowGap,
                () => { AdjustQuality(-1); },
                () => { AdjustQuality(1); });

            yS -= 10f;
            DarkButton(boxSet.transform, "BACK", ref yS, () => ShowMain());

            BuildMapSelectPanel();
            BuildLearningPanel();
            BuildHowToPlayPanel();
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
                    GameSession.SetLearningMode(false);
                    GameSession.SetSelectedMap(m);
                    SceneLoader.LoadSkirmishDemo();
                });
            }
            DarkButton(box.transform, "BACK", ref y, () => ShowPlay());
        }

        void BuildLearningPanel()
        {
            _panelLearning = MakePanel("LearningPanel");
            var box = DarkBox(_panelLearning.transform, 550, 550);
            PanelHeader(box.transform, "PLAY-GROUND", -50f);
            float y = -150f;
            DarkButton(box.transform, "SANDBOX MAP", ref y, () =>
            {
                GameSession.SetLearningMode(true);
                GameSession.SetTutorialMode(false);
                GameSession.SetSelectedMap(SkirmishMapPresets.GetLearningMap());
                SceneLoader.LoadSkirmishDemo();
            });
            DarkButton(box.transform, "SPOTLIGHT", ref y, () => ShowSpotlight());
            DarkButton(box.transform, "BACK", ref y, () => ShowMain());

            _spotlightPanel = SpotlightPanel.Create(
                _canvas.transform, visualLibrary, _font, buttonSprite, () => ShowLearning());
        }

        void BuildHowToPlayPanel()
        {
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
            img.type = Image.Type.Sliced;
            
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
        void ShowLearning() => SetActivePanels(_panelLearning);
        void ShowSpotlight() => SetActivePanels(_spotlightPanel != null ? _spotlightPanel.gameObject : null);
        void ShowSettings() => SetActivePanels(_panelSettings);
        void ShowMapSelect() => SetActivePanels(_panelMapSelect);

        void SetActivePanels(GameObject on)
        {
            if (_panelMain) _panelMain.SetActive(_panelMain == on);
            if (_panelPlay) _panelPlay.SetActive(_panelPlay == on);
            if (_panelMapSelect) _panelMapSelect.SetActive(_panelMapSelect == on);
            if (_panelHow) _panelHow.SetActive(_panelHow == on);
            if (_panelLearning) _panelLearning.SetActive(_panelLearning == on);
            if (_spotlightPanel) _spotlightPanel.gameObject.SetActive(_spotlightPanel.gameObject == on);
            if (_panelSettings) _panelSettings.SetActive(_panelSettings == on);
        }

        Text SettingsRow(Transform parent, string label, string initialValue, ref float y,
            float rowH, float rowGap,
            UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight)
        {
            const float labelW = 280f;
            const float valueW = 140f;
            const float arrowW = 52f;

            var row = new GameObject(label + "Row");
            row.transform.SetParent(parent, false);
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0, y);
            rowRt.sizeDelta = new Vector2(labelW + arrowW + valueW + arrowW + 16f, rowH);
            y -= rowGap;

            var lbl = Txt(row.transform, label, 20, ColTitle, TextAnchor.MiddleLeft);
            lbl.fontStyle = FontStyle.Bold;
            var lblRt = lbl.rectTransform;
            lblRt.anchorMin = new Vector2(0, 0); lblRt.anchorMax = new Vector2(0, 1);
            lblRt.pivot = new Vector2(0, 0.5f);
            lblRt.anchoredPosition = new Vector2(0, 0);
            lblRt.sizeDelta = new Vector2(labelW, 0);

            float cx = labelW;
            ArrowButton(row.transform, "<", cx, arrowW, rowH, onLeft);
            cx += arrowW + 4f;

            var val = Txt(row.transform, initialValue, 20, ColSub, TextAnchor.MiddleCenter);
            val.fontStyle = FontStyle.Bold;
            var valRt = val.rectTransform;
            valRt.anchorMin = new Vector2(0, 0); valRt.anchorMax = new Vector2(0, 1);
            valRt.pivot = new Vector2(0, 0.5f);
            valRt.anchoredPosition = new Vector2(cx, 0);
            valRt.sizeDelta = new Vector2(valueW, 0);
            cx += valueW + 4f;

            ArrowButton(row.transform, ">", cx, arrowW, rowH, onRight);

            return val;
        }

        void ArrowButton(Transform parent, string symbol, float x, float w, float h,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(symbol + "Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(w, 0);

            var img = go.AddComponent<Image>();
            img.sprite = buttonSprite;
            img.color = ColWhite;
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = new Color(1, 0.9f, 0.7f, 1f);
            cols.pressedColor = new Color(0.8f, 0.7f, 0.5f, 1f);
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var tx = Txt(go.transform, symbol, BtnFontSize, ColTitle, TextAnchor.MiddleCenter);
            tx.fontStyle = FontStyle.Bold;
            Stretch(tx.rectTransform);
        }

        void AdjustVolume(float delta)
        {
            float vol = Mathf.Clamp01(GameSession.GetSavedMasterVolume() + delta);
            GameSession.SetMasterVolume(vol);
            if (_volValueLabel) _volValueLabel.text = Mathf.RoundToInt(vol * 100) + "%";
        }

        void ToggleFullscreen()
        {
            bool fs = !Screen.fullScreen;
            GameSession.SetFullscreen(fs);
            if (_fsValueLabel) _fsValueLabel.text = fs ? "ON" : "OFF";
        }

        void AdjustQuality(int delta)
        {
            int cur = QualitySettings.GetQualityLevel();
            int next = Mathf.Clamp(cur + delta, 0, QualitySettings.names.Length - 1);
            GameSession.SetQualityLevel(next);
            if (_qualValueLabel) _qualValueLabel.text = QualitySettings.names[next].ToUpper();
        }

        void SetDiff(DemoDifficulty d) => GameSession.SetDifficulty(d);
    }

    public class VideoFreezeBeforeEnd : MonoBehaviour
    {
        public UnityEngine.Video.VideoPlayer player;
        public AudioSource videoAudio;
        public double freezeBeforeEnd = 0.4;
        public float fadeDuration = 1.0f;

        bool _transitionStarted = false;

        void Update()
        {
            if (player == null || !player.isPrepared) return;

            // If video is almost done
            if (!_transitionStarted && player.isPlaying && player.time >= player.length - freezeBeforeEnd)
            {
                StartTransition();
            }
            else if (!_transitionStarted && !player.isPlaying)
            {
                // Fallback if player stopped for any other reason
                StartTransition();
            }
        }

        void StartTransition()
        {
            if (_transitionStarted) return;
            _transitionStarted = true;

            StartCoroutine(FadeOutAndFreeze());
            GameAudio.PlayMenuMusic(fadeDuration);
        }

        System.Collections.IEnumerator FadeOutAndFreeze()
        {
            float elapsed = 0f;
            float startVol = videoAudio != null ? videoAudio.volume : 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                if (videoAudio != null) videoAudio.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            if (videoAudio != null) videoAudio.volume = 0f;
            player.Pause();
        }
    }
}
