namespace CometaChampions.Core.Enums
{
    /// <summary>
    /// The 4 main clans of the Cometa Champions universe, plus Neutral.
    /// Follows the "Círculo Cometa" advantage wheel:
    /// Milenarios > Fugitivos > Depredadores > Míticos > Milenarios.
    /// </summary>
    public enum ClanType
    {
        None = 0,
        Milenarios = 1,    // 🌳 Strength / Nature
        Fugitivos = 2,     // ⛓️ Cunning / Evasion
        Depredadores = 3,  // 🪓 Speed / Ferocity
        Miticos = 4,       // 🐉 Wisdom / Magic / Cosmic
        Neutral = 5        // Common arenas/artifacts
    }
}
