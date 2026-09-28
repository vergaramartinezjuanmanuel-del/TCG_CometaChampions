#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;
using Newtonsoft.Json;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class PlayerLevelRewardPackTests
    {
        [TearDown]
        public void TearDown()
        {
            // Ensure config is always reset after each test
            PlayerLevelRewardConfig.ResetToDefault();
        }

        [Test]
        public void Config_DefaultRewardLevels_AreCorrect()
        {
            // Default configured levels: 2, 4, 6, 8, 10, 12, 15, 20
            var defaults = PlayerLevelRewardConfig.RewardLevels;
            Assert.That(defaults, Is.EqualTo(new List<int> { 2, 4, 6, 8, 10, 12, 15, 20 }));

            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(1), Is.False);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(2), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(3), Is.False);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(4), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(5), Is.False);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(6), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(8), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(10), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(12), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(15), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(20), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(21), Is.False);
        }

        [Test]
        public void Config_CustomRewardLevels_CanBeSetAndReset()
        {
            PlayerLevelRewardConfig.SetRewardLevels(new[] { 3, 7, 11 });

            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(2), Is.False);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(3), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(7), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(11), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(20), Is.False);

            PlayerLevelRewardConfig.ResetToDefault();
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(2), Is.True);
            Assert.That(PlayerLevelRewardConfig.IsRewardLevel(3), Is.False);
        }

        [Test]
        public void LevelProgression_GrantsRewardPack_AtConfiguredLevels()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            Assert.That(profile.AccountLevel, Is.EqualTo(1));
            Assert.That(profile.RewardPacks, Is.EqualTo(0));
            Assert.That(profile.ClaimedLevelRewardPacks.Count, Is.EqualTo(0));

            // Level 1 -> Level 2 (100 XP threshold)
            profile.AddAccountExperience(100);
            Assert.That(profile.AccountLevel, Is.EqualTo(2));
            Assert.That(profile.RewardPacks, Is.EqualTo(1), "Level 2 grants 1 reward pack");
            Assert.That(profile.HasClaimedLevelReward(2), Is.True);

            // Level 2 -> Level 3 (200 XP threshold) - Level 3 is NOT a reward level
            profile.AddAccountExperience(200);
            Assert.That(profile.AccountLevel, Is.EqualTo(3));
            Assert.That(profile.RewardPacks, Is.EqualTo(1), "Level 3 does not grant a reward pack");
            Assert.That(profile.HasClaimedLevelReward(3), Is.False);

            // Level 3 -> Level 4 (300 XP threshold) - Level 4 IS a reward level
            profile.AddAccountExperience(300);
            Assert.That(profile.AccountLevel, Is.EqualTo(4));
            Assert.That(profile.RewardPacks, Is.EqualTo(2), "Level 4 grants 1 reward pack");
            Assert.That(profile.HasClaimedLevelReward(4), Is.True);
        }

        [Test]
        public void LevelReward_EachLevelReward_GrantedOnlyOnce()
        {
            var profile = PlayerProfile.CreateDefaultProfile();

            // Reach Level 2
            profile.AddAccountExperience(100);
            Assert.That(profile.AccountLevel, Is.EqualTo(2));
            Assert.That(profile.RewardPacks, Is.EqualTo(1));
            Assert.That(profile.ClaimedLevelRewardPacks.Count(l => l == 2), Is.EqualTo(1));

            // Add XP without reaching level 3
            profile.AddAccountExperience(50);
            Assert.That(profile.AccountLevel, Is.EqualTo(2));
            Assert.That(profile.RewardPacks, Is.EqualTo(1), "No extra pack while remaining at level 2");

            // Even if somehow ClaimedLevelReward contains 2, it shouldn't be added again
            Assert.That(profile.ClaimedLevelRewardPacks.Count(l => l == 2), Is.EqualTo(1));
        }

        [Test]
        public void RewardPack_ContainsExactlyThreeCards()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var packResult = CardPackGenerator.GenerateRewardPack(profile);

            Assert.That(packResult.PackType, Is.EqualTo(CardPackType.LevelReward));
            Assert.That(packResult.Cards.Count, Is.EqualTo(3));
            Assert.That(packResult.PackTitle, Does.Contain("RECOMPENSA DE NIVEL"));
        }

        [Test]
        public void RewardPack_HasNoNewCardGuarantee_AndCanContainOnlyDuplicates()
        {
            // Give player all cards in the pool except 1
            var profile = new PlayerProfile();
            var allCards = CardCatalog.AllCards;
            var testPool = allCards.Take(10).ToList();

            // Unlock 9 of the 10 cards, leaving 1 unowned
            for (int i = 0; i < 9; i++)
            {
                profile.Inventory[testPool[i].Id] = new CardInventoryEntry(testPool[i].Id, isUnlocked: true, initialCopies: 1);
            }

            var unownedCard = testPool[9];
            Assert.That(profile.IsCardUnlocked(unownedCard.Id), Is.False);

            // A standard pack (GeneratePack) would GUARANTEE that unownedCard is picked
            var standardResult = CardPackGenerator.GeneratePack(profile, customPool: testPool);
            Assert.That(standardResult.Cards.Any(c => c.Card.Id == unownedCard.Id && c.IsNew), Is.True,
                "Standard pack must guarantee the unowned card");

            // A reward pack (GenerateRewardPack) has NO new card guarantee.
            // Using a fixed RNG seed that doesn't hit index 9, we can verify it can produce purely duplicates.
            bool foundPureDuplicatePack = false;
            for (int seed = 0; seed < 100; seed++)
            {
                var copyProfile = new PlayerProfile();
                for (int i = 0; i < 9; i++)
                {
                    copyProfile.Inventory[testPool[i].Id] = new CardInventoryEntry(testPool[i].Id, isUnlocked: true, initialCopies: 1);
                }

                var rewardResult = CardPackGenerator.GenerateRewardPack(copyProfile, customPool: testPool, rng: new Random(seed));
                if (rewardResult.Cards.All(c => !c.IsNew))
                {
                    foundPureDuplicatePack = true;
                    Assert.That(rewardResult.NewCardsCount, Is.EqualTo(0));
                    Assert.That(rewardResult.DuplicateCardsCount, Is.EqualTo(3));
                    break;
                }
            }

            Assert.That(foundPureDuplicatePack, Is.True,
                "Reward pack should be able to produce 3 duplicate cards even when an unowned card exists in pool");
        }

        [Test]
        public void RewardPack_DuplicatesContributeToFoilProgression()
        {
            var profile = new PlayerProfile();
            var testCard = CardCatalog.AllCards.First();

            // Unlocked with 1 copy
            profile.Inventory[testCard.Id] = new CardInventoryEntry(testCard.Id, isUnlocked: true, initialCopies: 1);
            int initialCopies = profile.GetDuplicateCopies(testCard.Id);

            // Generate reward pack with only this card in custom pool
            var rewardPack = CardPackGenerator.GenerateRewardPack(profile, customPool: new List<CardDefinition> { testCard });

            Assert.That(rewardPack.Cards.Count, Is.EqualTo(3));
            Assert.That(rewardPack.Cards.All(c => c.Card.Id == testCard.Id), Is.True);
            Assert.That(rewardPack.Cards.All(c => !c.IsNew), Is.True);

            // Copies should have increased by 3
            Assert.That(profile.GetDuplicateCopies(testCard.Id), Is.EqualTo(initialCopies + 3));
        }

        [Test]
        public void RewardPack_AdheresToLimitedRarityDistribution()
        {
            // Slot 0: Common 85%, Rare 15% (Never Epic, Never Legendary)
            // Slot 1: Common 70%, Rare 25%, Epic 5% (Never Legendary)
            // Slot 2: Rare 80%, Epic 18%, Legendary 2% (Never Common)
            var rng = new Random(999);

            for (int i = 0; i < 500; i++)
            {
                var r0 = CardPackGenerator.RollRewardPackSlotRarity(0, rng);
                Assert.That(r0 == CardRarity.Common || r0 == CardRarity.Rare, Is.True,
                    $"Slot 0 rolled illegal rarity: {r0}");

                var r1 = CardPackGenerator.RollRewardPackSlotRarity(1, rng);
                Assert.That(r1 == CardRarity.Common || r1 == CardRarity.Rare || r1 == CardRarity.Epic, Is.True,
                    $"Slot 1 rolled illegal rarity: {r1}");

                var r2 = CardPackGenerator.RollRewardPackSlotRarity(2, rng);
                Assert.That(r2 == CardRarity.Rare || r2 == CardRarity.Epic || r2 == CardRarity.Legendary, Is.True,
                    $"Slot 2 rolled illegal rarity: {r2}");
            }
        }

        [Test]
        public void PlayerProfile_TryOpenRewardPack_DecrementsRewardPacks_AndGeneratesPack()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.AccountLevel = PlayerProfile.MaxAccountLevel; // Isolate from level-up pack grants
            profile.RewardPacks = 2;

            bool success = profile.TryOpenRewardPack(out var result, out string error);

            Assert.That(success, Is.True);
            Assert.That(string.IsNullOrEmpty(error), Is.True);
            Assert.That(profile.RewardPacks, Is.EqualTo(1));
            Assert.That(result.PackType, Is.EqualTo(CardPackType.LevelReward));
            Assert.That(result.Cards.Count, Is.EqualTo(3));
        }

        [Test]
        public void PlayerProfile_TryOpenRewardPack_Fails_WhenZeroRewardPacks()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.RewardPacks = 0;

            bool success = profile.TryOpenRewardPack(out var result, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("No tienes"));
            Assert.That(profile.RewardPacks, Is.EqualTo(0));
            Assert.That(result.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void PlayerProfile_RewardPacksAndClaimedLevels_PersistAcrossJsonSerialization()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.RewardPacks = 5;
            profile.ClaimedLevelRewardPacks = new List<int> { 2, 4, 6 };

            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            var restored = JsonConvert.DeserializeObject<PlayerProfile>(json)!;

            Assert.That(restored.RewardPacks, Is.EqualTo(5));
            Assert.That(restored.ClaimedLevelRewardPacks, Is.EquivalentTo(new List<int> { 2, 4, 6 }));
            Assert.That(restored.HasClaimedLevelReward(2), Is.True);
            Assert.That(restored.HasClaimedLevelReward(4), Is.True);
            Assert.That(restored.HasClaimedLevelReward(6), Is.True);
            Assert.That(restored.HasClaimedLevelReward(8), Is.False);
        }
    }
}
