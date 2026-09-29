#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class ArenaBackgroundTests
    {
        [Test]
        public void OfficialCatalog_ContainsExactlySixBackgrounds()
        {
            var all = ArenaBackgroundCatalog.GetAllBackgrounds();
            Assert.That(all.Count, Is.EqualTo(6));

            var names = ArenaBackgroundCatalog.GetAllBackgroundNames();
            Assert.That(names.Count, Is.EqualTo(6));

            CollectionAssert.Contains(names, ArenaBackgroundCatalog.TemploMilenario);
            CollectionAssert.Contains(names, ArenaBackgroundCatalog.PistaDraconia);
            CollectionAssert.Contains(names, ArenaBackgroundCatalog.LlanurasTirano);
            CollectionAssert.Contains(names, ArenaBackgroundCatalog.NebulosaCosmica);
            CollectionAssert.Contains(names, ArenaBackgroundCatalog.Pedestal3DActive);
            CollectionAssert.Contains(names, ArenaBackgroundCatalog.Pedestal3DIdle);
        }

        [Test]
        public void OfficialCatalog_AllEntries_HaveValidMetadata()
        {
            foreach (var bg in ArenaBackgroundCatalog.GetAllBackgrounds())
            {
                Assert.That(string.IsNullOrEmpty(bg.Id), Is.False, "Id must not be empty");
                Assert.That(string.IsNullOrEmpty(bg.DisplayName), Is.False, "DisplayName must not be empty");
                Assert.That(string.IsNullOrEmpty(bg.Description), Is.False, "Description must not be empty");
                Assert.That(bg.FileName, Is.EqualTo(bg.Id + ".jpg"), "FileName must match Id + .jpg");
            }
        }

        [Test]
        public void OfficialCatalog_AffiliatedClans_MatchExpectedLore()
        {
            var templo = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.TemploMilenario);
            Assert.That(templo, Is.Not.Null);
            Assert.That(templo!.AffiliatedClan, Is.EqualTo(ClanType.Milenarios));
            Assert.That(templo.DisplayName, Is.EqualTo("Templo Milenario"));

            var pista = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.PistaDraconia);
            Assert.That(pista, Is.Not.Null);
            Assert.That(pista!.AffiliatedClan, Is.EqualTo(ClanType.Fugitivos));
            Assert.That(pista.DisplayName, Is.EqualTo("Pista Draconia"));

            var llanuras = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.LlanurasTirano);
            Assert.That(llanuras, Is.Not.Null);
            Assert.That(llanuras!.AffiliatedClan, Is.EqualTo(ClanType.Depredadores));
            Assert.That(llanuras.DisplayName, Is.EqualTo("Llanuras del Tirano"));

            var nebulosa = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.NebulosaCosmica);
            Assert.That(nebulosa, Is.Not.Null);
            Assert.That(nebulosa!.AffiliatedClan, Is.EqualTo(ClanType.Miticos));
            Assert.That(nebulosa.DisplayName, Is.EqualTo("Nebulosa Cósmica"));

            var pedActive = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.Pedestal3DActive);
            Assert.That(pedActive, Is.Not.Null);
            Assert.That(pedActive!.AffiliatedClan, Is.Null);

            var pedIdle = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.Pedestal3DIdle);
            Assert.That(pedIdle, Is.Not.Null);
            Assert.That(pedIdle!.AffiliatedClan, Is.Null);
        }

        [Test]
        public void IsValidBackground_ReturnsTrueForOfficial_AndFalseForUnknown()
        {
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.TemploMilenario), Is.True);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.PistaDraconia), Is.True);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.LlanurasTirano), Is.True);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.NebulosaCosmica), Is.True);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.Pedestal3DActive), Is.True);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.Pedestal3DIdle), Is.True);

            Assert.That(ArenaBackgroundCatalog.IsValidBackground("bg_arena_desconocida"), Is.False);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(""), Is.False);
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(null!), Is.False);
        }

        [Test]
        public void GetRandomBackgroundName_AlwaysReturnsValidOfficialBackground()
        {
            var validNames = new HashSet<string>(ArenaBackgroundCatalog.GetAllBackgroundNames());
            var rng = new Random(12345);

            for (int i = 0; i < 100; i++)
            {
                string randomName = ArenaBackgroundCatalog.GetRandomBackgroundName(rng);
                Assert.That(validNames.Contains(randomName), Is.True, $"Random background '{randomName}' was not in valid list");
            }
        }

        [Test]
        public void GetRandomBackgroundName_WithSeed_IsDeterministic()
        {
            var rng1 = new Random(42);
            var rng2 = new Random(42);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(
                    ArenaBackgroundCatalog.GetRandomBackgroundName(rng1),
                    Is.EqualTo(ArenaBackgroundCatalog.GetRandomBackgroundName(rng2))
                );
            }
        }

        [Test]
        public void GetDisplayName_ReturnsFriendlyName_ForOfficialIds()
        {
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.TemploMilenario), Is.EqualTo("Templo Milenario"));
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.PistaDraconia), Is.EqualTo("Pista Draconia"));
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.LlanurasTirano), Is.EqualTo("Llanuras del Tirano"));
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.NebulosaCosmica), Is.EqualTo("Nebulosa Cósmica"));
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.Pedestal3DActive), Is.EqualTo("Pedestal 3D (Activo)"));
            Assert.That(ArenaBackgroundCatalog.GetDisplayName(ArenaBackgroundCatalog.Pedestal3DIdle), Is.EqualTo("Pedestal 3D (Reposo)"));
        }

        [Test]
        public void DefaultHomeBackground_IsPedestal3DActive()
        {
            Assert.That(ArenaBackgroundCatalog.IsValidBackground(ArenaBackgroundCatalog.Pedestal3DActive), Is.True);
            Assert.That(ArenaBackgroundCatalog.Pedestal3DActive, Is.EqualTo("bg_pedestal_3d_active"));

            var bg = ArenaBackgroundCatalog.GetBackgroundById(ArenaBackgroundCatalog.Pedestal3DActive);
            Assert.That(bg, Is.Not.Null);
            Assert.That(bg!.DisplayName, Is.EqualTo("Pedestal 3D (Activo)"));
            Assert.That(bg.FileName, Is.EqualTo("bg_pedestal_3d_active.jpg"));
        }
    }
}
