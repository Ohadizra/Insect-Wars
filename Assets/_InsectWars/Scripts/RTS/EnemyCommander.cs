using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Strategic AI controller for the enemy team. Manages economy (worker assignments),
    /// unit production, and attack wave timing. Runs as a single MonoBehaviour on the Systems object.
    /// </summary>
    public class EnemyCommander : MonoBehaviour
    {
        float _tickTimer;
        float _matchTime;
        float _nextAttackTime;
        float _nextProduceTime;
        float _nextWorkerAssignTime;
        bool _firstWaveSent;

        float TickInterval => 1.5f * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

        const int MaxEnemyWorkers = 8;
        const int MaxEnemyCombat = 12;
        const float FirstAttackDelay = 70f;
        const float AttackInterval = 55f;
        const float ProduceInterval = 6f;

        void Start()
        {
            _nextAttackTime = FirstAttackDelay * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
            _nextProduceTime = 8f;
            _nextWorkerAssignTime = 2f;
        }

        void Update()
        {
            if (MatchDirector.MatchEnded || PauseController.IsPaused) return;

            _matchTime += Time.deltaTime;
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            AssignIdleWorkers();
            TryProduceUnits();
            TryLaunchAttack();
        }

        void AssignIdleWorkers()
        {
            if (_matchTime < _nextWorkerAssignTime) return;
            _nextWorkerAssignTime = _matchTime + 3f;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (u.CurrentOrder != UnitOrder.Idle) continue;

                var fruit = FindNearestFruit(u.transform.position);
                if (fruit != null)
                    u.OrderGather(fruit);
            }
        }

        void TryProduceUnits()
        {
            if (_matchTime < _nextProduceTime) return;
            _nextProduceTime = _matchTime + ProduceInterval * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

            int workers = 0, combat = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype == UnitArchetype.Worker) workers++;
                else combat++;
            }

            ProductionBuilding antNest = null;
            ProductionBuilding mantisBranch = null;
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != Team.Enemy) continue;
                if (b.Type == BuildingType.AntNest && antNest == null) antNest = b;
                if (b.Type == BuildingType.MantisBranch && mantisBranch == null) mantisBranch = b;
            }

            bool needWorkers = workers < Mathf.Min(MaxEnemyWorkers, 3 + (int)(_matchTime / 40f));
            bool needCombat = combat < MaxEnemyCombat;

            if (needWorkers && antNest != null)
            {
                SetNestRallyToFruit(antNest);
                var unit = antNest.ProduceUnit();
                if (unit != null)
                {
                    var fruit = FindNearestFruit(unit.transform.position);
                    if (fruit != null) unit.OrderGather(fruit);
                }
            }

            if (needCombat && mantisBranch != null && _matchTime > 25f)
                mantisBranch.ProduceUnit();

            if (needCombat && !needWorkers && antNest != null && mantisBranch == null && _matchTime > 35f)
            {
                if (EnemyResources.Calories >= 150)
                {
                    TryBuildMantisBranch();
                }
            }
        }

        void TryBuildMantisBranch()
        {
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.MantisBranch))) return;

            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;

            var offset = new Vector3(-6f, 0f, -10f);
            var pos = hive.transform.position + offset;
            pos.y = 0.02f;
            ProductionBuilding.Place(pos, BuildingType.MantisBranch, Team.Enemy);
        }

        void TryLaunchAttack()
        {
            if (_matchTime < _nextAttackTime) return;
            _nextAttackTime = _matchTime + AttackInterval * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

            var combatUnits = new List<InsectUnit>();
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                combatUnits.Add(u);
            }

            if (combatUnits.Count < 2 && _firstWaveSent) return;

            var target = FindAttackTarget();
            if (target == Vector3.zero) return;

            foreach (var u in combatUnits)
                u.OrderAttackMove(target);

            _firstWaveSent = true;
        }

        Vector3 FindAttackTarget()
        {
            InsectUnit nearest = null;
            float bestDist = float.MaxValue;
            var hive = HiveDeposit.EnemyHive;
            var from = hive != null ? hive.transform.position : Vector3.zero;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                var d = Vector3.Distance(from, u.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = u;
                }
            }

            if (nearest != null) return nearest.transform.position;

            if (HiveDeposit.PlayerHive != null)
                return HiveDeposit.PlayerHive.transform.position;

            return Vector3.zero;
        }

        void SetNestRallyToFruit(ProductionBuilding nest)
        {
            if (nest.RallyGatherTarget != null && !nest.RallyGatherTarget.Depleted) return;
            var fruit = FindNearestFruit(nest.transform.position);
            if (fruit != null)
                nest.SetRallyGather(fruit.transform.position, fruit);
        }

        static RottingFruitNode FindNearestFruit(Vector3 from)
        {
            RottingFruitNode best = null;
            float bestDist = float.MaxValue;
            foreach (var f in RtsSimRegistry.FruitNodes)
            {
                if (f == null || f.Depleted) continue;
                var d = Vector3.Distance(from, f.transform.position);
                if (d < bestDist) { bestDist = d; best = f; }
            }
            return best;
        }
    }
}
