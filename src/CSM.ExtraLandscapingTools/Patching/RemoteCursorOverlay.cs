using CSM.ExtraLandscapingTools.CSM;
using HarmonyLib;
using UnityEngine;
using ColossalFramework;

namespace CSM.ExtraLandscapingTools.Patching
{
    [HarmonyPatch]
    public static class RemoteCursorOverlay
    {
        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            // Try ToolController first
            var method = typeof(ToolController).GetMethod("RenderOverlay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) return method;

            // Try ToolManager next
            method = typeof(ToolManager).GetMethod("RenderOverlay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) return method;

            // Try RenderManager as a fallback
            method = typeof(RenderManager).GetMethod("EndOverlay", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return method;
        }

        [HarmonyPostfix]
        public static void Postfix(object[] __args)
        {
            if (__args == null || __args.Length == 0) return;
            RenderManager.CameraInfo cameraInfo = __args[0] as RenderManager.CameraInfo;
            if (cameraInfo == null) return;

            foreach (var cursor in ToolCursorHandler.RemoteCursors.Values)
            {
                // Expiry: 2 seconds
                if (Time.time - cursor.LastUpdate > 2f) continue;

                Color color = Color.cyan;
                if (cursor.ToolName == "ResourceTool") color = Color.yellow;
                if (cursor.ToolName == "WaterTool") color = new Color(0f, 0.5f, 1f, 1f); // Brighter blue
                
                color.a = 0.5f; // Semitransparent

                // Use the game's OverlayEffect to draw the cursor
                var overlayEffect = Singleton<RenderManager>.instance.OverlayEffect;
                
                // Draw circle (standard for ELT brushes)
                // Signature: DrawCircle(cameraInfo, color, position, radius, minH, maxH, alphaBlend, renderReflections)
                overlayEffect.DrawCircle(cameraInfo, color, cursor.Position, cursor.BrushSize * 0.5f, -1f, 1024f, false, true);
            }
        }
    }
}
