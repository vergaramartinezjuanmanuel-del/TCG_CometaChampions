using System;
using System.Collections.Generic;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Represents a saved user deck configuration containing exactly 30 cards and assigned clan.
    /// </summary>
    [Serializable]
    public class SavedDeckData
    {
        public string DeckId { get; set; } = Guid.NewGuid().ToString("N");
        public string DeckName { get; set; } = "Nuevo Mazo";
        public ClanType PrimaryClan { get; set; } = ClanType.Neutral;
        public List<string> CardIds { get; set; } = new();

        public SavedDeckData() { }

        public SavedDeckData(string deckName, ClanType primaryClan, IEnumerable<string>? cardIds = null)
        {
            DeckName = deckName ?? "Nuevo Mazo";
            PrimaryClan = primaryClan;
            if (cardIds != null)
            {
                CardIds.AddRange(cardIds);
            }
        }
    }
}
