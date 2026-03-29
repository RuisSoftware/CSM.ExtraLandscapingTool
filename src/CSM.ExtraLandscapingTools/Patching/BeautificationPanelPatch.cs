using ColossalFramework.UI;
using HarmonyLib;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches BeautificationPanel to show brush options panel when tree/prop tools are selected.
    /// </summary>
    [HarmonyPatch(typeof(BeautificationPanel), "OnButtonClicked")]
    public static class BeautificationPanelPatch
    {
        private static UIPanel m_OptionsBrushPanel;

        public static bool Prefix(BeautificationPanel __instance, UIComponent comp)
        {
            object objectUserData = comp.objectUserData;
            BuildingInfo buildingInfo = objectUserData as BuildingInfo;
            NetInfo netInfo = objectUserData as NetInfo;
            TreeInfo treeInfo = objectUserData as TreeInfo;
            PropInfo propInfo = objectUserData as PropInfo;

            m_OptionsBrushPanel?.Hide();

            if (buildingInfo != null)
            {
                BuildingTool buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
                if (buildingTool != null)
                {
                    buildingTool.m_prefab = buildingInfo;
                    buildingTool.m_relocate = 0;
                }
                return false;
            }
            if (netInfo != null)
            {
                NetTool netTool = ToolsModifierControl.SetTool<NetTool>();
                if (netTool != null)
                {
                    if (netInfo.GetSubService() == ItemClass.SubService.BeautificationParks)
                        typeof(BeautificationPanel).GetMethod("ShowPathsOptionPanel",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.Invoke(__instance, null);
                    netTool.Prefab = netInfo;
                }
                return false;
            }
            if (treeInfo != null)
            {
                var prevTreeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
                TreeTool treeTool = ToolsModifierControl.SetTool<TreeTool>();
                if (treeTool != null)
                {
                    treeTool.m_prefab = treeInfo;
                    if (prevTreeTool == null)
                    {
                        treeTool.m_brush = ToolsModifierControl.toolController.m_brushes[3];
                        treeTool.m_brushSize = 30;
                        treeTool.m_mode = TreeTool.Mode.Single;
                    }
                    ShowBrushOptionsPanel(__instance);
                }
                return false;
            }
            if (propInfo != null)
            {
                var prevPropTool = ToolsModifierControl.GetCurrentTool<PropTool>();
                PropTool propTool = ToolsModifierControl.SetTool<PropTool>();
                if (propTool != null)
                {
                    propTool.m_prefab = propInfo;
                    if (prevPropTool == null)
                    {
                        propTool.m_mode = PropTool.Mode.Single;
                    }
                    ShowBrushOptionsPanel(__instance);
                }
                return false;
            }
            return false; // Skip original
        }

        private static void ShowBrushOptionsPanel(BeautificationPanel panel)
        {
            var optionsBar = typeof(GeneratedGroupPanel)
                .GetField("m_OptionsBar", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                ?.GetValue(panel) as UIComponent;

            if (optionsBar != null && m_OptionsBrushPanel == null)
            {
                m_OptionsBrushPanel = optionsBar.Find<UIPanel>("BrushPanel");
            }
            if (m_OptionsBrushPanel != null)
            {
                m_OptionsBrushPanel.zOrder = 1;
                m_OptionsBrushPanel.Show();
            }
        }
    }
}
