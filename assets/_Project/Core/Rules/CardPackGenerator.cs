#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# authoritative generator for Cometa Champions card booster packs (Rules 1 to 14).
    /// Enforces the mandatory New Card Guarantee (Rule 2 & 14), slot rarity distribution (Rule 8),
    /// sequential multi-pack transactions (Rule 13), and duplicate upgrade tracking (Rule 10).
    /// </summary>
    public static class CardPackGenerator
    {
        public const int CardsPerPack = 5;
        public const int CardsPerRewardPack = 3;

        /// <summary>
        /// Rolls the rarity of a specific slot in a 5-card booster pack using official odds.
        /// Slot 0, 1, 2: Standard/Common slots (75% Common, 20% Rare, 5% Epic)
        /// Slot 3: High/Rare slot (70% Rare, 22% Epic, 8% Legendary)
        /// Slot 4: Wildcard slot (45% Common, 30% Rare, 18% Epic, 7% Legendary)
        /// </summary>
        public static CardRarity RollSlotRarity(int slotIndex, Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int roll = rng.Next(0, 100);

            switch (slotIndex)
            {
                case 0:
                case 1:
                case 2:
                    if (roll < 75) return CardRarity.Common;
                    if (roll < 95) return CardRarity.Rare;
                    return CardRarity.Epic;

                case 3:
                    if (roll < 70) return CardRarity.Rare;
                    if (roll < 92) return CardRarity.Epic;
                    return CardRarity.Legendary;

                case 4:
                default:
                    if (roll < 45) return CardRarity.Common;
                    if (roll < 75) return CardRarity.Rare;
                    if (roll < 93) return CardRarity.Epic;
                    return CardRarity.Legendary;
            }
        }

        /// <summary>
        /// Rolls the rarity for a specific slot in a 3-card Player Level Reward Pack.
        /// Reward Packs use a more limited rarity distribution than purchased packs:
        /// Slot 0: Common (85%), Rare (15%)
        /// Slot 1: Common (70%), Rare (25%), Epic (5%)
        /// Slot 2: Rare (80%), Epic (18%), Legendary (2%)
        /// </summary>
        public static CardRarity RollRewardPackSlotRarity(int slotIndex, Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int roll = rng.Next(0, 100);

            switch (slotIndex)
            {
                case 0:
                    return roll < 85 ? CardRarity.Common : CardRarity.Rare;

                case 1:
                    if (roll < 70) return CardRarity.Common;
                    if (roll < 95) return CardRarity.Rare;
                    return CardRarity.Epic;

                case 2:
                default:
                    if (roll < 80) return CardRarity.Rare;
                    if (roll < 98) return CardRarity.Epic;
                    return CardRarity.Legendary;
            }
        }

        /// <summary>
        /// Generates a single 5-card booster pack and applies it directly to the player's profile (Rules 2-12).
        /// Mandatory Rule 14: Never produces 5 duplicates while unowned cards remain in the pack pool.
        /// </summary>
        public static CardPackResult GeneratePack(
            PlayerProfile profile,
            IReadOnlyList<CardDefinition>? customPool = null,
            Random? rng = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            rng ??= new Random();

            var pool = (customPool ?? CardCatalog.AllCards).ToList();
            if (pool.Count == 0)
            {
                throw new InvalidOperationException("Cannot generate pack: card pool is empty.");
            }

            // Step 1 & 2: Determine unowned cards in this pool (Rule 4 & Rule 3: foil levels don't count as new)
            var unownedCards = pool.Where(c => !profile.IsCardUnlocked(c.Id)).ToList();

            var rolledCards = new List<CardDefinition>(CardsPerPack);
            var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (unownedCards.Count == 0)
            {
                // Rule 7: Player already owns all cards in pool -> standard pack of duplicates without error
                for (int slot = 0; slot < CardsPerPack; slot++)
                {
                    var slotRarity = RollSlotRarity(slot, rng);
                    var card = PickRandomCard(pool, slotRarity, selectedIds, rng);
                    rolledCards.Add(card);
                    selectedIds.Add(card.Id);
                }
            }
            else
            {
                // Rules 2, 4, 8: At least ONE card must be unowned, respecting slot rarity rules
                var slotRarities = new CardRarity[CardsPerPack];
                for (int slot = 0; slot < CardsPerPack; slot++)
                {
                    slotRarities[slot] = RollSlotRarity(slot, rng);
                }

                // Check which slots naturally rolled a rarity that has unowned cards available
                var matchingSlotIndices = new List<int>();
                for (int slot = 0; slot < CardsPerPack; slot++)
                {
                    if (unownedCards.Any(c => c.Rarity == slotRarities[slot]))
                    {
                        matchingSlotIndices.Add(slot);
                    }
                }

                int guaranteedSlotIndex;
                if (matchingSlotIndices.Count > 0)
                {
                    // Rule 8: Select from the matching naturally-rolled slots
                    guaranteedSlotIndex = matchingSlotIndices[rng.Next(matchingSlotIndices.Count)];
                }
                else
                {
                    // Rule 8: If no unowned card exists in naturally rolled rarities,
                    // roll a slot that DOES have unowned cards available without forcing Legendary arbitrarily.
                    guaranteedSlotIndex = 4; // Designate the wildcard slot
                    var availableUnownedRarities = unownedCards
                        .Select(c => c.Rarity)
                        .Distinct()
                        .ToList();

                    slotRarities[guaranteedSlotIndex] = RollWeightedRarityFromAvailable(availableUnownedRarities, rng);
                }

                // Fill each of the 5 slots
                for (int slot = 0; slot < CardsPerPack; slot++)
                {
                    if (slot == guaranteedSlotIndex)
                    {
                        // Mandatory guaranteed unowned card
                        var unownedMatching = unownedCards
                            .Where(c => c.Rarity == slotRarities[slot] && !selectedIds.Contains(c.Id))
                            .ToList();

                        if (unownedMatching.Count == 0)
                        {
                            // Fallback to any unowned card if all of that rarity were picked in this pack
                            unownedMatching = unownedCards.Where(c => !selectedIds.Contains(c.Id)).ToList();
                            if (unownedMatching.Count == 0)
                            {
                                unownedMatching = unownedCards;
                            }
                        }

                        var guaranteedCard = unownedMatching[rng.Next(unownedMatching.Count)];
                        rolledCards.Add(guaranteedCard);
                        selectedIds.Add(guaranteedCard.Id);
                    }
                    else
                    {
                        // Rule 4 & 6: Remaining cards rolled via standard rarity distribution (can be duplicates or natural new cards)
                        var card = PickRandomCard(pool, slotRarities[slot], selectedIds, rng);
                        rolledCards.Add(card);
                        selectedIds.Add(card.Id);
                    }
                }
            }

            // Step 6: Record results and immediately update inventory (Rules 10, 11, 12)
            var cardResults = new List<PackOpeningCardResult>(CardsPerPack);
            foreach (var card in rolledCards)
            {
                bool isNew = !profile.IsCardUnlocked(card.Id);
                int dupBefore = profile.GetDuplicateCopies(card.Id);
                int foilLevel = profile.GetFoilLevel(card.Id);

                // Immediately adds card (unlocks if new, or adds duplicate copy) and awards XP
                profile.AddCard(card.Id, 1);

                int dupAfter = profile.GetDuplicateCopies(card.Id);
                cardResults.Add(new PackOpeningCardResult(card, isNew, dupBefore, dupAfter, foilLevel));
            }

            return new CardPackResult(1, cardResults, CardPackType.Purchased, "SOBRE TOMO 1");
        }

        /// <summary>
        /// Generates a single 3-card Player Level Reward Pack and applies it to the player's profile.
        /// Rules:
        /// - Contains exactly 3 cards (CardsPerRewardPack = 3).
        /// - NO guaranteed new card (can contain only duplicates).
        /// - Duplicates obtained count toward Bicolor -> Color -> Gold progression via profile.AddCard.
        /// - Uses the same card pool with a more limited rarity distribution than purchased packs.
        /// </summary>
        public static CardPackResult GenerateRewardPack(
            PlayerProfile profile,
            IReadOnlyList<CardDefinition>? customPool = null,
            Random? rng = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            rng ??= new Random();

            var pool = (customPool ?? CardCatalog.AllCards).ToList();
            if (pool.Count == 0)
            {
                throw new InvalidOperationException("Cannot generate reward pack: card pool is empty.");
            }

            var rolledCards = new List<CardDefinition>(CardsPerRewardPack);
            var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int slot = 0; slot < CardsPerRewardPack; slot++)
            {
                var slotRarity = RollRewardPackSlotRarity(slot, rng);
                var card = PickRandomCard(pool, slotRarity, selectedIds, rng);
                rolledCards.Add(card);
                selectedIds.Add(card.Id);
            }

            var cardResults = new List<PackOpeningCardResult>(CardsPerRewardPack);
            foreach (var card in rolledCards)
            {
                bool isNew = !profile.IsCardUnlocked(card.Id);
                int dupBefore = profile.GetDuplicateCopies(card.Id);
                int foilLevel = profile.GetFoilLevel(card.Id);

                // Immediately adds card (unlocks if new, or adds duplicate copy) and awards progression
                profile.AddCard(card.Id, 1);

                int dupAfter = profile.GetDuplicateCopies(card.Id);
                cardResults.Add(new PackOpeningCardResult(card, isNew, dupBefore, dupAfter, foilLevel));
            }

            return new CardPackResult(1, cardResults, CardPackType.LevelReward, "SOBRE DE RECOMPENSA DE NIVEL");
        }

        /// <summary>
        /// Generates multiple packs sequentially in one transaction (Rules 1 & 13).
        /// Cards unlocked in Pack 1 are counted as OWNED when generating Pack 2.
        /// </summary>
        public static MultiPackPurchaseResult GenerateMultiplePacks(
            PlayerProfile profile,
            int packCount,
            IReadOnlyList<CardDefinition>? customPool = null,
            Random? rng = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (packCount <= 0) throw new ArgumentOutOfRangeException(nameof(packCount), "Pack count must be positive.");

            rng ??= new Random();
            var packs = new List<CardPackResult>(packCount);

            for (int i = 1; i <= packCount; i++)
            {
                var packResult = GeneratePack(profile, customPool, rng);
                packResult.PackIndex = i;
                packs.Add(packResult);
            }

            int totalGoldSpent = packCount * PlayerProfile.PackGoldCost;
            return new MultiPackPurchaseResult(packCount, totalGoldSpent, packs);
        }

        private static CardDefinition PickRandomCard(
            IReadOnlyList<CardDefinition> pool,
            CardRarity targetRarity,
            HashSet<string> alreadyPickedIds,
            Random rng)
        {
            // Try picking an unpicked card of the target rarity
            var candidates = pool
                .Where(c => c.Rarity == targetRarity && !alreadyPickedIds.Contains(c.Id))
                .ToList();

            if (candidates.Count == 0)
            {
                // Fallback to any card of the target rarity
                candidates = pool.Where(c => c.Rarity == targetRarity).ToList();
            }

            if (candidates.Count == 0)
            {
                // Fallback to any card in the pool
                candidates = pool.ToList();
            }

            return candidates[rng.Next(candidates.Count)];
        }

        private static CardRarity RollWeightedRarityFromAvailable(
            IReadOnlyList<CardRarity> availableRarities,
            Random rng)
        {
            if (availableRarities.Count == 0) return CardRarity.Common;
            if (availableRarities.Count == 1) return availableRarities[0];

            // Weight mapping respecting standard drop proportions
            int GetWeight(CardRarity r) => r switch
            {
                CardRarity.Common => 60,
                CardRarity.Rare => 30,
                CardRarity.Epic => 8,
                CardRarity.Legendary => 2,
                _ => 10
            };

            int totalWeight = availableRarities.Sum(GetWeight);
            int roll = rng.Next(0, totalWeight);

            int cumulative = 0;
            foreach (var rarity in availableRarities)
            {
                cumulative += GetWeight(rarity);
                if (roll < cumulative)
                {
                    return rarity;
                }
            }

            return availableRarities[0];
        }
    }
}
