namespace InsectWars.RTS
{
    /// <summary>
    /// Runtime bounds for the skirmish field (set by <see cref="SkirmishDirector"/>).
    /// </summary>
    public static class SkirmishPlayArea
    {
        public static float HalfExtent { get; private set; }
        public static float MinimapOrthographicSize { get; private set; }

        public static bool HasBounds => HalfExtent > 0.5f;

        public static void Configure(float halfExtent, float minimapOrthoSize)
        {
            HalfExtent = halfExtent;
            MinimapOrthographicSize = minimapOrthoSize;
        }

        public static void Clear()
        {
            HalfExtent = 0f;
            MinimapOrthographicSize = 0f;
        }
    }
}
