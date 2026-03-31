using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches TerrainPanel (map/theme editor) to add Ditch and Sand buttons.
    /// </summary>
    [HarmonyPatch(typeof(TerrainPanel))]
    public static class TerrainPanelPatch
    {
        private static readonly PositionData<TerrainTool.Mode>[] kTools = ColossalFramework.Utils.GetOrderedEnumData<TerrainTool.Mode>();

        [HarmonyPatch("OnButtonClicked")]
        [HarmonyPrefix]
        public static bool OnButtonClickedPrefix(TerrainPanel __instance, UIComponent comp)
        {
            if (ToolsModifierControl.toolController.m_mode == ItemClass.Availability.ThemeEditor)
            {
                int zOrder = comp.zOrder;
                TerrainTool terrainTool = null;
                ResourceTool resourceTool = null;

                if (zOrder < kTools.Length + 1)
                {
                    terrainTool = ToolsModifierControl.SetTool<TerrainTool>();
                    if (terrainTool == null) return false;
                    InvokeShowPanel(__instance, "ShowUndoTerrainOptionsPanel", true);
                    UIView.library.Show("LandscapingInfoPanel");
                }
                else
                {
                    resourceTool = ToolsModifierControl.SetTool<ResourceTool>();
                    if (resourceTool == null) return false;
                    UIView.library.Hide("LandscapingInfoPanel");
                    InvokeShowPanel(__instance, "ShowUndoTerrainOptionsPanel", false);
                }
                InvokeShowPanel(__instance, "ShowBrushOptionsPanel", true);

                if (zOrder == 1 || zOrder == 3)
                    InvokeShowPanel(__instance, "ShowLevelHeightPanel", true);
                else
                    InvokeShowPanel(__instance, "ShowLevelHeightPanel", false);

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
            }
            else
            {
                int zOrder = comp.zOrder;
                TerrainTool terrainTool = ToolsModifierControl.SetTool<TerrainTool>();
                if (terrainTool == null) return false;

                InvokeShowPanel(__instance, "ShowUndoTerrainOptionsPanel", true);
                InvokeShowPanel(__instance, "ShowBrushOptionsPanel", true);
                UIView.library.Show("LandscapingInfoPanel");

                if (zOrder == 1 || zOrder == 3)
                    InvokeShowPanel(__instance, "ShowLevelHeightPanel", true);
                else
                    InvokeShowPanel(__instance, "ShowLevelHeightPanel", false);

                if (zOrder < kTools.Length)
                {
                    terrainTool.m_mode = kTools[zOrder].enumValue;
                    TerrainToolPatch.IsDitch = false;
                }
                else
                {
                    terrainTool.m_mode = TerrainTool.Mode.Shift;
                    TerrainToolPatch.IsDitch = true;
                }
            }
            return false;
        }

        [HarmonyPatch("OnHideOptionBars")]
        [HarmonyPostfix]
        public static void OnHideOptionBarsPostfix(TerrainPanel __instance)
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");
            if (optionsBar != null)
            {
                optionsBar.Find<UIPanel>("BrushPanel")?.Hide();
                optionsBar.Find<UIPanel>("UndoTerrainPanel")?.Hide();
                optionsBar.Find<UIPanel>("LevelHeightPanel")?.Hide();
            }
            UIView.library.Hide("LandscapingInfoPanel");
        }

        [HarmonyPatch("RefreshPanel")]
        [HarmonyPostfix]
        public static void RefreshPanelPostfix(TerrainPanel __instance)
        {
            // Add Ditch button
            var ditchBtn = __instance.Find("TerrainDitch");
            if (ditchBtn != null) return;

            var spawnMethod = typeof(TerrainPanel).GetMethod("SpawnEntry",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(int) },
                null);

            if (spawnMethod != null)
            {
                try
                {
                    var button = spawnMethod.Invoke(__instance, new object[] { "Ditch", kTools.Length }) as UIButton;
                    if (button != null)
                    {
                        button.atlas = Util.CreateAtlasFromEmbeddedResources(
                            Assembly.GetExecutingAssembly(),
                            "CSM.ExtraLandscapingTools.Resources",
                            new List<string> { "TerrainDitch" });
                    }

                    if (ToolsModifierControl.toolController.m_mode == ItemClass.Availability.ThemeEditor)
                    {
                        var sandButton = spawnMethod.Invoke(__instance, new object[] { "Sand", kTools.Length + 1 }) as UIButton;
                        if (sandButton != null)
                        {
                            sandButton.atlas = UIView.GetAView().defaultAtlas;
                            sandButton.normalFgSprite = "ResourceSand";
                            sandButton.hoveredFgSprite = "ResourceSandHovered";
                            sandButton.pressedFgSprite = "ResourceSandPressed";
                            sandButton.focusedFgSprite = "ResourceSandFocused";
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"TerrainPanelPatch.RefreshPanelPostfix: {e.Message}");
                }
            }
        }

        private static void InvokeShowPanel(TerrainPanel panel, string methodName, bool show)
        {
            var method = typeof(TerrainPanel).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(panel, new object[] { show });
        }
    }
}
