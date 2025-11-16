using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Schematica.Utils
{
    public static class SchematicaHelpers
    {
        /// <summary>
        /// Highlights blocks in the world with specified color
        /// </summary>
        public static void HighlightBlocks(ICoreClientAPI api, List<BlockPos> positions, int color, int id = 500)
        {
            api.World.HighlightBlocks(
                api.World.Player,
                id,
                positions,
                new List<int> { color },
                EnumHighlightBlocksMode.Absolute,
                EnumHighlightShape.Cubes
            );
        }

        /// <summary>
        /// Clear highlight by ID
        /// </summary>
        public static void ClearHighlight(ICoreClientAPI api, int id = 500)
        {
            api.World.HighlightBlocks(
                api.World.Player,
                id,
                new List<BlockPos>(),
                new List<int>()
            );
        }

        /// <summary>
        /// Get all block positions in a cuboid area
        /// </summary>
        public static List<BlockPos> GetBlocksInArea(BlockPos start, BlockPos end)
        {
            var blocks = new List<BlockPos>();
            var minPos = new BlockPos(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Min(start.Z, end.Z)
            );
            var maxPos = new BlockPos(
                Math.Max(start.X, end.X),
                Math.Max(start.Y, end.Y),
                Math.Max(start.Z, end.Z)
            );

            for (int x = minPos.X; x <= maxPos.X; x++)
            {
                for (int y = minPos.Y; y <= maxPos.Y; y++)
                {
                    for (int z = minPos.Z; z <= maxPos.Z; z++)
                    {
                        blocks.Add(new BlockPos(x, y, z));
                    }
                }
            }

            return blocks;
        }

        /// <summary>
        /// Format block position as string
        /// </summary>
        public static string FormatPosition(BlockPos pos)
        {
            return $"X: {pos.X}, Y: {pos.Y}, Z: {pos.Z}";
        }
    }
}