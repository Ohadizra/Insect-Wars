using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public enum PendingCommand
    {
        None,
        Move,
        Attack,
        Patrol,
        Gather,
        PlaceBuilding
    }

    public class BottomBar : MonoBehaviour
    {
        public static BottomBar Instance { get; private set; }

        public static RectTransform MinimapHost { get; private set; }
        public static PendingCommand Pending { get; private set; }
        public static BuildingType PendingBuildingType { get; private set; }

        [SerializeField] float barHeight = 263.5f;
        [SerializeField] float minimapSlot = 357f;
        [SerializeField] float commandPanelWidth = 527f;

        [Header("Natural Ornate Assets")]
        [SerializeField] Sprite barBackground;
        [SerializeField] Sprite minimapFrame;
        [SerializeField] Sprite commandCardFrame;
        [SerializeField] Sprite portraitFrame;
        [SerializeField] Sprite centerBlockFrame;
        [SerializeField] Sprite slotFrame;

        [Header("Ability Icons")]

        [SerializeField] Sprite iconMove;
        [SerializeField] Sprite iconStop;
        [SerializeField] Sprite iconHold;
        [SerializeField] Sprite iconPatrol;
        [SerializeField] Sprite iconAttack;
        [SerializeField] Sprite iconGather;
        [SerializeField] Sprite iconBuild;
        [SerializeField] Sprite iconCancel;
        [SerializeField] Sprite iconWorker;
        [SerializeField] Sprite iconFighter;
        [SerializeField] Sprite iconRanged;
        [SerializeField] Sprite iconUnderground;
        [SerializeField] Sprite iconSkyTower;
        [SerializeField] Sprite iconAntNest;
        [SerializeField] Sprite iconRootCellar;
        [SerializeField] Sprite iconEvolve;
        [SerializeField] Sprite iconClearRally;

        [Header("Unit Portraits")]
        [SerializeField] Sprite portraitWorker;
        [SerializeField] Sprite portraitFighter;
        [SerializeField] Sprite portraitRanged;
        [SerializeField] Sprite portraitBlackWidow;
        [SerializeField] Sprite portraitStick;
        [SerializeField] Sprite portraitStagBeetle;

        static readonly Color ColTitle = new(0.96f, 0.90f, 0.78f);
        static readonly Color ColSub   = new(0.83f, 0.69f, 0.44f);
        static readonly Color ColOutline = new(0.1f, 0.08f, 0.06f, 0.8f);

        Image[] _selectionCells;
        Image _portraitMain;
        GameObject _portraitBlock;
        Text _portraitLabel;
        Text _hpLabel;
        Text _attributeLabel;
        Image _hpBarBg;
        Image _hpBarFill;
        Text _pendingHint;
        Font _font;
        Transform _cmdGridParent;
        readonly Dictionary<string, Image> _cmdButtonImages = new();
        Image[] _selectionCellIcons;
        Image[] _selectionCellHpBg;
        Image[] _selectionCellHpFill;
        RectTransform _selectionGridRt;
        GridLayoutGroup _selectionGridLayout;
        bool _gridInMultiUnitMode;

        const int QueueSlotCount = 5;
        GameObject _prodRoot;
        Image _prodActiveIcon;
        Image _prodBarBgImg;
        Image _prodBarFillImg;
        Text _prodBarLabel;
        GameObject _queueRow;
        Image[] _queueSlotBg = new Image[QueueSlotCount];
        Image[] _queueSlotIcon = new Image[QueueSlotCount];
        ProductionBuilding _queueDisplayBuilding;
        HiveDeposit _queueDisplayHive;

        bool _buildMenuActive;
        GameObject _ghostPreview;
        Camera _cam;
        int _lastQueueSnapshot = -1;

        GameObject _tooltipRoot;
        Text _tooltipTitle;
        Text _tooltipBody;
        CanvasGroup _tooltipCanvasGroup;

        enum BarMode { None, Units, WorkerUnits, Hive, Resource, BuildMenu, Building }
        BarMode _currentBarMode = (BarMode)(-1);
        BuildingType? _lastActiveBuildingType;

        void Awake()
        {
            Instance = this;
            _font = UiFontHelper.GetFont();
            // Reset static state that might be stale from a previous play session
            // when Unity's domain reload is disabled.
            Pending = PendingCommand.None;
            PendingBuildingType = default;
            PatrolCoordinator.Reset();

            if (centerBlockFrame == null) centerBlockFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");

            if (minimapFrame == null) minimapFrame = centerBlockFrame;
            if (minimapFrame == null) minimapFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_ornate");
            if (minimapFrame == null) minimapFrame = GameHUD.LoadSpriteFromResources("UI/frame_minimap_hollow");

            if (commandCardFrame == null) commandCardFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_action_grid_empty");
            if (portraitFrame == null) portraitFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_portrait");
            if (centerBlockFrame == null) centerBlockFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");
            if (slotFrame == null) slotFrame = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");

            if (iconMove == null) iconMove = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/move_icon_v2");
            if (iconStop == null) iconStop = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/stop_icon_v2");
            if (iconHold == null) iconHold = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/hold_icon_v2");
            if (iconPatrol == null) iconPatrol = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/patrol_icon_v2");
            if (iconAttack == null) iconAttack = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/attack_icon_v2");
            if (iconGather == null) iconGather = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/gather_icon");
            if (iconBuild == null) iconBuild = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/build_icon_v2");
            if (iconCancel == null) iconCancel = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/cancel_icon_v2");

            if (iconWorker == null) iconWorker = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/worker_ant_icon");
            if (iconFighter == null) iconFighter = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/mantis_fighter_icon");
            if (iconRanged == null) iconRanged = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/beetle_ranged_icon");
            if (iconUnderground == null) iconUnderground = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/underground_icon_v3");
            if (iconSkyTower == null) iconSkyTower = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/skytower_icon_v3");
            if (iconAntNest == null) iconAntNest = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/antnest_icon_v3");
            if (iconRootCellar == null) iconRootCellar = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/rootcellar_icon_v3");

            if (iconEvolve == null) iconEvolve = GameHUD.LoadSpriteFromResources("UI/Icon_Evolve");
            if (iconClearRally == null) iconClearRally = GameHUD.LoadSpriteFromResources("UI/Icon_ClearRally");

            if (portraitWorker == null) portraitWorker = iconWorker;
            if (portraitFighter == null) portraitFighter = iconFighter;
            if (portraitRanged == null) portraitRanged = iconRanged;
            if (portraitBlackWidow == null) portraitBlackWidow = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/black_widow_icon");
            if (portraitStick == null) portraitStick = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/stick_spy_icon");
            if (portraitStagBeetle == null) portraitStagBeetle = GameHUD.LoadSpriteFromResources("UI/ImprovedIcons/stag_beetle_icon");
        }

        void Start()
        {
            BuildBar();
            BuildTooltip();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MinimapHost = null;
            Pending = PendingCommand.None;
            PendingBuildingType = default;
            PatrolCoordinator.Reset();
            DestroyGhost();
        }

        void Update()
        {
            HandleHotkeys();
        }

        void HandleHotkeys()
        {
            if (Keyboard.current == null) return;

            // Tab cycles through all subgroups (units, each building type)
            if (Keyboard.current.tabKey.wasPressedThisFrame &&
                SelectionController.Instance != null &&
                SelectionController.Instance.HasMultipleSubgroups)
            {
                SelectionController.Instance.CycleSubgroup();
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame && !GameHUD.SettingsPanelOpen)
            {
                if (_buildMenuActive)
                {
                    _buildMenuActive = false;
                    ForceRebuild();
                }
                else if (Pending != PendingCommand.None)
                {
                    SetPending(PendingCommand.None);
                }
                else if (SelectionController.Instance != null)
                {
                    bool cancelled = false;
                    foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
                    {
                        if (b.IsProducing)
                        {
                            b.CancelLast();
                            cancelled = true;
                            break;
                        }
                    }
                    if (cancelled)
                    {
                        ForceRebuild();
                    }
                    else
                    {
                        var selHive = SelectionController.Instance.SelectedHive;
                        if (selHive != null && selHive.IsProducing)
                        {
                            selHive.CancelLast();
                            ForceRebuild();
                        }
                    }
                }
                return;
            }

            if (SelectionController.Instance == null) return;

            if (_buildMenuActive)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.Underground);
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.SkyTower);
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.AntNest);
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.RootCellar);
                return;
            }

            var selectedBuilding = SelectionController.Instance.SelectedBuilding;
            if (selectedBuilding != null)
            {
                var producible = selectedBuilding.ProducibleUnits;
                if (Keyboard.current.qKey.wasPressedThisFrame && producible.Length > 0)
                    ProduceFromAllActiveBuildings(producible[0]);
                if (Keyboard.current.wKey.wasPressedThisFrame && producible.Length > 1)
                    ProduceFromAllActiveBuildings(producible[1]);
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    ClearAllActiveBuildingRallies();
                return;
            }

            if (SelectionController.Instance.SelectedHive != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    BuildWorker();
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    var hive = SelectionController.Instance.SelectedHive;
                    if (hive != null && hive.Team == Team.Player)
                    {
                        var evolution = hive.GetComponent<NestEvolution>();
                        if (evolution != null && !evolution.IsEvolved)
                        {
                            evolution.Evolve();
                            ForceRebuild();
                        }
                    }
                }
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    ClearRally();
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            var hasSel = false;
            foreach (var _ in SelectionController.Instance.SelectedPlayerUnits())
            {
                hasSel = true;
                break;
            }
            if (!hasSel) return;

            if (Keyboard.current.mKey.wasPressedThisFrame) SetPending(PendingCommand.Move);
            if (Keyboard.current.sKey.wasPressedThisFrame) IssueStopAll();
            if (Keyboard.current.hKey.wasPressedThisFrame) IssueHoldAll();
            if (Keyboard.current.pKey.wasPressedThisFrame) SetPending(PendingCommand.Patrol);
            if (Keyboard.current.aKey.wasPressedThisFrame) SetPending(PendingCommand.Attack);
            if (Keyboard.current.bKey.wasPressedThisFrame && SelectionController.Instance.HasWorkerSelected())
                EnterBuildMenu();
            }

        void LateUpdate()
        {
            UpdateBarMode();
            RefreshSelectionGrid();
            RefreshCommandHighlights();
            RefreshPendingHint();
            RefreshProductionBar();
            UpdateGhostPreview();
        }

        public static bool SuppressSelectionDrag =>
            Pending != PendingCommand.None || PatrolCoordinator.WaitingForSecondPoint;

        /// <summary>
        /// True when pressing Escape should be handled by the HUD (cancel command / close menu /
        /// cancel production) rather than by PauseController.
        /// </summary>
        public static bool WouldConsumeEscape
        {
            get
            {
                if (Instance == null) return false;
                if (Instance._buildMenuActive) return true;
                if (Pending != PendingCommand.None) return true;
                if (SelectionController.Instance == null) return false;
                foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
                    if (b.IsProducing) return true;
                var hive = SelectionController.Instance.SelectedHive;
                if (hive != null && hive.IsProducing) return true;
                return false;
            }
        }

        void UpdateBarMode()
        {
            var sc = SelectionController.Instance;
            if (sc == null) return;

            BarMode newMode;
            if (_buildMenuActive)
                newMode = BarMode.BuildMenu;
            else if (sc.SelectedBuilding != null)
                newMode = BarMode.Building;
            else if (sc.SelectedHive != null)
                newMode = BarMode.Hive;
            else if (sc.SelectedResource != null)
                newMode = BarMode.Resource;
            else
            {
                bool hasUnits = false;
                bool hasWorker = false;
                foreach (var u in sc.SelectedPlayerUnits())
                {
                    hasUnits = true;
                    if (u.Definition != null && u.Definition.canGather) hasWorker = true;
                    if (hasWorker) break;
                }
                if (hasWorker) newMode = BarMode.WorkerUnits;
                else if (hasUnits) newMode = BarMode.Units;
                else newMode = BarMode.None;
            }

            if (newMode != BarMode.BuildMenu && newMode != BarMode.WorkerUnits)
                _buildMenuActive = false;

            int queueSnap = 0;
            if (newMode == BarMode.Building)
            {
                foreach (var b in sc.SelectedBuildingsOfActiveType)
                    queueSnap += b.QueueCount;
            }
            else if (newMode == BarMode.Hive)
            {
                var hive = sc.SelectedHive;
                if (hive != null) queueSnap = hive.QueueCount;
            }

            var curBldType = sc.ActiveBuildingType;
            if (newMode != _currentBarMode || queueSnap != _lastQueueSnapshot || curBldType != _lastActiveBuildingType)
            {
                _currentBarMode = newMode;
                _lastQueueSnapshot = queueSnap;
                _lastActiveBuildingType = curBldType;
                RebuildCommandButtons();
            }
        }

        void RebuildCommandButtons()
        {
            if (_cmdGridParent == null) return;
            foreach (Transform child in _cmdGridParent)
                Destroy(child.gameObject);
            _cmdButtonImages.Clear();

            switch (_currentBarMode)
            {
                case BarMode.Units:
                    AddCmdButton(_cmdGridParent, "Move", "M", () => SetPending(PendingCommand.Move));
                    AddCmdButton(_cmdGridParent, "Stop", "S", IssueStopAll);
                    AddCmdButton(_cmdGridParent, "Hold", "H", IssueHoldAll);
                    AddCmdButton(_cmdGridParent, "Patrol", "P", () => SetPending(PendingCommand.Patrol));
                    AddCmdButton(_cmdGridParent, "Attack", "A", () => SetPending(PendingCommand.Attack));
                    break;
                case BarMode.WorkerUnits:
                    AddCmdButton(_cmdGridParent, "Move", "M", () => SetPending(PendingCommand.Move));
                    AddCmdButton(_cmdGridParent, "Stop", "S", IssueStopAll);
                    AddCmdButton(_cmdGridParent, "Hold", "H", IssueHoldAll);
                    AddCmdButton(_cmdGridParent, "Patrol", "P", () => SetPending(PendingCommand.Patrol));
                    AddCmdButton(_cmdGridParent, "Attack", "A", () => SetPending(PendingCommand.Attack));
                    AddCmdButton(_cmdGridParent, "Build", "B", EnterBuildMenu);
                    break;
                case BarMode.BuildMenu:
                    AddBuildButton(_cmdGridParent, BuildingType.Underground, "Q");
                    AddBuildButton(_cmdGridParent, BuildingType.SkyTower, "W");
                    AddBuildButton(_cmdGridParent, BuildingType.AntNest, "E");
                    AddBuildButton(_cmdGridParent, BuildingType.RootCellar, "R");
                    AddCmdButton(_cmdGridParent, "Cancel", "Esc", () => { _buildMenuActive = false; HideTooltip(); ForceRebuild(); });
                    break;
                case BarMode.Hive:
                    AddCmdButton(_cmdGridParent, $"Worker\n<size=11>{ProductionBuilding.GetUnitCost(UnitArchetype.Worker)} cal · {ColonyCapacity.GetUnitCCCost(UnitArchetype.Worker)} CC</size>", "W", BuildWorker);
                    var hive = SelectionController.Instance?.SelectedHive;
                    if (hive != null && hive.Team == Team.Player)
                    {
                        var evolution = hive.GetComponent<NestEvolution>();
                        if (evolution != null && !evolution.IsEvolved)
                            AddCmdButton(_cmdGridParent, $"Evolve\n<size=11>{evolution.EvolveCost} cal</size>", "E", () => { evolution.Evolve(); ForceRebuild(); });
                        if (hive.IsProducing)
                            AddCmdButton(_cmdGridParent, "Cancel", "Esc", () => hive.CancelLast());
                    }
                    AddCmdButton(_cmdGridParent, "Clear Rally", "R", ClearRally);
                    break;
                case BarMode.Building:
                    var bld = SelectionController.Instance?.SelectedBuilding;
                    if (bld != null)
                    {
                        if (bld.State == BuildingState.UnderConstruction)
                        {
                            int refund = Mathf.RoundToInt(ProductionBuilding.GetBuildCost(bld.Type) * 0.8f);
                            AddCmdButton(_cmdGridParent, $"Cancel\n<size=11>+{refund} cal</size>", "Esc",
                                () => { bld.CancelConstruction(); ForceRebuild(); });
                        }
                        else
                        {
                            string[] hotkeys = { "Q", "W", "E", "R" };
                            var units = bld.ProducibleUnits;
                            for (int i = 0; i < units.Length; i++)
                            {
                                var arch = units[i];
                                string hk = i < hotkeys.Length ? hotkeys[i] : "";
                                AddCmdButton(_cmdGridParent, $"{ProductionBuilding.GetUnitName(arch)}\n<size=11>{ProductionBuilding.GetUnitCost(arch)} cal</size>", hk, () => ProduceFromAllActiveBuildings(arch));
                            }
                            bool anyProducing = false;
                            foreach (var ab in SelectionController.Instance.SelectedBuildingsOfActiveType)
                                if (ab.IsProducing) { anyProducing = true; break; }
                            if (anyProducing)
                                AddCmdButton(_cmdGridParent, "Cancel", "Esc", () => CancelFirstActiveProduction());
                            AddCmdButton(_cmdGridParent, "Clear Rally", "R", ClearAllActiveBuildingRallies);
                        }
                    }
                    break;
            }
        }

        void BuildBar()
        {
            var hud = GameHUD.HudCanvasRect;
            if (hud == null) return;

            var bar = new GameObject("BottomBar");
            bar.transform.SetParent(hud, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, barHeight);

            var bg = bar.AddComponent<Image>();
            if (barBackground != null)
            {
                bg.sprite = barBackground;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.1f, 0.08f, 0.06f, 0.95f);
            }
            bg.raycastTarget = false; // Allow clicking through to the world

            var miniContainer = new GameObject("MinimapContainer");
            miniContainer.transform.SetParent(bar.transform, false);
            var mcrt = miniContainer.AddComponent<RectTransform>();
            mcrt.anchorMin = new Vector2(0f, 0f);
            mcrt.anchorMax = new Vector2(0f, 0f);
            mcrt.pivot = new Vector2(0f, 0.5f);
            mcrt.anchoredPosition = new Vector2(30, 131.75f); // Centered vertically in bar
            mcrt.sizeDelta = new Vector2(minimapSlot - 60, minimapSlot - 60);

            var miniFrameGo = new GameObject("MinimapFrame");
            miniFrameGo.transform.SetParent(miniContainer.transform, false);
            var mfrt = miniFrameGo.AddComponent<RectTransform>();
            mfrt.anchorMin = Vector2.zero;
            mfrt.anchorMax = Vector2.one;
            mfrt.offsetMin = new Vector2(-15, -15);
            mfrt.offsetMax = new Vector2(15, 15);
            var frameImg = miniFrameGo.AddComponent<Image>();
            frameImg.sprite = minimapFrame;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            var miniInner = new GameObject("MinimapInner");
            miniInner.transform.SetParent(miniContainer.transform, false);
            var mi = miniInner.AddComponent<RectTransform>();
            mi.anchorMin = Vector2.zero;
            mi.anchorMax = Vector2.one;
            mi.offsetMin = new Vector2(25, 25);
            mi.offsetMax = new Vector2(-25, -25);
            MinimapHost = mi;

            var cmdPanel = new GameObject("CommandPanel");
            cmdPanel.transform.SetParent(bar.transform, false);
            var cp = cmdPanel.AddComponent<RectTransform>();
            cp.anchorMin = new Vector2(1f, 0f);
            cp.anchorMax = new Vector2(1f, 1f);
            cp.pivot = new Vector2(1f, 0.5f);
            cp.anchoredPosition = new Vector2(-20, 0);
            cp.sizeDelta = new Vector2(commandPanelWidth - 20, -10);

            var cmdFrameImg = cmdPanel.AddComponent<Image>();
            cmdFrameImg.sprite = centerBlockFrame;
            cmdFrameImg.type = Image.Type.Sliced;
            cmdFrameImg.fillCenter = true; // Enable background
            cmdFrameImg.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Slight tint
            cmdFrameImg.raycastTarget = false; 

            var grid = new GameObject("CmdGrid");
            grid.transform.SetParent(cmdPanel.transform, false);
            var grt = grid.AddComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(80f, 30f);
            grt.offsetMax = new Vector2(-80f, -30f);
            
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(80f, 80f);
            gl.spacing = new Vector2(50f, 15f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.MiddleCenter;
            
            _cmdGridParent = grid.transform;

            _portraitBlock = new GameObject("PortraitBlock");
            _portraitBlock.transform.SetParent(bar.transform, false);
            var pr = _portraitBlock.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0.5f);
            pr.anchorMax = new Vector2(0f, 0.5f);
            pr.pivot = new Vector2(0f, 0.5f);
            pr.anchoredPosition = new Vector2(minimapSlot + 24f, 0f);
            pr.sizeDelta = new Vector2(128f, 128f);
            var pImg = _portraitBlock.AddComponent<Image>();
            pImg.sprite = portraitFrame;
            pImg.type = Image.Type.Simple; 
            pImg.color = Color.white;

            var portraitSub = new GameObject("PortraitInner");
            portraitSub.transform.SetParent(_portraitBlock.transform, false);
            var psr = portraitSub.AddComponent<RectTransform>();
            psr.anchorMin = Vector2.zero;
            psr.anchorMax = Vector2.one;
            psr.offsetMin = new Vector2(21f, 21f);
            psr.offsetMax = new Vector2(-21f, -21f);
            _portraitMain = portraitSub.AddComponent<Image>();
            _portraitMain.preserveAspect = true;

            var center = new GameObject("SelectionBlock");
            center.transform.SetParent(bar.transform, false);
            var cr = center.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 0f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.offsetMin = new Vector2(minimapSlot + 168f, 8f);
            cr.offsetMax = new Vector2(-commandPanelWidth, -8f);
            var centerBg = center.AddComponent<Image>();
            centerBg.sprite = centerBlockFrame;
            centerBg.type = Image.Type.Sliced;
            centerBg.fillCenter = true; // Enable background
            centerBg.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Slight tint
            centerBg.raycastTarget = true; 

            var infoBlock = new GameObject("InfoBlock");
            infoBlock.transform.SetParent(center.transform, false);
            var ibr = infoBlock.AddComponent<RectTransform>();
            ibr.anchorMin = new Vector2(0f, 0f);
            ibr.anchorMax = new Vector2(1f, 1f);
            ibr.pivot = new Vector2(0.5f, 0.5f);
            ibr.offsetMin = new Vector2(8f, 8f);
            ibr.offsetMax = new Vector2(-8f, -8f);

            _portraitLabel = CreateText("NameText", infoBlock.transform, 19, ColTitle, TextAnchor.MiddleCenter);
            var pl = _portraitLabel.rectTransform;
            pl.anchorMin = new Vector2(0f, 0.75f); 
            pl.anchorMax = new Vector2(1f, 0.95f);
            pl.offsetMin = pl.offsetMax = Vector2.zero;

            _attributeLabel = CreateText("AttributeText", infoBlock.transform, 11, ColSub, TextAnchor.MiddleCenter);
            var al = _attributeLabel.rectTransform;
            al.anchorMin = new Vector2(0f, 0.05f);
            al.anchorMax = new Vector2(1f, 0.15f);
            al.offsetMin = al.offsetMax = Vector2.zero;

            _hpLabel = CreateText("HpText", _portraitBlock.transform, 13, Color.white, TextAnchor.MiddleCenter);
            var hpRt = _hpLabel.rectTransform;
            hpRt.anchorMin = new Vector2(0f, 0f);
            hpRt.anchorMax = new Vector2(1f, 0f);
            hpRt.anchoredPosition = new Vector2(0f, -82f);
            hpRt.sizeDelta = new Vector2(0f, 20f);

            _hpBarBg = new GameObject("HpBarBg").AddComponent<Image>();
            _hpBarBg.transform.SetParent(_portraitBlock.transform, false);
            _hpBarBg.color = new Color(0f, 0f, 0f, 0.4f); 
            var hbr = _hpBarBg.rectTransform;
            hbr.anchorMin = new Vector2(0.1f, 0f);
            hbr.anchorMax = new Vector2(0.9f, 0f);
            hbr.anchoredPosition = new Vector2(0f, -102f);
            hbr.sizeDelta = new Vector2(0f, 10f);

            _hpBarFill = new GameObject("HpBarFill").AddComponent<Image>();
            _hpBarFill.transform.SetParent(_hpBarBg.transform, false);
            _hpBarFill.color = new Color(0.25f, 0.85f, 0.35f, 0.95f);
            var hfr = _hpBarFill.rectTransform;
            hfr.anchorMin = Vector2.zero;
            hfr.anchorMax = Vector2.one;
            hfr.offsetMin = hfr.offsetMax = Vector2.zero;

            var gridRoot = new GameObject("SelectionGrid");
            gridRoot.transform.SetParent(center.transform, false);
            var gr = gridRoot.AddComponent<RectTransform>();
            gr.anchorMin = new Vector2(0.40f, 1f);
            gr.anchorMax = new Vector2(1f, 1f);
            gr.pivot = new Vector2(0f, 1f);
            gr.anchoredPosition = new Vector2(0f, -8f);
            gr.sizeDelta = new Vector2(0f, 51f);
            _selectionGridRt = gr;

            var gridLayout = gridRoot.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(42f, 42f);
            gridLayout.spacing = new Vector2(5f, 5f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = 1;
            _selectionGridLayout = gridLayout;

            _selectionCells = new Image[24];
            _selectionCellIcons = new Image[24];
            _selectionCellHpBg = new Image[24];
            _selectionCellHpFill = new Image[24];
            for (var i = 0; i < 24; i++)
            {
                var cell = new GameObject($"Sel_{i}");
                cell.transform.SetParent(gridRoot.transform, false);
                var cellBg = cell.AddComponent<Image>();
                cellBg.sprite = slotFrame;
                cellBg.type = Image.Type.Sliced;
                cellBg.color = new Color(1f, 1f, 1f, 0f);
                cellBg.raycastTarget = false;

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(cell.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.color = new Color(1f, 1f, 1f, 0f);
                var iconRt = iconImg.rectTransform;
                iconRt.anchorMin = new Vector2(0.06f, 0.20f);
                iconRt.anchorMax = new Vector2(0.94f, 0.95f);
                iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

                var hpBgGo = new GameObject("HpBg");
                hpBgGo.transform.SetParent(cell.transform, false);
                var hpBgImg = hpBgGo.AddComponent<Image>();
                hpBgImg.color = new Color(0f, 0f, 0f, 0f);
                hpBgImg.raycastTarget = false;
                var hpBgRt = hpBgImg.rectTransform;
                hpBgRt.anchorMin = new Vector2(0.06f, 0.04f);
                hpBgRt.anchorMax = new Vector2(0.94f, 0.16f);
                hpBgRt.offsetMin = hpBgRt.offsetMax = Vector2.zero;

                var hpFillGo = new GameObject("HpFill");
                hpFillGo.transform.SetParent(hpBgGo.transform, false);
                var hpFillImg = hpFillGo.AddComponent<Image>();
                hpFillImg.color = new Color(0.25f, 0.85f, 0.35f, 0f);
                hpFillImg.raycastTarget = false;
                var hpFillRt = hpFillImg.rectTransform;
                hpFillRt.anchorMin = Vector2.zero;
                hpFillRt.anchorMax = Vector2.one;
                hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;

                var tx = CreateText("t", cell.transform, 11, Color.white, TextAnchor.LowerRight);
                var trt = tx.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = new Vector2(2f, 2f);

                _selectionCells[i] = cellBg;
                _selectionCellIcons[i] = iconImg;
                _selectionCellHpBg[i] = hpBgImg;
                _selectionCellHpFill[i] = hpFillImg;
            }

            _pendingHint = CreateText("PendingHint", hud, 15, ColTitle, TextAnchor.MiddleCenter);
            var ph = _pendingHint.rectTransform;
            ph.anchorMin = new Vector2(0.5f, 0.30f);
            ph.anchorMax = new Vector2(0.5f, 0.30f);
            ph.anchoredPosition = Vector2.zero;
            ph.sizeDelta = new Vector2(680f, 28f);

            _prodRoot = new GameObject("ProdRoot");
            _prodRoot.transform.SetParent(infoBlock.transform, false);
            var prodRt = _prodRoot.AddComponent<RectTransform>();
            prodRt.anchorMin = new Vector2(0.5f, 0.5f);
            prodRt.anchorMax = new Vector2(0.5f, 0.5f);
            prodRt.pivot = new Vector2(0.5f, 0.5f);
            prodRt.anchoredPosition = Vector2.zero;
            prodRt.sizeDelta = new Vector2(280f, 60f);

            // --- Row 1: active icon + progress bar ---
            var row1 = new GameObject("ProdRow");
            row1.transform.SetParent(_prodRoot.transform, false);
            var r1rt = row1.AddComponent<RectTransform>();
            r1rt.anchorMin = new Vector2(0f, 0.5f);
            r1rt.anchorMax = new Vector2(1f, 1f);
            r1rt.offsetMin = r1rt.offsetMax = Vector2.zero;

            var activeSlot = new GameObject("ActiveIcon");
            activeSlot.transform.SetParent(row1.transform, false);
            var asrt = activeSlot.AddComponent<RectTransform>();
            asrt.anchorMin = new Vector2(0f, 0f);
            asrt.anchorMax = new Vector2(0f, 1f);
            asrt.pivot = new Vector2(0f, 0.5f);
            asrt.anchoredPosition = Vector2.zero;
            asrt.sizeDelta = new Vector2(28f, 0f);
            var asBg = activeSlot.AddComponent<Image>();
            asBg.sprite = slotFrame;
            asBg.type = Image.Type.Simple;
            asBg.color = new Color(0.18f, 0.14f, 0.1f, 0.9f);

            var activeIconGo = new GameObject("Icon");
            activeIconGo.transform.SetParent(activeSlot.transform, false);
            _prodActiveIcon = activeIconGo.AddComponent<Image>();
            _prodActiveIcon.preserveAspect = true;
            _prodActiveIcon.raycastTarget = false;
            var airt = _prodActiveIcon.rectTransform;
            airt.anchorMin = new Vector2(0.1f, 0.1f);
            airt.anchorMax = new Vector2(0.9f, 0.9f);
            airt.offsetMin = airt.offsetMax = Vector2.zero;
            _prodActiveIcon.color = new Color(1f, 1f, 1f, 0f);

            var barContainer = new GameObject("BarContainer");
            barContainer.transform.SetParent(row1.transform, false);
            var bcrt = barContainer.AddComponent<RectTransform>();
            bcrt.anchorMin = new Vector2(0f, 0f);
            bcrt.anchorMax = new Vector2(1f, 1f);
            bcrt.offsetMin = new Vector2(32f, 2f);
            bcrt.offsetMax = new Vector2(0f, -2f);

            _prodBarBgImg = barContainer.AddComponent<Image>();
            _prodBarBgImg.sprite = null;
            _prodBarBgImg.type = Image.Type.Sliced;
            _prodBarBgImg.color = new Color(0.1f, 0.08f, 0.06f, 0.8f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barContainer.transform, false);
            _prodBarFillImg = fillGo.AddComponent<Image>();
            _prodBarFillImg.sprite = null;
            _prodBarFillImg.type = Image.Type.Sliced;
            _prodBarFillImg.color = new Color(0.1f, 0.8f, 0.1f, 1f);
            _prodBarFillImg.raycastTarget = false;
            var frt = _prodBarFillImg.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            _prodBarLabel = CreateText("BarLabel", barContainer.transform, 11, Color.white, TextAnchor.MiddleCenter);
            _prodBarLabel.raycastTarget = false;
            var blrt = _prodBarLabel.rectTransform;
            blrt.anchorMin = Vector2.zero;
            blrt.anchorMax = Vector2.one;
            blrt.offsetMin = blrt.offsetMax = Vector2.zero;

            // --- Row 2: queue slots ---
            _queueRow = new GameObject("QueueRow");
            _queueRow.transform.SetParent(_prodRoot.transform, false);
            var r2rt = _queueRow.AddComponent<RectTransform>();
            r2rt.anchorMin = new Vector2(0f, 0f);
            r2rt.anchorMax = new Vector2(1f, 0.48f);
            r2rt.offsetMin = r2rt.offsetMax = Vector2.zero;

            var qLayout = _queueRow.AddComponent<HorizontalLayoutGroup>();
            qLayout.spacing = 3f;
            qLayout.childAlignment = TextAnchor.MiddleCenter;
            qLayout.childForceExpandWidth = false;
            qLayout.childForceExpandHeight = false;
            qLayout.childControlWidth = false;
            qLayout.childControlHeight = false;

            for (int i = 0; i < QueueSlotCount; i++)
            {
                var slot = new GameObject($"QSlot_{i}");
                slot.transform.SetParent(_queueRow.transform, false);
                var slotRt = slot.AddComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(28f, 28f);

                var slotBg = slot.AddComponent<Image>();
                slotBg.sprite = slotFrame;
                slotBg.type = Image.Type.Simple;
                slotBg.color = new Color(0.12f, 0.1f, 0.08f, 0.6f);
                slotBg.raycastTarget = true;
                _queueSlotBg[i] = slotBg;

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slot.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                var irt = iconImg.rectTransform;
                irt.anchorMin = new Vector2(0.08f, 0.08f);
                irt.anchorMax = new Vector2(0.92f, 0.92f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                iconImg.color = new Color(1f, 1f, 1f, 0f);
                _queueSlotIcon[i] = iconImg;

                int idx = i;
                var trigger = slot.AddComponent<EventTrigger>();
                var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                clickEntry.callback.AddListener(data =>
                {
                    var ped = (PointerEventData)data;
                    if (ped.button == PointerEventData.InputButton.Right)
                        CancelQueueSlot(idx);
                });
                trigger.triggers.Add(clickEntry);
            }

            _prodRoot.SetActive(false);
        }

        Sprite GetBuildingIcon(BuildingType t)
        {
            return t switch
            {
                BuildingType.Underground => iconUnderground,
                BuildingType.AntNest => iconAntNest,
                BuildingType.SkyTower => iconSkyTower,
                BuildingType.RootCellar => iconRootCellar,
                BuildingType.Hive => iconAntNest,
                _ => null
            };
        }

        Text CreateText(string name, Transform parent, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ColOutline;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        void AddCmdButton(Transform parent, string name, string key, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Cmd_{name}");
            go.transform.SetParent(parent, false);
            
            var slotBg = go.AddComponent<Image>();
            slotBg.sprite = slotFrame;
            slotBg.color = Color.white;
            slotBg.type = Image.Type.Simple;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var img = iconGo.AddComponent<Image>();
            img.sprite = GetCommandIcon(name);
            img.color = Color.white;
            img.preserveAspect = true;
            var irt = img.rectTransform;
            irt.anchorMin = new Vector2(0.08f, 0.08f); irt.anchorMax = new Vector2(0.92f, 0.92f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.85f, 0.75f, 0.65f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var keyLabel = CreateText("Key", go.transform, 14, ColTitle, TextAnchor.UpperRight);
            keyLabel.text = key;
            var krt = keyLabel.rectTransform;
            krt.offsetMin = new Vector2(2f, 2f);
            krt.offsetMax = new Vector2(-2f, -1f);

            _cmdButtonImages[name] = img;
        }

        void AddBuildButton(Transform parent, BuildingType buildType, string key)
        {
            string displayName = ProductionBuilding.GetDisplayName(buildType);
            AddCmdButton(parent, displayName, key, () => StartPlaceBuilding(buildType));

            var btnGo = parent.GetChild(parent.childCount - 1).gameObject;
            var trigger = btnGo.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowBuildingTooltip(buildType, btnGo.transform as RectTransform));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        void BuildTooltip()
        {
            var hud = GameHUD.HudCanvasRect;
            if (hud == null) return;

            _tooltipRoot = new GameObject("BuildingTooltip");
            _tooltipRoot.transform.SetParent(hud, false);
            var rt = _tooltipRoot.AddComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(260f, 0f);

            _tooltipCanvasGroup = _tooltipRoot.AddComponent<CanvasGroup>();
            _tooltipCanvasGroup.alpha = 0f;
            _tooltipCanvasGroup.blocksRaycasts = false;
            _tooltipCanvasGroup.interactable = false;

            var bg = _tooltipRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.07f, 0.05f, 0.92f);

            var vlg = _tooltipRoot.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            var fitter = _tooltipRoot.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _tooltipTitle = CreateText("TooltipTitle", _tooltipRoot.transform, 15, ColTitle, TextAnchor.MiddleLeft);
            _tooltipTitle.fontStyle = FontStyle.Bold;

            _tooltipBody = CreateText("TooltipBody", _tooltipRoot.transform, 12, ColSub, TextAnchor.UpperLeft);

            _tooltipRoot.SetActive(false);
        }

        void ShowBuildingTooltip(BuildingType type, RectTransform anchor)
        {
            if (_tooltipRoot == null) return;

            string title = ProductionBuilding.GetDisplayName(type);
            int cost = ProductionBuilding.GetBuildCost(type);
            float hp = ProductionBuilding.GetMaxHealth(type);
            float buildTime = ProductionBuilding.GetConstructionTime(type);
            string desc = ProductionBuilding.GetDescription(type);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Cost: {cost} calories");
            sb.AppendLine($"HP: {hp:0}  ·  Build time: {buildTime:0}s");

            int cc = type switch
            {
                BuildingType.AntNest => ColonyCapacity.AntNestCCProvided,
                BuildingType.RootCellar => ColonyCapacity.RootCellarCCProvided,
                _ => 0
            };
            if (cc > 0)
                sb.AppendLine($"Supply: +{cc} colony capacity");

            var units = type switch
            {
                BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.GiantStagBeetle },
                BuildingType.AntNest => new[] { UnitArchetype.Worker },
                BuildingType.SkyTower => new[] { UnitArchetype.BlackWidow, UnitArchetype.StickSpy },
                _ => System.Array.Empty<UnitArchetype>()
            };
            if (units.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Produces:");
                foreach (var u in units)
                {
                    string uName = ProductionBuilding.GetUnitName(u);
                    int uCost = ProductionBuilding.GetUnitCost(u);
                    int uCC = ColonyCapacity.GetUnitCCCost(u);
                    sb.AppendLine($"  · {uName} — {uCost} cal, {uCC} CC");
                }
            }

            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine();
                sb.Append(desc);
            }

            _tooltipTitle.text = title;
            _tooltipBody.text = sb.ToString();

            _tooltipRoot.SetActive(true);
            _tooltipCanvasGroup.alpha = 1f;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRoot.GetComponent<RectTransform>());

            if (anchor != null)
            {
                var tooltipRt = _tooltipRoot.GetComponent<RectTransform>();
                Vector3 anchorWorld = anchor.position;
                float tooltipHeight = tooltipRt.rect.height;
                tooltipRt.position = new Vector3(anchorWorld.x, anchorWorld.y + anchor.rect.height * anchor.lossyScale.y + 8f, anchorWorld.z);
            }
        }

        void HideTooltip()
        {
            if (_tooltipRoot == null) return;
            _tooltipCanvasGroup.alpha = 0f;
            _tooltipRoot.SetActive(false);
        }

        Sprite GetCommandIcon(string cmdName)
        {
            if (cmdName.Contains("Move")) return iconMove;
            if (cmdName.Contains("Stop")) return iconStop;
            if (cmdName.Contains("Hold")) return iconHold;
            if (cmdName.Contains("Patrol")) return iconPatrol;
            if (cmdName.Contains("Attack")) return iconAttack;
            if (cmdName.Contains("Gather")) return iconGather;
            if (cmdName.Contains("Build")) return iconBuild;
            if (cmdName.Contains("Cancel")) return iconCancel;
            if (cmdName.Contains("Worker")) return iconWorker;
            if (cmdName.Contains("Mantis")) return iconFighter;
            if (cmdName.Contains("Beetle")) return iconRanged;
            if (cmdName.Contains("Black Widow")) return portraitBlackWidow;
            if (cmdName.Contains("Stick")) return portraitStick;
            if (cmdName.Contains("Stag Beetle")) return portraitStagBeetle;
            if (cmdName.Contains("Underground")) return iconUnderground;
            if (cmdName.Contains("Sky Tower")) return iconSkyTower;
            if (cmdName.Contains("Ant's Nest") || cmdName.Contains("Ant Nest")) return iconAntNest;
            if (cmdName.Contains("Root Cellar")) return iconRootCellar;
            if (cmdName.Contains("Evolve")) return iconEvolve;
            if (cmdName.Contains("Clear Rally")) return iconClearRally;
            return null;
        }

        public void SetPending(PendingCommand cmd)
        {
            if (cmd != PendingCommand.Patrol || Pending != PendingCommand.Patrol)
                PatrolCoordinator.Reset();
            if (cmd != PendingCommand.PlaceBuilding)
                DestroyGhost();
            Pending = cmd;
        }

        void IssueStopAll()
        {
            SetPending(PendingCommand.None);
            if (SelectionController.Instance == null) return;
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                u.OrderStop();
        }

        void IssueHoldAll()
        {
            SetPending(PendingCommand.None);
            if (SelectionController.Instance == null) return;
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                u.OrderHoldPosition();
        }

        void BuildWorker()
        {
            var hive = HiveDeposit.PlayerHive;
            if (hive == null) return;
            hive.QueueWorker();
        }

        void ClearRally()
        {
            var hive = SelectionController.Instance?.SelectedHive ?? HiveDeposit.PlayerHive;
            if (hive != null) hive.ClearRally();
        }

        void EnterBuildMenu()
        {
            _buildMenuActive = true;
            ForceRebuild();
        }

        void StartPlaceBuilding(BuildingType type)
        {
            _buildMenuActive = false;
            HideTooltip();
            PendingBuildingType = type;
            SetPending(PendingCommand.PlaceBuilding);
            CreateGhost(type);
            ForceRebuild();
        }

        void ProduceFromBuilding(ProductionBuilding bld, UnitArchetype archetype)
        {
            if (bld != null) bld.QueueUnit(archetype);
        }

        /// <summary>
        /// Queue one unit in the operational building of the active type that has the
        /// shortest queue (SC2 smart-queue distribution). Press the hotkey multiple
        /// times to fill queues across buildings round-robin style.
        /// </summary>
        void ProduceFromAllActiveBuildings(UnitArchetype archetype)
        {
            if (SelectionController.Instance == null) return;
            ProductionBuilding best = null;
            int bestQueue = int.MaxValue;
            foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
            {
                if (!b.IsOperational) continue;
                if (b.QueueCount < bestQueue)
                {
                    bestQueue = b.QueueCount;
                    best = b;
                }
            }
            if (best != null) best.QueueUnit(archetype);
        }

        void CancelFirstActiveProduction()
        {
            if (SelectionController.Instance == null) return;
            foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
            {
                if (b.IsProducing)
                {
                    b.CancelLast();
                    ForceRebuild();
                    return;
                }
            }
        }

        void ClearAllActiveBuildingRallies()
        {
            if (SelectionController.Instance == null) return;
            foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
                b.ClearRally();
        }

        public void ForceRebuild()
        {
            _currentBarMode = (BarMode)(-1);
        }

        void CreateGhost(BuildingType type)
        {
            DestroyGhost();

            _ghostPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghostPreview.name = "BuildingGhost";
            Destroy(_ghostPreview.GetComponent<Collider>());

            Vector3 scale = type switch
            {
                BuildingType.Underground => new Vector3(4f, 2f, 4f),
                BuildingType.AntNest => new Vector3(3.5f, 2f, 3.5f),
                BuildingType.SkyTower => new Vector3(4f, 2f, 4f),
                BuildingType.RootCellar => new Vector3(1.75f, 1f, 1.75f),
                _ => new Vector3(3f, 2f, 3f)
            };
            _ghostPreview.transform.localScale = scale;
            _ghostPreview.GetComponent<Renderer>().sharedMaterial =
                CreateGhostMaterial(new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        static Material CreateGhostMaterial(Color color)
        {
            var sh = Shader.Find("InsectWars/GhostPreview");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            
            var mat = new Material(sh);
            if (sh.name.Contains("InsectWars/GhostPreview"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (sh.name.Contains("Universal Render Pipeline"))
            {
                // Fallback transparency setup
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            }
            else
            {
                if (mat.HasProperty("_Color")) mat.color = color;
            }
            return mat;
        }

        void DestroyGhost()
        {
            if (_ghostPreview != null)
            {
                Destroy(_ghostPreview);
                _ghostPreview = null;
            }
        }

        void UpdateGhostPreview()
        {
            if (Pending != PendingCommand.PlaceBuilding || _ghostPreview == null)
                return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null || Mouse.current == null) return;

            var ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var enter))
            {
                var worldPos = ray.GetPoint(enter);
                var terrain = Terrain.activeTerrain;
                float terrainY = terrain != null ? terrain.SampleHeight(worldPos) : 0f;
                _ghostPreview.transform.position = new Vector3(worldPos.x, terrainY, worldPos.z);

                bool valid = CommandController.IsValidBuildLocation(
                    new Vector3(worldPos.x, terrainY, worldPos.z),
                    PendingBuildingType);

                var tint = valid
                    ? new Color(0.3f, 0.85f, 0.4f, 0.45f)
                    : new Color(0.85f, 0.25f, 0.2f, 0.45f);

                foreach (var r in _ghostPreview.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.material == null) continue;
                    if (r.material.HasProperty("_Color")) r.material.color = tint;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", tint);
                }
            }
        }

        static readonly Color CmdDefault = new(0.72f, 0.68f, 0.58f, 1f);
        static readonly Color CmdActive = new(0.96f, 0.90f, 0.78f, 1f);

        void RefreshCommandHighlights()
        {
            if (_cmdButtonImages.Count == 0) return;

            foreach (var kvp in _cmdButtonImages)
                kvp.Value.color = CmdDefault;

            if (SelectionController.Instance == null) return;

            var activeOrders = new HashSet<UnitOrder>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
                activeOrders.Add(u.CurrentOrder);

            if (_currentBarMode == BarMode.Hive)
            {
                var hive = HiveDeposit.PlayerHive;
                if (hive != null && hive.RallyPoint.HasValue &&
                    _cmdButtonImages.TryGetValue("Clear Rally", out var rallyImg))
                    rallyImg.color = CmdActive;
                return;
            }

            if (_currentBarMode == BarMode.Building)
            {
                foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
                {
                    if (b.RallyPoint.HasValue &&
                        _cmdButtonImages.TryGetValue("Clear Rally", out var rallyImg2))
                    {
                        rallyImg2.color = CmdActive;
                        break;
                    }
                }
                return;
            }

            foreach (var order in activeOrders)
            {
                string key = order switch
                {
                    UnitOrder.Move => "Move",
                    UnitOrder.Attack => "Attack",
                    UnitOrder.AttackBuilding => "Attack",
                    UnitOrder.Gather => "Gather",
                    UnitOrder.ReturnDeposit => "Gather",
                    UnitOrder.Patrol => "Patrol",
                    UnitOrder.Build => "Build",
                    _ => null
                };
                if (key != null && _cmdButtonImages.TryGetValue(key, out var img))
                    img.color = CmdActive;
            }
        }

        void RefreshProductionBar()
        {
            if (_prodRoot == null) return;

            _queueDisplayBuilding = null;
            _queueDisplayHive = null;

            ProductionBuilding displayBld = null;
            HiveDeposit displayHive = null;
            bool isConstruction = false;
            float constructionProgress = 0f;
            int constructionBuilders = 0;

            if (SelectionController.Instance != null)
            {
                var bld = SelectionController.Instance.SelectedBuilding;
                if (bld != null && bld.State == BuildingState.UnderConstruction)
                {
                    isConstruction = true;
                    constructionProgress = bld.ConstructionProgress;
                    constructionBuilders = bld.AssignedBuilders;
                }
                else if (bld != null)
                {
                    foreach (var ab in SelectionController.Instance.SelectedBuildingsOfActiveType)
                    {
                        if (!ab.IsProducing) continue;
                        displayBld = ab;
                        break;
                    }
                }

                if (displayBld == null && !isConstruction)
                {
                    var hive = SelectionController.Instance.SelectedHive;
                    if (hive != null && hive.IsProducing)
                        displayHive = hive;
                }
            }

            // --- Construction mode: show progress bar only, no queue row ---
            if (isConstruction)
            {
                _prodRoot.SetActive(true);
                _queueRow.SetActive(false);

                _prodActiveIcon.sprite = iconBuild;
                _prodActiveIcon.color = Color.white;

                float p = Mathf.Clamp01(constructionProgress);
                _prodBarFillImg.rectTransform.anchorMax = new Vector2(p, 1f);
                _prodBarFillImg.color = new Color(0.1f, 0.8f, 0.1f, 1f);

                string bldLabel = constructionBuilders > 0
                    ? $"Building... ({constructionBuilders} worker{(constructionBuilders > 1 ? "s" : "")}) {Mathf.RoundToInt(p * 100f)}%"
                    : "Building... (no workers)";
                _prodBarLabel.text = bldLabel;
                ClearQueueSlots();
                return;
                }

                // --- Nothing producing ---
                if (displayBld == null && displayHive == null)
                {
                _prodRoot.SetActive(false);
                return;
                }

                _prodRoot.SetActive(true);
                _queueDisplayBuilding = displayBld;
                _queueDisplayHive = displayHive;

                int queueCount = displayBld != null ? displayBld.QueueCount : displayHive.QueueCount;
                float progress = displayBld != null ? displayBld.ProductionProgress : displayHive.ProductionProgress;

                // --- Row 1: active unit icon + progress bar ---
                Sprite activeIcon;
                string unitName;
                if (displayBld != null)
                {
                var arch = displayBld.GetQueuedArchetype(0);
                activeIcon = GetUnitPortrait(arch);
                unitName = ProductionBuilding.GetUnitName(arch);
                }
                else
                {
                activeIcon = portraitWorker;
                unitName = "Worker";
                }

                _prodActiveIcon.sprite = activeIcon;
                _prodActiveIcon.color = Color.white;

                float prog = Mathf.Clamp01(progress);
                _prodBarFillImg.rectTransform.anchorMax = new Vector2(prog, 1f);
                _prodBarFillImg.color = new Color(0.1f, 0.8f, 0.1f, 1f);
                _prodBarLabel.text = $"{unitName}  {Mathf.RoundToInt(prog * 100f)}%";

            // --- Row 2: queued units (index 1+) ---
            bool hasQueued = queueCount > 1;
            _queueRow.SetActive(hasQueued);

            for (int i = 0; i < QueueSlotCount; i++)
            {
                int qIdx = i + 1;
                if (qIdx < queueCount)
                {
                    Sprite icon;
                    if (displayBld != null)
                        icon = GetUnitPortrait(displayBld.GetQueuedArchetype(qIdx));
                    else
                        icon = portraitWorker;

                    _queueSlotBg[i].color = new Color(0.18f, 0.14f, 0.1f, 0.9f);
                    _queueSlotIcon[i].sprite = icon;
                    _queueSlotIcon[i].color = Color.white;
                }
                else
                {
                    _queueSlotBg[i].color = new Color(0.1f, 0.08f, 0.06f, 0.35f);
                    _queueSlotIcon[i].sprite = null;
                    _queueSlotIcon[i].color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        void ClearQueueSlots()
        {
            for (int i = 0; i < QueueSlotCount; i++)
            {
                _queueSlotBg[i].color = new Color(0.1f, 0.08f, 0.06f, 0.35f);
                _queueSlotIcon[i].sprite = null;
                _queueSlotIcon[i].color = new Color(1f, 1f, 1f, 0f);
            }
        }

        void CancelQueueSlot(int slotIndex)
        {
            int queueIndex = slotIndex + 1;
            if (_queueDisplayBuilding != null && queueIndex < _queueDisplayBuilding.QueueCount)
            {
                _queueDisplayBuilding.CancelAtIndex(queueIndex);
                ForceRebuild();
            }
            else if (_queueDisplayHive != null && queueIndex < _queueDisplayHive.QueueCount)
            {
                _queueDisplayHive.CancelAtIndex(queueIndex);
                ForceRebuild();
            }
        }

        void RefreshPendingHint()
        {
            if (_pendingHint == null) return;
            _pendingHint.text = Pending switch
            {
                PendingCommand.Move => "Click ground: Move (M)",
                PendingCommand.Attack => "Click: Attack target or Attack-move to ground (A)",
                PendingCommand.Patrol => PatrolCoordinator.WaitingForSecondPoint
                    ? "Patrol: click second waypoint (P)"
                    : "Patrol: click first waypoint (P)",
                PendingCommand.Gather => "Click resource to gather (G)",
                PendingCommand.PlaceBuilding => $"Place {BuildingName(PendingBuildingType)} inside a build zone (green ring) · Esc to cancel",
                _ => ""
            };
        }

        void RefreshSelectionGrid()
        {
            if (_selectionCells == null) return;
            SetGridMode(false);
            for (int i = 0; i < _selectionCells.Length; i++)
            {
                _selectionCells[i].color = new Color(1f, 1f, 1f, 0f);
                _selectionCellIcons[i].sprite = null;
                _selectionCellIcons[i].color = new Color(1f, 1f, 1f, 0f);
                _selectionCellHpBg[i].color = new Color(0f, 0f, 0f, 0f);
                _selectionCellHpFill[i].color = new Color(0f, 0f, 0f, 0f);
                var t = _selectionCells[i].GetComponentInChildren<Text>();
                if (t != null) t.text = "";
            }

            HideHpDisplay();
            if (_portraitMain != null) { _portraitMain.sprite = null; _portraitMain.color = new Color(1f, 1f, 1f, 0f); }
            if (_portraitBlock != null) _portraitBlock.SetActive(false);
            if (_portraitLabel != null) _portraitLabel.text = "";
            if (_attributeLabel != null) _attributeLabel.text = "";

            if (SelectionController.Instance == null) return;

            if (SelectionController.Instance.SelectedResource != null)
            {
                var res = SelectionController.Instance.SelectedResource;
                _portraitLabel.text = "Rotting Apple";
                _attributeLabel.text = "Resource - Organic";
                
                var cell0 = _selectionCells[0];
                cell0.color = new Color(0.18f, 0.14f, 0.1f, 0.9f);
                _selectionCellIcons[0].color = Color.white;
                var tx0 = cell0.GetComponentInChildren<Text>();
                if (tx0 != null)
                {
                    tx0.text = $"{res.ChargesRemaining:N0}";
                    tx0.fontSize = 12;
                }
                return;
            }

            if (SelectionController.Instance.SelectedHive != null)
            {
                var hive = SelectionController.Instance.SelectedHive;
                _portraitLabel.text = "Ant Nest";
                _attributeLabel.text = "Structure - Biological";
                if (_portraitMain != null) { _portraitMain.sprite = iconAntNest; _portraitMain.color = Color.white; }
                if (_portraitBlock != null) _portraitBlock.SetActive(true);
                ShowHpDisplay(hive.CurrentHealth, hive.MaxHealth);
                var cell0 = _selectionCells[0];
                cell0.color = new Color(0.18f, 0.14f, 0.1f, 0.9f);
                _selectionCellIcons[0].sprite = iconAntNest;
                _selectionCellIcons[0].color = Color.white;
                return;
            }

            // ── Building subgroup active: show building info ──
            if (SelectionController.Instance.SelectedBuilding != null)
            {
                var sc = SelectionController.Instance;
                var primary = sc.SelectedBuilding;

                if (_portraitMain != null) { _portraitMain.sprite = GetBuildingIcon(primary.Type); _portraitMain.color = Color.white; }
                if (_portraitBlock != null) _portraitBlock.SetActive(true);

                int activeCount = 0;
                float activeHp = 0f, activeMaxHp = 0f;
                foreach (var b in sc.SelectedBuildingsOfActiveType)
                {
                    activeCount++;
                    activeHp += b.CurrentHealth;
                    activeMaxHp += b.MaxHealth;
                }

                if (activeCount <= 1)
                {
                    _portraitLabel.text = primary.DisplayName;
                    _attributeLabel.text = primary.State == BuildingState.UnderConstruction
                        ? $"Under Construction — {Mathf.RoundToInt(primary.ConstructionProgress * 100f)}%"
                        : "Structure - Biological";
                }
                else
                {
                    _portraitLabel.text = $"{primary.DisplayName} (x{activeCount})";
                    _attributeLabel.text = "Structure - Biological";
                }

                if (sc.HasMultipleSubgroups)
                    _attributeLabel.text += " · Tab to cycle";

                ShowHpDisplay(activeHp, activeMaxHp);

                ShowMixedSelectionGrid(sc);
                return;
            }

            // ── Units subgroup active (or units only): show unit info ──
            var list = new System.Collections.Generic.List<InsectUnit>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u != null && u.IsAlive) list.Add(u);
            }

            if (list.Count == 0) return;

            var sc2 = SelectionController.Instance;

            if (list.Count == 1)
            {
                var first = list[0];
                _portraitLabel.text = first.Definition != null ? first.Definition.displayName : "Unit";
                _attributeLabel.text = GetAttributes(first);
                if (_portraitMain != null) { _portraitMain.sprite = GetUnitPortrait(first.Archetype); _portraitMain.color = Color.white; }
                if (_portraitBlock != null) _portraitBlock.SetActive(true);
                ShowHpDisplay(first.CurrentHealth, first.MaxHealth);
                if (sc2.HasMultipleSubgroups)
                {
                    _attributeLabel.text += " · Tab to cycle";
                    ShowMixedSelectionGrid(sc2);
                }
                return;
            }

            // Multi-unit: show individual unit cards with HP bars
            SetGridMode(true);
            _portraitLabel.text = $"Selected: {list.Count} units";
            _attributeLabel.text = sc2.HasMultipleSubgroups ? "Tab to cycle groups" : "";

            int cellIdx = 0;
            for (int i = 0; i < list.Count && cellIdx < _selectionCells.Length; i++)
            {
                var u = list[i];
                _selectionCells[cellIdx].color = new Color(0.18f, 0.14f, 0.1f, 0.9f);
                _selectionCellIcons[cellIdx].sprite = GetUnitPortrait(u.Archetype);
                _selectionCellIcons[cellIdx].color = Color.white;

                float frac = u.MaxHealth > 0f ? Mathf.Clamp01(u.CurrentHealth / u.MaxHealth) : 0f;
                _selectionCellHpBg[cellIdx].color = new Color(0f, 0f, 0f, 0.6f);
                _selectionCellHpFill[cellIdx].color = HpBarColor(frac);
                _selectionCellHpFill[cellIdx].rectTransform.anchorMax = new Vector2(frac, 1f);
                cellIdx++;
            }

            // Show building type chips if mixed selection with Tab cycling
            if (sc2.HasMultipleSubgroups)
            {
                var bldCounts = new System.Collections.Generic.SortedDictionary<BuildingType, int>();
                foreach (var b in sc2.SelectedBuildings)
                {
                    if (b == null || !b.IsAlive) continue;
                    bldCounts.TryGetValue(b.Type, out var n);
                    bldCounts[b.Type] = n + 1;
                }
                foreach (var kvp in bldCounts)
                {
                    if (cellIdx >= _selectionCells.Length) break;
                    _selectionCells[cellIdx].color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                    _selectionCellIcons[cellIdx].sprite = GetBuildingIcon(kvp.Key);
                    _selectionCellIcons[cellIdx].color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    var tx = _selectionCells[cellIdx].GetComponentInChildren<Text>();
                    if (tx != null) tx.text = kvp.Value > 1 ? $"{kvp.Value}" : "";
                    cellIdx++;
                }
            }
        }

        void SetGridMode(bool multiUnit)
        {
            if (_selectionGridRt == null) return;
            if (_gridInMultiUnitMode == multiUnit) return;
            _gridInMultiUnitMode = multiUnit;
            if (multiUnit)
            {
                _selectionGridRt.anchorMin = new Vector2(0.02f, 0.08f);
                _selectionGridRt.anchorMax = new Vector2(0.98f, 0.85f);
                _selectionGridRt.pivot = new Vector2(0.5f, 0.5f);
                _selectionGridRt.anchoredPosition = Vector2.zero;
                _selectionGridRt.sizeDelta = Vector2.zero;
                _selectionGridLayout.cellSize = new Vector2(50f, 60f);
                _selectionGridLayout.spacing = new Vector2(5f, 5f);
                _selectionGridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
                _selectionGridLayout.childAlignment = TextAnchor.MiddleCenter;
            }
            else
            {
                _selectionGridRt.anchorMin = new Vector2(0.40f, 1f);
                _selectionGridRt.anchorMax = new Vector2(1f, 1f);
                _selectionGridRt.pivot = new Vector2(0f, 1f);
                _selectionGridRt.anchoredPosition = new Vector2(0f, -8f);
                _selectionGridRt.sizeDelta = new Vector2(0f, 51f);
                _selectionGridLayout.cellSize = new Vector2(42f, 42f);
                _selectionGridLayout.spacing = new Vector2(5f, 5f);
                _selectionGridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                _selectionGridLayout.constraintCount = 1;
                _selectionGridLayout.childAlignment = TextAnchor.MiddleLeft;
            }
        }

        /// <summary>Show the selection grid with all unit archetypes + building types, sorted for Tab order.</summary>
        void ShowMixedSelectionGrid(SelectionController sc)
        {
            int cellIdx = 0;
            bool hasActiveType = sc.ActiveBuildingType.HasValue;

            var unitCounts = new Dictionary<UnitArchetype, int>();
            foreach (var u in sc.SelectedPlayerUnits())
            {
                unitCounts.TryGetValue(u.Archetype, out var n);
                unitCounts[u.Archetype] = n + 1;
            }
            var unitOrder = new[] { UnitArchetype.Worker, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.BlackWidow, UnitArchetype.StickSpy, UnitArchetype.GiantStagBeetle };
            bool unitsAreActive = !hasActiveType && unitCounts.Count > 0;
            foreach (var arch in unitOrder)
            {
                if (!unitCounts.TryGetValue(arch, out var cnt)) continue;
                if (cellIdx >= _selectionCells.Length) break;
                _selectionCells[cellIdx].color = new Color(0.18f, 0.14f, 0.1f, 0.7f);
                _selectionCellIcons[cellIdx].sprite = GetUnitPortrait(arch);
                _selectionCellIcons[cellIdx].color = unitsAreActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                var tx = _selectionCells[cellIdx].GetComponentInChildren<Text>();
                if (tx != null) tx.text = cnt > 1 ? $"{cnt}" : "";
                cellIdx++;
            }

            var bldCounts = new SortedDictionary<BuildingType, int>();
            foreach (var b in sc.SelectedBuildings)
            {
                if (b == null || !b.IsAlive) continue;
                bldCounts.TryGetValue(b.Type, out var n);
                bldCounts[b.Type] = n + 1;
            }
            foreach (var kvp in bldCounts)
            {
                if (cellIdx >= _selectionCells.Length) break;
                bool isActive = hasActiveType && kvp.Key == sc.ActiveBuildingType.Value;
                _selectionCells[cellIdx].color = new Color(0.18f, 0.14f, 0.1f, 0.7f);
                _selectionCellIcons[cellIdx].sprite = GetBuildingIcon(kvp.Key);
                _selectionCellIcons[cellIdx].color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                var tx = _selectionCells[cellIdx].GetComponentInChildren<Text>();
                if (tx != null) tx.text = kvp.Value > 1 ? $"{kvp.Value}" : "";
                cellIdx++;
            }
        }

        string GetAttributes(InsectUnit u)
        {
            if (u.Archetype == UnitArchetype.Worker) return "Light - Biological - Worker";
            if (u.Archetype == UnitArchetype.BasicFighter) return "Armored - Biological - Fighter";
            if (u.Archetype == UnitArchetype.BasicRanged) return "Light - Biological - Ranged";
            if (u.Archetype == UnitArchetype.BlackWidow) return "Armored - Biological - Assassin";
            if (u.Archetype == UnitArchetype.StickSpy) return "Light - Biological - Spy";
            if (u.Archetype == UnitArchetype.GiantStagBeetle) return "Heavy - Biological - Tank";
            return "Biological";
        }

        Sprite GetUnitPortrait(UnitArchetype a)
        {
            return a switch
            {
                UnitArchetype.Worker => portraitWorker,
                UnitArchetype.BasicFighter => portraitFighter,
                UnitArchetype.BasicRanged => portraitRanged,
                UnitArchetype.BlackWidow => portraitBlackWidow,
                UnitArchetype.StickSpy => portraitStick,
                UnitArchetype.GiantStagBeetle => portraitStagBeetle,
                _ => null
            };
        }

        void ShowHpDisplay(float current, float max)
        {
            if (_hpLabel == null) return;
            _hpLabel.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(true);
            if (_hpBarFill != null)
            {
                float frac = max > 0f ? Mathf.Clamp01(current / max) : 0f;
                _hpBarFill.rectTransform.anchorMax = new Vector2(frac, 1f);
                _hpBarFill.color = HpBarColor(frac);
            }
        }

        void HideHpDisplay()
        {
            if (_hpLabel != null) _hpLabel.text = "";
            if (_hpBarBg != null) _hpBarBg.gameObject.SetActive(false);
        }

        static Color HpBarColor(float frac)
        {
            if (frac > 0.5f)
            {
                float t = (frac - 0.5f) * 2f;
                return Color.Lerp(new Color(0.95f, 0.75f, 0.35f), new Color(0.25f, 0.85f, 0.35f), t);
            }
            float t2 = frac * 2f;
            return Color.Lerp(new Color(0.95f, 0.15f, 0.1f), new Color(0.95f, 0.75f, 0.35f), t2);
        }

        static string BuildingName(BuildingType t)
        {
            return t switch
            {
                BuildingType.Underground => "Underground",
                BuildingType.AntNest => "Ant's Nest",
                BuildingType.SkyTower => "Sky Tower",
                BuildingType.RootCellar => "Root Cellar",
                _ => t.ToString()
            };
        }
    }
}
