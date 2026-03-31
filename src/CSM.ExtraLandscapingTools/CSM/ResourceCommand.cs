using ProtoBuf;
using CSM.API.Commands;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class ResourceCommand : CommandBase
    {
        [ProtoMember(1)]
        public NaturalResourceManager.Resource ResourceType { get; set; }

        [ProtoMember(2)]
        public int[] CellData { get; set; } // [z, x] pairs

        [ProtoMember(3)]
        public byte Amount { get; set; }

        [ProtoMember(4)]
        public bool IsNegate { get; set; }
    }
}
