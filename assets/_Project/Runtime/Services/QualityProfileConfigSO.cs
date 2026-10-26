#nullable enable
using UnityEngine;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Optional ScriptableObject configuration asset for tuning DeviceClassificationPolicy,
    /// AdaptiveQualityConfig, and the four QualityProfileData tiers in the Unity Inspector.
    /// Falls back automatically to QualityProfileCatalog defaults when not present in Resources.
    /// </summary>
    [CreateAssetMenu(fileName = "QualityProfileConfig", menuName = "Cometa Champions/Performance/Quality Profile Config")]
    public sealed class QualityProfileConfigSO : ScriptableObject
    {
        [Header("Device Classification Policy")]
        public DeviceClassificationPolicy classificationPolicy = DeviceClassificationPolicy.CreateDefault();

        [Header("Runtime Adaptive Hysteresis Policy")]
        public AdaptiveQualityConfig adaptiveConfig = AdaptiveQualityConfig.CreateDefault();

        [Header("Tier Profiles")]
        public QualityProfileData lowProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.Low);
        public QualityProfileData mediumProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.Medium);
        public QualityProfileData highProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.High);
        public QualityProfileData ultraProfile = QualityProfileCatalog.GetProfile(DevicePerformanceTier.Ultra);

        public QualityProfileData GetClonedProfile(DevicePerformanceTier tier)
        {
            QualityProfileData source = tier switch
            {
                DevicePerformanceTier.Low => lowProfile,
                DevicePerformanceTier.Medium => mediumProfile,
                DevicePerformanceTier.High => highProfile,
                DevicePerformanceTier.Ultra => ultraProfile,
                _ => mediumProfile
            };

            if (source == null)
            {
                return QualityProfileCatalog.GetProfile(tier);
            }

            var copy = source.Clone();
            copy.ValidateAndClamp();
            return copy;
        }
    }
}
