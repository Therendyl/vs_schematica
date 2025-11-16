using Vintagestory.API.MathTools;

namespace Schematica.Core
{
    public class SerializableBlock
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int BlockId { get; set; }
        public string BlockCode { get; set; }
        public byte[] BlockEntityData { get; set; }

        public BlockPos Position => new BlockPos(X, Y, Z);
    }
}