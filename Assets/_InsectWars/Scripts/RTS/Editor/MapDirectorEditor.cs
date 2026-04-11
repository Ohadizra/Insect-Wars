using InsectWars.Data;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using InsectWars.Core;

namespace InsectWars.RTS
{
    [CustomEditor(typeof(MapDirector))]
    public class MapDirectorEditor : UnityEditor.Editor
    {
        int _presetIndex;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapDirector sd = (MapDirector)target;

            GUILayout.Space(10);

            var presets = MapPresets.GetAll();
            var names = new string[presets.Length + 1];
            names[0] = "(Director default)";
            for (int i = 0; i < presets.Length; i++)
                names[i + 1] = presets[i].displayName;
            _presetIndex = Mathf.Clamp(_presetIndex, 0, names.Length - 1);

            EditorGUILayout.LabelField("Preview Map Preset", EditorStyles.boldLabel);
            _presetIndex = EditorGUILayout.Popup(_presetIndex, names);

            if (GUILayout.Button("Build World Preview"))
            {
                if (_presetIndex > 0)
                    GameSession.SetSelectedMap(presets[_presetIndex - 1]);
                else
                    GameSession.SetSelectedMap(null);

                sd.BuildWorldPreview();
                EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
            }
        }
    }
}
