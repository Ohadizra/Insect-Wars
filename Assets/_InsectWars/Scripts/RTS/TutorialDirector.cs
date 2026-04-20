using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

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
        static readonly Color ColHighlight = new(1f, 0.82f, 0.15f, 1f);
        static readonly Color ColPopupBg = new(0.14f, 0.12f, 0.10f, 0.96f);

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

        // ── Chapter state ──
        int _startingCalories;
        int _startingWorkerCount;
        int _startingFighterCount;

        // Ch4 army requirements
        const int ReqMantis = 1;
        const int ReqBombardiers = 1;
        bool _ccPopupShown;
        bool _ccPopupDismissed;
        GameObject _ccPopup;

        // ── Highlight system ──
        readonly List<HighlightEntry> _highlights = new();
        float _highlightPulse;
        GameObject _worldArrow;
        Transform _worldArrowTarget;
        Vector3 _worldArrowOffset = new(0f, 4f, 0f);

        struct HighlightEntry
        {
            public string uiNameMatch;
            public GameObject frame;
        }

        void Awake() { Instance = this; }

        void Start()
        {
            BuildChapters();
            BuildTracker();
            BuildHud();
            CreateWorldArrow();
            BeginChapter(0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ClearHighlights();
            if (_worldArrow != null) Destroy(_worldArrow);
        }

        // ═══════════════════════════════════════════════════════════
        //  Chapter Definitions
        // ═══════════════════════════════════════════════════════════

        void BuildChapters()
        {
            _chapters = new[]
            {
                new TutorialChapter(
                    "Train a Worker",
                    "Every colony starts with its Nest.\n\n" +
                    "Your first task is to produce a <b>Worker</b> ant.\n" +
                    "<b>Click</b> on the Ant's Nest (your main base),\n" +
                    "then press the <b>Worker</b> button to begin training.",
                    "Click the Ant's Nest, then train a Worker.",
                    SetupTrainWorkerChapter,
                    () => CountPlayerUnits(UnitArchetype.Worker) > _startingWorkerCount
                ),
                new TutorialChapter(
                    "Gather Resources",
                    "Workers collect Calories from rotting apples.\n\n" +
                    "<b>Left-click</b> a Worker to select it,\n" +
                    "then <b>right-click</b> the apple to send it gathering.\n" +
                    "Calories fuel everything in your colony.",
                    "Select a Worker, then right-click the apple.",
                    SetupGatherChapter,
                    () => PlayerResources.Instance != null && PlayerResources.Instance.Calories > _startingCalories
                ),
                new TutorialChapter(
                    "Build an Underground",
                    "Military buildings let you train combat units.\n\n" +
                    "<b>Select</b> a Worker, click the <b>Build</b> button,\n" +
                    "then choose <b>Underground</b> and place it near the hive.\n" +
                    "The worker will construct it automatically.",
                    "Select a Worker, open Build menu, place an Underground.",
                    SetupBuildUndergroundChapter,
                    IsUndergroundBuilt
                ),
                new TutorialChapter(
                    "Build Your Army",
                    "Time to raise a fighting force!\n\n" +
                    $"Train <b>{ReqMantis} Mantis</b> and <b>{ReqBombardiers} Bombardier</b>.\n" +
                    "Select the <b>Underground</b> to train combat units.",
                    GetArmyObjective(),
                    SetupArmyChapter,
                    IsArmyComplete
                ),
                new TutorialChapter(
                    "Attack!",
                    "Enemy scouts have been spotted nearby!\n\n" +
                    "<b>Select</b> your combat units,\n" +
                    "then <b>right-click</b> the enemies to attack.\n" +
                    "Destroy them all to prove your command!",
                    "Select your army and right-click enemies to attack!",
                    SetupAttackChapter,
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
                    UpdateChapterHighlights();
                    UpdateDynamicObjective();
                    CheckCCPopup();
                    if (_current != null && _current.IsComplete())
                        OnChapterComplete();
                    break;
                case Phase.Complete:
                    _completeTimer -= Time.deltaTime;
                    if (_completeTimer <= 0f) AdvanceChapter();
                    break;
            }
            UpdateHighlightFrames();
            UpdateWorldArrow();
        }

        // ── Chapter Flow ──

        void BeginChapter(int index)
        {
            _currentIndex = index;
            _current = _chapters[index];
            _phase = Phase.Intro;
            _ccPopupShown = false;
            _ccPopupDismissed = false;
            ClearHighlights();
            SetWorldArrowTarget(null);
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
            ClearHighlights();
            SetWorldArrowTarget(null);
            if (_ccPopup != null) { Destroy(_ccPopup); _ccPopup = null; }
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

        // ═══════════════════════════════════════════════════════════
        //  Chapter Setup
        // ═══════════════════════════════════════════════════════════

        void SetupTrainWorkerChapter()
        {
            _startingWorkerCount = CountPlayerUnits(UnitArchetype.Worker);
            if (PlayerResources.Instance != null) PlayerResources.Instance.AddCalories(200);
        }

        void SetupGatherChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();
            _startingCalories = PlayerResources.Instance != null ? PlayerResources.Instance.Calories : 0;
        }

        void SetupBuildUndergroundChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();
            if (PlayerResources.Instance != null) PlayerResources.Instance.AddCalories(600);
        }

        void SetupArmyChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();
            var worker = SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            if (worker != null) worker.OrderStop();

            ProductionBuilding.Place(hiveXZ + new Vector3(8f, 0f, -8f), BuildingType.Underground, Team.Player, startBuilt: true);

            if (PlayerResources.Instance != null) PlayerResources.Instance.AddCalories(800);

            _ccPopupShown = false;
            _ccPopupDismissed = false;
        }

        void SetupAttackChapter()
        {
            var hiveXZ = GetPlayerHiveXZ();

            // Spawn the player's army from Ch4
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(3f, 0f, 0f), Team.Player, UnitArchetype.BasicFighter);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(-3f, 0f, 0f), Team.Player, UnitArchetype.BasicFighter);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(0f, 0f, 3f), Team.Player, UnitArchetype.BasicFighter);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(6f, 0f, 3f), Team.Player, UnitArchetype.BasicRanged);
            SkirmishDirector.SpawnUnit(hiveXZ + new Vector3(-6f, 0f, 3f), Team.Player, UnitArchetype.BasicRanged);

            // Spawn enemy group at a distance
            var enemyPos = hiveXZ + new Vector3(20f, 0f, 20f);
            for (int i = 0; i < 3; i++)
            {
                var offset = new Vector3(i * 3f - 3f, 0f, 0f);
                var enemy = SkirmishDirector.SpawnUnit(enemyPos + offset, Team.Enemy, UnitArchetype.BasicFighter);
                if (enemy != null)
                {
                    var def = UnitDefinition.CreateRuntimeDefault(UnitArchetype.BasicFighter,
                        TeamPalette.UnitBody(Team.Enemy, UnitArchetype.BasicFighter));
                    def.maxHealth = 120f;
                    enemy.Configure(Team.Enemy, def);
                    enemy.gameObject.AddComponent<TrainingDummy>();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Completion Checks
        // ═══════════════════════════════════════════════════════════

        static bool IsUndergroundBuilt()
        {
            foreach (var bld in ProductionBuilding.All)
                if (bld != null && bld.IsAlive && !bld.IsUnderConstruction
                    && bld.Team == Team.Player && bld.Type == BuildingType.Underground)
                    return true;
            return false;
        }

        bool IsArmyComplete()
        {
            return CountPlayerUnits(UnitArchetype.BasicFighter) >= ReqMantis
                && CountPlayerUnits(UnitArchetype.BasicRanged) >= ReqBombardiers;
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
        //  Dynamic Objectives & Highlights per Chapter
        // ═══════════════════════════════════════════════════════════

        void UpdateChapterHighlights()
        {
            if (_phase != Phase.Playing) return;

            switch (_currentIndex)
            {
                case 0: UpdateCh1Highlights(); break;
                case 1: UpdateCh2Highlights(); break;
                case 2: UpdateCh3Highlights(); break;
                case 3: UpdateCh4Highlights(); break;
                case 4: UpdateCh5Highlights(); break;
            }
        }

        void UpdateCh1Highlights()
        {
            var hive = HiveDeposit.PlayerHive;
            var sc = SelectionController.Instance;
            bool hiveSelected = sc != null && sc.SelectedHive != null;

            if (!hiveSelected)
            {
                SetWorldArrowTarget(hive != null ? hive.transform : null);
                ClearHighlights();
            }
            else
            {
                SetWorldArrowTarget(null);
                SetHighlight("Worker");
            }
        }

        void UpdateCh2Highlights()
        {
            var sc = SelectionController.Instance;
            bool hasWorker = sc != null && sc.HasWorkerSelected();

            if (!hasWorker)
            {
                ClearHighlights();
                var worker = FindFirstPlayerUnit(UnitArchetype.Worker);
                SetWorldArrowTarget(worker != null ? worker.transform : null);
            }
            else
            {
                SetWorldArrowTarget(FindNearestApple());
            }
        }

        void UpdateCh3Highlights()
        {
            var sc = SelectionController.Instance;
            bool hasWorker = sc != null && sc.HasWorkerSelected();

            if (!hasWorker)
            {
                ClearHighlights();
                var worker = FindFirstPlayerUnit(UnitArchetype.Worker);
                SetWorldArrowTarget(worker != null ? worker.transform : null);
                return;
            }

            SetWorldArrowTarget(null);
            if (BottomBar.Instance != null)
            {
                bool buildMenuOpen = BottomBar.Pending == PendingCommand.PlaceBuilding;
                if (buildMenuOpen)
                    ClearHighlights();
                else if (IsBuildMenuOpen())
                    SetHighlight("Underground");
                else
                    SetHighlight("Build");
            }
        }

        void UpdateCh4Highlights()
        {
            var sc = SelectionController.Instance;
            bool bldSel = sc != null && sc.SelectedBuilding != null;

            if (!bldSel)
            {
                int mantis = CountPlayerUnits(UnitArchetype.BasicFighter);
                int bombardiers = CountPlayerUnits(UnitArchetype.BasicRanged);

                ClearHighlights();

                if (mantis < ReqMantis || bombardiers < ReqBombardiers)
                {
                    var underground = FindPlayerBuilding(BuildingType.Underground);
                    SetWorldArrowTarget(underground != null ? underground.transform : null);
                }
                else
                {
                    SetWorldArrowTarget(null);
                }
            }
            else
            {
                SetWorldArrowTarget(null);
                int mantis = CountPlayerUnits(UnitArchetype.BasicFighter);
                if (mantis < ReqMantis)
                    SetHighlight("Mantis");
                else
                    SetHighlight("Beetle");
            }
        }

        void UpdateCh5Highlights()
        {
            ClearHighlights();
            InsectUnit enemy = null;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u != null && u.IsAlive && u.Team == Team.Enemy)
                { enemy = u; break; }
            }
            if (enemy != null)
                SetWorldArrowTarget(enemy.transform);
            else
                SetWorldArrowTarget(null);
        }

        void UpdateDynamicObjective()
        {
            if (_objectiveLabel == null || _current == null) return;

            if (_currentIndex == 0)
            {
                var sc = SelectionController.Instance;
                bool hiveSelected = sc != null && sc.SelectedHive != null;
                _objectiveLabel.text = hiveSelected
                    ? "Now click the Worker button to train one!"
                    : "Click on the Ant's Nest (your base).";
            }
            else if (_currentIndex == 2)
            {
                var sc = SelectionController.Instance;
                bool hasWorker = sc != null && sc.HasWorkerSelected();
                if (!hasWorker)
                    _objectiveLabel.text = "Select a Worker first.";
                else if (IsBuildMenuOpen())
                    _objectiveLabel.text = "Click Underground to start placing it.";
                else if (BottomBar.Pending == PendingCommand.PlaceBuilding)
                    _objectiveLabel.text = "Click a spot near the hive to place the building.";
                else if (HasUndergroundUnderConstruction())
                    _objectiveLabel.text = "Good! Wait for construction to finish.";
                else
                    _objectiveLabel.text = "Select a Worker, then click Build (B).";
            }
            else if (_currentIndex == 3)
            {
                _objectiveLabel.text = GetArmyObjective();
            }
        }

        string GetArmyObjective()
        {
            int m = CountPlayerUnits(UnitArchetype.BasicFighter);
            int b = CountPlayerUnits(UnitArchetype.BasicRanged);
            return $"Mantis: {Mathf.Min(m, ReqMantis)}/{ReqMantis}  |  " +
                   $"Bombardiers: {Mathf.Min(b, ReqBombardiers)}/{ReqBombardiers}";
        }

        void CheckCCPopup()
        {
            if (_currentIndex != 3 || _ccPopupDismissed) return;

            int used = ColonyCapacity.GetUsed(Team.Player) + ColonyCapacity.GetQueued(Team.Player);
            int cap = ColonyCapacity.GetCap(Team.Player);

            if (used >= cap && !_ccPopupShown)
            {
                _ccPopupShown = true;
                ShowCCPopup();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Helpers — finding game objects
        // ═══════════════════════════════════════════════════════════

        static InsectUnit FindFirstPlayerUnit(UnitArchetype arch)
        {
            foreach (var u in RtsSimRegistry.Units)
                if (u != null && u.IsAlive && u.Team == Team.Player && u.Archetype == arch)
                    return u;
            return null;
        }

        static Transform FindNearestApple()
        {
            var fruits = Object.FindObjectsByType<RottingFruitNode>(FindObjectsSortMode.None);
            if (fruits.Length == 0) return null;
            return fruits[0].transform;
        }

        static ProductionBuilding FindPlayerBuilding(BuildingType type)
        {
            foreach (var b in ProductionBuilding.All)
                if (b != null && b.IsAlive && b.Team == Team.Player && b.Type == type)
                    return b;
            return null;
        }

        static bool IsBuildMenuOpen()
        {
            if (BottomBar.Instance == null) return false;
            var grid = FindCmdGrid();
            if (grid == null) return false;
            foreach (Transform child in grid)
            {
                if (child.name.Contains("Underground") || child.name.Contains("Sky Tower"))
                    return true;
            }
            return false;
        }

        static bool HasUndergroundUnderConstruction()
        {
            foreach (var b in ProductionBuilding.All)
                if (b != null && b.IsAlive && b.IsUnderConstruction
                    && b.Team == Team.Player && b.Type == BuildingType.Underground)
                    return true;
            return false;
        }

        static Transform FindCmdGrid()
        {
            var canvas = GameHUD.HudCanvasRect;
            if (canvas == null) return null;
            return FindChildRecursive(canvas, "CmdGrid");
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  Highlight System — UI button glow frames
        // ═══════════════════════════════════════════════════════════

        void SetHighlight(string nameContains)
        {
            if (_highlights.Count == 1 && _highlights[0].uiNameMatch == nameContains)
                return;
            ClearHighlights();

            var canvas = GameHUD.HudCanvasRect;
            if (canvas == null) return;

            var frame = CreateHighlightFrame(canvas);
            _highlights.Add(new HighlightEntry { uiNameMatch = nameContains, frame = frame });
        }

        void ClearHighlights()
        {
            foreach (var h in _highlights)
                if (h.frame != null) Destroy(h.frame);
            _highlights.Clear();
        }

        void UpdateHighlightFrames()
        {
            if (_highlights.Count == 0) return;

            _highlightPulse += Time.unscaledDeltaTime * 4f;
            float alpha = 0.55f + 0.45f * Mathf.Sin(_highlightPulse);

            var grid = FindCmdGrid();

            for (int i = 0; i < _highlights.Count; i++)
            {
                var entry = _highlights[i];
                if (entry.frame == null) continue;

                RectTransform targetRt = null;

                if (grid != null)
                {
                    foreach (Transform child in grid)
                    {
                        if (child.name.Contains(entry.uiNameMatch))
                        {
                            targetRt = child as RectTransform;
                            break;
                        }
                    }
                }

                if (targetRt == null)
                {
                    entry.frame.SetActive(false);
                    continue;
                }

                var canvasRect = GameHUD.HudCanvasRect;
                if (canvasRect == null) { entry.frame.SetActive(false); continue; }

                entry.frame.SetActive(true);
                var frt = entry.frame.GetComponent<RectTransform>();

                Vector3[] corners = new Vector3[4];
                targetRt.GetWorldCorners(corners);

                Vector2 min, max;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out max);

                frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.anchoredPosition = (min + max) * 0.5f;
                frt.sizeDelta = (max - min) + new Vector2(12f, 12f);

                var img = entry.frame.GetComponent<Image>();
                img.color = new Color(ColHighlight.r, ColHighlight.g, ColHighlight.b, alpha * 0.85f);
            }
        }

        GameObject CreateHighlightFrame(RectTransform parent)
        {
            var go = new GameObject("HighlightFrame");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 90f);

            var img = go.AddComponent<Image>();
            img.color = ColHighlight;
            img.raycastTarget = false;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(go.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(4f, 4f);
            irt.offsetMax = new Vector2(-4f, -4f);
            var iimg = inner.AddComponent<Image>();
            iimg.color = new Color(0f, 0f, 0f, 0f);
            iimg.raycastTarget = false;

            go.SetActive(false);
            return go;
        }

        // ═══════════════════════════════════════════════════════════
        //  Highlight System — World-space arrow
        // ═══════════════════════════════════════════════════════════

        void CreateWorldArrow()
        {
            _worldArrow = new GameObject("TutorialArrow");
            _worldArrow.transform.SetParent(transform);

            var lineGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineGo.name = "ArrowBody";
            lineGo.transform.SetParent(_worldArrow.transform, false);
            lineGo.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
            lineGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var col = lineGo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var tipGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipGo.name = "ArrowTip";
            tipGo.transform.SetParent(_worldArrow.transform, false);
            tipGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            tipGo.transform.localPosition = Vector3.zero;
            tipGo.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var tipCol = tipGo.GetComponent<Collider>();
            if (tipCol != null) Destroy(tipCol);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default"));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", ColHighlight);
            else if (mat.HasProperty("_Color"))
                mat.color = ColHighlight;

            foreach (var r in _worldArrow.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;

            _worldArrow.SetActive(false);
        }

        void SetWorldArrowTarget(Transform target)
        {
            _worldArrowTarget = target;
        }

        void UpdateWorldArrow()
        {
            if (_worldArrow == null) return;

            if (_worldArrowTarget == null || !_worldArrowTarget.gameObject.activeInHierarchy)
            {
                _worldArrow.SetActive(false);
                return;
            }

            _worldArrow.SetActive(true);
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 0.5f;
            _worldArrow.transform.position = _worldArrowTarget.position + _worldArrowOffset + new Vector3(0f, bob, 0f);
        }

        // ═══════════════════════════════════════════════════════════
        //  CC Popup (Chapter 4)
        // ═══════════════════════════════════════════════════════════

        void ShowCCPopup()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            _ccPopup = new GameObject("CCPopup");
            _ccPopup.transform.SetParent(canvasRect, false);
            var rt = _ccPopup.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 270);

            var bg = _ccPopup.AddComponent<Image>();
            bg.color = ColPopupBg;
            bg.raycastTarget = true;

            var title = MakeLabel(_ccPopup.transform, "Title",
                "Colony Capacity Full!", 32,
                FontStyle.Bold, ColHighlight, TextAnchor.MiddleCenter);
            AnchorFill(title, new Vector2(0f, 0.68f), new Vector2(1f, 0.93f));

            var body = MakeLabel(_ccPopup.transform, "Body",
                "Build an <b>Ant's Nest</b> (+25 CC) or\n" +
                "<b>Root Cellar</b> (+15 CC) to increase\nyour Colony Capacity.",
                22, FontStyle.Normal, ColPanelWhite, TextAnchor.MiddleCenter);
            AnchorFill(body, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.68f));

            var btn = MakeGreenButton(_ccPopup.transform, "OkBtn", "GOT IT!", () =>
            {
                _ccPopupDismissed = true;
                if (_ccPopup != null) { Destroy(_ccPopup); _ccPopup = null; }
            });
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.09f);
            brt.sizeDelta = new Vector2(220, 54);
        }

        // ═══════════════════════════════════════════════════════════
        //  UI — Progress Tracker  (top-right, below settings button)
        // ═══════════════════════════════════════════════════════════

        void BuildTracker()
        {
            var canvasRect = GameHUD.HudCanvasRect;
            if (canvasRect == null) return;

            float rowH = 42f;
            float headerH = 40f;
            float pad = 14f;
            float panelH = headerH + _chapters.Length * rowH + pad * 2;

            _trackerPanel = new GameObject("TutorialTracker");
            _trackerPanel.transform.SetParent(canvasRect, false);
            var rt = _trackerPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -105f);
            rt.sizeDelta = new Vector2(340f, panelH);

            var bg = _trackerPanel.AddComponent<Image>();
            bg.color = ColTrackerBg;
            bg.raycastTarget = false;

            var header = MakeLabel(_trackerPanel.transform, "Header", "PROGRESS",
                20, FontStyle.Bold, ColMuted, TextAnchor.MiddleCenter);
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
                    20, FontStyle.Bold, ColGray, TextAnchor.MiddleCenter);
                var crt = check.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(12f, yPos);
                crt.sizeDelta = new Vector2(42f, rowH);
                _trackerChecks[i] = check;

                var lbl = MakeLabel(_trackerPanel.transform, $"L{i}",
                    $"{i + 1}. {_chapters[i].Title}", 20,
                    FontStyle.Normal, ColGray, TextAnchor.MiddleLeft);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 1f);
                lrt.anchorMax = new Vector2(1f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(54f, yPos);
                lrt.sizeDelta = new Vector2(-66f, rowH);
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
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(700f, 100f);

            var bg = _hudPanel.AddComponent<Image>();
            bg.color = ColHudBg;
            bg.raycastTarget = false;

            _titleLabel = MakeLabel(_hudPanel.transform, "Title", "", 28,
                FontStyle.Bold, ColTitle, TextAnchor.MiddleCenter);
            var titleRt = _titleLabel.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-16f, -5f);

            _objectiveLabel = MakeLabel(_hudPanel.transform, "Obj", "", 20,
                FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            var objRt = _objectiveLabel.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 0f);
            objRt.anchorMax = new Vector2(1f, 0.5f);
            objRt.offsetMin = new Vector2(16f, 5f);
            objRt.offsetMax = new Vector2(-16f, 0f);

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
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.55f);
            prt.sizeDelta = new Vector2(780, 420);
            var pImg = panel.AddComponent<Image>();
            pImg.color = ColPanelWhite;

            var numTxt = MakeLabel(panel.transform, "Num",
                $"Chapter {_currentIndex + 1} of {_chapters.Length}",
                21, FontStyle.Normal, ColMuted, TextAnchor.MiddleCenter);
            AnchorFill(numTxt, new Vector2(0f, 0.91f), new Vector2(1f, 0.97f));

            var titleTxt = MakeLabel(panel.transform, "Title",
                _current.Title, 44, FontStyle.Bold, ColTitle, TextAnchor.MiddleCenter);
            AnchorFill(titleTxt, new Vector2(0f, 0.76f), new Vector2(1f, 0.91f));

            MakeSepLine(panel.transform, 0.74f);

            var bodyTxt = MakeLabel(panel.transform, "Body",
                _current.IntroText, 24, FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            AnchorFill(bodyTxt, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.72f));

            var btnGo = MakeGreenButton(panel.transform, "BeginBtn", "BEGIN", () => StartPlaying());
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.08f);
            brt.sizeDelta = new Vector2(260, 60);
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
            prt.sizeDelta = new Vector2(620, 240);
            panel.AddComponent<Image>().color = ColPanelWhite;

            bool isLast = _currentIndex + 1 >= _chapters.Length;

            var done = MakeLabel(panel.transform, "Done", "COMPLETE!", 42,
                FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
            AnchorFill(done, new Vector2(0f, 0.50f), new Vector2(1f, 0.90f));

            string subMsg = isLast
                ? "All chapters finished!"
                : $"Next: Chapter {_currentIndex + 2} - {_chapters[_currentIndex + 1].Title}";
            var sub = MakeLabel(panel.transform, "Sub", subMsg, 22,
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
            ClearHighlights();
            SetWorldArrowTarget(null);
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
            prt.sizeDelta = new Vector2(740, 540);
            panel.AddComponent<Image>().color = ColPanelWhite;

            var title = MakeLabel(panel.transform, "Title", "TUTORIAL COMPLETE", 46,
                FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
            AnchorFill(title, new Vector2(0f, 0.84f), new Vector2(1f, 0.96f));

            MakeSepLine(panel.transform, 0.82f);

            for (int i = 0; i < _chapters.Length; i++)
            {
                float rowTop = 0.76f - i * 0.09f;
                float rowBot = rowTop - 0.08f;

                var mark = MakeLabel(panel.transform, $"M{i}", "[X]", 22,
                    FontStyle.Bold, ColGreen, TextAnchor.MiddleCenter);
                AnchorFill(mark, new Vector2(0.06f, rowBot), new Vector2(0.14f, rowTop));

                var lbl = MakeLabel(panel.transform, $"D{i}",
                    $"Chapter {i + 1}: {_chapters[i].Title}", 24,
                    FontStyle.Normal, ColTitle, TextAnchor.MiddleLeft);
                AnchorFill(lbl, new Vector2(0.16f, rowBot), new Vector2(0.94f, rowTop));
            }

            MakeSepLine(panel.transform, 0.28f);

            var sub = MakeLabel(panel.transform, "Sub",
                "You've learned the basics.\nYou're ready for real battle!", 24,
                FontStyle.Normal, ColBody, TextAnchor.MiddleCenter);
            AnchorFill(sub, new Vector2(0f, 0.12f), new Vector2(1f, 0.26f));

            var btnGo = MakeGreenButton(panel.transform, "MenuBtn", "RETURN TO MENU", () =>
            {
                GameSession.SetTutorialMode(false);
                Time.timeScale = 1f;
                SceneLoader.LoadHome();
            });
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.05f);
            brt.sizeDelta = new Vector2(320, 60);
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
            outline.effectColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

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

            var lbl = MakeLabel(go.transform, "Lbl", label, 26,
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
