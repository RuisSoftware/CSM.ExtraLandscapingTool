using CSM.API.Commands;
using CSM.ExtraLandscapingTools.Utils;
using ColossalFramework;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class WaterHandler : CommandHandler<WaterCommand>
    {
        protected override void Handle(WaterCommand command)
        {
            Log.Info($"Received WaterCommand: Reset={command.ResetWater}, Capacity={command.Capacity}, Mode={command.PlaceWater}/{command.MoveSeaLevel}");

            using (CsmBridge.StartIgnore())
            {
                if (command.ResetWater)
                {
                    Singleton<TerrainManager>.instance.WaterSimulation.m_resetWater = true;
                    return;
                }

                var waterTool = ToolsModifierControl.GetTool<WaterTool>();
                if (waterTool != null)
                {
                    Util.SetPrivate(waterTool, "m_capacity", command.Capacity);
                    Util.SetPrivate(waterTool, "m_PlaceWater", command.PlaceWater);
                    Util.SetPrivate(waterTool, "m_MoveSeaLevel", command.MoveSeaLevel);
                    
                    // Refresh tool if it is active
                    if (waterTool.enabled)
                    {
                        waterTool.enabled = false;
                        waterTool.enabled = true;
                    }
                }
            }
        }
    }
}
