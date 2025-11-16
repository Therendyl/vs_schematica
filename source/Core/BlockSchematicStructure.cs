using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Schematica.Core
{
    public class BlockSchematicStructure
    {
        [JsonProperty]
        public string GameVersion = "1.20.0";

        [JsonProperty]
        public int SizeX;

        [JsonProperty]
        public int SizeY;

        [JsonProperty]
        public int SizeZ;

        [JsonProperty]
        public Dictionary<int, AssetLocation> BlockCodes = new Dictionary<int, AssetLocation>();

        [JsonProperty]
        public Dictionary<int, AssetLocation> ItemCodes = new Dictionary<int, AssetLocation>();

        [JsonProperty]
        public List<uint> Indices = new List<uint>();

        [JsonProperty]
        public List<int> BlockIds = new List<int>();

        [JsonProperty]
        public Dictionary<uint, string> BlockEntities = new Dictionary<uint, string>();

        // Runtime data (not serialized)
        [JsonIgnore]
        public List<SerializableBlock> Blocks { get; private set; } = new List<SerializableBlock>();

        [JsonIgnore]
        public Dictionary<string, int> BlockCounts { get; private set; } = new Dictionary<string, int>();

        [JsonIgnore]
        public Vec3i Size => new Vec3i(SizeX, SizeY, SizeZ);

        [JsonIgnore]
        public int MaxY => SizeY - 1;

        [JsonIgnore]
        public int TotalBlocks => Blocks.Count;

        // Static methods for loading/saving
        public static BlockSchematicStructure CreateFromSelection(ICoreClientAPI api, BlockPos startPos, BlockPos endPos)
        {
            var minPos = new BlockPos(
                Math.Min(startPos.X, endPos.X),
                Math.Min(startPos.Y, endPos.Y),
                Math.Min(startPos.Z, endPos.Z)
            );
            var maxPos = new BlockPos(
                Math.Max(startPos.X, endPos.X),
                Math.Max(startPos.Y, endPos.Y),
                Math.Max(startPos.Z, endPos.Z)
            );

            var schematic = new BlockSchematicStructure();
            AddArea(api, schematic, minPos, maxPos);
            schematic.Pack(api.World, minPos);
            schematic.Unpack(api);

            return schematic;
        }

        private static void AddArea(ICoreClientAPI api, BlockSchematicStructure schematic, BlockPos start, BlockPos end)
        {
            var blockDataMap = new Dictionary<BlockPos, int>();
            var entityDataMap = new Dictionary<BlockPos, string>();

            var readPos = new BlockPos();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        readPos.Set(x, y, z);

                        var block = api.World.BlockAccessor.GetBlock(readPos);
                        if (block.Id != 0)
                        {
                            var keyPos = new BlockPos(x, y, z);
                            blockDataMap[keyPos] = block.Id;

                            // Handle BlockEntity
                            var blockEntity = api.World.BlockAccessor.GetBlockEntity(readPos);
                            if (blockEntity != null)
                            {
                                entityDataMap[keyPos] = EncodeBlockEntityData(blockEntity, api);
                                blockEntity.OnStoreCollectibleMappings(schematic.BlockCodes, schematic.ItemCodes);

                                // Special handling for chiseled blocks
                                if (block.Code.Path.Contains("chiseled"))
                                {
                                    var microBlock = blockEntity as BlockEntityMicroBlock;
                                    if (microBlock?.BlockIds != null)
                                    {
                                        foreach (int materialId in microBlock.BlockIds)
                                        {
                                            var materialBlock = api.World.GetBlock(materialId);
                                            if (materialBlock != null && !schematic.BlockCodes.ContainsKey(materialId))
                                            {
                                                schematic.BlockCodes[materialId] = materialBlock.Code;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Pack data directly
            schematic.PackFromMaps(api.World, blockDataMap, entityDataMap, start);
        }

        private void PackFromMaps(IWorldAccessor world, Dictionary<BlockPos, int> blockMap, Dictionary<BlockPos, string> entityMap, BlockPos startPos)
        {
            Indices.Clear();
            BlockIds.Clear();
            BlockEntities.Clear();

            if (blockMap.Count == 0) return;

            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;

            foreach (var pos in blockMap.Keys)
            {
                minX = Math.Min(minX, pos.X);
                minY = Math.Min(minY, pos.Y);
                minZ = Math.Min(minZ, pos.Z);
            }

            SizeX = 0;
            SizeY = 0;
            SizeZ = 0;

            foreach (var kvp in blockMap)
            {
                int blockid = kvp.Value;
                if (blockid != 0)
                {
                    BlockCodes[blockid] = world.BlockAccessor.GetBlock(blockid).Code;

                    int dx = kvp.Key.X - minX;
                    int dy = kvp.Key.Y - minY;
                    int dz = kvp.Key.Z - minZ;

                    SizeX = Math.Max(dx, SizeX);
                    SizeY = Math.Max(dy, SizeY);
                    SizeZ = Math.Max(dz, SizeZ);

                    uint index = (uint)(dy << 20 | dz << 10 | dx);
                    Indices.Add(index);
                    BlockIds.Add(blockid);

                    if (entityMap.ContainsKey(kvp.Key))
                    {
                        BlockEntities[index] = entityMap[kvp.Key];
                    }
                }
            }

            SizeX++;
            SizeY++;
            SizeZ++;
        }

        private static string EncodeBlockEntityData(BlockEntity be, ICoreClientAPI api)
        {
            var tree = new TreeAttribute();
            be.ToTreeAttributes(tree);

            if (be is BlockEntityMicroBlock microBlock && microBlock.BlockIds != null)
            {
                var materialCodes = new StringArrayAttribute();
                materialCodes.value = new string[microBlock.BlockIds.Length];

                for (int i = 0; i < microBlock.BlockIds.Length; i++)
                {
                    var block = api.World.GetBlock(microBlock.BlockIds[i]);
                    materialCodes.value[i] = block?.Code?.ToString() ?? "game:air";
                }

                tree["materialCodes"] = materialCodes;
            }

            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                tree.ToBytes(writer);
                return Ascii85.Encode(ms.ToArray());
            }
        }

        public static void SaveToFile(ICoreClientAPI api, BlockSchematicStructure schematic, string filename)
        {
            if (!filename.EndsWith(".json"))
                filename += ".json";

            string fullPath = Path.Combine(api.GetOrCreateDataPath("Schematics"), filename);

            try
            {
                using (var textWriter = new StreamWriter(fullPath))
                {
                    textWriter.Write(JsonConvert.SerializeObject(schematic, Newtonsoft.Json.Formatting.Indented));
                }

                api.ShowChatMessage($"Schematic saved to: {fullPath}");
            }
            catch (IOException e)
            {
                throw new Exception($"Failed exporting: {e.Message}");
            }
        }

        public static BlockSchematicStructure LoadFromFile(ICoreClientAPI api, string filename)
        {
            if (!filename.EndsWith(".json"))
                filename += ".json";

            string fullPath = Path.Combine(api.GetOrCreateDataPath("Schematics"), filename);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Schematic file not found: {filename}");

            try
            {
                using (var textReader = new StreamReader(fullPath))
                {
                    var schematic = JsonConvert.DeserializeObject<BlockSchematicStructure>(textReader.ReadToEnd());
                    schematic.Init(api.World.BlockAccessor);
                    schematic.Unpack(api);
                    return schematic;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed loading {filename}: {e.Message}");
            }
        }

        public static List<string> GetAvailableSchematics(ICoreClientAPI api)
        {
            var schematicsPath = api.GetOrCreateDataPath("Schematics");
            if (!Directory.Exists(schematicsPath))
                return new List<string>();

            return Directory.GetFiles(schematicsPath, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(x => x)
                .ToList();
        }

        public void Init(IBlockAccessor blockAccessor)
        {
            // Remap blocks if needed (for future version compatibility)
        }

        public void Pack(IWorldAccessor world, BlockPos startPos)
        {
            // This method is kept for compatibility but typically not used directly
            // Use PackFromMaps instead
        }

        public void Unpack(ICoreClientAPI api)
        {
            Blocks.Clear();
            BlockCounts.Clear();

            for (int i = 0; i < Indices.Count; i++)
            {
                uint index = Indices[i];
                int blockId = BlockIds[i];

                int dx = (int)(index & 1023);
                int dy = (int)(index >> 20 & 1023);
                int dz = (int)(index >> 10 & 1023);

                var block = new SerializableBlock
                {
                    X = dx,
                    Y = dy,
                    Z = dz,
                    BlockId = blockId,
                    BlockCode = BlockCodes.ContainsKey(blockId) ? BlockCodes[blockId].ToString() : "unknown"
                };

                if (BlockEntities.ContainsKey(index))
                {
                    block.BlockEntityData = System.Text.Encoding.UTF8.GetBytes(BlockEntities[index]);
                }

                Blocks.Add(block);

                // Update block counts
                if (!string.IsNullOrEmpty(block.BlockCode))
                {
                    if (BlockCounts.ContainsKey(block.BlockCode))
                        BlockCounts[block.BlockCode]++;
                    else
                        BlockCounts[block.BlockCode] = 1;
                }
            }
        }

        public void TransformWhilePacked(IWorldAccessor worldForResolve, EnumOrigin aroundOrigin, int angle, EnumAxis? flipAxis = null)
        {
            angle = GameMath.Mod(angle, 360);
            if (angle == 0 && flipAxis == null) return;

            var tempBlocksMap = new Dictionary<BlockPos, int>();
            var tempEntitiesMap = new Dictionary<BlockPos, string>();

            for (int i = 0; i < Indices.Count; i++)
            {
                uint index = Indices[i];
                int storedBlockid = BlockIds[i];

                int dx = (int)(index & 1023);
                int dy = (int)(index >> 20 & 1023);
                int dz = (int)(index >> 10 & 1023);

                AssetLocation blockCode = BlockCodes[storedBlockid];
                Block newBlock = worldForResolve.GetBlock(blockCode);

                if (newBlock == null) continue;

                // Apply flip
                if (flipAxis != null)
                {
                    if (flipAxis.Value == EnumAxis.Y)
                    {
                        dy = SizeY - dy - 1;
                        AssetLocation newCode = newBlock.GetVerticallyFlippedBlockCode();
                        newBlock = worldForResolve.GetBlock(newCode) ?? newBlock;
                    }
                    if (flipAxis.Value == EnumAxis.X)
                    {
                        dx = SizeX - dx - 1;
                        AssetLocation newCode = newBlock.GetHorizontallyFlippedBlockCode(flipAxis.Value);
                        newBlock = worldForResolve.GetBlock(newCode) ?? newBlock;
                    }
                    if (flipAxis.Value == EnumAxis.Z)
                    {
                        dz = SizeZ - dz - 1;
                        AssetLocation newCode = newBlock.GetHorizontallyFlippedBlockCode(flipAxis.Value);
                        newBlock = worldForResolve.GetBlock(newCode) ?? newBlock;
                    }
                }

                // Apply rotation
                if (angle != 0)
                {
                    AssetLocation newCode = newBlock.GetRotatedBlockCode(angle);
                    Block rotBlock = worldForResolve.GetBlock(newCode);
                    if (rotBlock != null)
                        newBlock = rotBlock;
                }

                BlockPos pos = GetRotatedPos(aroundOrigin, angle, dx, dy, dz);
                tempBlocksMap[pos] = newBlock.BlockId;

                if (BlockEntities.ContainsKey(index))
                {
                    tempEntitiesMap[pos] = TransformBlockEntity(BlockEntities[index], angle, flipAxis, worldForResolve);
                }
            }

            // Re-pack the transformed data
            PackFromMaps(worldForResolve, tempBlocksMap, tempEntitiesMap, new BlockPos(0, 0, 0));
        }

        private string TransformBlockEntity(string beData, int angle, EnumAxis? flipAxis, IWorldAccessor world)
        {
            var tree = DecodeBlockEntityData(beData);

            // Handle chiseled blocks rotation
            var tempBE = new BlockEntityMicroBlock();
            tempBE.Initialize(world.Api);

            var rotatable = tempBE as IRotatable;
            if (rotatable != null)
            {
                rotatable.OnTransformed(world, tree, angle, BlockCodes, ItemCodes, flipAxis);
            }

            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                tree.ToBytes(writer);
                return Ascii85.Encode(ms.ToArray());
            }
        }

        private TreeAttribute DecodeBlockEntityData(string data)
        {
            byte[] buffer = Ascii85.Decode(data);
            TreeAttribute tree = new TreeAttribute();

            using (var ms = new MemoryStream(buffer))
            {
                var reader = new BinaryReader(ms);
                tree.FromBytes(reader);
            }

            return tree;
        }

        private BlockPos GetRotatedPos(EnumOrigin aroundOrigin, int angle, int dx, int dy, int dz)
        {
            if (aroundOrigin != EnumOrigin.StartPos)
            {
                dx -= SizeX / 2;
                dz -= SizeZ / 2;
            }

            BlockPos pos = new BlockPos(dx, dy, dz);

            switch (angle)
            {
                case 90:
                    pos.Set(-dz, dy, dx);
                    break;
                case 180:
                    pos.Set(-dx, dy, -dz);
                    break;
                case 270:
                    pos.Set(dz, dy, -dx);
                    break;
            }

            if (aroundOrigin != EnumOrigin.StartPos)
            {
                pos.X += SizeX / 2;
                pos.Z += SizeZ / 2;
            }

            return pos;
        }

        public List<SerializableBlock> GetBlocksAtLayer(int y)
        {
            return Blocks.Where(b => b.Y == y).ToList();
        }

        public BlockSchematicStructure ClonePacked()
        {
            return new BlockSchematicStructure
            {
                SizeX = this.SizeX,
                SizeY = this.SizeY,
                SizeZ = this.SizeZ,
                GameVersion = this.GameVersion,
                BlockCodes = new Dictionary<int, AssetLocation>(this.BlockCodes),
                ItemCodes = new Dictionary<int, AssetLocation>(this.ItemCodes),
                Indices = new List<uint>(this.Indices),
                BlockIds = new List<int>(this.BlockIds),
                BlockEntities = new Dictionary<uint, string>(this.BlockEntities)
            };
        }
    }
}