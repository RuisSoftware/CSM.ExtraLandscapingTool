using ColossalFramework.UI;
using HarmonyLib;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches LevelHeightOptionPanel to read/write TerrainToolPatch.StartPosition.y
    /// instead of the original TerrainTool internal field.
    /// </summary>
    [HarmonyPatch(typeof(LevelHeightOptionPanel))]
    public static class LevelHeightOptionPanelPatch
    {
        private static UISlider m_HeightSlider;

        [HarmonyPatch("SetHeight")]
        [HarmonyPrefix]
        public static bool SetHeightPrefix(float height)
        {
            TerrainToolPatch.StartPosition = new UnityEngine.Vector3(
                TerrainToolPatch.StartPosition.x,
                height,
                TerrainToolPatch.StartPosition.z);
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool UpdatePrefix(LevelHeightOptionPanel __instance)
        {
            if (!__instance.component.isVisible)
                return false;
            if (m_HeightSlider == null)
                m_HeightSlider = __instance.Find<UISlider>("Height");
            if (m_HeightSlider != null)
                m_HeightSlider.value = TerrainToolPatch.StartPosition.y;
            return false;
        }
    }

    /// <summary>
    /// Patches UndoTerrainOptionPanel.UndoTerrain to use the new undo system.
    /// </summary>
    [HarmonyPatch(typeof(UndoTerrainOptionPanel), "UndoTerrain")]
    public static class UndoTerrainOptionPanelPatch
    {
        public static bool Prefix()
        {
            var terrainTool = ToolsModifierControl.GetTool<TerrainTool>();
            if (terrainTool == null || !TerrainToolPatch.IsUndoAvailable())
                return false;
            TerrainToolPatch.RequestUndo();
            return false;
        }
    }
}
