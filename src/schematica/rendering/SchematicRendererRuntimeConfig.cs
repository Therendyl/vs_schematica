using System;

namespace Schematica.Rendering
{
    public sealed class SchematicRendererRuntimeConfig
    {
        public bool SafeMode { get; set; }
        public bool EnableAdaptiveSafeMode { get; set; } = true;
        public int SafeModeBlockThreshold { get; set; } = 120000;
        public bool EnableChunkIndex { get; set; } = true;
        public bool EnableChunkRangeIteration { get; set; } = true;
        public bool EnableBlockCodeCache { get; set; } = true;
        public bool EnableFallbackOnIndexMiss { get; set; } = true;
        public bool EnableRenderLogThrottling { get; set; } = true;
        public bool EnableHotPathLogs { get; set; }
        public bool EnableChunkParityCheckInDebugBurst { get; set; }
        public int ChunkRangeConservativeMargin { get; set; } = 1;
        public int BlockCodeCacheMaxEntries { get; set; } = 2048;
        public int ChunkBuildsPerFrame { get; set; } = 8;
        public int ChunkUpdateQueueMaxSize { get; set; } = 4096;
        public int RenderLogCooldownMs { get; set; } = 2000;
        public int DistanceWarningCooldownMs { get; set; } = 10000;
        public int DebugBurstDefaultSeconds { get; set; } = 30;

        public static SchematicRendererRuntimeConfig CreateDefault()
        {
            return new SchematicRendererRuntimeConfig();
        }

        public SchematicRendererRuntimeConfig Normalize()
        {
            return new SchematicRendererRuntimeConfig
            {
                SafeMode = SafeMode,
                EnableAdaptiveSafeMode = EnableAdaptiveSafeMode,
                EnableChunkIndex = EnableChunkIndex,
                EnableChunkRangeIteration = EnableChunkRangeIteration,
                EnableBlockCodeCache = EnableBlockCodeCache,
                EnableFallbackOnIndexMiss = EnableFallbackOnIndexMiss,
                EnableRenderLogThrottling = EnableRenderLogThrottling,
                EnableHotPathLogs = EnableHotPathLogs,
                EnableChunkParityCheckInDebugBurst = EnableChunkParityCheckInDebugBurst,
                ChunkRangeConservativeMargin = Math.Clamp(ChunkRangeConservativeMargin, 0, 4),
                SafeModeBlockThreshold = Math.Clamp(SafeModeBlockThreshold, 1, 10_000_000),
                BlockCodeCacheMaxEntries = Math.Clamp(BlockCodeCacheMaxEntries, 32, 65536),
                ChunkBuildsPerFrame = Math.Clamp(ChunkBuildsPerFrame, 1, 256),
                ChunkUpdateQueueMaxSize = Math.Clamp(ChunkUpdateQueueMaxSize, 64, 20000),
                RenderLogCooldownMs = Math.Clamp(RenderLogCooldownMs, 0, 120000),
                DistanceWarningCooldownMs = Math.Clamp(DistanceWarningCooldownMs, 0, 300000),
                DebugBurstDefaultSeconds = Math.Clamp(DebugBurstDefaultSeconds, 1, 300)
            };
        }
    }
}
