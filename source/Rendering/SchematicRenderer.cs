using Schematica.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Schematica.Rendering
{
    public class SchematicRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private SchematicaModSystem modSystem;
        private Dictionary<Vec3i, GhostChunk> ghostChunks = new Dictionary<Vec3i, GhostChunk>();
        private BlockPos currentOrigin;
        private List<SerializableBlock> currentBlocks;
        private int chunkSize = 4;

        // Layer rendering
        private int currentLayer = 0;
        private bool showAllLayers = false;
        private BlockSchematicStructure currentSchematic;

        public double RenderOrder => 0.5;
        public int RenderRange => 999;
        public int CurrentLayer => currentLayer;
        public bool ShowAllLayers => showAllLayers;

        public SchematicRenderer(ICoreClientAPI api, SchematicaModSystem modSystem)
        {
            this.capi = api;
            this.modSystem = modSystem;
        }

        public void SetSchematic(BlockSchematicStructure schematic)
        {
            currentSchematic = schematic;
            currentLayer = 0;
        }

        public void SetRenderOrigin(BlockPos origin)
        {
            currentOrigin = origin?.Copy();
            UpdateRender();
        }

        public void NextLayer()
        {
            if (currentSchematic == null) return;

            if (currentLayer < currentSchematic.MaxY)
            {
                currentLayer++;
                UpdateRender();
                capi.ShowChatMessage(Lang.Get("schematica:msg-layer", currentLayer, currentSchematic.MaxY));
            }
        }

        public void PreviousLayer()
        {
            if (currentSchematic == null) return;

            if (currentLayer > 0)
            {
                currentLayer--;
                UpdateRender();
                capi.ShowChatMessage(Lang.Get("schematica:msg-layer", currentLayer, currentSchematic.MaxY));
            }
        }

        public void SetShowAllLayers(bool show)
        {
            showAllLayers = show;
            UpdateRender();
        }

        public void ToggleAllLayers()
        {
            showAllLayers = !showAllLayers;
            UpdateRender();
        }

        public void SetLayer(int layer)
        {
            if (currentSchematic == null) return;

            currentLayer = Math.Max(0, Math.Min(layer, currentSchematic.MaxY));
            UpdateRender();
        }

        public void UpdateRender()
        {
            if (currentSchematic == null) return;

            if (currentOrigin == null) return;

            ClearAllProjections();

            var blocksToRender = showAllLayers
                ? currentSchematic.Blocks
                : currentSchematic.GetBlocksAtLayer(currentLayer);

            BuildProjection(blocksToRender, currentOrigin);
        }

        public void Clear()
        {
            currentSchematic = null;
            currentOrigin = null;
            currentLayer = 0;
            showAllLayers = false;
            ClearAllProjections();
        }

        public void BuildProjection(List<SerializableBlock> blocks, BlockPos startPos)
        {
            ClearAllProjections();
            if (blocks == null || blocks.Count == 0 || startPos == null) return;

            currentOrigin = startPos.Copy();
            currentBlocks = blocks;

            capi.Logger.Debug($"[Schematica] BuildProjection: startPos={startPos}, blocks={blocks.Count}");

            var blocksByChunk = new Dictionary<Vec3i, List<GhostBlock>>();
            int validBlocks = 0;
            int skippedBlocks = 0;

            foreach (var blockData in blocks)
            {
                var worldPos = new BlockPos(
                    currentOrigin.X + blockData.X,
                    currentOrigin.Y + blockData.Y,
                    currentOrigin.Z + blockData.Z
                );

                var chunkPos = new Vec3i(
                    worldPos.X / chunkSize,
                    worldPos.Y / chunkSize,
                    worldPos.Z / chunkSize
                );

                if (!blocksByChunk.ContainsKey(chunkPos))
                {
                    blocksByChunk[chunkPos] = new List<GhostBlock>();
                }

                var block = capi.World.GetBlock(new AssetLocation(blockData.BlockCode));
                if (block != null && block.Id != 0)
                {
                    var currentBlock = capi.World.BlockAccessor.GetBlock(worldPos);
                    bool isEmpty = currentBlock.Id == 0;
                    bool isCorrect = BlockValidator.IsBlockCorrect(capi, worldPos, blockData);

                    if (!isCorrect)
                    {
                        blocksByChunk[chunkPos].Add(new GhostBlock
                        {
                            Position = worldPos,
                            Block = block,
                            BlockData = blockData,
                            IsEmpty = isEmpty
                        });
                    }
                }
                capi.Logger.Debug($"[Schematica] Built {ghostChunks.Count} chunks with {validBlocks} valid blocks, skipped {skippedBlocks}");
            }

            foreach (var kvp in blocksByChunk)
            {
                var chunk = new GhostChunk(capi, kvp.Key, chunkSize);
                chunk.BuildMesh(kvp.Value);
                ghostChunks[kvp.Key] = chunk;
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (ghostChunks.Count == 0) return;

            if (ghostChunks.Count > 0 && deltaTime > 0)
            {
                capi.Logger.VerboseDebug($"[Schematica] Rendering {ghostChunks.Count} chunks");
            }

            var prog = capi.Render.PreparedStandardShader(0, 0, 0);
            prog.Use();

            capi.Render.GlToggleBlend(true, EnumBlendMode.Standard);

            var cameraPos = capi.World.Player.Entity.CameraPos;

            // Проверка расстояния до ближайшего чанка
            if (ghostChunks.Count > 0)
            {
                var firstChunk = ghostChunks.First().Key;
                var chunkWorldPos = new Vec3d(
                    firstChunk.X * chunkSize,
                    firstChunk.Y * chunkSize,
                    firstChunk.Z * chunkSize
                );
                var distance = chunkWorldPos.DistanceTo(cameraPos);

                if (distance > 1000) // Если слишком далеко
                {
                    capi.Logger.Warning($"[Schematica] Chunks too far from camera: {distance} blocks");
                }
            }

            prog.ViewMatrix = capi.Render.CameraMatrixOriginf;
            prog.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
            prog.ExtraGlow = 20;

            foreach (var chunk in ghostChunks.Values)
            {
                chunk.Render(prog, cameraPos);
            }

            prog.Stop();
            capi.Render.GlToggleBlend(false);
        }

        public void ClearAllProjections()
        {
            foreach (var chunk in ghostChunks.Values)
            {
                chunk.Dispose();
            }
            ghostChunks.Clear();
            currentOrigin = null;
            currentBlocks = null;
        }

        public void UpdateChunksNearPlayer(BlockPos playerPos, int radius = 10)
        {
            if (currentBlocks == null || currentOrigin == null) return;

            var chunksToUpdate = new HashSet<Vec3i>();

            for (int x = playerPos.X - radius; x <= playerPos.X + radius; x++)
            {
                for (int y = playerPos.Y - radius; y <= playerPos.Y + radius; y++)
                {
                    for (int z = playerPos.Z - radius; z <= playerPos.Z + radius; z++)
                    {
                        var chunkPos = new Vec3i(x / chunkSize, y / chunkSize, z / chunkSize);
                        chunksToUpdate.Add(chunkPos);
                    }
                }
            }

            foreach (var chunkPos in chunksToUpdate)
            {
                if (ghostChunks.ContainsKey(chunkPos))
                {
                    UpdateSingleChunk(chunkPos);
                }
            }
        }

        private void UpdateSingleChunk(Vec3i chunkPos)
        {
            var blocksInChunk = new List<GhostBlock>();

            foreach (var blockData in currentBlocks)
            {
                var worldPos = new BlockPos(
                    currentOrigin.X + blockData.X,
                    currentOrigin.Y + blockData.Y,
                    currentOrigin.Z + blockData.Z
                );

                var blockChunkPos = new Vec3i(
                    worldPos.X / chunkSize,
                    worldPos.Y / chunkSize,
                    worldPos.Z / chunkSize
                );

                if (blockChunkPos.Equals(chunkPos))
                {
                    var block = capi.World.GetBlock(new AssetLocation(blockData.BlockCode));
                    if (block != null && block.Id != 0)
                    {
                        var currentBlock = capi.World.BlockAccessor.GetBlock(worldPos);
                        bool isCorrect = BlockValidator.IsBlockCorrect(capi, worldPos, blockData);
                        bool isEmpty = currentBlock.Id == 0;

                        if (!isCorrect)
                        {
                            blocksInChunk.Add(new GhostBlock
                            {
                                Position = worldPos,
                                Block = block,
                                BlockData = blockData,
                                IsEmpty = isEmpty
                            });
                        }
                    }
                }
            }

            if (ghostChunks.ContainsKey(chunkPos))
            {
                ghostChunks[chunkPos].Dispose();
            }

            if (blocksInChunk.Count > 0)
            {
                var chunk = new GhostChunk(capi, chunkPos, chunkSize);
                chunk.BuildMesh(blocksInChunk);
                ghostChunks[chunkPos] = chunk;
            }
            else
            {
                ghostChunks.Remove(chunkPos);
            }
        }

        public void Dispose()
        {
            ClearAllProjections();
        }

        private class GhostChunk
        {
            private Vec3i chunkPos;
            private int chunkSize;
            private MultiTextureMeshRef meshRef;
            private ICoreClientAPI capi;

            public GhostChunk(ICoreClientAPI api, Vec3i pos, int size)
            {
                this.capi = api;
                this.chunkPos = pos;
                this.chunkSize = size;
            }

            public void BuildMesh(List<GhostBlock> blocks)
            {
                var meshData = new MeshData(24 * blocks.Count, 36 * blocks.Count, false, true, true, false);

                foreach (var ghostBlock in blocks)
                {
                    var blockMesh = GenerateMeshForBlock(ghostBlock);
                    if (blockMesh == null || blockMesh.VerticesCount == 0) continue;

                    var clonedMesh = blockMesh.Clone();

                    byte alpha = 120;
                    for (int i = 0; i < clonedMesh.VerticesCount; i++)
                    {
                        if (ghostBlock.IsEmpty)
                        {
                            clonedMesh.Rgba[i * 4] = 255;
                            clonedMesh.Rgba[i * 4 + 1] = 255;
                            clonedMesh.Rgba[i * 4 + 2] = 255;
                        }
                        else
                        {
                            clonedMesh.Rgba[i * 4] = 255;
                            clonedMesh.Rgba[i * 4 + 1] = 100;
                            clonedMesh.Rgba[i * 4 + 2] = 100;
                        }
                        clonedMesh.Rgba[i * 4 + 3] = alpha;
                    }

                    var localPos = new Vec3i(
                        ghostBlock.Position.X - (chunkPos.X * chunkSize),
                        ghostBlock.Position.Y - (chunkPos.Y * chunkSize),
                        ghostBlock.Position.Z - (chunkPos.Z * chunkSize)
                    );

                    meshData.AddMeshData(clonedMesh, localPos.X, localPos.Y, localPos.Z);
                }

                if (meshData.VerticesCount > 0)
                {
                    meshRef = capi.Render.UploadMultiTextureMesh(meshData);
                }
            }

            private MeshData GenerateMeshForBlock(GhostBlock ghostBlock)
            {
                if (ghostBlock.Block.Code.Path.Contains("chiseled") && ghostBlock.BlockData.BlockEntityData != null)
                {
                    try
                    {
                        string ascii85Data = System.Text.Encoding.UTF8.GetString(ghostBlock.BlockData.BlockEntityData);
                        byte[] decodedData = Ascii85.Decode(ascii85Data);

                        TreeAttribute tree = new TreeAttribute();
                        using (var ms = new MemoryStream(decodedData))
                        {
                            var reader = new BinaryReader(ms);
                            tree.FromBytes(reader);
                        }

                        int[] materials = null;

                        var materialCodesAttr = tree["materialCodes"] as StringArrayAttribute;
                        if (materialCodesAttr != null)
                        {
                            materials = new int[materialCodesAttr.value.Length];
                            for (int i = 0; i < materialCodesAttr.value.Length; i++)
                            {
                                var block = capi.World.GetBlock(new AssetLocation(materialCodesAttr.value[i]));
                                materials[i] = block?.Id ?? 0;
                            }
                        }
                        else
                        {
                            materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, capi.World);
                        }

                        uint[] cuboids = BlockEntityMicroBlock.GetVoxelCuboids(tree);

                        if (materials != null && cuboids != null && materials.Length > 0 && cuboids.Length > 0)
                        {
                            var voxelCuboids = new List<uint>(cuboids);
                            var meshData = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, materials, null, null, cuboids, 0);

                            if (meshData != null && meshData.VerticesCount > 0)
                            {
                                return meshData.Clone();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        capi.Logger.Warning($"[Schematica] Failed to create chiseled mesh: {ex.Message}");
                    }
                }

                return capi.TesselatorManager.GetDefaultBlockMesh(ghostBlock.Block)?.Clone();
            }

            public void Render(IStandardShaderProgram prog, Vec3d cameraPos)
            {
                if (meshRef == null) return;

                var chunkWorldPos = new Vec3d(
                    chunkPos.X * chunkSize,
                    chunkPos.Y * chunkSize,
                    chunkPos.Z * chunkSize
                );

                prog.ModelMatrix = new Matrixf()
                    .Identity()
                    .Translate(
                        (float)(chunkWorldPos.X - cameraPos.X),
                        (float)(chunkWorldPos.Y - cameraPos.Y),
                        (float)(chunkWorldPos.Z - cameraPos.Z))
                    .Values;

                capi.Render.RenderMultiTextureMesh(meshRef, "tex");
            }

            public void Dispose()
            {
                meshRef?.Dispose();
            }
        }

        private class GhostBlock
        {
            public BlockPos Position { get; set; }
            public Block Block { get; set; }
            public SerializableBlock BlockData { get; set; }
            public bool IsEmpty { get; set; }
        }
    }
}