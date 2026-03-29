using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Utils;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches WaterPanel to add Map Editor water tools (Water Source, Sea Level) to the in-game UI.
    /// </summary>
    [HarmonyPatch(typeof(WaterPanel))]
    public static class WaterPanelPatch
    {
        private static UIPanel m_OptionsWaterPanel;

        [HarmonyPatch("RefreshPanel")]
        [HarmonyPostfix]
        public static void RefreshPanelPostfix(WaterPanel __instance)
        {
            Log.Info("WaterPanel.RefreshPanelPostfix called.");
            var spawnMethod = typeof(WaterPanel).GetMethod("SpawnEntry",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(UITextureAtlas), typeof(UIComponent), typeof(bool) },
                null);

            if (spawnMethod == null)
            {
                Log.Warn("WaterPanel.RefreshPanelPostfix: SpawnEntry method not found.");
                return;
            }

            // Only add if not already present
            if (__instance.Find("PlaceWater") != null) return;

            var atlas = Util.CreateAtlasFromResources(new List<string> { "WaterPlaceWater", "WaterMoveSeaLevel" });
            if (atlas == null)
            {
                Log.Warn("WaterPanel.RefreshPanelPostfix: Failed to create atlas.");
            }

            // Note: In-game we use custom strings, or we could use MAPEDITOR_WATER_PLACE/MAPEDITOR_WATER_MOVESEA if available.
            string placeWaterTooltip = "Water Creator Tool";
            string moveSeaLevelTooltip = "Sea Level Editor Tool";

            var tooltipBox = GeneratedPanel.landscapingTooltipBox;

            try
            {
                var placeBtn = spawnMethod.Invoke(__instance, new object[] {
                    "PlaceWater", placeWaterTooltip, "WaterPlaceWater", atlas,
                    tooltipBox, true
                }) as UIButton;
                if (placeBtn != null) Log.Info("Added PlaceWater button.");

                var seaLevelBtn = spawnMethod.Invoke(__instance, new object[] {
                    "MoveSeaLevel", moveSeaLevelTooltip, "WaterMoveSeaLevel", atlas,
                    tooltipBox, true
                }) as UIButton;
                if (seaLevelBtn != null) Log.Info("Added MoveSeaLevel button.");
            }
            catch (System.Exception e)
            {
                Log.Error($"WaterPanelPatch.RefreshPanelPostfix error: {e.Message}");
            }
        }

        [HarmonyPatch("OnButtonClicked")]
        [HarmonyPrefix]
        public static bool OnButtonClickedPrefix(WaterPanel __instance, UIComponent comp)
        {
            if (comp.name == "PlaceWater" || comp.name == "MoveSeaLevel")
            {
                WaterTool waterTool = ToolsModifierControl.SetTool<WaterTool>();
                if (waterTool == null) return false;

                if (comp.name == "PlaceWater")
                {
                    Util.SetPrivate(waterTool, "m_PlaceWater", true);
                    Util.SetPrivate(waterTool, "m_MoveSeaLevel", false);
                }
                else
                {
                    Util.SetPrivate(waterTool, "m_PlaceWater", false);
                    Util.SetPrivate(waterTool, "m_MoveSeaLevel", true);
                }

                ShowWaterOptionsPanel(__instance);
                UIView.library.Show("WaterInfoPanel");
                return false;
            }

            return true; // Let original handle other buttons (if any)
        }

        private static void ShowWaterOptionsPanel(WaterPanel panel)
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");

            if (optionsBar != null && m_OptionsWaterPanel == null)
            {
                m_OptionsWaterPanel = optionsBar.Find<UIPanel>("WaterPanel");
            }
            if (m_OptionsWaterPanel != null)
            {
                m_OptionsWaterPanel.Show();
            }
        }
    }
}
