using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Pure C# immutable definition for a selectable player avatar.
    /// Represents character portraits that unlock through account progression (Level 1..20)
    /// and/or Gold Coin purchases.
    /// </summary>
    [Serializable]
    public class PlayerAvatarDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Clan { get; }
        public string FileName { get; }
        public int RequiredLevel { get; }
        public int GoldCost { get; }

        public bool IsFree => GoldCost <= 0;

        public PlayerAvatarDefinition(
            string id,
            string name,
            string clan,
            string fileName,
            int requiredLevel,
            int goldCost)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Clan = clan ?? string.Empty;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            RequiredLevel = Math.Max(1, requiredLevel);
            GoldCost = Math.Max(0, goldCost);
        }
    }
}
