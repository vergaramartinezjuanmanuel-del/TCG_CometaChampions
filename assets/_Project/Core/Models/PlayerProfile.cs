#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Authoritative player profile and progression state.
    /// Serialized directly to local JSON disk storage and portable to multiplayer cloud backends.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int DefaultInitialGold = 1256;
        public const int DefaultInitialDust = 150;
        public const int DefaultInitialPacks = 3;
        public const int RequiredCopiesForFoilUpgrade = 5;
        public const int GoldCostPerFoilUpgrade = 120;

        public const int MaxAccountLevel = 20;
        public const int BaseXpPerLevel = 100;
        public const int XpIncrementPerLevel = 50;

        // Card Pack Acquisition Constants (Rules 1 & 4)
        public const int PackGoldCost = 500;
        public const int CardsPerPack = 5;

        // Monetization & IAP Constants
        public const string IapPack1000Id = "com.cometachampions.gold.1000";
        public const string IapPack5000Id = "com.cometachampions.gold.5000";
        public const string IapPack10000Id = "com.cometachampions.gold.10000";

        public const int CoinsPerPack1000 = 1000;
        public const int CoinsPerPack5000 = 5000;
        public const int CoinsPerPack10000 = 10000;
        public const int CoinsPerAdReward = 100;

        private static readonly Random Rng = new();

        public static string GenerateDefaultPlayerName()
        {
            lock (Rng)
            {
                return $"Guest{Rng.Next(100, 1000)}";
            }
        }

        public static int GetXpRequiredForNextLevel(int level)
        {
            if (level >= MaxAccountLevel) return 0;
            return BaseXpPerLevel + Math.Max(0, level - 1) * XpIncrementPerLevel;
        }

        public string PlayerId { get; set; } = Guid.NewGuid().ToString("N");
        public string PlayerName { get; set; } = GenerateDefaultPlayerName();
        public int AccountLevel { get; set; } = 1;
        public int CurrentLevelXp { get; set; } = 0;
        public int GoldCoins { get; set; } = DefaultInitialGold;
        public int EsenciaCometa { get; set; } = DefaultInitialDust;
        public int BoosterPacks { get; set; } = DefaultInitialPacks;
        public int RewardPacks { get; set; } = 0;
        public List<int> ClaimedLevelRewardPacks { get; set; } = new();

        public bool HasClaimedLevelReward(int level) => ClaimedLevelRewardPacks != null && ClaimedLevelRewardPacks.Contains(level);

        public bool IsMaxAccountLevel => AccountLevel >= MaxAccountLevel;

        public string LastLoginDateUtcString { get; set; } = string.Empty;
        public int ConsecutiveLoginDays { get; set; } = 1;

        public string SelectedAvatarId { get; set; } = AvatarCatalog.DefaultAvatarId;
        public List<string> UnlockedAvatarIds { get; set; } = new(AvatarCatalog.GetDefaultUnlockedAvatarIds());

        public Dictionary<string, CardInventoryEntry> Inventory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<SavedDeckData> SavedDecks { get; set; } = new();

        public bool SfxEnabled { get; set; } = true;
        public bool MusicEnabled { get; set; } = true;

        public DateTime? LastLoginDateUtc
        {
            get => DateTime.TryParse(LastLoginDateUtcString, out var dt) ? dt : null;
            set => LastLoginDateUtcString = value.HasValue ? value.Value.ToString("O") : string.Empty;
        }

        public string LastDailyRewardClaimDateUtcString { get; set; } = string.Empty;

        public DateTime? LastDailyRewardClaimDateUtc
        {
            get => DateTime.TryParse(LastDailyRewardClaimDateUtcString, out var dt) ? dt : null;
            set => LastDailyRewardClaimDateUtcString = value.HasValue ? value.Value.ToString("O") : string.Empty;
        }

        public PlayerProfile() { }

        public CardInventoryEntry GetOrCreateEntry(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) throw new ArgumentNullException(nameof(cardId));

            if (!Inventory.TryGetValue(cardId, out var entry))
            {
                entry = new CardInventoryEntry(cardId, isUnlocked: false, initialCopies: 0, foilLevel: 1);
                Inventory[cardId] = entry;
            }
            return entry;
        }

        public bool IsCardUnlocked(string cardId)
        {
            return Inventory.TryGetValue(cardId, out var entry) && entry.IsUnlocked;
        }

        public int GetFoilLevel(string cardId)
        {
            return Inventory.TryGetValue(cardId, out var entry) ? entry.FoilLevel : 1;
        }

        public int GetDuplicateCopies(string cardId)
        {
            return Inventory.TryGetValue(cardId, out var entry) ? entry.DuplicateCopies : 0;
        }

        public void AddCard(string cardId, int copies = 1)
        {
            if (string.IsNullOrEmpty(cardId) || copies <= 0) return;
            var entry = GetOrCreateEntry(cardId);
            bool wasUnlocked = entry.IsUnlocked;
            entry.AddCopies(copies);

            if (!wasUnlocked && entry.IsUnlocked)
            {
                // New card unlock or craft awards 40 XP
                AddAccountExperience(40, out _, out _, out _);
            }
        }

        public bool AddAccountExperience(int xpAmount, out int levelsGained, out int totalRewardGold, out int totalRewardPacks)
        {
            levelsGained = 0;
            totalRewardGold = 0;
            totalRewardPacks = 0;

            if (xpAmount <= 0 || IsMaxAccountLevel) return false;

            CurrentLevelXp += xpAmount;

            while (!IsMaxAccountLevel)
            {
                int needed = GetXpRequiredForNextLevel(AccountLevel);
                if (CurrentLevelXp >= needed)
                {
                    CurrentLevelXp -= needed;
                    AccountLevel++;
                    levelsGained++;

                    int goldReward = AccountLevel * 50;
                    totalRewardGold += goldReward;
                    GoldCoins += goldReward;

                    // Player Level Reward Pack: configurable levels (e.g. 2, 4, 6, 8, 10, 12, 15, 20), granted once
                    if (PlayerLevelRewardConfig.IsRewardLevel(AccountLevel) && !HasClaimedLevelReward(AccountLevel))
                    {
                        ClaimedLevelRewardPacks ??= new List<int>();
                        ClaimedLevelRewardPacks.Add(AccountLevel);
                        RewardPacks++;
                        totalRewardPacks++;
                    }
                }
                else
                {
                    break;
                }
            }

            if (IsMaxAccountLevel)
            {
                CurrentLevelXp = 0;
            }

            if (levelsGained > 0)
            {
                CheckAndUnlockMilestoneAvatars();
            }

            return levelsGained > 0;
        }

        /// <summary>
        /// Automatically unlocks any free milestone avatars that have become available at the current AccountLevel.
        /// </summary>
        public void CheckAndUnlockMilestoneAvatars()
        {
            UnlockedAvatarIds ??= new List<string>();
            foreach (var avatar in AvatarCatalog.GetAll())
            {
                if (avatar.IsFree && avatar.RequiredLevel <= AccountLevel)
                {
                    if (!UnlockedAvatarIds.Contains(avatar.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        UnlockedAvatarIds.Add(avatar.Id);
                    }
                }
            }
        }

        public bool AddAccountExperience(int xpAmount)
        {
            return AddAccountExperience(xpAmount, out _, out _, out _);
        }

        /// <summary>
        /// Safely increments player's gold coin balance from IAP purchases or rewarded ads.
        /// </summary>
        public int AddGoldCoins(int amount)
        {
            if (amount <= 0) return GoldCoins;
            GoldCoins += amount;
            return GoldCoins;
        }

        public bool TrySetPlayerName(string newName, out string failureReason)
        {
            failureReason = string.Empty;
            if (string.IsNullOrWhiteSpace(newName))
            {
                failureReason = "El apodo no puede estar vacío.";
                return false;
            }

            var trimmed = newName.Trim();
            if (trimmed.Length < 3 || trimmed.Length > 16)
            {
                failureReason = "El apodo debe tener entre 3 y 16 caracteres.";
                return false;
            }

            PlayerName = trimmed;
            return true;
        }

        /// <summary>
        /// Checks whether a specific avatar is unlocked by the player (either purchased or awarded free by level).
        /// </summary>
        public bool IsAvatarUnlocked(string? avatarId)
        {
            if (string.IsNullOrEmpty(avatarId)) return false;
            if (UnlockedAvatarIds != null && UnlockedAvatarIds.Contains(avatarId, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var def = AvatarCatalog.GetAvatarById(avatarId);
            if (def != null && def.IsFree && AccountLevel >= def.RequiredLevel)
            {
                UnlockedAvatarIds ??= new List<string>();
                if (!UnlockedAvatarIds.Contains(def.Id, StringComparer.OrdinalIgnoreCase))
                {
                    UnlockedAvatarIds.Add(def.Id);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Purchases an avatar with gold coins if requirements are satisfied, unlocking and equipping it.
        /// </summary>
        public bool TryPurchaseAvatar(string? avatarId, out string failureReason)
        {
            failureReason = string.Empty;
            var def = AvatarCatalog.GetAvatarById(avatarId);
            if (def == null)
            {
                failureReason = "Avatar no encontrado.";
                return false;
            }

            if (IsAvatarUnlocked(avatarId))
            {
                failureReason = "Ya posees este avatar.";
                return false;
            }

            if (AccountLevel < def.RequiredLevel)
            {
                failureReason = $"Requiere Nivel de Cuenta {def.RequiredLevel}.";
                return false;
            }

            if (GoldCoins < def.GoldCost)
            {
                failureReason = $"Monedas de oro insuficientes. Requiere {def.GoldCost:N0} Oro.";
                return false;
            }

            GoldCoins -= def.GoldCost;
            UnlockedAvatarIds ??= new List<string>();
            if (!UnlockedAvatarIds.Contains(def.Id, StringComparer.OrdinalIgnoreCase))
            {
                UnlockedAvatarIds.Add(def.Id);
            }
            SelectedAvatarId = def.Id;
            return true;
        }

        /// <summary>
        /// Equips an already unlocked avatar as the active player portrait.
        /// </summary>
        public bool TryEquipAvatar(string? avatarId, out string failureReason)
        {
            failureReason = string.Empty;
            var def = AvatarCatalog.GetAvatarById(avatarId);
            if (def == null)
            {
                failureReason = "Avatar no encontrado.";
                return false;
            }

            if (!IsAvatarUnlocked(avatarId))
            {
                failureReason = "No tienes desbloqueado este avatar.";
                return false;
            }

            SelectedAvatarId = def.Id;
            return true;
        }

        public bool TryUpgradeFoil(string cardId, out string failureReason, int requiredCopies = RequiredCopiesForFoilUpgrade, int goldCost = GoldCostPerFoilUpgrade)
        {
            failureReason = string.Empty;

            if (!Inventory.TryGetValue(cardId, out var entry) || !entry.IsUnlocked)
            {
                failureReason = "Card is not unlocked in your collection.";
                return false;
            }

            if (entry.FoilLevel >= CardInventoryEntry.MaxFoilLevel)
            {
                failureReason = "Card has already achieved maximum Gold Foil tier.";
                return false;
            }

            if (entry.DuplicateCopies < requiredCopies)
            {
                failureReason = $"Requires {requiredCopies} duplicate copies (Current: {entry.DuplicateCopies}).";
                return false;
            }

            if (GoldCoins < goldCost)
            {
                failureReason = $"Requires {goldCost} Gold Coins (Current: {GoldCoins}).";
                return false;
            }

            // Deduct economy & apply upgrade
            GoldCoins -= goldCost;
            entry.DuplicateCopies -= requiredCopies;
            entry.FoilLevel = Math.Min(CardInventoryEntry.MaxFoilLevel, entry.FoilLevel + 1);

            // Award account progression XP: Tier 2 (Foil) = 100 XP, Tier 3 (Gold) = 200 XP
            int xpReward = entry.FoilLevel == 2 ? 100 : 200;
            AddAccountExperience(xpReward, out _, out _, out _);

            return true;
        }

        public bool TryCraftWithDust(string cardId, int dustCost, out string failureReason)
        {
            failureReason = string.Empty;

            if (dustCost <= 0)
            {
                failureReason = "Invalid dust cost.";
                return false;
            }

            if (EsenciaCometa < dustCost)
            {
                failureReason = $"Requires {dustCost} Esencia Cometa (Current: {EsenciaCometa}).";
                return false;
            }

            EsenciaCometa -= dustCost;
            AddCard(cardId, 1);
            return true;
        }

        /// <summary>
        /// Crafts a card using Esencia Cometa based on its rarity (Option A).
        /// If the card is locked, it unlocks it and awards 40 account XP.
        /// If already unlocked, adds a duplicate copy contributing toward Foil upgrades.
        /// </summary>
        public bool TryCraftCard(CardDefinition card, out string failureReason)
        {
            failureReason = string.Empty;
            if (card == null)
            {
                failureReason = "Carta inválida.";
                return false;
            }

            int cost = CardCraftingConfig.GetCraftCost(card.Rarity);
            if (EsenciaCometa < cost)
            {
                failureReason = $"Esencia insuficiente ({EsenciaCometa}/{cost} Esencia).";
                return false;
            }

            return TryCraftWithDust(card.Id, cost, out failureReason);
        }

        public bool CanCraftCard(CardDefinition card, out int craftCost)
        {
            craftCost = card != null ? CardCraftingConfig.GetCraftCost(card.Rarity) : 0;
            return card != null && EsenciaCometa >= craftCost;
        }

        public bool TryDisenchant(string cardId, int copiesToDisenchant, int dustYieldPerCopy, out string failureReason)
        {
            failureReason = string.Empty;

            if (copiesToDisenchant <= 0 || dustYieldPerCopy <= 0)
            {
                failureReason = "Invalid disenchant parameters.";
                return false;
            }

            if (!Inventory.TryGetValue(cardId, out var entry) || entry.DuplicateCopies < copiesToDisenchant)
            {
                failureReason = "Not enough duplicate copies available to disenchant.";
                return false;
            }

            entry.DuplicateCopies -= copiesToDisenchant;
            EsenciaCometa += copiesToDisenchant * dustYieldPerCopy;
            return true;
        }

        /// <summary>
        /// Disenchants duplicate copies of a card to receive Esencia Cometa based on its rarity.
        /// </summary>
        public bool TryDisenchantCard(CardDefinition card, int copiesToDisenchant, out string failureReason)
        {
            if (card == null)
            {
                failureReason = "Carta inválida.";
                return false;
            }

            int yieldPerCopy = CardCraftingConfig.GetDisenchantYield(card.Rarity);
            return TryDisenchant(card.Id, copiesToDisenchant, yieldPerCopy, out failureReason);
        }

        /// <summary>
        /// Purchases card packs using gold coins (Rules 1 & 13).
        /// Each pack costs 500 gold coins and contains 5 cards with guaranteed unowned cards.
        /// </summary>
        public bool TryPurchaseCardPacks(
            int packCount,
            out MultiPackPurchaseResult result,
            out string failureReason,
            Random? rng = null)
        {
            failureReason = string.Empty;
            result = new MultiPackPurchaseResult();

            if (packCount <= 0)
            {
                failureReason = "Cantidad de sobres inválida.";
                return false;
            }

            int totalCost = packCount * PackGoldCost;
            if (GoldCoins < totalCost)
            {
                failureReason = $"Monedas insuficientes. Requiere {totalCost:N0} Oro (Tienes: {GoldCoins:N0} Oro).";
                return false;
            }

            GoldCoins -= totalCost;
            result = CometaChampions.Core.Rules.CardPackGenerator.GenerateMultiplePacks(this, packCount, null, rng);
            return true;
        }

        /// <summary>
        /// Opens a single booster pack from the player's saved booster pack inventory.
        /// </summary>
        public bool TryOpenBoosterPack(
            out CardPackResult result,
            out string failureReason,
            Random? rng = null)
        {
            failureReason = string.Empty;
            result = new CardPackResult();

            if (BoosterPacks <= 0)
            {
                failureReason = "No tienes sobres guardados disponibles para abrir.";
                return false;
            }

            BoosterPacks--;
            result = CometaChampions.Core.Rules.CardPackGenerator.GeneratePack(this, null, rng);
            return true;
        }

        /// <summary>
        /// Opens a single 3-card Player Level Reward Pack from the player's saved reward pack inventory.
        /// Reward Packs have NO new-card guarantee, can contain only duplicates, and count towards foil progression.
        /// </summary>
        public bool TryOpenRewardPack(
            out CardPackResult result,
            out string failureReason,
            Random? rng = null)
        {
            failureReason = string.Empty;
            result = new CardPackResult();

            if (RewardPacks <= 0)
            {
                failureReason = "No tienes sobres de recompensa disponibles para abrir.";
                return false;
            }

            RewardPacks--;
            result = CometaChampions.Core.Rules.CardPackGenerator.GenerateRewardPack(this, null, rng);
            return true;
        }

        /// <summary>
        /// Evaluates login streak. Returns true if a new day login reward is ready to claim.
        /// </summary>
        public bool ProcessDailyLogin(DateTime currentDateUtc)
        {
            var lastLogin = LastLoginDateUtc;
            LastLoginDateUtc = currentDateUtc;

            if (!lastLogin.HasValue)
            {
                ConsecutiveLoginDays = 1;
                AddAccountExperience(25, out _, out _, out _);
                return true;
            }

            var daysDifference = (currentDateUtc.Date - lastLogin.Value.Date).Days;

            if (daysDifference == 1)
            {
                // Consecutive day
                ConsecutiveLoginDays = (ConsecutiveLoginDays % 7) + 1;
                AddAccountExperience(25, out _, out _, out _);
                return true;
            }
            else if (daysDifference > 1)
            {
                // Streak broken, reset to Day 1
                ConsecutiveLoginDays = 1;
                AddAccountExperience(25, out _, out _, out _);
                return true;
            }

            // Same day, streak already accounted for
            return false;
        }

        /// <summary>
        /// Checks whether the player is eligible to open/claim their daily reward pack today.
        /// </summary>
        public bool CanClaimDailyReward(DateTime currentDateUtc)
        {
            var lastClaim = LastDailyRewardClaimDateUtc;
            if (!lastClaim.HasValue) return true;
            return (currentDateUtc.Date - lastClaim.Value.Date).Days >= 1;
        }

        /// <summary>
        /// Returns the day index in the 7-day cycle (1..7) that corresponds to today's reward pack:
        /// - If eligible to claim today: returns the day index that will be awarded (advancing streak if consecutive, or resetting to 1).
        /// - If already claimed today: returns the day index that was claimed today.
        /// </summary>
        public int GetCurrentDailyRewardDayIndex(DateTime currentDateUtc)
        {
            var lastClaim = LastDailyRewardClaimDateUtc;
            if (!lastClaim.HasValue)
            {
                return 1;
            }

            int daysDiff = (currentDateUtc.Date - lastClaim.Value.Date).Days;
            if (daysDiff <= 0)
            {
                // Already claimed today (or same day): return the day index of today's claim
                int current = ConsecutiveLoginDays;
                if (current <= 0) current = 1;
                return ((current - 1) % 7) + 1;
            }
            else if (daysDiff == 1)
            {
                // Consecutive day: advances to the next day in the 7-day cycle
                int current = ConsecutiveLoginDays;
                if (current <= 0) current = 0;
                return (current % 7) + 1;
            }
            else
            {
                // Missed one or more days: streak resets to day 1
                return 1;
            }
        }

        /// <summary>
        /// Claims the daily reward pack for today, evaluating consecutive streak, advancing days (1..7),
        /// and distributing cards, gold coins, dust, booster packs, and avatars.
        /// </summary>
        public DailyRewardResult ClaimDailyReward(DateTime currentDateUtc)
        {
            if (!CanClaimDailyReward(currentDateUtc))
            {
                throw new InvalidOperationException("El pack de recompensas diarias ya fue reclamado hoy.");
            }

            ConsecutiveLoginDays = GetCurrentDailyRewardDayIndex(currentDateUtc);

            var result = DailyRewardCatalog.GenerateReward(ConsecutiveLoginDays, this);

            // Apply rewards to profile
            AddGoldCoins(result.GoldCoins);
            foreach (var cardId in result.AwardedCardIds)
            {
                AddCard(cardId, 1);
            }
            EsenciaCometa += result.EsenciaCometa;
            BoosterPacks += result.BoosterPacks;

            if (!string.IsNullOrEmpty(result.AwardedAvatarId))
            {
                UnlockedAvatarIds ??= new List<string>();
                if (!UnlockedAvatarIds.Contains(result.AwardedAvatarId, StringComparer.OrdinalIgnoreCase))
                {
                    UnlockedAvatarIds.Add(result.AwardedAvatarId);
                }
            }

            if (result.AccountXp > 0)
            {
                AddAccountExperience(result.AccountXp);
            }

            LastDailyRewardClaimDateUtc = currentDateUtc;
            LastLoginDateUtc = currentDateUtc;

            return result;
        }

        /// <summary>
        /// Creates a starting profile with default resources and starter cards matching UI_test.png.
        /// </summary>
        public static PlayerProfile CreateDefaultProfile()
        {
            var profile = new PlayerProfile
            {
                PlayerId = Guid.NewGuid().ToString("N"),
                PlayerName = GenerateDefaultPlayerName(),
                AccountLevel = 1,
                CurrentLevelXp = 0,
                GoldCoins = DefaultInitialGold,
                EsenciaCometa = DefaultInitialDust,
                BoosterPacks = DefaultInitialPacks,
                RewardPacks = 0,
                ClaimedLevelRewardPacks = new List<int>(),
                SelectedAvatarId = AvatarCatalog.DefaultAvatarId,
                UnlockedAvatarIds = new List<string>(AvatarCatalog.GetDefaultUnlockedAvatarIds()),
                LastLoginDateUtc = DateTime.UtcNow,
                ConsecutiveLoginDays = 1
            };

            // Starter cards from UI_test.png
            var starterCards = new[]
            {
                "CHAMP_ESPECTRO",
                "CHAMP_AZTECA",
                "CHAMP_JUMBO_COBRA",
                "CHAMP_DRAKY",
                "CHAMP_ORION",
                "TROMPO_ESPECTRO",
                "TROMPO_AZTECA",
                "BEAST_ESPECTRO",
                "ARENA_TEMPLO_MILENARIO"
            };

            // Register all cards in catalog, setting starters as unlocked with 2 duplicates (2/5 for foil upgrade)
            // Espectro starts with 5 copies (5/5) so it is immediately upgradable to Foil
            foreach (var card in CardCatalog.AllCards)
            {
                bool isStarter = starterCards.Contains(card.Id, StringComparer.OrdinalIgnoreCase);
                int initialCopies = 0;
                if (string.Equals(card.Id, "CHAMP_ESPECTRO", StringComparison.OrdinalIgnoreCase))
                {
                    initialCopies = 5; // Ready to upgrade to Foil immediately
                }
                else if (isStarter)
                {
                    initialCopies = 2;
                }

                profile.Inventory[card.Id] = new CardInventoryEntry(
                    card.Id,
                    isUnlocked: isStarter,
                    initialCopies: initialCopies,
                    foilLevel: 1);
            }

            return profile;
        }
    }
}
