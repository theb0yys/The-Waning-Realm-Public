using System;
using TaleWorlds.Library;

namespace XorberaxTFTAOMPackControl
{
    internal static class PackLog
    {
        internal static void Info(string message)
        {
            try
            {
                Debug.Print("[Xorberax TFTAOM Experience] " + message);
            }
            catch { }
        }

        internal static void Error(string message, Exception ex)
        {
            Info(message + ": " + ex.GetType().Name + " - " + ex.Message);
        }
    }
}
