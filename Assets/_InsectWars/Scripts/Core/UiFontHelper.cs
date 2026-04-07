using UnityEngine;

namespace InsectWars.Core
{
    public static class UiFontHelper
    {
        static Font s_cached;

        public static Font GetFont()
        {
            if (s_cached != null) return s_cached;
            s_cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (s_cached == null) s_cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (s_cached == null)
            {
                try
                {
                    s_cached = Font.CreateDynamicFontFromOSFont(
                        new[] { "Arial", "Helvetica", "Liberation Sans", "Segoe UI" }, 16);
                }
                catch
                {
                    /* ignored */
                }
            }
            return s_cached;
        }
    }
}
