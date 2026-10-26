using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Configuration settings for Unity Ads integration (Game IDs, Ad Unit IDs, Test Mode).
    /// Serialized in Assets/Resources/UnityAdsSettings.json.
    /// </summary>
    [Serializable]
    public class UnityAdsConfig
    {
        public string androidGameId = "6205221";
        public string iosGameId = "6205220";
        public string androidAdUnitId = "Rewarded_Android";
        public string iosAdUnitId = "Rewarded_iOS";
        public bool testMode = false;
        public bool enableEditorSimulation = true;
        public int rewardGoldCoins = PlayerProfile.CoinsPerAdReward;

        public string ActiveGameId
        {
            get
            {
#if UNITY_IOS
                return iosGameId;
#else
                return androidGameId;
#endif
            }
        }

        public string ActiveAdUnitId
        {
            get
            {
#if UNITY_IOS
                return iosAdUnitId;
#else
                return androidAdUnitId;
#endif
            }
        }
    }
}
