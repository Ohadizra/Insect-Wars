using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace InsectWars.RTS
{
    [CustomEditor(typeof(SkirmishDirector))]
    public class SkirmishDirectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SkirmishDirector sd = (SkirmishDirector)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Build World Preview"))
            {
                // We can't easily call Start() from Editor, but we can call a public method.
                // Let's modify SkirmishDirector to have a public Build() method.
                // For now, let's just log.
                Debug.Log("World build should be done by entering Play Mode. The script builds everything at runtime.");
            }
        }
    }
}
