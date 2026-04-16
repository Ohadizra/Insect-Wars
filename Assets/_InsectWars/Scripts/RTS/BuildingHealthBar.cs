using System.Linq;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// World-space billboard health bar for ProductionBuilding and HiveDeposit.
    /// Shows only when the structure is selected and not at full HP.
    /// Reuses the same quad-mesh approach as UnitHealthBar.
    /// </summary>
    public class BuildingHealthBar : MonoBehaviour
    {
        const float BarWidth = 1.6f;
        const float BarHeight = 0.12f;
        const float DamageShowDuration = 3f;

        ProductionBuilding _building;
        HiveDeposit _hive;

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
            _building = GetComponent<ProductionBuilding>();
            _hive = GetComponent<HiveDeposit>();
            if (_building == null && _hive == null)
            {
                Destroy(this);
                return;
            }
            _verticalOffset = ComputeVerticalOffset();
            BuildBar();
        }

        float ComputeVerticalOffset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                return col.bounds.max.y - transform.position.y + 0.3f;
            return 3f;
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
            if (_barRoot == null) return;

            bool visible = ShouldShow();
            if (_barRoot.gameObject.activeSelf != visible)
                _barRoot.gameObject.SetActive(visible);

            if (!visible) return;

            BillboardToCamera();
            UpdateFill();
        }

        bool ShouldShow()
        {
            float current, max;
            float lastDmgTime;

            if (_building != null)
            {
                if (!_building.IsAlive) return false;
                current = _building.CurrentHealth;
                max = _building.MaxHealth;
                lastDmgTime = _building.LastDamageTime;
            }
            else if (_hive != null)
            {
                if (!_hive.IsAlive) return false;
                current = _hive.CurrentHealth;
                max = _hive.MaxHealth;
                lastDmgTime = _hive.LastDamageTime;
            }
            else return false;

            float fraction = max > 0f ? current / max : 1f;
            bool damaged = fraction < 1f;

            if (IsThisSelected() && damaged)
                return true;

            if (lastDmgTime > 0f && Time.time - lastDmgTime < DamageShowDuration)
                return true;

            return false;
        }

        bool IsThisSelected()
        {
            var sc = SelectionController.Instance;
            if (sc == null) return false;

            if (_building != null)
                return sc.SelectedBuildings.Contains(_building);

            if (_hive != null)
                return sc.SelectedHive == _hive;

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
            float current, max;
            if (_building != null) { current = _building.CurrentHealth; max = _building.MaxHealth; }
            else if (_hive != null) { current = _hive.CurrentHealth; max = _hive.MaxHealth; }
            else return;

            float fraction = max > 0f ? Mathf.Clamp01(current / max) : 1f;

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
