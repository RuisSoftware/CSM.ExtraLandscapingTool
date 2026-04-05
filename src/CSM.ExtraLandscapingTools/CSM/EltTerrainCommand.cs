using ProtoBuf;
using CSM.API.Commands;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class EltTerrainCommand : CommandBase
    {
        [ProtoMember(1)]
        public int MinX { get; set; }

        [ProtoMember(2)]
        public int MinZ { get; set; }

        [ProtoMember(3)]
        public int MaxX { get; set; }

        [ProtoMember(4)]
        public int MaxZ { get; set; }

        [ProtoMember(5)]
        public ushort[] Heights { get; set; }
    }
}
