#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class CardCraftingTests
    {
        [TearDown]
        public void TearDown()
        {
            CardCraftingConfig.ResetToDefault();
        }

        [Test]
        public void CraftCosts_AdhereToConfiguredRarityDefaults()
        {
            // Default Crafting Costs (Option A):
            // Common: 50, Rare: 100, Epic: 300, Legendary: 800
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Common), Is.EqualTo(50));
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Rare), Is.EqualTo(100));
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Epic), Is.EqualTo(300));
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Legendary), Is.EqualTo(800));
        }

        [Test]
        public void DisenchantYields_AdhereToConfiguredRarityDefaults()
        {
            // Default Disenchant Yields:
            // Common: 10, Rare: 25, Epic: 75, Legendary: 200
            Assert.That(CardCraftingConfig.GetDisenchantYield(CardRarity.Common), Is.EqualTo(10));
            Assert.That(CardCraftingConfig.GetDisenchantYield(CardRarity.Rare), Is.EqualTo(25));
            Assert.That(CardCraftingConfig.GetDisenchantYield(CardRarity.Epic), Is.EqualTo(75));
            Assert.That(CardCraftingConfig.GetDisenchantYield(CardRarity.Legendary), Is.EqualTo(200));
        }

        [Test]
        public void TryCraftCard_Succeeds_WhenSufficientDust_AndUnlocksCard()
        {
            var profile = new PlayerProfile
            {
                EsenciaCometa = 200,
                AccountLevel = 1,
                CurrentLevelXp = 0
            };

            var commonCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Common);
            Assert.That(profile.IsCardUnlocked(commonCard.Id), Is.False);

            bool success = profile.TryCraftCard(commonCard, out string error);

            Assert.That(success, Is.True);
            Assert.That(string.IsNullOrEmpty(error), Is.True);
            Assert.That(profile.IsCardUnlocked(commonCard.Id), Is.True);
            Assert.That(profile.EsenciaCometa, Is.EqualTo(150)); // 200 - 50
            Assert.That(profile.CurrentLevelXp, Is.EqualTo(40)); // 40 XP for new card unlock
        }

        [Test]
        public void TryCraftCard_AddsDuplicateCopies_WhenCardAlreadyUnlocked()
        {
            var profile = new PlayerProfile
            {
                EsenciaCometa = 350
            };

            var rareCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Rare);
            profile.Inventory[rareCard.Id] = new CardInventoryEntry(rareCard.Id, isUnlocked: true, initialCopies: 2);

            Assert.That(profile.IsCardUnlocked(rareCard.Id), Is.True);
            Assert.That(profile.GetDuplicateCopies(rareCard.Id), Is.EqualTo(2));

            bool success = profile.TryCraftCard(rareCard, out string error);

            Assert.That(success, Is.True);
            Assert.That(profile.EsenciaCometa, Is.EqualTo(250)); // 350 - 100
            Assert.That(profile.GetDuplicateCopies(rareCard.Id), Is.EqualTo(3)); // 2 + 1
        }

        [Test]
        public void TryCraftCard_Fails_WhenInsufficientDust()
        {
            var profile = new PlayerProfile
            {
                EsenciaCometa = 40
            };

            var commonCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Common); // Costs 50

            bool success = profile.TryCraftCard(commonCard, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Esencia insuficiente"));
            Assert.That(profile.EsenciaCometa, Is.EqualTo(40)); // Untouched
            Assert.That(profile.IsCardUnlocked(commonCard.Id), Is.False);
        }

        [Test]
        public void TryCraftCard_Fails_WhenCardIsNull()
        {
            var profile = new PlayerProfile { EsenciaCometa = 500 };

            bool success = profile.TryCraftCard(null!, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("inválida"));
            Assert.That(profile.EsenciaCometa, Is.EqualTo(500));
        }

        [Test]
        public void CanCraftCard_ReturnsAccurateAvailability()
        {
            var profile = new PlayerProfile { EsenciaCometa = 100 };
            var commonCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Common); // 50
            var rareCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Rare);     // 100
            var epicCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Epic);     // 300

            Assert.That(profile.CanCraftCard(commonCard, out int commonCost), Is.True);
            Assert.That(commonCost, Is.EqualTo(50));

            Assert.That(profile.CanCraftCard(rareCard, out int rareCost), Is.True);
            Assert.That(rareCost, Is.EqualTo(100));

            Assert.That(profile.CanCraftCard(epicCard, out int epicCost), Is.False);
            Assert.That(epicCost, Is.EqualTo(300));
        }

        [Test]
        public void TryDisenchantCard_Succeeds_WhenDuplicateCopiesAvailable()
        {
            var profile = new PlayerProfile { EsenciaCometa = 50 };
            var epicCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Epic); // Disenchant yield = 75

            profile.Inventory[epicCard.Id] = new CardInventoryEntry(epicCard.Id, isUnlocked: true, initialCopies: 3);

            bool success = profile.TryDisenchantCard(epicCard, copiesToDisenchant: 2, out string error);

            Assert.That(success, Is.True);
            Assert.That(profile.GetDuplicateCopies(epicCard.Id), Is.EqualTo(1));
            Assert.That(profile.EsenciaCometa, Is.EqualTo(50 + (2 * 75))); // 200
        }

        [Test]
        public void TryDisenchantCard_Fails_WhenInsufficientDuplicates()
        {
            var profile = new PlayerProfile { EsenciaCometa = 50 };
            var rareCard = CardCatalog.AllCards.First(c => c.Rarity == CardRarity.Rare);

            profile.Inventory[rareCard.Id] = new CardInventoryEntry(rareCard.Id, isUnlocked: true, initialCopies: 0);

            bool success = profile.TryDisenchantCard(rareCard, copiesToDisenchant: 1, out string error);

            Assert.That(success, Is.False);
            Assert.That(profile.EsenciaCometa, Is.EqualTo(50));
        }

        [Test]
        public void Config_SetCraftCostAndReset_WorksCorrectly()
        {
            CardCraftingConfig.SetCraftCost(CardRarity.Legendary, 999);
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Legendary), Is.EqualTo(999));

            CardCraftingConfig.ResetToDefault();
            Assert.That(CardCraftingConfig.GetCraftCost(CardRarity.Legendary), Is.EqualTo(800));
        }
    }
}
