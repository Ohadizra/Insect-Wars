using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    public static class TeamPalette
    {
        public static Color UnitBody(Team team, UnitArchetype archetype) => GetShellColor(team);

        public static Color WeaponAccent(Team team) => team == Team.Player
            ? new Color(0.2f, 0.5f, 1f)
            : new Color(1f, 0.2f, 0.2f);

        public static Color GetTeamColor(Team team) => team == Team.Player
            ? new Color(0f, 0.45f, 1f)
            : new Color(1f, 0.05f, 0f);

        public static Color GetShellColor(Team team) => team == Team.Player
            ? new Color(0.3f, 0.65f, 1f)
            : new Color(1f, 0.35f, 0.2f);

        /// <summary>
        /// Tints every Renderer on <paramref name="root"/> (and its children)
        /// with the team's shell color. Pass <paramref name="skip"/> to exclude
        /// a specific child GameObject (e.g. a selection ring).
        /// </summary>
        public static void ApplyToGameObject(Team team, UnityEngine.GameObject root, UnityEngine.GameObject skip = null)
        {
            if (root == null) return;
            var col = GetShellColor(team);
            foreach (var rend in root.GetComponentsInChildren<UnityEngine.Renderer>())
            {
                if (skip != null && rend.gameObject == skip) continue;
                foreach (var mat in rend.materials)
                {
                    if (mat == null) continue;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                    else if (mat.HasProperty("_Color"))  mat.color = col;
                }
            }
        }
    }
    }
