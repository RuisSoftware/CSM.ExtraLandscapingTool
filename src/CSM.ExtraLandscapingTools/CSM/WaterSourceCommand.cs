using ProtoBuf;
using CSM.API.Commands;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class WaterSourceCommand : CommandBase
    {
        [ProtoMember(1)]
        public WaterSourceAction Action { get; set; }

        [ProtoMember(2)]
        public int SourceIndex { get; set; } // -1 if new

        [ProtoMember(3)]
        public Vector3 Position { get; set; }

        [ProtoMember(4)]
        public float TargetWaterLevel { get; set; }

        [ProtoMember(5)]
        public float MaxFlow { get; set; }

        [ProtoMember(6)]
        public ushort Type { get; set; } // enum WaterSource.Type (m_type in WaterSource struct)
    }

    public enum WaterSourceAction
    {
        Create,
        Update,
        Delete
    }
}
