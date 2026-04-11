using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Full-map orthographic minimap with fog-of-war overlay, viewport indicator,
    /// and click-to-pan (SC2-style left-click / drag on the minimap moves the camera).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SkirmishMinimap : MonoBehaviour
    {
        [SerializeField] float cameraHeight = 95f;
        [SerializeField] float orthographicSize = 42f;
        [SerializeField] Vector2 uiSize = new Vector2(200f, 200f);
        [SerializeField] int textureResolution = 256;

        Camera _miniCam;
        RenderTexture _rt;
        RawImage _raw;
        Material _fogMat;
        RectTransform _viewBox;
        RTSCameraController _camCtrl;

        static readonly Plane GroundPlane = new(Vector3.up, Vector3.zero);

        void Start()
        {
            StartCoroutine(CoInit());
        }

        IEnumerator CoInit()
        {
            if (Sc2BottomBar.MinimapHost == null)
                yield return null;
            BuildMinimapUi();
            BuildMinimapCamera();
            BuildViewportIndicator();
            BuildMinimapClickArea();
            _camCtrl = FindFirstObjectByType<RTSCameraController>();
        }

        void OnDestroy()
        {
            if (_rt != null)
            {
                _miniCam.targetTexture = null;
                _rt.Release();
                Destroy(_rt);
            }
            if (_fogMat != null)
                Destroy(_fogMat);
        }

        void LateUpdate()
        {
            UpdateViewportIndicator();
        }

        void BuildMinimapUi()
        {
            var hudRt = GameHUD.HudCanvasRect;
            if (hudRt == null)
            {
                Debug.LogWarning("SkirmishMinimap: GameHUD canvas missing; minimap UI skipped.");
                return;
            }

            var host = Sc2BottomBar.MinimapHost;
            var parent = host != null ? host : hudRt;

            var panel = new GameObject("MinimapPanel");
            panel.transform.SetParent(parent, false);
            var prt = panel.AddComponent<RectTransform>();
            if (host != null)
            {
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
                prt.anchoredPosition = Vector2.zero;
                prt.sizeDelta = Vector2.zero;
            }
            else
            {
                prt.anchorMin = new Vector2(1f, 0f);
                prt.anchorMax = new Vector2(1f, 0f);
                prt.pivot = new Vector2(1f, 0f);
                prt.anchoredPosition = new Vector2(-16f, 16f);
                prt.sizeDelta = uiSize + new Vector2(8f, 8f);
            }

            var border = panel.AddComponent<Image>();
            border.color = new Color(0.1f, 0.08f, 0.06f, 0.95f); // Natural Charcoal
            border.raycastTarget = false;

            var rawGo = new GameObject("MinimapRT");
            rawGo.transform.SetParent(panel.transform, false);
            _raw = rawGo.AddComponent<RawImage>();
            _raw.raycastTarget = false;
            _raw.color = Color.white;
            var rrt = _raw.rectTransform;
            if (host != null)
            {
                rrt.anchorMin = Vector2.zero;
                rrt.anchorMax = Vector2.one;
                rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.offsetMin = new Vector2(4f, 4f);
                rrt.offsetMax = new Vector2(-4f, -4f);
            }
            else
            {
                rrt.anchorMin = new Vector2(0.5f, 0.5f);
                rrt.anchorMax = new Vector2(0.5f, 0.5f);
                rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.sizeDelta = uiSize;
                rrt.anchoredPosition = Vector2.zero;
            }

            var lbl = new GameObject("MinimapLabel").AddComponent<Text>();
            lbl.transform.SetParent(panel.transform, false);
            lbl.font = InsectWars.Core.UiFontHelper.GetFont();
            lbl.fontSize = 13;
            lbl.color = new Color(0.85f, 0.9f, 0.75f);
            lbl.text = "MAP";
            lbl.alignment = TextAnchor.UpperCenter;
            lbl.raycastTarget = false;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 1f);
            lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, 4f);
            lrt.sizeDelta = new Vector2(120f, 22f);
        }

        void BuildMinimapCamera()
        {
            if (_raw == null) return;

            _rt = new RenderTexture(textureResolution, textureResolution, 16, RenderTextureFormat.ARGB32);
            _rt.Create();

            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform);
            _miniCam = camGo.AddComponent<Camera>();
            _miniCam.orthographic = true;

            float ortho = SkirmishPlayArea.HasBounds
                ? SkirmishPlayArea.HalfExtent
                : orthographicSize;
            _miniCam.orthographicSize = ortho;

            _miniCam.clearFlags = CameraClearFlags.SolidColor;
            _miniCam.backgroundColor = new Color(0.02f, 0.025f, 0.04f, 1f);
            _miniCam.cullingMask = ~(1 << 5);
            _miniCam.depth = -10f;
            _miniCam.targetTexture = _rt;
            _miniCam.nearClipPlane = 1f;
            _miniCam.farClipPlane = 200f;

            camGo.transform.position = new Vector3(0f, cameraHeight, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var fogShader = Shader.Find("InsectWars/MinimapFog");
            // #region agent log
            try { var lp = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), ".cursor", "debug-ad7c7c.log"); var j = "{\"sessionId\":\"ad7c7c\",\"location\":\"SkirmishMinimap.cs\",\"message\":\"MinimapFogShader\",\"data\":{\"shaderFound\":" + (fogShader != null ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ",\"hypothesisId\":\"D\"}"; System.IO.File.AppendAllText(lp, j + "\n"); Debug.Log("[DBG-ad7c7c] MinimapFogShader found=" + (fogShader != null)); } catch (System.Exception ex) { Debug.LogError("[DBG-ad7c7c] Log write failed: " + ex.Message); }
            // #endregion
            if (fogShader != null)
            {
                _fogMat = new Material(fogShader);
                _raw.material = _fogMat;
            }
            _raw.texture = _rt;
        }

        void BuildViewportIndicator()
        {
            if (_raw == null) return;

            var boxGo = new GameObject("ViewportBox");
            boxGo.transform.SetParent(_raw.transform, false);
            _viewBox = boxGo.AddComponent<RectTransform>();
            _viewBox.anchorMin = Vector2.zero;
            _viewBox.anchorMax = Vector2.one;
            _viewBox.offsetMin = Vector2.zero;
            _viewBox.offsetMax = Vector2.zero;

            const float thick = 1.5f;
            var c = new Color(1f, 1f, 1f, 0.85f);

            MakeBorderEdge("VB_Top", _viewBox,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -thick), Vector2.zero, c);
            MakeBorderEdge("VB_Bot", _viewBox,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, thick), c);
            MakeBorderEdge("VB_Left", _viewBox,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(thick, 0f), c);
            MakeBorderEdge("VB_Right", _viewBox,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-thick, 0f), Vector2.zero, c);
        }

        static void MakeBorderEdge(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        void BuildMinimapClickArea()
        {
            if (_raw == null) return;

            var clickGo = new GameObject("MinimapClickArea");
            clickGo.transform.SetParent(_raw.transform, false);
            var crt = clickGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            var img = clickGo.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;

            var receiver = clickGo.AddComponent<MinimapClickReceiver>();
            receiver.Init(_raw.rectTransform, HandleMinimapClick);
        }

        void HandleMinimapClick(Vector3 worldPos)
        {
            if (_camCtrl == null)
                _camCtrl = FindFirstObjectByType<RTSCameraController>();
            if (_camCtrl != null)
                _camCtrl.FocusWorldPosition(worldPos);
        }

        void UpdateViewportIndicator()
        {
            if (_viewBox == null) return;
            var mc = Camera.main;
            if (mc == null || !SkirmishPlayArea.HasBounds) return;

            float h = SkirmishPlayArea.HalfExtent;
            float inv = 1f / (2f * h);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                float vx = (i & 1) != 0 ? 1f : 0f;
                float vy = (i & 2) != 0 ? 1f : 0f;
                var ray = mc.ViewportPointToRay(new Vector3(vx, vy, 0f));

                Vector3 pt;
                if (GroundPlane.Raycast(ray, out float enter) && enter > 0f)
                    pt = ray.GetPoint(enter);
                else
                {
                    pt = ray.GetPoint(300f);
                    pt.y = 0f;
                }

                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.z < minZ) minZ = pt.z;
                if (pt.z > maxZ) maxZ = pt.z;
            }

            minX = Mathf.Clamp(minX, -h, h);
            maxX = Mathf.Clamp(maxX, -h, h);
            minZ = Mathf.Clamp(minZ, -h, h);
            maxZ = Mathf.Clamp(maxZ, -h, h);

            _viewBox.anchorMin = new Vector2((minX + h) * inv, (minZ + h) * inv);
            _viewBox.anchorMax = new Vector2((maxX + h) * inv, (maxZ + h) * inv);
            _viewBox.offsetMin = Vector2.zero;
            _viewBox.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Receives pointer-down and drag on the minimap overlay, converts the screen
    /// position to a world XZ coordinate, and forwards it to the minimap owner.
    /// </summary>
    sealed class MinimapClickReceiver : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        RectTransform _mapRect;
        System.Action<Vector3> _onClick;

        public void Init(RectTransform mapRect, System.Action<Vector3> onClick)
        {
            _mapRect = mapRect;
            _onClick = onClick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            HandlePointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            HandlePointer(eventData);
        }

        void HandlePointer(PointerEventData eventData)
        {
            if (_mapRect == null || _onClick == null || !SkirmishPlayArea.HasBounds) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapRect, eventData.position, eventData.pressEventCamera, out var local))
                return;

            var rect = _mapRect.rect;
            float u = (local.x - rect.x) / rect.width;
            float v = (local.y - rect.y) / rect.height;

            float h = SkirmishPlayArea.HalfExtent;
            float worldX = (u * 2f - 1f) * h;
            float worldZ = (v * 2f - 1f) * h;

            _onClick(new Vector3(worldX, 0f, worldZ));
        }
    }
}
