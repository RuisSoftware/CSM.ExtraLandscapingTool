using HarmonyLib;
using UnityEngine;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches WaterTool.Awake to set up the water level/source materials properly.
    /// The original mod used redirect-revert-redirect pattern; with Harmony we just
    /// set the materials in a postfix.
    /// </summary>
    [HarmonyPatch(typeof(WaterTool), "Awake")]
    public static class WaterToolPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(WaterTool __instance)
        {
            try
            {
                var levelMaterial = new Material(Shader.Find("Custom/Overlay/WaterLevel"));
                Util.SetPrivate(__instance, "m_levelMaterial", levelMaterial);

                var sourceMaterial = new Material(Shader.Find("Custom/Tools/WaterSource"));
                sourceMaterial.color = new Color(48.0f / 255.0f, 140.0f / 255.0f, 1.0f, 54.0f / 255.0f);
                Util.SetPrivate(__instance, "m_sourceMaterial", sourceMaterial);

                var mesh = Util.Load<Mesh>("Cylinder01");
                if (mesh != null)
                {
                    Util.SetPrivate(__instance, "m_sourceMesh", mesh);
                }
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"WaterToolPatch.Prefix: {e.Message}");
                return true;
            }
        }
    }
}
