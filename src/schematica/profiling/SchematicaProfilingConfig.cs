using System;

namespace Schematica.Profiling
{
    public sealed class SchematicaProfilingConfig
    {
        public bool Enabled { get; set; }
        public bool AutoStartOnWorldLoad { get; set; }
        public bool ChatNotifications { get; set; } = true;
        public string ScenarioName { get; set; } = "default";
        public string BaselineName { get; set; } = "default";
        public int WarmupSeconds { get; set; } = 3;
        public int MeasureSeconds { get; set; } = 20;
        public int Iterations { get; set; } = 5;
        public int ChunkRadius { get; set; } = 10;
        public int ChunkUpdateIntervalTicks { get; set; } = 20;
        public int LayerStepIntervalTicks { get; set; } = 10;
        public bool SimulateMovement { get; set; }
        public int MovementCycleTicks { get; set; } = 30;
        public double CameraSweepDegrees { get; set; } = 120;
        public double CameraPitchDegrees { get; set; } = 16;
        public int MaxEvents { get; set; } = 50000;
        public string CaptureMode { get; set; } = "summary";
        public int TickTraceSampleInterval { get; set; } = 20;
        public int RenderTraceSampleInterval { get; set; } = 30;
        public int ChunkTraceSampleInterval { get; set; } = 20;
        public int LayerTraceSampleInterval { get; set; } = 30;
        public double RenderP95RegressionThresholdPercent { get; set; } = 10;
        public double TickP95RegressionThresholdPercent { get; set; } = 10;
        public double RenderAllocationRegressionThresholdPercent { get; set; } = 20;

        public static SchematicaProfilingConfig CreateDefault()
        {
            return new SchematicaProfilingConfig
            {
                Enabled = false,
                AutoStartOnWorldLoad = false,
                ChatNotifications = true,
                ScenarioName = "default",
                BaselineName = "default",
                WarmupSeconds = 3,
                MeasureSeconds = 20,
                Iterations = 5,
                ChunkRadius = 10,
                ChunkUpdateIntervalTicks = 20,
                LayerStepIntervalTicks = 10,
                SimulateMovement = false,
                MovementCycleTicks = 30,
                CameraSweepDegrees = 120,
                CameraPitchDegrees = 16,
                MaxEvents = 50000,
                CaptureMode = "summary",
                TickTraceSampleInterval = 20,
                RenderTraceSampleInterval = 30,
                ChunkTraceSampleInterval = 20,
                LayerTraceSampleInterval = 30,
                RenderP95RegressionThresholdPercent = 10,
                TickP95RegressionThresholdPercent = 10,
                RenderAllocationRegressionThresholdPercent = 20
            };
        }

        public SchematicaProfilingConfig Normalize()
        {
            return new SchematicaProfilingConfig
            {
                Enabled = Enabled,
                AutoStartOnWorldLoad = AutoStartOnWorldLoad,
                ChatNotifications = ChatNotifications,
                ScenarioName = NormalizeString(ScenarioName, "default"),
                BaselineName = NormalizeString(BaselineName, "default"),
                WarmupSeconds = Math.Clamp(WarmupSeconds, 0, 300),
                MeasureSeconds = Math.Clamp(MeasureSeconds, 1, 1800),
                Iterations = Math.Clamp(Iterations, 1, 50),
                ChunkRadius = Math.Clamp(ChunkRadius, 1, 64),
                ChunkUpdateIntervalTicks = Math.Clamp(ChunkUpdateIntervalTicks, 1, 400),
                LayerStepIntervalTicks = Math.Clamp(LayerStepIntervalTicks, 1, 400),
                SimulateMovement = SimulateMovement,
                MovementCycleTicks = Math.Clamp(MovementCycleTicks, 4, 400),
                CameraSweepDegrees = Math.Clamp(CameraSweepDegrees, 0, 360),
                CameraPitchDegrees = Math.Clamp(CameraPitchDegrees, 0, 89),
                MaxEvents = Math.Clamp(MaxEvents, 1000, 500000),
                CaptureMode = NormalizeCaptureMode(CaptureMode),
                TickTraceSampleInterval = Math.Clamp(TickTraceSampleInterval, 1, 1000),
                RenderTraceSampleInterval = Math.Clamp(RenderTraceSampleInterval, 1, 1000),
                ChunkTraceSampleInterval = Math.Clamp(ChunkTraceSampleInterval, 1, 1000),
                LayerTraceSampleInterval = Math.Clamp(LayerTraceSampleInterval, 1, 1000),
                RenderP95RegressionThresholdPercent = Math.Clamp(RenderP95RegressionThresholdPercent, 0, 500),
                TickP95RegressionThresholdPercent = Math.Clamp(TickP95RegressionThresholdPercent, 0, 500),
                RenderAllocationRegressionThresholdPercent = Math.Clamp(RenderAllocationRegressionThresholdPercent, 0, 500)
            };
        }

        private static string NormalizeString(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string NormalizeCaptureMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "summary";
            }

            string normalized = value.Trim().ToUpperInvariant();
            return normalized is "TRACE" ? "trace" : "summary";
        }
    }
}
