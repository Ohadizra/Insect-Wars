using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsectWars.Data
{
    [CreateAssetMenu(fileName = "UnitVisualLibrary", menuName = "Insect Wars/Unit Visual Library")]
    public class UnitVisualLibrary : ScriptableObject
    {
#if UNITY_EDITOR
        void OnValidate()
        {
            if (undergroundPrefab == null)
                undergroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Buildings/Underground/Meshy_AI_the_underground_0409154300_texture.fbx");
            if (skyTowerPrefab == null)
                skyTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Buildings/Meshy_AI_Citadel_of_the_Hexed__0409154908_texture.fbx");
        }
#endif

        [Header("Units — root must have NavMeshAgent, CapsuleCollider, InsectUnit")]
        public GameObject workerPrefab;
        public GameObject meleePrefab;
        public GameObject rangedPrefab;

        [Header("Buildings")]
        public GameObject hivePrefab;
        public GameObject undergroundPrefab;
        public GameObject skyTowerPrefab;

        [Header("Environment")]
        public GameObject rottingApplePrefab;
        public Material groundMaterial;
        public TerrainLayer baseSoilLayer;
        public TerrainLayer drySoilLayer;

        [Header("Terrain Features (optional — falls back to procedural)")]
        public GameObject waterPuddlePrefab;
        public GameObject tallGrassPrefab;
        public GameObject mudPatchPrefab;
        public GameObject thornPatchPrefab;
        public GameObject rockyRidgePrefab;

        public GameObject GetTerrainFeaturePrefab(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.WaterPuddle => waterPuddlePrefab,
            TerrainFeatureType.TallGrass   => tallGrassPrefab,
            TerrainFeatureType.MudPatch    => mudPatchPrefab,
            TerrainFeatureType.ThornPatch  => thornPatchPrefab,
            TerrainFeatureType.RockyRidge  => rockyRidgePrefab,
            _ => null
        };

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

        public GameObject GetBuildingPrefab(RTS.BuildingType type)
        {
            return type switch
            {
                RTS.BuildingType.AntNest => hivePrefab,
                RTS.BuildingType.Underground => undergroundPrefab,
                RTS.BuildingType.SkyTower => skyTowerPrefab,
                _ => null
            };
        }
    }
}
