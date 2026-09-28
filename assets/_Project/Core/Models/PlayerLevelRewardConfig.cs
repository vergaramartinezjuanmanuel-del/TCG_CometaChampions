#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Configurable settings for Player Level Reward Packs.
    /// Certain Player Levels grant an additional free Reward Pack (3 cards, no new-card guarantee).
    /// Default configurable levels: 2, 4, 6, 8, 10, 12, 15, 20.
    /// </summary>
    public static class PlayerLevelRewardConfig
    {
        public const int CardsPerRewardPack = 3;

        private static HashSet<int> rewardLevels = new() { 2, 4, 6, 8, 10, 12, 15, 20 };

        /// <summary>
        /// Read-only collection of currently configured reward levels.
        /// </summary>
        public static IReadOnlyCollection<int> RewardLevels => rewardLevels;

        /// <summary>
        /// Sets a custom collection of player levels that grant a free reward pack upon level-up.
        /// </summary>
        public static void SetRewardLevels(IEnumerable<int> levels)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            rewardLevels = new HashSet<int>(levels.Where(lvl => lvl > 1));
        }

        /// <summary>
        /// Resets the reward levels to the official default schedule: { 2, 4, 6, 8, 10, 12, 15, 20 }.
        /// </summary>
        public static void ResetToDefault()
        {
            rewardLevels = new HashSet<int> { 2, 4, 6, 8, 10, 12, 15, 20 };
        }

        /// <summary>
        /// Evaluates whether a given account level is configured to grant a free reward pack.
        /// </summary>
        public static bool IsRewardLevel(int level) => rewardLevels.Contains(level);
    }
}
