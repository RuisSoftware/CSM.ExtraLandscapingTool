using UnityEngine;

namespace CSM.ExtraLandscapingTools.Utils
{
    internal static class Log
    {
        private const string Prefix = "[CSM.ELT] ";

        internal static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        internal static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        internal static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }
}
