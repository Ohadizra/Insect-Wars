using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    public enum WarningType
    {
        BaseUnderAttack,
        BuildingUnderAttack,
        WorkerUnderAttack,
        UnitsUnderAttack,

        NotEnoughCalories,
        ColonyCapFull,
        QueueFull,
        IdleWorkers
    }

    public enum WarningSeverity { Red, Yellow }

    public class WarningSystem : MonoBehaviour
    {
        public static WarningSystem Instance { get; private set; }

        const float RedCooldown = 8f;
        const float YellowCooldown = 5f;
        const float RedExpiry = 4f;
        const float YellowExpiry = 3f;
        const float SpatialCellSize = 15f;
        const float IdleScanInterval = 3f;
        const float IdleThreshold = 5f;

        struct ActiveWarning
        {
            public WarningType Type;
            public WarningSeverity Severity;
            public Vector3 Position;
            public float TriggerTime;
            public float ExpiryTime;
            public RectTransform MinimapMarker;
            public int SpatialKey;
        }

        readonly List<ActiveWarning> _active = new();
        readonly Dictionary<long, float> _throttle = new();
        float _idleScanTimer;

        // Banner UI
        RectTransform _bannerRoot;
        Image _bannerBg;
        Text _bannerText;
        Text _bannerHint;
        CanvasGroup _bannerGroup;
        WarningType _displayedType;
        Vector3 _displayedPosition;
        bool _bannerVisible;
        float _pulsePhase;

        // Minimap marker parent
        RectTransform _minimapMarkerParent;

        static readonly Color RedBgColor = new(0.6f, 0.08f, 0.08f, 0.75f);
        static readonly Color YellowBgColor = new(0.7f, 0.55f, 0.05f, 0.75f);
        static readonly Color MarkerColor = new(1f, 0.15f, 0.15f, 0.9f);

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            BuildBannerUI();
        }

        void Update()
        {
            TickIdleWorkerScan();
            ExpireWarnings();
            UpdateBanner();
            UpdateMinimapMarkers();
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        public static void ReportWarning(WarningType type, Vector3 worldPos)
        {
            Instance?.HandleWarning(type, worldPos);
        }

        public static void ReportWarning(WarningType type)
        {
            Instance?.HandleWarning(type, Vector3.zero);
        }

        // ----------------------------------------------------------------
        // Warning intake & throttle
        // ----------------------------------------------------------------

        void HandleWarning(WarningType type, Vector3 pos)
        {
            var severity = GetSeverity(type);
            int spatialKey = severity == WarningSeverity.Red ? SpatialHash(pos) : 0;
            long throttleKey = ((long)type << 32) | (uint)spatialKey;

            if (_throttle.TryGetValue(throttleKey, out float lastTime))
            {
                float cooldown = severity == WarningSeverity.Red ? RedCooldown : YellowCooldown;
                if (Time.time - lastTime < cooldown)
                {
                    RefreshExisting(type, spatialKey, pos);
                    return;
                }
            }

            _throttle[throttleKey] = Time.time;

            float expiry = severity == WarningSeverity.Red ? RedExpiry : YellowExpiry;
            var warning = new ActiveWarning
            {
                Type = type,
                Severity = severity,
                Position = pos,
                TriggerTime = Time.time,
                ExpiryTime = Time.time + expiry,
                SpatialKey = spatialKey,
                MinimapMarker = severity == WarningSeverity.Red ? CreateMinimapMarker(pos) : null
            };
            _active.Add(warning);

            PlayWarningAudio(type);
        }

        void RefreshExisting(WarningType type, int spatialKey, Vector3 pos)
        {
            var severity = GetSeverity(type);
            float expiry = severity == WarningSeverity.Red ? RedExpiry : YellowExpiry;
            for (int i = 0; i < _active.Count; i++)
            {
                var w = _active[i];
                if (w.Type == type && w.SpatialKey == spatialKey)
                {
                    w.ExpiryTime = Time.time + expiry;
                    w.Position = pos;
                    _active[i] = w;
                    return;
                }
            }
        }

        // ----------------------------------------------------------------
        // Expiry
        // ----------------------------------------------------------------

        void ExpireWarnings()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _active[i].ExpiryTime)
                {
                    if (_active[i].MinimapMarker != null)
                        Destroy(_active[i].MinimapMarker.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }

        // ----------------------------------------------------------------
        // Idle worker scan
        // ----------------------------------------------------------------

        void TickIdleWorkerScan()
        {
            _idleScanTimer += Time.deltaTime;
            if (_idleScanTimer < IdleScanInterval) return;
            _idleScanTimer = 0f;

            InsectUnit firstIdle = null;
            int idleCount = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Team != Team.Player) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (u.CurrentOrder != UnitOrder.Idle) continue;
                if (Time.time - u.LastDamageTime < 2f) continue;
                if (firstIdle == null) firstIdle = u;
                idleCount++;
            }

            if (idleCount > 0 && firstIdle != null)
                HandleWarning(WarningType.IdleWorkers, firstIdle.transform.position);
        }

        // ----------------------------------------------------------------
        // Banner UI
        // ----------------------------------------------------------------

        void BuildBannerUI()
        {
            var hudRt = GameHUD.HudCanvasRect;
            if (hudRt == null) return;

            // Position above both BottomBar (263.5) and ControlGroupBar (263.5 + 44 = 307.5)
            var bannerGo = new GameObject("WarningBanner");
            bannerGo.transform.SetParent(hudRt, false);
            _bannerRoot = bannerGo.AddComponent<RectTransform>();
            _bannerRoot.anchorMin = new Vector2(0.25f, 0f);
            _bannerRoot.anchorMax = new Vector2(0.75f, 0f);
            _bannerRoot.pivot = new Vector2(0.5f, 0f);
            _bannerRoot.anchoredPosition = new Vector2(0f, 312f);
            _bannerRoot.sizeDelta = new Vector2(0f, 72f);

            _bannerBg = bannerGo.AddComponent<Image>();
            _bannerBg.color = RedBgColor;
            _bannerBg.raycastTarget = true;

            _bannerGroup = bannerGo.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;

            // Main warning text (upper portion)
            var textGo = new GameObject("WarningText");
            textGo.transform.SetParent(bannerGo.transform, false);
            _bannerText = textGo.AddComponent<Text>();
            _bannerText.font = UiFontHelper.GetFont();
            _bannerText.fontSize = 20;
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.color = Color.white;
            _bannerText.alignment = TextAnchor.MiddleCenter;
            _bannerText.raycastTarget = false;
            var trt = _bannerText.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.35f);
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 0f);
            trt.offsetMax = new Vector2(-12f, -4f);

            // Hint text (lower portion) — "Click to jump to location"
            var hintGo = new GameObject("WarningHint");
            hintGo.transform.SetParent(bannerGo.transform, false);
            _bannerHint = hintGo.AddComponent<Text>();
            _bannerHint.font = UiFontHelper.GetFont();
            _bannerHint.fontSize = 12;
            _bannerHint.fontStyle = FontStyle.Italic;
            _bannerHint.color = new Color(1f, 1f, 1f, 0.7f);
            _bannerHint.alignment = TextAnchor.MiddleCenter;
            _bannerHint.raycastTarget = false;
            var hrt = _bannerHint.rectTransform;
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = new Vector2(1f, 0.35f);
            hrt.offsetMin = new Vector2(12f, 4f);
            hrt.offsetMax = new Vector2(-12f, 0f);

            var clickHandler = bannerGo.AddComponent<WarningBannerClick>();
            clickHandler.Init(this);
        }

        void UpdateBanner()
        {
            ActiveWarning? best = GetHighestPriority();
            if (best == null)
            {
                if (_bannerVisible)
                {
                    _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, 0f, Time.deltaTime * 3f);
                    if (_bannerGroup.alpha <= 0.01f)
                    {
                        _bannerGroup.alpha = 0f;
                        _bannerGroup.blocksRaycasts = false;
                        _bannerVisible = false;
                    }
                }
                return;
            }

            var w = best.Value;
            _displayedType = w.Type;
            _displayedPosition = w.Position;
            _bannerText.text = GetWarningText(w.Type);

            if (w.Severity == WarningSeverity.Red)
            {
                _pulsePhase += Time.deltaTime * 4f;
                float pulse = Mathf.Lerp(0.6f, 0.95f, (Mathf.Sin(_pulsePhase) + 1f) * 0.5f);
                var c = RedBgColor;
                c.a = pulse;
                _bannerBg.color = c;
                _bannerGroup.blocksRaycasts = true;
                _bannerHint.text = "\u25B6  Click to jump to location";
            }
            else if (w.Type == WarningType.IdleWorkers)
            {
                _bannerBg.color = YellowBgColor;
                _bannerGroup.blocksRaycasts = true;
                _pulsePhase = 0f;
                _bannerHint.text = "\u25B6  Click to select idle workers";
            }
            else
            {
                _bannerBg.color = YellowBgColor;
                _bannerGroup.blocksRaycasts = false;
                _pulsePhase = 0f;
                _bannerHint.text = "";
            }

            _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, 1f, Time.deltaTime * 6f);
            _bannerVisible = true;
        }

        ActiveWarning? GetHighestPriority()
        {
            ActiveWarning? best = null;
            int bestPrio = int.MaxValue;
            foreach (var w in _active)
            {
                int prio = (int)w.Type;
                if (prio < bestPrio)
                {
                    bestPrio = prio;
                    best = w;
                }
            }
            return best;
        }

        internal void OnBannerClicked()
        {
            if (_displayedType == WarningType.IdleWorkers)
            {
                JumpToIdleWorkers();
                return;
            }

            if (GetSeverity(_displayedType) != WarningSeverity.Red) return;
            var cam = FindFirstObjectByType<RTSCameraController>();
            if (cam != null)
                cam.FocusWorldPosition(_displayedPosition);
        }

        void JumpToIdleWorkers()
        {
            var idleWorkers = new List<InsectUnit>();
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.Team != Team.Player) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (u.CurrentOrder != UnitOrder.Idle) continue;
                idleWorkers.Add(u);
            }

            if (idleWorkers.Count == 0) return;

            if (SelectionController.Instance != null)
                SelectionController.Instance.SetSelection(idleWorkers);

            Vector3 centroid = Vector3.zero;
            foreach (var w in idleWorkers)
                centroid += w.transform.position;
            centroid /= idleWorkers.Count;

            var cam = FindFirstObjectByType<RTSCameraController>();
            if (cam != null)
                cam.FocusWorldPosition(centroid);
        }

        // ----------------------------------------------------------------
        // Minimap markers
        // ----------------------------------------------------------------

        RectTransform FindMinimapMarkerParent()
        {
            if (_minimapMarkerParent != null) return _minimapMarkerParent;

            var rawImages = FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (var ri in rawImages)
            {
                if (ri.gameObject.name == "MinimapRT")
                {
                    _minimapMarkerParent = ri.rectTransform;
                    break;
                }
            }
            return _minimapMarkerParent;
        }

        RectTransform CreateMinimapMarker(Vector3 worldPos)
        {
            var parent = FindMinimapMarkerParent();
            if (parent == null || !SkirmishPlayArea.HasBounds) return null;

            float h = SkirmishPlayArea.HalfExtent;
            float u = (worldPos.x + h) / (2f * h);
            float v = (worldPos.z + h) / (2f * h);

            var go = new GameObject("WarningMarker");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(u, v);
            rt.anchorMax = new Vector2(u, v);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(12f, 12f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = MarkerColor;
            img.raycastTarget = false;

            return rt;
        }

        void UpdateMinimapMarkers()
        {
            foreach (var w in _active)
            {
                if (w.MinimapMarker == null) continue;
                float t = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(0.7f, 1.3f, t);
                w.MinimapMarker.localScale = Vector3.one * scale;

                var img = w.MinimapMarker.GetComponent<Image>();
                if (img != null)
                {
                    var c = MarkerColor;
                    c.a = Mathf.Lerp(0.5f, 1f, t);
                    img.color = c;
                }
            }
        }

        // ----------------------------------------------------------------
        // Audio
        // ----------------------------------------------------------------

        void PlayWarningAudio(WarningType type)
        {
            if (GameAudio.Instance == null) return;

            switch (GetSeverity(type))
            {
                case WarningSeverity.Red:
                    GameAudio.PlayUi(GameAudio.UiKind.Click);
                    break;
                case WarningSeverity.Yellow:
                    GameAudio.PlayUi(GameAudio.UiKind.Click);
                    break;
            }
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        static WarningSeverity GetSeverity(WarningType type)
        {
            return type switch
            {
                WarningType.BaseUnderAttack => WarningSeverity.Red,
                WarningType.BuildingUnderAttack => WarningSeverity.Red,
                WarningType.WorkerUnderAttack => WarningSeverity.Red,
                WarningType.UnitsUnderAttack => WarningSeverity.Red,
                _ => WarningSeverity.Yellow
            };
        }

        static string GetWarningText(WarningType type)
        {
            return type switch
            {
                WarningType.BaseUnderAttack => "BASE UNDER ATTACK!",
                WarningType.BuildingUnderAttack => "BUILDING UNDER ATTACK!",
                WarningType.WorkerUnderAttack => "WORKERS UNDER ATTACK!",
                WarningType.UnitsUnderAttack => "UNITS UNDER ATTACK!",
                WarningType.NotEnoughCalories => "NOT ENOUGH CALORIES",
                WarningType.ColonyCapFull => "COLONY CAPACITY FULL",
                WarningType.QueueFull => "PRODUCTION QUEUE FULL",
                WarningType.IdleWorkers => "IDLE WORKERS",
                _ => ""
            };
        }

        static int SpatialHash(Vector3 pos)
        {
            int x = Mathf.FloorToInt(pos.x / SpatialCellSize);
            int z = Mathf.FloorToInt(pos.z / SpatialCellSize);
            return x * 73856093 ^ z * 19349663;
        }
    }

    sealed class WarningBannerClick : MonoBehaviour, IPointerClickHandler
    {
        WarningSystem _system;

        public void Init(WarningSystem system) => _system = system;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _system?.OnBannerClicked();
        }
    }
}
