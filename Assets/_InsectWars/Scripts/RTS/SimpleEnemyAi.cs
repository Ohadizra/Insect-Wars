using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2/WC3-grade per-unit AI for enemy team.
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

        // Combat state
        bool _aggro;
        Vector3 _engagementOrigin;
        bool _hasEngagementOrigin;
        float _lastKnownHp = float.MaxValue;

        // Worker gather tracking
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

            // Re-aggro on damage: any HP loss while passive triggers aggro
            if (!_aggro && _self.Archetype != UnitArchetype.Worker)
            {
                if (_self.CurrentHealth < _lastKnownHp - 0.01f)
                {
                    _aggro = true;
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

            if (_self.Definition != null && !_self.Definition.canAttack)
                return;

            TickCombat();
        }

        // ──────────── Worker AI ────────────

        void TickWorker()
        {
            float vision = _self.Definition != null ? _self.Definition.visionRadius : 12f;

            // Threat scan — flee from non-worker player combat units
            InsectUnit threat = null;
            float threatDist = vision;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d >= threatDist) continue;
                float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                if (concealment > 0f && d > concealment) continue;
                threatDist = d; threat = u;
            }

            if (threat != null)
            {
                ClearFruitAssignment();
                _self.OrderMove(FindFleeDestination());
                return;
            }

            // Already busy gathering or returning — keep going
            if (_self.CurrentOrder is UnitOrder.Gather or UnitOrder.ReturnDeposit)
                return;

            // Idle — pick the best fruit using round-trip scoring
            if (_self.CurrentOrder == UnitOrder.Idle)
            {
                ClearFruitAssignment();
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
            // Prefer fleeing toward a nearby allied combat unit so the threat gets engaged
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

        // ──────────── Combat AI ────────────

        void TickCombat()
        {
            float vision = _self.Definition != null ? _self.Definition.visionRadius : 12f;
            float hpFrac = _self.MaxHealth > 0.01f ? _self.CurrentHealth / _self.MaxHealth : 1f;
            bool isRanged = _self.Archetype == UnitArchetype.BasicRanged;

            // --- Low HP retreat ---
            if (hpFrac <= RetreatHpFraction)
            {
                _aggro = false;
                _hasEngagementOrigin = false;
                var hive = HiveDeposit.EnemyHive;
                if (hive != null) _self.OrderMove(hive.DepositPoint);
                return;
            }

            // --- Leash: disengage if chased too far from engagement origin ---
            if (_hasEngagementOrigin && _aggro)
            {
                if (Vector3.Distance(transform.position, _engagementOrigin) > LeashRadius)
                {
                    _aggro = false;
                    _hasEngagementOrigin = false;
                    _self.OrderMove(_engagementOrigin);
                    return;
                }
            }

            // --- Aggro trigger ---
            if (!_aggro)
            {
                foreach (var u in RtsSimRegistry.Units)
                {
                    if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                    float d = Vector3.Distance(transform.position, u.transform.position);
                    if (d > vision) continue;
                    float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                    if (concealment > 0f && d > concealment) continue;
                    _aggro = true;
                    _engagementOrigin = transform.position;
                    _hasEngagementOrigin = true;
                    break;
                }
                if (!_aggro) return;
            }

            // --- Assist nearby ally under attack ---
            var assistTarget = FindAssistTarget(vision);
            if (assistTarget != null)
            {
                if (!(_self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget == assistTarget.transform))
                    _self.OrderAttack(assistTarget);
                return;
            }

            // --- Ranged kiting ---
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

            // --- Target selection with focus-fire scoring ---
            InsectUnit best = null;
            float bestScore = float.MaxValue;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || u.Team != Team.Player || !u.IsAlive) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d > vision * 2.5f) continue;
                if (u.IsCloaked && d > 8f) continue;
                float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                if (concealment > 0f && d > concealment) continue;

                float uHpFrac = u.MaxHealth > 0.01f ? u.CurrentHealth / u.MaxHealth : 1f;

                float workerBias = u.Archetype == UnitArchetype.Worker ? 0.75f : 1f;
                float injuredBias = 0.3f + 0.7f * uHpFrac;

                // Prefer targets already taking damage from allies (focus-fire)
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

        /// <summary>
        /// Scans nearby allies; if one was recently damaged, returns the closest
        /// player unit to that ally (the likely attacker).
        /// </summary>
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
                if (attacker != null) return attacker;
            }
            return null;
        }
    }
}
