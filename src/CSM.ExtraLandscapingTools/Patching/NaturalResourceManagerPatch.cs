using System.Collections.Generic;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using CSM.ExtraLandscapingTools.CSM;
using CSM.ExtraLandscapingTools.Mod;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Patching
{
    [HarmonyPatch]
    public static class NaturalResourceManagerPatch
    {
        private static List<int> _cellBuffer = new List<int>();
        private static NaturalResourceManager.Resource _activeResource;
        private static byte _lastAmount;
        private static bool _buffering = false;

        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            var type = typeof(NaturalResourceManager);
            // Looking for: private int CountResource(Resource resource, Vector3 position, float radius, int cellDelta, out int numCells, out int totalCells, out int resultDelta, bool refresh)
            var method = type.GetMethod("CountResource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
                new[] { typeof(NaturalResourceManager.Resource), typeof(Vector3), typeof(float), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(bool) }, null);
            
            if (method == null)
            {
                Log.Error("Could not find NaturalResourceManager.CountResource overloading with 8 parameters!");
            }
            return method;
        }

        [HarmonyPrefix]
        public static void Prefix(NaturalResourceManager.Resource resource, Vector3 position, float radius, int cellDelta)
        {
            if (CsmBridge.IsIgnoring()) return;
            if (cellDelta == 0) return; // Not a modification call

            if (!_buffering)
            {
                _buffering = true;
                _activeResource = resource;
                _lastAmount = (byte)Mathf.Clamp(Mathf.Abs(cellDelta >> 20), 0, 255);
                _cellBuffer.Clear();
            }
        }

        public static void AddCells(int minX, int minZ, int maxX, int maxZ)
        {
            if (!_buffering) return;

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    _cellBuffer.Add(z);
                    _cellBuffer.Add(x);
                }
            }
        }

        public static void Flush()
        {
            if (_cellBuffer.Count > 0)
            {
                CsmBridge.SendResourcePaint(_cellBuffer.ToArray(), _activeResource, _lastAmount);
                _cellBuffer.Clear();
            }
            _buffering = false;
        }
    }

    [HarmonyPatch(typeof(NaturalResourceManager), "AreaModified")]
    public static class ResourceAreaPatch
    {
        [HarmonyPrefix]
        public static void Prefix(int minX, int minZ, int maxX, int maxZ)
        {
            if (CsmBridge.IsIgnoring()) return;
            NaturalResourceManagerPatch.AddCells(minX, minZ, maxX, maxZ);
        }
    }

    [HarmonyPatch(typeof(ResourceTool), "SimulationStep")]
    public static class ResourceToolSyncPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            NaturalResourceManagerPatch.Flush();
        }
    }
}
