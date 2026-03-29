using ProtoBuf;
using CSM.API.Commands;

namespace CSM.ExtraLandscapingTools.CSM
{
    [ProtoContract]
    public class SurfacePaintCommand : CommandBase
    {
        /// <summary>
        /// Packed cell coordinates: [z0, x0, z1, x1, ...] pairs
        /// </summary>
        [ProtoMember(2)]
        public int[] CellData { get; set; }

        /// <summary>
        /// Encoded surface type (0=None, 1=PavementA, etc.)
        /// </summary>
        [ProtoMember(3)]
        public byte SurfaceType { get; set; }

        /// <summary>
        /// Whether to override existing surfaces
        /// </summary>
        [ProtoMember(4)]
        public bool OverrideExisting { get; set; }

        /// <summary>
        /// Terrain update area bounds
        /// </summary>
        [ProtoMember(5)]
        public int MinX { get; set; }

        [ProtoMember(6)]
        public int MinZ { get; set; }

        [ProtoMember(7)]
        public int MaxX { get; set; }

        [ProtoMember(8)]
        public int MaxZ { get; set; }
    }
}
