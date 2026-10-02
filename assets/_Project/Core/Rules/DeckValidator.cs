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
        public const int DuplicateCardDeckGoldCost = 50;

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

            // 2. Ownership check: every card must be unlocked in profile inventory (all 4 card types allowed)
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

            // Alias check for Arenas
            if (cardId.Equals("ARENA_TEMPLO", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_TEMPLO_MILENARIO"))
                return true;
            if (cardId.Equals("ARENA_TEMPLO_MILENARIO", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_TEMPLO"))
                return true;
            if (cardId.Equals("ARENA_PISTA", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_PISTA_CARRERAS"))
                return true;
            if (cardId.Equals("ARENA_PISTA_CARRERAS", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_PISTA"))
                return true;
            if (cardId.Equals("ARENA_LLANURAS", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_LLANURAS_TIRANO"))
                return true;
            if (cardId.Equals("ARENA_LLANURAS_TIRANO", StringComparison.OrdinalIgnoreCase) && profile.IsCardUnlocked("ARENA_LLANURAS"))
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

            // 1. Gather all unlocked playable cards from profile inventory (all 4 card categories allowed)
            var unlockedCards = new List<CardDefinition>();
            foreach (var kvp in profile.Inventory)
            {
                if (!kvp.Value.IsUnlocked) continue;

                var cardDef = MvpCardCatalog.GetCardById(kvp.Key) ?? CardCatalog.GetCardById(kvp.Key);
                if (cardDef != null)
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

        /// <summary>
        /// Retrieves the number of copies of a card owned in the player profile, supporting ID aliases.
        /// </summary>
        public static int GetOwnedCopiesInProfile(PlayerProfile profile, string cardId)
        {
            if (profile == null || string.IsNullOrEmpty(cardId)) return 0;

            int copies = profile.GetDuplicateCopies(cardId);
            if (copies > 0) return copies;

            if (cardId.StartsWith("HERO_", StringComparison.OrdinalIgnoreCase))
            {
                string champId = "CHAMP_" + cardId.Substring(5);
                copies = profile.GetDuplicateCopies(champId);
                if (copies > 0) return copies;
            }
            else if (cardId.StartsWith("CHAMP_", StringComparison.OrdinalIgnoreCase))
            {
                string heroId = "HERO_" + cardId.Substring(6);
                copies = profile.GetDuplicateCopies(heroId);
                if (copies > 0) return copies;
            }

            if (cardId.Equals("BEAST_LEVIATAN", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("BEAST_DIAMANTE");
            if (cardId.Equals("BEAST_DIAMANTE", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("BEAST_LEVIATAN");
            if (cardId.Equals("ARENA_TEMPLO", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_TEMPLO_MILENARIO");
            if (cardId.Equals("ARENA_TEMPLO_MILENARIO", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_TEMPLO");
            if (cardId.Equals("ARENA_PISTA", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_PISTA_CARRERAS");
            if (cardId.Equals("ARENA_PISTA_CARRERAS", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_PISTA");
            if (cardId.Equals("ARENA_LLANURAS", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_LLANURAS_TIRANO");
            if (cardId.Equals("ARENA_LLANURAS_TIRANO", StringComparison.OrdinalIgnoreCase))
                return profile.GetDuplicateCopies("ARENA_LLANURAS");

            return 0;
        }

        /// <summary>
        /// Checks whether two card IDs refer to the same card (accounting for CHAMP_/HERO_ and Arena/Beast aliases).
        /// </summary>
        public static bool AreCardIdsEquivalent(string? idA, string? idB)
        {
            if (string.IsNullOrEmpty(idA) || string.IsNullOrEmpty(idB)) return false;
            if (string.Equals(idA, idB, StringComparison.OrdinalIgnoreCase)) return true;

            var defA = CardCatalog.GetCardById(idA!) ?? MvpCardCatalog.GetCardById(idA!);
            var defB = CardCatalog.GetCardById(idB!) ?? MvpCardCatalog.GetCardById(idB!);
            if (defA != null && defB != null && string.Equals(defA.Name, defB.Name, StringComparison.OrdinalIgnoreCase) && defA.Category == defB.Category)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to add a card to the player's combat deck.
        /// - 1st copy of an unlocked card is free.
        /// - 2nd copy (duplicate) requires owning at least 2 copies in inventory AND paying 50 Gold Coins.
        /// - Maximum 2 copies per card and 12 cards total in deck.
        /// </summary>
        public static bool TryAddCardToDeck(PlayerProfile profile, IList<string> deckCardIds, string cardId, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (profile == null || deckCardIds == null || string.IsNullOrEmpty(cardId))
            {
                errorMessage = "Datos de perfil o mazo inválidos.";
                return false;
            }

            if (deckCardIds.Count >= ArenaRequiredDeckSize)
            {
                errorMessage = $"Tu mazo ya está completo ({ArenaRequiredDeckSize}/{ArenaRequiredDeckSize} cartas).";
                return false;
            }

            if (!IsCardUnlockedInProfile(profile, cardId))
            {
                errorMessage = "Esta tarjeta aún no está desbloqueada en tu inventario.";
                return false;
            }

            int copiesInDeck = deckCardIds.Count(id => AreCardIdsEquivalent(id, cardId));
            if (copiesInDeck >= ArenaMaxCopiesPerCard)
            {
                errorMessage = $"Solo puedes agregar un máximo de {ArenaMaxCopiesPerCard} veces la misma tarjeta al mazo.";
                return false;
            }

            if (copiesInDeck == 0)
            {
                deckCardIds.Add(cardId);
                return true;
            }

            // Adding a duplicate (2nd copy) requires >= 2 copies in inventory AND 50 Gold Coins
            int ownedCopies = GetOwnedCopiesInProfile(profile, cardId);
            if (ownedCopies < 2)
            {
                errorMessage = $"Para agregar una copia duplicada (2/2) al mazo necesitas tener al menos 2 copias de esta tarjeta en tu inventario (Tienes: {ownedCopies}) y {DuplicateCardDeckGoldCost} monedas de oro.";
                return false;
            }

            if (profile.GoldCoins < DuplicateCardDeckGoldCost)
            {
                errorMessage = $"Para agregar una copia duplicada (2/2) al mazo necesitas {DuplicateCardDeckGoldCost} monedas de oro (Tienes: {profile.GoldCoins}).";
                return false;
            }

            profile.GoldCoins -= DuplicateCardDeckGoldCost;
            deckCardIds.Add(cardId);
            return true;
        }

        /// <summary>
        /// Auto-completes the given deck by preserving existing cards and filling empty slots (up to 12)
        /// exclusively with random unlocked cards from inventory that are NOT already in the deck (0 duplicates added).
        /// Returns the number of cards added.
        /// </summary>
        public static int AutoCompleteDeckWithoutDuplicates(PlayerProfile profile, IList<string> deckCardIds, Random? rng = null)
        {
            if (profile == null || profile.Inventory == null || deckCardIds == null) return 0;
            if (deckCardIds.Count >= ArenaRequiredDeckSize) return 0;

            var random = rng ?? new Random();

            var candidateIds = new List<string>();
            foreach (var kvp in profile.Inventory)
            {
                if (!kvp.Value.IsUnlocked) continue;

                var cardDef = CardCatalog.GetCardById(kvp.Key) ?? MvpCardCatalog.GetCardById(kvp.Key);
                if (cardDef == null) continue;

                bool alreadyInDeck = deckCardIds.Any(existingId => AreCardIdsEquivalent(existingId, cardDef.Id) || AreCardIdsEquivalent(existingId, kvp.Key));

                if (!alreadyInDeck && !candidateIds.Any(cId => AreCardIdsEquivalent(cId, cardDef.Id)))
                {
                    candidateIds.Add(cardDef.Id);
                }
            }

            var shuffled = candidateIds.OrderBy(_ => random.Next()).ToList();
            int addedCount = 0;

            foreach (var id in shuffled)
            {
                if (deckCardIds.Count >= ArenaRequiredDeckSize) break;
                deckCardIds.Add(id);
                addedCount++;
            }

            return addedCount;
        }
    }
}
