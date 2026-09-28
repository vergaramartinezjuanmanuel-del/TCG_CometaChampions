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

            // Use 6 unlocked starter cards, 2 copies each = 12 cards
            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO", "TROMPO_AZTECA" };
            foreach (var id in starterIds)
            {
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

            // 5 unlocked starter cards (2 copies each = 10 cards)
            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO" };
            foreach (var id in starterIds)
            {
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
        public void ValidateDeckOwnership_ContainsArenaCard_ReturnsFalse()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var cards = new List<CardDefinition>();

            var starterIds = new[] { "CHAMP_ESPECTRO", "CHAMP_AZTECA", "CHAMP_DRAKY", "CHAMP_ORION", "TROMPO_ESPECTRO" };
            foreach (var id in starterIds)
            {
                var card = MvpCardCatalog.GetCardById(id) ?? CardCatalog.GetCardById(id);
                cards.Add(card!);
                cards.Add(card!);
            }

            // Add 2 copies of an Arena card (arenas are battlefield locations, cannot be in combat hand deck)
            var arenaCard = CardCatalog.GetCardById("ARENA_TEMPLO_MILENARIO") ?? MvpCardCatalog.GetCardById("ARENA_TEMPLO");
            Assert.IsNotNull(arenaCard);
            cards.Add(arenaCard!);
            cards.Add(arenaCard!);

            bool isValid = DeckValidator.ValidateDeckOwnership(cards, profile, out var errors);
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("Arena card")));
        }

        [Test]
        public void CreateDeckFromInventory_ProducesExactly12Cards_FromUnlockedPoolOnly()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var deck = DeckValidator.CreateDeckFromInventory(profile, new System.Random(42));

            Assert.AreEqual(DeckValidator.ArenaRequiredDeckSize, deck.Count, "Deck should have exactly 12 cards.");

            // Verify every single card is unlocked in the player's profile
            foreach (var card in deck)
            {
                bool isUnlocked = DeckValidator.IsCardUnlockedInProfile(profile, card.Id);
                Assert.IsTrue(isUnlocked, $"Card '{card.Name}' ({card.Id}) in deck must be unlocked in profile inventory.");
                Assert.AreNotEqual(CardCategory.Arena, card.Category, "No Arena cards allowed in combat deck.");
            }

            // Verify ownership validation passes
            bool isValid = DeckValidator.ValidateDeckOwnership(deck, profile, out var errors);
            Assert.IsTrue(isValid, string.Join("; ", errors));
        }

        [Test]
        public void CreateDeckFromInventory_NeverExceeds2CopiesPerCard()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
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
    }
}
