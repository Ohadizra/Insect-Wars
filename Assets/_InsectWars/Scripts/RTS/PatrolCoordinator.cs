using UnityEngine;

namespace InsectWars.RTS
{
    public static class PatrolCoordinator
    {
        static Vector3? s_first;

        public static bool WaitingForSecondPoint => s_first.HasValue;

        public static void Reset()
        {
            s_first = null;
        }

        /// <summary>Returns true if this click was the first waypoint (waiting for second).</summary>
        public static bool TryHandlePatrolClick(Vector3 world, out Vector3 a, out Vector3 b)
        {
            a = default;
            b = default;
            if (!s_first.HasValue)
            {
                s_first = world;
                return true;
            }
            a = s_first.Value;
            b = world;
            s_first = null;
            return false;
        }
    }
}
