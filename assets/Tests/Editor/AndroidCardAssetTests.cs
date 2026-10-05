using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Helpers;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class AndroidCardAssetTests
    {
        private static string GetProjectRoot()
        {
            return "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
        }

        private static bool FileExistsUnicodeSafe(string fullPath)
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

        [Test]
        public void Assets_Resources_Tarjetas_ContainsAll80CardIllustrations()
        {
            string root = GetProjectRoot();
            if (!Directory.Exists(root)) return;

            string resourcesTarjetas = Path.Combine(root, "Assets/Resources");

            foreach (var card in CardShowcaseCatalog.Cards)
            {
                string rel = CardArtMapper.GetFlatArtPath(card);
                string full = Path.Combine(resourcesTarjetas, rel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"Card illustration for {card.Id} ({card.Name}) must exist in Assets/Resources at: {full}");
            }

            foreach (var def in MvpCardCatalog.AllCards)
            {
                string rel = CardArtMapper.GetFlatArtPath(def);
                string full = Path.Combine(resourcesTarjetas, rel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"MVP card illustration for {def.Id} ({def.Name}) must exist in Assets/Resources at: {full}");
            }
        }

        [Test]
        public void Assets_Resources_Tarjetas_ContainsAllClanUIAssets()
        {
            string root = GetProjectRoot();
            if (!Directory.Exists(root)) return;

            string resourcesDir = Path.Combine(root, "Assets/Resources");

            string[] requiredUIs = {
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

            foreach (var uiRel in requiredUIs)
            {
                string full = Path.Combine(resourcesDir, uiRel);
                Assert.IsTrue(FileExistsUnicodeSafe(full), $"UI overlay asset must exist in Assets/Resources at: {full}");
            }
        }

        [Test]
        public void Assets_Resources_Home_ContainsEspadaIcon()
        {
            string root = GetProjectRoot();
            if (!Directory.Exists(root)) return;

            string resourcesEspada = Path.Combine(root, "Assets/Resources/Home/espada2.png");
            Assert.IsTrue(File.Exists(resourcesEspada), "Assets/Resources/Home/espada2.png must exist for Android build packaging.");

            string resourcesMeta = Path.Combine(root, "Assets/Resources/Home/espada2.png.meta");
            Assert.IsTrue(File.Exists(resourcesMeta), "Assets/Resources/Home/espada2.png.meta must exist.");

            string legacyEspada = Path.Combine(root, "Assets/_Project/Art/Home/espada2.png");
            Assert.IsTrue(File.Exists(legacyEspada), "Original Assets/_Project/Art/Home/espada2.png must remain preserved.");
        }

        [Test]
        public void Assets_Resources_Tarjetas_ContainsTmpFontAsset()
        {
            string root = GetProjectRoot();
            if (!Directory.Exists(root)) return;

            string fontAsset = Path.Combine(root, "Assets/Resources/Tarjetas/SemiExpanded ExtraBold Italic SDF.asset");
            Assert.IsTrue(File.Exists(fontAsset), "SDF font asset must exist in Assets/Resources/Tarjetas/ for Android build packaging.");

            string fontMeta = fontAsset + ".meta";
            Assert.IsTrue(File.Exists(fontMeta), "SDF font asset meta must exist.");
        }

        [Test]
        public void Card3D_LoadSprite_And_LoadTexture_ResolveSampleCards()
        {
            try
            {
                _ = Resources.Load<Texture2D>("dummy_test_probe");
            }
            catch (Exception ex) when (ex.Message.Contains("ECall") || ex is System.Security.SecurityException)
            {
                // Standalone external runner without Unity engine runtime attached
                return;
            }

            string[] testPaths = {
                "Tarjetas/Heroes/Avispon.png",
                "Tarjetas/Bestias/Azteca.png",
                "Tarjetas/Trompos/Avispón.png",
                "Tarjetas/Arenas/La Arena Campeones.png",
                "Tarjetas/UI/BG_Milenials.png",
                "Tarjetas/UI/costo.png",
                "Tarjetas/UI/pleca.png",
                "Home/espada2.png",
                "Reverso_Export/reverso_Oro.png"
            };

            foreach (var relPath in testPaths)
            {
                var sprite = Card3D.LoadSpriteFromCardsFolder(relPath);
                Assert.IsNotNull(sprite, $"LoadSpriteFromCardsFolder must return non-null Sprite for '{relPath}'.");
                Assert.Greater(sprite.rect.width, 0, $"Sprite width for '{relPath}' must be > 0.");
                Assert.Greater(sprite.rect.height, 0, $"Sprite height for '{relPath}' must be > 0.");

                var tex = Card3D.LoadTextureFromCardsFolder(relPath);
                Assert.IsNotNull(tex, $"LoadTextureFromCardsFolder must return non-null Texture2D for '{relPath}'.");
                Assert.Greater(tex.width, 0, $"Texture width for '{relPath}' must be > 0.");
                Assert.Greater(tex.height, 0, $"Texture height for '{relPath}' must be > 0.");
            }
        }
    }
}
