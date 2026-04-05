using CSM.API.Commands;
using CSM.ExtraLandscapingTools.Utils;
using ColossalFramework;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class EltTerrainHandler : CommandHandler<EltTerrainCommand>
    {
        protected override void Handle(EltTerrainCommand command)
        {
            Log.Info($"Received EltTerrainCommand: area ({command.MinX},{command.MinZ})-({command.MaxX},{command.MaxZ})");

            using (CsmBridge.StartIgnore())
            {
                ushort[] rawHeights = Singleton<TerrainManager>.instance.RawHeights;
                int idx = 0;
                for (int z = command.MinZ; z <= command.MaxZ; z++)
                {
                    for (int x = command.MinX; x <= command.MaxX; x++)
                    {
                        rawHeights[z * 1081 + x] = command.Heights[idx++];
                    }
                }

                TerrainModify.UpdateArea(command.MinX, command.MinZ, command.MaxX, command.MaxZ, true, true, false);
            }
        }
    }
}
