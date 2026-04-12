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

        // ──────────── Control Groups (SC2 style) ────────────
        // Keys 1-9, 0 → group slots 0-9
        // Ctrl+Number (Win/Linux) / Cmd+Number (Mac) = set group
        // Shift+Number = add current selection to group
        // Alt+Number = append group to current selection
        // Number = recall group
        // Double-tap Number = recall + center camera
        public const string ControlGroupKeys = "<Keyboard>/1 .. <Keyboard>/0";
        public const string ModifierCreate = "<Keyboard>/leftCtrl (Win) | <Keyboard>/leftCommand (Mac)";
        public const string ModifierAdd = "<Keyboard>/leftShift";
        public const string ModifierAppend = "<Keyboard>/leftAlt";
    }
}
