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
        public static void RefreshPanelPostfix(GeneratedGroupPanel __instance)
        {
            if (__instance == null || __instance.name != "Water")
                return;

            Log.Info($"WaterPanelPatch.RefreshPanelPostfix called for {__instance.GetType().Name}.");
            AddWaterButtons(__instance);
        }

        private static void AddWaterButtons(GeneratedGroupPanel __instance)
        {
            // Debug: Log all methods named SpawnEntry
            var methods = __instance.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var m in methods)
            {
                if (m.Name == "SpawnEntry")
                {
                    string args = "";
                    foreach (var p in m.GetParameters()) args += p.ParameterType.Name + ", ";
                    Log.Info($"Found SpawnEntry on {__instance.GetType().Name}: ({args})");
                }
            }

            // Try the complex signature first (from LandscapingPanelPatch)
            var spawnMethod = __instance.GetType().GetMethod("SpawnEntry",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(UITextureAtlas), typeof(UIComponent), typeof(bool) },
                null);

            if (spawnMethod == null)
            {
                // Try the simple signature (from TerrainPanelPatch)
                spawnMethod = __instance.GetType().GetMethod("SpawnEntry",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);
            }

            if (spawnMethod == null)
            {
                Log.Warn($"WaterPanel ({__instance.GetType().Name}): No known SpawnEntry signature found.");
                return;
            }

            // Only add if not already present
            if (__instance.Find("PlaceWater") != null)
            {
                return;
            }

            var atlas = Util.CreateAtlasFromResources(new List<string> { "WaterPlaceWater", "WaterMoveSeaLevel" });
            if (atlas == null)
            {
                Log.Warn("WaterPanel: Failed to create atlas.");
            }

            string placeWaterTooltip = "Water Creator Tool";
            string moveSeaLevelTooltip = "Sea Level Editor Tool";

            var tooltipBox = GeneratedPanel.landscapingTooltipBox;

            try
            {
                if (spawnMethod.GetParameters().Length > 2)
                {
                    var placeBtn = spawnMethod.Invoke(__instance, new object[] {
                        "PlaceWater", placeWaterTooltip, "WaterPlaceWater", atlas,
                        tooltipBox, true
                    }) as UIButton;
                    if (placeBtn != null) Log.Info("Added PlaceWater button (complex).");

                    var seaLevelBtn = spawnMethod.Invoke(__instance, new object[] {
                        "MoveSeaLevel", moveSeaLevelTooltip, "WaterMoveSeaLevel", atlas,
                        tooltipBox, true
                    }) as UIButton;
                    if (seaLevelBtn != null) Log.Info("Added MoveSeaLevel button (complex).");
                }
                else
                {
                    var placeBtn = spawnMethod.Invoke(__instance, new object[] { "PlaceWater", 0 }) as UIButton;
                    if (placeBtn != null)
                    {
                        placeBtn.atlas = atlas;
                        placeBtn.tooltip = placeWaterTooltip;
                        placeBtn.normalFgSprite = "WaterPlaceWater";
                        placeBtn.hoveredFgSprite = "WaterPlaceWaterHovered";
                        placeBtn.pressedFgSprite = "WaterPlaceWaterPressed";
                        placeBtn.disabledFgSprite = "WaterPlaceWaterDisabled";
                        placeBtn.focusedFgSprite = "WaterPlaceWaterFocused";
                        Log.Info("Added PlaceWater button (simple).");
                    }

                    var seaLevelBtn = spawnMethod.Invoke(__instance, new object[] { "MoveSeaLevel", 1 }) as UIButton;
                    if (seaLevelBtn != null)
                    {
                        seaLevelBtn.atlas = atlas;
                        seaLevelBtn.tooltip = moveSeaLevelTooltip;
                        seaLevelBtn.normalFgSprite = "WaterMoveSeaLevel";
                        seaLevelBtn.hoveredFgSprite = "WaterMoveSeaLevelHovered";
                        seaLevelBtn.pressedFgSprite = "WaterMoveSeaLevelPressed";
                        seaLevelBtn.disabledFgSprite = "WaterMoveSeaLevelDisabled";
                        seaLevelBtn.focusedFgSprite = "WaterMoveSeaLevelFocused";
                        Log.Info("Added MoveSeaLevel button (simple).");
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"WaterPanel.AddWaterButtons error: {e.Message}");
            }
        }

        [HarmonyPatch("OnButtonClicked")]
        [HarmonyPrefix]
        public static bool OnButtonClickedPrefix(GeneratedGroupPanel __instance, UIComponent comp)
        {
            if (__instance == null || __instance.name != "Water")
                return true;

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

                var optionsBar = UIView.Find<UIPanel>("OptionsBar");
                if (optionsBar != null)
                {
                    var brushPanel = optionsBar.Find<UIPanel>("BrushPanel");
                    brushPanel?.Hide();
                }

                ShowWaterOptionsPanel(__instance);
                UIView.library.Show("WaterInfoPanel");
                return false;
            }

            return true; // Let original handle other buttons (if any)
        }

        private static void ShowWaterOptionsPanel(GeneratedGroupPanel panel)
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
