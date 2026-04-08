using System;
using InsectWars.Core;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Player stockpile (calories from rotting fruits / apples, cacti seeds).
    /// </summary>
    public class PlayerResources : MonoBehaviour
    {
        public static PlayerResources Instance { get; private set; }

        public int Calories { get; private set; }
        public int CactiSeeds { get; private set; }

        public event Action<int> OnCaloriesChanged;
        public event Action<int> OnCactiSeedsChanged;

        [SerializeField] int startingCalories = 200;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            var start = Mathf.RoundToInt(startingCalories * GameSession.DifficultyStartingCaloriesMultiplier);
            if (start > 0)
                AddCalories(start);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddCalories(int amount)
        {
            if (amount <= 0) return;
            Calories += amount;
            OnCaloriesChanged?.Invoke(Calories);
        }

        public void AddCactiSeeds(int amount)
        {
            if (amount <= 0) return;
            CactiSeeds += amount;
            OnCactiSeedsChanged?.Invoke(CactiSeeds);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Calories < amount) return false;
            Calories -= amount;
            OnCaloriesChanged?.Invoke(Calories);
            return true;
        }
    }
}
