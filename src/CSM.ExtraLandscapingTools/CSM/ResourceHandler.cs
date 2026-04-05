using CSM.API.Commands;
using CSM.ExtraLandscapingTools.Utils;
using ColossalFramework;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class ResourceHandler : CommandHandler<ResourceCommand>
    {
        protected override void Handle(ResourceCommand command)
        {
            if (command.CellData == null || command.CellData.Length < 2)
                return;

            Log.Info($"Received resource painting: {command.ResourceType}, {command.CellData.Length / 2} cells");

            using (CsmBridge.StartIgnore())
            {
                var mgr = Singleton<NaturalResourceManager>.instance;
                if (mgr == null) return;

                var resources = mgr.m_naturalResources;
                if (resources == null) return;

                // ResourceCell is a nested struct
                var structType = typeof(NaturalResourceManager).GetNestedType("ResourceCell", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (structType == null) return;

                string fieldName = "m_" + command.ResourceType.ToString().ToLower();
                
                var field = structType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field == null)
                {
                    Log.Error($"Could not find field {fieldName} in ResourceCell struct!");
                    return;
                }

                for (int i = 0; i < command.CellData.Length - 1; i += 2)
                {
                    int z = command.CellData[i];
                    int x = command.CellData[i + 1];

                    // Log identifying first few cells
                    if (i == 0) Log.Info($"Updating cells for {command.ResourceType}: starting at ({x},{z})");

                    if (x < 0 || x >= 512 || z < 0 || z >= 512)
                        continue;

                    int index = z * 512 + x;

                    if (index < resources.Length)
                    {
                        // Since ResourceCell is a struct, we must box, modify, and re-assign.
                        object cell = resources[index];
                        field.SetValue(cell, command.Amount);
                        
                        // We must cast back to the internal struct type.
                        // Reflection can be used to set the array element if the type is unknown at compile time.
                        resources.SetValue(cell, index);
                    }
                }

                mgr.AreaModified(0, 0, 511, 511);
            }
        }
    }
}
