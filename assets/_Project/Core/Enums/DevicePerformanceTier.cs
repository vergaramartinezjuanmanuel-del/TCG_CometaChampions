namespace CometaChampions.Core.Enums
{
    /// <summary>
    /// Hardware performance tier classified at startup or selected by the user.
    /// </summary>
    public enum DevicePerformanceTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// User-facing graphics quality mode persisted in settings.
    /// Auto selects the initial profile from DevicePerformanceClassifier and allows bounded runtime adaptation.
    /// </summary>
    public enum GraphicsQualityMode
    {
        Auto = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    /// <summary>
    /// Ordered runtime adaptation steps from least intrusive to most impactful.
    /// Card artwork and UI readability are never degraded.
    /// </summary>
    public enum AdaptiveAdjustmentStep
    {
        None = 0,
        PostProcessingAndOptionalVfx = 1,
        ParticleBudget = 2,
        ShadowQuality = 3,
        RenderScale = 4
    }
}
