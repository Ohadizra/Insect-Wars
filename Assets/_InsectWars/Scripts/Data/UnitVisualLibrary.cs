using UnityEngine;

namespace InsectWars.Data
{
    [CreateAssetMenu(fileName = "UnitVisualLibrary", menuName = "Insect Wars/Unit Visual Library")]
    public class UnitVisualLibrary : ScriptableObject
    {
        [Header("Units — root must have NavMeshAgent, CapsuleCollider, InsectUnit")]
        public GameObject workerPrefab;
        public GameObject meleePrefab;
        public GameObject rangedPrefab;

        [Header("Command nest")]
        public GameObject hivePrefab;

        [Header("Environment")]
        public GameObject rottingApplePrefab;
        public Material groundMaterial;
        public TerrainLayer baseSoilLayer;
        public TerrainLayer drySoilLayer;

        [Header("Combat")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 38f;
        public float projectileMaxLifetime = 4f;

        public GameObject GetUnitPrefab(UnitArchetype arch)
        {
            return arch switch
            {
                UnitArchetype.Worker => workerPrefab,
                UnitArchetype.BasicFighter => meleePrefab,
                UnitArchetype.BasicRanged => rangedPrefab,
                _ => null
            };
        }
    }
}
