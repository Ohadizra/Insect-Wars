using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Full moth behaviour state machine:
    ///   Flying (airborne, melee-immune) → idle 2s → Landing → Grounded → 5s still → Cloaked.
    ///   On damage while cloaked/grounded: panic flee in opposite direction, back to Flying.
    ///   Proximity reveal: enemy units within 8u see a semi-transparent silhouette.
    /// </summary>
    public class MothStealth : MonoBehaviour
    {
        public enum MothState { Flying, Landing, Grounded, Cloaked }

        const float LandDelay = 2f;
        const float CloakDelay = 5f;
        const float PanicFleeDistance = 12f;
        const float LandingDuration = 0.5f;
        const float FlyHeight = 1.2f;
        const float GroundClearance = 0.5f;
        const float ProximityRevealRange = 8f;
        const float CloakedAlpha = 0.25f;
        const float PartialAlpha = 0.45f;

        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        static readonly int ColorID = Shader.PropertyToID("_Color");

        InsectUnit _unit;
        NavMeshAgent _agent;
        Transform _visual;
        Renderer[] _renderers;
        Color[] _teamColors;

        MothState _state = MothState.Flying;
        float _stateTimer;
        float _lastDamageTime;
        Vector3 _lastPos;
        float _landingT;

        public MothState CurrentState => _state;

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
            _visual = transform.Find("Visual");

            CaptureTeamColors();
            EnterFlying();
        }

        void CaptureTeamColors()
        {
            _teamColors = new Color[_renderers.Length];
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) { _teamColors[i] = Color.white; continue; }
                _renderers[i].GetPropertyBlock(block);
                var c = block.GetColor(BaseColorID);
                if (c.a < 0.01f)
                    c = _renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty("_BaseColor")
                        ? _renderers[i].sharedMaterial.GetColor("_BaseColor")
                        : Color.white;
                c.a = 1f;
                _teamColors[i] = c;
            }
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;

            bool tookDamage = _unit.LastDamageTime > _lastDamageTime;
            _lastDamageTime = _unit.LastDamageTime;

            bool moved = _unit.CurrentOrder == UnitOrder.Move
                      && (transform.position - _lastPos).sqrMagnitude > 0.01f;
            _lastPos = transform.position;

            switch (_state)
            {
                case MothState.Flying:
                    UpdateFlying(moved, tookDamage);
                    break;
                case MothState.Landing:
                    UpdateLanding(moved, tookDamage);
                    break;
                case MothState.Grounded:
                    UpdateGrounded(moved, tookDamage);
                    break;
                case MothState.Cloaked:
                    UpdateCloaked(moved, tookDamage);
                    break;
            }

            UpdateVisualHeight();
        }

        // ───────────── State: Flying ─────────────
        void EnterFlying()
        {
            _state = MothState.Flying;
            _stateTimer = 0f;
            _unit.IsAirborne = true;
            _unit.IsCloaked = false;
            _landingT = 0f;
            ApplyAlpha(1f);

            var driver = GetComponent<UnitAnimationDriver>();
            if (driver != null) driver.NotifyTakeoff();
        }

        void UpdateFlying(bool moved, bool tookDamage)
        {
            if (moved)
            {
                _stateTimer = 0f;
                return;
            }

            _stateTimer += Time.deltaTime;
            if (_stateTimer >= LandDelay)
                EnterLanding();
        }

        // ───────────── State: Landing ─────────────
        void EnterLanding()
        {
            _state = MothState.Landing;
            _stateTimer = 0f;
            _landingT = 0f;
        }

        void UpdateLanding(bool moved, bool tookDamage)
        {
            if (moved || tookDamage)
            {
                EnterFlying();
                return;
            }

            _stateTimer += Time.deltaTime;
            _landingT = Mathf.Clamp01(_stateTimer / LandingDuration);

            if (_landingT >= 1f)
                EnterGrounded();
        }

        // ───────────── State: Grounded ─────────────
        void EnterGrounded()
        {
            _state = MothState.Grounded;
            _stateTimer = 0f;
            _unit.IsAirborne = false;
            _landingT = 1f;
        }

        void UpdateGrounded(bool moved, bool tookDamage)
        {
            if (moved)
            {
                EnterFlying();
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
            _state = MothState.Cloaked;
            _stateTimer = 0f;
            _unit.IsCloaked = true;
            ApplyAlpha(CloakedAlpha);
        }

        void UpdateCloaked(bool moved, bool tookDamage)
        {
            if (moved)
            {
                EnterFlying();
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
            EnterFlying();

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

        // ───────────── Visual height (flying vs grounded) ─────────────
        void UpdateVisualHeight()
        {
            if (_visual == null) return;

            float targetY;
            switch (_state)
            {
                case MothState.Flying:
                    targetY = FlyHeight + GroundClearance;
                    break;
                case MothState.Landing:
                    targetY = Mathf.Lerp(FlyHeight + GroundClearance, GroundClearance, _landingT);
                    break;
                default:
                    targetY = GroundClearance;
                    break;
            }

            var lp = _visual.localPosition;
            lp.y = Mathf.Lerp(lp.y, targetY, Time.deltaTime * 6f);
            _visual.localPosition = lp;
        }

        // ───────────── Renderer alpha ─────────────

        /// <summary>
        /// Called by FogOfWarSystem to set partial visibility when
        /// enemy units are within proximity of a cloaked moth.
        /// </summary>
        public void SetProximityReveal(bool revealed)
        {
            if (_state != MothState.Cloaked) return;
            ApplyAlpha(revealed ? PartialAlpha : CloakedAlpha);
        }

        void ApplyAlpha(float alpha)
        {
            if (_renderers == null || _teamColors == null) return;
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);

                var c = _teamColors[i];
                c.a = alpha;
                block.SetColor("_BaseColor", c);

                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    var c2 = _teamColors[i];
                    c2.a = alpha;
                    block.SetColor("_Color", c2);
                }

                r.SetPropertyBlock(block);
            }
        }

        void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.IsCloaked = false;
                _unit.IsAirborne = false;
            }
        }
    }

    static class Vector2Extensions
    {
        public static Vector3 ToXZ(this Vector2 v) => new Vector3(v.x, 0f, v.y);
    }
}
