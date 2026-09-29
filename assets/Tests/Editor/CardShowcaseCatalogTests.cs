using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Helpers;
using CometaChampions.Core.Models;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class CardShowcaseCatalogTests
    {
        [Test]
        public void Catalog_ContainsExactly80Cards()
        {
            Assert.IsNotNull(CardShowcaseCatalog.Cards);
            Assert.AreEqual(80, CardShowcaseCatalog.Cards.Count, "The showcase catalog must contain all 80 official cards.");
        }

        [Test]
        public void Catalog_Contains20CardsPerCategory()
        {
            var champions = CardShowcaseCatalog.GetCardsByCategory("champions");
            var trompos = CardShowcaseCatalog.GetCardsByCategory("trompos");
            var kombat = CardShowcaseCatalog.GetCardsByCategory("kombat");
            var arenas = CardShowcaseCatalog.GetCardsByCategory("arenas");

            Assert.AreEqual(20, champions.Count, "Must have 20 Champions cards.");
            Assert.AreEqual(20, trompos.Count, "Must have 20 Trompos cards.");
            Assert.AreEqual(20, kombat.Count, "Must have 20 Kombat cards.");
            Assert.AreEqual(20, arenas.Count, "Must have 20 Arenas cards.");
        }

        [Test]
        public void Catalog_Contains3OfficialReversos()
        {
            Assert.IsNotNull(CardShowcaseCatalog.Reversos);
            Assert.AreEqual(3, CardShowcaseCatalog.Reversos.Count);

            var oro = CardShowcaseCatalog.GetReversoById("oro");
            var color = CardShowcaseCatalog.GetReversoById("color");
            var bi = CardShowcaseCatalog.GetReversoById("bi");

            Assert.IsNotNull(oro);
            Assert.AreEqual("Dorado de Campeón", oro.Name);
            Assert.AreEqual("Reverso_Export/reverso_Oro.png", oro.File);

            Assert.IsNotNull(color);
            Assert.AreEqual("Color Oficial", color.Name);
            Assert.AreEqual("Reverso_Export/reverso_Color.png", color.File);

            Assert.IsNotNull(bi);
            Assert.AreEqual("Bicolor Clásico", bi.Name);
            Assert.AreEqual("Reverso_Export/reverso_Bi.png", bi.File);
        }

        [Test]
        public void Catalog_ContainsClansData()
        {
            Assert.IsNotNull(CardShowcaseCatalog.Clans);
            Assert.GreaterOrEqual(CardShowcaseCatalog.Clans.Count, 4);

            var milenarios = CardShowcaseCatalog.GetClanById("milenarios");
            var fugitivos = CardShowcaseCatalog.GetClanById("fugitivos");
            var depredadores = CardShowcaseCatalog.GetClanById("depredadores");
            var miticos = CardShowcaseCatalog.GetClanById("miticos");

            Assert.IsNotNull(milenarios);
            Assert.AreEqual("Milenarios", milenarios.Name);
            Assert.IsNotNull(fugitivos);
            Assert.AreEqual("Fugitivos", fugitivos.Name);
            Assert.IsNotNull(depredadores);
            Assert.AreEqual("Depredadores", depredadores.Name);
            Assert.IsNotNull(miticos);
            Assert.AreEqual("Míticos", miticos.Name);
        }

        [Test]
        public void Catalog_GetCardById_FindsCard()
        {
            var card = CardShowcaseCatalog.GetCardById("CC-01");
            Assert.IsNotNull(card);
            Assert.AreEqual("Avispón", card.Name);
            Assert.AreEqual("champions", card.Category);
            Assert.AreEqual("depredadores", card.Clan);
            Assert.AreEqual(2, card.Cost);
            Assert.AreEqual(3, card.Attack);
            Assert.AreEqual(1, card.Defense);
            Assert.AreEqual("CC_Export/Avispon.png", card.Image);
        }

        [Test]
        public void Catalog_AllCardsHaveNonEmptyProperties()
        {
            foreach (var card in CardShowcaseCatalog.Cards)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.Id), "Card Id must not be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.Name), $"Card {card.Id} Name must not be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.Category), $"Card {card.Id} Category must not be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.Image), $"Card {card.Id} Image must not be empty.");
            }
        }

        [Test]
        public void Catalog_GetCardByDefinition_MatchesAll80Cards()
        {
            Assert.AreEqual(80, CardCatalog.AllCards.Count);
            foreach (var def in CardCatalog.AllCards)
            {
                var showcaseCard = CardShowcaseCatalog.GetCardByDefinition(def);
                Assert.IsNotNull(showcaseCard, $"CardDefinition '{def.Id}' ({def.Name}) must match a ShowcaseCardData.");
                Assert.IsFalse(string.IsNullOrEmpty(showcaseCard.Image), $"Matched card for '{def.Id}' must have an image path.");
            }
        }

        [Test]
        public void CardArtMapper_MapsAll80ShowcaseCards_ToValidTarjetasPath()
        {
            foreach (var card in CardShowcaseCatalog.Cards)
            {
                string path = CardArtMapper.GetFlatArtPath(card);
                Assert.IsFalse(string.IsNullOrEmpty(path), $"Flat art path for {card.Id} ({card.Name}) must not be empty.");
                Assert.IsTrue(path.StartsWith("Tarjetas/"), $"Path '{path}' for {card.Id} must start with 'Tarjetas/'.");
                Assert.IsTrue(path.EndsWith(".png"), $"Path '{path}' for {card.Id} must end with '.png'.");
            }
        }

        [Test]
        public void CardArtMapper_MapsAll20MvpCards_ToValidTarjetasPath()
        {
            foreach (var def in MvpCardCatalog.AllCards)
            {
                string path = CardArtMapper.GetFlatArtPath(def);
                Assert.IsFalse(string.IsNullOrEmpty(path), $"Flat art path for MVP card {def.Id} ({def.Name}) must not be empty.");
                Assert.IsTrue(path.StartsWith("Tarjetas/"), $"Path '{path}' for {def.Id} must start with 'Tarjetas/'.");
                Assert.IsTrue(path.EndsWith(".png"), $"Path '{path}' for {def.Id} must end with '.png'.");
            }
        }

        [Test]
        public void CardArtMapper_CategoryDisplayNames_AndClanAssets_AreCorrect()
        {
            Assert.AreEqual("HEROES", CardArtMapper.GetCategoryDisplayName(CardCategory.Champion));
            Assert.AreEqual("BESTIAS", CardArtMapper.GetCategoryDisplayName(CardCategory.KombatBeast));
            Assert.AreEqual("TROMPOS", CardArtMapper.GetCategoryDisplayName(CardCategory.PhysicalTrompo));
            Assert.AreEqual("ARENAS", CardArtMapper.GetCategoryDisplayName(CardCategory.Arena));

            Assert.AreEqual("HEROES", CardArtMapper.GetCategoryDisplayName("champions"));
            Assert.AreEqual("BESTIAS", CardArtMapper.GetCategoryDisplayName("kombat"));
            Assert.AreEqual("TROMPOS", CardArtMapper.GetCategoryDisplayName("trompos"));
            Assert.AreEqual("ARENAS", CardArtMapper.GetCategoryDisplayName("arenas"));

            Assert.AreEqual("Tarjetas/UI/Milenials.png", CardArtMapper.GetClanBannerPath(ClanType.Milenarios));
            Assert.AreEqual("Tarjetas/UI/Fugitives.png", CardArtMapper.GetClanBannerPath(ClanType.Fugitivos));
            Assert.AreEqual("Tarjetas/UI/Predators.png", CardArtMapper.GetClanBannerPath(ClanType.Depredadores));
            Assert.AreEqual("Tarjetas/UI/Mythicals.png", CardArtMapper.GetClanBannerPath(ClanType.Miticos));

            Assert.AreEqual("Tarjetas/UI/BG_Milenials.png", CardArtMapper.GetClanBgPath(ClanType.Milenarios));
            Assert.AreEqual("Tarjetas/UI/BG_Fugitives.png", CardArtMapper.GetClanBgPath(ClanType.Fugitivos));
            Assert.AreEqual("Tarjetas/UI/BG_Predators.png", CardArtMapper.GetClanBgPath(ClanType.Depredadores));
            Assert.AreEqual("Tarjetas/UI/BG_Mythicals.png", CardArtMapper.GetClanBgPath(ClanType.Miticos));
        }

        [Test]
        public void CardArtMapper_AllMappedFiles_ExistOnDisk()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return; // Skip only if running on a different machine

            string artRoot = Path.Combine(projectRoot, "Assets/_Project/Art");

            static bool FileExistsUnicodeSafe(string fullPath)
            {
                if (File.Exists(fullPath)) return true;
                if (File.Exists(fullPath.Normalize(NormalizationForm.FormC))) return true;
                if (File.Exists(fullPath.Normalize(NormalizationForm.FormD))) return true;

                string dir = Path.GetDirectoryName(fullPath);
                string fileName = Path.GetFileName(fullPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    string targetNfc = fileName.Normalize(NormalizationForm.FormC);
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        if (string.Equals(Path.GetFileName(f).Normalize(NormalizationForm.FormC), targetNfc, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            foreach (var card in CardShowcaseCatalog.Cards)
            {
                string rel = CardArtMapper.GetFlatArtPath(card);
                string full = Path.Combine(artRoot, rel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"Missing flat art file on disk for Showcase card {card.Id} ({card.Name}): {full}");
            }

            foreach (var def in MvpCardCatalog.AllCards)
            {
                string rel = CardArtMapper.GetFlatArtPath(def);
                string full = Path.Combine(artRoot, rel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"Missing flat art file on disk for MVP card {def.Id} ({def.Name}): {full}");
            }

            string[] uiPaths =
            {
                CardArtMapper.GetCostIconPath(),
                CardArtMapper.GetPlecaPath(),
                CardArtMapper.GetClanBannerPath(ClanType.Milenarios),
                CardArtMapper.GetClanBannerPath(ClanType.Fugitivos),
                CardArtMapper.GetClanBannerPath(ClanType.Depredadores),
                CardArtMapper.GetClanBannerPath(ClanType.Miticos),
                CardArtMapper.GetClanBgPath(ClanType.Milenarios),
                CardArtMapper.GetClanBgPath(ClanType.Fugitivos),
                CardArtMapper.GetClanBgPath(ClanType.Depredadores),
                CardArtMapper.GetClanBgPath(ClanType.Miticos)
            };

            foreach (var uiRel in uiPaths)
            {
                string full = Path.Combine(artRoot, uiRel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"Missing UI asset file on disk: {full}");
            }
        }
    }
}

