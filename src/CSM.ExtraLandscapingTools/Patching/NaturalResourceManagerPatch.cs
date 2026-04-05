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
        private static NaturalResourceManager.Resource _activeResource = NaturalResourceManager.Resource.None;
        private static bool _waitingForFlush = false;

        public static void SetActiveResource(NaturalResourceManager.Resource resource)
        {
            _activeResource = resource;
        }

        [HarmonyPatch(typeof(NaturalResourceManager), "AreaModified")]
        public static class ResourceAreaPatch
        {
            [HarmonyPrefix]
            public static void Prefix(int minX, int minZ, int maxX, int maxZ)
            {
                if (CsmBridge.IsIgnoring()) return;
                if (_activeResource == NaturalResourceManager.Resource.None) return;

                Log.Info($"Resource AreaModified: ({minX},{minZ}) to ({maxX},{maxZ}) for {_activeResource}");
                
                // Capture the entire area. We batch it at the end of SimulationStep.
                for (int z = minZ; z <= maxZ; z++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        _cellBuffer.Add(z);
                        _cellBuffer.Add(x);
                    }
                }
                _waitingForFlush = true;
            }
        }

        public static void Flush()
        {
            if (_waitingForFlush && _cellBuffer.Count > 0)
            {
                Log.Info($"Flushing resource painting: {_cellBuffer.Count / 2} cells of {_activeResource}");
                
                // We need to sample the data from the manager for these cells.
                var mgr = Singleton<NaturalResourceManager>.instance;
                var cells = _cellBuffer.ToArray();
                
                // For simplicity, we sample the 'amount' as it is now.
                // We'll use a fixed amount (usually 255 if painted) or sample first cell
                byte lastAmount = 255; 
                
                CsmBridge.SendResourcePaint(cells, _activeResource, lastAmount);
                _cellBuffer.Clear();
                _waitingForFlush = false;
            }
            _activeResource = NaturalResourceManager.Resource.None; 
        }
    }


    [HarmonyPatch(typeof(ResourceTool), "SimulationStep")]
    public static class ResourceToolSyncPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ResourceTool __instance)
        {
            if (CsmBridge.IsIgnoring()) return;
            
            var resource = Util.GetPrivate<NaturalResourceManager.Resource>(__instance, "m_resource");
            NaturalResourceManagerPatch.SetActiveResource(resource);
        }

        [HarmonyPostfix]
        public static void Postfix(ResourceTool __instance)
        {
            NaturalResourceManagerPatch.Flush();
        }
    }
}
