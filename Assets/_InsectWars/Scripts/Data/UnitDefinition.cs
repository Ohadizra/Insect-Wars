using UnityEngine;

namespace InsectWars.Data
{
    public enum UnitArchetype
    {
        Worker = 0,
        BasicFighter = 1,
        BasicRanged = 2
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
                _ => type.ToString()
            };
            d.unitColor = bodyColor;
            d.canGather = type == UnitArchetype.Worker;
            d.moveSpeed = type switch
            {
                UnitArchetype.Worker => 4f,
                UnitArchetype.BasicFighter => 5.4f,
                UnitArchetype.BasicRanged => 4.2f,
                _ => 4.5f
            };
            d.maxHealth = type switch
            {
                UnitArchetype.Worker => 35f,
                UnitArchetype.BasicFighter => 38f,
                UnitArchetype.BasicRanged => 42f,
                _ => 40f
            };
            d.attackDamage = type switch
            {
                UnitArchetype.BasicRanged => 5f,
                UnitArchetype.BasicFighter => 9f,
                _ => 4f
            };
            d.attackRange = type == UnitArchetype.BasicRanged ? 5.5f : 1.55f;
            d.attackCooldown = type == UnitArchetype.BasicRanged ? 1.3f : 0.95f;
            d.visionRadius = 12f;
            return d;
        }
    }
}
