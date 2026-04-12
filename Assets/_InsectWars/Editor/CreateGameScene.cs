using InsectWars.Data;
using InsectWars.RTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [MenuItem("Insect Wars/Rebuild SkirmishDemo Scene")]
        static void RebuildSkirmishScene()
        {
            const string scenePath = "Assets/_InsectWars/Scenes/SkirmishDemo.unity";
            const string libPath = "Assets/_InsectWars/Data/DefaultVisualLibrary.asset";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var directorGo = new GameObject("SkirmishDirector");
            var director = directorGo.AddComponent<SkirmishDirector>();

            var lib = AssetDatabase.LoadAssetAtPath<UnitVisualLibrary>(libPath);
            if (lib != null)
            {
                var so = new SerializedObject(director);
                var libProp = so.FindProperty("visualLibrary");
                if (libProp != null)
                {
                    libProp.objectReferenceValue = lib;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.84f);
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<RTSCameraController>();
            camGo.transform.position = new Vector3(0f, 24f, -20f);
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            EnsureInBuildSettings(scenePath);

            Debug.Log("[EditorAutoFix] SkirmishDemo.unity rebuilt with SkirmishDirector, Camera, and Light.");
        }

        static void EnsureInBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Insect Wars/Fix Visual Library — Set Hive Prefab")]
        static void FixHivePrefab()
        {
            FixHivePrefabIfNeeded();
        }
    }
}
