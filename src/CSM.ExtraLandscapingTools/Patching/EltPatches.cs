using HarmonyLib;

namespace CSM.ExtraLandscapingTools.Patching
{
    public static class EltPatches
    {
        private const string HarmonyId = "CSM.ExtraLandscapingTools";

        public static void PatchAll()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(EltPatches).Assembly);
            Utils.Log.Info("All Harmony patches applied.");
        }

        public static void UnpatchAll()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            Utils.Log.Info("All Harmony patches reverted.");
        }
    }
}
