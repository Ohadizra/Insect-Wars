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

        HiveDeposit _selectedHive;
        RottingFruitNode _selectedResource;
        ProductionBuilding _selectedBuilding;

        float _lastClickTime;
        InsectUnit _lastClickedUnit;
        const float DoubleClickThreshold = 0.3f;

        public HiveDeposit SelectedHive => _selectedHive;
        public RottingFruitNode SelectedResource => _selectedResource;
        public ProductionBuilding SelectedBuilding => _selectedBuilding;

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
            var go = new GameObject("SelectionMarqueeCanvas");
            go.transform.SetParent(transform);
            _marqueeCanvas = go.AddComponent<Canvas>();
            _marqueeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _marqueeCanvas.sortingOrder = 40;
            _marqueeCanvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            _marqueeCanvasRt = go.GetComponent<RectTransform>();
            _marqueeCanvasRt.anchorMin = Vector2.zero;
            _marqueeCanvasRt.anchorMax = Vector2.one;
            _marqueeCanvasRt.offsetMin = Vector2.zero;
            _marqueeCanvasRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("MarqueeFill");
            fillGo.transform.SetParent(go.transform, false);
            _marqueeFill = fillGo.AddComponent<RectTransform>();
            _marqueeFill.anchorMin = _marqueeFill.anchorMax = new Vector2(0.5f, 0.5f);
            _marqueeFill.pivot = new Vector2(0.5f, 0.5f);
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
                if (BottomBar.SuppressSelectionDrag || overUi)
                    _boxActive = false;
                else
                {
                    _boxActive = true;
                    UpdateMarqueeVisual(_dragStart, _dragStart);
                    _marqueeFill.gameObject.SetActive(true);
                }
            }

            if (_boxActive && Mouse.current.leftButton.isPressed)
            {
                UpdateMarqueeVisual(_dragStart, Mouse.current.position.ReadValue());
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame && _boxActive)
            {
                _boxActive = false;
                _marqueeFill.gameObject.SetActive(false);
                var end = Mouse.current.position.ReadValue();
                var dist = Vector2.Distance(_dragStart, end);
                if (dist < 6f)
                    ClickSelect(_dragStart);
                else
                    BoxSelect(_dragStart, end);
            }
        }

        void UpdateMarqueeVisual(Vector2 screenA, Vector2 screenB)
        {
            if (_marqueeFill == null || _marqueeCanvasRt == null) return;
            var min = Vector2.Min(screenA, screenB);
            var max = Vector2.Max(screenA, screenB);
            var size = max - min;
            size.x = Mathf.Max(size.x, 2f);
            size.y = Mathf.Max(size.y, 2f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _marqueeCanvasRt, min, null, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _marqueeCanvasRt, max, null, out var localMax);
            var center = (localMin + localMax) * 0.5f;

            _marqueeFill.anchoredPosition = center;
            _marqueeFill.sizeDelta = new Vector2(
                Mathf.Abs(localMax.x - localMin.x),
                Mathf.Abs(localMax.y - localMin.y));
        }

        void ClearAll()
        {
            foreach (var s in _selected)
                s.IsSelected = false;
            _selected.Clear();
            _selectedHive = null;
            _selectedResource = null;
            _selectedBuilding = null;
        }

        void ClickSelect(Vector2 screen)
        {
            var ray = _cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 500f)) return;

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
                if (!shift) ClearAll();
                if (!_selected.Contains(u))
                {
                    _selected.Add(u);
                    u.IsSelected = true;
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
            if (building != null)
            {
                ClearAll();
                _selectedBuilding = building;
                return;
            }

            var fruit = hit.collider.GetComponentInParent<RottingFruitNode>();
            if (fruit != null)
            {
                ClearAll();
                _selectedResource = fruit;
                return;
            }
        }

        void BoxSelect(Vector2 a, Vector2 b)
        {
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);
            var rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            if (Keyboard.current == null || !Keyboard.current.leftShiftKey.isPressed)
                ClearAll();
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u.Team != Team.Player || !u.IsAlive) continue;
                var p = _cam.WorldToScreenPoint(u.transform.position);
                if (p.z < 0) continue;
                if (!rect.Contains(new Vector2(p.x, p.y))) continue;
                if (!_selected.Contains(u))
                {
                    _selected.Add(u);
                    u.IsSelected = true;
                }
            }

            if (_selected.Count > 0) return;

            if (HiveDeposit.PlayerHive != null)
            {
                var hp = _cam.WorldToScreenPoint(HiveDeposit.PlayerHive.transform.position);
                if (hp.z > 0 && rect.Contains(new Vector2(hp.x, hp.y)))
                {
                    _selectedHive = HiveDeposit.PlayerHive;
                    return;
                }
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null) continue;
                var bp = _cam.WorldToScreenPoint(bld.transform.position);
                if (bp.z > 0 && rect.Contains(new Vector2(bp.x, bp.y)))
                {
                    _selectedBuilding = bld;
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
                if (!_selected.Contains(u))
                {
                    _selected.Add(u);
                    u.IsSelected = true;
                }
            }
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
    }
}
