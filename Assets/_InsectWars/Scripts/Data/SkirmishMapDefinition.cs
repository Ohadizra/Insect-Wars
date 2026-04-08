using System;
using UnityEngine;

namespace InsectWars.Data
{
    [Serializable]
    public struct ClayPlaced
    {
        public Vector3 position;
        public Vector3 scale;
    }

    [Serializable]
    public struct FruitPlaced
    {
        public Vector3 position;
        public int calories;
        public int gatherPerTick;
        public float gatherSeconds;
    }

    [Serializable]
    public struct CactiSeedPlaced
    {
        public Vector3 position;
    }

    /// <summary>
    /// Serialized map layout for SkirmishDirector. Assign on the director or leave null for built-in Demo 0 defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "SkirmishMap", menuName = "Insect Wars/Skirmish Map Definition")]
    public class SkirmishMapDefinition : ScriptableObject
    {
        public float mapHalfExtent = 88f;
        public Vector3 playerArmyStart = new Vector3(-54f, 0f, -44f);
        public Vector3 enemyArmyStart = new Vector3(62f, 0f, 52f);
        public Vector3 playerHivePosition = new Vector3(-62f, 1f, -52f);
        public Vector3 enemyHivePosition = new Vector3(62f, 1f, 52f);
        public Vector3 cameraFocusWorld = new Vector3(-48f, 0f, -38f);
        public Vector3 bigApplePosition = new Vector3(-50f, 1.5f, -42f);
        public int passiveScatterSeed = 18427;
        public ClayPlaced[] clay = Array.Empty<ClayPlaced>();
        public FruitPlaced[] fruits = Array.Empty<FruitPlaced>();
        public CactiSeedPlaced[] cactiSeeds = Array.Empty<CactiSeedPlaced>();
    }
}
