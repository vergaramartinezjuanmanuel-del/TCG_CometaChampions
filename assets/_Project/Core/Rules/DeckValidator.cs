using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Validates deck construction rules for official Cometa Champions matches:
    /// 1. Exactly 30 cards per deck for standard mode, 12 cards for Arena Combat.
    /// 2. Maximum of 3 copies per card (standard), 2 copies per card (Arena Combat).
    /// 3. In combat, players can ONLY use cards they own in their inventory.
    /// </summary>
    public static class DeckValidator
    {
        public const int RequiredDeckSize = 30;
        public const int MaxCopiesPerCard = 3;

        public const int ArenaRequiredDeckSize = 12;
        public const int ArenaMaxCopiesPerCard = 2;

        public static bool Validate(IEnumerable<CardDefinition> deckCards, out List<string> errors)
        {
            errors = new List<string>();
            var cardsList = deckCards?.ToList() ?? new List<CardDefinition>();

            if (cardsList.Count != RequiredDeckSize)
            {
                errors.Add($"Deck must contain exactly {RequiredDeckSize} cards. Current count: {cardsList.Count}.");
            }

            var cardCounts = cardsList
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > MaxCopiesPerCard);

            foreach (var group in cardCounts)
            {
                var cardName = group.First().Name;
                errors.Add($"Card '{cardName}' (ID: {group.Key}) exceeds maximum allowed copies of {MaxCopiesPerCard} (Current: {group.Count()}).");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates deck construction rules for Arena Combat according to Rule 1:
        /// - Exactly 12 cards
        /// - Maximum 2 copies per card
        /// </summary>
        public static bool ValidateArenaDeck(IEnumerable<CardDefinition> deckCards, out List<string> errors)
        {
            errors = new List<string>();
            var cardsList = deckCards?.ToList() ?? new List<CardDefinition>();

            if (cardsList.Count != ArenaRequiredDeckSize)
            {
                errors.Add($"Arena Deck must contain exactly {ArenaRequiredDeckSize} cards. Current count: {cardsList.Count}.");
            }

            var cardCounts = cardsList
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > ArenaMaxCopiesPerCard);

            foreach (var group in cardCounts)
            {
                var cardName = group.First().Name;
                errors.Add($"Card '{cardName}' (ID: {group.Key}) exceeds maximum allowed copies of {ArenaMaxCopiesPerCard} (Current: {group.Count()}).");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates that an Arena combat deck meets all Rule 1 criteria AND contains ONLY
        /// cards that are unlocked and possessed in the player's profile inventory.
        /// </summary>
        public static bool ValidateDeckOwnership(
            IEnumerable<CardDefinition> deckCards,
            PlayerProfile? profile,
            out List<string> errors)
        {
            errors = new List<string>();
            if (profile == null)
            {
                errors.Add("Player profile is required to validate card ownership.");
                return false;
            }

            var cardsList = deckCards?.ToList() ?? new List<CardDefinition>();

            // 1. General Arena structure (12 cards, max 2 copies)
            ValidateArenaDeck(cardsList, out var arenaErrors);
            errors.AddRange(arenaErrors);

            // 2. Arenas cannot be in combat decks
            foreach (var card in cardsList)
            {
                if (card.Category == CardCategory.Arena || card.Type == CardType.Arena)
                {
                    errors.Add($"Arena card '{card.Name}' ({card.Id}) cannot be included in a player's combat hand deck.");
                }
            }

            // 3. Ownership check: every card must be unlocked in profile inventory
            var uniqueGroups = cardsList.GroupBy(c => c.Id);
            foreach (var group in uniqueGroups)
            {
                string cardId = group.Key;
                bool isUnlocked = IsCardUnlockedInProfile(profile, cardId);

                if (!isUnlocked)
                {
                    var cardName = group.First().Name;
                    errors.Add($"Card '{cardName}' (ID: {cardId}) is not unlocked in player inventory.");
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Checks whether a card is unlocked in the player profile, supporting ID aliases.
        /// </summary>
        public static bool IsCardUnlockedInProfile(PlayerProfile profile, string cardId)
        {
            if (profile == null || string.IsNullOrEmpty(cardId)) return false;

            if (profile.IsCardUnlocked(cardId)) return true;

            // Alias check for CHAMP_ <-> HERO_
            if (cardId.StartsWith("HERO_", StringComparison.OrdinalIgnoreCase))
            {
                string champId = "CHAMP_" + cardId.Substring(5);
                if (profile.IsCardUnlocked(champId)) return true;
            }
            else if (cardId.StartsWith("CHAMP_", StringComparison.OrdinalIgnoreCase))
            {
                string heroId = "HERO_" + cardId.Substring(6);
                if (profile.IsCardUnlocked(heroId)) return true;
            }

            // Alias check for Leviatán / Diamante
            if (cardId.Equals("BEAST_LEVIATAN", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("BEAST_DIAMANTE"))
                return true;
            if (cardId.Equals("BEAST_DIAMANTE", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("BEAST_LEVIATAN"))
                return true;

            return false;
        }

        /// <summary>
        /// Constructs a balanced, legal 12-card Arena Combat deck strictly and exclusively from
        /// cards that are currently unlocked in the player's profile inventory.
        /// Respects Rule 1: Exactly 12 cards, maximum 2 copies per card.
        /// </summary>
        public static List<CardDefinition> CreateDeckFromInventory(PlayerProfile profile, Random? rng = null)
        {
            var random = rng ?? new Random();
            var deck = new List<CardDefinition>();

            if (profile == null || profile.Inventory == null) return deck;

            // 1. Gather all unlocked playable cards from profile inventory
            var unlockedCards = new List<CardDefinition>();
            foreach (var kvp in profile.Inventory)
            {
                if (!kvp.Value.IsUnlocked) continue;

                var cardDef = MvpCardCatalog.GetCardById(kvp.Key) ?? CardCatalog.GetCardById(kvp.Key);
                if (cardDef != null && cardDef.Category != CardCategory.Arena && cardDef.Type != CardType.Arena)
                {
                    unlockedCards.Add(cardDef);
                }
            }

            if (unlockedCards.Count == 0)
            {
                return deck;
            }

            // 2. Order by energy cost with shuffle for variety within same cost
            var pool = unlockedCards
                .OrderBy(c => c.EnergyCost)
                .ThenBy(_ => random.Next())
                .ToList();

            // First pass: add 1 copy of distinct cards with a balanced curve
            foreach (var card in pool)
            {
                if (deck.Count >= ArenaRequiredDeckSize) break;
                deck.Add(card);
            }

            // Second pass: add second copy up to ArenaMaxCopiesPerCard (2) to reach 12 cards
            if (deck.Count < ArenaRequiredDeckSize)
            {
                var candidates = pool
                    .Where(c => deck.Count(d => d.Id == c.Id) < ArenaMaxCopiesPerCard)
                    .OrderBy(c => c.EnergyCost)
                    .ThenBy(_ => random.Next())
                    .ToList();

                foreach (var card in candidates)
                {
                    if (deck.Count >= ArenaRequiredDeckSize) break;
                    deck.Add(card);
                }
            }

            // Fallback pass: keep adding allowed copies until exactly 12 cards
            while (deck.Count < ArenaRequiredDeckSize)
            {
                var candidate = pool.FirstOrDefault(c => deck.Count(d => d.Id == c.Id) < ArenaMaxCopiesPerCard);
                if (candidate != null)
                {
                    deck.Add(candidate);
                }
                else
                {
                    // Inventory has fewer than 6 unique non-arena cards
                    break;
                }
            }

            return deck;
        }
    }
}
