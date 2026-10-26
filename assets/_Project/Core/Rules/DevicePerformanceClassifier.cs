#nullable enable
using System;
using System.Collections.Generic;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Configurable thresholds and weights for DevicePerformanceClassifier.
    /// Allows tuning classification behavior without modifying core detection logic.
    /// </summary>
    [Serializable]
    public sealed class DeviceClassificationPolicy
    {
        // Component Weights (normalized automatically if sum != 1.0)
        public float RamWeight { get; set; } = 0.40f;
        public float GpuCapabilityWeight { get; set; } = 0.35f;
        public float CpuWeight { get; set; } = 0.15f;
        public float DisplayCostWeight { get; set; } = 0.10f;

        // Weighted Score Thresholds (0..100 scale)
        public float MediumTierMinScore { get; set; } = 42f;
        public float HighTierMinScore { get; set; } = 68f;
        public float UltraTierMinScore { get; set; } = 85f;

        // Hard Memory Safety Caps (MB)
        public int LowRamHardCapMB { get; set; } = 3200;     // <= ~3 GB RAM -> Low
        public int MediumRamHardCapMB { get; set; } = 4608;  // <= ~4.5 GB RAM -> max Medium
        public int HighRamThresholdMB { get; set; } = 5600;  // ~6 GB RAM
        public int UltraRamThresholdMB { get; set; } = 7400; // ~8+ GB RAM

        // Minimum GPU capability score required to unlock High or Ultra (prevents RAM-only inflation)
        public float MinGpuScoreForHigh { get; set; } = 60f;
        public float MinGpuScoreForUltra { get; set; } = 92f;

        // Display Pixel Count Thresholds (for fill-rate cost evaluation)
        public long StandardFhdPixels { get; set; } = 1920L * 1080L;       // ~2.07M pixels
        public long HighResQhdPixels { get; set; } = 2560L * 1440L;        // ~3.68M pixels
        public long UltraHighResPixels { get; set; } = 3200L * 1440L;      // ~4.60M pixels

        public static DeviceClassificationPolicy CreateDefault() => new();
    }

    /// <summary>
    /// Detailed result of classifying a device's hardware capabilities.
    /// </summary>
    public sealed class DeviceClassificationResult
    {
        public DevicePerformanceTier Tier { get; }
        public float FinalWeightedScore { get; }
        public float RamScore { get; }
        public float GpuScore { get; }
        public float CpuScore { get; }
        public float DisplayScore { get; }
        public string SafetyCapApplied { get; }
        public IReadOnlyList<string> DiagnosticNotes { get; }

        public DeviceClassificationResult(
            DevicePerformanceTier tier,
            float finalWeightedScore,
            float ramScore,
            float gpuScore,
            float cpuScore,
            float displayScore,
            string safetyCapApplied,
            IReadOnlyList<string> diagnosticNotes)
        {
            Tier = tier;
            FinalWeightedScore = finalWeightedScore;
            RamScore = ramScore;
            GpuScore = gpuScore;
            CpuScore = cpuScore;
            DisplayScore = displayScore;
            SafetyCapApplied = safetyCapApplied;
            DiagnosticNotes = diagnosticNotes;
        }

        public override string ToString()
        {
            string capInfo = string.IsNullOrEmpty(SafetyCapApplied) ? "None" : SafetyCapApplied;
            return $"Tier={Tier} (Score={FinalWeightedScore:F1}/100 | RAM={RamScore:F0}, GPU={GpuScore:F0}, CPU={CpuScore:F0}, Display={DisplayScore:F0} | Cap={capInfo})";
        }
    }

    /// <summary>
    /// Classifies Android/mobile hardware into Low, Medium, High, or Ultra tiers
    /// using a weighted multi-factor evaluation and conservative safety caps.
    /// Pure C# implementation so it can be unit-tested independently of Unity runtime.
    /// </summary>
    public static class DevicePerformanceClassifier
    {
        public static DeviceClassificationResult Classify(
            DeviceCapabilities? capabilities,
            DeviceClassificationPolicy? policy = null)
        {
            policy ??= DeviceClassificationPolicy.CreateDefault();
            var notes = new List<string>();

            if (capabilities == null)
            {
                notes.Add("Capabilities snapshot was null; defaulting conservatively to Low.");
                return new DeviceClassificationResult(
                    DevicePerformanceTier.Low,
                    0f, 0f, 0f, 0f, 0f,
                    "NullCapabilitiesFallback",
                    notes);
            }

            // 1. Evaluate RAM Score (0..100)
            float ramScore = EvaluateRamScore(capabilities, policy, notes);

            // 2. Evaluate GPU Capability Score (0..100)
            float gpuScore = EvaluateGpuScore(capabilities, notes, out bool isGpuConservativeOrLimited);

            // 3. Evaluate CPU Score (0..100)
            float cpuScore = EvaluateCpuScore(capabilities, notes);

            // 4. Evaluate Display Fill-Rate Cost Score (0..100, lower score = higher rendering burden)
            float displayScore = EvaluateDisplayCostScore(capabilities, gpuScore, policy, notes);

            // Normalize weights
            float totalWeight = policy.RamWeight + policy.GpuCapabilityWeight + policy.CpuWeight + policy.DisplayCostWeight;
            if (totalWeight <= 0.001f) totalWeight = 1f;

            float weightedScore =
                (ramScore * policy.RamWeight +
                 gpuScore * policy.GpuCapabilityWeight +
                 cpuScore * policy.CpuWeight +
                 displayScore * policy.DisplayCostWeight) / totalWeight;

            // Initial tier from weighted score
            DevicePerformanceTier rawTier;
            if (weightedScore >= policy.UltraTierMinScore)
                rawTier = DevicePerformanceTier.Ultra;
            else if (weightedScore >= policy.HighTierMinScore)
                rawTier = DevicePerformanceTier.High;
            else if (weightedScore >= policy.MediumTierMinScore)
                rawTier = DevicePerformanceTier.Medium;
            else
                rawTier = DevicePerformanceTier.Low;

            // Apply Conservative Safety Caps
            DevicePerformanceTier finalTier = rawTier;
            string safetyCap = string.Empty;

            // Safety Cap A: Unreliable or missing system RAM -> never exceed Medium (or Low if GPU is limited)
            if (!capabilities.HasReliableSystemMemory)
            {
                var maxFallback = isGpuConservativeOrLimited ? DevicePerformanceTier.Low : DevicePerformanceTier.Medium;
                if (finalTier > maxFallback)
                {
                    finalTier = maxFallback;
                    safetyCap = "UnreliableSystemMemoryCap";
                    notes.Add($"System RAM ({capabilities.SystemMemoryMB}MB) is missing/unreliable; capped at {finalTier}.");
                }
            }
            else
            {
                // Safety Cap B: <= LowRamHardCapMB (~3 GB) -> strictly Low to prevent Android OOM
                if (capabilities.SystemMemoryMB <= policy.LowRamHardCapMB && finalTier > DevicePerformanceTier.Low)
                {
                    finalTier = DevicePerformanceTier.Low;
                    safetyCap = "LowRamHardCap";
                    notes.Add($"System RAM ({capabilities.SystemMemoryMB}MB <= {policy.LowRamHardCapMB}MB); capped at Low.");
                }
                // Safety Cap C: <= MediumRamHardCapMB (~4.5 GB) -> max Medium
                else if (capabilities.SystemMemoryMB <= policy.MediumRamHardCapMB && finalTier > DevicePerformanceTier.Medium)
                {
                    finalTier = DevicePerformanceTier.Medium;
                    safetyCap = "MediumRamHardCap";
                    notes.Add($"System RAM ({capabilities.SystemMemoryMB}MB <= {policy.MediumRamHardCapMB}MB); capped at Medium.");
                }
                // Safety Cap D: Ultra requires >= UltraRamThresholdMB (~8 GB)
                else if (capabilities.SystemMemoryMB < policy.UltraRamThresholdMB && finalTier == DevicePerformanceTier.Ultra)
                {
                    finalTier = DevicePerformanceTier.High;
                    safetyCap = "RamBelowUltraThresholdCap";
                    notes.Add($"System RAM ({capabilities.SystemMemoryMB}MB < {policy.UltraRamThresholdMB}MB); capped at High.");
                }
            }

            // Safety Cap E: Large RAM alone cannot produce Ultra or High without matching GPU capability score
            if (finalTier == DevicePerformanceTier.Ultra && gpuScore < policy.MinGpuScoreForUltra)
            {
                finalTier = gpuScore >= policy.MinGpuScoreForHigh
                    ? DevicePerformanceTier.High
                    : DevicePerformanceTier.Medium;
                safetyCap = string.IsNullOrEmpty(safetyCap) ? "GpuCapabilityBelowUltraCap" : safetyCap;
                notes.Add($"GPU score ({gpuScore:F0}) is below Ultra minimum ({policy.MinGpuScoreForUltra:F0}); capped at {finalTier}.");
            }
            else if (finalTier == DevicePerformanceTier.High && gpuScore < policy.MinGpuScoreForHigh)
            {
                finalTier = DevicePerformanceTier.Medium;
                safetyCap = string.IsNullOrEmpty(safetyCap) ? "GpuCapabilityBelowHighCap" : safetyCap;
                notes.Add($"GPU score ({gpuScore:F0}) is below High minimum ({policy.MinGpuScoreForHigh:F0}); capped at Medium.");
            }

            // Safety Cap F: Missing basic graphics features -> force Low
            if (!capabilities.SupportsRenderTextures || capabilities.ShaderLevel < 30 ||
                (capabilities.MaxTextureSize > 0 && capabilities.MaxTextureSize < 2048))
            {
                if (finalTier > DevicePerformanceTier.Low)
                {
                    finalTier = DevicePerformanceTier.Low;
                    safetyCap = "LimitedGraphicsFeaturesCap";
                    notes.Add("Essential graphics features (RenderTextures, ShaderLevel>=30, or MaxTextureSize>=2048) are limited; capped at Low.");
                }
            }

            return new DeviceClassificationResult(
                finalTier,
                weightedScore,
                ramScore,
                gpuScore,
                cpuScore,
                displayScore,
                safetyCap,
                notes);
        }

        private static float EvaluateRamScore(
            DeviceCapabilities cap,
            DeviceClassificationPolicy policy,
            List<string> notes)
        {
            if (!cap.HasReliableSystemMemory)
            {
                notes.Add("SystemMemoryMB unavailable; using conservative baseline RAM score (35).");
                return 35f;
            }

            int ram = cap.SystemMemoryMB;
            if (ram <= 2048) return 15f;
            if (ram <= policy.LowRamHardCapMB) return 30f;
            if (ram <= 4096) return 52f;
            if (ram <= policy.MediumRamHardCapMB) return 60f;
            if (ram < policy.HighRamThresholdMB) return 72f;
            if (ram < policy.UltraRamThresholdMB) return 85f;
            return 95f;
        }

        private static float EvaluateGpuScore(
            DeviceCapabilities cap,
            List<string> notes,
            out bool isConservativeOrLimited)
        {
            isConservativeOrLimited = false;
            float score = 50f;

            // 1. Shader capability level (e.g., 30 = ES 3.0, 35 = ES 3.1, 45 = ES 3.1+AEP / Compute, 50 = Vulkan / SM5.0)
            if (cap.ShaderLevel >= 50)
                score += 22f;
            else if (cap.ShaderLevel >= 45)
                score += 14f;
            else if (cap.ShaderLevel >= 35)
                score += 4f;
            else
            {
                score -= 25f;
                isConservativeOrLimited = true;
            }

            // 2. Compute shaders, instancing, ASTC hardware support
            if (cap.SupportsComputeShaders)
                score += 10f;
            else
            {
                score -= 15f;
                isConservativeOrLimited = true;
            }

            if (cap.SupportsInstancing)
                score += 6f;
            else
                score -= 10f;

            if (cap.SupportsAstcCompression)
                score += 6f;
            else
                score -= 8f;

            // 3. Max texture size support
            if (cap.MaxTextureSize >= 16384)
                score += 8f;
            else if (cap.MaxTextureSize >= 8192)
                score += 4f;
            else if (cap.MaxTextureSize > 0 && cap.MaxTextureSize < 4096)
            {
                score -= 20f;
                isConservativeOrLimited = true;
            }

            // 4. Graphics API type (Vulkan / Metal indicate modern driver path)
            string api = cap.GraphicsApiType;
            if (api.IndexOf("Vulkan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                api.IndexOf("Metal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 6f;
            }
            else if (api.IndexOf("OpenGLES2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 25f;
                isConservativeOrLimited = true;
            }

            // 5. Broad GPU family hints (used only as a secondary modifier, never as the sole classifier)
            string gpuName = cap.GraphicsDeviceName;
            if (string.Equals(gpuName, "Unknown GPU", StringComparison.OrdinalIgnoreCase))
            {
                // Unreliable/missing GPU name -> cap GPU score so it cannot qualify for Ultra
                score = Math.Min(score, 62f);
                isConservativeOrLimited = true;
                notes.Add("GPU device name is unknown; applying conservative GPU score cap.");
            }
            else if (IsKnownEntryLevelMobileGpuHint(gpuName))
            {
                score = Math.Min(score, 38f);
                isConservativeOrLimited = true;
                notes.Add($"Entry-level GPU family detected ('{gpuName}'); applying conservative GPU score.");
            }

            return Math.Clamp(score, 0f, 100f);
        }

        private static bool IsKnownEntryLevelMobileGpuHint(string gpuName)
        {
            // Broad architectural families known to be entry-level / low fill-rate on Android
            return gpuName.IndexOf("Mali-4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Mali-T", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Mali-G3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Mali-G51", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Mali-G52", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("PowerVR Rogue GE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno (TM) 3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno (TM) 4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno (TM) 50", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno 3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno 4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("Adreno 50", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("SwiftShader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   gpuName.IndexOf("llvmpipe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float EvaluateCpuScore(DeviceCapabilities cap, List<string> notes)
        {
            if (!cap.HasReliableCpuCount)
            {
                notes.Add("CPU core count unavailable; using conservative CPU score (45).");
                return 45f;
            }

            // Do not assume core count alone equals high performance (many budget Android SoCs have 8 A53 cores)
            float score;
            if (cap.ProcessorCount <= 2)
                score = 20f;
            else if (cap.ProcessorCount <= 4)
                score = 40f;
            else if (cap.ProcessorCount <= 6)
                score = 65f;
            else
                score = 72f; // 8+ cores baseline is mid-range until frequency/GPU corroborate

            if (cap.HasReliableCpuFrequency)
            {
                if (cap.ProcessorFrequencyMHz >= 2800)
                    score += 20f;
                else if (cap.ProcessorFrequencyMHz >= 2300)
                    score += 12f;
                else if (cap.ProcessorFrequencyMHz < 1800)
                    score -= 15f;
            }

            return Math.Clamp(score, 0f, 100f);
        }

        private static float EvaluateDisplayCostScore(
            DeviceCapabilities cap,
            float gpuScore,
            DeviceClassificationPolicy policy,
            List<string> notes)
        {
            if (!cap.HasReliableScreenDimensions)
            {
                return 65f;
            }

            long pixels = cap.TotalScreenPixels;
            if (pixels >= policy.UltraHighResPixels)
            {
                // Very high pixel count (e.g., 1440x3200 QHD+) increases fill-rate pressure
                notes.Add($"Ultra-high display resolution ({cap.ScreenWidth}x{cap.ScreenHeight}); accounting for fill-rate load.");
                return gpuScore >= 80f ? 75f : 45f;
            }

            if (pixels >= policy.HighResQhdPixels)
            {
                return gpuScore >= 75f ? 80f : 55f;
            }

            if (pixels <= policy.StandardFhdPixels)
            {
                return 80f;
            }

            return 72f; // Typical FHD+ (e.g., 2400x1080)
        }
    }
}
