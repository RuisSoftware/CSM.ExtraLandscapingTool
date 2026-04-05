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
            var field = typeof(WaterManager).GetField("m_waterSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) return field.FieldType;

            // Fallback
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "WaterSimulation") return type;
                    }
                }
                catch { }
            }

            return null;
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
            public static void Postfix(bool __result, object[] __args)
            {
                if (CsmBridge.IsIgnoring()) return;
                Log.Info($"CreateWaterSource Postfix: result={__result}, args={(__args != null ? __args.Length.ToString() : "null")}");
                if (!__result) return;

                // New Signature likely: bool CreateWaterSource(ushort index, Vector3 inputPosition, Vector3 outputPosition, ushort type, ushort target, uint inputRate, ...)
                if (__args != null && __args.Length >= 6)
                {
                    try {
                        ushort index = (ushort)__args[0];
                        Vector3 pos = (Vector3)__args[1];
                        ushort type = (ushort)__args[3];
                        ushort target = (ushort)__args[4];
                        uint inputRate = (uint)__args[5];
                        Log.Info($"Captured WaterSource: index={index}, pos={pos}, target={target}, rate={inputRate}");
                        
                        float targetWaterLevel = (float)target * (1f / 64f);
                        float maxFlow = (float)inputRate / 65535f;

                        CsmBridge.SendWaterSource(WaterSourceAction.Create, index, pos, targetWaterLevel, maxFlow, type);
                    } catch (System.Exception ex) {
                        Log.Error($"Error parsing CreateWaterSource args: {ex.Message}");
                    }
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
