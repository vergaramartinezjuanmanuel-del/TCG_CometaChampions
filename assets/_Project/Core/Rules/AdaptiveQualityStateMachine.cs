#nullable enable
using System;
using System.Collections.Generic;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Configurable parameters governing runtime adaptive quality monitoring and hysteresis.
    /// </summary>
    [Serializable]
    public sealed class AdaptiveQualityConfig
    {
        public int SampleCapacity { get; set; } = 120;
        public int MinSamplesForEvaluation { get; set; } = 45;
        public float EvaluationIntervalSeconds { get; set; } = 3.0f;
        public float DegradeStabilizationSeconds { get; set; } = 5.0f;
        public float UpgradeStabilizationSeconds { get; set; } = 15.0f;

        /// <summary>
        /// Sustained average frame time multiplier above target (e.g. 1.28x of 16.67ms = 21.3ms / ~46.8 FPS) required to trigger a step-down.
        /// </summary>
        public float DegradeAvgMultiplier { get; set; } = 1.28f;

        /// <summary>
        /// P95 frame time multiplier above target required alongside average degradation to confirm sustained pressure rather than a single spike.
        /// </summary>
        public float DegradeP95Multiplier { get; set; } = 1.40f;

        /// <summary>
        /// Average frame time multiplier below which performance is considered comfortably stable for recovery.
        /// </summary>
        public float UpgradeAvgMultiplier { get; set; } = 1.06f;

        /// <summary>
        /// P95 frame time multiplier required for recovery.
        /// </summary>
        public float UpgradeP95Multiplier { get; set; } = 1.15f;

        /// <summary>
        /// Single frame spikes above this threshold (e.g. asset load hitch) are clamped so one isolated spike cannot skew the rolling average.
        /// </summary>
        public float MaxClampedFrameSpikeMs { get; set; } = 100f;

        public float RenderScaleStep { get; set; } = 0.05f;

        public static AdaptiveQualityConfig CreateDefault() => new();
    }

    /// <summary>
    /// Allocation-free ring buffer for tracking rolling average and P95 frame times in milliseconds.
    /// </summary>
    public sealed class FrameTimeSampleBuffer
    {
        private readonly float[] samples;
        private readonly float[] sortScratch;
        private int writeIndex;
        private int count;

        public int Count => count;
        public int Capacity => samples.Length;

        public FrameTimeSampleBuffer(int capacity = 120)
        {
            int safeCapacity = Math.Max(16, capacity);
            samples = new float[safeCapacity];
            sortScratch = new float[safeCapacity];
        }

        public void AddSample(float frameTimeMs, float maxClampMs = 100f)
        {
            if (float.IsNaN(frameTimeMs) || float.IsInfinity(frameTimeMs) || frameTimeMs <= 0f)
            {
                return;
            }

            float clamped = Math.Min(frameTimeMs, maxClampMs);
            samples[writeIndex] = clamped;
            writeIndex = (writeIndex + 1) % samples.Length;
            if (count < samples.Length)
            {
                count++;
            }
        }

        public void Clear()
        {
            writeIndex = 0;
            count = 0;
        }

        public float GetAverageMs()
        {
            if (count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < count; i++)
            {
                sum += samples[i];
            }
            return sum / count;
        }

        public float GetPercentileMs(float percentile = 0.95f)
        {
            if (count == 0) return 0f;
            for (int i = 0; i < count; i++)
            {
                sortScratch[i] = samples[i];
            }
            Array.Sort(sortScratch, 0, count);
            int idx = (int)Math.Ceiling(Math.Clamp(percentile, 0f, 1f) * count) - 1;
            idx = Math.Clamp(idx, 0, count - 1);
            return sortScratch[idx];
        }
    }

    /// <summary>
    /// Conservative runtime adaptation state machine with hysteresis, stabilization timers,
    /// and TCG animation deferral so quality adjustments never interrupt active card animations.
    /// </summary>
    public sealed class AdaptiveQualityStateMachine
    {
        private readonly AdaptiveQualityConfig config;
        private readonly FrameTimeSampleBuffer sampleBuffer;

        private QualityProfileData ceilingProfile;
        private QualityProfileData currentProfile;

        private float timeSinceLastEvaluation;
        private float stabilizationCooldownRemaining;
        private float continuousStableTime;
        private bool hasDeferredDegradation;

        private readonly List<string> adjustmentHistory = new();

        public QualityProfileData CeilingProfile => ceilingProfile;
        public QualityProfileData CurrentProfile => currentProfile;
        public FrameTimeSampleBuffer SampleBuffer => sampleBuffer;
        public int ActiveDegradationSteps { get; private set; }
        public string LastAdjustmentReason { get; private set; } = "Initial profile applied";
        public IReadOnlyList<string> AdjustmentHistory => adjustmentHistory;
        public bool HasPendingDeferredAdjustment => hasDeferredDegradation;

        public AdaptiveQualityStateMachine(
            QualityProfileData initialProfile,
            AdaptiveQualityConfig? config = null)
        {
            this.config = config ?? AdaptiveQualityConfig.CreateDefault();
            sampleBuffer = new FrameTimeSampleBuffer(this.config.SampleCapacity);
            ceilingProfile = initialProfile.Clone();
            ceilingProfile.ValidateAndClamp();
            currentProfile = ceilingProfile.Clone();
        }

        /// <summary>
        /// Resets the state machine to a new base ceiling profile (e.g., when the user changes quality mode).
        /// </summary>
        public void ResetToProfile(QualityProfileData newCeilingProfile, string reason = "Profile reset")
        {
            ceilingProfile = newCeilingProfile.Clone();
            ceilingProfile.ValidateAndClamp();
            currentProfile = ceilingProfile.Clone();
            sampleBuffer.Clear();
            timeSinceLastEvaluation = 0f;
            stabilizationCooldownRemaining = 0f;
            continuousStableTime = 0f;
            hasDeferredDegradation = false;
            ActiveDegradationSteps = 0;
            LastAdjustmentReason = reason;
            RecordHistory(reason);
        }

        /// <summary>
        /// Records a frame delta time and evaluates whether a single incremental quality adjustment is warranted.
        /// Returns true if CurrentProfile was modified on this tick.
        /// </summary>
        public bool RecordFrameAndEvaluate(
            float unscaledDeltaTimeSeconds,
            bool isSceneLoading = false,
            bool isCriticalAnimationActive = false,
            bool isMenuIdleScene = false)
        {
            if (isSceneLoading || unscaledDeltaTimeSeconds <= 0f || float.IsNaN(unscaledDeltaTimeSeconds))
            {
                return false;
            }

            // If a degradation was deferred during a critical card/combat animation and the animation just finished, apply it now
            if (hasDeferredDegradation && !isCriticalAnimationActive)
            {
                hasDeferredDegradation = false;
                if (TryDegradeOneStep(out string deferredReason))
                {
                    stabilizationCooldownRemaining = config.DegradeStabilizationSeconds;
                    continuousStableTime = 0f;
                    sampleBuffer.Clear();
                    LastAdjustmentReason = $"{deferredReason} (Applied after animation)";
                    RecordHistory(LastAdjustmentReason);
                    return true;
                }
            }

            float frameTimeMs = unscaledDeltaTimeSeconds * 1000f;
            sampleBuffer.AddSample(frameTimeMs, config.MaxClampedFrameSpikeMs);

            if (stabilizationCooldownRemaining > 0f)
            {
                stabilizationCooldownRemaining = Math.Max(0f, stabilizationCooldownRemaining - unscaledDeltaTimeSeconds);
            }

            timeSinceLastEvaluation += unscaledDeltaTimeSeconds;
            if (timeSinceLastEvaluation < config.EvaluationIntervalSeconds)
            {
                return false;
            }

            float evalDelta = timeSinceLastEvaluation;
            timeSinceLastEvaluation = 0f;

            if (sampleBuffer.Count < config.MinSamplesForEvaluation)
            {
                return false;
            }

            float targetFrameTimeMs = 1000f / Math.Max(30, currentProfile.TargetFrameRate);
            float avgMs = sampleBuffer.GetAverageMs();
            float p95Ms = sampleBuffer.GetPercentileMs(0.95f);

            bool isSustainedDegradation =
                avgMs > targetFrameTimeMs * config.DegradeAvgMultiplier &&
                p95Ms > targetFrameTimeMs * config.DegradeP95Multiplier;

            if (isSustainedDegradation)
            {
                continuousStableTime = 0f;

                if (stabilizationCooldownRemaining > 0f)
                {
                    return false;
                }

                // TCG rule: Defer quality changes during critical gameplay animations to prevent visual hitches
                if (isCriticalAnimationActive)
                {
                    hasDeferredDegradation = true;
                    LastAdjustmentReason = $"Deferred degradation during active animation (avg={avgMs:F1}ms, p95={p95Ms:F1}ms)";
                    return false;
                }

                if (TryDegradeOneStep(out string stepReason))
                {
                    stabilizationCooldownRemaining = config.DegradeStabilizationSeconds;
                    sampleBuffer.Clear();
                    LastAdjustmentReason = $"{stepReason} (avg={avgMs:F1}ms, p95={p95Ms:F1}ms, target={targetFrameTimeMs:F1}ms)";
                    RecordHistory(LastAdjustmentReason);
                    return true;
                }

                return false;
            }

            // Check for sustained stability to recover toward CeilingProfile
            // TCG rule: Do not confuse low activity in a static menu with proof that the device can upgrade quality for battle
            bool isComfortablyStable =
                !isMenuIdleScene &&
                ActiveDegradationSteps > 0 &&
                stabilizationCooldownRemaining <= 0f &&
                avgMs <= targetFrameTimeMs * config.UpgradeAvgMultiplier &&
                p95Ms <= targetFrameTimeMs * config.UpgradeP95Multiplier;

            if (isComfortablyStable)
            {
                continuousStableTime += evalDelta;
                if (continuousStableTime >= config.UpgradeStabilizationSeconds && !isCriticalAnimationActive)
                {
                    if (TryUpgradeOneStep(out string upgradeReason))
                    {
                        continuousStableTime = 0f;
                        stabilizationCooldownRemaining = config.DegradeStabilizationSeconds;
                        sampleBuffer.Clear();
                        LastAdjustmentReason = $"{upgradeReason} (stable avg={avgMs:F1}ms, p95={p95Ms:F1}ms)";
                        RecordHistory(LastAdjustmentReason);
                        return true;
                    }
                }
            }
            else
            {
                continuousStableTime = 0f;
            }

            return false;
        }

        /// <summary>
        /// Steps down exactly one graphics setting in priority order:
        /// 1. Post-processing & ambient card VFX
        /// 2. Particle & effect concurrency budget
        /// 3. Shadow quality & anti-aliasing
        /// 4. Render scale (down to MinRenderScale, never blurring UI/cards)
        /// </summary>
        private bool TryDegradeOneStep(out string reason)
        {
            // Step 1: Expensive post-processing & optional ambient VFX
            if (currentProfile.PostProcessingEnabled || currentProfile.EnableAmbientCardVfx)
            {
                currentProfile.PostProcessingEnabled = false;
                currentProfile.EnableAmbientCardVfx = false;
                currentProfile.EnableCardParallaxTilt = false;
                ActiveDegradationSteps++;
                reason = "Step-down [1/4]: Disabled Post-Processing & Ambient Card VFX";
                return true;
            }

            // Step 2: Particle & effect concurrency limits
            float minParticleScale = 0.25f;
            if (currentProfile.ParticleBudgetScale > minParticleScale + 0.01f || currentProfile.MaxConcurrentVisualEffects > 1)
            {
                currentProfile.ParticleBudgetScale = Math.Max(minParticleScale, currentProfile.ParticleBudgetScale - 0.30f);
                currentProfile.MaxConcurrentVisualEffects = Math.Max(1, currentProfile.MaxConcurrentVisualEffects - 2);
                ActiveDegradationSteps++;
                reason = $"Step-down [2/4]: Reduced Particle Budget ({currentProfile.ParticleBudgetScale:P0}) & MaxConcurrentVFX ({currentProfile.MaxConcurrentVisualEffects})";
                return true;
            }

            // Step 3: Shadow distance, shadow quality & MSAA
            if (currentProfile.ShadowsEnabled || currentProfile.AntiAliasingSamples > 0)
            {
                currentProfile.ShadowsEnabled = false;
                currentProfile.ShadowDistance = 0f;
                currentProfile.AntiAliasingSamples = 0;
                ActiveDegradationSteps++;
                reason = "Step-down [3/4]: Disabled Shadows & MSAA";
                return true;
            }

            // Step 4: Render scale within safe configured bounds
            if (currentProfile.RenderScale > currentProfile.MinRenderScale + 0.01f)
            {
                currentProfile.RenderScale = Math.Max(
                    currentProfile.MinRenderScale,
                    (float)Math.Round(currentProfile.RenderScale - config.RenderScaleStep, 2));
                ActiveDegradationSteps++;
                reason = $"Step-down [4/4]: Reduced 3D Render Scale to {currentProfile.RenderScale:F2} (Min={currentProfile.MinRenderScale:F2})";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        /// <summary>
        /// Restores one graphics setting in reverse order, strictly bounded by CeilingProfile.
        /// </summary>
        private bool TryUpgradeOneStep(out string reason)
        {
            // Reverse Step 4: Restore Render Scale first
            if (currentProfile.RenderScale + 0.01f < ceilingProfile.RenderScale)
            {
                currentProfile.RenderScale = Math.Min(
                    ceilingProfile.RenderScale,
                    (float)Math.Round(currentProfile.RenderScale + config.RenderScaleStep, 2));
                ActiveDegradationSteps = Math.Max(0, ActiveDegradationSteps - 1);
                reason = $"Step-up: Restored 3D Render Scale to {currentProfile.RenderScale:F2}";
                return true;
            }

            // Reverse Step 3: Restore Shadows & MSAA
            if ((ceilingProfile.ShadowsEnabled && !currentProfile.ShadowsEnabled) ||
                currentProfile.AntiAliasingSamples < ceilingProfile.AntiAliasingSamples)
            {
                currentProfile.ShadowsEnabled = ceilingProfile.ShadowsEnabled;
                currentProfile.ShadowDistance = ceilingProfile.ShadowDistance;
                currentProfile.ShadowResolutionTier = ceilingProfile.ShadowResolutionTier;
                currentProfile.AntiAliasingSamples = ceilingProfile.AntiAliasingSamples;
                ActiveDegradationSteps = Math.Max(0, ActiveDegradationSteps - 1);
                reason = "Step-up: Restored Shadows & MSAA";
                return true;
            }

            // Reverse Step 2: Restore Particle & VFX budgets
            if (currentProfile.ParticleBudgetScale + 0.01f < ceilingProfile.ParticleBudgetScale ||
                currentProfile.MaxConcurrentVisualEffects < ceilingProfile.MaxConcurrentVisualEffects)
            {
                currentProfile.ParticleBudgetScale = ceilingProfile.ParticleBudgetScale;
                currentProfile.MaxConcurrentVisualEffects = ceilingProfile.MaxConcurrentVisualEffects;
                ActiveDegradationSteps = Math.Max(0, ActiveDegradationSteps - 1);
                reason = $"Step-up: Restored Particle Budget ({currentProfile.ParticleBudgetScale:P0})";
                return true;
            }

            // Reverse Step 1: Restore Post-processing & Ambient Card VFX
            if ((ceilingProfile.PostProcessingEnabled && !currentProfile.PostProcessingEnabled) ||
                (ceilingProfile.EnableAmbientCardVfx && !currentProfile.EnableAmbientCardVfx))
            {
                currentProfile.PostProcessingEnabled = ceilingProfile.PostProcessingEnabled;
                currentProfile.EnableAmbientCardVfx = ceilingProfile.EnableAmbientCardVfx;
                currentProfile.EnableCardParallaxTilt = ceilingProfile.EnableCardParallaxTilt;
                ActiveDegradationSteps = 0;
                reason = "Step-up: Restored Post-Processing & Ambient Card VFX";
                return true;
            }

            ActiveDegradationSteps = 0;
            reason = string.Empty;
            return false;
        }

        private void RecordHistory(string entry)
        {
            if (adjustmentHistory.Count >= 24)
            {
                adjustmentHistory.RemoveAt(0);
            }
            adjustmentHistory.Add(entry);
        }
    }
}
