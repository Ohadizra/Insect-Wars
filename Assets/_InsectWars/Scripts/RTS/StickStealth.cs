using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Stick spy stealth: after standing still for 5 seconds the unit becomes
    /// invisible to enemies further than bombardier firing range (8 units).
    /// Moving or receiving damage breaks cloak.
    /// </summary>
    public class StickStealth : MonoBehaviour
    {
        const float CloakDelay = 5f;
        const float CloakedAlpha = 0.25f;

        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        InsectUnit _unit;
        Renderer[] _renderers;
        Color[] _originalColors;

        float _stillTimer;
        Vector3 _lastPos;
        float _lastDamageTime;
        bool _cloaked;

        void Awake()
        {
            _unit = GetComponent<InsectUnit>();
        }

        void Start()
        {
            _lastPos = transform.position;
            _lastDamageTime = _unit.LastDamageTime;
            _renderers = GetComponentsInChildren<Renderer>(true);
            CaptureColors();
        }

        void CaptureColors()
        {
            _originalColors = new Color[_renderers.Length];
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) { _originalColors[i] = Color.white; continue; }
                _renderers[i].GetPropertyBlock(block);
                var c = block.GetColor(BaseColorID);
                if (c.a < 0.01f)
                    c = _renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty("_BaseColor")
                        ? _renderers[i].sharedMaterial.GetColor("_BaseColor")
                        : Color.white;
                c.a = 1f;
                _originalColors[i] = c;
            }
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;

            bool tookDamage = _unit.LastDamageTime > _lastDamageTime;
            _lastDamageTime = _unit.LastDamageTime;

            bool moved = (transform.position - _lastPos).sqrMagnitude > 0.01f;
            _lastPos = transform.position;

            if (moved || tookDamage)
            {
                _stillTimer = 0f;
                if (_cloaked)
                    Decloak();
                return;
            }

            if (!_cloaked)
            {
                _stillTimer += Time.deltaTime;
                if (_stillTimer >= CloakDelay)
                    Cloak();
            }
        }

        void Cloak()
        {
            _cloaked = true;
            _unit.IsCloaked = true;
            ApplyAlpha(CloakedAlpha);
        }

        void Decloak()
        {
            _cloaked = false;
            _unit.IsCloaked = false;
            ApplyAlpha(1f);
        }

        void ApplyAlpha(float alpha)
        {
            if (_renderers == null || _originalColors == null) return;
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);

                var c = _originalColors[i];
                c.a = alpha;
                block.SetColor("_BaseColor", c);

                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    var c2 = _originalColors[i];
                    c2.a = alpha;
                    block.SetColor("_Color", c2);
                }

                r.SetPropertyBlock(block);
            }
        }

        void OnDestroy()
        {
            if (_unit != null)
                _unit.IsCloaked = false;
        }
    }
}
