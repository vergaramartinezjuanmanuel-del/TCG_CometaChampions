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
        public string androidGameId = "5738200";
        public string iosGameId = "5738201";
        public string androidAdUnitId = "Rewarded_Android";
        public string iosAdUnitId = "Rewarded_iOS";
        public bool testMode = true;
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
