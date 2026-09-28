#nullable enable
using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Pure C# immutable definition for a daily login reward pack.
    /// Represents one day in the 7-day cyclical calendar.
    /// </summary>
    [Serializable]
    public class DailyRewardPackDefinition
    {
        public int DayIndex { get; }
        public string Title { get; }
        public int GoldCoins { get; }
        public int CardCount { get; }
        public int EsenciaCometa { get; }
        public int BoosterPacks { get; }
        public int AccountXp { get; }
        public bool HasAvatarReward { get; }

        public DailyRewardPackDefinition(
            int dayIndex,
            string title,
            int goldCoins,
            int cardCount,
            int esenciaCometa,
            int boosterPacks,
            int accountXp,
            bool hasAvatarReward = false)
        {
            DayIndex = Math.Clamp(dayIndex, 1, 7);
            Title = title ?? throw new ArgumentNullException(nameof(title));
            GoldCoins = Math.Max(0, goldCoins);
            CardCount = Math.Max(0, cardCount);
            EsenciaCometa = Math.Max(0, esenciaCometa);
            BoosterPacks = Math.Max(0, boosterPacks);
            AccountXp = Math.Max(0, accountXp);
            HasAvatarReward = hasAvatarReward;
        }
    }
}
