using CSM.API.Commands;
using UnityEngine;
using System.Collections.Generic;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class ToolCursorHandler : CommandHandler<ToolCursorCommand>
    {
        // Keep track of remote cursors by sender ID
        public static readonly Dictionary<int, RemoteCursorData> RemoteCursors = new Dictionary<int, RemoteCursorData>();

        protected override void Handle(ToolCursorCommand command)
        {
            RemoteCursors[command.PlayerID] = new RemoteCursorData {
                Position = command.MousePosition,
                BrushSize = command.BrushSize,
                ToolName = command.ToolName,
                LastUpdate = Time.time
            };
        }
    }

    public class RemoteCursorData
    {
        public Vector3 Position;
        public float BrushSize;
        public string ToolName;
        public float LastUpdate;
    }
}
