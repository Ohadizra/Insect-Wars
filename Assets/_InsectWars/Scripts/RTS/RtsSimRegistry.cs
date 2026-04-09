using System.Collections.Generic;

namespace InsectWars.RTS
{
    /// <summary>
    /// O(1) registration for units and resources — avoids hot-path FindObjectsByType scans.
    /// </summary>
    public static class RtsSimRegistry
    {
        static readonly List<InsectUnit> s_units = new(256);
        static readonly List<RottingFruitNode> s_fruits = new(64);

        public static IReadOnlyList<InsectUnit> Units => s_units;
        public static IReadOnlyList<RottingFruitNode> FruitNodes => s_fruits;

        public static void Register(InsectUnit u)
        {
            if (u != null && !s_units.Contains(u)) s_units.Add(u);
        }

        public static void Unregister(InsectUnit u)
        {
            if (u != null) s_units.Remove(u);
        }

        public static void Register(RottingFruitNode n)
        {
            if (n != null && !s_fruits.Contains(n)) s_fruits.Add(n);
        }

        public static void Unregister(RottingFruitNode n)
        {
            if (n != null) s_fruits.Remove(n);
        }

        public static int CountAlive(Team team)
        {
            var n = 0;
            for (var i = 0; i < s_units.Count; i++)
            {
                var u = s_units[i];
                if (u != null && u.IsAlive && u.Team == team) n++;
            }
            return n;
        }
    }
}
