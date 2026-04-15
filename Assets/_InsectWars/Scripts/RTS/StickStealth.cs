using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Stick spy stealth — mirrors MothStealth flow but without flying/landing:
    ///   Moving → Still (5s timer) → Cloaked.
    ///   On damage while cloaked: panic flee, back to Moving.
    ///   Invisible to enemies further than 8 units (bombardier range).
    /// </summary>
    public class StickStealth : MonoBehaviour
    {
        public enum StickState { Moving, Still, Cloaked }

        const float CloakDelay = 5f;
        const float PanicFleeDistance = 10f;
        const float CloakedAlpha = 0.5f;

        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        static readonly int ColorID = Shader.PropertyToID("_Color");
        static readonly int SurfaceID = Shader.PropertyToID("_Surface");
        static readonly int BlendID = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");

        InsectUnit _unit;
        NavMeshAgent _agent;
        Renderer[] _renderers;
        Color[] _teamColors;
        bool[] _wasOpaque;

        StickState _state = StickState.Moving;
        float _stateTimer;
        float _lastDamageTime;
        Vector3 _lastPos;

        public StickState CurrentState => _state;

        void Awake()
        {
            _unit = GetComponent<InsectUnit>();
            _agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            _lastPos = transform.position;
            _lastDamageTime = _unit.LastDamageTime;
            _renderers = GetComponentsInChildren<Renderer>(true);

            CaptureTeamColors();
        }

        void CaptureTeamColors()
        {
            _teamColors = new Color[_renderers.Length];
            _wasOpaque = new bool[_renderers.Length];
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) { _teamColors[i] = Color.white; _wasOpaque[i] = true; continue; }
                _renderers[i].GetPropertyBlock(block);
                var c = block.GetColor(BaseColorID);
                if (c.a < 0.01f)
                    c = _renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty(BaseColorID)
                        ? _renderers[i].sharedMaterial.GetColor(BaseColorID)
                        : Color.white;
                c.a = 1f;
                _teamColors[i] = c;
                _wasOpaque[i] = _renderers[i].sharedMaterial != null
                    && (!_renderers[i].sharedMaterial.HasProperty(SurfaceID)
                        || _renderers[i].sharedMaterial.GetFloat(SurfaceID) < 0.5f);
            }
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;

            bool tookDamage = _unit.LastDamageTime > _lastDamageTime;
            _lastDamageTime = _unit.LastDamageTime;

            bool moved = (transform.position - _lastPos).sqrMagnitude > 0.01f
                      || _unit.CurrentOrder == UnitOrder.Move
                      || _unit.CurrentOrder == UnitOrder.AttackMove;
            _lastPos = transform.position;

            switch (_state)
            {
                case StickState.Moving:
                    UpdateMoving(moved, tookDamage);
                    break;
                case StickState.Still:
                    UpdateStill(moved, tookDamage);
                    break;
                case StickState.Cloaked:
                    UpdateCloaked(moved, tookDamage);
                    break;
            }
        }

        // ───────────── State: Moving ─────────────
        void EnterMoving()
        {
            _state = StickState.Moving;
            _stateTimer = 0f;
            _unit.IsCloaked = false;
            ApplyAlpha(1f, transparent: false);
        }

        void UpdateMoving(bool moved, bool tookDamage)
        {
            if (moved)
            {
                _stateTimer = 0f;
                return;
            }

            _stateTimer += Time.deltaTime;
            if (_stateTimer >= 0.1f)
                EnterStill();
        }

        // ───────────── State: Still ─────────────
        void EnterStill()
        {
            _state = StickState.Still;
            _stateTimer = 0f;
        }

        void UpdateStill(bool moved, bool tookDamage)
        {
            if (moved)
            {
                EnterMoving();
                return;
            }
            if (tookDamage)
            {
                PanicFlee();
                return;
            }

            _stateTimer += Time.deltaTime;
            if (_stateTimer >= CloakDelay)
                EnterCloaked();
        }

        // ───────────── State: Cloaked ─────────────
        void EnterCloaked()
        {
            _state = StickState.Cloaked;
            _stateTimer = 0f;
            _unit.IsCloaked = true;
            ApplyAlpha(CloakedAlpha, transparent: true);
        }

        void UpdateCloaked(bool moved, bool tookDamage)
        {
            if (moved)
            {
                EnterMoving();
                return;
            }
            if (tookDamage)
            {
                PanicFlee();
                return;
            }
        }

        // ───────────── Panic flee ─────────────
        void PanicFlee()
        {
            _unit.IsCloaked = false;
            EnterMoving();

            var attacker = FindNearestEnemy();
            Vector3 fleeDir;
            if (attacker != null)
                fleeDir = (transform.position - attacker.transform.position).normalized;
            else
                fleeDir = -transform.forward;

            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.01f)
                fleeDir = Random.insideUnitCircle.normalized.ToXZ();

            var dest = transform.position + fleeDir * PanicFleeDistance;
            if (NavMesh.SamplePosition(dest, out var hit, PanicFleeDistance, NavMesh.AllAreas))
                dest = hit.position;

            _unit.OrderMove(dest);
        }

        InsectUnit FindNearestEnemy()
        {
            InsectUnit best = null;
            float bestD = float.MaxValue;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team == _unit.Team) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestD) { bestD = d; best = u; }
            }
            return best;
        }

        // ───────────── Renderer alpha + URP transparency ─────────────

        void ApplyAlpha(float alpha, bool transparent)
        {
            if (_renderers == null || _teamColors == null) return;
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                var mat = r.material;
                if (transparent && _wasOpaque[i])
                {
                    mat.SetFloat(SurfaceID, 1f);
                    mat.SetFloat(BlendID, 0f);
                    mat.SetFloat(SrcBlendID, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetFloat(DstBlendID, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat(ZWriteID, 0f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else if (!transparent && _wasOpaque[i])
                {
                    mat.SetFloat(SurfaceID, 0f);
                    mat.SetFloat(SrcBlendID, (float)UnityEngine.Rendering.BlendMode.One);
                    mat.SetFloat(DstBlendID, (float)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetFloat(ZWriteID, 1f);
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                }

                r.GetPropertyBlock(block);

                var c = _teamColors[i];
                c.a = alpha;
                block.SetColor(BaseColorID, c);

                if (mat.HasProperty(ColorID))
                {
                    var c2 = _teamColors[i];
                    c2.a = alpha;
                    block.SetColor(ColorID, c2);
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
