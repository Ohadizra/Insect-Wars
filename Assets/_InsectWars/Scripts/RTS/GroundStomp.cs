using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Giant Stag Beetle's ground stomp — AoE slow that triggers only while
    /// the beetle is in combat (attacking a unit, attacking a building, or
    /// attack-moving). Pulses every <see cref="StompInterval"/> seconds.
    /// </summary>
    public class GroundStomp : MonoBehaviour
    {
        const float StompRadius = 6f;
        const float StompInterval = 8f;
        const float SlowDuration = 3f;
        const float MoveSpeedMultiplier = 0.5f;
        const float AttackSpeedMultiplier = 0.6f;

        InsectUnit _unit;
        float _stompTimer;

        void Awake() => _unit = GetComponent<InsectUnit>();

        static bool IsInCombat(InsectUnit u)
        {
            var order = u.CurrentOrder;
            return order == UnitOrder.Attack
                || order == UnitOrder.AttackBuilding
                || order == UnitOrder.AttackMove;
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;
            if (!IsInCombat(_unit)) return;

            _stompTimer += Time.deltaTime;
            if (_stompTimer < StompInterval) return;
            _stompTimer = 0f;

            if (GetComponentInChildren<UnitAnimationDriver>() is { } driver)
                driver.NotifyStomp();

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team == _unit.Team) continue;
                if (Vector3.Distance(transform.position, u.transform.position) > StompRadius) continue;

                var debuff = u.GetComponent<StompSlow>();
                if (debuff == null) debuff = u.gameObject.AddComponent<StompSlow>();
                debuff.Apply(SlowDuration, MoveSpeedMultiplier, AttackSpeedMultiplier);
            }
        }
    }

    /// <summary>
    /// Debuff applied to enemies by <see cref="GroundStomp"/>.
    /// Reduces NavMeshAgent speed and stretches attack cooldown.
    /// Stacks refresh duration but do not multiply.
    /// </summary>
    public class StompSlow : MonoBehaviour
    {
        float _remaining;
        float _moveMultiplier = 1f;
        float _atkMultiplier = 1f;

        float _originalSpeed = -1f;
        UnityEngine.AI.NavMeshAgent _agent;
        InsectUnit _unit;

        public float AttackSpeedMultiplier => _remaining > 0f ? _atkMultiplier : 1f;

        public void Apply(float duration, float moveMult, float atkMult)
        {
            if (_agent == null) _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (_unit == null) _unit = GetComponent<InsectUnit>();

            if (_originalSpeed < 0f && _agent != null)
                _originalSpeed = _agent.speed;

            _remaining = duration;
            _moveMultiplier = moveMult;
            _atkMultiplier = atkMult;

            if (_agent != null && _originalSpeed >= 0f)
                _agent.speed = _originalSpeed * _moveMultiplier;
        }

        void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                if (_agent != null && _originalSpeed >= 0f)
                    _agent.speed = _originalSpeed;
                _originalSpeed = -1f;
            }
        }

        void OnDestroy()
        {
            if (_agent != null && _originalSpeed >= 0f)
                _agent.speed = _originalSpeed;
        }
    }
}
