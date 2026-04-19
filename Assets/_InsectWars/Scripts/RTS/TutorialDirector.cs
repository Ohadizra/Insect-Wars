using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        // White-panel palette
        static readonly Color ColPanelWhite = new(0.97f, 0.96f, 0.93f, 1f);
        static readonly Color ColTitle = new(0.12f, 0.10f, 0.08f);
        static readonly Color ColBody = new(0.22f, 0.20f, 0.18f);
        static readonly Color ColMuted = new(0.45f, 0.42f, 0.38f);
        static readonly Color ColGreen = new(0.15f, 0.62f, 0.22f);
        static readonly Color ColGold = new(0.72f, 0.58f, 0.10f);
        static readonly Color ColGray = new(0.55f, 0.52f, 0.48f);
        static readonly Color ColSep = new(0.82f, 0.80f, 0.75f, 1f);
        static readonly Color ColBtnBg = new(0.18f, 0.50f, 0.22f, 1f);
        static readonly Color ColBtnHover = new(0.22f, 0.60f, 0.28f, 1f);
        static readonly Color ColBtnPress = new(0.14f, 0.40f, 0.18f, 1f);
        static readonly Color ColBtnText = Color.white;
        static readonly Color ColDimBg = new(0f, 0f, 0f, 0.55f);
        static readonly Color ColHudBg = new(0.97f, 0.96f, 0.93f, 0.95f);
        static readonly Color ColTrackerBg = new(0.97f, 0.96f, 0.93f, 0.92f);
        static readonly Color ColNone = new(0f, 0f, 0f, 0f);

        enum Phase { Intro, Playing, Complete, Finished }

        TutorialChapter[] _chapters;
        int _currentIndex = -1;
        TutorialChapter _current;
        Phase _phase = Phase.Intro;

        GameObject _trackerPanel;
        Text[] _trackerLabels;
        Text[] _trackerChecks;

        GameObject _hudPanel;
        Text _titleLabel;
        Text _objectiveLabel;

        GameObject _introOverlay;
        GameObject _completeOverlay;
        float _completeTimer;
        const float CompleteShowDuration = 2.5f;

        float _graceTimer;
        const float GraceDuration = 1f;

        int _startingCalories;
        int _startingFighterCount;

        void Awake() { Instance = this; }

        void Start()
        {
            BuildChapters();
            BuildTracker();
            BuildHud();
            BeginChapter(0);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void BuildChapters()
        {
            _chapters = new[]
            {
                new TutorialChapter(
                    "Gathering Resources",
                    "Every colony needs food to survive.\n\n" +
                    "In this lesson you will learn how to send a Worker ant " +
                    "to collect Calories from a rotting apple " +
                    "and bring them back to the hive.\n\n" +
                    "<b>Left-click</b> the worker to select it.\n" +
                    "<b>Right-click</b> the apple to send it gathering.",
                    "Select the worker, then right-click the apple.",
                    SetupGatheringChapter,
                    () => PlayerResources.Instance != null && PlayerResources.Instance.Calories > _startingCalories
                ),
                new TutorialChapter(
                    "Constructing Buildings",
                    "A strong colony needs structures.\n\n" +
                    "You will learn how to place an Ant Nest building " +
                    "and have your worker construct it. " +
                    "Buildings let you train new units and grow your colony.\n\n" +
                    "<b>Select</b> the worker, then click the <b>Ant Nest</b> button " +
                    "in the bottom bar and place it near the hive.",
                    "Place an Ant Nest near the hive and wait for it to finish.",
                    SetupBuildingChapter,
                    IsAntNestBuilt
                ),
                new TutorialChapter(
                    "Training Units",
                    "Time to grow your army.\n\n" +
                    "You will learn how to produce combat units " +
                    "from a military building. " +
                    "The Underground building can train Mantis fighters.\n\n" +
                    "<b>Select</b> the Underground building, then click " +
                    "the <b>Mantis</b> icon to queue a fighter.",
                    "Select the Underground and queue a Mantis fighter.",
                    SetupTrainingChapter,
                    () => CountPlayerUnits(UnitArchetype.BasicFighter) > _startingFighterCount
                ),
                new TutorialChapter(
                    "Combat",
                    "Your soldiers are ready for battle.\n\n" +
                    "You will learn how to command your units " +
                    "to attack an enemy target. " +
                    "Destroy the enemy training dummy to complete the tutorial!\n\n" +
                    "<b>Select</b> your fighters, then <b>right-click</b> the enemy.",
                    "Select your fighters and right-click the enemy to attack.",
                    SetupCombatChapter,
                    IsEnemyDummyDestroyed
                ),
            };
        }

        // ── Main Loop ──

        void Update()
        {
            switch (_phase)
            {
                case Phase.Playing:
                    if (_graceTimer > 0f) { _graceTimer -= Time.deltaTime; return; }
                    if (_current != null && _current.IsComplete())
                        OnChapterComplete();
                    break;
                case Phase.Complete:
                    _completeTimer -= Time.deltaTime;
                    if (_completeTimer <= 0f) AdvanceChapter();
                    break;
            }
        }

        // ── Chapter Flow ──

        void BeginChapter(int index)
        {
            _currentIndex = index;
            _current = _chapters[index];
            _phase = Phase.Intro;
            UpdateTracker();
            ShowIntroOverlay();
        }

        void StartPlaying()
        {
            if (_introOverlay != null) { Destroy(_introOverlay); _introOverlay = null; }
            Time.timeScale = 1f;
            _current.SetupWorld();
            _graceTimer = GraceDuration;
            _phase = Phase.Playing;
            if (_hudPanel != null) _hudPanel.SetActive(true);
            UpdateHud();
        }

        void OnChapterComplete()
        {
            _phase = Phase.Complete;
            MarkChapterDone(_currentIndex);
            GameAudio.PlayUi(GameAudio.UiKind.MatchVictory);
            if (_hudPanel != null) _hudPanel.SetActive(false);
            ShowCompleteOverlay();
        }

        void AdvanceChapter()
        {
            if (_completeOverlay != null) { Destroy(_completeOverlay); _completeOverlay = null; }
            int next = _currentIndex + 1;
            if (next >= _chapters.Length) { ShowTutorialFinished(); return; }
            RebuildWorldForNextChapter(next);
        }

        void RebuildWorldForNextChapter(int nextIndex)
        {
            var director = FindFirstObjectByType<SkirmishDirector>();
            if (director != null)
            {
                _hudPanel = null; _titleLabel = null; _objectiveLabel = null;
                _trackerPanel = null; _trackerLabels = null; _trackerChecks = null;
                director.RebuildWorldForTutorial();
                BuildTracker();
                BuildHud();
            }
            BeginChapter(nextIndex);
        }

        // ── Chapter Setup ──

        void SetupGatheringChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();
            _startingCalories = PlayerResources.Instance != null ? PlayerResources.Instance.Calories : 0;
        }

        void SetupBuildingChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();
            if (PlayerResources.Instance != null) PlayerResources.Instance.AddCalories(500);
        }

        void SetupTrainingChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();
            ProductionBuilding.Place(hiveXZ + new Vector3(8f, 0f, -8f), BuildingType.Underground, Team.Player, startBuilt: true);
            if (PlayerResources.Instance != null) PlayerResources.Instance.AddCalories(300);
            _startingFighterCount = CountPlayerUnits(UnitArchetype.BasicFighter);
        }

        void SetupCombatChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 0f), Team.Player, UnitArchetype.BasicFighter);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(-3f, 0f, 0f), Team.Player, UnitArchetype.BasicFighter);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(0f, 0f, 3f), Team.Player, UnitArchetype.BasicRanged);

            var dummyPos = hiveXZ + new Vector3(18f, 0f, 18f);
            var dummyUnit = SkirmishDirector.SpawnUnit(dummyPos, Team.Enemy, UnitArchetype.BasicFighter);
            if (dummyUnit != null)
            {
                var def = UnitDefinition.CreateRuntimeDefault(UnitArchetype.BasicFighter,
                    TeamPalette.UnitBody(Team.Enemy, UnitArchetype.BasicFighter));
                def.maxHealth = 200f;
                dummyUnit.Configure(Team.Enemy, def);
                dummyUnit.gameObject.AddComponent<TrainingDummy>();
            }
        }

        // ── Completion Checks ──

        static bool IsAntNestBuilt()
        {
            foreach (var bld in ProductionBuilding.All)
                if (bld != null && bld.IsAlive && !bld.IsUnderConstruction
                    && bld.Team == Team.Player && bld.Type == BuildingType.AntNest)
                    return true;
            return false;
        }

        static bool IsEnemyDummyDestroyed()
        {
            foreach (var u in RtsSimRegistry.Units)
                if (u != null && u.IsAlive && u.Team == Team.Enemy)
                    return false;
            return true;
        }

        static int CountPlayerUnits(UnitArchetype arch)
        {
            int count = 0;
            foreach (var u in RtsSimRegistry.Units)
                if (u != null && u.IsAlive && u.Team == Team.Player && u.Archetype == arch)
                    count++;
            return count;
        }

        static Vector3 GetPlayerHiveXZ()
        {
            var hive = HiveDeposit.PlayerHive;
            if (hive != null) return new Vector3(hive.transform.position.x, 0f, hive.transform.position.z);
            return Vector3.zero;
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Progress Tracker  (top-right)
        // ═══════════════════════════════════════════════════════════

        void BuildTracker()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            float rowH = 36f;
            float headerH = 34f;
            float pad = 12f;
            float panelH = headerH + _chapters.Length * rowH + pad * 2;

            _trackerPanel = new GameObject("TutorialTracker");
            _trackerPanel.transform.SetParent(canvasRect, false);
            var rt = _trackerPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12f, -100f);
            rt.sizeDelta = new Vector2(280f, panelH);

            var bg = _trackerPanel.AddComponent<Image>();
            bg.color = ColTrackerBg;
            bg.raycastTarget = false;

            var header = MakeLabel(_trackerPanel.transform, "Header", "PROGRESS",
                16, FontStyle.Bold, ColMuted, TextAnchor.MiddleCenter);
            var hrt = header.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -pad);
            hrt.sizeDelta = new Vector2(0f, headerH);

            _trackerChecks = new Text[_chapters.Length];
            _trackerLabels = new Text[_chapters.Length];

            for (int i = 0; i < _chapters.Length; i++)
            {
                float yPos = -(pad + headerH + i * rowH);

                var check = MakeLabel(_trackerPanel.transform, $"C{i}", "[ ]",
                    16, FontStyle.Bold, ColGray, TextAnchor.MiddleCenter);
                var crt = check.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(10f, yPos);
                crt.sizeDelta = new Vector2(36f, rowH);
                _trackerChecks[i] = check;

                var lbl = MakeLabel(_trackerPanel.transform, $"L{i}",
                    $"{i + 1}. {_chapters[i].Title}", 16,
                    FontStyle.Normal, ColGray, TextAnchor.MiddleLeft);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 1f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(46f, yPos);
                lrt.sizeDelta = new Vector2(-56f, rowH);
                _trackerLabels[i] = lbl;
            }
        }

        void UpdateTracker()
        {
            if (_trackerLabels == null) return;
            for (int i = 0; i < _chapters.Length; i++)
            {
                if (_trackerChecks[i] == null) continue;
                if (i < _currentIndex)
                {
                    _trackerChecks[i].text = "[X]";
                    _trackerChecks[i].color = ColGreen;
                    _trackerLabels[i].color = ColGreen;
                    _trackerLabels[i].fontStyle = FontStyle.Normal;
                }
                else if (i == _currentIndex)
                {
                    _trackerChecks[i].text = " > ";
                    _trackerChecks[i].color = ColGold;
                    _trackerLabels[i].color = ColTitle;
                    _trackerLabels[i].fontStyle = FontStyle.Bold;
                }
                else
                {
                    _trackerChecks[i].text = "[ ]";
                    _trackerChecks[i].color = ColGray;
                    _trackerLabels[i].color = ColGray;
                    _trackerLabels[i].fontStyle = FontStyle.Normal;
                }
            }
        }

        void MarkChapterDone(int index)
        {
            if (_trackerChecks == null || index >= _trackerChecks.Length) return;
            if (_trackerChecks[index] != null)
            {
                _trackerChecks[index].text = "[X]";
                _trackerChecks[index].color = ColGreen;
            }
            if (_trackerLabels != null && index < _trackerLabels.Length && _trackerLabels[index] != null)
            {
                _trackerLabels[index].color = ColGreen;
                _trackerLabels[index].fontStyle = FontStyle.Normal;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Objective HUD  (top-center during gameplay)
        // ═══════════════════════════════════════════════════════════

        void BuildHud()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            _hudPanel = new GameObject("TutorialHUD");
            _hudPanel.transform.SetParent(canvasRect, false);
            var rt = _hudPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(580f, 80f);

            var bg = _hudPanel.AddComponent<Image>();
            bg.color = ColHudBg;
            bg.raycastTarget = false;

            _titleLabel = MakeLabel(_hudPanel.transform, "Title", "", 22,
                FontStyle.Bold, ColTitle, TextAnchor.MiddleCenter);
            var titleRt = _titleLabel.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(14f, 0f);
            titleRt.offsetMax = new Vector2(-14f, -4f);

            _objectiveLabel = MakeLabel(_hudPanel.transform, "Obj", "", 16,
                FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            var objRt = _objectiveLabel.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 0f);
            objRt.anchorMax = new Vector2(1f, 0.5f);
            objRt.offsetMin = new Vector2(14f, 4f);
            objRt.offsetMax = new Vector2(-14f, 0f);

            _hudPanel.SetActive(false);
        }

        void UpdateHud()
        {
            if (_titleLabel != null && _current != null)
                _titleLabel.text = $"Chapter {_currentIndex + 1}: {_current.Title}";
            if (_objectiveLabel != null && _current != null)
                _objectiveLabel.text = _current.ObjectiveText;
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Intro Overlay
        // ═══════════════════════════════════════════════════════════

        void ShowIntroOverlay()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) { StartPlaying(); return; }

            Time.timeScale = 0f;

            _introOverlay = new GameObject("TutorialIntro");
            _introOverlay.transform.SetParent(canvasRect, false);
            StretchRT(_introOverlay.AddComponent<RectTransform>());
            var dim = _introOverlay.AddComponent<Image>();
            dim.color = ColDimBg;
            dim.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_introOverlay.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(640, 440);
            var pImg = panel.AddComponent<Image>();
            pImg.color = ColPanelWhite;

            // Chapter number
            var numTxt = MakeLabel(panel.transform, "Num",
                $"Chapter {_currentIndex + 1} of {_chapters.Length}",
                17, FontStyle.Normal, ColMuted, TextAnchor.MiddleCenter);
            AnchorFill(numTxt, new Vector2(0f, 0.90f), new Vector2(1f, 0.97f));

            // Title
            var titleTxt = MakeLabel(panel.transform, "Title",
                _current.Title, 36, FontStyle.Bold, ColTitle, TextAnchor.MiddleCenter);
            AnchorFill(titleTxt, new Vector2(0f, 0.76f), new Vector2(1f, 0.90f));

            // Separator line
            MakeSepLine(panel.transform, 0.74f);

            // Body text
            var bodyTxt = MakeLabel(panel.transform, "Body",
                _current.IntroText, 19, FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            AnchorFill(bodyTxt, new Vector2(0.07f, 0.22f), new Vector2(0.93f, 0.72f));

            // BEGIN button
            var btnGo = MakeGreenButton(panel.transform, "BeginBtn", "BEGIN", () => StartPlaying());
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.09f);
            brt.sizeDelta = new Vector2(220, 52);
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Chapter Complete Overlay
        // ═══════════════════════════════════════════════════════════

        void ShowCompleteOverlay()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            _completeOverlay = new GameObject("ChapterDone");
            _completeOverlay.transform.SetParent(canvasRect, false);
            StretchRT(_completeOverlay.AddComponent<RectTransform>());
            var dim = _completeOverlay.AddComponent<Image>();
            dim.color = ColDimBg;
            dim.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_completeOverlay.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(520, 200);
            panel.AddComponent<Image>().color = ColPanelWhite;

            bool isLast = _currentIndex + 1 >= _chapters.Length;

            var done = MakeLabel(panel.transform, "Done", "COMPLETE!", 34,
                FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
            AnchorFill(done, new Vector2(0f, 0.50f), new Vector2(1f, 0.90f));

            string subMsg = isLast
                ? "All chapters finished!"
                : $"Next: Chapter {_currentIndex + 2} - {_chapters[_currentIndex + 1].Title}";
            var sub = MakeLabel(panel.transform, "Sub", subMsg, 18,
                FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            AnchorFill(sub, new Vector2(0f, 0.10f), new Vector2(1f, 0.48f));

            _completeTimer = CompleteShowDuration;
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Tutorial Finished
        // ═══════════════════════════════════════════════════════════

        void ShowTutorialFinished()
        {
            _current = null;
            _phase = Phase.Finished;
            if (_hudPanel != null) _hudPanel.SetActive(false);

            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            var root = new GameObject("TutorialFinished");
            root.transform.SetParent(canvasRect, false);
            StretchRT(root.AddComponent<RectTransform>());
            root.AddComponent<Image>().color = ColDimBg;
            root.GetComponent<Image>().raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(620, 460);
            panel.AddComponent<Image>().color = ColPanelWhite;

            // Title
            var title = MakeLabel(panel.transform, "Title", "TUTORIAL COMPLETE", 38,
                FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
            AnchorFill(title, new Vector2(0f, 0.82f), new Vector2(1f, 0.96f));

            MakeSepLine(panel.transform, 0.80f);

            // Achievement list
            for (int i = 0; i < _chapters.Length; i++)
            {
                float rowTop = 0.74f - i * 0.10f;
                float rowBot = rowTop - 0.09f;

                var mark = MakeLabel(panel.transform, $"M{i}", "[X]", 18,
                    FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
                AnchorFill(mark, new Vector2(0.06f, rowBot), new Vector2(0.14f, rowTop));

                var lbl = MakeLabel(panel.transform, $"D{i}",
                    $"Chapter {i + 1}: {_chapters[i].Title}", 19,
                    FontStyle.Normal, ColTitle, TextAnchor.MiddleLeft);
                AnchorFill(lbl, new Vector2(0.16f, rowBot), new Vector2(0.94f, rowTop));
            }

            MakeSepLine(panel.transform, 0.32f);

            var sub = MakeLabel(panel.transform, "Sub",
                "You've learned the basics.\nYou're ready for real battle!", 19,
                FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            AnchorFill(sub, new Vector2(0f, 0.16f), new Vector2(1f, 0.30f));

            var btnGo = MakeGreenButton(panel.transform, "MenuBtn", "RETURN TO MENU", () =>
            {
                GameSession.SetTutorialMode(false);
                Time.timeScale = 1f;
                SceneLoader.LoadHome();
            });
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.06f);
            brt.sizeDelta = new Vector2(280, 54);
        }

        // ═══════════════════════════════════════════════════════════
        //  UI Helpers
        // ═══════════════════════════════════════════════════════════

        Text MakeLabel(Transform parent, string name, string content, int fontSize,
            FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.font = UiFontHelper.GetFont();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.text = content;
            t.supportRichText = true;
            t.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.3f);
            outline.effectDistance = new Vector2(0.8f, -0.8f);

            return t;
        }

        GameObject MakeGreenButton(Transform parent, string name, string label,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = ColBtnBg;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = ColBtnHover;
            cols.pressedColor = ColBtnPress;
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var lbl = MakeLabel(go.transform, "Lbl", label, 22,
                FontStyle.Bold, ColBtnText, TextAnchor.MiddleCenter);
            StretchRT(lbl.GetComponent<RectTransform>());

            return go;
        }

        void MakeSepLine(Transform parent, float anchorY)
        {
            var sep = new GameObject("Sep");
            sep.transform.SetParent(parent, false);
            var srt = sep.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.08f, anchorY);
            srt.anchorMax = new Vector2(0.92f, anchorY);
            srt.sizeDelta = new Vector2(0f, 1f);
            sep.AddComponent<Image>().color = ColSep;
        }

        static void AnchorFill(Text t, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void StretchRT(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
