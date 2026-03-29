using CSM.API.Commands;
using CSM.ExtraLandscapingTools.Surface;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class SurfacePaintHandler : CommandHandler<SurfacePaintCommand>
    {
        protected override void Handle(SurfacePaintCommand command)
        {
            if (command.CellData == null || command.CellData.Length < 2)
                return;

            var surface = SurfaceManager.GetSurface(command.SurfaceType);

            Log.Info($"Received surface paint: {command.CellData.Length / 2} cells, surface={surface}");

            using (CsmBridge.StartIgnore())
            {
                // Apply all cell changes
                for (int i = 0; i < command.CellData.Length - 1; i += 2)
                {
                    int z = command.CellData[i];
                    int x = command.CellData[i + 1];
                    SurfaceManager.instance.SetSurfaceItem(z, x, surface, command.OverrideExisting);
                }

                // Update terrain rendering
                TerrainModify.UpdateArea(
                    command.MinX / 4 - 1, command.MinZ / 4 - 1,
                    command.MaxX / 4 + 1, command.MaxZ / 4 + 1,
                    false, true, false);
            }
        }
    }
}
