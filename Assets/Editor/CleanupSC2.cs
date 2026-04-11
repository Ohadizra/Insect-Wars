using UnityEditor;
using UnityEngine;

public class CleanupSC2 : IRunCommand
{
    public void Execute(IRunCommandContext context)
    {
        // Delete the old script
        string oldScriptPath = "Assets/_InsectWars/Scripts/RTS/Sc2BottomBar.cs";
        if (AssetDatabase.DeleteAsset(oldScriptPath))
            Debug.Log("Deleted: " + oldScriptPath);

        // Rename the SC2BottomBar object in the scene
        var go = GameObject.Find("SC2BottomBar");
        if (go != null)
        {
            Undo.RecordObject(go, "Rename SC2BottomBar");
            go.name = "BottomBar";
            EditorUtility.SetDirty(go);
            Debug.Log("Renamed SC2BottomBar GameObject to BottomBar");
        }
    }
}
