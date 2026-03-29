using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Surface;
using CSM.ExtraLandscapingTools.Utils;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches BrushOptionPanel to support surface tool brush size/strength/brush selection.
    /// </summary>
    [HarmonyPatch(typeof(BrushOptionPanel))]
    public static class BrushOptionPanelPatch
    {
        [HarmonyPatch("SetBrushSize")]
        [HarmonyPrefix]
        public static bool SetBrushSizePrefix(BrushOptionPanel __instance, float val)
        {
            var brushSizeSlider = (UISlider)typeof(BrushOptionPanel)
                .GetField("m_BrushSizeSlider", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance);

            PropTool currentTool1 = ToolsModifierControl.GetCurrentTool<PropTool>();
            if (currentTool1 != null)
            {
                currentTool1.m_brushSize = val;
                currentTool1.m_mode = (double)currentTool1.m_brushSize != (double)brushSizeSlider.minValue
                    ? PropTool.Mode.Brush : PropTool.Mode.Single;
            }
            TerrainTool currentTool2 = ToolsModifierControl.GetCurrentTool<TerrainTool>();
            if (currentTool2 != null)
            {
                currentTool2.m_brushSize = val;
                TerrainToolPatch.SizeMode = (double)currentTool2.m_brushSize != (double)brushSizeSlider.minValue
                    ? TerrainToolPatch.BrushSizeMode.Brush : TerrainToolPatch.BrushSizeMode.Single;
            }
            TreeTool currentTool3 = ToolsModifierControl.GetCurrentTool<TreeTool>();
            if (currentTool3 != null)
            {
                currentTool3.m_brushSize = val;
                currentTool3.m_mode = (double)currentTool3.m_brushSize != (double)brushSizeSlider.minValue
                    ? TreeTool.Mode.Brush : TreeTool.Mode.Single;
            }
            ResourceTool currentTool4 = ToolsModifierControl.GetCurrentTool<ResourceTool>();
            if (currentTool4 != null)
            {
                currentTool4.m_brushSize = val;
            }

            // Surface tool
            var surfaceTool = ToolsModifierControl.GetCurrentTool<InGameSurfaceTool>();
            if (surfaceTool != null)
            {
                surfaceTool.m_brushSize = val;
                surfaceTool.m_mode = val == (double)brushSizeSlider.minValue
                    ? InGameSurfaceTool.Mode.Single : InGameSurfaceTool.Mode.Brush;
            }

            return false; // Skip original
        }

        [HarmonyPatch("SetBrushStrength")]
        [HarmonyPrefix]
        public static bool SetBrushStrengthPrefix(float val)
        {
            PropTool currentTool1 = ToolsModifierControl.GetCurrentTool<PropTool>();
            if (currentTool1 != null)
                currentTool1.m_strength = val;
            TerrainTool currentTool2 = ToolsModifierControl.GetCurrentTool<TerrainTool>();
            if (currentTool2 != null)
                currentTool2.m_strength = val;
            TreeTool currentTool3 = ToolsModifierControl.GetCurrentTool<TreeTool>();
            if (currentTool3 != null)
                currentTool3.m_strength = val;
            ResourceTool currentTool4 = ToolsModifierControl.GetCurrentTool<ResourceTool>();
            if (currentTool4 != null)
                currentTool4.m_strength = val;

            return false; // Skip original
        }

        [HarmonyPatch("OnMouseDown")]
        [HarmonyPrefix]
        public static bool OnMouseDownPrefix(BrushOptionPanel __instance, UIComponent comp, UIMouseEventParameter p)
        {
            UIButton uiButton = p.source as UIButton;
            var getByIndex = typeof(BrushOptionPanel).GetMethod("GetByIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            int byIndex = (int)getByIndex.Invoke(__instance, new object[] { (UIComponent)uiButton });
            if (byIndex == -1)
                return false;

            var selectByIndex = typeof(BrushOptionPanel).GetMethod("SelectByIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            selectByIndex.Invoke(__instance, new object[] { byIndex });

            Texture2D texture2D = uiButton.objectUserData as Texture2D;
            var brushesContainer = (UIScrollablePanel)typeof(BrushOptionPanel)
                .GetField("m_BrushesContainer", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance);

            if ((UnityEngine.Object)uiButton.parent != (UnityEngine.Object)brushesContainer || texture2D == null)
                return false;

            TerrainTool tool1 = ToolsModifierControl.GetTool<TerrainTool>();
            if (tool1 != null) tool1.m_brush = texture2D;
            TreeTool tool2 = ToolsModifierControl.GetTool<TreeTool>();
            if (tool2 != null) tool2.m_brush = texture2D;
            ResourceTool tool3 = ToolsModifierControl.GetTool<ResourceTool>();
            if (tool3 != null) tool3.m_brush = texture2D;
            PropTool tool4 = ToolsModifierControl.GetTool<PropTool>();
            if (tool4 != null) tool4.m_brush = texture2D;

            // Surface tool
            var surfaceTool = ToolsModifierControl.GetCurrentTool<InGameSurfaceTool>();
            if (surfaceTool != null)
                surfaceTool.m_brush = texture2D;

            return false; // Skip original
        }

        [HarmonyPatch("SupportsSingle")]
        [HarmonyPrefix]
        public static bool SupportsSinglePrefix(ref bool __result)
        {
            // Surface tool supports single mode
            if (ToolsModifierControl.GetCurrentTool<InGameSurfaceTool>() != null)
            {
                __result = true;
                return false;
            }
            // Let original handle PropTool, TreeTool, TerrainTool
            if (ToolsModifierControl.GetCurrentTool<PropTool>() != null ||
                ToolsModifierControl.GetCurrentTool<TreeTool>() != null ||
                ToolsModifierControl.GetCurrentTool<TerrainTool>() != null)
            {
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }
}
