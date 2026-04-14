using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public class SelectionController : MonoBehaviour
    {
        public static SelectionController Instance { get; private set; }

        readonly HashSet<InsectUnit> _selected = new();
        Camera _cam;
        Vector2 _dragStart;
        bool _boxActive;
        bool _pressedOnWorld;

        HiveDeposit _selectedHive;
        RottingFruitNode _selectedResource;
        readonly HashSet<ProductionBuilding> _selectedBuildings = new();

        // null = units subgroup active (or buildings-only with auto-set);
        // non-null = a specific building type subgroup is active for the command card.
        BuildingType? _activeBuildingType;

        float _lastClickTime;
        InsectUnit _lastClickedUnit;
        ProductionBuilding _lastClickedBuilding;
        float _lastBuildingClickTime;
        const float DoubleClickThreshold = 0.3f;

        public HiveDeposit SelectedHive => _selectedHive;
        public RottingFruitNode SelectedResource => _selectedResource;

        /// <summary>
        /// Primary selected building of the active building subgroup.
        /// Returns null when the units subgroup is active in a mixed selection.
        /// </summary>
        public ProductionBuilding SelectedBuilding
        {
            get
            {
                if (_selectedBuildings.Count == 0 || _activeBuildingType == null) return null;
                foreach (var b in _selectedBuildings)
                {
                    if (b == null || !b.IsAlive) continue;
                    if (b.Type == _activeBuildingType) return b;
                }
                return null;
            }
        }

        public IReadOnlyCollection<ProductionBuilding> SelectedBuildings => _selectedBuildings;

        public IEnumerable<ProductionBuilding> SelectedBuildingsOfActiveType
        {
            get
            {
                if (_activeBuildingType == null) yield break;
                foreach (var b in _selectedBuildings)
                    if (b != null && b.IsAlive && b.Type == _activeBuildingType)
                        yield return b;
            }
        }

        public BuildingType? ActiveBuildingType => _activeBuildingType;

        /// <summary>True when Tab has something to cycle through: mixed unit+building, or multiple building types.</summary>
        public bool HasMultipleSubgroups
        {
            get
            {
                bool hasUnits = false;
                foreach (var u in _selected)
                {
                    if (u != null && u.IsAlive && u.Team == Team.Player) { hasUnits = true; break; }
                }

                BuildingType? firstBldType = null;
                bool multipleTypes = false;
                foreach (var b in _selectedBuildings)
                {
                    if (b == null || !b.IsAlive) continue;
                    if (firstBldType == null) { firstBldType = b.Type; continue; }
                    if (b.Type != firstBldType) { multipleTypes = true; break; }
                }

                bool hasBuildings = firstBldType != null;
                return (hasUnits && hasBuildings) || multipleTypes;
            }
        }

        /// <summary>
        /// Cycle to the next subgroup (Tab key).
        /// Order: units (if any) -> each building type sorted -> wrap.
        /// </summary>
        public void CycleSubgroup()
        {
            bool hasUnits = false;
            foreach (var u in _selected)
            {
                if (u != null && u.IsAlive && u.Team == Team.Player) { hasUnits = true; break; }
            }

            var bldTypes = new List<BuildingType>();
            foreach (var b in _selectedBuildings)
            {
                if (b == null || !b.IsAlive) continue;
                if (!bldTypes.Contains(b.Type)) bldTypes.Add(b.Type);
            }
            bldTypes.Sort();

            int totalSubgroups = (hasUnits ? 1 : 0) + bldTypes.Count;
            if (totalSubgroups <= 1) return;

            int currentIdx;
            if (_activeBuildingType == null)
            {
                currentIdx = 0;
            }
            else
            {
                int bldIdx = bldTypes.IndexOf(_activeBuildingType.Value);
                currentIdx = (hasUnits ? 1 : 0) + (bldIdx >= 0 ? bldIdx : 0);
            }

            int nextIdx = (currentIdx + 1) % totalSubgroups;

            if (hasUnits && nextIdx == 0)
                _activeBuildingType = null;
            else
                _activeBuildingType = bldTypes[nextIdx - (hasUnits ? 1 : 0)];

            BottomBar.Instance?.ForceRebuild();
        }

        RectTransform _marqueeCanvasRt;
        RectTransform _marqueeFill;
        Canvas _marqueeCanvas;

        void Awake()
        {
            Instance = this;
            _cam = Camera.main;
            BuildMarqueeOverlay();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildMarqueeOverlay()
        {
            var go = new GameObject("MarqueeBox");
            go.transform.SetParent(transform);
            _marqueeCanvas = go.AddComponent<Canvas>();
            _marqueeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _marqueeCanvas.sortingOrder = 210;
            _marqueeCanvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            _marqueeCanvasRt = go.GetComponent<RectTransform>();

            var fillGo = new GameObject("MarqueeFill");
            fillGo.transform.SetParent(go.transform, false);
            _marqueeFill = fillGo.AddComponent<RectTransform>();
            _marqueeFill.anchorMin = Vector2.zero;
            _marqueeFill.anchorMax = Vector2.zero;
            _marqueeFill.pivot = Vector2.zero;
            var img = fillGo.AddComponent<Image>();
            img.color = new Color(0.25f, 0.85f, 0.35f, 0.22f);
            img.raycastTarget = false;
            var ol = fillGo.AddComponent<Outline>();
            ol.effectColor = new Color(0.35f, 1f, 0.45f, 0.95f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
            _marqueeFill.sizeDelta = Vector2.zero;
            _marqueeFill.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _dragStart = Mouse.current.position.ReadValue();
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                _pressedOnWorld = !overUi;

                if (!BottomBar.SuppressSelectionDrag && !overUi)
                {
                    _boxActive = true;
                    UpdateMarqueeVisual(_dragStart, _dragStart);
                    _marqueeFill.gameObject.SetActive(true);
                }
                else
                {
                    _boxActive = false;
                }
            }

            if (_boxActive && Mouse.current.leftButton.isPressed)
            {
                UpdateMarqueeVisual(_dragStart, Mouse.current.position.ReadValue());
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _marqueeFill.gameObject.SetActive(false);
                var end = Mouse.current.position.ReadValue();
                var dist = Vector2.Distance(_dragStart, end);

                if (_boxActive)
                {
                    _boxActive = false;
                    if (dist < 12f)
                        ClickSelect(_dragStart);
                    else
                        BoxSelect(_dragStart, end);
                }
                else if (_pressedOnWorld && dist < 12f && !BottomBar.SuppressSelectionDrag)
                {
                    ClickSelect(_dragStart);
                }

                _pressedOnWorld = false;
            }
        }

        void UpdateMarqueeVisual(Vector2 screenA, Vector2 screenB)
        {
            if (_marqueeFill == null) return;
            var min = Vector2.Min(screenA, screenB);
            var max = Vector2.Max(screenA, screenB);
            _marqueeFill.anchoredPosition = min;
            _marqueeFill.sizeDelta = new Vector2(
                Mathf.Max(max.x - min.x, 2f),
                Mathf.Max(max.y - min.y, 2f));
        }

        void ClearAll()
        {
            foreach (var s in _selected)
                s.IsSelected = false;
            _selected.Clear();
            _selectedHive = null;
            _selectedResource = null;
            _selectedBuildings.Clear();
            _activeBuildingType = null;
        }

        void ClickSelect(Vector2 screen)
        {
            var ray = _cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 1500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) return;

            var u = hit.collider.GetComponentInParent<InsectUnit>();
            if (u != null && u.Team == Team.Player && u.IsAlive)
            {
                float now = Time.unscaledTime;
                bool isDoubleClick = _lastClickedUnit != null
                    && _lastClickedUnit.Archetype == u.Archetype
                    && (now - _lastClickTime) <= DoubleClickThreshold;

                _lastClickTime = now;
                _lastClickedUnit = u;

                if (isDoubleClick)
                {
                    SelectAllOfTypeInView(u.Archetype);
                    return;
                }

                var shift = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
                if (!shift)
                {
                    ClearAll();
                    _selected.Add(u);
                    u.IsSelected = true;
                }
                else
                {
                    // Toggle: deselect if already selected, otherwise add
                    _selectedHive = null;
                    _selectedResource = null;
                    if (_selected.Contains(u))
                    {
                        _selected.Remove(u);
                        u.IsSelected = false;
                    }
                    else
                    {
                        _selected.Add(u);
                        u.IsSelected = true;
                    }
                    // Keep _selectedBuildings and _activeBuildingType unchanged (mixed OK)
                }
                return;
            }

            var hive = hit.collider.GetComponentInParent<HiveDeposit>();
            if (hive != null && hive == HiveDeposit.PlayerHive)
            {
                ClearAll();
                _selectedHive = hive;
                return;
            }

            var building = hit.collider.GetComponentInParent<ProductionBuilding>();
            if (building != null && building.Team == Team.Player)
            {
                float now = Time.unscaledTime;
                bool isBldDoubleClick = _lastClickedBuilding != null
                    && _lastClickedBuilding.Type == building.Type
                    && (now - _lastBuildingClickTime) <= DoubleClickThreshold;

                _lastBuildingClickTime = now;
                _lastClickedBuilding = building;

                if (isBldDoubleClick)
                {
                    SelectAllBuildingsOfTypeInView(building.Type);
                    return;
                }

                var shift = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
                if (!shift)
                {
                    ClearAll();
                    _selectedBuildings.Add(building);
                    _activeBuildingType = building.Type;
                }
                else
                {
                    // Toggle: deselect if already selected, otherwise add
                    _selectedHive = null;
                    _selectedResource = null;
                    if (_selectedBuildings.Contains(building))
                    {
                        _selectedBuildings.Remove(building);
                        ValidateActiveType();
                    }
                    else
                    {
                        _selectedBuildings.Add(building);
                        if (_activeBuildingType == null && _selected.Count == 0)
                            _activeBuildingType = building.Type;
                    }
                    // Keep _selected unchanged (mixed OK)
                }
                return;
            }

            var fruit = hit.collider.GetComponentInParent<RottingFruitNode>();
            if (fruit != null)
            {
                ClearAll();
                _selectedResource = fruit;
                return;
            }

            ClearAll();
        }

        void BoxSelect(Vector2 a, Vector2 b)
        {
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);
            var rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            if (Keyboard.current == null || !Keyboard.current.leftShiftKey.isPressed)
                ClearAll();

            var boxUnits = new List<InsectUnit>();
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u.Team != Team.Player || !u.IsAlive) continue;
                var p = _cam.WorldToScreenPoint(u.transform.position);
                if (p.z < 0) continue;
                if (rect.Contains(new Vector2(p.x, p.y)))
                    boxUnits.Add(u);
            }

            var boxBuildings = new List<ProductionBuilding>();
            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != Team.Player || !bld.IsAlive) continue;
                var bp = _cam.WorldToScreenPoint(bld.transform.position);
                if (bp.z > 0 && rect.Contains(new Vector2(bp.x, bp.y)))
                    boxBuildings.Add(bld);
            }

            // Units take priority in box-select (SC2 behavior).
            if (boxUnits.Count > 0)
            {
                foreach (var u in boxUnits)
                {
                    if (_selected.Add(u))
                        u.IsSelected = true;
                }
                return;
            }

            if (boxBuildings.Count > 0)
            {
                foreach (var bld in boxBuildings)
                    _selectedBuildings.Add(bld);
                if (_activeBuildingType == null && _selected.Count == 0)
                    AutoSetActiveBuildingType();
                return;
            }

            if (HiveDeposit.PlayerHive != null)
            {
                var hp = _cam.WorldToScreenPoint(HiveDeposit.PlayerHive.transform.position);
                if (hp.z > 0 && rect.Contains(new Vector2(hp.x, hp.y)))
                {
                    _selectedHive = HiveDeposit.PlayerHive;
                    return;
                }
            }

            foreach (var node in RtsSimRegistry.FruitNodes)
            {
                if (node.Depleted) continue;
                var np = _cam.WorldToScreenPoint(node.transform.position);
                if (np.z < 0) continue;
                if (rect.Contains(new Vector2(np.x, np.y)))
                {
                    _selectedResource = node;
                    return;
                }
            }
        }

        void SelectAllOfTypeInView(UnitArchetype archetype)
        {
            ClearAll();
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u.Team != Team.Player || !u.IsAlive) continue;
                if (u.Archetype != archetype) continue;
                var VP = _cam.WorldToViewportPoint(u.transform.position);
                if (VP.z <= 0 || VP.x < 0 || VP.x > 1 || VP.y < 0 || VP.y > 1) continue;
                if (_selected.Add(u))
                    u.IsSelected = true;
            }
        }

        void SelectAllBuildingsOfTypeInView(BuildingType type)
        {
            ClearAll();
            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != Team.Player || !bld.IsAlive) continue;
                if (bld.Type != type) continue;
                var vp = _cam.WorldToViewportPoint(bld.transform.position);
                if (vp.z <= 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1) continue;
                _selectedBuildings.Add(bld);
            }
            _activeBuildingType = type;
        }

        // ──────────── Public API ────────────

        /// <summary>Replace the entire selection with units + buildings + hive at once (control group recall).</summary>
        public void SetFullSelection(IEnumerable<InsectUnit> units, IEnumerable<ProductionBuilding> buildings, HiveDeposit hive)
        {
            ClearAll();

            if (units != null)
            {
                foreach (var u in units)
                {
                    if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                    if (_selected.Add(u)) u.IsSelected = true;
                }
            }

            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b == null || !b.IsAlive || b.Team != Team.Player) continue;
                    _selectedBuildings.Add(b);
                }
            }

            if (hive != null && hive.IsAlive && hive.Team == Team.Player)
                _selectedHive = hive;

            // Default to units subgroup if units exist; otherwise building subgroup
            if (_selected.Count > 0)
                _activeBuildingType = null;
            else
                AutoSetActiveBuildingType();
        }

        /// <summary>Merge units + buildings + hive into the current selection (Alt+Number).</summary>
        public void AddAllToSelection(IEnumerable<InsectUnit> units, IEnumerable<ProductionBuilding> buildings, HiveDeposit hive)
        {
            _selectedResource = null;

            if (units != null)
            {
                foreach (var u in units)
                {
                    if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                    if (_selected.Add(u)) u.IsSelected = true;
                }
            }

            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b == null || !b.IsAlive || b.Team != Team.Player) continue;
                    _selectedBuildings.Add(b);
                }
            }

            if (hive != null && hive.IsAlive && hive.Team == Team.Player)
                _selectedHive = hive;
        }

        /// <summary>Replace the current selection with the given units only.</summary>
        public void SetSelection(IEnumerable<InsectUnit> units)
        {
            ClearAll();
            foreach (var u in units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (_selected.Add(u))
                    u.IsSelected = true;
            }
        }

        /// <summary>Select a single production building.</summary>
        public void SelectBuilding(ProductionBuilding bld)
        {
            if (bld == null) return;
            ClearAll();
            _selectedBuildings.Add(bld);
            _activeBuildingType = bld.Type;
        }

        /// <summary>Select multiple production buildings with Tab-cycling support.</summary>
        public void SelectBuildings(IEnumerable<ProductionBuilding> buildings)
        {
            ClearAll();
            foreach (var b in buildings)
            {
                if (b == null || !b.IsAlive || b.Team != Team.Player) continue;
                _selectedBuildings.Add(b);
            }
            AutoSetActiveBuildingType();
        }

        /// <summary>Select the player hive.</summary>
        public void SelectHive(HiveDeposit hive)
        {
            if (hive == null) return;
            ClearAll();
            _selectedHive = hive;
        }

        public void Deselect(InsectUnit u)
        {
            if (_selected.Remove(u))
                u.IsSelected = false;
        }

        public IEnumerable<InsectUnit> SelectedPlayerUnits()
        {
            foreach (var u in _selected)
                if (u != null && u.IsAlive && u.Team == Team.Player)
                    yield return u;
        }

        public bool HasWorkerSelected()
        {
            foreach (var u in _selected)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Definition != null && u.Definition.canGather) return true;
            }
            return false;
        }

        void AutoSetActiveBuildingType()
        {
            _activeBuildingType = null;
            foreach (var b in _selectedBuildings)
            {
                if (b == null || !b.IsAlive) continue;
                _activeBuildingType = b.Type;
                break;
            }
        }

        /// <summary>Ensure _activeBuildingType still refers to a type present in the selection.</summary>
        void ValidateActiveType()
        {
            if (_activeBuildingType == null) return;
            foreach (var b in _selectedBuildings)
            {
                if (b != null && b.IsAlive && b.Type == _activeBuildingType) return;
            }
            AutoSetActiveBuildingType();
        }
    }
}
