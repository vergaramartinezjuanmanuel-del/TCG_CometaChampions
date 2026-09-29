#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Authoritative catalog of the 7-day cyclical daily login reward packs.
    /// Rolls random cards, gold, dust, booster packs, and avatars.
    /// </summary>
    public static class DailyRewardCatalog
    {
        public const int BonusGoldForOwnedAvatar = 300;

        private static readonly List<DailyRewardPackDefinition> Schedule = new()
        {
            new DailyRewardPackDefinition(1, "Pack Bienvenida", 150, 1, 25, 0, 50, false),
            new DailyRewardPackDefinition(2, "Pack Impulso", 200, 1, 35, 0, 50, false),
            new DailyRewardPackDefinition(3, "Pack Estratega", 250, 2, 50, 0, 75, false),
            new DailyRewardPackDefinition(4, "Pack Refuerzo", 300, 1, 50, 1, 75, false),
            new DailyRewardPackDefinition(5, "Pack Campeón", 350, 2, 75, 0, 100, false),
            new DailyRewardPackDefinition(6, "Pack Maestro", 400, 2, 100, 0, 100, false),
            new DailyRewardPackDefinition(7, "Mega Pack Épico", 600, 3, 150, 1, 200, true)
        };

        private static readonly Dictionary<int, DailyRewardPackDefinition> Lookup =
            Schedule.ToDictionary(s => s.DayIndex);

        /// <summary>
        /// Retrieves the reward pack definition for a specific day in the 7-day cycle (1..7).
        /// </summary>
        public static DailyRewardPackDefinition GetPackForDay(int dayIndex)
        {
            int clamped = Math.Clamp(dayIndex, 1, 7);
            return Lookup[clamped];
        }

        /// <summary>
        /// Returns all 7 days of the reward cycle.
        /// </summary>
        public static IReadOnlyList<DailyRewardPackDefinition> GetAllDays() => Schedule;

        /// <summary>
        /// Generates concrete rewards for the given day index and player profile.
        /// </summary>
        public static DailyRewardResult GenerateReward(int dayIndex, PlayerProfile profile, Random? rng = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            rng ??= new Random();

            var packDef = GetPackForDay(dayIndex);
            var result = new DailyRewardResult
            {
                DayIndex = packDef.DayIndex,
                GoldCoins = packDef.GoldCoins,
                EsenciaCometa = packDef.EsenciaCometa,
                BoosterPacks = packDef.BoosterPacks,
                AccountXp = packDef.AccountXp
            };

            // 1. Roll Random Cards from catalog
            var allCards = CardCatalog.AllCards;
            if (allCards.Count > 0 && packDef.CardCount > 0)
            {
                for (int i = 0; i < packDef.CardCount; i++)
                {
                    int randomIndex = rng.Next(0, allCards.Count);
                    result.AwardedCardIds.Add(allCards[randomIndex].Id);
                }
            }

            // 2. Roll Avatar Reward if day provides one
            if (packDef.HasAvatarReward)
            {
                var unownedAvatars = AvatarCatalog.GetAll()
                    .Where(a => !profile.IsAvatarUnlocked(a.Id))
                    .ToList();

                if (unownedAvatars.Count > 0)
                {
                    int chosenIndex = rng.Next(0, unownedAvatars.Count);
                    result.AwardedAvatarId = unownedAvatars[chosenIndex].Id;
                }
                else
                {
                    // Player already unlocked all 20 avatars; grant bonus gold compensation
                    result.GoldCoins += BonusGoldForOwnedAvatar;
                    result.AvatarConvertedToGold = true;
                }
            }

            // Build human-readable summary
            var parts = new List<string> { $"+{result.GoldCoins} Monedas" };
            if (result.AwardedCardIds.Count > 0)
            {
                parts.Add($"+{result.AwardedCardIds.Count} Cartas");
            }
            if (result.EsenciaCometa > 0)
            {
                parts.Add($"+{result.EsenciaCometa} Esencia");
            }
            if (result.BoosterPacks > 0)
            {
                parts.Add($"+{result.BoosterPacks} Sobre");
            }
            if (!string.IsNullOrEmpty(result.AwardedAvatarId))
            {
                var avatarDef = AvatarCatalog.GetAvatarById(result.AwardedAvatarId);
                parts.Add($"¡Avatar {avatarDef?.Name}!");
            }
            else if (result.AvatarConvertedToGold)
            {
                parts.Add("(+300 Oro Avatar Duplicado)");
            }
            if (result.AccountXp > 0)
            {
                parts.Add($"+{result.AccountXp} XP");
            }

            result.SummaryText = string.Join(" • ", parts);
            return result;
        }
    }
}
