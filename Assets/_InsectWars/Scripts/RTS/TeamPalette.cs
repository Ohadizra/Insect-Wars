using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    public static class TeamPalette
    {
        public static Color UnitBody(Team team, UnitArchetype archetype)
        {
            // Shell color for the unit's "skin"
            return GetShellColor(team);
        }

        public static Color WeaponAccent(Team team)
        {
            return team == Team.Player
                ? new Color(0.2f, 0.5f, 1f) // Player Blue
                : new Color(1f, 0.2f, 0.2f); // Enemy Red
        }

        public static Color GetTeamColor(Team team)
        {
            return team == Team.Player
                ? new Color(0f, 0.45f, 1f)   // Vibrant Blue
                : new Color(1f, 0.05f, 0f);  // Vibrant Red
        }
        
        public static Color GetShellColor(Team team)
        {
            // A slightly lighter, more "skin-like" version of the team color
            return team == Team.Player
                ? new Color(0.3f, 0.65f, 1f)
                : new Color(1f, 0.35f, 0.2f);
        }
    }
    }
