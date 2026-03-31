using HarmonyLib;
using UnityEngine;
using CSM.ExtraLandscapingTools.CSM;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    [HarmonyPatch(typeof(WaterTool), "SimulationStep")]
    public static class WaterToolSyncPatch
    {
        private static int m_frameCounter = 0;
        private static Vector3 m_lastSentPosition;

        [HarmonyPostfix]
        public static void Postfix(WaterTool __instance)
        {
            if (CsmBridge.IsIgnoring()) return;

            m_frameCounter++;
            if (m_frameCounter >= 2) // Sync every 2 frames if moved
            {
                m_frameCounter = 0;
                var pos = Util.GetPrivate<Vector3>(__instance, "m_mousePosition");
                if (Vector3.Distance(pos, m_lastSentPosition) > 0.5f)
                {
                    CsmBridge.SendToolCursor(pos, 70f, "WaterTool"); // Increased visibility for water tool
                    m_lastSentPosition = pos;
                }
            }
        }
    }
}
