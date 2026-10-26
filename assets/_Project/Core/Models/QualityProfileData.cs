#nullable enable
using System;
using System.Collections.Generic;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Centralized quality configuration profile for a specific DevicePerformanceTier.
    /// Keeps profile configuration decoupled from device classification and runtime application.
    /// Protects TCG card readability and UI clarity by keeping TextureMipmapLimit at 0.
    /// </summary>
    [Serializable]
    public sealed class QualityProfileData
    {
        public DevicePerformanceTier Tier { get; set; }
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Index in ProjectSettings/QualitySettings.asset (1=Low, 2=Medium, 3=High, 5=Ultra).
        /// Level 0 (Very Low) is intentionally skipped because it sets globalTextureMipmapLimit=1.
        /// </summary>
        public int UnityQualityLevelIndex { get; set; }

        public int TargetFrameRate { get; set; }
        public int MenuTargetFrameRate { get; set; }
        public int BackgroundTargetFrameRate { get; set; } = 15;

        public float RenderScale { get; set; }
        public float MinRenderScale { get; set; }

        public bool ShadowsEnabled { get; set; }
        public int ShadowResolutionTier { get; set; }
        public float ShadowDistance { get; set; }

        /// <summary>
        /// MSAA sample count (0, 2, or 4).
        /// </summary>
        public int AntiAliasingSamples { get; set; }

        /// <summary>
        /// Kept at 0 across all tiers so card illustrations and UI icons never blur.
        /// </summary>
        public int TextureMipmapLimit { get; set; }

        /// <summary>
        /// 0 = Disable, 1 = Enable (Per Texture), 2 = ForceEnable.
        /// </summary>
        public int AnisotropicFilteringMode { get; set; }

        public float ParticleBudgetScale { get; set; }
        public int ParticleRaycastBudget { get; set; }
        public bool PostProcessingEnabled { get; set; }
        public int MaxConcurrentVisualEffects { get; set; }
        public bool EnableAmbientCardVfx { get; set; }
        public bool EnableCardParallaxTilt { get; set; }

        /// <summary>
        /// Maximum number of dynamically assembled 750x1050 RGBA32 card textures kept in RAM cache.
        /// </summary>
        public int MaxCachedDynamicCardTextures { get; set; }

        public QualityProfileData Clone()
        {
            return new QualityProfileData
            {
                Tier = Tier,
                ProfileName = ProfileName,
                UnityQualityLevelIndex = UnityQualityLevelIndex,
                TargetFrameRate = TargetFrameRate,
                MenuTargetFrameRate = MenuTargetFrameRate,
                BackgroundTargetFrameRate = BackgroundTargetFrameRate,
                RenderScale = RenderScale,
                MinRenderScale = MinRenderScale,
                ShadowsEnabled = ShadowsEnabled,
                ShadowResolutionTier = ShadowResolutionTier,
                ShadowDistance = ShadowDistance,
                AntiAliasingSamples = AntiAliasingSamples,
                TextureMipmapLimit = TextureMipmapLimit,
                AnisotropicFilteringMode = AnisotropicFilteringMode,
                ParticleBudgetScale = ParticleBudgetScale,
                ParticleRaycastBudget = ParticleRaycastBudget,
                PostProcessingEnabled = PostProcessingEnabled,
                MaxConcurrentVisualEffects = MaxConcurrentVisualEffects,
                EnableAmbientCardVfx = EnableAmbientCardVfx,
                EnableCardParallaxTilt = EnableCardParallaxTilt,
                MaxCachedDynamicCardTextures = MaxCachedDynamicCardTextures
            };
        }

        public void ValidateAndClamp()
        {
            TargetFrameRate = TargetFrameRate <= 30 ? 30 : 60;
            MenuTargetFrameRate = MenuTargetFrameRate <= 30 ? 30 : 60;
            BackgroundTargetFrameRate = Math.Clamp(BackgroundTargetFrameRate, 10, 30);
            MinRenderScale = Math.Clamp(MinRenderScale, 0.70f, 1.00f);
            RenderScale = Math.Clamp(RenderScale, MinRenderScale, 1.00f);
            ShadowDistance = ShadowsEnabled ? Math.Clamp(ShadowDistance, 0f, 150f) : 0f;
            ShadowResolutionTier = Math.Clamp(ShadowResolutionTier, 0, 3);
            if (AntiAliasingSamples != 0 && AntiAliasingSamples != 2 && AntiAliasingSamples != 4)
            {
                AntiAliasingSamples = AntiAliasingSamples < 2 ? 0 : 2;
            }
            // TCG-specific rule: never blur card artwork or UI via global mipmap limit
            TextureMipmapLimit = 0;
            AnisotropicFilteringMode = Math.Clamp(AnisotropicFilteringMode, 0, 2);
            ParticleBudgetScale = Math.Clamp(ParticleBudgetScale, 0.15f, 1.00f);
            ParticleRaycastBudget = Math.Clamp(ParticleRaycastBudget, 4, 4096);
            MaxConcurrentVisualEffects = Math.Clamp(MaxConcurrentVisualEffects, 1, 8);
            MaxCachedDynamicCardTextures = Math.Clamp(MaxCachedDynamicCardTextures, 8, 100);
        }
    }

    /// <summary>
    /// Authoritative provider of default Low, Medium, High, and Ultra quality profiles
    /// tailored for Cometa Legends on Android.
    /// </summary>
    public static class QualityProfileCatalog
    {
        private static readonly Dictionary<DevicePerformanceTier, QualityProfileData> DefaultProfiles = new()
        {
            [DevicePerformanceTier.Low] = new QualityProfileData
            {
                Tier = DevicePerformanceTier.Low,
                ProfileName = "Low (30 FPS • Battery & Stability)",
                UnityQualityLevelIndex = 1, // "Low" in QualitySettings.asset (globalTextureMipmapLimit = 0)
                TargetFrameRate = 30,
                MenuTargetFrameRate = 30,
                BackgroundTargetFrameRate = 15,
                RenderScale = 0.75f,
                MinRenderScale = 0.70f,
                ShadowsEnabled = false,
                ShadowResolutionTier = 0,
                ShadowDistance = 0f,
                AntiAliasingSamples = 0,
                TextureMipmapLimit = 0,
                AnisotropicFilteringMode = 0,
                ParticleBudgetScale = 0.35f,
                ParticleRaycastBudget = 16,
                PostProcessingEnabled = false,
                MaxConcurrentVisualEffects = 1,
                EnableAmbientCardVfx = false,
                EnableCardParallaxTilt = false,
                MaxCachedDynamicCardTextures = 16
            },
            [DevicePerformanceTier.Medium] = new QualityProfileData
            {
                Tier = DevicePerformanceTier.Medium,
                ProfileName = "Medium (60 FPS • Balanced)",
                UnityQualityLevelIndex = 2, // "Medium" in QualitySettings.asset
                TargetFrameRate = 60,
                MenuTargetFrameRate = 60,
                BackgroundTargetFrameRate = 15,
                RenderScale = 0.85f,
                MinRenderScale = 0.75f,
                ShadowsEnabled = true,
                ShadowResolutionTier = 0,
                ShadowDistance = 20f,
                AntiAliasingSamples = 0,
                TextureMipmapLimit = 0,
                AnisotropicFilteringMode = 1,
                ParticleBudgetScale = 0.65f,
                ParticleRaycastBudget = 64,
                PostProcessingEnabled = false,
                MaxConcurrentVisualEffects = 2,
                EnableAmbientCardVfx = true,
                EnableCardParallaxTilt = true,
                MaxCachedDynamicCardTextures = 28
            },
            [DevicePerformanceTier.High] = new QualityProfileData
            {
                Tier = DevicePerformanceTier.High,
                ProfileName = "High (60 FPS • Enhanced Visuals)",
                UnityQualityLevelIndex = 3, // "High" in QualitySettings.asset
                TargetFrameRate = 60,
                MenuTargetFrameRate = 60,
                BackgroundTargetFrameRate = 15,
                RenderScale = 0.95f,
                MinRenderScale = 0.85f,
                ShadowsEnabled = true,
                ShadowResolutionTier = 1,
                ShadowDistance = 40f,
                AntiAliasingSamples = 2,
                TextureMipmapLimit = 0,
                AnisotropicFilteringMode = 1,
                ParticleBudgetScale = 1.00f,
                ParticleRaycastBudget = 256,
                PostProcessingEnabled = true,
                MaxConcurrentVisualEffects = 4,
                EnableAmbientCardVfx = true,
                EnableCardParallaxTilt = true,
                MaxCachedDynamicCardTextures = 48
            },
            [DevicePerformanceTier.Ultra] = new QualityProfileData
            {
                Tier = DevicePerformanceTier.Ultra,
                ProfileName = "Ultra (60 FPS • Maximum Fidelity)",
                UnityQualityLevelIndex = 5, // "Ultra" in QualitySettings.asset
                TargetFrameRate = 60,
                MenuTargetFrameRate = 60,
                BackgroundTargetFrameRate = 15,
                RenderScale = 1.00f,
                MinRenderScale = 0.90f,
                ShadowsEnabled = true,
                ShadowResolutionTier = 2,
                ShadowDistance = 70f,
                AntiAliasingSamples = 4,
                TextureMipmapLimit = 0,
                AnisotropicFilteringMode = 2,
                ParticleBudgetScale = 1.00f,
                ParticleRaycastBudget = 1024,
                PostProcessingEnabled = true,
                MaxConcurrentVisualEffects = 6,
                EnableAmbientCardVfx = true,
                EnableCardParallaxTilt = true,
                MaxCachedDynamicCardTextures = 80
            }
        };

        /// <summary>
        /// Returns a fresh clone of the profile for the specified tier so runtime adaptive adjustments
        /// never mutate the shared catalog defaults.
        /// </summary>
        public static QualityProfileData GetProfile(DevicePerformanceTier tier)
        {
            if (DefaultProfiles.TryGetValue(tier, out var profile))
            {
                var copy = profile.Clone();
                copy.ValidateAndClamp();
                return copy;
            }

            return DefaultProfiles[DevicePerformanceTier.Medium].Clone();
        }

        /// <summary>
        /// Resolves the effective tier given a user-selected GraphicsQualityMode and the hardware-classified tier.
        /// </summary>
        public static DevicePerformanceTier ResolveEffectiveTier(GraphicsQualityMode mode, DevicePerformanceTier detectedTier)
        {
            return mode switch
            {
                GraphicsQualityMode.Low => DevicePerformanceTier.Low,
                GraphicsQualityMode.Medium => DevicePerformanceTier.Medium,
                GraphicsQualityMode.High => DevicePerformanceTier.High,
                GraphicsQualityMode.Ultra => DevicePerformanceTier.Ultra,
                _ => detectedTier
            };
        }

        /// <summary>
        /// Safely parses a stored preference string into GraphicsQualityMode, falling back to Auto on invalid input.
        /// </summary>
        public static GraphicsQualityMode ParseModeSafe(string? rawPreference)
        {
            if (string.IsNullOrWhiteSpace(rawPreference))
            {
                return GraphicsQualityMode.Auto;
            }

            if (Enum.TryParse<GraphicsQualityMode>(rawPreference!.Trim(), ignoreCase: true, out var parsed) &&
                Enum.IsDefined(typeof(GraphicsQualityMode), parsed))
            {
                return parsed;
            }

            return GraphicsQualityMode.Auto;
        }
    }
}
