using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Tracks an individual card's unlock status, duplicate count, and foil enhancement level
    /// in the player's collection.
    /// </summary>
    [Serializable]
    public class CardInventoryEntry
    {
        public const int MaxFoilLevel = 3; // 1 = Standard, 2 = Foil, 3 = Gold

        public string CardId { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
        public int DuplicateCopies { get; set; }
        public int FoilLevel { get; set; } = 1;

        public CardInventoryEntry() { }

        public CardInventoryEntry(string cardId, bool isUnlocked = true, int initialCopies = 0, int foilLevel = 1)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            IsUnlocked = isUnlocked;
            DuplicateCopies = Math.Max(0, initialCopies);
            FoilLevel = Math.Clamp(foilLevel, 1, MaxFoilLevel);
        }

        public void AddCopies(int count)
        {
            if (count <= 0) return;
            IsUnlocked = true;
            DuplicateCopies += count;
        }

        public bool CanUpgradeFoil(int requiredCopies = 5)
        {
            return IsUnlocked && FoilLevel < MaxFoilLevel && DuplicateCopies >= requiredCopies;
        }

        public bool TryUpgradeFoil(int requiredCopies = 5)
        {
            if (!CanUpgradeFoil(requiredCopies)) return false;

            DuplicateCopies -= requiredCopies;
            FoilLevel = Math.Min(MaxFoilLevel, FoilLevel + 1);
            return true;
        }
    }
}
