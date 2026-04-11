using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InsectWars.EditorTools
{
    public static class CreateMapScenes
    {
        static readonly string[] MapSceneNames =
        {
            "ThornBasin",
            "Lalush",
            "BraveNewWorld",
            "ShazuBell",
            "ShazuDen",
        };

        const string SourceScene = "Assets/_InsectWars/Scenes/SkirmishDemo.unity";
        const string ScenesFolder = "Assets/_InsectWars/Scenes/";

        [MenuItem("Insect Wars/Create All Map Scenes")]
        static void CreateAll()
        {
            if (!System.IO.File.Exists(SourceScene))
            {
                Debug.LogError($"Source scene not found: {SourceScene}");
                return;
            }

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int created = 0;

            foreach (var mapName in MapSceneNames)
            {
                string dst = ScenesFolder + mapName + ".unity";

                if (System.IO.File.Exists(dst))
                {
                    Debug.Log($"[CreateMapScenes] {mapName}.unity already exists — skipping.");
                    EnsureInBuildSettings(buildScenes, dst);
                    continue;
                }

                if (AssetDatabase.CopyAsset(SourceScene, dst))
                {
                    Debug.Log($"[CreateMapScenes] Created {dst}");
                    EnsureInBuildSettings(buildScenes, dst);
                    created++;
                }
                else
                {
                    Debug.LogError($"[CreateMapScenes] Failed to copy scene to {dst}");
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.Refresh();
            Debug.Log($"[CreateMapScenes] Done. Created {created} new scene(s). Build Settings updated.");
        }

        static void EnsureInBuildSettings(List<EditorBuildSettingsScene> scenes, string path)
        {
            foreach (var s in scenes)
            {
                if (s.path == path) return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
