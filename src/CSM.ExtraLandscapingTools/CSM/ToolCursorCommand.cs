using ProtoBuf;
using CSM.API.Commands;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class ToolCursorCommand : CommandBase
    {
        [ProtoMember(1)]
        public Vector3 MousePosition { get; set; }

        [ProtoMember(2)]
        public float BrushSize { get; set; }

        [ProtoMember(3)]
        public string ToolName { get; set; }

        [ProtoMember(4)]
        public int PlayerID { get; set; }
    }
}
