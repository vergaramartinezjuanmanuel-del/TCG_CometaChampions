#nullable enable
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;
using CometaChampions.Runtime.Cards;
using CometaChampions.Runtime.UI;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Production-ready Adaptive Quality and Performance Optimization service for Cometa Legends.
    /// Automatically detects hardware capabilities, classifies the device into Low/Medium/High/Ultra,
    /// applies safe graphics settings (preserving TCG card and UI clarity), respects user overrides,
    /// and optionally adapts runtime settings using a conservative hysteresis state machine.
    /// </summary>
    public sealed class AdaptiveQualityManager : MonoBehaviour
    {
        public const string PlayerPrefsQualityModeKey = "Cometa_GraphicsQualityMode";
        public const string PlayerPrefsAdaptiveEnabledKey = "Cometa_AdaptiveQualityEnabled";

        private static AdaptiveQualityManager? instance;
        private static bool isQuitting;

        public static AdaptiveQualityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindAnyObjectByType<AdaptiveQualityManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("AdaptiveQualityManager");
                        instance = go.AddComponent<AdaptiveQualityManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        [Header("Configuration (Optional Override Asset)")]
        [SerializeField] private QualityProfileConfigSO? configAsset;

        private DeviceCapabilities? detectedCapabilities;
        private DeviceClassificationResult? classificationResult;
        private AdaptiveQualityStateMachine? stateMachine;

        private GraphicsQualityMode currentMode = GraphicsQualityMode.Auto;
        private bool isAdaptiveEnabled = true;
        private bool isInitialized;
        private bool isApplicationPaused;
        private int activeCriticalAnimationsCount;
        private int lastAppliedUnityQualityIndex = -1;

        public DeviceCapabilities DetectedCapabilities
        {
            get
            {
                EnsureInitialized();
                return detectedCapabilities!;
            }
        }

        public DeviceClassificationResult ClassificationResult
        {
            get
            {
                EnsureInitialized();
                return classificationResult!;
            }
        }

        public DevicePerformanceTier ClassifiedHardwareTier => ClassificationResult.Tier;

        public GraphicsQualityMode QualityMode
        {
            get
            {
                EnsureInitialized();
                return currentMode;
            }
        }

        public bool IsAdaptiveMonitoringEnabled
        {
            get
            {
                EnsureInitialized();
                return isAdaptiveEnabled;
            }
        }

        public QualityProfileData ActiveProfile
        {
            get
            {
                EnsureInitialized();
                return stateMachine!.CurrentProfile;
            }
        }

        public QualityProfileData CeilingProfile
        {
            get
            {
                EnsureInitialized();
                return stateMachine!.CeilingProfile;
            }
        }

        public AdaptiveQualityStateMachine StateMachine
        {
            get
            {
                EnsureInitialized();
                return stateMachine!;
            }
        }

        public bool IsCriticalAnimationActive => activeCriticalAnimationsCount > 0;

        public event Action<QualityProfileData>? OnQualityProfileApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapOnStartup()
        {
            if (isQuitting) return;
            _ = Instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureInitialized();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Application.lowMemory -= OnLowMemoryWarning;
            Application.lowMemory += OnLowMemoryWarning;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.lowMemory -= OnLowMemoryWarning;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            isApplicationPaused = pauseStatus;
            if (!isInitialized || stateMachine == null) return;

            var profile = stateMachine.CurrentProfile;
            if (pauseStatus)
            {
                // Lower frame rate while backgrounded to conserve battery without reinitializing graphics
                Application.targetFrameRate = profile.BackgroundTargetFrameRate;
            }
            else
            {
                // Restore active target frame rate and clear frame-time samples so pause duration is not treated as a frame drop
                stateMachine.SampleBuffer.Clear();
                ApplyTargetFrameRate(profile);
            }
        }

        private void Update()
        {
            if (!isInitialized || !isAdaptiveEnabled || isApplicationPaused || stateMachine == null)
            {
                return;
            }

            bool isSceneLoading = IsSceneTransitionActive();
            bool isMenuScene = IsCurrentSceneMenuIdle();

            bool profileChanged = stateMachine.RecordFrameAndEvaluate(
                unscaledDeltaTimeSeconds: Time.unscaledDeltaTime,
                isSceneLoading: isSceneLoading,
                isCriticalAnimationActive: IsCriticalAnimationActive,
                isMenuIdleScene: isMenuScene);

            if (profileChanged)
            {
                ApplyRuntimeProfile(stateMachine.CurrentProfile, applyQualityIndexChange: false);
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"[AdaptiveQuality] Runtime adjustment applied: {stateMachine.LastAdjustmentReason}");
                }
            }
        }

        public void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;

            if (configAsset == null)
            {
                configAsset = Resources.Load<QualityProfileConfigSO>("Performance/QualityProfileConfig");
            }

            var policy = configAsset != null
                ? configAsset.classificationPolicy
                : DeviceClassificationPolicy.CreateDefault();

            var adaptiveCfg = configAsset != null
                ? configAsset.adaptiveConfig
                : AdaptiveQualityConfig.CreateDefault();

            detectedCapabilities = DeviceCapabilityDetector.Detect();
            classificationResult = DevicePerformanceClassifier.Classify(detectedCapabilities, policy);

            LoadPersistedPreferences();

            var effectiveTier = QualityProfileCatalog.ResolveEffectiveTier(currentMode, classificationResult.Tier);
            var initialProfile = ResolveBaseProfileForTier(effectiveTier);

            stateMachine = new AdaptiveQualityStateMachine(initialProfile, adaptiveCfg);
            ApplyRuntimeProfile(stateMachine.CurrentProfile, applyQualityIndexChange: true);

            if (Debug.isDebugBuild)
            {
                Debug.Log($"[AdaptiveQuality] Initialized: Mode={currentMode}, ClassifiedTier={classificationResult.Tier}, EffectiveTier={effectiveTier}, TargetFPS={initialProfile.TargetFrameRate}, RenderScale={initialProfile.RenderScale:F2}");
            }
        }

        /// <summary>
        /// Sets the user-selected quality mode (Auto, Low, Medium, High, Ultra) and persists the preference.
        /// User overrides take immediate effect and bound any runtime adaptation ceiling.
        /// </summary>
        public void SetQualityMode(GraphicsQualityMode mode, bool savePreference = true)
        {
            EnsureInitialized();

            if (!Enum.IsDefined(typeof(GraphicsQualityMode), mode))
            {
                mode = GraphicsQualityMode.Auto;
            }

            currentMode = mode;
            if (savePreference)
            {
                PersistPreferences();
            }

            var effectiveTier = QualityProfileCatalog.ResolveEffectiveTier(currentMode, classificationResult!.Tier);
            var baseProfile = ResolveBaseProfileForTier(effectiveTier);

            stateMachine!.ResetToProfile(baseProfile, $"Quality mode set to {currentMode} (Effective tier: {effectiveTier})");
            ApplyRuntimeProfile(stateMachine.CurrentProfile, applyQualityIndexChange: true);
        }

        /// <summary>
        /// Enables or disables runtime frame-time adaptation independently of initial device classification.
        /// </summary>
        public void SetAdaptiveMonitoringEnabled(bool enabled, bool savePreference = true)
        {
            EnsureInitialized();
            isAdaptiveEnabled = enabled;

            if (savePreference)
            {
                PersistPreferences();
            }

            if (!enabled && stateMachine != null && stateMachine.ActiveDegradationSteps > 0)
            {
                // Restore to the ceiling profile when adaptive stepping is turned off
                stateMachine.ResetToProfile(stateMachine.CeilingProfile, "Adaptive monitoring disabled; restored ceiling profile");
                ApplyRuntimeProfile(stateMachine.CurrentProfile, applyQualityIndexChange: false);
            }
        }

        /// <summary>
        /// Notifies the controller that a critical TCG animation (e.g., turn combat resolution, mulligan spin, pack opening)
        /// is starting or ending so quality changes are deferred until the animation completes.
        /// </summary>
        public void SetCriticalAnimationActive(bool active)
        {
            if (active)
            {
                activeCriticalAnimationsCount++;
            }
            else
            {
                activeCriticalAnimationsCount = Math.Max(0, activeCriticalAnimationsCount - 1);
            }
        }

        private QualityProfileData ResolveBaseProfileForTier(DevicePerformanceTier tier)
        {
            if (configAsset != null)
            {
                return configAsset.GetClonedProfile(tier);
            }
            return QualityProfileCatalog.GetProfile(tier);
        }

        private void LoadPersistedPreferences()
        {
            string? rawMode = null;
            bool adaptivePref = true;

            // 1. Check PlayerPrefs first (available before SaveManager loads disk JSON)
            if (PlayerPrefs.HasKey(PlayerPrefsQualityModeKey))
            {
                rawMode = PlayerPrefs.GetString(PlayerPrefsQualityModeKey, "Auto");
            }
            if (PlayerPrefs.HasKey(PlayerPrefsAdaptiveEnabledKey))
            {
                adaptivePref = PlayerPrefs.GetInt(PlayerPrefsAdaptiveEnabledKey, 1) != 0;
            }

            // 2. Synchronize with SaveManager.Profile if already loaded
            try
            {
                var profile = SaveManager.Profile;
                if (profile != null)
                {
                    if (string.IsNullOrWhiteSpace(rawMode) && !string.IsNullOrWhiteSpace(profile.GraphicsQualityPreference))
                    {
                        rawMode = profile.GraphicsQualityPreference;
                        adaptivePref = profile.AdaptiveQualityEnabled;
                    }
                    else if (!string.IsNullOrWhiteSpace(rawMode))
                    {
                        profile.GraphicsQualityPreference = rawMode!;
                        profile.AdaptiveQualityEnabled = adaptivePref;
                    }
                }
            }
            catch
            {
                // Ignore if SaveManager is not yet ready in unit test fixtures
            }

            currentMode = QualityProfileCatalog.ParseModeSafe(rawMode);
            isAdaptiveEnabled = adaptivePref;
        }

        private void PersistPreferences()
        {
            string modeStr = currentMode.ToString();
            PlayerPrefs.SetString(PlayerPrefsQualityModeKey, modeStr);
            PlayerPrefs.SetInt(PlayerPrefsAdaptiveEnabledKey, isAdaptiveEnabled ? 1 : 0);
            PlayerPrefs.Save();

            try
            {
                var profile = SaveManager.Profile;
                if (profile != null)
                {
                    profile.GraphicsQualityPreference = modeStr;
                    profile.AdaptiveQualityEnabled = isAdaptiveEnabled;
                    SaveManager.Save();
                }
            }
            catch
            {
                // Safe fallback if SaveManager is not active
            }
        }

        private void ApplyRuntimeProfile(QualityProfileData profile, bool applyQualityIndexChange)
        {
            profile.ValidateAndClamp();

            // 1. Unity QualitySettings index (applyExpensiveChanges = false to avoid frame hitches)
            if (applyQualityIndexChange && profile.UnityQualityLevelIndex != lastAppliedUnityQualityIndex)
            {
                int maxLevel = QualitySettings.names != null ? QualitySettings.names.Length - 1 : 5;
                int clampedIndex = Mathf.Clamp(profile.UnityQualityLevelIndex, 0, Math.Max(0, maxLevel));
                QualitySettings.SetQualityLevel(clampedIndex, applyExpensiveChanges: false);
                lastAppliedUnityQualityIndex = clampedIndex;
            }

            // 2. Frame-rate policy (vSyncCount = 0 so Application.targetFrameRate governs pacing on Android)
            QualitySettings.vSyncCount = 0;
            ApplyTargetFrameRate(profile);

            // 3. Protect TCG card artwork and UI clarity: never downscale mipmaps globally
            QualitySettings.globalTextureMipmapLimit = 0;

            // 4. Shadows, Anti-Aliasing, Anisotropic Filtering, Particle Raycast Budget
            QualitySettings.shadows = profile.ShadowsEnabled ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowResolution = (UnityEngine.ShadowResolution)Mathf.Clamp(profile.ShadowResolutionTier, 0, 3);
            QualitySettings.shadowDistance = profile.ShadowsEnabled ? profile.ShadowDistance : 0f;
            QualitySettings.antiAliasing = profile.AntiAliasingSamples;
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)Mathf.Clamp(profile.AnisotropicFilteringMode, 0, 2);
            QualitySettings.particleRaycastBudget = profile.ParticleRaycastBudget;

            // 5. Render Pipeline specific settings (URP if active, or Built-in 3D cameras)
            ApplyRenderPipelineSettings(profile);

            // 6. Scene-specific TCG VFX and 3D card viewport settings
            ApplySceneSpecificBudgets(profile);

            OnQualityProfileApplied?.Invoke(profile);
        }

        private void ApplyTargetFrameRate(QualityProfileData profile)
        {
            if (isApplicationPaused)
            {
                Application.targetFrameRate = profile.BackgroundTargetFrameRate;
                return;
            }

            bool isMenu = IsCurrentSceneMenuIdle();
            Application.targetFrameRate = isMenu ? profile.MenuTargetFrameRate : profile.TargetFrameRate;
        }

        private void ApplyRenderPipelineSettings(QualityProfileData profile)
        {
            // Check if a UniversalRenderPipelineAsset is currently active
            var urpAsset = (QualitySettings.renderPipeline as UniversalRenderPipelineAsset)
                           ?? (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);

            if (urpAsset != null)
            {
                urpAsset.renderScale = profile.RenderScale;
                urpAsset.msaaSampleCount = profile.AntiAliasingSamples > 0 ? profile.AntiAliasingSamples : 1;
                urpAsset.shadowDistance = profile.ShadowsEnabled ? profile.ShadowDistance : 0f;
            }

            // Configure 3D cameras (Screen Space Overlay UI Canvases are unaffected and stay 100% crisp)
            var cameras = FindObjectsByType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam == null) continue;
                cam.allowMSAA = profile.AntiAliasingSamples > 0;
                cam.allowHDR = profile.PostProcessingEnabled;

                bool canScale3DBuffer = detectedCapabilities != null &&
                                        detectedCapabilities.SupportsDynamicResolution &&
                                        profile.RenderScale < 0.99f;
                cam.allowDynamicResolution = canScale3DBuffer;
            }

            if (detectedCapabilities != null && detectedCapabilities.SupportsDynamicResolution)
            {
                ScalableBufferManager.ResizeBuffers(profile.RenderScale, profile.RenderScale);
            }
        }

        private void ApplySceneSpecificBudgets(QualityProfileData profile)
        {
            // 1. Ambient 3D Card Viewport Particle Systems in Home scene
            var vfxAnchor = GameObject.Find("Center_Card3D_VFX_Anchor");
            if (vfxAnchor != null)
            {
                if (!profile.EnableAmbientCardVfx)
                {
                    if (vfxAnchor.activeSelf) vfxAnchor.SetActive(false);
                }
                else
                {
                    if (!vfxAnchor.activeSelf) vfxAnchor.SetActive(true);
                    ScaleParticleSystemBudget(vfxAnchor.transform.Find("CardVortexSwirl_Particles"), 140, 38f, profile.ParticleBudgetScale);
                    ScaleParticleSystemBudget(vfxAnchor.transform.Find("CardAuraOrbs_Particles"), 80, 18f, profile.ParticleBudgetScale);
                    ScaleParticleSystemBudget(vfxAnchor.transform.Find("PedestalRisingSparks_Particles"), 60, 16f, profile.ParticleBudgetScale);
                }
            }

            // 2. 3D Card Viewer idle parallax tilt budget
            var cardViewer = FindAnyObjectByType<Card3DViewer>();
            if (cardViewer != null)
            {
                cardViewer.SetParallaxTiltEnabled(profile.EnableCardParallaxTilt);
            }
        }

        public void ApplySceneBudgetsNow()
        {
            EnsureInitialized();
            if (stateMachine != null)
            {
                ApplySceneSpecificBudgets(stateMachine.CurrentProfile);
            }
        }

        private static void ScaleParticleSystemBudget(Transform? psTransform, int baseMaxParticles, float baseEmissionRate, float scale)
        {
            if (psTransform == null) return;
            var ps = psTransform.GetComponent<ParticleSystem>();
            if (ps == null) return;

            var main = ps.main;
            main.maxParticles = Mathf.Max(8, Mathf.RoundToInt(baseMaxParticles * scale));

            var emission = ps.emission;
            emission.rateOverTime = Mathf.Max(2f, baseEmissionRate * scale);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!isInitialized || stateMachine == null) return;

            activeCriticalAnimationsCount = 0;
            stateMachine.SampleBuffer.Clear();

            var profile = stateMachine.CurrentProfile;

            // Safely trim excess dynamically rendered card textures during scene transition
            DynamicCardAssembler.TrimDynamicCache(profile.MaxCachedDynamicCardTextures);

            // Re-apply frame rate, camera flags, and scene VFX budgets to newly loaded scene objects
            ApplyTargetFrameRate(profile);
            ApplyRenderPipelineSettings(profile);
            ApplySceneSpecificBudgets(profile);
        }

        private void OnLowMemoryWarning()
        {
            if (!isInitialized || stateMachine == null) return;

            // Trim cached dynamic card textures down to a conservative minimum on OS low-memory warning
            int trimmed = DynamicCardAssembler.TrimDynamicCache(8);
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning($"[AdaptiveQuality] Android/OS LowMemory warning received. Trimmed {trimmed} cached dynamic card textures.");
            }
        }

        private static bool IsSceneTransitionActive()
        {
            var loadingMgr = FindAnyObjectByType<LoadingScreenManager>();
            return loadingMgr != null && loadingMgr.IsLoading;
        }

        private static bool IsCurrentSceneMenuIdle()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return !string.Equals(sceneName, "Battle", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generates a structured developer diagnostic report detailing hardware capabilities,
        /// classification scores, active quality settings, frame-time statistics, and memory usage.
        /// </summary>
        public string GenerateDiagnosticReport()
        {
            EnsureInitialized();

            var cap = detectedCapabilities!;
            var cls = classificationResult!;
            var prof = stateMachine!.CurrentProfile;
            var buf = stateMachine.SampleBuffer;

            float avgMs = buf.GetAverageMs();
            float p95Ms = buf.GetPercentileMs(0.95f);
            float approxFps = avgMs > 0.01f ? 1000f / avgMs : prof.TargetFrameRate;

            long allocatedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            long reservedMb = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);

            var sb = new StringBuilder(512);
            sb.AppendLine("=== COMETA LEGENDS ADAPTIVE QUALITY DIAGNOSTIC REPORT ===");
            sb.AppendLine($"[Hardware] {cap}");
            sb.AppendLine($"[Classification] {cls}");
            sb.AppendLine($"[User Mode] Mode={currentMode} | AdaptiveMonitoring={isAdaptiveEnabled} | ActiveDegradationSteps={stateMachine.ActiveDegradationSteps}");
            sb.AppendLine($"[Active Profile] Name='{prof.ProfileName}' | UnityQualityIdx={prof.UnityQualityLevelIndex} | TargetFPS={Application.targetFrameRate} | RenderScale={prof.RenderScale:F2} (Min={prof.MinRenderScale:F2})");
            sb.AppendLine($"[Graphics Flags] Shadows={(prof.ShadowsEnabled ? $"On ({prof.ShadowDistance}m)" : "Off")} | MSAA={prof.AntiAliasingSamples}x | TexMipLimit={prof.TextureMipmapLimit} | Aniso={prof.AnisotropicFilteringMode} | PostProc={prof.PostProcessingEnabled}");
            sb.AppendLine($"[VFX Budgets] AmbientCardVFX={prof.EnableAmbientCardVfx} | ParticleScale={prof.ParticleBudgetScale:P0} | MaxConcurrentVFX={prof.MaxConcurrentVisualEffects} | ParallaxTilt={prof.EnableCardParallaxTilt}");
            sb.AppendLine($"[Frame Timing] Samples={buf.Count}/{buf.Capacity} | RollingAvg={avgMs:F2}ms (~{approxFps:F1} FPS) | P95={p95Ms:F2}ms");
            sb.AppendLine($"[Memory & Caches] UnityAllocated={allocatedMb}MB | UnityReserved={reservedMb}MB | DynamicCardTexCache={DynamicCardAssembler.CachedDynamicTextureCount}/{prof.MaxCachedDynamicCardTextures} | Card3DTexCache={Card3D.CachedTextureCount} | Card3DSpriteCache={Card3D.CachedSpriteCount}");
            sb.AppendLine($"[Last Adjustment] {stateMachine.LastAdjustmentReason}");
            if (cls.DiagnosticNotes.Count > 0)
            {
                sb.AppendLine($"[Classifier Notes] {string.Join(" | ", cls.DiagnosticNotes)}");
            }
            sb.AppendLine("=========================================================");
            return sb.ToString();
        }
    }
}
