#nullable enable
using System;
using System.Collections.Generic;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Configuration for card crafting with Esencia Cometa and disenchanting (Option A).
    /// Default Crafting Costs:
    /// - Common: 50 Esencia
    /// - Rare: 100 Esencia
    /// - Epic: 300 Esencia
    /// - Legendary: 800 Esencia
    /// 
    /// Default Disenchant Yields:
    /// - Common: 10 Esencia
    /// - Rare: 25 Esencia
    /// - Epic: 75 Esencia
    /// - Legendary: 200 Esencia
    /// </summary>
    public static class CardCraftingConfig
    {
        public const int DefaultCommonCraftCost = 50;
        public const int DefaultRareCraftCost = 100;
        public const int DefaultEpicCraftCost = 300;
        public const int DefaultLegendaryCraftCost = 800;

        public const int DefaultCommonDisenchantYield = 10;
        public const int DefaultRareDisenchantYield = 25;
        public const int DefaultEpicDisenchantYield = 75;
        public const int DefaultLegendaryDisenchantYield = 200;

        private static readonly Dictionary<CardRarity, int> CraftCosts = new()
        {
            [CardRarity.Common] = DefaultCommonCraftCost,
            [CardRarity.Rare] = DefaultRareCraftCost,
            [CardRarity.Epic] = DefaultEpicCraftCost,
            [CardRarity.Legendary] = DefaultLegendaryCraftCost,
        };

        private static readonly Dictionary<CardRarity, int> DisenchantYields = new()
        {
            [CardRarity.Common] = DefaultCommonDisenchantYield,
            [CardRarity.Rare] = DefaultRareDisenchantYield,
            [CardRarity.Epic] = DefaultEpicDisenchantYield,
            [CardRarity.Legendary] = DefaultLegendaryDisenchantYield,
        };

        public static int GetCraftCost(CardRarity rarity)
        {
            return CraftCosts.TryGetValue(rarity, out int cost) ? cost : DefaultCommonCraftCost;
        }

        public static int GetDisenchantYield(CardRarity rarity)
        {
            return DisenchantYields.TryGetValue(rarity, out int yield) ? yield : DefaultCommonDisenchantYield;
        }

        public static void SetCraftCost(CardRarity rarity, int cost)
        {
            if (cost <= 0) throw new ArgumentOutOfRangeException(nameof(cost), "Craft cost must be positive.");
            CraftCosts[rarity] = cost;
        }

        public static void SetDisenchantYield(CardRarity rarity, int yield)
        {
            if (yield <= 0) throw new ArgumentOutOfRangeException(nameof(yield), "Disenchant yield must be positive.");
            DisenchantYields[rarity] = yield;
        }

        public static void ResetToDefault()
        {
            CraftCosts[CardRarity.Common] = DefaultCommonCraftCost;
            CraftCosts[CardRarity.Rare] = DefaultRareCraftCost;
            CraftCosts[CardRarity.Epic] = DefaultEpicCraftCost;
            CraftCosts[CardRarity.Legendary] = DefaultLegendaryCraftCost;

            DisenchantYields[CardRarity.Common] = DefaultCommonDisenchantYield;
            DisenchantYields[CardRarity.Rare] = DefaultRareDisenchantYield;
            DisenchantYields[CardRarity.Epic] = DefaultEpicDisenchantYield;
            DisenchantYields[CardRarity.Legendary] = DefaultLegendaryDisenchantYield;
        }
    }
}
