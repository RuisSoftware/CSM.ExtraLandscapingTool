using ICities;
using CitiesHarmony.API;
using CSM.ExtraLandscapingTools.Patching;
using CSM.ExtraLandscapingTools.Surface;

namespace CSM.ExtraLandscapingTools.Mod
{
    public class MyUserMod : IUserMod
    {
        public string Name => ModMetadata.ModName + " " + ModMetadata.Version;
        public string Description => ModMetadata.Description;

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => EltPatches.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                EltPatches.UnpatchAll();
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            var group = helper.AddGroup("Extra Landscaping Tools");
            group.AddCheckbox("Resource Tool", EltOptions.ResourceTool, val => EltOptions.ResourceTool = val);
            group.AddCheckbox("Water Tool", EltOptions.WaterTool, val => EltOptions.WaterTool = val);
            group.AddCheckbox("Terrain Tool", EltOptions.TerrainTool, val => EltOptions.TerrainTool = val);
            group.AddCheckbox("Tree Brush", EltOptions.TreeBrush, val => EltOptions.TreeBrush = val);
            group.AddCheckbox("Tree Pencil (drag placement)", EltOptions.TreePencil, val => EltOptions.TreePencil = val);
            group.AddCheckbox("Terrain Topography", EltOptions.TerrainTopography, val => EltOptions.TerrainTopography = val);

            var surfaceGroup = helper.AddGroup("Surface Painter");
            surfaceGroup.AddButton("Update Whole Map", () => SurfaceManager.UpdateWholeMap());
        }
    }
}
