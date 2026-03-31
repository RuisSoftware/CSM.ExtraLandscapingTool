using ProtoBuf;
using CSM.API.Commands;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class WaterCommand : CommandBase
    {
        [ProtoMember(1)]
        public bool ResetWater { get; set; }

        [ProtoMember(2)]
        public float Capacity { get; set; }

        [ProtoMember(3)]
        public bool PlaceWater { get; set; }

        [ProtoMember(4)]
        public bool MoveSeaLevel { get; set; }
    }
}
