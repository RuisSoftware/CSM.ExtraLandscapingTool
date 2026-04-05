using System;
using CSM.API.Commands;
using UnityEngine;
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

        /// <summary>
        /// Called from UIInjections and handlers to sync water tool changes.
        /// </summary>
        internal static void SendWaterCommand(bool resetWater, float capacity, bool placeWater, bool moveSeaLevel)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new WaterCommand
            {
                ResetWater = resetWater,
                Capacity = capacity,
                PlaceWater = placeWater,
                MoveSeaLevel = moveSeaLevel
            };
            SendToAll(cmd);
        }

        internal static void SendResourcePaint(int[] cellData, NaturalResourceManager.Resource type, byte amount)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new ResourceCommand
            {
                CellData = cellData,
                ResourceType = type,
                Amount = amount
            };
            SendToAll(cmd);
        }

        internal static void SendWaterSource(WaterSourceAction action, int index, Vector3 pos, float level, float flow, ushort type)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new WaterSourceCommand
            {
                Action = action,
                SourceIndex = index,
                Position = pos,
                TargetWaterLevel = level,
                MaxFlow = flow,
                Type = type
            };
            SendToAll(cmd);
        }

        internal static void SendTerrainHeights(int minX, int minZ, int maxX, int maxZ, ushort[] heights)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new EltTerrainCommand
            {
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ,
                Heights = heights
            };
            SendToAll(cmd);
        }

        internal static void SendToolCursor(Vector3 pos, float size, string toolName)
        {
            if (IsIgnoring()) return;
            if (Command.SendToAll == null) return;

            var cmd = new ToolCursorCommand
            {
                MousePosition = pos,
                BrushSize = size,
                ToolName = toolName,
                PlayerID = 0 // Will be handled on receipt or we can add a local ID if available
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
