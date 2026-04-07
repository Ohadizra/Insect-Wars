using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    public static class TeamPalette
    {
        public static Color UnitBody(Team team, UnitArchetype archetype)
        {
            if (team == Team.Player)
            {
                return archetype switch
                {
                    UnitArchetype.Worker => new Color(0.45f, 0.72f, 1f),
                    UnitArchetype.BasicFighter => new Color(0.18f, 0.42f, 0.95f),
                    UnitArchetype.BasicRanged => new Color(0.28f, 0.55f, 1f),
                    _ => new Color(0.25f, 0.55f, 1f)
                };
            }
            return archetype switch
            {
                UnitArchetype.Worker => new Color(1f, 0.52f, 0.48f),
                UnitArchetype.BasicFighter => new Color(0.92f, 0.14f, 0.12f),
                UnitArchetype.BasicRanged => new Color(1f, 0.32f, 0.22f),
                _ => new Color(0.95f, 0.22f, 0.18f)
            };
        }

        public static Color WeaponAccent(Team team)
        {
            return team == Team.Player
                ? new Color(0.08f, 0.22f, 0.55f)
                : new Color(0.45f, 0.05f, 0.05f);
        }

        public static Color GetTeamColor(Team team)
        {
            return team == Team.Player
                ? new Color(0.1f, 0.4f, 1f)  // Bright Blue
                : new Color(1f, 0.1f, 0.1f); // Bright Red
        }
        }
        }
