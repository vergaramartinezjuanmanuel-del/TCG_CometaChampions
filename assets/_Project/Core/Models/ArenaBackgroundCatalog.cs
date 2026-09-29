using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Metadata definition for an official arena background.
    /// </summary>
    [Serializable]
    public class ArenaBackgroundDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public ClanType? AffiliatedClan { get; }
        public string FileName { get; }

        public ArenaBackgroundDefinition(string id, string displayName, string description, ClanType? affiliatedClan)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            AffiliatedClan = affiliatedClan;
            FileName = id + ".jpg";
        }
    }

    /// <summary>
    /// Authoritative catalog of the 6 official arena backgrounds and UI environments
    /// used in Cometa Champions TCG.
    /// </summary>
    public static class ArenaBackgroundCatalog
    {
        public const string TemploMilenario = "bg_arena_templo_milenario";
        public const string PistaDraconia = "bg_arena_pista_draconia";
        public const string LlanurasTirano = "bg_arena_llanuras_tirano";
        public const string NebulosaCosmica = "bg_arena_nebulosa_cosmica";
        public const string Pedestal3DActive = "bg_pedestal_3d_active";
        public const string Pedestal3DIdle = "bg_pedestal_3d_idle";

        private static readonly List<ArenaBackgroundDefinition> Backgrounds = new()
        {
            new ArenaBackgroundDefinition(
                TemploMilenario,
                "Templo Milenario",
                "Arena ancestral del clan Milenarios con glifos rúnicos verdes.",
                ClanType.Milenarios
            ),
            new ArenaBackgroundDefinition(
                PistaDraconia,
                "Pista Draconia",
                "Arena de velocidad cyber-neón del clan Fugitivos.",
                ClanType.Fugitivos
            ),
            new ArenaBackgroundDefinition(
                LlanurasTirano,
                "Llanuras del Tirano",
                "Arena volcánica de magma del clan Depredadores.",
                ClanType.Depredadores
            ),
            new ArenaBackgroundDefinition(
                NebulosaCosmica,
                "Nebulosa Cósmica",
                "Vórtice estelar para batallas míticas o invocación de cartas.",
                ClanType.Miticos
            ),
            new ArenaBackgroundDefinition(
                Pedestal3DActive,
                "Pedestal 3D (Activo)",
                "Base holográfica con portal de energía encendido.",
                null
            ),
            new ArenaBackgroundDefinition(
                Pedestal3DIdle,
                "Pedestal 3D (Reposo)",
                "Base neutral limpia en estado inactivo sin portal de luz.",
                null
            )
        };

        private static readonly string[] BackgroundNames = Backgrounds.Select(b => b.Id).ToArray();
        private static readonly Dictionary<string, ArenaBackgroundDefinition> Lookup =
            Backgrounds.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets all 6 official arena background definitions.
        /// </summary>
        public static IReadOnlyList<ArenaBackgroundDefinition> GetAllBackgrounds()
        {
            return Backgrounds;
        }

        /// <summary>
        /// Gets all 6 official arena background IDs.
        /// </summary>
        public static IReadOnlyList<string> GetAllBackgroundNames()
        {
            return BackgroundNames;
        }

        /// <summary>
        /// Returns the definition for a given background ID, or null if not found.
        /// </summary>
        public static ArenaBackgroundDefinition GetBackgroundById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Lookup.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>
        /// Returns true if the given background ID is one of the 6 official backgrounds.
        /// </summary>
        public static bool IsValidBackground(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return Lookup.ContainsKey(id);
        }

        /// <summary>
        /// Returns the friendly display name for a background ID.
        /// </summary>
        public static string GetDisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Arena";
            var def = GetBackgroundById(id);
            return def != null ? def.DisplayName : id;
        }

        /// <summary>
        /// Randomly selects one of the 6 official background definitions.
        /// </summary>
        public static ArenaBackgroundDefinition GetRandomBackground(System.Random rng = null)
        {
            rng ??= new System.Random();
            int index = rng.Next(Backgrounds.Count);
            return Backgrounds[index];
        }

        /// <summary>
        /// Randomly selects one of the 6 official background IDs.
        /// </summary>
        public static string GetRandomBackgroundName(System.Random rng = null)
        {
            return GetRandomBackground(rng).Id;
        }
    }
}
