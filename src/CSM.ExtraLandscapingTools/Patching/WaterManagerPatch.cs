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
            var typesToSearch = new[] { typeof(TerrainManager), typeof(WaterManager) };
            foreach (var t in typesToSearch)
            {
                // Check all members (fields, properties) for "WaterSimulation"
                var members = t.GetMember("WaterSimulation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                foreach (var m in members)
                {
                    if (m is System.Reflection.FieldInfo fi) return fi.FieldType;
                    if (m is System.Reflection.PropertyInfo pi) return pi.PropertyType;
                }
            }

            // Fallback: search all assemblies for a type named "WaterSimulation" or similar
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                // We use Try/Catch to avoid issues with dynamic or unreadable assemblies
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("WaterSimulation")) return type;
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
                if (!__result) return;

                // New Signature likely: bool CreateWaterSource(ushort index, Vector3 inputPosition, Vector3 outputPosition, ushort type, ushort target, uint inputRate, ...)
                if (__args.Length >= 6)
                {
                    ushort index = (ushort)__args[0];
                    Vector3 pos = (Vector3)__args[1];
                    ushort type = (ushort)__args[3];
                    ushort target = (ushort)__args[4];
                    uint inputRate = (uint)__args[5];
                    
                    float targetWaterLevel = (float)target * (1f / 64f);
                    float maxFlow = (float)inputRate / 65535f;

                    CsmBridge.SendWaterSource(WaterSourceAction.Create, index, pos, targetWaterLevel, maxFlow, type);
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
