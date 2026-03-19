namespace Schematica.Profiling
{
    public interface ISchematicaProfilingSink
    {
        bool IsCapturing { get; }

        void OnRenderFrameMeasured(double elapsedMilliseconds, long allocatedBytes, int renderedChunkCount);

        void OnBuildProjectionMeasured(double elapsedMilliseconds, int blockCount, int chunkCount, long allocatedBytes);

        void OnChunkScanMeasured(double elapsedMilliseconds, int candidateChunkCount, int updatedChunkCount, long allocatedBytes);

        void OnChunkUpdateQueueMeasured(double elapsedMilliseconds, int candidateChunkCount, int processedChunkCount, int remainingChunkCount, long allocatedBytes);

        void OnLayerLookupMeasured(double elapsedMilliseconds, int requestedLayer, int matchedBlockCount, long allocatedBytes);
    }
}
