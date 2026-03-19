using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Schematica.Core
{
    public class SerializableBlock
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int BlockId { get; set; }
        public string BlockCode { get; set; } = string.Empty;

        [JsonIgnore]
        public ReadOnlyMemory<byte> BlockEntityData
        {
            get => blockEntityData;
            set => blockEntityData = value.IsEmpty ? Array.Empty<byte>() : value.ToArray();
        }

        [JsonProperty(nameof(BlockEntityData))]
        private byte[] JsonBlockEntityData
        {
            get => blockEntityData;
            set => blockEntityData = value ?? Array.Empty<byte>();
        }

        private byte[] blockEntityData = Array.Empty<byte>();

        public BlockPos Position => new BlockPos(X, Y, Z);
    }
}



