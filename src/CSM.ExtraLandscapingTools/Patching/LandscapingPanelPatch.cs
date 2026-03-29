using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches LandscapingPanel to add Ditch and Sand buttons (in-game mode).
    /// </summary>
    [HarmonyPatch(typeof(LandscapingPanel))]
    public static class LandscapingPanelPatch
    {
        private static readonly PositionData<TerrainTool.Mode>[] kTools = ColossalFramework.Utils.GetOrderedEnumData<TerrainTool.Mode>();
        private static UIPanel m_OptionsUndoTerrainPanel;
        private static UIPanel m_OptionsBrushPanel;
        private static UIPanel m_OptionsLevelHeightPanel;

        [HarmonyPatch("RefreshPanel")]
        [HarmonyPostfix]
        public static void RefreshPanelPostfix(LandscapingPanel __instance)
        {
            // Spawn Ditch and Sand buttons after the standard terrain mode buttons
            var spawnMethod = typeof(LandscapingPanel).GetMethod("SpawnEntry",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(UITextureAtlas), typeof(UIComponent), typeof(bool) },
                null);

            if (spawnMethod == null) return;

            // Only add if not already present
            var ditchBtn = __instance.Find("TerrainDitch");
            if (ditchBtn != null) return;

            // Create Ditch entry
            var atlas = Util.CreateAtlasFromEmbeddedResources(
                Assembly.GetExecutingAssembly(),
                "CSM.ExtraLandscapingTools.Resources",
                new List<string> { "TerrainDitch" });

            var landscapingInfo = typeof(LandscapingPanel)
                .GetProperty("landscapingInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(__instance, null);

            string ditchStr = TooltipHelper.Format(
                LocaleFormatter.Title, "Ditch",
                LocaleFormatter.Sprite, "Ditch");

            try
            {
                var ditchButton = spawnMethod.Invoke(__instance, new object[] {
                    "Ditch", ditchStr, "TerrainDitch", atlas,
                    GeneratedPanel.landscapingTooltipBox, true
                }) as UIButton;
                if (ditchButton != null && landscapingInfo != null)
                    ditchButton.objectUserData = landscapingInfo;
            }
            catch { /* SpawnEntry signature mismatch, skip */ }
        }

        [HarmonyPatch("OnButtonClicked")]
        [HarmonyPrefix]
        public static bool OnButtonClickedPrefix(LandscapingPanel __instance, UIComponent comp)
        {
            InitializePanels();

            int zOrder = comp.zOrder;
            TerrainTool terrainTool = null;
            ResourceTool resourceTool = null;

            if (zOrder < kTools.Length + 1)
            {
                terrainTool = ToolsModifierControl.SetTool<TerrainTool>();
                if (terrainTool == null) return false;
                ShowPanel(m_OptionsUndoTerrainPanel, true);
                UIView.library.Show("LandscapingInfoPanel");
            }
            else
            {
                resourceTool = ToolsModifierControl.SetTool<ResourceTool>();
                if (resourceTool == null) return false;
                UIView.library.Hide("LandscapingInfoPanel");
                ShowPanel(m_OptionsUndoTerrainPanel, false);
            }
            ShowPanel(m_OptionsBrushPanel, true);

            if (zOrder == 1 || zOrder == 3)
                ShowPanel(m_OptionsLevelHeightPanel, true);
            else
                ShowPanel(m_OptionsLevelHeightPanel, false);

            if (zOrder < kTools.Length)
            {
                terrainTool.m_mode = kTools[zOrder].enumValue;
                TerrainToolPatch.IsDitch = false;
            }
            else if (zOrder < kTools.Length + 1)
            {
                terrainTool.m_mode = TerrainTool.Mode.Shift;
                TerrainToolPatch.IsDitch = true;
            }
            else
            {
                resourceTool.m_resource = NaturalResourceManager.Resource.Sand;
            }
            return false;
        }

        [HarmonyPatch("OnHideOptionBars")]
        [HarmonyPrefix]
        public static bool OnHideOptionBarsPrefix()
        {
            InitializePanels();
            ShowPanel(m_OptionsBrushPanel, false);
            ShowPanel(m_OptionsUndoTerrainPanel, false);
            ShowPanel(m_OptionsLevelHeightPanel, false);
            UIView.library.Hide("LandscapingInfoPanel");
            return false;
        }

        private static void InitializePanels()
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");
            if (optionsBar == null) return;
            if (m_OptionsBrushPanel == null)
                m_OptionsBrushPanel = optionsBar.Find<UIPanel>("BrushPanel");
            if (m_OptionsLevelHeightPanel == null)
                m_OptionsLevelHeightPanel = optionsBar.Find<UIPanel>("LevelHeightPanel");
            if (m_OptionsUndoTerrainPanel == null)
                m_OptionsUndoTerrainPanel = optionsBar.Find<UIPanel>("UndoTerrainPanel");
        }

        private static void ShowPanel(UIPanel panel, bool show)
        {
            if (panel == null) return;
            panel.isVisible = show;
        }
    }
}
