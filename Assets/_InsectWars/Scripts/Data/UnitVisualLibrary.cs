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
                    "Assets/_InsectWars/Prefabs/UndergroundBuilding.prefab");
            if (skyTowerPrefab == null)
                skyTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Prefabs/SkyTower.prefab");
            if (calorieChunkPrefab == null)
                calorieChunkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Models/CalorieChunk 4.prefab");
            if (bigAppleMaterial == null)
                bigAppleMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_InsectWars/Materials/BigRootedApple.mat");
            if (clayWallPrefab == null)
                clayWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Models/ClayWall.prefab");
            if (hiveMaterial == null)
                hiveMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_InsectWars/Buildings/AntNest/AntNestStage2Mat.mat");
            if (rootCellarPrefab == null)
                rootCellarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Prefabs/RootCellar.prefab");
            if (completionVfxPrefab == null)
                completionVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Prefabs/BuildingCompleteVFX.prefab");
            if (constructionPrefab == null)
                constructionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_InsectWars/Models/ConstructionCocoon.prefab");
            }
            #endif

            [Header("Units — root must have NavMeshAgent, CapsuleCollider, InsectUnit")]
            public GameObject workerPrefab;
            public GameObject meleePrefab;
            public GameObject rangedPrefab;
            public GameObject blackWidowPrefab;
            public GameObject hawkMothPrefab;

            [Header("Buildings")]
            public GameObject hivePrefab;
            public Material hiveMaterial;
            public GameObject undergroundPrefab;
            public GameObject skyTowerPrefab;
            public GameObject rootCellarPrefab;
            public GameObject completionVfxPrefab;
            public GameObject constructionPrefab;

            [Header("Environment")]
        public GameObject rottingApplePrefab;
        public GameObject calorieChunkPrefab;
        public GameObject clayWallPrefab;
        public GameObject clayWallCornerPrefab;
        public GameObject clayWallPillarPrefab;
        public Material bigAppleMaterial;
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
                UnitArchetype.BlackWidow => blackWidowPrefab,
                UnitArchetype.HawkMoth => hawkMothPrefab,
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
                RTS.BuildingType.RootCellar => rootCellarPrefab,
                _ => null
            };
        }
    }
}
