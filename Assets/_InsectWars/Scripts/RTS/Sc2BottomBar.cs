using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
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

    /// <summary>
    /// StarCraft-style bottom bar: minimap host (left), selection grid (center), command card (right).
    /// Command card rebuilds dynamically based on what is selected (units / hive / resource).
    /// </summary>
    public class Sc2BottomBar : MonoBehaviour
    {
        public static Sc2BottomBar Instance { get; private set; }

        public static RectTransform MinimapHost { get; private set; }
        public static PendingCommand Pending { get; private set; }
        public static BuildingType PendingBuildingType { get; private set; }

        [SerializeField] float barHeight = 208f;
        [SerializeField] float minimapSlot = 200f;
        [SerializeField] float commandPanelWidth = 360f;

        Image[] _selectionCells;
        Text _portraitLabel;
        Text _hpLabel;
        Image _hpBarBg;
        Image _hpBarFill;
        Text _pendingHint;
        Font _font;
        Transform _cmdGridParent;
        readonly Dictionary<string, Image> _cmdButtonImages = new();

        bool _buildMenuActive;
        GameObject _ghostPreview;
        Camera _cam;

        enum BarMode { None, Units, WorkerUnits, Hive, Resource, CactiSeed, BuildMenu, Building }
        BarMode _currentBarMode = (BarMode)(-1);

        void Awake()
        {
            Instance = this;
            _font = UiFontHelper.GetFont();
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
                else
                    SetPending(PendingCommand.None);
                return;
            }

            if (SelectionController.Instance == null) return;

            if (_buildMenuActive)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.MantisBranch);
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    StartPlaceBuilding(BuildingType.AntNest);
                return;
            }

            var selectedBuilding = SelectionController.Instance.SelectedBuilding;
            if (selectedBuilding != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                    ProduceFromBuilding(selectedBuilding);
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    ClearBuildingRally(selectedBuilding);
                return;
            }

            if (SelectionController.Instance.SelectedHive != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame)
                    BuildWorker();
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
            UpdateGhostPreview();
        }

        public static bool SuppressSelectionDrag =>
            Pending != PendingCommand.None || PatrolCoordinator.WaitingForSecondPoint;

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
            else if (sc.SelectedSeed != null)
                newMode = BarMode.CactiSeed;
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

            if (newMode != _currentBarMode)
            {
                _currentBarMode = newMode;
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
                    AddCmdButton(_cmdGridParent, "Manti's Branch\n<size=11>150 cal</size>", "Q",
                        () => StartPlaceBuilding(BuildingType.MantisBranch));
                    AddCmdButton(_cmdGridParent, "Ant's Nest\n<size=11>400 cal</size>", "W",
                        () => StartPlaceBuilding(BuildingType.AntNest));
                    AddCmdButton(_cmdGridParent, "Cancel", "Esc", () =>
                    {
                        _buildMenuActive = false;
                        ForceRebuild();
                    });
                    break;
                case BarMode.Hive:
                    AddCmdButton(_cmdGridParent, "Worker\n<size=11>50 cal</size>", "W", BuildWorker);
                    AddCmdButton(_cmdGridParent, "Clear Rally", "R", ClearRally);
                    break;
                case BarMode.Building:
                {
                    var bld = SelectionController.Instance?.SelectedBuilding;
                    if (bld != null)
                    {
                        AddCmdButton(_cmdGridParent,
                            $"{bld.UnitName}\n<size=11>{bld.UnitCost} cal</size>", "Q",
                            () => ProduceFromBuilding(bld));
                        AddCmdButton(_cmdGridParent, "Clear Rally", "R",
                            () => ClearBuildingRally(bld));
                    }
                    break;
                }
            }
        }

        void BuildBar()
        {
            var hud = GameHUD.HudCanvasRect;
            if (hud == null) return;

            var bar = new GameObject("SC2BottomBar");
            bar.transform.SetParent(hud, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, barHeight);

            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.96f);
            bg.raycastTarget = true;

            var topLine = new GameObject("TopAccent");
            topLine.transform.SetParent(bar.transform, false);
            var tl = topLine.AddComponent<RectTransform>();
            tl.anchorMin = new Vector2(0f, 1f);
            tl.anchorMax = new Vector2(1f, 1f);
            tl.pivot = new Vector2(0.5f, 1f);
            tl.sizeDelta = new Vector2(0f, 3f);
            tl.anchoredPosition = Vector2.zero;
            var timg = topLine.AddComponent<Image>();
            timg.color = new Color(0.15f, 0.35f, 0.2f, 1f);
            timg.raycastTarget = false;

            float pad = 8f;
            var miniSlot = new GameObject("MinimapHost");
            miniSlot.transform.SetParent(bar.transform, false);
            var ms = miniSlot.AddComponent<RectTransform>();
            ms.anchorMin = new Vector2(0f, 0f);
            ms.anchorMax = new Vector2(0f, 0f);
            ms.pivot = new Vector2(0f, 0f);
            ms.anchoredPosition = new Vector2(pad, pad);
            ms.sizeDelta = new Vector2(minimapSlot, barHeight - pad * 2f);
            var msBorder = miniSlot.AddComponent<Image>();
            msBorder.color = new Color(0.12f, 0.14f, 0.16f, 1f);
            msBorder.raycastTarget = false;

            var miniInner = new GameObject("MinimapInner");
            miniInner.transform.SetParent(miniSlot.transform, false);
            var mi = miniInner.AddComponent<RectTransform>();
            mi.anchorMin = Vector2.zero;
            mi.anchorMax = Vector2.one;
            mi.offsetMin = new Vector2(3f, 3f);
            mi.offsetMax = new Vector2(-3f, -3f);
            MinimapHost = mi;

            var cmdPanel = new GameObject("CommandPanel");
            cmdPanel.transform.SetParent(bar.transform, false);
            var cp = cmdPanel.AddComponent<RectTransform>();
            cp.anchorMin = new Vector2(1f, 0f);
            cp.anchorMax = new Vector2(1f, 1f);
            cp.pivot = new Vector2(1f, 0.5f);
            cp.anchoredPosition = new Vector2(-pad, 0f);
            cp.sizeDelta = new Vector2(commandPanelWidth - pad, 0f);
            var cpBg = cmdPanel.AddComponent<Image>();
            cpBg.color = new Color(0.08f, 0.09f, 0.11f, 1f);
            cpBg.raycastTarget = false;

            var grid = new GameObject("CmdGrid");
            grid.transform.SetParent(cmdPanel.transform, false);
            var grt = grid.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(340f, 180f);
            grt.anchoredPosition = Vector2.zero;
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(78f, 54f);
            gl.spacing = new Vector2(5f, 5f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.MiddleCenter;
            _cmdGridParent = grid.transform;

            var center = new GameObject("SelectionBlock");
            center.transform.SetParent(bar.transform, false);
            var cr = center.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 0f);
            cr.anchorMax = new Vector2(1f, 1f);
            cr.offsetMin = new Vector2(minimapSlot + pad * 2f, pad);
            cr.offsetMax = new Vector2(-commandPanelWidth - pad, -pad);

            var portrait = new GameObject("Portrait");
            portrait.transform.SetParent(center.transform, false);
            var pr = portrait.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(1f, 0.5f);
            pr.anchorMax = new Vector2(1f, 0.5f);
            pr.pivot = new Vector2(1f, 0.5f);
            pr.anchoredPosition = new Vector2(-8f, 0f);
            pr.sizeDelta = new Vector2(112f, 168f);
            var pbg = portrait.AddComponent<Image>();
            pbg.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            pbg.raycastTarget = false;
            _portraitLabel = new GameObject("PortraitText").AddComponent<Text>();
            _portraitLabel.transform.SetParent(portrait.transform, false);
            _portraitLabel.font = _font;
            _portraitLabel.fontSize = 15;
            _portraitLabel.color = new Color(0.75f, 0.9f, 0.7f);
            _portraitLabel.alignment = TextAnchor.MiddleCenter;
            _portraitLabel.text = "\u2014";
            var pl = _portraitLabel.rectTransform;
            pl.anchorMin = new Vector2(0f, 0.3f);
            pl.anchorMax = Vector2.one;
            pl.offsetMin = new Vector2(6f, 0f);
            pl.offsetMax = new Vector2(-6f, -6f);

            _hpLabel = new GameObject("HpText").AddComponent<Text>();
            _hpLabel.transform.SetParent(portrait.transform, false);
            _hpLabel.font = _font;
            _hpLabel.fontSize = 12;
            _hpLabel.color = new Color(0.85f, 0.95f, 0.8f);
            _hpLabel.alignment = TextAnchor.MiddleCenter;
            _hpLabel.text = "";
            var hpRt = _hpLabel.rectTransform;
            hpRt.anchorMin = new Vector2(0f, 0.12f);
            hpRt.anchorMax = new Vector2(1f, 0.3f);
            hpRt.offsetMin = new Vector2(6f, 0f);
            hpRt.offsetMax = new Vector2(-6f, 0f);

            var hpBarBgGo = new GameObject("HpBarBg");
            hpBarBgGo.transform.SetParent(portrait.transform, false);
            var hbr = hpBarBgGo.AddComponent<RectTransform>();
            hbr.anchorMin = new Vector2(0f, 0f);
            hbr.anchorMax = new Vector2(1f, 0f);
            hbr.pivot = new Vector2(0f, 0f);
            hbr.anchoredPosition = new Vector2(6f, 6f);
            hbr.sizeDelta = new Vector2(-12f, 12f);
            _hpBarBg = hpBarBgGo.AddComponent<Image>();
            _hpBarBg.color = new Color(0.2f, 0.08f, 0.08f, 0.9f);
            _hpBarBg.raycastTarget = false;

            var hpFillGo = new GameObject("HpBarFill");
            hpFillGo.transform.SetParent(hpBarBgGo.transform, false);
            var hfr = hpFillGo.AddComponent<RectTransform>();
            hfr.anchorMin = Vector2.zero;
            hfr.anchorMax = Vector2.one;
            hfr.offsetMin = Vector2.zero;
            hfr.offsetMax = Vector2.zero;
            _hpBarFill = hpFillGo.AddComponent<Image>();
            _hpBarFill.color = new Color(0.2f, 0.85f, 0.3f, 1f);
            _hpBarFill.raycastTarget = false;

            var gridRoot = new GameObject("SelectionGrid");
            gridRoot.transform.SetParent(center.transform, false);
            var gr = gridRoot.AddComponent<RectTransform>();
            gr.anchorMin = new Vector2(0f, 0.5f);
            gr.anchorMax = new Vector2(1f, 0.5f);
            gr.pivot = new Vector2(0f, 0.5f);
            gr.anchoredPosition = new Vector2(12f, 0f);
            gr.sizeDelta = new Vector2(-132f, 168f);

            var gridLayout = gridRoot.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(52f, 52f);
            gridLayout.spacing = new Vector2(4f, 4f);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 8;

            _selectionCells = new Image[24];
            for (var i = 0; i < 24; i++)
            {
                var cell = new GameObject($"Sel_{i}");
                cell.transform.SetParent(gridRoot.transform, false);
                var img = cell.AddComponent<Image>();
                img.color = new Color(0.1f, 0.12f, 0.14f, 0.9f);
                img.raycastTarget = false;
                var tx = new GameObject("t").AddComponent<Text>();
                tx.transform.SetParent(cell.transform, false);
                tx.font = _font;
                tx.fontSize = 18;
                tx.color = Color.white;
                tx.alignment = TextAnchor.MiddleCenter;
                var trt = tx.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                _selectionCells[i] = img;
            }

            _pendingHint = new GameObject("PendingHint").AddComponent<Text>();
            _pendingHint.transform.SetParent(bar.transform, false);
            _pendingHint.font = _font;
            _pendingHint.fontSize = 14;
            _pendingHint.color = new Color(1f, 0.92f, 0.35f);
            _pendingHint.alignment = TextAnchor.MiddleLeft;
            var ph = _pendingHint.rectTransform;
            ph.anchorMin = new Vector2(0f, 1f);
            ph.anchorMax = new Vector2(0.5f, 1f);
            ph.pivot = new Vector2(0f, 1f);
            ph.anchoredPosition = new Vector2(minimapSlot + 16f, -4f);
            ph.sizeDelta = new Vector2(600f, 22f);
        }

        void AddCmdButton(Transform parent, string name, string key, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Cmd_{name}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.12f, 1f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.4f, 0.28f);
            colors.pressedColor = new Color(0.35f, 0.55f, 0.35f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var tx = new GameObject("Label").AddComponent<Text>();
            tx.transform.SetParent(go.transform, false);
            tx.font = _font;
            tx.fontSize = 15;
            tx.color = new Color(0.85f, 1f, 0.75f);
            tx.alignment = TextAnchor.MiddleCenter;
            tx.text = $"{name}\n<size=12><color=#88ff99>[{key}]</color></size>";
            tx.supportRichText = true;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _cmdButtonImages[name] = img;
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
            if (PlayerResources.Instance == null || !PlayerResources.Instance.TrySpend(50)) return;
            var hive = HiveDeposit.PlayerHive;
            if (hive == null) return;
            var center = new Vector3(hive.transform.position.x, 0f, hive.transform.position.z);
            var hiveExtent = hive.transform.localScale.x * 0.5f + 1.2f;
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angle) * hiveExtent, 0f, Mathf.Sin(angle) * hiveExtent);
            var spawnPos = center + offset;
            if (NavMesh.SamplePosition(spawnPos, out var hit, 4f, NavMesh.AllAreas))
                spawnPos = hit.position;
            var unit = SkirmishDirector.SpawnUnit(spawnPos, Team.Player, UnitArchetype.Worker);

            if (unit != null && hive.RallyGatherTarget != null && !hive.RallyGatherTarget.Depleted)
                unit.OrderGather(hive.RallyGatherTarget);
            else if (unit != null && hive.RallyPoint.HasValue)
                unit.OrderMove(hive.RallyPoint.Value);
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

        void ProduceFromBuilding(ProductionBuilding bld)
        {
            if (bld != null) bld.ProduceUnit();
        }

        static void ClearBuildingRally(ProductionBuilding bld)
        {
            if (bld != null) bld.ClearRally();
        }

        void ForceRebuild()
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
                case BuildingType.MantisBranch:
                    scale = new Vector3(3f, 3.5f, 3f);
                    col = new Color(0.45f, 0.7f, 0.3f, 0.4f);
                    break;
                case BuildingType.AntNest:
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    col = new Color(0.5f, 0.35f, 0.2f, 0.4f);
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
                var halfY = _ghostPreview.transform.localScale.y * 0.5f;
                _ghostPreview.transform.position = new Vector3(worldPos.x, halfY, worldPos.z);
            }
        }

        static readonly Color CmdDefault = new(0.12f, 0.18f, 0.12f, 1f);
        static readonly Color CmdActive = new(0.22f, 0.5f, 0.28f, 1f);

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
                    UnitOrder.Gather => "Gather",
                    UnitOrder.ReturnDeposit => "Gather",
                    UnitOrder.PickupSeed => "Gather",
                    UnitOrder.Patrol => "Patrol",
                    _ => null
                };
                if (key != null && _cmdButtonImages.TryGetValue(key, out var img))
                    img.color = CmdActive;
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
                PendingCommand.PlaceBuilding => $"Click ground to place {BuildingName(PendingBuildingType)} (Esc to cancel)",
                _ => ""
            };
        }

        void RefreshSelectionGrid()
        {
            if (_selectionCells == null) return;
            foreach (var c in _selectionCells)
            {
                c.color = new Color(0.1f, 0.12f, 0.14f, 0.9f);
                var t = c.GetComponentInChildren<Text>();
                if (t != null) t.text = "";
            }

            HideHpDisplay();

            if (SelectionController.Instance == null || _portraitLabel == null) return;

            if (SelectionController.Instance.SelectedResource != null)
            {
                var res = SelectionController.Instance.SelectedResource;
                _portraitLabel.text = res.Depleted ? "Rotting\nApple\n<size=12>(depleted)</size>" : "Rotting\nApple";
                _portraitLabel.supportRichText = true;

                var cell0 = _selectionCells[0];
                cell0.color = new Color(0.55f, 0.42f, 0.08f, 0.9f);
                var tx0 = cell0.GetComponentInChildren<Text>();
                if (tx0 != null)
                {
                    tx0.text = $"{res.ChargesRemaining:N0}\ncal";
                    tx0.fontSize = 12;
                    tx0.color = new Color(1f, 0.95f, 0.7f);
                }
                return;
            }

            if (SelectionController.Instance.SelectedSeed != null)
            {
                var seedNode = SelectionController.Instance.SelectedSeed;
                _portraitLabel.text = seedNode.PickedUp
                    ? "Cacti\nSeed\n<size=12>(picked up)</size>"
                    : "Cacti\nSeed";
                _portraitLabel.supportRichText = true;

                var cell0 = _selectionCells[0];
                cell0.color = new Color(0.3f, 0.5f, 0.18f, 0.9f);
                var tx0 = cell0.GetComponentInChildren<Text>();
                if (tx0 != null)
                {
                    tx0.text = "1\nseed";
                    tx0.fontSize = 12;
                    tx0.color = new Color(0.8f, 1f, 0.7f);
                }
                return;
            }

            if (SelectionController.Instance.SelectedHive != null)
            {
                _portraitLabel.text = "Ant\nNest";
                var cell0 = _selectionCells[0];
                cell0.color = new Color(0.2f, 0.35f, 0.6f, 0.9f);
                var tx0 = cell0.GetComponentInChildren<Text>();
                if (tx0 != null)
                {
                    tx0.text = "HQ";
                    tx0.color = new Color(0.6f, 0.8f, 1f);
                }
                return;
            }

            if (SelectionController.Instance.SelectedBuilding != null)
            {
                var bld = SelectionController.Instance.SelectedBuilding;
                _portraitLabel.text = bld.DisplayName;
                _portraitLabel.supportRichText = true;
                var cell0 = _selectionCells[0];
                cell0.color = bld.Type == BuildingType.MantisBranch
                    ? new Color(0.3f, 0.5f, 0.2f, 0.9f)
                    : new Color(0.4f, 0.28f, 0.15f, 0.9f);
                var tx0 = cell0.GetComponentInChildren<Text>();
                if (tx0 != null)
                {
                    tx0.text = bld.Type == BuildingType.MantisBranch ? "MB" : "AN";
                    tx0.color = new Color(0.8f, 1f, 0.7f);
                }
                return;
            }

            var list = new System.Collections.Generic.List<InsectUnit>();
            foreach (var u in SelectionController.Instance.SelectedPlayerUnits())
            {
                if (u != null && u.IsAlive) list.Add(u);
            }

            if (list.Count == 0)
            {
                _portraitLabel.text = "No selection";
                return;
            }

            _portraitLabel.text = list[0].Definition != null
                ? $"{list[0].Definition.displayName}\n({list.Count} units)"
                : $"{list.Count} units";

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
                var label = Abbrev(arch);
                var tx = img.GetComponentInChildren<Text>();
                if (tx != null)
                {
                    tx.text = cnt > 1 ? $"{label}\n\u00d7{cnt}" : label;
                    tx.fontSize = 18;
                    tx.color = Color.white;
                }
                img.color = TeamPalette.UnitBody(Team.Player, arch) * 0.45f + new Color(0.05f, 0.05f, 0.05f);
                idx++;
            }
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
                return Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.85f, 0.3f), t);
            }
            float t2 = frac * 2f;
            return Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.85f, 0.1f), t2);
        }

        static string Abbrev(UnitArchetype a)
        {
            return a switch
            {
                UnitArchetype.Worker => "W",
                UnitArchetype.BasicFighter => "F",
                UnitArchetype.BasicRanged => "R",
                _ => "?"
            };
        }

        static string BuildingName(BuildingType t)
        {
            return t switch
            {
                BuildingType.MantisBranch => "Manti's Branch",
                BuildingType.AntNest => "Ant's Nest",
                _ => t.ToString()
            };
        }
    }
}
