namespace InsectWars.Core
{
    /// <summary>
    /// Documents RTS controls for rebinding / future InputAction assets.
    /// Implementation uses <see cref="UnityEngine.InputSystem"/> mouse and keyboard directly (Demo 0).
    /// </summary>
    public static class RtsInputBindings
    {
        public const string Select = "<Mouse>/leftButton";
        public const string Command = "<Mouse>/rightButton";
        public const string CameraOrbit = "<Mouse>/middleButton";
        public const string CameraScroll = "<Mouse>/scroll";
        public const string ModifierShift = "<Keyboard>/shift";
        public const string MenuCancel = "<Keyboard>/escape";
    }
}
