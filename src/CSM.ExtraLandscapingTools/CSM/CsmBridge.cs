using System;
using CSM.API.Commands;
using CSM.API.Helpers;
using CSM.ExtraLandscapingTools.Surface;

namespace CSM.ExtraLandscapingTools.CSM
{
    internal static class CsmBridge
    {
        internal static void SendToAll(CommandBase command)
        {
            if (command == null) return;
            if (Command.SendToAll == null) return;
            Command.SendToAll.Invoke(command);
        }

        internal static bool IsIgnoring()
        {
            var helper = IgnoreHelper.Instance;
            return helper != null && helper.IsIgnored();
        }

        internal static IDisposable StartIgnore()
        {
            try
            {
                var helper = IgnoreHelper.Instance;
                if (helper == null) return DummyScope.Instance;
                helper.StartIgnore();
                return new IgnoreScope(helper);
            }
            catch { return DummyScope.Instance; }
        }

        /// <summary>
        /// Called from InGameSurfaceTool after painting cells.
        /// Batches cell data and sends as a CSM command.
        /// </summary>
        internal static void SendSurfacePaint(int[] cellData, TerrainModify.Surface surface, bool overrideExisting,
            int minX, int minZ, int maxX, int maxZ)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new SurfacePaintCommand
            {
                CellData = cellData,
                SurfaceType = SurfaceManager.GetSurfaceCode(surface),
                OverrideExisting = overrideExisting,
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ
            };
            SendToAll(cmd);
        }

        private sealed class IgnoreScope : IDisposable
        {
            private readonly IgnoreHelper _helper;
            internal IgnoreScope(IgnoreHelper helper) { _helper = helper; }
            public void Dispose() { _helper.EndIgnore(); }
        }

        private sealed class DummyScope : IDisposable
        {
            internal static readonly DummyScope Instance = new DummyScope();
            public void Dispose() { }
        }
    }
}
