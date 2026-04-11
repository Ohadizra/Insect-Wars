using UnityEngine;
using UnityEngine.InputSystem;

namespace InsectWars.RTS
{
    /// <summary>
    /// Edge pan, MMB drag orbit, scroll zoom — RTS-style on a tilted pivot.
    /// </summary>
    public class RTSCameraController : MonoBehaviour
    {
        [SerializeField] float panSpeed = 22f;
        [SerializeField] float zoomSpeed = 10f;
        [SerializeField] float minHeight = 9f;
        [SerializeField] float maxHeight = 42f;
        [SerializeField] float defaultOrbitDistance = 24f;
        [SerializeField] float edgePx = 14f;
        [SerializeField] float boundsMargin = 16f;

        Transform _pivot;
        float _yaw;
        float _orbitPitch = 48f;

        void Awake()
        {
            _pivot = new GameObject("RTSCameraPivot").transform;
            _pivot.position = new Vector3(0f, 0f, 0f);
            _pivot.rotation = Quaternion.Euler(_orbitPitch, 0f, 0f);
            _yaw = 0f;
            transform.SetParent(_pivot, true);
        }

        void Start()
        {
            var lp = transform.localPosition;
            if (lp.sqrMagnitude < 0.0001f)
                lp = new Vector3(0f, 1f, -1f);
                
            var d = Mathf.Clamp(defaultOrbitDistance, minHeight, maxHeight);
            transform.localPosition = lp.normalized * d;
        }

        void Update()
        {
            if (Mouse.current == null) return;
            var mp = Mouse.current.position.ReadValue();
            Vector2 pan = Vector2.zero;
            if (mp.x <= edgePx) pan.x -= 1f;
            else if (mp.x >= Screen.width - edgePx) pan.x += 1f;
            if (mp.y <= edgePx) pan.y -= 1f; // Fixed: move back when mouse is at bottom
            else if (mp.y >= Screen.height - edgePx) pan.y += 1f; // Fixed: move forward when mouse is at top

            if (pan.sqrMagnitude > 0.01f)
            {
                var right = _pivot.right;
                right.y = 0;
                right.Normalize();
                var fwd = Vector3.Cross(Vector3.up, right);
                _pivot.position += (right * pan.x + fwd * pan.y) * (panSpeed * Time.deltaTime);
            }

            if (Mouse.current.middleButton.isPressed)
            {
                var d = Mouse.current.delta.ReadValue();
                _yaw += d.x * 0.22f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - d.y * 0.12f, 22f, 78f);
                _pivot.rotation = Quaternion.Euler(_orbitPitch, _yaw, 0f);
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var local = transform.localPosition;
                var len = local.magnitude;
                len = Mathf.Clamp(len - scroll * zoomSpeed * 0.012f, minHeight, maxHeight);
                if (local.sqrMagnitude < 0.01f)
                    local = new Vector3(0f, 0.71f, -0.71f);
                transform.localPosition = local.normalized * len;
            }

            ClampPivotToPlayArea();
        }

        void ClampPivotToPlayArea()
        {
            if (!PlayArea.HasBounds) return;
            var h = PlayArea.HalfExtent;
            var m = Mathf.Max(boundsMargin, 6f);
            if (h <= m + 0.5f) return;
            var p = _pivot.position;
            p.x = Mathf.Clamp(p.x, -h + m, h - m);
            p.z = Mathf.Clamp(p.z, -h + m, h - m);
            p.y = 0f;
            _pivot.position = p;
        }

        /// <summary>Move pan pivot on XZ (e.g. start focused on player base). Call after play area is configured.</summary>
        public void FocusWorldPosition(Vector3 worldXZ)
        {
            if (_pivot == null) return;
            var p = worldXZ;
            p.y = 0f;
            _pivot.position = p;
            ClampPivotToPlayArea();
        }
    }
}
