using Schematica.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var currentBlock = api.World.BlockAccessor.GetBlock(worldPos);

            // Special check for chiseled blocks
            if (currentBlock.Code?.Path.Contains("chiseled") == true && blockData.BlockEntityData != null)
            {
                return IsChiseledBlockCorrect(api, worldPos, blockData);
            }

            // Normal check for other blocks
            return currentBlock.Code?.ToString() == blockData.BlockCode;
        }

        private static bool IsChiseledBlockCorrect(ICoreClientAPI api, BlockPos worldPos, SerializableBlock blockData)
        {
            var currentBlock = api.World.BlockAccessor.GetBlock(worldPos);
            if (!currentBlock.Code.Path.Contains("chiseled")) return false;

            var blockEntity = api.World.BlockAccessor.GetBlockEntity(worldPos);
            if (blockEntity == null || blockData.BlockEntityData == null) return false;

            try
            {
                // Decode schematic data
                string ascii85Data = System.Text.Encoding.UTF8.GetString(blockData.BlockEntityData);
                byte[] decodedData = Ascii85.Decode(ascii85Data);
                TreeAttribute schematicTree = new TreeAttribute();
                using (var ms = new MemoryStream(decodedData))
                {
                    var reader = new BinaryReader(ms);
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
                var materialMapping = new Dictionary<int, int>();
                for (int i = 0; i < schematicMaterialCodes.Length; i++)
                {
                    for (int j = 0; j < currentMaterialCodes.Length; j++)
                    {
                        if (schematicMaterialCodes[i] == currentMaterialCodes[j])
                        {
                            materialMapping[i] = j;
                            break;
                        }
                    }
                }

                // Check all materials are found
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
                    if (materialMapping.ContainsKey(schematicMatIndex))
                    {
                        if (materialMapping[schematicMatIndex] != currentMatIndex) return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                api.Logger.Error($"[Schematica] Error checking chiseled block: {ex}");
                return false;
            }
        }

        private static string[] GetMaterialCodes(ICoreClientAPI api, TreeAttribute tree)
        {
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
            var block = api.World.BlockAccessor.GetBlock(worldPos);
            return block.Id == 0 || block.Code?.ToString() == "air";
        }
    }
}