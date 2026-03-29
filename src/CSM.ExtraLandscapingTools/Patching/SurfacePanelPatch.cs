using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Surface;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches SurfacePanel.OnButtonClicked to activate InGameSurfaceTool with the correct surface type.
    /// </summary>
    [HarmonyPatch(typeof(SurfacePanel))]
    public static class SurfacePanelPatch
    {
        private static readonly PositionData<TerrainModify.Surface>[] kSurfaces =
            ColossalFramework.Utils.GetOrderedEnumData<TerrainModify.Surface>();
        private static UIPanel m_OptionsBrushPanel;

        [HarmonyPatch("OnButtonClicked")]
        [HarmonyPrefix]
        public static bool OnButtonClickedPrefix(SurfacePanel __instance, UIComponent comp)
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");

            if (optionsBar != null && m_OptionsBrushPanel == null)
                m_OptionsBrushPanel = optionsBar.Find<UIPanel>("BrushPanel");

            InGameSurfaceTool surfaceTool = ToolsModifierControl.SetTool<InGameSurfaceTool>();
            if (surfaceTool == null)
                return false;

            if (m_OptionsBrushPanel != null)
                m_OptionsBrushPanel.isVisible = true;

            // Map by zOrder like original SurfacePainter
            if (comp.zOrder >= 0 && comp.zOrder < kSurfaces.Length)
                surfaceTool.m_surface = kSurfaces[comp.zOrder].enumValue;

            return false;
        }
    }
}
