using UnityEngine;

namespace InsectWars.Data
{
    public enum UnitArchetype
    {
        Worker = 0,
        BasicFighter = 1,
        BasicRanged = 2,
        BlackWidow = 3
    }

    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Insect Wars/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        public UnitArchetype archetype;
        public string displayName;
        public Color unitColor = Color.white;
        public float moveSpeed = 4.5f;
        public float maxHealth = 40f;
        public float attackDamage = 8f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.1f;
        public bool canGather;
        public float visionRadius = 12f;

        public static UnitDefinition CreateRuntimeDefault(UnitArchetype type, Color bodyColor)
        {
            var d = CreateInstance<UnitDefinition>();
            d.archetype = type;
            d.displayName = type switch
            {
                UnitArchetype.Worker => "Worker",
                UnitArchetype.BasicFighter => "Fighter",
                UnitArchetype.BasicRanged => "Bombardier",
                UnitArchetype.BlackWidow => "Black Widow",
                _ => type.ToString()
            };
            d.unitColor = bodyColor;
            d.canGather = type == UnitArchetype.Worker;
            d.moveSpeed = type switch
            {
                UnitArchetype.Worker => 4f,
                UnitArchetype.BasicFighter => 5.4f,
                UnitArchetype.BasicRanged => 4.2f,
                UnitArchetype.BlackWidow => 4.8f,
                _ => 4.5f
            };
            d.maxHealth = type switch
            {
                UnitArchetype.Worker => 35f,
                UnitArchetype.BasicFighter => 38f,
                UnitArchetype.BasicRanged => 42f,
                UnitArchetype.BlackWidow => 55f,
                _ => 40f
            };
            d.attackDamage = type switch
            {
                UnitArchetype.BasicRanged => 7f,
                UnitArchetype.BasicFighter => 9f,
                UnitArchetype.BlackWidow => 6f,
                _ => 4f
            };
            d.attackRange = type switch
            {
                UnitArchetype.BasicRanged => 8f,
                _ => 1.55f
            };
            d.attackCooldown = type switch
            {
                UnitArchetype.BasicRanged => 0.82f,
                UnitArchetype.BlackWidow => 1.1f,
                _ => 0.95f
            };
            d.visionRadius = 12f;
            return d;
        }
    }
}
