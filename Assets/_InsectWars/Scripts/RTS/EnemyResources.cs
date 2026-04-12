using InsectWars.Core;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Enemy-side economy tracker. Mirror of PlayerResources but static (no MonoBehaviour needed).
    /// Reset each match via <see cref="MapDirector"/>.
    /// </summary>
    public static class EnemyResources
    {
        public static int Calories { get; private set; }

        public static void Reset()
        {
            var start = Mathf.RoundToInt(200 * GameSession.DifficultyStartingCaloriesMultiplier);
            Calories = start;
        }

        public static void AddCalories(int amount)
        {
            if (amount <= 0) return;
            Calories += amount;
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || Calories < amount) return false;
            Calories -= amount;
            return true;
        }
    }
}
