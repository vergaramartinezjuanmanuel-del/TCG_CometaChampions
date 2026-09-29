using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Helpers
{
    /// <summary>
    /// Resolves flat card artwork, clan banners, clan backgrounds, UI badges,
    /// and category display names from Assets/_Project/Art/Tarjetas/.
    /// </summary>
    public static class CardArtMapper
    {
        private static readonly Dictionary<string, string> CharacterAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Panther Cazadora"] = "Panther",
            ["Diamante Leviatán"] = "Diamante",
            ["Diamante Leviatan"] = "Diamante",
            ["Fénix Ave Ígnea"] = "Fenix",
            ["Fenix Ave Ignea"] = "Fenix",
            ["Rex Dracónico"] = "Rex",
            ["Rex Draconico"] = "Rex",
            ["Gólem Azteca"] = "Azteca",
            ["Golem Azteca"] = "Azteca",
            ["Jumbo King Cobra"] = "King Cobra",
            ["Trompo Clásico"] = "Silver",
            ["Trompo Clasico"] = "Silver",
            ["Clásico"] = "Silver",
            ["Clasico"] = "Silver",
            ["Trompo Cometa Fuego"] = "Fénix",
            ["Cometa Fuego"] = "Fénix",
            ["Trompo Dragón Solar"] = "Turbo Dragón",
            ["Trompo Dragon Solar"] = "Turbo Dragón",
            ["Dragón Solar"] = "Turbo Dragón",
            ["Dragon Solar"] = "Turbo Dragón",
            ["Trompo Vórtice"] = "Espectro",
            ["Trompo Vortice"] = "Espectro",
            ["Vórtice"] = "Espectro",
            ["Vortice"] = "Espectro",
            ["Pista Draconia"] = "La Pista de Carreras"
        };

        private static readonly Dictionary<string, string> TrompoAccentedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Avispon"] = "Avispón",
            ["Avispón"] = "Avispón",
            ["Cometin"] = "Cometín",
            ["Cometín"] = "Cometín",
            ["Diamantin"] = "Diamantín",
            ["Diamantín"] = "Diamantín",
            ["Fenix"] = "Fénix",
            ["Fénix"] = "Fénix",
            ["Orion"] = "Orión",
            ["Orión"] = "Orión",
            ["Turbo Dragon"] = "Turbo Dragón",
            ["Turbo Dragón"] = "Turbo Dragón"
        };

        public static string GetCostIconPath() => "Tarjetas/UI/costo.png";

        public static string GetPlecaPath() => "Tarjetas/UI/pleca.png";

        public static string GetClanBannerPath(ClanType clan)
        {
            return clan switch
            {
                ClanType.Milenarios => "Tarjetas/UI/Milenials.png",
                ClanType.Fugitivos => "Tarjetas/UI/Fugitives.png",
                ClanType.Depredadores => "Tarjetas/UI/Predators.png",
                ClanType.Miticos => "Tarjetas/UI/Mythicals.png",
                _ => "Tarjetas/UI/Milenials.png"
            };
        }

        public static string GetClanBannerPath(string clanId)
        {
            return GetClanBannerPath(ParseClanType(clanId));
        }

        public static string GetClanBgPath(ClanType clan)
        {
            return clan switch
            {
                ClanType.Milenarios => "Tarjetas/UI/BG_Milenials.png",
                ClanType.Fugitivos => "Tarjetas/UI/BG_Fugitives.png",
                ClanType.Depredadores => "Tarjetas/UI/BG_Predators.png",
                ClanType.Miticos => "Tarjetas/UI/BG_Mythicals.png",
                _ => "Tarjetas/UI/BG_Milenials.png"
            };
        }

        public static string GetClanBgPath(string clanId)
        {
            return GetClanBgPath(ParseClanType(clanId));
        }

        public static string GetClanColorHex(ClanType clan)
        {
            return clan switch
            {
                ClanType.Milenarios => "#2D7FF9",
                ClanType.Fugitivos => "#5AC828",
                ClanType.Depredadores => "#F05C08",
                ClanType.Miticos => "#D848E8",
                _ => "#2D7FF9"
            };
        }

        public static string GetClanColorHex(string clanId)
        {
            return GetClanColorHex(ParseClanType(clanId));
        }

        public static ClanType ParseClanType(string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId)) return ClanType.Milenarios;
            string norm = RemoveDiacritics(clanId.Trim()).ToLowerInvariant();
            if (norm.Contains("milen")) return ClanType.Milenarios;
            if (norm.Contains("fugit")) return ClanType.Fugitivos;
            if (norm.Contains("depred") || norm.Contains("predat")) return ClanType.Depredadores;
            if (norm.Contains("mitic") || norm.Contains("mythic")) return ClanType.Miticos;
            if (norm.Contains("neutral")) return ClanType.Neutral;
            return ClanType.Milenarios;
        }

        public static string GetCategoryDisplayName(CardCategory category)
        {
            return category switch
            {
                CardCategory.Champion => "HEROES",
                CardCategory.KombatBeast => "BESTIAS",
                CardCategory.PhysicalTrompo => "TROMPOS",
                CardCategory.Arena => "ARENAS",
                _ => "HEROES"
            };
        }

        public static string GetCategoryDisplayName(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "HEROES";
            string norm = RemoveDiacritics(category.Trim()).ToLowerInvariant();
            if (norm.Contains("champ") || norm.Contains("hero")) return "HEROES";
            if (norm.Contains("kombat") || norm.Contains("beast") || norm.Contains("bestia")) return "BESTIAS";
            if (norm.Contains("trompo")) return "TROMPOS";
            if (norm.Contains("arena") || norm.Contains("locacion")) return "ARENAS";
            return "HEROES";
        }

        public static string GetCardDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
            string clean = StripParentheses(rawName).Trim();
            return RemoveDiacritics(clean).ToUpperInvariant();
        }

        public static string GetFlatArtPath(CardDefinition definition)
        {
            if (definition == null) return string.Empty;

            if (!string.IsNullOrEmpty(definition.ImagePath))
            {
                string converted = ConvertLegacyPathToTarjetasPath(definition.ImagePath);
                if (!string.IsNullOrEmpty(converted)) return converted;
            }

            var showcase = CardShowcaseCatalog.GetCardByDefinition(definition);
            if (showcase != null)
            {
                return GetFlatArtPath(showcase);
            }

            return GetFlatArtPath(definition.Category.ToString(), definition.Name);
        }

        public static string GetFlatArtPath(ShowcaseCardData cardData)
        {
            if (cardData == null) return string.Empty;

            if (!string.IsNullOrEmpty(cardData.Image))
            {
                string converted = ConvertLegacyPathToTarjetasPath(cardData.Image);
                if (!string.IsNullOrEmpty(converted)) return converted;
            }

            return GetFlatArtPath(cardData.Category, cardData.Name);
        }

        public static string GetFlatArtPath(string category, string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName)) return string.Empty;

            string folder = ResolveFolderName(category);
            string cleanName = NormalizeCharacterFileName(folder, baseName);
            return $"Tarjetas/{folder}/{cleanName}.png";
        }

        public static string ConvertLegacyPathToTarjetasPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;

            string path = relativePath.Replace('\\', '/').Trim();
            if (path.StartsWith("/")) path = path.Substring(1);

            if (path.StartsWith("Tarjetas/", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeTrailingUnderscoreAndSpace(path);
            }

            if (path.StartsWith("UI/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Heroes/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Bestias/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Trompos/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Arenas/", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeTrailingUnderscoreAndSpace("Tarjetas/" + path);
            }

            if (path.StartsWith("CC_Export/", StringComparison.OrdinalIgnoreCase))
            {
                string file = path.Substring("CC_Export/".Length);
                return NormalizeTrailingUnderscoreAndSpace("Tarjetas/Heroes/" + file);
            }

            if (path.StartsWith("CK_Export/", StringComparison.OrdinalIgnoreCase))
            {
                string file = path.Substring("CK_Export/".Length);
                return NormalizeTrailingUnderscoreAndSpace("Tarjetas/Bestias/" + file);
            }

            if (path.StartsWith("Trompos_Export/", StringComparison.OrdinalIgnoreCase))
            {
                string file = path.Substring("Trompos_Export/".Length);
                return NormalizeTrailingUnderscoreAndSpace("Tarjetas/Trompos/" + file);
            }

            if (path.StartsWith("Arena_Export/", StringComparison.OrdinalIgnoreCase))
            {
                string file = path.Substring("Arena_Export/".Length);
                return NormalizeTrailingUnderscoreAndSpace("Tarjetas/Arenas/" + file);
            }

            return string.Empty;
        }

        private static string NormalizeTrailingUnderscoreAndSpace(string path)
        {
            if (path.EndsWith("_.png", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 5).TrimEnd() + ".png";
            }
            else if (path.EndsWith(" .png", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 5).TrimEnd() + ".png";
            }
            return path;
        }

        private static string ResolveFolderName(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "Heroes";
            string norm = RemoveDiacritics(category.Trim()).ToLowerInvariant();
            if (norm.Contains("champ") || norm.Contains("hero")) return "Heroes";
            if (norm.Contains("kombat") || norm.Contains("beast") || norm.Contains("bestia")) return "Bestias";
            if (norm.Contains("trompo")) return "Trompos";
            if (norm.Contains("arena") || norm.Contains("locacion")) return "Arenas";
            return "Heroes";
        }

        private static string NormalizeCharacterFileName(string folder, string rawName)
        {
            string name = StripParentheses(rawName).Trim();
            if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4).TrimEnd('_', ' ');
            }

            if (CharacterAliases.TryGetValue(name, out var alias))
            {
                name = alias;
            }

            if (folder.Equals("Trompos", StringComparison.OrdinalIgnoreCase))
            {
                if (name.StartsWith("Trompo ", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring("Trompo ".Length).Trim();
                }
                if (CharacterAliases.TryGetValue(name, out var trompoAlias))
                {
                    name = trompoAlias;
                }
                string unaccented = RemoveDiacritics(name);
                if (TrompoAccentedNames.TryGetValue(unaccented, out var accented))
                {
                    return accented;
                }
                return name;
            }

            if (folder.Equals("Heroes", StringComparison.OrdinalIgnoreCase) ||
                folder.Equals("Bestias", StringComparison.OrdinalIgnoreCase))
            {
                return RemoveDiacritics(name);
            }

            // Arenas keep their exact Spanish accents
            return name;
        }

        private static string StripParentheses(string input)
        {
            int idx = input.IndexOf('(');
            return idx > 0 ? input.Substring(0, idx).Trim() : input;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
