using System;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// StarCraft-style supply system. Buildings provide capacity; units consume it.
    /// Production is blocked when used + cost would exceed the effective cap.
    /// </summary>
    public static class ColonyCapacity
    {
        public const int MaxCap = 250;
        public const int HiveCCProvided = 25;
        public const int AntNestCCProvided = 25;
        public const int RootCellarCCProvided = 15;

        public static event Action OnPlayerCCChanged;

        public static int GetUnitCCCost(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker => 1,
            UnitArchetype.BasicFighter => 2,
            UnitArchetype.BasicRanged => 3,
            UnitArchetype.BlackWidow => 5,
            UnitArchetype.StickSpy => 2,
            UnitArchetype.GiantStagBeetle => 6,
            _ => 1
        };

        /// <summary>Total CC provided by alive hive + operational buildings for a team.</summary>
        public static int GetCap(Team team)
        {
            if (team == Team.Player && Core.GameSession.IsLearningMode && !Core.GameSession.IsTutorialMode) return MaxCap;

            int cap = 0;

            // Main hive always provides base CC
            var hive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (hive != null && hive.IsAlive)
                cap += HiveCCProvided;

            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != team || !b.IsOperational) continue;
                cap += b.Type switch
                {
                    BuildingType.AntNest => AntNestCCProvided,
                    BuildingType.RootCellar => RootCellarCCProvided,
                    _ => 0
                };
            }

            return Mathf.Min(cap, MaxCap);
        }

        /// <summary>CC consumed by all alive units of a team.</summary>
        public static int GetUsed(Team team)
        {
            int used = 0;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != team) continue;
                used += GetUnitCCCost(u.Archetype);
            }
            return used;
        }

        /// <summary>CC that is queued but not yet spawned (pending production).</summary>
        public static int GetQueued(Team team)
        {
            int queued = 0;
            foreach (var b in ProductionBuilding.All)
            {
                if (b == null || b.Team != team) continue;
                queued += b.GetQueuedCCCost();
            }

            // Hive worker queues
            var hive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (hive != null)
                queued += hive.QueueCount * GetUnitCCCost(UnitArchetype.Worker);

            return queued;
        }

        /// <summary>Whether adding one more unit of this type would stay within capacity.</summary>
        public static bool CanAfford(Team team, UnitArchetype arch)
        {
            return GetUsed(team) + GetQueued(team) + GetUnitCCCost(arch) <= GetCap(team);
        }

        public static void NotifyChanged()
        {
            OnPlayerCCChanged?.Invoke();
        }
    }
}
