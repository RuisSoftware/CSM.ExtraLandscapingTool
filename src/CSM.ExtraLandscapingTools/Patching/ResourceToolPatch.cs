using System.Collections.Generic;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using CSM.ExtraLandscapingTools.CSM;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    [HarmonyPatch(typeof(ResourceTool), "SimulationStep")]
    public static class ResourceToolPatch
    {
        private static int m_frameCounter = 0;
        private static Vector3 m_lastSentPosition;

        [HarmonyPostfix]
        public static void Postfix(ResourceTool __instance)
        {
            if (CsmBridge.IsIgnoring()) return;

            // Sync Cursor
            m_frameCounter++;
            if (m_frameCounter >= 2) // Sync every 2 frames if moved
            {
                m_frameCounter = 0;
                var pos = Util.GetPrivate<Vector3>(__instance, "m_mousePosition");
                var size = Util.GetPrivate<float>(__instance, "m_brushSize");
                if (Vector3.Distance(pos, m_lastSentPosition) > 0.5f)
                {
                    CsmBridge.SendToolCursor(pos, size, "ResourceTool");
                    m_lastSentPosition = pos;
                }
            }

            // Sync Painting
            bool leftDown = Util.GetPrivate<bool>(__instance, "m_mouseLeftDown");
            bool rightDown = Util.GetPrivate<bool>(__instance, "m_mouseRightDown");

            if (leftDown || rightDown)
            {
                // Capture the painted cells. 
                // Since ResourceTool works in SimulationStep, we need to replicate its logic 
                // or capture the modification.
                
                // For CSM, we typically send the mouse position and brush settings 
                // and let the handler re-apply the brush. 
                // BUT, our ResourceHandler (Step 2315) expects CellData.
                
                // I'll update ResourceTool's ApplyBrush logic or similar.
                // Assuming it has an ApplyBrush method.
                // If not, we'll send the mouse click action.
            }
        }
    }
}
