#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Static authoritative catalog of player avatars with progression milestones and gold costs.
    /// Spans Levels 1 through 20 across all 4 clans (Milenarios, Fugitivos, Depredadores, Míticos).
    /// </summary>
    public static class AvatarCatalog
    {
        public const string DefaultAvatarId = "AVATAR_COMETIN";

        private static readonly List<PlayerAvatarDefinition> Avatars = new()
        {
            // === NIVEL 1: 5 Avatares Iniciales (3 Gratuitos por defecto, 2 por Oro) ===
            new PlayerAvatarDefinition("AVATAR_COMETIN", "Cometín", "Milenarios", "cometin_Mi.png", 1, 0),
            new PlayerAvatarDefinition("AVATAR_ESPECTRO", "Espectro", "Míticos", "espectro_My.png", 1, 0),
            new PlayerAvatarDefinition("AVATAR_AVISPON", "Avispón", "Depredadores", "avispon_P.png", 1, 0),
            new PlayerAvatarDefinition("AVATAR_AZTECA", "Azteca", "Milenarios", "azteca_Mi.png", 1, 100),
            new PlayerAvatarDefinition("AVATAR_DIAMANTE", "Diamante", "Fugitivos", "diamante_F.png", 1, 150),

            // === NIVEL 2 - 3: 3 Avatares ===
            new PlayerAvatarDefinition("AVATAR_DRAKY", "Draky", "Fugitivos", "draky_F.png", 2, 0),
            new PlayerAvatarDefinition("AVATAR_SILVER", "Silver", "Milenarios", "silver_Mi.png", 2, 250),
            new PlayerAvatarDefinition("AVATAR_PANTHER", "Panther", "Depredadores", "panther_P.png", 3, 350),

            // === NIVEL 4 - 6: 3 Avatares ===
            new PlayerAvatarDefinition("AVATAR_TIGRE", "Tigre", "Depredadores", "tigre_P.png", 4, 0),
            new PlayerAvatarDefinition("AVATAR_DIAMANTIN", "Diamantín", "Milenarios", "diamantin_Mi.png", 5, 450),
            new PlayerAvatarDefinition("AVATAR_ORION", "Orión", "Míticos", "orion_My.png", 6, 500),

            // === NIVEL 7 - 9: 3 Avatares ===
            new PlayerAvatarDefinition("AVATAR_JUMBO_COBRA", "Jumbo Cobra", "Fugitivos", "jumboCobra_F.png", 7, 0),
            new PlayerAvatarDefinition("AVATAR_SPIDER", "Spider", "Fugitivos", "spider_F.png", 8, 600),
            new PlayerAvatarDefinition("AVATAR_FENIX", "Fénix", "Míticos", "fenix_My.png", 9, 750),

            // === NIVEL 10 - 14: 3 Avatares ===
            new PlayerAvatarDefinition("AVATAR_REX", "Rex", "Míticos", "rex_My.png", 10, 0),
            new PlayerAvatarDefinition("AVATAR_KING_COBRA", "King Cobra", "Depredadores", "kingCobra_P.png", 12, 1000),
            new PlayerAvatarDefinition("AVATAR_TURBO_COBRA", "Turbo Cobra", "Milenarios", "turboCobra_Mi.png", 14, 1200),

            // === NIVEL 15 - 20: 3 Avatares Finales / Maestría ===
            new PlayerAvatarDefinition("AVATAR_TURBO_DRAGON", "Turbo Dragon", "Depredadores", "turboDragon_P.png", 15, 0),
            new PlayerAvatarDefinition("AVATAR_KING_TURBO", "King Turbo", "Fugitivos", "kingTurbo_F.png", 18, 2000),
            new PlayerAvatarDefinition("AVATAR_TURBO_FLASH", "Turbo Flash", "Míticos", "turboFlash_My.png", 20, 0)
        };

        private static readonly Dictionary<string, PlayerAvatarDefinition> Lookup =
            Avatars.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves an avatar definition by its unique identifier, or null if not found.
        /// </summary>
        public static PlayerAvatarDefinition? GetAvatarById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Lookup.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>
        /// Returns all 20 avatars in the progression catalog.
        /// </summary>
        public static IReadOnlyList<PlayerAvatarDefinition> GetAll() => Avatars;

        /// <summary>
        /// Returns the initial free avatars awarded to every new player at Account Level 1.
        /// </summary>
        public static IReadOnlyList<string> GetDefaultUnlockedAvatarIds()
        {
            return Avatars
                .Where(a => a.RequiredLevel == 1 && a.IsFree)
                .Select(a => a.Id)
                .ToList();
        }
    }
}
