using System;
using InsectWars.Core;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Player stockpile (calories from rotting fruits / apples).
    /// </summary>
    public class PlayerResources : MonoBehaviour
    {
        public static PlayerResources Instance { get; private set; }

        public int Calories { get; private set; }

        public event Action<int> OnCaloriesChanged;

        [SerializeField] int startingCalories = 200;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (GameSession.IsLearningMode)
            {
                AddCalories(10000);
                return;
            }
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
            MatchStats.RecordCaloriesGathered(amount);
            OnCaloriesChanged?.Invoke(Calories);
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
