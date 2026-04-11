using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Per-unit AI for enemy team.
    /// Workers: round-trip gathering via <see cref="EnemyCommander.FindBestFruit"/>,
    ///          saturation tracking, flee toward nearby allies.
    /// Combat:  leash radius, assist nearby allies, focus-fire injured targets,
    ///          ranged kiting, low-HP retreat, re-aggro on damage.
    /// </summary>
    public class SimpleEnemyAi : MonoBehaviour, IAiController
    {
        [SerializeField] float thinkInterval = 1.2f;

        InsectUnit _self;
        float _timer;
        float _thinkSeconds;

        bool _aggro;
        Vector3 _engagementOrigin;
        bool _hasEngagementOrigin;
        float _lastKnownHp = float.MaxValue;

        RottingFruitNode _assignedFruit;

        const float LeashRadius = 40f;
        const float RetreatHpFraction = 0.2f;
        const float AssistRadius = 15f;

        void Awake()
        {
            _self = GetComponent<InsectUnit>();
            _thinkSeconds = thinkInterval * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
        }

        void OnDestroy()
        {
            ClearFruitAssignment();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (_self == null || !_self.IsAlive || _self.Team != Team.Enemy) return;

            if (!_aggro && _self.Archetype != UnitArchetype.Worker)
            {
                if (_self.CurrentHealth < _lastKnownHp - 0.01f)
                {\n                    _aggro = true;
                    _engagementOrigin = transform.position;
                    _hasEngagementOrigin = true;
                }
            }
            _lastKnownHp = _self.CurrentHealth;

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
        {\n            float vision = _self.Definition != null ? _self.Definition.visionRadius : 12f;

            InsectUnit threat = null;
            float threatDist = vision;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d >= threatDist) continue;
                float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                if (concealment > 0f && d > concealment) continue;\n                threatDist = d; threat = u;
            }

            if (threat != null)
            {
                ClearFruitAssignment();
                _self.OrderMove(FindFleeDestination());
                return;
            }

            if (_self.CurrentOrder is UnitOrder.Gather or UnitOrder.ReturnDeposit)
                return;

            if (_self.CurrentOrder == UnitOrder.Idle)
            {\n                ClearFruitAssignment();
                var fruit = EnemyCommander.FindBestFruit(transform.position);
                if (fruit != null)
                {
                    AssignFruit(fruit);
                    _self.OrderGather(fruit);
                }
            }
        }

        Vector3 FindFleeDestination()
        {
            InsectUnit nearestAlly = null;
            float bestDist = 25f;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy || u == _self) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestDist) { bestDist = d; nearestAlly = u; }
            }

            if (nearestAlly != null) return nearestAlly.transform.position;

            var hive = HiveDeposit.EnemyHive;
            return hive != null ? hive.DepositPoint : transform.position;
        }

        void AssignFruit(RottingFruitNode node)
        {
            ClearFruitAssignment();
            _assignedFruit = node;
            EnemyCommander.RegisterGatherAssignment(node);
        }

        void ClearFruitAssignment()
        {
            if (_assignedFruit == null) return;
            EnemyCommander.UnregisterGatherAssignment(_assignedFruit);
            _assignedFruit = null;
        }

        void TickCombat()
        {\n            float vision = _self.Definition != null ? _self.Definition.visionRadius : 12f;
            float hpFrac = _self.MaxHealth > 0.01f ? _self.CurrentHealth / _self.MaxHealth : 1f;
            bool isRanged = _self.Archetype == UnitArchetype.BasicRanged;

            if (hpFrac <= RetreatHpFraction)
            {
                _aggro = false;
                _hasEngagementOrigin = false;
                var hive = HiveDeposit.EnemyHive;
                if (hive != null) _self.OrderMove(hive.DepositPoint);
                return;
            }

            if (_hasEngagementOrigin && _aggro)
            {
                if (Vector3.Distance(transform.position, _engagementOrigin) > LeashRadius)
                {\n                    _aggro = false;
                    _hasEngagementOrigin = false;
                    _self.OrderMove(_engagementOrigin);
                    return;
                }
            }

            if (!_aggro)
            {
                foreach (var u in RtsSimRegistry.Units)
                {
                    if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                    float d = Vector3.Distance(transform.position, u.transform.position);
                    if (d > vision) continue;
                    float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                    if (concealment > 0f && d > concealment) continue;\n                    _aggro = true;
                    _engagementOrigin = transform.position;
                    _hasEngagementOrigin = true;
                    break;
                }
                if (!_aggro) return;
            }

            var assistTarget = FindAssistTarget(vision);
            if (assistTarget != null)
            {
                if (!(_self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget == assistTarget.transform))
                    _self.OrderAttack(assistTarget);
                return;
            }

            if (isRanged && _self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget != null)
            {
                float dist = Vector3.Distance(transform.position, _self.AttackTarget.position);
                float atkRange = _self.Definition.attackRange;
                if (dist < atkRange * 0.5f)
                {
                    var away = transform.position - _self.AttackTarget.position;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.01f)
                    {
                        _self.OrderMove(transform.position + away.normalized * (atkRange * 0.6f));
                        return;
                    }
                }
            }

            InsectUnit best = null;
            float bestScore = float.MaxValue;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d > vision * 2.5f) continue;
                float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                if (concealment > 0f && d > concealment) continue;

                float uHpFrac = u.MaxHealth > 0.01f ? u.CurrentHealth / u.MaxHealth : 1f;

                float workerBias = u.Archetype == UnitArchetype.Worker ? 0.75f : 1f;
                float injuredBias = 0.3f + 0.7f * uHpFrac;

                float focusBias = (u.LastDamageTime >= 0 && Time.time - u.LastDamageTime < 3f) ? 0.7f : 1f;

                float score = d * workerBias * injuredBias * focusBias;
                if (score < bestScore) { bestScore = score; best = u; }
            }

            if (best != null
                && !(_self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget == best.transform))
            {
                _self.OrderAttack(best);
            }
        }

        InsectUnit FindAssistTarget(float visionRange)
        {
            foreach (var ally in RtsSimRegistry.Units)
            {
                if (ally == null || !ally.IsAlive || ally.Team != Team.Enemy || ally == _self) continue;
                if (Vector3.Distance(transform.position, ally.transform.position) > AssistRadius) continue;
                if (ally.LastDamageTime < 0 || Time.time - ally.LastDamageTime > 2f) continue;

                InsectUnit attacker = null;
                float bestDist = visionRange;
                foreach (var u in RtsSimRegistry.Units)
                {
                    if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                    float d = Vector3.Distance(ally.transform.position, u.transform.position);
                    if (d < bestDist) { bestDist = d; attacker = u; }
                }
                if (attacker != null) return attacker;\n            }
            return null;
        }
    }
}
