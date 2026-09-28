using System;
using System.Linq;
using NUnit.Framework;
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
    }
}
