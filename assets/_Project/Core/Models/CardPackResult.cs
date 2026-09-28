#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Details for an individual card revealed from a card pack.
    /// </summary>
    [Serializable]
    public class PackOpeningCardResult
    {
        public CardDefinition Card { get; set; } = null!;
        public bool IsNew { get; set; }
        public int DuplicateCopiesBefore { get; set; }
        public int DuplicateCopiesAfter { get; set; }
        public int FoilLevel { get; set; } = 1;

        public PackOpeningCardResult() { }

        public PackOpeningCardResult(
            CardDefinition card,
            bool isNew,
            int duplicateCopiesBefore,
            int duplicateCopiesAfter,
            int foilLevel)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            IsNew = isNew;
            DuplicateCopiesBefore = duplicateCopiesBefore;
            DuplicateCopiesAfter = duplicateCopiesAfter;
            FoilLevel = foilLevel;
        }
    }

    /// <summary>
    /// Type of booster pack determining card count, guarantees, and origin.
    /// </summary>
    public enum CardPackType
    {
        Purchased,      // 5 cards, guarantees >= 1 new card when available
        LevelReward     // 3 cards, free level reward, no new-card guarantee, duplicates allowed
    }

    /// <summary>
    /// Authoritative result of opening a single booster pack.
    /// </summary>
    [Serializable]
    public class CardPackResult
    {
        public int PackIndex { get; set; } = 1;
        public CardPackType PackType { get; set; } = CardPackType.Purchased;
        public string PackTitle { get; set; } = "SOBRE TOMO 1";
        public List<PackOpeningCardResult> Cards { get; set; } = new();

        public int NewCardsCount => Cards.Count(c => c.IsNew);
        public int DuplicateCardsCount => Cards.Count(c => !c.IsNew);

        public CardPackResult() { }

        public CardPackResult(int packIndex, IEnumerable<PackOpeningCardResult> cards)
        {
            PackIndex = packIndex;
            Cards = cards?.ToList() ?? new List<PackOpeningCardResult>();
            PackType = CardPackType.Purchased;
            PackTitle = "SOBRE TOMO 1";
        }

        public CardPackResult(
            int packIndex,
            IEnumerable<PackOpeningCardResult> cards,
            CardPackType packType,
            string packTitle)
        {
            PackIndex = packIndex;
            Cards = cards?.ToList() ?? new List<PackOpeningCardResult>();
            PackType = packType;
            PackTitle = packTitle;
        }
    }

    /// <summary>
    /// Authoritative result of a multi-pack purchase transaction (Rules 1 & 13).
    /// </summary>
    [Serializable]
    public class MultiPackPurchaseResult
    {
        public int PacksCount { get; set; }
        public int TotalGoldSpent { get; set; }
        public List<CardPackResult> Packs { get; set; } = new();

        public int TotalCardsCount => Packs.Sum(p => p.Cards.Count);
        public int TotalNewCardsUnlocked => Packs.Sum(p => p.NewCardsCount);
        public int TotalDuplicatesAcquired => Packs.Sum(p => p.DuplicateCardsCount);

        public MultiPackPurchaseResult() { }

        public MultiPackPurchaseResult(int packsCount, int totalGoldSpent, IEnumerable<CardPackResult> packs)
        {
            PacksCount = packsCount;
            TotalGoldSpent = totalGoldSpent;
            Packs = packs?.ToList() ?? new List<CardPackResult>();
        }
    }
}
