using InsectWars.Data;
using UnityEditor;
using UnityEngine;

namespace InsectWars.EditorTools
{
    [InitializeOnLoad]
    public static class EditorAutoFix
    {
        static EditorAutoFix()
        {
            EditorApplication.delayCall += () =>
            {
                FixHivePrefabIfNeeded();
            };
        }

        static void FixHivePrefabIfNeeded()
        {
            const string libPath = "Assets/_InsectWars/Data/DefaultVisualLibrary.asset";
            const string prefabPath = "Assets/_InsectWars/Buildings/AntNest/AntNestStage2Visual.prefab";

            var lib = AssetDatabase.LoadAssetAtPath<UnitVisualLibrary>(libPath);
            if (lib == null) return;

            var correctPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (correctPrefab == null) return;

            if (lib.hivePrefab == correctPrefab) return;

            lib.hivePrefab = correctPrefab;
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EditorAutoFix] hivePrefab → {prefabPath}");
        }

        [MenuItem("Insect Wars/Fix Visual Library — Set Hive Prefab")]
        static void FixHivePrefab()
        {
            FixHivePrefabIfNeeded();
        }
    }
}
