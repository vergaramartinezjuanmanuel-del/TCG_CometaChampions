using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class DeckValidatorTests
    {
        private CardDefinition CreateDummyCard(string id) =>
            new CardDefinition(id, id, CardCategory.Champion, ClanType.Milenarios, 1, 1, 1, "", "");

        [Test]
        public void Validate_Valid30CardDeck_ReturnsTrue()
        {
            var cards = new List<CardDefinition>();
            // 10 distinct cards, 3 copies each = 30 cards
            for (int i = 0; i < 10; i++)
            {
                var card = CreateDummyCard($"CARD_{i}");
                cards.Add(card);
                cards.Add(card);
                cards.Add(card);
            }

            bool isValid = DeckValidator.Validate(cards, out var errors);
            Assert.IsTrue(isValid);
            Assert.IsEmpty(errors);
        }

        [Test]
        public void Validate_DeckWithLessThan30Cards_ReturnsFalse()
        {
            var cards = new List<CardDefinition>();
            for (int i = 0; i < 29; i++)
            {
                cards.Add(CreateDummyCard($"CARD_{i}"));
            }

            bool isValid = DeckValidator.Validate(cards, out var errors);
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("exactly 30 cards")));
        }

        [Test]
        public void Validate_DeckWithMoreThan3Copies_ReturnsFalse()
        {
            var cards = new List<CardDefinition>();
            var repeatedCard = CreateDummyCard("REPEATED_CARD");

            // Add 4 copies of one card
            for (int i = 0; i < 4; i++) cards.Add(repeatedCard);

            // Fill remainder to reach 30
            for (int i = 0; i < 26; i++) cards.Add(CreateDummyCard($"CARD_{i}"));

            bool isValid = DeckValidator.Validate(cards, out var errors);
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("exceeds maximum allowed copies")));
        }

        [Test]
        public void ValidateArenaDeck_Valid12CardDeckWithMax2Copies_ReturnsTrue()
        {
            var cards = new List<CardDefinition>();
            // 6 distinct cards, 2 copies each = 12 cards
            for (int i = 0; i < 6; i++)
            {
                var card = CreateDummyCard($"ARENA_CARD_{i}");
                cards.Add(card);
                cards.Add(card);
            }

            bool isValid = DeckValidator.ValidateArenaDeck(cards, out var errors);
            Assert.IsTrue(isValid);
            Assert.IsEmpty(errors);
        }

        [Test]
        public void ValidateArenaDeck_InvalidCountOrMoreThan2Copies_ReturnsFalse()
        {
            var cards = new List<CardDefinition>();
            var repeatedCard = CreateDummyCard("TRIPLE_CARD");
            cards.Add(repeatedCard);
            cards.Add(repeatedCard);
            cards.Add(repeatedCard); // 3 copies!
            for (int i = 0; i < 9; i++) cards.Add(CreateDummyCard($"CARD_{i}")); // 12 cards total

            bool isValid = DeckValidator.ValidateArenaDeck(cards, out var errors);
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("exceeds maximum allowed copies of 2")));

            var smallDeck = new List<CardDefinition> { repeatedCard };
            bool isSmallValid = DeckValidator.ValidateArenaDeck(smallDeck, out var smallErrors);
            Assert.IsFalse(isSmallValid);
            Assert.IsTrue(smallErrors.Exists(e => e.Contains("exactly 12 cards")));
        }

        [Test]
        public void ValidateDeckOwnership_AllCardsUnlocked_ReturnsTrue()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var cards = new List<CardDefinition>();

            // Unlock 6 test cards, 2 copies each = 12 cards
            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO", "TROMPO_AZTECA" };
            foreach (var id in starterIds)
            {
                profile.AddCard(id, 1);
                var card = MvpCardCatalog.GetCardById(id) ?? CardCatalog.GetCardById(id);
                Assert.IsNotNull(card, $"Card '{id}' should be found in catalogs.");
                cards.Add(card!);
                cards.Add(card!);
            }

            bool isValid = DeckValidator.ValidateDeckOwnership(cards, profile, out var errors);
            Assert.IsTrue(isValid, string.Join("; ", errors));
            Assert.IsEmpty(errors);
        }

        [Test]
        public void ValidateDeckOwnership_ContainsLockedCard_ReturnsFalse()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var cards = new List<CardDefinition>();

            // 5 unlocked cards (2 copies each = 10 cards)
            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO" };
            foreach (var id in starterIds)
            {
                profile.AddCard(id, 1);
                var card = MvpCardCatalog.GetCardById(id) ?? CardCatalog.GetCardById(id);
                cards.Add(card!);
                cards.Add(card!);
            }

            // Add 2 copies of Avispón (which is locked by default in starter profile)
            var lockedCard = MvpCardCatalog.GetCardById("CHAMP_AVISPON") ?? CardCatalog.GetCardById("CHAMP_AVISPON");
            Assert.IsNotNull(lockedCard);
            cards.Add(lockedCard!);
            cards.Add(lockedCard!);

            bool isValid = DeckValidator.ValidateDeckOwnership(cards, profile, out var errors);
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("not unlocked in player inventory")));
        }

        [Test]
        public void ValidateDeckOwnership_AllowsUnlockedArenaAndTrompoCards_ReturnsTrue()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var cards = new List<CardDefinition>();

            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO" };
            foreach (var id in starterIds)
            {
                profile.AddCard(id, 1);
                var card = MvpCardCatalog.GetCardById(id) ?? CardCatalog.GetCardById(id);
                Assert.IsNotNull(card);
                cards.Add(card!);
                cards.Add(card!);
            }

            // Unlock and add 2 copies of Arena card (Templo Milenario)
            profile.AddCard("ARENA_TEMPLO_MILENARIO", 1);
            var arenaCard = CardCatalog.GetCardById("ARENA_TEMPLO_MILENARIO") ?? MvpCardCatalog.GetCardById("ARENA_TEMPLO");
            Assert.IsNotNull(arenaCard);
            Assert.Greater(arenaCard!.EnergyCost, 0, "Arena card must have positive EnergyCost.");
            Assert.Greater(arenaCard.BaseAttack, 0, "Arena card must have positive BaseAttack.");
            Assert.Greater(arenaCard.BaseHealth, 0, "Arena card must have positive BaseHealth.");
            cards.Add(arenaCard);
            cards.Add(arenaCard);

            bool isValid = DeckValidator.ValidateDeckOwnership(cards, profile, out var errors);
            Assert.IsTrue(isValid, string.Join("; ", errors));
            Assert.IsEmpty(errors);
        }

        [Test]
        public void CreateDeckFromInventory_ProducesExactly12Cards_FromUnlockedPoolOnly()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.TryOpenBoosterPack(out _, out _); // Open initial free booster pack to reach >= 8 unlocked cards
            var deck = DeckValidator.CreateDeckFromInventory(profile, new System.Random(42));

            Assert.AreEqual(DeckValidator.ArenaRequiredDeckSize, deck.Count, "Deck should have exactly 12 cards.");

            // Verify every single card is unlocked in the player's profile and has valid Cost, Attack, and Health
            foreach (var card in deck)
            {
                bool isUnlocked = DeckValidator.IsCardUnlockedInProfile(profile, card.Id);
                Assert.IsTrue(isUnlocked, $"Card '{card.Name}' ({card.Id}) in deck must be unlocked in profile inventory.");
                Assert.Greater(card.EnergyCost, 0, $"Card '{card.Name}' must have positive EnergyCost.");
                Assert.Greater(card.BaseAttack, 0, $"Card '{card.Name}' must have positive BaseAttack.");
                Assert.Greater(card.BaseHealth, 0, $"Card '{card.Name}' must have positive BaseHealth.");
            }

            // Verify ownership validation passes
            bool isValid = DeckValidator.ValidateDeckOwnership(deck, profile, out var errors);
            Assert.IsTrue(isValid, string.Join("; ", errors));
        }

        [Test]
        public void CreateDeckFromInventory_NeverExceeds2CopiesPerCard()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.TryOpenBoosterPack(out _, out _);
            var deck = DeckValidator.CreateDeckFromInventory(profile, new System.Random(12345));

            var groups = deck.GroupBy(c => c.Id);
            foreach (var g in groups)
            {
                Assert.LessOrEqual(g.Count(), DeckValidator.ArenaMaxCopiesPerCard, $"Card {g.Key} must not exceed {DeckValidator.ArenaMaxCopiesPerCard} copies.");
            }
        }

        [Test]
        public void MvpCardCatalog_AliasResolution_ResolvesChampToHero()
        {
            var champAzteca = MvpCardCatalog.GetCardById("CHAMP_AZTECA");
            Assert.IsNotNull(champAzteca);
            Assert.AreEqual("Azteca", champAzteca!.Name);

            var heroAzteca = MvpCardCatalog.GetCardById("HERO_AZTECA");
            Assert.IsNotNull(heroAzteca);
            Assert.AreEqual("Azteca", heroAzteca!.Name);

            var leviatan = MvpCardCatalog.GetCardById("BEAST_DIAMANTE");
            Assert.IsNotNull(leviatan);
            Assert.AreEqual("Diamante Leviatán", leviatan!.Name);
        }

        [Test]
        public void TryAddCardToDeck_FirstCopyFree_SecondCopyRequiresTwoOwnedCopiesAnd50Gold()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var deckIds = new List<string>();

            // 1. First copy of unlocked starter card (CHAMP_DRAKY, 1 copy owned, 0 gold) is FREE
            bool addedFirst = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err1);
            Assert.IsTrue(addedFirst, err1);
            Assert.AreEqual(1, deckIds.Count);
            Assert.AreEqual(0, profile.GoldCoins);

            // 2. Second copy fails when player only owns 1 copy in inventory (even if they have 100 gold)
            profile.GoldCoins = 100;
            bool addedSecondWithoutInventoryCopy = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err2);
            Assert.IsFalse(addedSecondWithoutInventoryCopy);
            StringAssert.Contains("2 copias", err2);
            Assert.AreEqual(1, deckIds.Count);
            Assert.AreEqual(100, profile.GoldCoins);

            // 3. Give player a 2nd copy in inventory, but set gold < 50 -> fails due to insufficient gold
            profile.AddCard("CHAMP_DRAKY", 1); // Now 2 copies in inventory
            profile.GoldCoins = 30;
            bool addedSecondWithoutGold = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err3);
            Assert.IsFalse(addedSecondWithoutGold);
            StringAssert.Contains("50 monedas", err3);
            Assert.AreEqual(1, deckIds.Count);
            Assert.AreEqual(30, profile.GoldCoins);

            // 4. With 2 copies in inventory AND >= 50 gold -> succeeds and deducts 50 gold!
            profile.GoldCoins = 120;
            bool addedSecondSuccess = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err4);
            Assert.IsTrue(addedSecondSuccess, err4);
            Assert.AreEqual(2, deckIds.Count);
            Assert.AreEqual(70, profile.GoldCoins);

            // 5. Third copy is blocked (max 2 copies per card)
            profile.AddCard("CHAMP_DRAKY", 1);
            bool addedThird = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err5);
            Assert.IsFalse(addedThird);
            Assert.AreEqual(2, deckIds.Count);
            Assert.AreEqual(70, profile.GoldCoins);

            // 6. Removing duplicate copy does not refund gold, and adding it again costs 50 gold
            deckIds.Remove("CHAMP_DRAKY");
            Assert.AreEqual(1, deckIds.Count);
            Assert.AreEqual(70, profile.GoldCoins);

            bool reAddedSecond = DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out var err6);
            Assert.IsTrue(reAddedSecond, err6);
            Assert.AreEqual(2, deckIds.Count);
            Assert.AreEqual(20, profile.GoldCoins);
        }

        [Test]
        public void AutoCompleteDeckWithoutDuplicates_PreservesExistingCards_AndNeverAddsDuplicates()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var deckIds = new List<string>();

            // 1. With only 4 starter cards unlocked, AutoComplete adds only those 4 unique cards (no duplicates!)
            int addedStarter = DeckValidator.AutoCompleteDeckWithoutDuplicates(profile, deckIds, new System.Random(42));
            Assert.AreEqual(4, addedStarter);
            Assert.AreEqual(4, deckIds.Count);
            Assert.AreEqual(4, deckIds.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(), "Must not contain any duplicate cards.");

            // 2. Suppose player manually paid for 1 duplicate of CHAMP_DRAKY (so deck has 5 cards, 4 unique)
            // and unlocked 10 more unique cards (14 unique unlocked total)
            profile.AddCard("CHAMP_DRAKY", 1);
            profile.GoldCoins = 100;
            Assert.IsTrue(DeckValidator.TryAddCardToDeck(profile, deckIds, "CHAMP_DRAKY", out _));
            Assert.AreEqual(5, deckIds.Count);

            var extraCards = CardCatalog.AllCards.Where(c => !profile.IsCardUnlocked(c.Id)).Take(10).ToList();
            foreach (var extra in extraCards)
            {
                profile.AddCard(extra.Id, 1);
            }

            // AutoComplete must preserve the 5 existing cards and add 7 unique unlocked cards to reach 12
            int addedRemaining = DeckValidator.AutoCompleteDeckWithoutDuplicates(profile, deckIds, new System.Random(99));
            Assert.AreEqual(7, addedRemaining);
            Assert.AreEqual(DeckValidator.ArenaRequiredDeckSize, deckIds.Count);

            // Only CHAMP_DRAKY (which was manually duplicated before AutoComplete) has 2 copies; all others have 1 copy
            var groups = deckIds.GroupBy(id => id, System.StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var g in groups)
            {
                if (string.Equals(g.Key, "CHAMP_DRAKY", System.StringComparison.OrdinalIgnoreCase))
                    Assert.AreEqual(2, g.Count());
                else
                    Assert.AreEqual(1, g.Count(), $"Auto-completed card {g.Key} must have only 1 copy.");
            }
        }
    }
}
