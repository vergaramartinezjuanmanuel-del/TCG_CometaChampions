#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class CardPackGeneratorTests
    {
        [Test]
        public void PackGenerator_AlwaysGuaranteesAtLeastOneNewCard_WhenUnownedCardsExist()
        {
            // Rule 2 & 14: Every pack must contain at least 1 card that the player does not currently own
            var profile = PlayerProfile.CreateDefaultProfile();
            // Default profile starts with 9 cards unlocked, leaving 71 unowned cards
            Assert.That(profile.Inventory.Values.Count(c => c.IsUnlocked), Is.LessThan(CardCatalog.AllCards.Count));

            var result = CardPackGenerator.GeneratePack(profile);

            Assert.That(result.Cards.Count, Is.EqualTo(5));
            Assert.That(result.NewCardsCount, Is.GreaterThanOrEqualTo(1), "Pack must contain at least 1 new card.");
            Assert.That(result.Cards.Any(c => c.IsNew), Is.True);
        }

        [Test]
        public void PackGenerator_NeverProducesFiveDuplicates_WhilePoolHasUnownedCards()
        {
            // Rule 2 & 14: Pack must NEVER contain 5 duplicates while unowned cards remain in pool
            var rng = new Random(12345);

            for (int i = 0; i < 50; i++)
            {
                var profile = PlayerProfile.CreateDefaultProfile();
                var result = CardPackGenerator.GeneratePack(profile, rng: rng);

                Assert.That(result.NewCardsCount, Is.GreaterThanOrEqualTo(1),
                    $"Iteration {i}: Pack contained 0 new cards while 71 cards were unowned!");
            }
        }

        [Test]
        public void PackGenerator_DefinitionOfNewCard_FoilLevelsDoNotCountAsNew()
        {
            // Rule 3: Foil versions do NOT count as new cards
            var profile = new PlayerProfile();
            // Unlock Espectro at FoilLevel 2
            profile.Inventory["CHAMP_ESPECTRO"] = new CardInventoryEntry("CHAMP_ESPECTRO", isUnlocked: true, initialCopies: 5, foilLevel: 2);

            Assert.That(profile.IsCardUnlocked("CHAMP_ESPECTRO"), Is.True);

            // If another Espectro is generated, it is a duplicate, not new
            int beforeCopies = profile.GetDuplicateCopies("CHAMP_ESPECTRO");
            profile.AddCard("CHAMP_ESPECTRO", 1);

            Assert.That(profile.GetDuplicateCopies("CHAMP_ESPECTRO"), Is.EqualTo(beforeCopies + 1));
        }

        [Test]
        public void PackGenerator_AllowsMultipleNewCards_WhenRNGPermits()
        {
            // Rule 6: Packs can contain between 1 and 5 new cards
            var profile = new PlayerProfile(); // completely empty profile (0 cards unlocked)
            var result = CardPackGenerator.GeneratePack(profile);

            Assert.That(result.Cards.Count, Is.EqualTo(5));
            Assert.That(result.NewCardsCount, Is.InRange(1, 5));
        }

        [Test]
        public void PackGenerator_AllCardsOwned_ProducesDuplicatesWithoutError()
        {
            // Rule 7: If all cards are owned, pack generates 5 duplicate cards without error
            var profile = new PlayerProfile();
            foreach (var card in CardCatalog.AllCards)
            {
                profile.Inventory[card.Id] = new CardInventoryEntry(card.Id, isUnlocked: true, initialCopies: 1);
            }

            // Verify 100% unlocked
            Assert.That(CardCatalog.AllCards.All(c => profile.IsCardUnlocked(c.Id)), Is.True);

            var result = CardPackGenerator.GeneratePack(profile);

            Assert.That(result.Cards.Count, Is.EqualTo(5));
            Assert.That(result.NewCardsCount, Is.EqualTo(0), "No new cards possible when all are owned.");
            Assert.That(result.DuplicateCardsCount, Is.EqualTo(5));
            Assert.That(result.Cards.All(c => !c.IsNew), Is.True);
        }

        [Test]
        public void PackGenerator_RarityAdherence_RespectsSlotRarityAndDoesNotForceLegendary()
        {
            // Rule 8: Guaranteed new card must respect slot rarity rules and not force Legendary
            // Suppose player only has Commons unowned
            var profile = new PlayerProfile();
            foreach (var card in CardCatalog.AllCards)
            {
                if (card.Rarity != CardRarity.Common)
                {
                    profile.Inventory[card.Id] = new CardInventoryEntry(card.Id, isUnlocked: true, initialCopies: 1);
                }
            }

            var rng = new Random(42);
            var result = CardPackGenerator.GeneratePack(profile, rng: rng);

            // The guaranteed new card must be a Common, not an Epic or Legendary
            var newCards = result.Cards.Where(c => c.IsNew).ToList();
            Assert.That(newCards.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(newCards.All(c => c.Card.Rarity == CardRarity.Common), Is.True,
                "New card must be Common since only Commons were unowned.");
        }

        [Test]
        public void PackGenerator_MultiPackPurchase_AppliesGuaranteeSequentiallyToEachPack()
        {
            // Rule 13: Guarantee applies to EACH pack individually; cards unlocked in Pack 1 are counted as owned in Pack 2
            var profile = PlayerProfile.CreateDefaultProfile();
            int initialUnlocked = profile.Inventory.Values.Count(c => c.IsUnlocked);

            var multiResult = CardPackGenerator.GenerateMultiplePacks(profile, packCount: 5);

            Assert.That(multiResult.Packs.Count, Is.EqualTo(5));
            Assert.That(multiResult.TotalGoldSpent, Is.EqualTo(5 * PlayerProfile.PackGoldCost));

            // Every pack must have had at least 1 new card
            foreach (var pack in multiResult.Packs)
            {
                Assert.That(pack.NewCardsCount, Is.GreaterThanOrEqualTo(1),
                    $"Pack {pack.PackIndex} failed to guarantee a new card.");
            }

            // Total unlocked cards increased by at least 5
            int finalUnlocked = profile.Inventory.Values.Count(c => c.IsUnlocked);
            Assert.That(finalUnlocked, Is.GreaterThanOrEqualTo(initialUnlocked + 5));
        }

        [Test]
        public void PlayerProfile_TryPurchaseCardPacks_DeductsGoldCorrectly()
        {
            // Rule 1: Pack costs 500 gold coins
            var profile = PlayerProfile.CreateDefaultProfile();
            // Set to MaxAccountLevel to isolate gold deduction from level-up gold bonuses
            profile.AccountLevel = PlayerProfile.MaxAccountLevel;
            profile.GoldCoins = 1500;

            bool success = profile.TryPurchaseCardPacks(2, out var result, out string error);

            Assert.That(success, Is.True);
            Assert.That(string.IsNullOrEmpty(error), Is.True);
            Assert.That(profile.GoldCoins, Is.EqualTo(500)); // 1500 - (2 * 500)
            Assert.That(result.Packs.Count, Is.EqualTo(2));
            Assert.That(result.TotalGoldSpent, Is.EqualTo(1000));
        }

        [Test]
        public void PlayerProfile_TryPurchaseCardPacks_FailsWhenInsufficientGold()
        {
            // Rule 1: Insufficient gold prevents transaction
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.GoldCoins = 450; // Needs 500

            bool success = profile.TryPurchaseCardPacks(1, out var result, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Monedas insuficientes"));
            Assert.That(profile.GoldCoins, Is.EqualTo(450)); // untouched
            Assert.That(result.Packs.Count, Is.EqualTo(0));
        }

        [Test]
        public void PlayerProfile_TryOpenBoosterPack_DecrementsBoosterCount_AndGeneratesPack()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.BoosterPacks = 3;

            bool success = profile.TryOpenBoosterPack(out var result, out string error);

            Assert.That(success, Is.True);
            Assert.That(profile.BoosterPacks, Is.EqualTo(2));
            Assert.That(result.Cards.Count, Is.EqualTo(5));
            Assert.That(result.NewCardsCount, Is.GreaterThanOrEqualTo(1));
        }
    }
}
