using UnityEngine;
using UnityEngine.InputSystem;

namespace InsectWars.RTS
{
    /// <summary>
    /// World-space billboard health bar rendered with simple quads.
    /// Visibility: selected units, Alt-held, recently damaged, or enemies below full HP.
    /// </summary>
    public class UnitHealthBar : MonoBehaviour
    {
        const float BarWidth = 0.8f;
        const float BarHeight = 0.08f;
        const float DamageShowDuration = 3f;

        InsectUnit _unit;
        Transform _barRoot;
        Transform _fillQuad;
        Material _fillMat;
        Material _bgMat;
        float _verticalOffset;

        static Mesh s_quadMesh;
        static Material s_bgTemplate;
        static Material s_fillTemplate;

        void Start()
        {
            _unit = GetComponent<InsectUnit>();
            if (_unit == null)
            {
                Destroy(this);
                return;
            }

            _verticalOffset = ComputeVerticalOffset();
            BuildBar();
        }

        float ComputeVerticalOffset()
        {
            var col = GetComponent<CapsuleCollider>();
            if (col != null)
                return col.center.y + col.height * 0.5f + 0.2f;
            var col2 = GetComponent<Collider>();
            if (col2 != null)
                return col2.bounds.max.y - transform.position.y + 0.2f;
            return 1.2f;
        }

        void BuildBar()
        {
            EnsureSharedAssets();

            _barRoot = new GameObject("HealthBar").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0f, _verticalOffset, 0f);

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(_barRoot, false);
            bgGo.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
            var bgMf = bgGo.AddComponent<MeshFilter>();
            bgMf.sharedMesh = s_quadMesh;
            var bgMr = bgGo.AddComponent<MeshRenderer>();
            _bgMat = new Material(s_bgTemplate);
            bgMr.sharedMaterial = _bgMat;
            bgMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgMr.receiveShadows = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_barRoot, false);
            fillGo.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            fillGo.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
            var fillMf = fillGo.AddComponent<MeshFilter>();
            fillMf.sharedMesh = s_quadMesh;
            var fillMr = fillGo.AddComponent<MeshRenderer>();
            _fillMat = new Material(s_fillTemplate);
            fillMr.sharedMaterial = _fillMat;
            fillMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fillMr.receiveShadows = false;
            _fillQuad = fillGo.transform;

            _barRoot.gameObject.SetActive(false);
        }

        static void EnsureSharedAssets()
        {
            if (s_quadMesh != null) return;

            s_quadMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            s_quadMesh.RecalculateNormals();

            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");

            s_bgTemplate = new Material(sh);
            var bgCol = new Color(0.1f, 0.1f, 0.1f, 0.75f);
            if (s_bgTemplate.HasProperty("_Color")) s_bgTemplate.color = bgCol;
            if (s_bgTemplate.HasProperty("_BaseColor")) s_bgTemplate.SetColor("_BaseColor", bgCol);

            s_fillTemplate = new Material(sh);
            var fillCol = new Color(0.2f, 1f, 0.3f, 0.9f);
            if (s_fillTemplate.HasProperty("_Color")) s_fillTemplate.color = fillCol;
            if (s_fillTemplate.HasProperty("_BaseColor")) s_fillTemplate.SetColor("_BaseColor", fillCol);
        }

        void LateUpdate()
        {
            if (_unit == null || _barRoot == null) return;

            bool visible = ShouldShow();
            if (_barRoot.gameObject.activeSelf != visible)
                _barRoot.gameObject.SetActive(visible);

            if (!visible) return;

            BillboardToCamera();
            UpdateFill();
        }

        bool ShouldShow()
        {
            if (!_unit.IsAlive) return false;

            float fraction = _unit.MaxHealth > 0f ? _unit.CurrentHealth / _unit.MaxHealth : 1f;

            if (_unit.IsSelected)
                return true;

            if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed)
                return true;

            if (_unit.LastDamageTime > 0f && Time.time - _unit.LastDamageTime < DamageShowDuration)
                return true;

            if (_unit.Team == Team.Enemy && fraction < 1f)
                return true;

            return false;
        }

        void BillboardToCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _barRoot.rotation = cam.transform.rotation;
        }

        void UpdateFill()
        {
            float fraction = _unit.MaxHealth > 0f
                ? Mathf.Clamp01(_unit.CurrentHealth / _unit.MaxHealth)
                : 1f;

            float offset = (1f - fraction) * BarWidth * -0.5f;
            _fillQuad.localScale = new Vector3(BarWidth * fraction, BarHeight, 1f);
            _fillQuad.localPosition = new Vector3(offset, 0f, -0.001f);

            var color = HealthColor(fraction);
            if (_fillMat.HasProperty("_Color")) _fillMat.color = color;
            if (_fillMat.HasProperty("_BaseColor")) _fillMat.SetColor("_BaseColor", color);
        }

        static Color HealthColor(float fraction)
        {
            if (fraction > 0.5f)
            {
                float t = (fraction - 0.5f) * 2f;
                return Color.Lerp(new Color(1f, 0.9f, 0.15f, 0.9f), new Color(0.2f, 1f, 0.3f, 0.9f), t);
            }
            else
            {
                float t = fraction * 2f;
                return Color.Lerp(new Color(0.95f, 0.15f, 0.1f, 0.9f), new Color(1f, 0.9f, 0.15f, 0.9f), t);
            }
        }
    }
}
