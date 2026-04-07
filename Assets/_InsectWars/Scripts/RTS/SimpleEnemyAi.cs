using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Idle until a player unit enters vision, then prefer softer targets (workers, injured) using the sim registry.
    /// </summary>
    public class SimpleEnemyAi : MonoBehaviour, IAiController
    {
        [SerializeField] float thinkInterval = 1.2f;
        InsectUnit _self;
        float _timer;
        float _thinkSeconds;
        bool _aggro;

        void Awake()
        {
            _self = GetComponent<InsectUnit>();
            _thinkSeconds = thinkInterval * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (_self == null || !_self.IsAlive || _self.Team != Team.Enemy) return;
            _timer -= deltaTime;
            if (_timer > 0) return;
            _timer = _thinkSeconds;
            var range = _self.Definition != null ? _self.Definition.visionRadius : 12f;
            if (!_aggro)
            {
                foreach (var u in RtsSimRegistry.Units)
                {
                    if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                    if (Vector3.Distance(transform.position, u.transform.position) <= range)
                    {
                        _aggro = true;
                        break;
                    }
                }
                return;
            }
            InsectUnit best = null;
            var bestScore = float.MaxValue;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                var d = Vector3.Distance(transform.position, u.transform.position);
                var hpFrac = u.MaxHealth > 0.01f ? u.CurrentHealth / u.MaxHealth : 1f;
                var workerBias = u.Archetype == UnitArchetype.Worker ? 0.88f : 1f;
                var injuredBias = 0.55f + 0.45f * hpFrac;
                var score = d * workerBias * injuredBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = u;
                }
            }
            if (best != null &&
                !(_self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget == best.transform))
                _self.OrderAttack(best);
        }
    }
}
