using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Attached to an InsectUnit to make it an indestructible training target.
    /// The dummy stands still, never attacks back, and regenerates health when
    /// not hit for a few seconds.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TrainingDummy : MonoBehaviour
    {
        const float RegenDelay = 3f;
        const float RegenPerSecond = 50f;

        InsectUnit _unit;
        UnityEngine.AI.NavMeshAgent _agent;

        void Awake()
        {
            _unit = GetComponent<InsectUnit>();
            _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;

            if (_unit.CurrentOrder != UnitOrder.Idle)
                _unit.OrderStop();

            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            if (_unit.LastDamageTime < 0f) return;

            float elapsed = Time.time - _unit.LastDamageTime;
            if (elapsed >= RegenDelay && _unit.CurrentHealth < _unit.MaxHealth)
                _unit.Heal(RegenPerSecond * Time.deltaTime);
        }
    }
}
