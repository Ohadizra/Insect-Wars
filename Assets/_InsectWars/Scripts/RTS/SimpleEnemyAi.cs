using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Per-unit AI for enemy team. Workers auto-gather and flee threats.
    /// Combat units scan for targets with soft-target preference (workers, injured).
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

            if (_self.Archetype == UnitArchetype.Worker)
            {
                TickWorker();
                return;
            }

            TickCombat();
        }

        void TickWorker()
        {
            var range = _self.Definition != null ? _self.Definition.visionRadius : 12f;

            InsectUnit threat = null;
            float threatDist = range;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                var d = Vector3.Distance(transform.position, u.transform.position);
                if (d < threatDist)
                {
                    threatDist = d;
                    threat = u;
                }
            }

            if (threat != null)
            {
                var hive = HiveDeposit.EnemyHive;
                if (hive != null)
                    _self.OrderMove(hive.DepositPoint);
                return;
            }

            if (_self.CurrentOrder == UnitOrder.Gather || _self.CurrentOrder == UnitOrder.ReturnDeposit)
                return;

            if (_self.CurrentOrder == UnitOrder.Idle)
            {
                var fruit = FindNearestFruit();
                if (fruit != null)
                    _self.OrderGather(fruit);
            }
        }

        void TickCombat()
        {
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

        RottingFruitNode FindNearestFruit()
        {
            RottingFruitNode best = null;
            float bestDist = float.MaxValue;
            foreach (var f in RtsSimRegistry.FruitNodes)
            {
                if (f == null || f.Depleted) continue;
                var d = Vector3.Distance(transform.position, f.transform.position);
                if (d < bestDist) { bestDist = d; best = f; }
            }
            return best;
        }
    }
}
