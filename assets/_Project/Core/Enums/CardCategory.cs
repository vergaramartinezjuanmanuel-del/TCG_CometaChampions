namespace CometaChampions.Core.Enums
{
    /// <summary>
    /// The 4 main card categories defined in the 80-card base set ("Tomo 1").
    /// </summary>
    public enum CardCategory
    {
        Champion = 1,       // 20 cards: Youth warriors in specialized suits
        KombatBeast = 2,    // 20 cards: Ancient mythical beasts (evolved/aggressive stats)
        PhysicalTrompo = 3, // 20 cards: Action/Equipment/Spell support cards
        Arena = 4           // 20 cards: Battlefield terrain modifiers (1 active at a time)
    }

    /// <summary>
    /// Rarity tiers determining card foil visual effects, drops, and craft costs.
    /// </summary>
    public enum CardRarity
    {
        Common = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// The three sequential phases of each player's turn.
    /// </summary>
    public enum TurnPhase
    {
        Preparation = 1, // Draw 1 card, ready all exhausted cards/energy, optionally convert 1 hand card to energy
        Action = 2,      // Spend energy to summon, cast spells, play arenas, attack
        Ending = 3       // Resolve end-of-turn effects, clean up, pass turn
    }
}
