using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace InsectWars.RTS
{
    [CustomEditor(typeof(SkirmishDirector))]
    public class SkirmishDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SkirmishDirector sd = (SkirmishDirector)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Build World Preview"))
            {
                sd.BuildWorldPreview();
                EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
            }
        }
    }
}
