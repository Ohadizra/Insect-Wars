using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace InsectWars.Editor
{
    public static class BuildPlayer
    {
        [MenuItem("Build/Build macOS")]
        public static void BuildMacOS()
        {
            string[] scenes = new string[]
            {
                "Assets/_InsectWars/Scenes/Home.unity",
                "Assets/_InsectWars/Scenes/SkirmishDemo.unity"
            };

            string buildPath = "Builds/InsectWars.app";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize / (1024 * 1024)} MB, time: {summary.totalTime}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"Build failed: {summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Warning)
                            Debug.LogError($"[{step.name}] {msg.content}");
                    }
                }
                EditorApplication.Exit(1);
            }
        }
    }
}
