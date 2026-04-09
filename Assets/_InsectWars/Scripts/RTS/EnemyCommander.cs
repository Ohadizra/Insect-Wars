using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2/WC3-grade strategic AI for the enemy team.
    /// Economy: round-trip resource scoring, worker saturation tracking.
    /// Construction: expansion nests, multiple MantisBranches with NavMesh-validated placement.
    /// Production: dynamic worker caps, mixed army composition from all buildings.
    /// Combat: army-threshold attacks, worker harass, defensive recall, all-in detection.
    /// </summary>
    public class EnemyCommander : MonoBehaviour
    {
        // ──────────── Worker-per-node saturation tracking ────────────

        static readonly Dictionary<int, int> s_workersByNode = new();
        const int MaxWorkersPerNode = 3;

        public static void RegisterGatherAssignment(RottingFruitNode node)
        {
            if (node == null) return;
            var id = node.GetInstanceID();
            s_workersByNode.TryGetValue(id, out var count);
            s_workersByNode[id] = count + 1;
        }

        public static void UnregisterGatherAssignment(RottingFruitNode node)
        {
            if (node == null) return;
            var id = node.GetInstanceID();
            if (!s_workersByNode.TryGetValue(id, out var count)) return;
            if (--count <= 0) s_workersByNode.Remove(id);
            else s_workersByNode[id] = count;
        }

        public static int GetAssignedWorkers(RottingFruitNode node)
        {
            if (node == null) return 0;
            return s_workersByNode.TryGetValue(node.GetInstanceID(), out var c) ? c : 0;
        }

        public static void ResetTracking() => s_workersByNode.Clear();

        // ──────────── Round-trip resource scoring ────────────

        /// <summary>
        /// Picks the best fruit for an enemy worker using round-trip distance
        /// (worker→fruit + fruit→nearest deposit) with a saturation penalty so
        /// workers spread across nodes instead of all piling onto one.
        /// </summary>
        public static RottingFruitNode FindBestFruit(Vector3 workerPos, RottingFruitNode exclude = null)
        {
            RottingFruitNode best = null;
            float bestScore = float.MaxValue;

            foreach (var f in RtsSimRegistry.FruitNodes)
            {
                if (f == null || f.Depleted || f == exclude) continue;
                if (f.ChargesRemaining < 20) continue;

                float toFruit = Vector3.Distance(workerPos, f.transform.position);
                float fruitToDeposit = DistToNearestEnemyDeposit(f.transform.position);
                float roundTrip = toFruit + fruitToDeposit;

                int assigned = GetAssignedWorkers(f);
                float saturationPenalty = assigned >= MaxWorkersPerNode ? 200f : assigned * 15f;

                float score = roundTrip + saturationPenalty;
                if (score < bestScore) { bestScore = score; best = f; }
            }

            return best;
        }

        /// <summary>Distance from a position to the nearest enemy deposit (hive or AntNest).</summary>
        public static float DistToNearestEnemyDeposit(Vector3 pos)
        {
            float best = float.MaxValue;
            var hive = HiveDeposit.EnemyHive;
            if (hive != null)
                best = Vector3.Distance(pos, hive.transform.position);

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != Team.Enemy || bld.Type != BuildingType.AntNest) continue;
                float d = Vector3.Distance(pos, bld.transform.position);
                if (d < best) best = d;
            }
            return best;
        }

        // ──────────── Strategic state ────────────

        float _matchTime;
        float _tickTimer;
        float _nextProduceTime;
        float _nextBuildTime;
        float _nextWorkerAssignTime;
        float _nextHarassTime;
        float _nextMainAttackTime;
        bool _firstWaveSent;
        int _rangedToggle;

        float TickInterval => 1.5f * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

        const float ProduceBaseInterval = 4f;
        const float BuildCheckInterval = 10f;
        const float HarassInterval = 45f;
        const float AttackCooldown = 50f;

        int DesiredWorkers => Mathf.Clamp(3 + (int)(_matchTime / 35f), 3, 10);
        int MaxCombat => Mathf.Clamp(4 + (int)(_matchTime / 25f), 4, 20);
        int MaxMantisBranches => _matchTime > 180f ? 2 : 1;

        void Start()
        {
            ResetTracking();
            float diffMul = GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
            _nextProduceTime = 6f;
            _nextBuildTime = 25f * diffMul;
            _nextWorkerAssignTime = 2f;
            _nextHarassTime = 80f * diffMul;
            _nextMainAttackTime = 55f * diffMul;
        }

        void Update()
        {
            if (MatchDirector.MatchEnded || PauseController.IsPaused) return;

            _matchTime += Time.deltaTime;
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            AssignIdleWorkers();
            TryBuild();
            TryProduceUnits();
            TryAttackOrDefend();
        }

        // ──────────── Economy: worker assignment ────────────

        void AssignIdleWorkers()
        {
            if (_matchTime < _nextWorkerAssignTime) return;
            _nextWorkerAssignTime = _matchTime + 2.5f;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (u.CurrentOrder != UnitOrder.Idle) continue;

                var fruit = FindBestFruit(u.transform.position);
                if (fruit != null)
                    u.OrderGather(fruit);
            }
        }

        // ──────────── Building construction ────────────

        void TryBuild()
        {
            if (_matchTime < _nextBuildTime) return;
            _nextBuildTime = _matchTime + BuildCheckInterval;

            int mantisBranches = 0, antNests = 0;
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != Team.Enemy) continue;
                if (b.Type == BuildingType.MantisBranch) mantisBranches++;
                if (b.Type == BuildingType.AntNest) antNests++;
            }

            if (mantisBranches == 0 && _matchTime > 25f)
            {
                TryPlaceMantisBranch();
                return;
            }

            int nestCost = ProductionBuilding.GetBuildCost(BuildingType.AntNest);
            if (antNests < 2 && _matchTime > 90f && EnemyResources.Calories >= nestCost + 100)
            {
                if (TryPlaceExpansionNest()) return;
            }

            int branchCost = ProductionBuilding.GetBuildCost(BuildingType.MantisBranch);
            if (mantisBranches < MaxMantisBranches && _matchTime > 150f
                && EnemyResources.Calories >= branchCost + 100)
            {
                TryPlaceMantisBranch();
            }
        }

        void TryPlaceMantisBranch()
        {
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.MantisBranch))) return;
            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;
            ProductionBuilding.Place(FindBuildPosition(hive.transform.position, 8f),
                BuildingType.MantisBranch, Team.Enemy);
        }

        bool TryPlaceExpansionNest()
        {
            RottingFruitNode bestNode = null;
            float bestValue = 0f;

            foreach (var f in RtsSimRegistry.FruitNodes)
            {
                if (f == null || f.Depleted || f.ChargesRemaining < 2000) continue;
                float distToDeposit = DistToNearestEnemyDeposit(f.transform.position);
                if (distToDeposit < 25f) continue;
                float value = f.ChargesRemaining * Mathf.Clamp01(distToDeposit / 60f);
                if (value > bestValue) { bestValue = value; bestNode = f; }
            }

            if (bestNode == null) return false;
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.AntNest))) return false;
            ProductionBuilding.Place(FindBuildPosition(bestNode.transform.position, 6f),
                BuildingType.AntNest, Team.Enemy);
            return true;
        }

        Vector3 FindBuildPosition(Vector3 near, float radius)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                var candidate = near + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                candidate.y = 0.02f;
                if (NavMesh.SamplePosition(candidate, out var hit, 5f, NavMesh.AllAreas))
                    return new Vector3(hit.position.x, 0.02f, hit.position.z);
            }
            return new Vector3(near.x - radius, 0.02f, near.z - radius);
        }

        // ──────────── Unit production ────────────

        void TryProduceUnits()
        {
            if (_matchTime < _nextProduceTime) return;

            float cadence = ProduceBaseInterval;
            if (EnemyResources.Calories > 500) cadence *= 0.6f;
            else if (EnemyResources.Calories < 100) cadence *= 1.5f;
            _nextProduceTime = _matchTime + cadence * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

            int workers = 0, fighters = 0, ranged = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                switch (u.Archetype)
                {
                    case UnitArchetype.Worker: workers++; break;
                    case UnitArchetype.BasicFighter: fighters++; break;
                    case UnitArchetype.BasicRanged: ranged++; break;
                }
            }
            int combat = fighters + ranged;

            var nests = new List<ProductionBuilding>(4);
            var branches = new List<ProductionBuilding>(4);
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != Team.Enemy) continue;
                if (b.Type == BuildingType.AntNest) nests.Add(b);
                else if (b.Type == BuildingType.MantisBranch) branches.Add(b);
            }

            // --- Workers from all AntNests ---
            if (workers < DesiredWorkers)
            {
                foreach (var nest in nests)
                {
                    if (workers >= DesiredWorkers) break;
                    SetNestRallyToFruit(nest);
                    var unit = nest.ProduceUnit();
                    if (unit == null) continue;
                    workers++;
                    var fruit = FindBestFruit(unit.transform.position);
                    if (fruit != null) unit.OrderGather(fruit);
                }
            }

            // --- Combat units from all MantisBranches (mixed fighter / ranged) ---
            if (combat < MaxCombat && _matchTime > 20f)
            {
                foreach (var branch in branches)
                {
                    if (combat >= MaxCombat) break;

                    bool produceRanged = (_rangedToggle % 5) >= 3; // ~40 % ranged
                    _rangedToggle++;

                    if (produceRanged)
                    {
                        int unitCost = branch.UnitCost;
                        if (!EnemyResources.TrySpend(unitCost)) continue;
                        var spawnPos = SpawnPosNear(branch.transform);
                        var unit = SkirmishDirector.SpawnUnit(spawnPos, Team.Enemy, UnitArchetype.BasicRanged);
                        if (unit != null) combat++;
                    }
                    else
                    {
                        if (branch.ProduceUnit() != null) combat++;
                    }
                }
            }
        }

        static Vector3 SpawnPosNear(Transform building)
        {
            var center = new Vector3(building.position.x, 0f, building.position.z);
            float extent = building.localScale.x * 0.5f + 1.5f;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var pos = center + new Vector3(Mathf.Cos(angle) * extent, 0f, Mathf.Sin(angle) * extent);
            if (NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas))
                pos = hit.position;
            return pos;
        }

        void SetNestRallyToFruit(ProductionBuilding nest)
        {
            if (nest.RallyGatherTarget != null && !nest.RallyGatherTarget.Depleted) return;
            var fruit = FindBestFruit(nest.transform.position);
            if (fruit != null)
                nest.SetRallyGather(fruit.transform.position, fruit);
        }

        // ──────────── Attack & Defense ────────────

        void TryAttackOrDefend()
        {
            if (TryDefendBase()) return;
            TryHarass();
            TryMainAttack();
        }

        bool TryDefendBase()
        {
            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return false;

            bool threatened = false;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (Vector3.Distance(hive.transform.position, u.transform.position) < 25f)
                { threatened = true; break; }
            }
            if (!threatened) return false;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                if (Vector3.Distance(hive.transform.position, u.transform.position) > 30f)
                    u.OrderAttackMove(hive.transform.position);
            }
            return true;
        }

        void TryHarass()
        {
            if (_matchTime < _nextHarassTime) return;
            _nextHarassTime = _matchTime + HarassInterval * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

            Vector3? workerPos = null;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (u.Archetype == UnitArchetype.Worker) { workerPos = u.transform.position; break; }
            }
            if (workerPos == null) return;

            int sent = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (sent >= 3) break;
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                if (u.CurrentOrder is UnitOrder.Idle or UnitOrder.Move)
                {
                    u.OrderAttackMove(workerPos.Value);
                    sent++;
                }
            }
        }

        void TryMainAttack()
        {
            if (_matchTime < _nextMainAttackTime) return;

            var army = new List<InsectUnit>(16);
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype != UnitArchetype.Worker) army.Add(u);
            }

            int threshold = _matchTime < 60f ? 3 : (_matchTime < 120f ? 5 : 7);
            bool allIn = EnemyResources.Calories < 30 && !HasActiveFruit() && army.Count > 0;

            if (!allIn)
            {
                if (army.Count < threshold) return;
                if (!_firstWaveSent && _matchTime < 55f * GameSession.DifficultyEnemyAiThinkIntervalMultiplier) return;
            }

            var target = PickAttackTarget();
            if (target == Vector3.zero) return;

            foreach (var u in army)
                u.OrderAttackMove(target);

            _firstWaveSent = true;
            _nextMainAttackTime = _matchTime + AttackCooldown * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
        }

        Vector3 PickAttackTarget()
        {
            var from = HiveDeposit.EnemyHive != null ? HiveDeposit.EnemyHive.transform.position : Vector3.zero;

            InsectUnit bestWorker = null;
            float bestWorkerDist = float.MaxValue;
            InsectUnit bestAny = null;
            float bestAnyDist = float.MaxValue;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                float d = Vector3.Distance(from, u.transform.position);
                if (u.Archetype == UnitArchetype.Worker && d < bestWorkerDist)
                { bestWorkerDist = d; bestWorker = u; }
                if (d < bestAnyDist) { bestAnyDist = d; bestAny = u; }
            }

            if (bestWorker != null && bestWorkerDist < bestAnyDist * 1.5f)
                return bestWorker.transform.position;
            if (bestAny != null) return bestAny.transform.position;
            if (HiveDeposit.PlayerHive != null) return HiveDeposit.PlayerHive.transform.position;
            return Vector3.zero;
        }

        static bool HasActiveFruit()
        {
            foreach (var f in RtsSimRegistry.FruitNodes)
                if (f != null && !f.Depleted) return true;
            return false;
        }
    }
}
