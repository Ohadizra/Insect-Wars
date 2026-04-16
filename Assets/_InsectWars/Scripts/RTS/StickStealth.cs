using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Stick spy stealth:
    ///   Moving → Still (5s timer) → Cloaked.
    ///   Once cloaked, stays cloaked even while moving — only damage breaks it.
    ///   Invisible to enemies further than 8 units (bombardier range).
    ///   Visual: mostly opaque with strong desaturation + green tint + shimmer pulse.
    /// </summary>
    public class StickStealth : MonoBehaviour
    {
        public enum StickState { Moving, Still, Cloaked }

        const float CloakDelay = 5f;
        const float PanicFleeDistance = 10f;
        const float CloakedAlpha = 0.25f;
        const float ShimmerAlphaMin = 0.20f;
        const float ShimmerAlphaMax = 0.35f;
        const float ShimmerSpeed = 2.2f;
        const float CloakedDesaturation = 0.55f;
        static readonly Color CloakTint = new Color(0.50f, 0.65f, 0.42f, 1f);

        InsectUnit _unit;
        NavMeshAgent _agent;
        Renderer[] _renderers;
        Color[] _teamColors;
        Color[] _teamEmissions;

        StickState _state = StickState.Moving;
        float _stateTimer;
        float _shimmerPhase;
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
            _teamEmissions = new Color[_renderers.Length];
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                
                // Retrieve the block set by SkirmishDirector
                _renderers[i].GetPropertyBlock(block);
                
                // 1. Capture Base Color (from block or sharedMaterial)
                Color c = Color.white;
                c = block.GetColor("_BaseColor");
                if (c.a < 0.01f) c = block.GetColor("_Color");
                
                if (c.a < 0.01f)
                {
                    if (_renderers[i].sharedMaterial != null)
                    {
                        if (_renderers[i].sharedMaterial.HasProperty("_BaseColor"))
                            c = _renderers[i].sharedMaterial.GetColor("_BaseColor");
                        else if (_renderers[i].sharedMaterial.HasProperty("_Color"))
                            c = _renderers[i].sharedMaterial.GetColor("_Color");
                    }
                }
                c.a = 1f;
                _teamColors[i] = c;

                // 2. Capture Emission (from block or sharedMaterial)
                Color e = Color.black;
                e = block.GetColor("_EmissionColor");
                if (e.r < 0.001f && e.g < 0.001f && e.b < 0.001f)
                {
                    if (_renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty("_EmissionColor"))
                        e = _renderers[i].sharedMaterial.GetColor("_EmissionColor");
                }
                _teamEmissions[i] = e;
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
            ApplyAlpha(1f);
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
            _shimmerPhase = 0f;
            _unit.IsCloaked = true;
            ApplyShimmer();
        }

        void UpdateCloaked(bool moved, bool tookDamage)
        {
            if (tookDamage)
            {
                PanicFlee();
                return;
            }

            _shimmerPhase += Time.deltaTime * ShimmerSpeed;
            ApplyShimmer();
        }

        void ApplyShimmer()
        {
            float t = (Mathf.Sin(_shimmerPhase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(ShimmerAlphaMin, ShimmerAlphaMax, t);
            ApplyAlpha(alpha, desaturate: true);
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

        // ───────────── Renderer alpha (Moth-style property block only) ─────────────

        void ApplyAlpha(float alpha, bool desaturate = false)
        {
            if (_renderers == null || _teamColors == null) return;
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);

                var c = _teamColors[i];
                if (desaturate)
                {
                    float grey = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                    c = Color.Lerp(c, new Color(grey, grey, grey, 1f), CloakedDesaturation);
                    c = Color.Lerp(c, CloakTint, 0.35f);
                }
                c.a = alpha;
                block.SetColor("_BaseColor", c);

                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    var c2 = c;
                    block.SetColor("_Color", c2);
                }
                
                // Re-apply original emission from SkirmishDirector
                if (i < _teamEmissions.Length)
                {
                    Color e = _teamEmissions[i];
                    // Also fade emission with alpha for better cloak look
                    e *= alpha; 
                    block.SetColor("_EmissionColor", e);
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
