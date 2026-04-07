using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Optional wiring for a prefab-based demo HUD. Assign fields on an instantiated prefab root or child.
    /// </summary>
    public class DemoHudBindings : MonoBehaviour
    {
        [SerializeField] Text calorieText;
        [SerializeField] Text selectionText;

        public Text CalorieText => calorieText;
        public Text SelectionText => selectionText;
    }
}
