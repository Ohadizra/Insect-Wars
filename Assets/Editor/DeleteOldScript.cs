using UnityEditor;
using UnityEngine;

public class DeleteOldScript : IRunCommand
{
    public void Execute(IRunCommandContext context)
    {
        string oldPath = "Assets/_InsectWars/Scripts/RTS/Sc2BottomBar.cs";
        if (AssetDatabase.DeleteAsset(oldPath))
        {
            Debug.Log("Deleted " + oldPath);
        }
        else
        {
            Debug.LogWarning("Could not delete " + oldPath);
        }
    }
}
