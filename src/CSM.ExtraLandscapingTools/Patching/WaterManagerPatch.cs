using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using CSM.ExtraLandscapingTools.CSM;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    [HarmonyPatch]
    public static class WaterSimulationPatch
    {
        private static System.Type GetSimType()
        {
            // Look for WaterSimulation in TerrainManager (as seen in source) or WaterManager
            var tm = Singleton<TerrainManager>.instance;
            if (tm != null)
            {
                var field = tm.GetType().GetField("m_waterSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field.FieldType;
                
                // Try property
                var prop = tm.GetType().GetProperty("WaterSimulation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (prop != null) return prop.PropertyType;
            }

            var wm = Singleton<WaterManager>.instance;
            if (wm != null)
            {
                var field = wm.GetType().GetField("m_waterSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field.FieldType;
            }
            
            return Util.FindType("WaterSimulation");
        }

        [HarmonyPatch]
        public static class CreateWaterSourcePatch
        {
            [HarmonyTargetMethod]
            public static System.Reflection.MethodBase TargetMethod()
            {
                return GetSimType()?.GetMethod("CreateWaterSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            [HarmonyPostfix]
            public static void Postfix(bool __result, ref ushort source, object sourceData)
            {
                if (CsmBridge.IsIgnoring()) return;
                if (!__result) return;
                
                Log.Info($"CreateWaterSource Postfix: index={source}");

                try {
                    // Extract fields from WaterSource struct (sourceData)
                    Vector3 inputPos = Util.GetPrivate<Vector3>(sourceData, "m_inputPosition");
                    ushort target = Util.GetPrivate<ushort>(sourceData, "m_target");
                    uint inputRate = Util.GetPrivate<uint>(sourceData, "m_inputRate");
                    ushort type = Util.GetPrivate<ushort>(sourceData, "m_type");

                    float targetWaterLevel = (float)target * (1f / 64f);
                    float maxFlow = (float)inputRate / 65535f;

                    CsmBridge.SendWaterSource(WaterSourceAction.Create, source, inputPos, targetWaterLevel, maxFlow, type);
                } catch (System.Exception ex) {
                    Log.Error($"Error parsing WaterSource struct: {ex.Message}");
                }
            }
        }

        [HarmonyPatch]
        public static class ReleaseWaterSourcePatch
        {
            [HarmonyTargetMethod]
            public static System.Reflection.MethodBase TargetMethod()
            {
                return GetSimType()?.GetMethod("ReleaseWaterSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            [HarmonyPrefix]
            public static void Prefix(ushort source)
            {
                if (CsmBridge.IsIgnoring()) return;
                CsmBridge.SendWaterSource(WaterSourceAction.Delete, source, Vector3.zero, 0f, 0f, 0);
            }
        }
    }
}
