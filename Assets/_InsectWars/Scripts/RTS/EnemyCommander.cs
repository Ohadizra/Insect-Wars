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
    /// Construction: expansion nests, multiple Undergrounds with NavMesh-validated placement.
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

        // Scouting
        bool _scoutSent;
        const float ScoutSendTime = 18f;

        float TickInterval => 1.5f * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

        const float ProduceBaseInterval = 4f;
        const float HarassInterval = 45f;
        const float AttackCooldown = 50f;

        static bool IsHard => GameSession.Difficulty == DemoDifficulty.Hard;
        static bool IsNormalOrHard => GameSession.Difficulty >= DemoDifficulty.Normal;

        float BuildCheckInterval => IsHard ? 7f : 10f;

        int DesiredWorkers => IsHard
            ? Mathf.Clamp(4 + (int)(_matchTime / 25f), 4, 12)
            : Mathf.Clamp(3 + (int)(_matchTime / 35f), 3, 10);

        int MaxCombat => IsHard
            ? Mathf.Clamp(6 + (int)(_matchTime / 18f), 6, 30)
            : IsNormalOrHard
                ? Mathf.Clamp(5 + (int)(_matchTime / 22f), 5, 25)
                : Mathf.Clamp(4 + (int)(_matchTime / 25f), 4, 20);

        int MaxUndergrounds => IsHard
            ? (_matchTime > 100f ? 3 : _matchTime > 50f ? 2 : 1)
            : (_matchTime > 180f ? 2 : 1);

        void Start()
        {
            ResetTracking();
            float diffMul = GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
            _nextProduceTime = IsHard ? 3f : 6f;
            _nextBuildTime = (IsHard ? 15f : 25f) * diffMul;
            _nextWorkerAssignTime = 2f;
            _nextHarassTime = (IsHard ? 50f : 80f) * diffMul;
            _nextMainAttackTime = (IsHard ? 40f : 55f) * diffMul;
        }

        void Update()
        {
            if (MatchDirector.MatchEnded || PauseController.IsPaused) return;

            _matchTime += Time.deltaTime;
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            TryScout();
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

            int undergrounds = 0, antNests = 0, rootCellars = 0, skyTowers = 0;
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != Team.Enemy) continue;
                switch (b.Type)
                {
                    case BuildingType.Underground: undergrounds++; break;
                    case BuildingType.AntNest: antNests++; break;
                    case BuildingType.RootCellar: rootCellars++; break;
                    case BuildingType.SkyTower: skyTowers++; break;
                }
            }

            int ccUsed = ColonyCapacity.GetUsed(Team.Enemy) + ColonyCapacity.GetQueued(Team.Enemy);
            int ccCap = ColonyCapacity.GetCap(Team.Enemy);
            int ccRoom = ccCap - ccUsed;
            int cellarCost = ProductionBuilding.GetBuildCost(BuildingType.RootCellar);
            int ccRoomThreshold = IsHard ? 8 : 5;
            if (ccRoom <= ccRoomThreshold && ccCap < ColonyCapacity.MaxCap
                && EnemyResources.Calories >= cellarCost + 50)
            {
                TryPlaceRootCellar();
            }

            float firstUndergroundTime = IsHard ? 15f : (IsNormalOrHard ? 20f : 25f);
            if (undergrounds == 0 && _matchTime > firstUndergroundTime)
            {
                TryPlaceUnderground();
                return;
            }

            int nestCost = ProductionBuilding.GetBuildCost(BuildingType.AntNest);
            float expansionTime = IsHard ? 50f : (IsNormalOrHard ? 70f : 90f);
            int maxNests = IsHard ? 3 : 2;
            if (antNests < maxNests && _matchTime > expansionTime && EnemyResources.Calories >= nestCost + 100)
            {
                if (TryPlaceExpansionNest()) return;
            }

            // Normal/Hard: build a SkyTower for Black Widows
            if (IsNormalOrHard && skyTowers == 0 && _matchTime > (IsHard ? 80f : 120f))
            {
                int towerCost = ProductionBuilding.GetBuildCost(BuildingType.SkyTower);
                if (EnemyResources.Calories >= towerCost + 100)
                    TryPlaceSkyTower();
            }

            int undergroundCost = ProductionBuilding.GetBuildCost(BuildingType.Underground);
            float secondUndergroundTime = IsHard ? 80f : (IsNormalOrHard ? 120f : 150f);
            if (undergrounds < MaxUndergrounds && _matchTime > secondUndergroundTime
                && EnemyResources.Calories >= undergroundCost + 100)
            {
                TryPlaceUnderground();
            }
        }

        void TryPlaceUnderground()
        {
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.Underground))) return;
            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;
            ProductionBuilding.Place(FindBuildPosition(hive.transform.position, 8f),
                BuildingType.Underground, Team.Enemy, startBuilt: true);
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
                BuildingType.AntNest, Team.Enemy, startBuilt: true);
            return true;
        }

        void TryPlaceRootCellar()
        {
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.RootCellar))) return;
            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;
            ProductionBuilding.Place(FindBuildPosition(hive.transform.position, 6f),
                BuildingType.RootCellar, Team.Enemy, startBuilt: true);
        }

        void TryPlaceSkyTower()
        {
            if (!EnemyResources.TrySpend(ProductionBuilding.GetBuildCost(BuildingType.SkyTower))) return;
            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;
            ProductionBuilding.Place(FindBuildPosition(hive.transform.position, 10f),
                BuildingType.SkyTower, Team.Enemy, startBuilt: true);
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
                    case UnitArchetype.BlackWidow: fighters++; break;
                    case UnitArchetype.GiantStagBeetle: fighters++; break;
                    case UnitArchetype.StickSpy: break;
                }
            }
            int combat = fighters + ranged;

            var nests = new List<ProductionBuilding>(4);
            var undergrounds = new List<ProductionBuilding>(4);
            var skyTowers = new List<ProductionBuilding>(4);
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != Team.Enemy) continue;
                if (b.Type == BuildingType.AntNest) nests.Add(b);
                else if (b.Type == BuildingType.Underground) undergrounds.Add(b);
                else if (b.Type == BuildingType.SkyTower) skyTowers.Add(b);
            }

            // --- Workers from all AntNests ---
            if (workers < DesiredWorkers)
            {
                foreach (var nest in nests)
                {
                    if (workers >= DesiredWorkers) break;
                    if (!ColonyCapacity.CanAfford(Team.Enemy, UnitArchetype.Worker)) break;
                    SetNestRallyToFruit(nest);
                    var unit = nest.ProduceUnit(UnitArchetype.Worker);
                    if (unit == null) continue;
                    workers++;
                    var fruit = FindBestFruit(unit.transform.position);
                    if (fruit != null) unit.OrderGather(fruit);
                }
            }

            // --- Combat units from all Undergrounds (mixed fighter / ranged / stag beetle) ---
            if (combat < MaxCombat && _matchTime > 20f)
            {
                foreach (var ug in undergrounds)
                {
                    if (combat >= MaxCombat) break;

                    UnitArchetype arch;
                    if (_rangedToggle % 8 == 7 && _matchTime > 60f && EnemyResources.Calories >= 350)
                        arch = UnitArchetype.GiantStagBeetle;
                    else if ((_rangedToggle % 5) >= 3)
                        arch = UnitArchetype.BasicRanged;
                    else
                        arch = UnitArchetype.BasicFighter;
                    _rangedToggle++;

                    if (!ColonyCapacity.CanAfford(Team.Enemy, arch)) break;
                    if (ug.ProduceUnit(arch) != null) combat++;
                }
            }

            // --- Black Widows from Sky Towers (max 2 active) ---
            if (_matchTime > 40f && skyTowers.Count > 0)
            {
                foreach (var st in skyTowers)
                {
                    if (combat >= MaxCombat) break;
                    if (!ColonyCapacity.CanAfford(Team.Enemy, UnitArchetype.BlackWidow)) break;
                    if (EnemyResources.Calories < 250) break;
                    if (st.ProduceUnit(UnitArchetype.BlackWidow) != null) combat++;
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

        // ──────────── Scouting ────────────

        void TryScout()
        {
            if (_scoutSent) return;
            if (_matchTime < ScoutSendTime) return;
            var fog = FogOfWarSystem.Instance;
            if (fog != null && fog.PlayerHiveDiscovered) { _scoutSent = true; return; }

            var hive = HiveDeposit.EnemyHive;
            if (hive == null) return;

            // Mirror the enemy hive position through map center to guess the player base location.
            var scoutTarget = -hive.transform.position;
            scoutTarget.y = 0f;

            InsectUnit scout = null;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (u.CurrentOrder == UnitOrder.Idle)
                {
                    scout = u;
                    break;
                }
            }

            if (scout == null) return;
            scout.OrderMove(scoutTarget);
            _scoutSent = true;
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

            int threatCount = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (Vector3.Distance(hive.transform.position, u.transform.position) < 25f)
                    threatCount++;
            }
            if (threatCount == 0) return false;

            float recallRange = IsHard ? 50f : 30f;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (u.Archetype == UnitArchetype.Worker) continue;
                if (Vector3.Distance(hive.transform.position, u.transform.position) > recallRange)
                    u.OrderAttackMove(hive.transform.position);
            }

            // Hard: pull nearby idle workers to fight when outnumbered
            if (IsHard && threatCount >= 3)
            {
                foreach (var u in RtsSimRegistry.Units)
                {
                    if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                    if (u.Archetype != UnitArchetype.Worker) continue;
                    if (Vector3.Distance(hive.transform.position, u.transform.position) < 20f
                        && u.CurrentOrder is UnitOrder.Idle or UnitOrder.Gather)
                    {
                        u.OrderAttackMove(hive.transform.position);
                    }
                }
            }

            return true;
        }

        void TryHarass()
        {
            if (_matchTime < _nextHarassTime) return;
            float harassCooldown = IsHard ? 25f : HarassInterval;
            _nextHarassTime = _matchTime + harassCooldown * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;

            var fog = FogOfWarSystem.Instance;

            Vector3? workerPos = null;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (u.Archetype != UnitArchetype.Worker) continue;
                if (fog != null && !fog.IsVisibleToEnemy(u.transform.position)) continue;
                workerPos = u.transform.position;
                break;
            }
            if (workerPos == null) return;

            int maxHarass = IsHard ? 5 : (IsNormalOrHard ? 4 : 3);
            int sent = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (sent >= maxHarass) break;
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

            int threshold;
            if (IsHard)
                threshold = _matchTime < 45f ? 3 : (_matchTime < 90f ? 5 : 6);
            else if (IsNormalOrHard)
                threshold = _matchTime < 55f ? 3 : (_matchTime < 110f ? 5 : 7);
            else
                threshold = _matchTime < 60f ? 3 : (_matchTime < 120f ? 5 : 7);

            bool allIn = EnemyResources.Calories < 30 && !HasActiveFruit() && army.Count > 0;

            if (!allIn)
            {
                if (army.Count < threshold) return;
                float firstWaveTime = IsHard ? 35f : 55f;
                if (!_firstWaveSent && _matchTime < firstWaveTime * GameSession.DifficultyEnemyAiThinkIntervalMultiplier) return;
            }

            var target = PickAttackTarget();
            if (target == Vector3.zero) return;

            // Normal/Hard: split army for a multi-prong attack when big enough
            if (IsNormalOrHard && army.Count >= 8)
            {
                var flanking = new List<InsectUnit>();
                var main = new List<InsectUnit>();

                for (int i = 0; i < army.Count; i++)
                {
                    if (i % 4 == 0 && flanking.Count < army.Count / 3)
                        flanking.Add(army[i]);
                    else
                        main.Add(army[i]);
                }

                var flankOffset = Vector3.Cross(Vector3.up,
                    (target - (HiveDeposit.EnemyHive != null ? HiveDeposit.EnemyHive.transform.position : Vector3.zero)).normalized) * 15f;
                var flankTarget = target + flankOffset;

                foreach (var u in main)
                    u.OrderAttackMove(target);
                foreach (var u in flanking)
                    u.OrderAttackMove(flankTarget);
            }
            else
            {
                foreach (var u in army)
                    u.OrderAttackMove(target);
            }

            _firstWaveSent = true;
            float cooldown = IsHard ? 35f : AttackCooldown;
            _nextMainAttackTime = _matchTime + cooldown * GameSession.DifficultyEnemyAiThinkIntervalMultiplier;
        }

        Vector3 PickAttackTarget()
        {
            var from = HiveDeposit.EnemyHive != null ? HiveDeposit.EnemyHive.transform.position : Vector3.zero;
            var fog = FogOfWarSystem.Instance;

            InsectUnit bestWorker = null;
            float bestWorkerDist = float.MaxValue;
            InsectUnit bestAny = null;
            float bestAnyDist = float.MaxValue;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                if (fog != null && !fog.IsVisibleToEnemy(u.transform.position)) continue;
                float d = Vector3.Distance(from, u.transform.position);
                if (u.Archetype == UnitArchetype.Worker && d < bestWorkerDist)
                { bestWorkerDist = d; bestWorker = u; }
                if (d < bestAnyDist) { bestAnyDist = d; bestAny = u; }
            }

            if (bestWorker != null && bestWorkerDist < bestAnyDist * 1.5f)
                return bestWorker.transform.position;
            if (bestAny != null) return bestAny.transform.position;

            // Fall back to scouted player hive position, or map center if not yet discovered.
            if (fog != null && fog.KnownPlayerHivePos.HasValue)
                return fog.KnownPlayerHivePos.Value;
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
