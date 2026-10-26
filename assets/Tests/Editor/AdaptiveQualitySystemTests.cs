#nullable enable
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class AdaptiveQualitySystemTests
    {
        // =====================================================================
        // 1. Device Validation Matrix (6 Required Android Archetypes)
        // =====================================================================

        [Test]
        public void Matrix1_LowEndAndroidDevice_ClassifiedAsLow()
        {
            // Entry-level Android: 2 GB RAM, 4 cores @ 1.4 GHz, Mali-G31, 720x1600, OpenGLES3 (Shader 35, no compute)
            var cap = new DeviceCapabilities(
                systemMemoryMB: 2048,
                graphicsMemoryMB: 512,
                processorCount: 4,
                processorFrequencyMHz: 1400,
                graphicsDeviceName: "Mali-G31",
                graphicsDeviceVendor: "ARM",
                graphicsApiType: "OpenGLES3",
                maxTextureSize: 4096,
                screenWidth: 720,
                screenHeight: 1600,
                screenDpi: 260f,
                shaderLevel: 35,
                supportsComputeShaders: false,
                supportsInstancing: true,
                supportsAstcCompression: true);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Low, result.Tier);
        }

        [Test]
        public void Matrix2_MidRangeAndroidDevice_ClassifiedAsMedium()
        {
            // Mid-range Android: 4 GB RAM, 8 cores @ 2.0 GHz, Adreno 619, 1080x2400, Vulkan
            var cap = new DeviceCapabilities(
                systemMemoryMB: 4096,
                graphicsMemoryMB: 1024,
                processorCount: 8,
                processorFrequencyMHz: 2000,
                graphicsDeviceName: "Adreno (TM) 619",
                graphicsDeviceVendor: "Qualcomm",
                graphicsApiType: "Vulkan",
                maxTextureSize: 8192,
                screenWidth: 1080,
                screenHeight: 2400,
                screenDpi: 400f,
                shaderLevel: 45,
                supportsComputeShaders: true,
                supportsInstancing: true,
                supportsAstcCompression: true);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Medium, result.Tier);
            Assert.AreEqual("MediumRamHardCap", result.SafetyCapApplied);
        }

        [Test]
        public void Matrix3_HighEndAndroidDevice_ClassifiedAsHigh()
        {
            // Upper mid / High-end Android: 6 GB RAM, 8 cores @ 2.4 GHz, Adreno 642L, 1080x2400, OpenGLES3 (Shader 45)
            var cap = new DeviceCapabilities(
                systemMemoryMB: 6144,
                graphicsMemoryMB: 2048,
                processorCount: 8,
                processorFrequencyMHz: 2400,
                graphicsDeviceName: "Adreno (TM) 642L",
                graphicsDeviceVendor: "Qualcomm",
                graphicsApiType: "OpenGLES3",
                maxTextureSize: 8192,
                screenWidth: 1080,
                screenHeight: 2400,
                screenDpi: 405f,
                shaderLevel: 45,
                supportsComputeShaders: true,
                supportsInstancing: true,
                supportsAstcCompression: true);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.High, result.Tier);
        }

        [Test]
        public void Matrix4_FlagshipHighResolutionDevice_ClassifiedAsUltra()
        {
            // Flagship Android (e.g. Galaxy S25+): 12 GB RAM, 8 cores @ 3.3 GHz, Adreno 830, 1440x3120 QHD+, Vulkan (Shader 50)
            var cap = new DeviceCapabilities(
                systemMemoryMB: 12288,
                graphicsMemoryMB: 4096,
                processorCount: 8,
                processorFrequencyMHz: 3300,
                graphicsDeviceName: "Adreno (TM) 830",
                graphicsDeviceVendor: "Qualcomm",
                graphicsApiType: "Vulkan",
                maxTextureSize: 16384,
                screenWidth: 1440,
                screenHeight: 3120,
                screenDpi: 513f,
                shaderLevel: 50,
                supportsComputeShaders: true,
                supportsInstancing: true,
                supportsAstcCompression: true);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Ultra, result.Tier);
        }

        [Test]
        public void Matrix5_DeviceWithLimitedGraphicsCapabilities_ForcedToLowDespiteHighRam()
        {
            // 8 GB RAM, but limited GPU (ShaderLevel 25, MaxTextureSize 1024, no Compute)
            var cap = new DeviceCapabilities(
                systemMemoryMB: 8192,
                graphicsMemoryMB: 1024,
                processorCount: 8,
                processorFrequencyMHz: 2400,
                graphicsDeviceName: "Legacy Mobile GPU",
                graphicsDeviceVendor: "Generic",
                graphicsApiType: "OpenGLES2",
                maxTextureSize: 1024,
                screenWidth: 1080,
                screenHeight: 1920,
                screenDpi: 320f,
                shaderLevel: 25,
                supportsComputeShaders: false,
                supportsInstancing: false,
                supportsAstcCompression: false);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Low, result.Tier);
            Assert.AreEqual("LimitedGraphicsFeaturesCap", result.SafetyCapApplied);
        }

        [Test]
        public void Matrix6_IncompleteOrUnreliableHardwareData_FallsBackConservatively()
        {
            // Negative / zero / missing hardware values
            var cap = new DeviceCapabilities(
                systemMemoryMB: -1,
                graphicsMemoryMB: 0,
                processorCount: 0,
                processorFrequencyMHz: -500,
                graphicsDeviceName: "",
                graphicsDeviceVendor: null,
                graphicsApiType: null,
                maxTextureSize: 0,
                screenWidth: 0,
                screenHeight: 0,
                screenDpi: float.NaN,
                shaderLevel: 35,
                supportsComputeShaders: false);

            Assert.IsFalse(cap.HasReliableSystemMemory);
            Assert.IsFalse(cap.HasReliableGpuMemory);
            Assert.IsFalse(cap.HasReliableCpuCount);
            Assert.IsFalse(cap.HasReliableCpuFrequency);
            Assert.AreEqual(0f, cap.ScreenDpi);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Low, result.Tier);
        }

        // =====================================================================
        // 2. Boundary Conditions & RAM-Alone Anti-Inflation Rule
        // =====================================================================

        [Test]
        public void HighRamAlone_WithWeakEntryLevelGpu_DoesNotClassifyAsUltraOrHigh()
        {
            // 12 GB RAM budget tablet with entry-level Mali-G52 GPU
            var cap = new DeviceCapabilities(
                systemMemoryMB: 12288,
                graphicsMemoryMB: 2048,
                processorCount: 8,
                processorFrequencyMHz: 2000,
                graphicsDeviceName: "Mali-G52 MC2",
                graphicsDeviceVendor: "ARM",
                graphicsApiType: "OpenGLES3",
                maxTextureSize: 4096,
                screenWidth: 1200,
                screenHeight: 2000,
                screenDpi: 240f,
                shaderLevel: 35,
                supportsComputeShaders: false);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.LessOrEqual(result.Tier, DevicePerformanceTier.Medium, "High RAM with weak GPU must not be classified as High or Ultra.");
        }

        [Test]
        public void BoundaryRam_3072MB_IsStrictlyCappedAtLow()
        {
            var cap = new DeviceCapabilities(
                systemMemoryMB: 3072,
                graphicsMemoryMB: 2048,
                processorCount: 8,
                processorFrequencyMHz: 2800,
                graphicsDeviceName: "Adreno (TM) 730",
                graphicsDeviceVendor: "Qualcomm",
                graphicsApiType: "Vulkan",
                maxTextureSize: 16384,
                screenWidth: 1080,
                screenHeight: 2400,
                screenDpi: 400f,
                shaderLevel: 50,
                supportsComputeShaders: true);

            var result = DevicePerformanceClassifier.Classify(cap);
            Assert.AreEqual(DevicePerformanceTier.Low, result.Tier);
            Assert.AreEqual("LowRamHardCap", result.SafetyCapApplied);
        }

        // =====================================================================
        // 3. Quality Profiles & TCG Card Readability Protection
        // =====================================================================

        [Test]
        public void AllQualityProfiles_ProtectCardReadability_WithZeroMipmapLimitAndValidBounds()
        {
            foreach (DevicePerformanceTier tier in System.Enum.GetValues(typeof(DevicePerformanceTier)))
            {
                var profile = QualityProfileCatalog.GetProfile(tier);
                Assert.IsNotNull(profile);
                Assert.AreEqual(0, profile.TextureMipmapLimit, $"Tier {tier} must keep TextureMipmapLimit at 0 to protect card art clarity.");
                Assert.GreaterOrEqual(profile.RenderScale, 0.70f);
                Assert.LessOrEqual(profile.RenderScale, 1.00f);
                Assert.GreaterOrEqual(profile.MinRenderScale, 0.70f);
            }

            Assert.AreEqual(30, QualityProfileCatalog.GetProfile(DevicePerformanceTier.Low).TargetFrameRate);
            Assert.AreEqual(60, QualityProfileCatalog.GetProfile(DevicePerformanceTier.Medium).TargetFrameRate);
            Assert.AreEqual(60, QualityProfileCatalog.GetProfile(DevicePerformanceTier.High).TargetFrameRate);
            Assert.AreEqual(60, QualityProfileCatalog.GetProfile(DevicePerformanceTier.Ultra).TargetFrameRate);
        }

        // =====================================================================
        // 4. User Overrides & Preference Parsing Validation
        // =====================================================================

        [Test]
        public void ParseModeSafe_HandlesValidInvalidAndEmptyPreferences()
        {
            Assert.AreEqual(GraphicsQualityMode.Auto, QualityProfileCatalog.ParseModeSafe(null));
            Assert.AreEqual(GraphicsQualityMode.Auto, QualityProfileCatalog.ParseModeSafe(""));
            Assert.AreEqual(GraphicsQualityMode.Auto, QualityProfileCatalog.ParseModeSafe("INVALID_VALUE"));
            Assert.AreEqual(GraphicsQualityMode.Low, QualityProfileCatalog.ParseModeSafe("low"));
            Assert.AreEqual(GraphicsQualityMode.Medium, QualityProfileCatalog.ParseModeSafe("Medium"));
            Assert.AreEqual(GraphicsQualityMode.High, QualityProfileCatalog.ParseModeSafe("HIGH"));
            Assert.AreEqual(GraphicsQualityMode.Ultra, QualityProfileCatalog.ParseModeSafe("Ultra"));
        }

        [Test]
        public void ResolveEffectiveTier_UserSelectionOverridesAutoClassifier()
        {
            Assert.AreEqual(DevicePerformanceTier.Low, QualityProfileCatalog.ResolveEffectiveTier(GraphicsQualityMode.Auto, DevicePerformanceTier.Low));
            Assert.AreEqual(DevicePerformanceTier.High, QualityProfileCatalog.ResolveEffectiveTier(GraphicsQualityMode.High, DevicePerformanceTier.Low));
            Assert.AreEqual(DevicePerformanceTier.Low, QualityProfileCatalog.ResolveEffectiveTier(GraphicsQualityMode.Low, DevicePerformanceTier.Ultra));
        }

        // =====================================================================
        // 5. Adaptive State Machine: Hysteresis, Ordering, Limits & Deferral
        // =====================================================================

        [Test]
        public void AdaptiveStateMachine_DegradesInStrictPriorityOrder_AndRespectsFloorLimits()
        {
            var ultraProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.Ultra);
            var cfg = new AdaptiveQualityConfig
            {
                SampleCapacity = 60,
                MinSamplesForEvaluation = 10,
                EvaluationIntervalSeconds = 1.0f,
                DegradeStabilizationSeconds = 2.0f,
                UpgradeStabilizationSeconds = 5.0f,
                RenderScaleStep = 0.05f
            };

            var sm = new AdaptiveQualityStateMachine(ultraProfile, cfg);

            // Simulate sustained slow frames (28ms per frame on a 60 FPS / 16.67ms target)
            bool step1Triggered = false;
            for (int i = 0; i < 40; i++)
            {
                if (sm.RecordFrameAndEvaluate(0.028f))
                {
                    step1Triggered = true;
                    break;
                }
            }

            // Step 1: Post-processing & Ambient Card VFX disabled first; Shadows & RenderScale untouched
            Assert.IsTrue(step1Triggered, "Step 1 degradation should trigger after sustained slow frames.");
            Assert.IsFalse(sm.CurrentProfile.PostProcessingEnabled);
            Assert.IsFalse(sm.CurrentProfile.EnableAmbientCardVfx);
            Assert.IsTrue(sm.CurrentProfile.ShadowsEnabled, "Shadows should still be enabled after Step 1.");
            Assert.AreEqual(1.00f, sm.CurrentProfile.RenderScale, 0.001f);
            Assert.AreEqual(0, sm.CurrentProfile.TextureMipmapLimit, "Card texture quality must never be degraded.");

            // Advance past stabilization cooldown (2.0s) with continued slow frames -> Step 2 (Particle budget)
            bool step2Triggered = false;
            for (int i = 0; i < 100; i++)
            {
                if (sm.RecordFrameAndEvaluate(0.028f))
                {
                    step2Triggered = true;
                    break;
                }
            }
            Assert.IsTrue(step2Triggered, "Step 2 degradation (Particle budget) should trigger after stabilization interval.");
            Assert.Less(sm.CurrentProfile.ParticleBudgetScale, 1.00f);

            // Keep degrading all the way to floor limits and verify MinRenderScale and TextureMipmapLimit are respected
            for (int i = 0; i < 600; i++)
            {
                sm.RecordFrameAndEvaluate(0.030f);
            }

            Assert.GreaterOrEqual(sm.CurrentProfile.RenderScale, ultraProfile.MinRenderScale - 0.001f);
            Assert.AreEqual(0, sm.CurrentProfile.TextureMipmapLimit);
        }

        [Test]
        public void AdaptiveStateMachine_DefersAdjustmentDuringCriticalBattleAnimation()
        {
            var highProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.High);
            var cfg = new AdaptiveQualityConfig
            {
                SampleCapacity = 30,
                MinSamplesForEvaluation = 10,
                EvaluationIntervalSeconds = 0.5f,
                DegradeStabilizationSeconds = 1.0f
            };

            var sm = new AdaptiveQualityStateMachine(highProfile, cfg);

            // Feed sustained slow frames while isCriticalAnimationActive = true
            bool changedDuringAnim = false;
            for (int i = 0; i < 30; i++)
            {
                if (sm.RecordFrameAndEvaluate(0.028f, isSceneLoading: false, isCriticalAnimationActive: true))
                {
                    changedDuringAnim = true;
                }
            }

            Assert.IsFalse(changedDuringAnim, "Must not apply quality change mid-animation.");
            Assert.IsTrue(sm.HasPendingDeferredAdjustment, "Degradation should be queued until animation finishes.");

            // First frame after animation ends should apply the deferred step-down cleanly
            bool appliedAfterAnim = sm.RecordFrameAndEvaluate(0.016f, isSceneLoading: false, isCriticalAnimationActive: false);
            Assert.IsTrue(appliedAfterAnim, "Queued degradation should apply immediately once critical animation completes.");
            Assert.IsFalse(sm.HasPendingDeferredAdjustment);
        }

        [Test]
        public void AdaptiveStateMachine_DoesNotUpgradeInMenuIdle_AndRecoversInBattleOnlyUpToCeiling()
        {
            var highProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.High);
            var cfg = new AdaptiveQualityConfig
            {
                SampleCapacity = 30,
                MinSamplesForEvaluation = 10,
                EvaluationIntervalSeconds = 0.5f,
                DegradeStabilizationSeconds = 0.5f,
                UpgradeStabilizationSeconds = 1.5f
            };

            var sm = new AdaptiveQualityStateMachine(highProfile, cfg);

            // Trigger 1 step of degradation
            for (int i = 0; i < 30; i++)
            {
                if (sm.RecordFrameAndEvaluate(0.028f)) break;
            }
            Assert.AreEqual(1, sm.ActiveDegradationSteps);

            // Fast frames in Menu (isMenuIdleScene = true) must NOT trigger an upgrade
            for (int i = 0; i < 200; i++)
            {
                sm.RecordFrameAndEvaluate(0.012f, isSceneLoading: false, isCriticalAnimationActive: false, isMenuIdleScene: true);
            }
            Assert.AreEqual(1, sm.ActiveDegradationSteps, "Low activity in a menu must not trigger quality upgrades.");

            // Fast frames in active Battle (isMenuIdleScene = false) for >= UpgradeStabilizationSeconds recovers back to ceiling
            for (int i = 0; i < 200; i++)
            {
                sm.RecordFrameAndEvaluate(0.014f, isSceneLoading: false, isCriticalAnimationActive: false, isMenuIdleScene: false);
            }
            Assert.AreEqual(0, sm.ActiveDegradationSteps, "Sustained smooth frames in battle should recover back to ceiling profile.");
            Assert.AreEqual(highProfile.RenderScale, sm.CurrentProfile.RenderScale, 0.001f);
        }
    }
}
