#nullable enable
using System;
using System.Collections.Generic;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Encapsulates the complete breakdown of items awarded when a player opens a daily reward pack.
    /// </summary>
    [Serializable]
    public class DailyRewardResult
    {
        public int DayIndex { get; set; }
        public int GoldCoins { get; set; }
        public List<string> AwardedCardIds { get; set; } = new();
        public int EsenciaCometa { get; set; }
        public int BoosterPacks { get; set; }
        public int AccountXp { get; set; }
        public string? AwardedAvatarId { get; set; }
        public bool AvatarConvertedToGold { get; set; }
        public string SummaryText { get; set; } = string.Empty;

        public bool HasAvatar => !string.IsNullOrEmpty(AwardedAvatarId);
    }
}
