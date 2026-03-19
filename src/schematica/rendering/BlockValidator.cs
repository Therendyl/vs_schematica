using System;
using System.Collections.Generic;
using System.IO;
using Schematica.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Schematica.Rendering
{
    public static class BlockValidator
    {
        public static bool IsBlockCorrect(ICoreClientAPI api, BlockPos worldPos, SerializableBlock blockData)
        {
            ArgumentNullException.ThrowIfNull(api);
            ArgumentNullException.ThrowIfNull(worldPos);
            ArgumentNullException.ThrowIfNull(blockData);

            var currentBlock = api.World.BlockAccessor.GetBlock(worldPos);
            if (currentBlock?.Code == null)
            {
                return false;
            }

            if (currentBlock.Code.Path.Contains("chiseled", StringComparison.Ordinal) && blockData.BlockEntityData.Length > 0)
            {
                return IsChiseledBlockCorrect(api, worldPos, blockData);
            }

            return string.Equals(currentBlock.Code.ToString(), blockData.BlockCode, StringComparison.Ordinal);
        }

        private static bool IsChiseledBlockCorrect(ICoreClientAPI api, BlockPos worldPos, SerializableBlock blockData)
        {
            ArgumentNullException.ThrowIfNull(api);
            ArgumentNullException.ThrowIfNull(worldPos);
            ArgumentNullException.ThrowIfNull(blockData);

            var currentBlock = api.World.BlockAccessor.GetBlock(worldPos);
            if (currentBlock?.Code?.Path.Contains("chiseled", StringComparison.Ordinal) != true || blockData.BlockEntityData.IsEmpty)
            {
                return false;
            }

            var blockEntity = api.World.BlockAccessor.GetBlockEntity(worldPos);
            if (blockEntity == null || blockData.BlockEntityData.IsEmpty)
            {
                return false;
            }

            var ascii85Data = System.Text.Encoding.UTF8.GetString(blockData.BlockEntityData.ToArray());
            try
            {
                byte[] decodedData = Ascii85.Decode(ascii85Data);
                TreeAttribute schematicTree = new TreeAttribute();
                using (var ms = new MemoryStream(decodedData))
                {
                    using var reader = new BinaryReader(ms);
                    schematicTree.FromBytes(reader);
                }

                // Get current block data
                TreeAttribute currentTree = new TreeAttribute();
                blockEntity.ToTreeAttributes(currentTree);

                // Compare cuboids
                var schematicCuboids = BlockEntityMicroBlock.GetVoxelCuboids(schematicTree);
                var currentCuboids = BlockEntityMicroBlock.GetVoxelCuboids(currentTree);

                if (schematicCuboids.Length != currentCuboids.Length) return false;

                // Get material codes
                var schematicMaterialCodes = GetMaterialCodes(api, schematicTree);
                var currentMaterialCodes = GetMaterialCodes(api, currentTree);

                if (schematicMaterialCodes.Length != currentMaterialCodes.Length) return false;

                // Create material mapping
                var materialMapping = new Dictionary<int, int>(schematicMaterialCodes.Length);
                for (int i = 0; i < schematicMaterialCodes.Length; i++)
                {
                    for (int j = 0; j < currentMaterialCodes.Length; j++)
                    {
                        if (string.Equals(schematicMaterialCodes[i], currentMaterialCodes[j], StringComparison.Ordinal))
                        {
                            materialMapping[i] = j;
                            break;
                        }
                    }
                }

                if (materialMapping.Count != schematicMaterialCodes.Length) return false;

                // Compare cuboids with material mapping
                for (int i = 0; i < schematicCuboids.Length; i++)
                {
                    uint schematicCuboid = schematicCuboids[i];
                    uint currentCuboid = currentCuboids[i];

                    // Compare positions (first 12 bits)
                    if ((schematicCuboid & 0xFFF) != (currentCuboid & 0xFFF)) return false;

                    // Get material indices
                    int schematicMatIndex = (int)((schematicCuboid >> 12) & 0xF);
                    int currentMatIndex = (int)((currentCuboid >> 12) & 0xF);

                    // Check materials match through mapping
                    if (materialMapping.TryGetValue(schematicMatIndex, out int currentMatMapped))
                    {
                        if (currentMatMapped != currentMatIndex) return false;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (FormatException ex)
            {
                api.Logger.Error($"[Schematica Plus] Error checking chiseled block: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                api.Logger.Error($"[Schematica Plus] Error checking chiseled block: {ex.Message}");
                return false;
            }
        }

        private static string[] GetMaterialCodes(ICoreClientAPI api, TreeAttribute tree)
        {
            ArgumentNullException.ThrowIfNull(api);
            ArgumentNullException.ThrowIfNull(tree);

            // Try to get saved codes first
            var materialCodesAttr = tree["materialCodes"] as StringArrayAttribute;
            if (materialCodesAttr?.value != null && materialCodesAttr.value.Length > 0)
            {
                return materialCodesAttr.value;
            }

            // If not, convert IDs to codes
            var materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, api.World);
            var codes = new string[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                var block = api.World.GetBlock(materials[i]);
                codes[i] = block?.Code?.ToString() ?? "unknown";
            }
            return codes;
        }

        public static bool IsPositionEmpty(ICoreClientAPI api, BlockPos worldPos)
        {
            ArgumentNullException.ThrowIfNull(api);

            var block = api.World.BlockAccessor.GetBlock(worldPos);
            return block == null || block.Id == 0 || block.Code?.ToString() == "air";
        }
    }
}



