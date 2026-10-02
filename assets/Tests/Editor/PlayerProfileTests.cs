using System;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using CometaChampions.Core.Models;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class PlayerProfileTests
    {
        [Test]
        public void DefaultProfile_InitializesWithCorrectStartingValues()
        {
            var profile = PlayerProfile.CreateDefaultProfile();

            Assert.AreEqual(0, profile.GoldCoins);
            Assert.AreEqual(PlayerProfile.DefaultInitialGold, profile.GoldCoins);
            Assert.AreEqual(0, profile.EsenciaCometa);
            Assert.AreEqual(PlayerProfile.DefaultInitialDust, profile.EsenciaCometa);
            Assert.AreEqual(1, profile.BoosterPacks);
            Assert.AreEqual(PlayerProfile.DefaultInitialPacks, profile.BoosterPacks);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);
            StringAssert.IsMatch(@"^Guest\d{3}$", profile.PlayerName, "Default player name must be Guest + 3-digit number (e.g. Guest123)");

            // Verify exactly 4 starter cards unlocked (1 per category and 1 per clan), each with 1 copy
            int unlockedCount = profile.Inventory.Values.Count(e => e.IsUnlocked);
            Assert.AreEqual(4, unlockedCount, "Default profile must start with exactly 4 unlocked cards.");
            Assert.IsTrue(profile.IsCardUnlocked("CHAMP_DRAKY"));
            Assert.IsTrue(profile.IsCardUnlocked("BEAST_COMETIN"));
            Assert.IsTrue(profile.IsCardUnlocked("TROMPO_AVISPON"));
            Assert.IsTrue(profile.IsCardUnlocked("ARENA_SANTUARIO_SOL"));
            Assert.AreEqual(1, profile.GetDuplicateCopies("CHAMP_DRAKY"));
            Assert.AreEqual(1, profile.GetFoilLevel("CHAMP_DRAKY"));
        }

        [Test]
        public void GenerateDefaultPlayerName_ProducesGuestWithThreeDigits()
        {
            for (int i = 0; i < 20; i++)
            {
                string name = PlayerProfile.GenerateDefaultPlayerName();
                StringAssert.IsMatch(@"^Guest\d{3}$", name);
            }
        }

        [Test]
        public void TrySetPlayerName_Succeeds_WhenValid()
        {
            var profile = new PlayerProfile();
            bool success = profile.TrySetPlayerName("TrompoMaster", out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual("TrompoMaster", profile.PlayerName);
        }

        [Test]
        [TestCase("", ExpectedResult = false)]
        [TestCase("   ", ExpectedResult = false)]
        [TestCase("ab", ExpectedResult = false)]
        [TestCase("NameThatIsWayTooLongForAProfile12345", ExpectedResult = false)]
        public bool TrySetPlayerName_Fails_WhenInvalid(string candidateName)
        {
            var profile = new PlayerProfile();
            return profile.TrySetPlayerName(candidateName, out _);
        }

        [Test]
        public void AddCard_UnlocksAndAccumulatesDuplicates()
        {
            var profile = new PlayerProfile();
            const string cardId = "TEST_CARD_01";

            Assert.IsFalse(profile.IsCardUnlocked(cardId));
            Assert.AreEqual(0, profile.GetDuplicateCopies(cardId));

            // First copy unlocks the card and counts as 1 copy obtained
            profile.AddCard(cardId, 1);
            Assert.IsTrue(profile.IsCardUnlocked(cardId));
            Assert.AreEqual(1, profile.GetDuplicateCopies(cardId));

            // Subsequent copies increment duplicate count
            profile.AddCard(cardId, 3);
            Assert.AreEqual(4, profile.GetDuplicateCopies(cardId));
        }

        [Test]
        public void TryUpgradeFoil_Succeeds_WhenRequirementsMet()
        {
            var profile = new PlayerProfile
            {
                GoldCoins = 200
            };
            const string cardId = "CHAMP_ESPECTRO";
            var entry = profile.GetOrCreateEntry(cardId);
            entry.IsUnlocked = true;
            entry.DuplicateCopies = 5;
            entry.FoilLevel = 1;

            bool success = profile.TryUpgradeFoil(cardId, out var failureReason);

            Assert.IsTrue(success, $"Upgrade should succeed, but failed: {failureReason}");
            Assert.AreEqual(2, entry.FoilLevel);
            Assert.AreEqual(0, entry.DuplicateCopies, "5 duplicate copies should have been consumed.");
            // Upgrading to Foil 2 costs 120 Gold, but awards 100 XP which levels up from Lvl 1 -> Lvl 2,
            // awarding 2 * 50 = +100 Gold Coins.
            int expectedGold = 200 - PlayerProfile.GoldCostPerFoilUpgrade + (2 * 50);
            Assert.AreEqual(expectedGold, profile.GoldCoins);
        }

        [Test]
        public void TryUpgradeFoil_Fails_WhenInsufficientDuplicates()
        {
            var profile = new PlayerProfile
            {
                GoldCoins = 500
            };
            const string cardId = "CHAMP_ESPECTRO";
            var entry = profile.GetOrCreateEntry(cardId);
            entry.IsUnlocked = true;
            entry.DuplicateCopies = 4; // Needs 5
            entry.FoilLevel = 1;

            bool success = profile.TryUpgradeFoil(cardId, out var failureReason);

            Assert.IsFalse(success);
            Assert.That(failureReason, Does.Contain("duplicate copies"));
            Assert.AreEqual(1, entry.FoilLevel);
            Assert.AreEqual(4, entry.DuplicateCopies);
            Assert.AreEqual(500, profile.GoldCoins, "Coins should not be deducted upon failure.");
        }

        [Test]
        public void TryUpgradeFoil_Fails_WhenInsufficientGold()
        {
            var profile = new PlayerProfile
            {
                GoldCoins = 50 // Needs 120
            };
            const string cardId = "CHAMP_ESPECTRO";
            var entry = profile.GetOrCreateEntry(cardId);
            entry.IsUnlocked = true;
            entry.DuplicateCopies = 6;
            entry.FoilLevel = 1;

            bool success = profile.TryUpgradeFoil(cardId, out var failureReason);

            Assert.IsFalse(success);
            Assert.That(failureReason, Does.Contain("Gold Coins"));
            Assert.AreEqual(1, entry.FoilLevel);
            Assert.AreEqual(6, entry.DuplicateCopies, "Copies should not be deducted upon failure.");
            Assert.AreEqual(50, profile.GoldCoins);
        }

        [Test]
        public void TryUpgradeFoil_Fails_WhenAtMaxFoilLevel()
        {
            var profile = new PlayerProfile
            {
                GoldCoins = 1000
            };
            const string cardId = "CHAMP_ESPECTRO";
            var entry = profile.GetOrCreateEntry(cardId);
            entry.IsUnlocked = true;
            entry.DuplicateCopies = 10;
            entry.FoilLevel = CardInventoryEntry.MaxFoilLevel; // 3

            bool success = profile.TryUpgradeFoil(cardId, out var failureReason);

            Assert.IsFalse(success);
            Assert.That(failureReason, Does.Contain("maximum Gold Foil tier"));
        }

        [Test]
        public void EsenciaCometa_CraftingAndDisenchanting_ModifiesDustAndInventory()
        {
            var profile = new PlayerProfile
            {
                EsenciaCometa = 300
            };
            const string cardId = "CHAMP_DRAKY";

            // Crafting unlocks the card with 1 copy
            bool craftSuccess = profile.TryCraftWithDust(cardId, dustCost: 100, out var craftError);
            Assert.IsTrue(craftSuccess, craftError);
            Assert.AreEqual(200, profile.EsenciaCometa);
            Assert.IsTrue(profile.IsCardUnlocked(cardId));
            Assert.AreEqual(1, profile.GetDuplicateCopies(cardId));

            // Add 2 more copies (total: 3)
            profile.AddCard(cardId, 2);
            Assert.AreEqual(3, profile.GetDuplicateCopies(cardId));

            // Disenchant 1 copy for 40 dust (total remaining: 2)
            bool disenchantSuccess = profile.TryDisenchant(cardId, copiesToDisenchant: 1, dustYieldPerCopy: 40, out var disError);
            Assert.IsTrue(disenchantSuccess, disError);
            Assert.AreEqual(2, profile.GetDuplicateCopies(cardId));
            Assert.AreEqual(240, profile.EsenciaCometa);
        }

        [Test]
        public void ProcessDailyLogin_TracksConsecutiveStreaksAndResets()
        {
            var profile = new PlayerProfile();
            var day1 = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 9, 2, 11, 0, 0, DateTimeKind.Utc);
            var day4 = new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc); // Skipped day 3

            // First login
            bool canClaim1 = profile.ProcessDailyLogin(day1);
            Assert.IsTrue(canClaim1);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);

            // Same day repeat login
            bool canClaimSameDay = profile.ProcessDailyLogin(day1.AddHours(2));
            Assert.IsFalse(canClaimSameDay, "Cannot claim multiple rewards on the same calendar day.");
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);

            // Consecutive day login
            bool canClaim2 = profile.ProcessDailyLogin(day2);
            Assert.IsTrue(canClaim2);
            Assert.AreEqual(2, profile.ConsecutiveLoginDays);

            // Missed day -> reset to Day 1
            bool canClaim4 = profile.ProcessDailyLogin(day4);
            Assert.IsTrue(canClaim4);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays, "Streak should reset to 1 after missing a day.");
        }

        [Test]
        public void JsonSerialization_RoundtripsAccurately()
        {
            var original = PlayerProfile.CreateDefaultProfile();
            original.PlayerName = "TrompoMaster_99";
            original.GoldCoins = 4820;
            original.EsenciaCometa = 630;
            original.BoosterPacks = 7;
            original.ConsecutiveLoginDays = 4;

            // Upgrade a card to foil 2
            var espectro = original.GetOrCreateEntry("CHAMP_ESPECTRO");
            espectro.IsUnlocked = true;
            espectro.FoilLevel = 2;
            espectro.DuplicateCopies = 3;

            string json = JsonConvert.SerializeObject(original, Formatting.Indented);
            Assert.IsNotNull(json);

            var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(original.PlayerId, deserialized.PlayerId);
            Assert.AreEqual("TrompoMaster_99", deserialized.PlayerName);
            Assert.AreEqual(4820, deserialized.GoldCoins);
            Assert.AreEqual(630, deserialized.EsenciaCometa);
            Assert.AreEqual(7, deserialized.BoosterPacks);
            Assert.AreEqual(4, deserialized.ConsecutiveLoginDays);

            Assert.IsTrue(deserialized.IsCardUnlocked("CHAMP_ESPECTRO"));
            Assert.AreEqual(2, deserialized.GetFoilLevel("CHAMP_ESPECTRO"));
            Assert.AreEqual(3, deserialized.GetDuplicateCopies("CHAMP_ESPECTRO"));
        }

        [Test]
        public void AccountLevel_InitializesAtLevel1_WithZeroXp()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            Assert.AreEqual(1, profile.AccountLevel);
            Assert.AreEqual(0, profile.CurrentLevelXp);
            Assert.IsFalse(profile.IsMaxAccountLevel);
            Assert.AreEqual(100, PlayerProfile.GetXpRequiredForNextLevel(1));
        }

        [Test]
        public void AddAccountExperience_IncrementsXpWithoutLevelUp_WhenUnderThreshold()
        {
            var profile = new PlayerProfile { AccountLevel = 1, CurrentLevelXp = 0 };

            bool leveledUp = profile.AddAccountExperience(60, out int levelsGained, out int goldReward, out int packReward);

            Assert.IsFalse(leveledUp);
            Assert.AreEqual(0, levelsGained);
            Assert.AreEqual(0, goldReward);
            Assert.AreEqual(0, packReward);
            Assert.AreEqual(1, profile.AccountLevel);
            Assert.AreEqual(60, profile.CurrentLevelXp);
        }

        [Test]
        public void AddAccountExperience_LevelsUpCorrectly_AndGrantsRewards()
        {
            var profile = new PlayerProfile
            {
                AccountLevel = 1,
                CurrentLevelXp = 0,
                GoldCoins = 1000,
                BoosterPacks = 0
            };

            // 120 XP: 100 consumed to reach Level 2, 20 XP remains.
            // Level 2 grants: 2 * 50 = 100 Gold Coins.
            bool leveledUp = profile.AddAccountExperience(120, out int levelsGained, out int goldReward, out int packReward);

            Assert.IsTrue(leveledUp);
            Assert.AreEqual(1, levelsGained);
            Assert.AreEqual(2, profile.AccountLevel);
            Assert.AreEqual(20, profile.CurrentLevelXp);
            Assert.AreEqual(100, goldReward);
            Assert.AreEqual(1100, profile.GoldCoins);
            Assert.AreEqual(1, packReward);
            Assert.AreEqual(1, profile.RewardPacks);
            Assert.AreEqual(0, profile.BoosterPacks);
        }

        [Test]
        public void AddAccountExperience_MultiLevelUp_AndMilestonePackRewards()
        {
            var profile = new PlayerProfile
            {
                AccountLevel = 1,
                CurrentLevelXp = 0,
                GoldCoins = 0,
                BoosterPacks = 0
            };

            // XP needed:
            // 1->2: 100
            // 2->3: 150
            // 3->4: 200
            // 4->5: 250
            // Total to reach Level 5: 700 XP.
            // 750 XP reaches Level 5 with 50 XP leftover.
            // Gold: 2*50 + 3*50 + 4*50 + 5*50 = 100 + 150 + 200 + 250 = 700 Gold.
            // Packs: Levels 2 and 4 are configured reward levels -> +2 reward packs.
            bool leveledUp = profile.AddAccountExperience(750, out int levelsGained, out int goldReward, out int packReward);

            Assert.IsTrue(leveledUp);
            Assert.AreEqual(4, levelsGained);
            Assert.AreEqual(5, profile.AccountLevel);
            Assert.AreEqual(50, profile.CurrentLevelXp);
            Assert.AreEqual(700, goldReward);
            Assert.AreEqual(700, profile.GoldCoins);
            Assert.AreEqual(2, packReward);
            Assert.AreEqual(2, profile.RewardPacks);
            Assert.AreEqual(0, profile.BoosterPacks);
        }

        [Test]
        public void AddAccountExperience_StrictlyCappedAtLevel20()
        {
            var profile = new PlayerProfile
            {
                AccountLevel = 1,
                CurrentLevelXp = 0
            };

            // Grant massive XP to hit the cap
            profile.AddAccountExperience(100000);

            Assert.AreEqual(PlayerProfile.MaxAccountLevel, profile.AccountLevel);
            Assert.AreEqual(20, profile.AccountLevel);
            Assert.IsTrue(profile.IsMaxAccountLevel);
            Assert.AreEqual(0, profile.CurrentLevelXp, "XP must be capped at 0 once max level 20 is reached.");
            Assert.AreEqual(0, PlayerProfile.GetXpRequiredForNextLevel(20));

            // Attempting to add more XP at level 20 must be a no-op
            bool extraResult = profile.AddAccountExperience(500, out int levelsGained, out _, out _);
            Assert.IsFalse(extraResult);
            Assert.AreEqual(0, levelsGained);
            Assert.AreEqual(20, profile.AccountLevel);
            Assert.AreEqual(0, profile.CurrentLevelXp);
        }

        [Test]
        public void TryUpgradeFoil_AwardsAccountProgressionXp()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.GoldCoins = 200;
            var entry = profile.GetOrCreateEntry("CHAMP_ESPECTRO");
            entry.IsUnlocked = true;
            entry.DuplicateCopies = 5;

            Assert.AreEqual(1, profile.AccountLevel);
            Assert.AreEqual(0, profile.CurrentLevelXp);

            bool success = profile.TryUpgradeFoil("CHAMP_ESPECTRO", out var error);
            Assert.IsTrue(success, error);
            Assert.AreEqual(2, profile.GetFoilLevel("CHAMP_ESPECTRO"));

            // Foil level 2 awards 100 XP, which is exactly enough to level up from Level 1 -> Level 2
            Assert.AreEqual(2, profile.AccountLevel);
            Assert.AreEqual(0, profile.CurrentLevelXp);
        }

        [Test]
        public void AccountLevel_PersistsAcrossJsonSerialization()
        {
            var original = new PlayerProfile
            {
                AccountLevel = 14,
                CurrentLevelXp = 320,
                GoldCoins = 5000
            };

            string json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(14, deserialized.AccountLevel);
            Assert.AreEqual(320, deserialized.CurrentLevelXp);
            Assert.AreEqual(5000, deserialized.GoldCoins);
            Assert.IsFalse(deserialized.IsMaxAccountLevel);
        }

        [Test]
        public void AddGoldCoins_IncrementsBalanceCorrectly()
        {
            var profile = new PlayerProfile { GoldCoins = 500 };

            int balance1 = profile.AddGoldCoins(250);
            Assert.AreEqual(750, balance1);
            Assert.AreEqual(750, profile.GoldCoins);

            // Adding zero or negative amount should be ignored
            int balance2 = profile.AddGoldCoins(0);
            Assert.AreEqual(750, balance2);

            int balance3 = profile.AddGoldCoins(-100);
            Assert.AreEqual(750, balance3);
            Assert.AreEqual(750, profile.GoldCoins);
        }

        [Test]
        public void CoinPacks_CreditExactAmounts_AndPersist()
        {
            var profile = new PlayerProfile { GoldCoins = 0 };

            // Pack 1: 1,000 coins
            profile.AddGoldCoins(PlayerProfile.CoinsPerPack1000);
            Assert.AreEqual(1000, profile.GoldCoins);

            // Pack 2: 5,000 coins
            profile.AddGoldCoins(PlayerProfile.CoinsPerPack5000);
            Assert.AreEqual(6000, profile.GoldCoins);

            // Pack 3: 10,000 coins
            profile.AddGoldCoins(PlayerProfile.CoinsPerPack10000);
            Assert.AreEqual(16000, profile.GoldCoins);

            // JSON persistence roundtrip
            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<PlayerProfile>(json);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(16000, loaded.GoldCoins);
        }

        [Test]
        public void AdReward_Credits100Coins_AndPersists()
        {
            var profile = new PlayerProfile { GoldCoins = 1256 };

            // Reward for watching video ad
            profile.AddGoldCoins(PlayerProfile.CoinsPerAdReward);
            Assert.AreEqual(1356, profile.GoldCoins);

            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<PlayerProfile>(json);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1356, loaded.GoldCoins);
        }

        [Test]
        public void UnityAdsConfig_InitializesWithValidDefaults_AndPersistsJson()
        {
            var config = new UnityAdsConfig();
            Assert.AreEqual("5738200", config.androidGameId);
            Assert.AreEqual("5738201", config.iosGameId);
            Assert.AreEqual("Rewarded_Android", config.androidAdUnitId);
            Assert.AreEqual("Rewarded_iOS", config.iosAdUnitId);
            Assert.IsTrue(config.testMode);
            Assert.IsTrue(config.enableEditorSimulation);
            Assert.AreEqual(100, config.rewardGoldCoins);

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            Assert.IsNotNull(json);

            var loaded = JsonConvert.DeserializeObject<UnityAdsConfig>(json);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(config.androidGameId, loaded.androidGameId);
            Assert.AreEqual(config.iosGameId, loaded.iosGameId);
            Assert.AreEqual(config.ActiveAdUnitId, loaded.ActiveAdUnitId);
            Assert.AreEqual(100, loaded.rewardGoldCoins);
        }

        [Test]
        public void UnityAdsConfig_ActiveIds_ReturnPlatformAppropriateValues()
        {
            var config = new UnityAdsConfig
            {
                androidGameId = "Android_123",
                iosGameId = "iOS_456",
                androidAdUnitId = "Rewarded_Custom_Android",
                iosAdUnitId = "Rewarded_Custom_iOS"
            };

            Assert.IsNotEmpty(config.ActiveGameId);
            Assert.IsNotEmpty(config.ActiveAdUnitId);
        }

        [Test]
        public void AvatarCatalog_Contains20Avatars_AcrossLevels1To20()
        {
            var all = AvatarCatalog.GetAll();
            Assert.AreEqual(20, all.Count, "Catalog must contain exactly 20 avatars mapping to champion art.");

            // Level 1 starter tier: 3 avatars (1 free, 2 paid)
            var level1Avatars = AvatarCatalog.GetAll();
            int lvl1Count = 0;
            int lvl1FreeCount = 0;
            foreach (var a in level1Avatars)
            {
                if (a.RequiredLevel == 1)
                {
                    lvl1Count++;
                    if (a.IsFree) lvl1FreeCount++;
                }
            }
            Assert.AreEqual(3, lvl1Count, "Level 1 must have 3 avatars.");
            Assert.AreEqual(1, lvl1FreeCount, "Level 1 must have 1 free avatar (Cometín).");

            var maxLvlAvatar = AvatarCatalog.GetAvatarById("AVATAR_TURBO_FLASH");
            Assert.IsNotNull(maxLvlAvatar);
            Assert.AreEqual(20, maxLvlAvatar.RequiredLevel);
            Assert.IsTrue(maxLvlAvatar.IsFree, "Turbo Flash is master reward at level 20.");
        }

        [Test]
        public void DefaultProfile_InitializesWithStarterAvatars()
        {
            var profile = PlayerProfile.CreateDefaultProfile();

            Assert.AreEqual("AVATAR_COMETIN", profile.SelectedAvatarId);
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_COMETIN"));
            Assert.AreEqual(1, profile.UnlockedAvatarIds.Count, "New profile starts with only 1 unlocked avatar.");

            // Paid or higher level avatars are not unlocked at start
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_ESPECTRO"));
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_AVISPON"));
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_AZTECA"));
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_DRAKY"));
        }

        [Test]
        public void TryPurchaseAvatar_Succeeds_WhenLevelAndGoldMet()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.GoldCoins = 500;
            int startingGold = profile.GoldCoins;

            bool success = profile.TryPurchaseAvatar("AVATAR_AZTECA", out var failureReason);

            Assert.IsTrue(success, failureReason);
            Assert.AreEqual(startingGold - 100, profile.GoldCoins);
            Assert.AreEqual("AVATAR_AZTECA", profile.SelectedAvatarId);
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_AZTECA"));
        }

        [Test]
        public void TryPurchaseAvatar_Fails_WhenInsufficientGold()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.GoldCoins = 50; // Azteca costs 100

            bool success = profile.TryPurchaseAvatar("AVATAR_AZTECA", out var failureReason);

            Assert.IsFalse(success);
            StringAssert.Contains("insuficientes", failureReason);
            Assert.AreEqual(50, profile.GoldCoins);
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_AZTECA"));
        }

        [Test]
        public void TryPurchaseAvatar_Fails_WhenLevelTooLow()
        {
            var profile = PlayerProfile.CreateDefaultProfile(); // Level 1
            int startingGold = profile.GoldCoins;

            // Silver requires Level 2
            bool success = profile.TryPurchaseAvatar("AVATAR_SILVER", out var failureReason);

            Assert.IsFalse(success);
            StringAssert.Contains("Requiere Nivel", failureReason);
            Assert.AreEqual(startingGold, profile.GoldCoins);
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_SILVER"));
        }

        [Test]
        public void LevelUp_AutoUnlocksFreeMilestoneAvatars()
        {
            var profile = PlayerProfile.CreateDefaultProfile(); // Level 1
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_DRAKY"), "Draky is locked at Level 1.");
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_ESPECTRO"), "Espectro is locked at Level 1.");
            Assert.IsFalse(profile.IsAvatarUnlocked("AVATAR_AVISPON"), "Avispón is locked at Level 1.");

            // Add XP to level up to Level 2 (100 XP required)
            profile.AddAccountExperience(100);

            Assert.AreEqual(2, profile.AccountLevel);
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_DRAKY"), "Draky (free) should auto-unlock upon reaching Level 2.");
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_ESPECTRO"), "Espectro (free) should auto-unlock upon reaching Level 2.");
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_AVISPON"), "Avispón (free) should auto-unlock upon reaching Level 2.");
        }

        [Test]
        public void TryEquipAvatar_Succeeds_WhenUnlocked_FailsWhenLocked()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.AddAccountExperience(100); // Reach Level 2 to unlock Espectro

            // Equip unlocked Espectro
            bool equipEspectro = profile.TryEquipAvatar("AVATAR_ESPECTRO", out var reason1);
            Assert.IsTrue(equipEspectro, reason1);
            Assert.AreEqual("AVATAR_ESPECTRO", profile.SelectedAvatarId);

            // Try to equip locked Rex
            bool equipRex = profile.TryEquipAvatar("AVATAR_REX", out var reason2);
            Assert.IsFalse(equipRex);
            StringAssert.Contains("No tienes desbloqueado", reason2);
            Assert.AreEqual("AVATAR_ESPECTRO", profile.SelectedAvatarId);
        }

        [Test]
        public void AvatarState_PersistsThroughJsonSerialization()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.GoldCoins = 500;
            profile.TryPurchaseAvatar("AVATAR_AZTECA", out _);
            Assert.AreEqual("AVATAR_AZTECA", profile.SelectedAvatarId);

            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("AVATAR_AZTECA", deserialized.SelectedAvatarId);
            Assert.IsTrue(deserialized.IsAvatarUnlocked("AVATAR_AZTECA"));
            Assert.IsTrue(deserialized.IsAvatarUnlocked("AVATAR_COMETIN"));
        }

        [Test]
        public void DailyRewardCatalog_Contains7Days_WithValidSchedule()
        {
            var schedule = DailyRewardCatalog.GetAllDays();
            Assert.AreEqual(7, schedule.Count);

            var day1 = DailyRewardCatalog.GetPackForDay(1);
            Assert.AreEqual(150, day1.GoldCoins);
            Assert.AreEqual(1, day1.CardCount);
            Assert.AreEqual(25, day1.EsenciaCometa);
            Assert.AreEqual(0, day1.BoosterPacks);
            Assert.IsFalse(day1.HasAvatarReward);

            var day4 = DailyRewardCatalog.GetPackForDay(4);
            Assert.AreEqual(1, day4.BoosterPacks, "Day 4 awards a booster pack.");

            var day7 = DailyRewardCatalog.GetPackForDay(7);
            Assert.AreEqual(600, day7.GoldCoins);
            Assert.AreEqual(3, day7.CardCount);
            Assert.AreEqual(150, day7.EsenciaCometa);
            Assert.AreEqual(1, day7.BoosterPacks);
            Assert.IsTrue(day7.HasAvatarReward, "Day 7 mega pack includes an avatar reward.");
        }

        [Test]
        public void CanClaimDailyReward_EvaluatesCorrectly()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var day1 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);

            // Never claimed before -> can claim
            Assert.IsTrue(profile.CanClaimDailyReward(day1));

            // Claim Day 1
            profile.ClaimDailyReward(day1);

            // Same calendar day -> cannot claim again
            Assert.IsFalse(profile.CanClaimDailyReward(day1.AddHours(5)));

            // Next calendar day -> can claim
            var day2 = day1.AddDays(1);
            Assert.IsTrue(profile.CanClaimDailyReward(day2));
        }

        [Test]
        public void ClaimDailyReward_Day1_AwardsExpectedResources_AndSetsClaimDate()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            int startingGold = profile.GoldCoins;
            int startingDust = profile.EsenciaCometa;
            var claimTime = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);

            var result = profile.ClaimDailyReward(claimTime);

            Assert.AreEqual(1, result.DayIndex);
            Assert.AreEqual(150, result.GoldCoins);
            Assert.AreEqual(1, result.AwardedCardIds.Count);
            Assert.AreEqual(25, result.EsenciaCometa);
            Assert.AreEqual(startingGold + 150, profile.GoldCoins);
            Assert.AreEqual(startingDust + 25, profile.EsenciaCometa);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);
            Assert.IsNotNull(profile.LastDailyRewardClaimDateUtc);
        }

        [Test]
        public void ClaimDailyReward_ConsecutiveDays_AdvancesStreak()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var day1 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 9, 23, 11, 0, 0, DateTimeKind.Utc);

            profile.ClaimDailyReward(day1);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);

            var resultDay2 = profile.ClaimDailyReward(day2);
            Assert.AreEqual(2, resultDay2.DayIndex);
            Assert.AreEqual(200, resultDay2.GoldCoins);
            Assert.AreEqual(2, profile.ConsecutiveLoginDays);
        }

        [Test]
        public void ClaimDailyReward_SkippedDay_ResetsStreakToDay1()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var day1 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var day3 = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc); // Missed Sep 23

            profile.ClaimDailyReward(day1);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);

            var result = profile.ClaimDailyReward(day3);
            Assert.AreEqual(1, result.DayIndex, "Streak should reset to Day 1 after missing a day.");
            Assert.AreEqual(150, result.GoldCoins);
            Assert.AreEqual(1, profile.ConsecutiveLoginDays);
        }

        [Test]
        public void ClaimDailyReward_Day7_AwardsBoosterPackAndAvatar()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            profile.ConsecutiveLoginDays = 6;
            profile.LastDailyRewardClaimDateUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            int startingPacks = profile.BoosterPacks;

            var day7 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var result = profile.ClaimDailyReward(day7);

            Assert.AreEqual(7, result.DayIndex);
            Assert.AreEqual(7, profile.ConsecutiveLoginDays);
            Assert.AreEqual(600, result.GoldCoins);
            Assert.AreEqual(3, result.AwardedCardIds.Count);
            Assert.AreEqual(1, result.BoosterPacks);
            Assert.AreEqual(startingPacks + 1, profile.BoosterPacks);
            Assert.IsTrue(result.HasAvatar, "Day 7 should roll and award an unowned avatar.");
            Assert.IsTrue(profile.IsAvatarUnlocked(result.AwardedAvatarId));
        }

        [Test]
        public void ClaimDailyReward_Day7_ConvertsToGold_WhenAllAvatarsUnlocked()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            // Unlock all 20 avatars in the profile
            foreach (var a in AvatarCatalog.GetAll())
            {
                if (!profile.UnlockedAvatarIds.Contains(a.Id, StringComparer.OrdinalIgnoreCase))
                {
                    profile.UnlockedAvatarIds.Add(a.Id);
                }
            }

            profile.ConsecutiveLoginDays = 6;
            profile.LastDailyRewardClaimDateUtc = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            int startingGold = profile.GoldCoins;

            var day7 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var result = profile.ClaimDailyReward(day7);

            Assert.AreEqual(7, result.DayIndex);
            Assert.IsTrue(result.AvatarConvertedToGold);
            Assert.AreEqual(600 + DailyRewardCatalog.BonusGoldForOwnedAvatar, result.GoldCoins);
            Assert.IsTrue(profile.GoldCoins >= startingGold + result.GoldCoins);
        }

        [Test]
        public void DailyReward_PersistsAcrossJsonSerialization()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var claimTime = new DateTime(2026, 9, 22, 15, 30, 0, DateTimeKind.Utc);
            profile.ClaimDailyReward(claimTime);

            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.ConsecutiveLoginDays);
            Assert.IsNotNull(deserialized.LastDailyRewardClaimDateUtc);
            Assert.AreEqual(claimTime.Date, deserialized.LastDailyRewardClaimDateUtc.Value.Date);
            Assert.IsFalse(deserialized.CanClaimDailyReward(claimTime.AddHours(2)));
        }

        [Test]
        public void GetCurrentDailyRewardDayIndex_EvaluatesAccurately_AcrossStreakProgression()
        {
            var profile = PlayerProfile.CreateDefaultProfile();
            var day1 = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
            var day2 = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
            var day3 = new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);
            var day5 = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

            // 1. Fresh profile before claim: Day 1
            Assert.AreEqual(1, profile.GetCurrentDailyRewardDayIndex(day1));
            Assert.IsTrue(profile.CanClaimDailyReward(day1));

            // 2. Claim Day 1: same day evaluates to Day 1
            profile.ClaimDailyReward(day1);
            Assert.IsFalse(profile.CanClaimDailyReward(day1));
            Assert.AreEqual(1, profile.GetCurrentDailyRewardDayIndex(day1));

            // 3. Day 2 before claim: evaluates to Day 2
            Assert.IsTrue(profile.CanClaimDailyReward(day2));
            Assert.AreEqual(2, profile.GetCurrentDailyRewardDayIndex(day2));

            // 4. Claim Day 2: same day evaluates to Day 2
            profile.ClaimDailyReward(day2);
            Assert.IsFalse(profile.CanClaimDailyReward(day2));
            Assert.AreEqual(2, profile.GetCurrentDailyRewardDayIndex(day2));

            // 5. Day 3 before claim: evaluates to Day 3
            Assert.IsTrue(profile.CanClaimDailyReward(day3));
            Assert.AreEqual(3, profile.GetCurrentDailyRewardDayIndex(day3));
            profile.ClaimDailyReward(day3);

            // 6. Skipped Day 4, arrived at Day 5: streak broken, evaluates to Day 1
            Assert.IsTrue(profile.CanClaimDailyReward(day5));
            Assert.AreEqual(1, profile.GetCurrentDailyRewardDayIndex(day5));
        }

        [Test]
        public void FirstTimeOnboarding_ReachesFirstCombatThreshold()
        {
            // Step 0: Create default profile
            var profile = PlayerProfile.CreateDefaultProfile();
            Assert.AreEqual(4, profile.Inventory.Values.Count(e => e.IsUnlocked), "Must start with 4 unlocked cards.");
            Assert.AreEqual(0, profile.GoldCoins, "Must start with 0 gold.");
            Assert.AreEqual(0, profile.EsenciaCometa, "Must start with 0 esencia.");
            Assert.AreEqual(1, profile.BoosterPacks, "Must start with 1 free booster pack.");
            Assert.AreEqual(0, profile.RewardPacks, "Must start with 0 reward packs.");
            Assert.AreEqual(1, profile.AccountLevel, "Must start at Account Level 1.");
            Assert.AreEqual(1, profile.UnlockedAvatarIds.Count, "Must start with 1 unlocked avatar (Cometín).");
            Assert.IsTrue(profile.IsAvatarUnlocked("AVATAR_COMETIN"));

            // Step 1: Open the free welcome booster pack (5 cards)
            bool openedWelcomePack = profile.TryOpenBoosterPack(out var welcomePackResult, out var packError);
            Assert.IsTrue(openedWelcomePack, packError);
            Assert.AreEqual(0, profile.BoosterPacks);
            Assert.AreEqual(5, welcomePackResult.Cards.Count);
            Assert.GreaterOrEqual(profile.Inventory.Values.Count(e => e.IsUnlocked), 8, "Should have at least 8-9 unlocked cards after opening welcome pack.");
            Assert.GreaterOrEqual(profile.AccountLevel, 2, "Opening welcome pack should grant enough XP to reach at least Level 2.");
            Assert.GreaterOrEqual(profile.RewardPacks, 1, "Reaching Level 2 must grant a Level Reward Pack.");
            Assert.AreEqual(4, profile.UnlockedAvatarIds.Count, "Reaching Level 2 must unlock Draky, Espectro, and Avispón (4 total avatars).");

            // Step 2: Open the Level 2 Reward Pack (3 cards)
            bool openedRewardPack = profile.TryOpenRewardPack(out var rewardPackResult, out var rewardError);
            Assert.IsTrue(openedRewardPack, rewardError);
            Assert.AreEqual(3, rewardPackResult.Cards.Count);

            // Step 3: Claim Day 1 Daily Reward Pack
            var claimTime = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
            var dailyResult = profile.ClaimDailyReward(claimTime);
            Assert.AreEqual(1, dailyResult.DayIndex);
            Assert.GreaterOrEqual(profile.GoldCoins, 250, "Player should have accumulated gold from level-ups and daily reward.");
            Assert.GreaterOrEqual(profile.EsenciaCometa, 25, "Player should have accumulated at least 25 esencia from daily reward.");

            // Verify player can now build a full, legal 12-card Arena Combat deck from unlocked inventory
            int totalUnlockedUnique = profile.Inventory.Values.Count(e => e.IsUnlocked);
            Assert.GreaterOrEqual(totalUnlockedUnique, 6, "At least 6 unique unlocked cards are required to form a 12-card deck (max 2 copies each).");

            var combatDeck = CometaChampions.Core.Rules.DeckValidator.CreateDeckFromInventory(profile, new Random(42));
            Assert.AreEqual(CometaChampions.Core.Rules.DeckValidator.ArenaRequiredDeckSize, combatDeck.Count, "Player must be able to assemble a full 12-card combat deck.");
            Assert.IsTrue(CometaChampions.Core.Rules.DeckValidator.ValidateDeckOwnership(combatDeck, profile, out var deckErrors), string.Join("; ", deckErrors));
        }
    }
}
