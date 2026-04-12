using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
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

        enum BarMode { None, Units, WorkerUnits, Hive, Resource, BuildMenu, Building }
        BarMode _currentBarMode = (BarMode)(-1);

        void Awake()
        {
            Instance = this;
            _font = UiFontHelper.GetFont();

        #if UNITY_EDITOR
            string p = "Assets/_InsectWars/Sprites/UI/Extracted/";
            if (minimapFrame == null) minimapFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/frame_minimap_hollow.png");
            if (minimapFrame == null) minimapFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_InsectWars/Sprites/UI/frame_minimap_picture.png");
            if (minimapFrame == null) minimapFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_ornate.png");
if (commandCardFrame == null) commandCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_action_grid_empty.png");
if (portraitFrame == null) portraitFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_portrait.png");
            if (centerBlockFrame == null) centerBlockFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");
            if (slotFrame == null) slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(p + "frame_square_panel.png");

            if (iconMove == null) iconMove = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_running_person.png");
            if (iconAttack == null) iconAttack = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_beetle_action.png");
            if (iconBuild == null) iconBuild = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_hammer_wrench.png");
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
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MinimapHost = null;
            Pending = PendingCommand.None;
            DestroyGhost();
        }

        void Update()
        {
            HandleHotkeys();
        }

        void HandleHotkeys()
        {
            if (Keyboard.current == null) return;

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
                    var selBld = SelectionController.Instance.SelectedBuilding;
                    if (selBld != null && selBld.IsProducing)
                    {
                        selBld.CancelLast();
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
                    ProduceFromBuilding(selectedBuilding, producible[0]);
                if (Keyboard.current.wKey.wasPressedThisFrame && producible.Length > 1)
                    ProduceFromBuilding(selectedBuilding, producible[1]);
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    ClearBuildingRally(selectedBuilding);
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
                var bld = SelectionController.Instance.SelectedBuilding;
                if (bld != null && bld.IsProducing) return true;
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
                var bld = sc.SelectedBuilding;
                if (bld != null) queueSnap = bld.QueueCount;
            }
            else if (newMode == BarMode.Hive)
            {
                var hive = sc.SelectedHive;
                if (hive != null) queueSnap = hive.QueueCount;
            }

            if (newMode != _currentBarMode || queueSnap != _lastQueueSnapshot)
            {
                _currentBarMode = newMode;
                _lastQueueSnapshot = queueSnap;
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
                    AddCmdButton(_cmdGridParent, $"Underground\n<size=11>{ProductionBuilding.GetBuildCost(BuildingType.Underground)} cal</size>", "Q", () => StartPlaceBuilding(BuildingType.Underground));
                    AddCmdButton(_cmdGridParent, $"Sky Tower\n<size=11>{ProductionBuilding.GetBuildCost(BuildingType.SkyTower)} cal</size>", "W", () => StartPlaceBuilding(BuildingType.SkyTower));
                    AddCmdButton(_cmdGridParent, $"Ant's Nest\n<size=11>{ProductionBuilding.GetBuildCost(BuildingType.AntNest)} cal</size>", "E", () => StartPlaceBuilding(BuildingType.AntNest));
                    AddCmdButton(_cmdGridParent, $"Root Cellar\n<size=11>{ProductionBuilding.GetBuildCost(BuildingType.RootCellar)} cal</size>", "R", () => StartPlaceBuilding(BuildingType.RootCellar));
                    AddCmdButton(_cmdGridParent, "Cancel", "Esc", () => { _buildMenuActive = false; ForceRebuild(); });
                    break;
                case BarMode.Hive:
                    AddCmdButton(_cmdGridParent, $"Worker\n<size=11>{ProductionBuilding.GetUnitCost(UnitArchetype.Worker)} cal</size>", "W", BuildWorker);
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
                            // No production buttons while under construction — workers must build first
                        }
                        else
                        {
                            string[] hotkeys = { "Q", "W", "E", "R" };
                            var units = bld.ProducibleUnits;
                            for (int i = 0; i < units.Length; i++)
                            {
                                var arch = units[i];
                                string hk = i < hotkeys.Length ? hotkeys[i] : "";
                                AddCmdButton(_cmdGridParent, $"{ProductionBuilding.GetUnitName(arch)}\n<size=11>{ProductionBuilding.GetUnitCost(arch)} cal</size>", hk, () => ProduceFromBuilding(bld, arch));
                            }
                            if (bld.IsProducing)
                                AddCmdButton(_cmdGridParent, "Cancel", "Esc", () => bld.CancelLast());
                            AddCmdButton(_cmdGridParent, "Clear Rally", "R", () => ClearBuildingRally(bld));
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
            mi.offsetMin = new Vector2(15, 15);
            mi.offsetMax = new Vector2(-15, -15);
            MinimapHost = mi;

            var miniFrameGo = new GameObject("MinimapFrame");
            miniFrameGo.transform.SetParent(miniContainer.transform, false);
            var mfrt = miniFrameGo.AddComponent<RectTransform>();
            mfrt.anchorMin = Vector2.zero;
            mfrt.anchorMax = Vector2.one;
            mfrt.offsetMin = Vector2.zero;
            mfrt.offsetMax = Vector2.zero;
            var frameImg = miniFrameGo.AddComponent<Image>();
            frameImg.sprite = minimapFrame;
            frameImg.raycastTarget = false; // Fix: don't block minimap clicks
            frameImg.type = Image.Type.Simple;

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

            var label = CreateText("Label", go.transform, 9, ColTitle, TextAnchor.UpperCenter);
            label.supportRichText = true;
            label.text = name;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(-0.2f, -0.6f);
            lrt.anchorMax = new Vector2(1.2f, -0.1f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _cmdButtonImages[name] = img;
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
            PendingBuildingType = type;
            SetPending(PendingCommand.PlaceBuilding);
            CreateGhost(type);
            ForceRebuild();
        }

        void ProduceFromBuilding(ProductionBuilding bld, UnitArchetype archetype)
        {
            if (bld != null) bld.QueueUnit(archetype);
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
            Vector3 scale;
            Color col;
            switch (type)
            {
                case BuildingType.Underground:
                    scale = new Vector3(4f, 2f, 4f);
                    col = new Color(0.35f, 0.25f, 0.45f, 0.4f);
                    break;
                case BuildingType.AntNest:
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    col = new Color(0.5f, 0.35f, 0.2f, 0.4f);
                    break;
                case BuildingType.RootCellar:
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    col = new Color(0.5f, 0.35f, 0.2f, 0.4f);
                    break;
                case BuildingType.SkyTower:
                    scale = new Vector3(2.5f, 5f, 2.5f);
                    col = new Color(0.3f, 0.5f, 0.6f, 0.4f);
                    break;
                default:
                    scale = new Vector3(3f, 2f, 3f);
                    col = new Color(0.5f, 0.5f, 0.5f, 0.4f);
                    break;
            }
            _ghostPreview.transform.localScale = scale;
            _ghostPreview.transform.position = new Vector3(0f, scale.y * 0.5f, 0f);
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(sh);
            if (mat.HasProperty("_Color")) mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            _ghostPreview.GetComponent<Renderer>().sharedMaterial = mat;
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
                var halfY = _ghostPreview.transform.localScale.y * 0.5f;
                _ghostPreview.transform.position = new Vector3(worldPos.x, terrainY + halfY, worldPos.z);

                bool valid = BuildZoneRegistry.IsInBuildZone(worldPos);
                var tint = valid
                    ? new Color(0.3f, 0.85f, 0.4f, 0.45f)
                    : new Color(0.85f, 0.25f, 0.2f, 0.45f);
                var r = _ghostPreview.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
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
                var bld = SelectionController.Instance.SelectedBuilding;
                if (bld != null && bld.RallyPoint.HasValue &&
                    _cmdButtonImages.TryGetValue("Clear Rally", out var rallyImg2))
                    rallyImg2.color = CmdActive;
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
                else if (bld != null && bld.IsProducing)
                {
                    progress = bld.ProductionProgress;
                    var arch = bld.CurrentProducing;
                    label = arch.HasValue ? ProductionBuilding.GetUnitName(arch.Value) : "Unit";
                    queueCount = bld.QueueCount;
                }
                else
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
                var bld = SelectionController.Instance.SelectedBuilding;
                _portraitLabel.text = bld.DisplayName;
                _attributeLabel.text = bld.State == BuildingState.UnderConstruction
                    ? $"Under Construction — {Mathf.RoundToInt(bld.ConstructionProgress * 100f)}%"
                    : "Structure - Biological";
                ShowHpDisplay(bld.CurrentHealth, bld.MaxHealth);
                var cell0 = _selectionCells[0];
                cell0.color = Color.white;
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
