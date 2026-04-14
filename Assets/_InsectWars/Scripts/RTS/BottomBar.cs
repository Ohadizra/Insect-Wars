using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] Sprite iconEvolve;
        [SerializeField] Sprite iconClearRally;

        [Header("Unit Portraits")]
        [SerializeField] Sprite portraitWorker;
        [SerializeField] Sprite portraitFighter;
        [SerializeField] Sprite portraitRanged;

        static readonly Color ColTitle = new(0.96f, 0.90f, 0.78f);
        static readonly Color ColSub   = new(0.83f, 0.69f, 0.44f);
        static readonly Color ColOutline = new(0.1f, 0.08f, 0.06f, 0.8f);

        Image[] _selectionCells;
        Image _portraitMain;
        Text _portraitLabel;
        Text _hpLabel;
        Text _attributeLabel;
        Image _hpBarBg;
        Image _hpBarFill;
        Text _pendingHint;
        Font _font;
        Transform _cmdGridParent;
        readonly Dictionary<string, Image> _cmdButtonImages = new();

        GameObject _prodBarRoot;
        Image _prodBarFill;
        Text _prodLabel;

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

        #if UNITY_EDITOR
            string p = "Assets/_InsectWars/Sprites/UI/Extracted/";
            string np = "Assets/_InsectWars/Sprites/UI/NewIcons/";
            
            if (centerBlockFrame == null) centerBlockFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            
            // Set the minimap frame to match the other blocks (centerBlockFrame)
            if (minimapFrame == null) minimapFrame = centerBlockFrame;
            if (minimapFrame == null) minimapFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_ornate.png");
            if (minimapFrame == null) minimapFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/frame_minimap_hollow.png");

            if (commandCardFrame == null) commandCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_action_grid_empty.png");
            if (portraitFrame == null) portraitFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_portrait.png");
            if (centerBlockFrame == null) centerBlockFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            if (slotFrame == null) slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");

            // Load symbolic icons instead of insect art
            iconMove = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_move.png");
            iconStop = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_stop.png");
            iconHold = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_hold.png");
            iconPatrol = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_patrol.png");
            iconAttack = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_attack.png");
            iconGather = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_gather.png");
            iconBuild = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_build.png");
            iconCancel = AssetDatabase.LoadAssetAtPath<Sprite>(np + "icon_cancel.png");
            
            if (iconWorker == null) iconWorker = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_larva.png");
            if (iconFighter == null) iconFighter = AssetDatabase.LoadAssetAtPath<Sprite>(p + "beetle_top_view.png");
            if (iconRanged == null) iconRanged = AssetDatabase.LoadAssetAtPath<Sprite>(p + "beetle_side_view.png");

            if (portraitWorker == null) portraitWorker = AssetDatabase.LoadAssetAtPath<Sprite>(p + "portrait_stag_beetle.png");
            if (portraitFighter == null) portraitFighter = AssetDatabase.LoadAssetAtPath<Sprite>(p + "beetle_top_alt.png");
            if (portraitRanged == null) portraitRanged = AssetDatabase.LoadAssetAtPath<Sprite>(p + "beetle_side_alt.png");
        #endif
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

            // Tab cycles building subgroups when multiple building types are selected
            if (Keyboard.current.tabKey.wasPressedThisFrame &&
                SelectionController.Instance != null &&
                SelectionController.Instance.HasMultipleBuildingTypes)
            {
                SelectionController.Instance.CycleBuildingSubgroup();
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
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
                    // Cancel production on first producing building of active type
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
            if (Keyboard.current.gKey.wasPressedThisFrame && SelectionController.Instance.HasWorkerSelected())
                SetPending(PendingCommand.Gather);
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
                    AddCmdButton(_cmdGridParent, "Gather", "G", () => SetPending(PendingCommand.Gather));
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
            mcrt.anchorMax = new Vector2(0f, 1f);
            mcrt.pivot = new Vector2(0f, 0.5f);
            mcrt.anchoredPosition = new Vector2(30, 0);
            mcrt.sizeDelta = new Vector2(minimapSlot - 40, -10);

            var miniInner = new GameObject("MinimapInner");
            miniInner.transform.SetParent(miniContainer.transform, false);
            var mi = miniInner.AddComponent<RectTransform>();
            mi.anchorMin = Vector2.zero;
            mi.anchorMax = Vector2.one;
            // Bring the map slightly inward so it fits nicely inside the frame
            mi.offsetMin = new Vector2(10, 10);
            mi.offsetMax = new Vector2(-10, -10);
            MinimapHost = mi;

            var miniFrameGo = new GameObject("MinimapFrame");
            miniFrameGo.transform.SetParent(miniContainer.transform, false);
            var mfrt = miniFrameGo.AddComponent<RectTransform>();
            mfrt.anchorMin = Vector2.zero;
            mfrt.anchorMax = Vector2.one;
            // Expand the ornate frame significantly to cover the background slot
            mfrt.offsetMin = new Vector2(-35, -35);
            mfrt.offsetMax = new Vector2(35, 35);
            var frameImg = miniFrameGo.AddComponent<Image>();
            frameImg.sprite = minimapFrame;
            frameImg.raycastTarget = false; // Fix: don't block minimap clicks
            frameImg.type = Image.Type.Sliced; // Changed to Sliced to match other panels
            frameImg.fillCenter = false; // Keep the map area clear
            frameImg.color = Color.white;

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
            gl.cellSize = new Vector2(48f, 48f);
            gl.spacing = new Vector2(76f, 13f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.MiddleCenter;
            
            _cmdGridParent = grid.transform;

            var center = new GameObject("SelectionBlock");
            center.transform.SetParent(bar.transform, false);
            var cr = center.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 0f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.offsetMin = new Vector2(minimapSlot, 8f);
            cr.offsetMax = new Vector2(-commandPanelWidth, -8f);
            var centerBg = center.AddComponent<Image>();
            centerBg.sprite = centerBlockFrame;
            centerBg.type = Image.Type.Sliced;
            centerBg.fillCenter = true; // Enable background
            centerBg.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Slight tint
            centerBg.raycastTarget = true; 

            var portrait = new GameObject("PortraitBlock");
            portrait.transform.SetParent(center.transform, false);
            var pr = portrait.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0.5f);
            pr.anchorMax = new Vector2(0f, 0.5f);
            pr.pivot = new Vector2(0f, 0.5f);
            pr.anchoredPosition = new Vector2(34f, 0f);
            pr.sizeDelta = new Vector2(128f, 128f);
            var pImg = portrait.AddComponent<Image>();
            pImg.sprite = portraitFrame;
            pImg.type = Image.Type.Simple; 
            pImg.color = Color.white;

            var portraitSub = new GameObject("PortraitInner");
            portraitSub.transform.SetParent(portrait.transform, false);
            var psr = portraitSub.AddComponent<RectTransform>();
            psr.anchorMin = Vector2.zero;
            psr.anchorMax = Vector2.one;
            psr.offsetMin = new Vector2(21f, 21f);
            psr.offsetMax = new Vector2(-21f, -21f);
            _portraitMain = portraitSub.AddComponent<Image>();
            _portraitMain.preserveAspect = true;

            var infoBlock = new GameObject("InfoBlock");
            infoBlock.transform.SetParent(center.transform, false);
            var ibr = infoBlock.AddComponent<RectTransform>();
            ibr.anchorMin = new Vector2(0f, 0f);
            ibr.anchorMax = new Vector2(1f, 1f);
            ibr.pivot = new Vector2(0.5f, 0.5f);
            ibr.offsetMin = new Vector2(178f, 8f);
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

            _hpLabel = CreateText("HpText", infoBlock.transform, 13, Color.white, TextAnchor.MiddleCenter);
            var hpRt = _hpLabel.rectTransform;
            hpRt.anchorMin = new Vector2(0f, 0.45f);
            hpRt.anchorMax = new Vector2(1f, 0.55f);
            hpRt.offsetMin = hpRt.offsetMax = Vector2.zero;

            _hpBarBg = new GameObject("HpBarBg").AddComponent<Image>();
            _hpBarBg.transform.SetParent(infoBlock.transform, false);
            _hpBarBg.color = new Color(0f, 0f, 0f, 0.4f); 
            var hbr = _hpBarBg.rectTransform;
            hbr.anchorMin = new Vector2(0.1f, 0.35f);
            hbr.anchorMax = new Vector2(0.9f, 0.40f);
            hbr.offsetMin = hbr.offsetMax = Vector2.zero;

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

            var gridLayout = gridRoot.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(42f, 42f);
            gridLayout.spacing = new Vector2(5f, 5f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = 1;

            _selectionCells = new Image[24];
            for (var i = 0; i < 24; i++)
            {
                var cell = new GameObject($"Sel_{i}");
                cell.transform.SetParent(gridRoot.transform, false);
                var img = cell.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = false; 
                var tx = CreateText("t", cell.transform, 12, Color.white, TextAnchor.LowerRight);
                var trt = tx.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = new Vector2(2f, 2f);
                _selectionCells[i] = img;
            }

            _pendingHint = CreateText("PendingHint", hud, 15, ColTitle, TextAnchor.MiddleCenter);
            var ph = _pendingHint.rectTransform;
            ph.anchorMin = new Vector2(0.5f, 0.30f);
            ph.anchorMax = new Vector2(0.5f, 0.30f);
            ph.anchoredPosition = Vector2.zero;
            ph.sizeDelta = new Vector2(680f, 28f);

            _prodBarRoot = new GameObject("ProdBar");
            _prodBarRoot.transform.SetParent(infoBlock.transform, false);
            var pbr = _prodBarRoot.AddComponent<RectTransform>();
            pbr.anchorMin = new Vector2(0.05f, 0.65f);
            pbr.anchorMax = new Vector2(0.95f, 0.72f);
            pbr.offsetMin = pbr.offsetMax = Vector2.zero;
            var pbBg = _prodBarRoot.AddComponent<Image>();
            pbBg.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black

            var fillGo = new GameObject("ProdFill");
            fillGo.transform.SetParent(_prodBarRoot.transform, false);
            _prodBarFill = fillGo.AddComponent<Image>();
            _prodBarFill.color = new Color(0.95f, 0.75f, 0.35f, 1f);
            var pfr = _prodBarFill.rectTransform;
            pfr.anchorMin = Vector2.zero;
            pfr.anchorMax = new Vector2(0f, 1f);
            pfr.offsetMin = pfr.offsetMax = Vector2.zero;

            _prodLabel = CreateText("ProdText", _prodBarRoot.transform, 11, Color.white, TextAnchor.MiddleCenter);
            var plr = _prodLabel.rectTransform;
            plr.anchorMin = Vector2.zero;
            plr.anchorMax = Vector2.one;
            plr.offsetMin = plr.offsetMax = Vector2.zero;

            _prodBarRoot.SetActive(false);
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

            var keyLabel = CreateText("Key", go.transform, 11, ColTitle, TextAnchor.UpperRight);
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
                BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged },
                BuildingType.AntNest => new[] { UnitArchetype.Worker },
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
            if (cmdName.Contains("Underground")) return iconUnderground;
            if (cmdName.Contains("Sky Tower")) return iconSkyTower;
            if (cmdName.Contains("Ant's Nest")) return iconAntNest;
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
            var hive = HiveDeposit.PlayerHive;
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

        /// <summary>Queue one unit in each operational building of the active type.</summary>
        void ProduceFromAllActiveBuildings(UnitArchetype archetype)
        {
            if (SelectionController.Instance == null) return;
            foreach (var b in SelectionController.Instance.SelectedBuildingsOfActiveType)
            {
                if (b.IsOperational)
                    b.QueueUnit(archetype);
            }
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

        static void ClearBuildingRally(ProductionBuilding bld)
        {
            if (bld != null) bld.ClearRally();
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
            if (_prodBarRoot == null) return;

            float progress = 0f;
            string label = null;
            int queueCount = 0;
            Color barColor = new Color(0.3f, 0.7f, 1f);

            if (SelectionController.Instance != null)
            {
                var bld = SelectionController.Instance.SelectedBuilding;
                if (bld != null && bld.State == BuildingState.UnderConstruction)
                {
                    progress = bld.ConstructionProgress;
                    int builders = bld.AssignedBuilders;
                    label = builders > 0 ? $"Building... ({builders} worker{(builders > 1 ? "s" : "")})" : "Building... (no workers)";
                    barColor = new Color(0.95f, 0.75f, 0.2f);
                }
                else if (bld != null)
                {
                    // Show production of the first producing building of the active type
                    foreach (var ab in SelectionController.Instance.SelectedBuildingsOfActiveType)
                    {
                        if (!ab.IsProducing) continue;
                        progress = ab.ProductionProgress;
                        var arch = ab.CurrentProducing;
                        label = arch.HasValue ? ProductionBuilding.GetUnitName(arch.Value) : "Unit";
                        queueCount = ab.QueueCount;
                        break;
                    }
                }

                if (label == null)
                {
                    var hive = SelectionController.Instance.SelectedHive;
                    if (hive != null && hive.IsProducing)
                    {
                        progress = hive.ProductionProgress;
                        label = "Worker";
                        queueCount = hive.QueueCount;
                    }
                }
            }

            if (label == null)
            {
                _prodBarRoot.SetActive(false);
                return;
            }

            _prodBarRoot.SetActive(true);
            _prodBarFill.color = barColor;
            _prodBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            string queueSuffix = queueCount > 1 ? $" [{queueCount}]" : "";
            _prodLabel.text = $"{label} {Mathf.RoundToInt(progress * 100f)}%{queueSuffix}";
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
            foreach (var c in _selectionCells)
            {
                c.color = new Color(1f, 1f, 1f, 0f);
                c.sprite = null;
                var t = c.GetComponentInChildren<Text>();
                if (t != null) t.text = "";
            }

            HideHpDisplay();
            if (_portraitMain != null) { _portraitMain.sprite = null; _portraitMain.color = new Color(1f, 1f, 1f, 0f); }
            if (_portraitLabel != null) _portraitLabel.text = "";
            if (_attributeLabel != null) _attributeLabel.text = "";

            if (SelectionController.Instance == null) return;

            if (SelectionController.Instance.SelectedResource != null)
            {
                var res = SelectionController.Instance.SelectedResource;
                _portraitLabel.text = "Rotting Apple";
                _attributeLabel.text = "Resource - Organic";
                
                var cell0 = _selectionCells[0];
                cell0.color = Color.white;
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
                if (_portraitMain != null) { _portraitMain.sprite = portraitWorker; _portraitMain.color = Color.white; }
                ShowHpDisplay(hive.CurrentHealth, hive.MaxHealth);
                var cell0 = _selectionCells[0];
                cell0.color = Color.white;
                cell0.sprite = portraitWorker;
                return;
            }

            if (SelectionController.Instance.SelectedBuilding != null)
            {
                var sc = SelectionController.Instance;
                var primary = sc.SelectedBuilding;

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

                if (sc.HasMultipleBuildingTypes)
                {
                    _attributeLabel.text += " · Tab to cycle";
                }

                ShowHpDisplay(activeHp, activeMaxHp);

                var bldCounts = new Dictionary<BuildingType, int>();
                foreach (var b in sc.SelectedBuildings)
                {
                    if (b == null || !b.IsAlive) continue;
                    bldCounts.TryGetValue(b.Type, out var n);
                    bldCounts[b.Type] = n + 1;
                }
                int cellIdx = 0;
                foreach (var kvp in bldCounts)
                {
                    if (cellIdx >= _selectionCells.Length) break;
                    var cell = _selectionCells[cellIdx];
                    bool isActive = sc.ActiveBuildingType.HasValue && kvp.Key == sc.ActiveBuildingType.Value;
                    cell.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    var tx = cell.GetComponentInChildren<Text>();
                    if (tx != null) tx.text = kvp.Value > 1 ? $"{kvp.Value}" : "";
                    cellIdx++;
                }
                return;
            }

            var list = new System.Collections.Generic.List<InsectUnit>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u != null && u.IsAlive) list.Add(u);
            }

            if (list.Count == 0) return;

            var first = list[0];
            _portraitLabel.text = first.Definition != null ? first.Definition.displayName : "Unit";
            _attributeLabel.text = GetAttributes(first);
            if (_portraitMain != null) { _portraitMain.sprite = GetUnitPortrait(first.Archetype); _portraitMain.color = Color.white; }

            float totalHp = 0f, totalMax = 0f;
            foreach (var u in list)
            {
                totalHp += u.CurrentHealth;
                totalMax += u.MaxHealth;
            }
            ShowHpDisplay(totalHp, totalMax);

            var counts = new System.Collections.Generic.Dictionary<UnitArchetype, int>();
            foreach (var u in list)
            {
                var a = u.Archetype;
                counts.TryGetValue(a, out var n);
                counts[a] = n + 1;
            }

            var order = new[] { UnitArchetype.Worker, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
            var idx = 0;
            foreach (var arch in order)
            {
                if (!counts.TryGetValue(arch, out var cnt)) continue;
                if (idx >= _selectionCells.Length) break;
                var img = _selectionCells[idx];
                img.sprite = GetUnitPortrait(arch);
                img.color = Color.white;
                var tx = img.GetComponentInChildren<Text>();
                if (tx != null) tx.text = cnt > 1 ? $"{cnt}" : "";
                idx++;
            }
        }

        string GetAttributes(InsectUnit u)
        {
            if (u.Archetype == UnitArchetype.Worker) return "Light - Biological - Worker";
            if (u.Archetype == UnitArchetype.BasicFighter) return "Armored - Biological - Fighter";
            if (u.Archetype == UnitArchetype.BasicRanged) return "Light - Biological - Ranged";
            return "Biological";
        }

        Sprite GetUnitPortrait(UnitArchetype a)
        {
            return a switch
            {
                UnitArchetype.Worker => portraitWorker,
                UnitArchetype.BasicFighter => portraitFighter,
                UnitArchetype.BasicRanged => portraitRanged,
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
