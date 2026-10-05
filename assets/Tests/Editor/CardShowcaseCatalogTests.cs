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

            string artRoot = Directory.Exists(Path.Combine(projectRoot, "Assets/Resources/Tarjetas"))
                ? Path.Combine(projectRoot, "Assets/Resources")
                : Path.Combine(projectRoot, "Assets/_Project/Art");

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

        [Test]
        public void AudioSystem_AllSfxTypes_HaveCorrespondingWavFilesOnDisk()
        {
            var sfxValues = (SfxType[])Enum.GetValues(typeof(SfxType));
            Assert.GreaterOrEqual(sfxValues.Length, 40, "SfxType enum must define at least 40 distinct sound effect types.");

            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            string sfxRoot = Path.Combine(projectRoot, "Assets/Resources/Audio/SFX");
            Assert.IsTrue(Directory.Exists(sfxRoot), $"SFX directory must exist at {sfxRoot}");

            foreach (var sfx in sfxValues)
            {
                string wavPath = Path.Combine(sfxRoot, $"{sfx}.wav");
                Assert.IsTrue(File.Exists(wavPath), $"Missing placeholder WAV file for SfxType.{sfx} at {wavPath}");
                var fileInfo = new FileInfo(wavPath);
                Assert.Greater(fileInfo.Length, 44, $"WAV file for SfxType.{sfx} must contain valid PCM audio data (> 44 bytes header).");
            }
        }

        [Test]
        public void AudioSystem_AllBgmTypes_HaveCorrespondingWavFilesOnDisk()
        {
            var bgmValues = (BgmType[])Enum.GetValues(typeof(BgmType));
            Assert.GreaterOrEqual(bgmValues.Length, 8, "BgmType enum must define None, Home, Showcase, and the 6 Battle arena tracks.");

            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            string bgmRoot = Path.Combine(projectRoot, "Assets/Resources/Audio/Music");
            Assert.IsTrue(Directory.Exists(bgmRoot), $"Music directory must exist at {bgmRoot}");

            foreach (var bgm in bgmValues)
            {
                if (bgm == BgmType.None || bgm == BgmType.Battle_Pedestal3DActive || bgm == BgmType.Battle_Pedestal3DIdle) continue;
                string wavPath = Path.Combine(bgmRoot, $"{bgm}.wav");
                string mp3Path = Path.Combine(bgmRoot, $"{bgm}.mp3");
                string oggPath = Path.Combine(bgmRoot, $"{bgm}.ogg");
                string filePath = File.Exists(wavPath) ? wavPath : (File.Exists(mp3Path) ? mp3Path : oggPath);
                Assert.IsTrue(File.Exists(filePath), $"Missing music track file for BgmType.{bgm} at {bgmRoot}");
                var fileInfo = new FileInfo(filePath);
                Assert.Greater(fileInfo.Length, 44, $"Music file for BgmType.{bgm} must contain valid audio data (> 44 bytes).");
            }
        }

        [Test]
        public void PlayerProfile_AudioFlags_DefaultToTrue()
        {
            var profile = new PlayerProfile();
            Assert.IsTrue(profile.SfxEnabled, "PlayerProfile.SfxEnabled should default to true.");
            Assert.IsTrue(profile.MusicEnabled, "PlayerProfile.MusicEnabled should default to true.");

            profile.SfxEnabled = false;
            profile.MusicEnabled = false;
            Assert.IsFalse(profile.SfxEnabled);
            Assert.IsFalse(profile.MusicEnabled);
        }

        [Test]
        public void AllCategories_ShareCostAttackAndHealthStats_AndCombatBehavior()
        {
            // 1. All 80 cards in CardCatalog (Heroes, Beasts, Trompos, Arenas) must have Cost > 0, BaseAttack > 0, BaseHealth > 0, and IsCreature == true
            foreach (var def in CardCatalog.AllCards)
            {
                Assert.IsTrue(def.IsCreature, $"Card '{def.Id}' ({def.Category}) must have IsCreature == true.");
                Assert.Greater(def.EnergyCost, 0, $"CardCatalog '{def.Id}' ({def.Name}) must have EnergyCost > 0.");
                Assert.Greater(def.BaseAttack, 0, $"CardCatalog '{def.Id}' ({def.Name}) must have BaseAttack > 0.");
                Assert.Greater(def.BaseHealth, 0, $"CardCatalog '{def.Id}' ({def.Name}) must have BaseHealth > 0.");
            }

            // 2. All 80 cards in CardShowcaseCatalog must have Cost > 0, Attack > 0, Defense > 0
            foreach (var card in CardShowcaseCatalog.Cards)
            {
                Assert.Greater(card.Cost, 0, $"ShowcaseCardData '{card.Id}' ({card.Name}) must have Cost > 0.");
                Assert.Greater(card.Attack, 0, $"ShowcaseCardData '{card.Id}' ({card.Name}) must have Attack > 0.");
                Assert.Greater(card.Defense, 0, $"ShowcaseCardData '{card.Id}' ({card.Name}) must have Defense > 0.");
            }

            // 3. All 20 cards in MvpCardCatalog must have EnergyCost > 0, BaseAttack > 0, BaseHealth > 0
            foreach (var mvp in MvpCardCatalog.AllCards)
            {
                Assert.IsTrue(mvp.IsCreature, $"MvpCardCatalog '{mvp.Id}' ({mvp.Category}) must have IsCreature == true.");
                Assert.Greater(mvp.EnergyCost, 0, $"MvpCardCatalog '{mvp.Id}' ({mvp.Name}) must have EnergyCost > 0.");
                Assert.Greater(mvp.BaseAttack, 0, $"MvpCardCatalog '{mvp.Id}' ({mvp.Name}) must have BaseAttack > 0.");
                Assert.Greater(mvp.BaseHealth, 0, $"MvpCardCatalog '{mvp.Id}' ({mvp.Name}) must have BaseHealth > 0.");
            }

            // 4. Verify Trompos and Arenas behave identically to Champions and Beasts in ArenaPowerCalculator (Clan advantage +1 & Arena affinity +1)
            var neutralLocation = new CardDefinition("LOC_NEUTRAL", "Neutral Location", CardCategory.Arena, ClanType.Neutral, 0, 0, 0);
            var milenariosLocation = new CardDefinition("LOC_MIL", "Santuario Milenario", CardCategory.Arena, ClanType.Milenarios, 0, 0, 0);
            var fugitivoEnemy = new CardDefinition("ENEMY_FUG", "Fugitivo Rival", CardCategory.Champion, ClanType.Fugitivos, 2, 3, 3);

            var trompoAzteca = CardCatalog.GetCardById("TROMPO_AZTECA")!;
            var arenaValle = CardCatalog.GetCardById("ARENA_VALLE_PRUEBAS")!;

            // Trompo Azteca (Milenarios, ATK 4) vs Fugitivos (+1 clan advantage) in Milenarios location (+1 affinity) = 6
            int trompoPower = CometaChampions.Core.Rules.ArenaPowerCalculator.CalculateCardEffectivePower(
                trompoAzteca, milenariosLocation, new[] { fugitivoEnemy });
            Assert.AreEqual(trompoAzteca.BaseAttack + 2, trompoPower, "Trompo must receive +1 Clan advantage and +1 Arena affinity based on its BaseAttack and Clan.");

            // Arena Valle de Pruebas (Milenarios, ATK 2) vs Fugitivos (+1 clan advantage) in Milenarios location (+1 affinity) = 4
            int arenaPower = CometaChampions.Core.Rules.ArenaPowerCalculator.CalculateCardEffectivePower(
                arenaValle, milenariosLocation, new[] { fugitivoEnemy });
            Assert.AreEqual(arenaValle.BaseAttack + 2, arenaPower, "Arena card played in lane must receive +1 Clan advantage and +1 Arena affinity based on its BaseAttack and Clan.");
        }

        [Test]
        public void Home_MisCartasGrid_ConfiguredForTwoCardsPerRow()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            string homeScenePath = Path.Combine(projectRoot, "Assets/_Project/Scenes/Home.unity");
            Assert.IsTrue(File.Exists(homeScenePath), "Home.unity scene must exist.");
            string homeSceneContent = File.ReadAllText(homeScenePath);

            Assert.IsTrue(homeSceneContent.Contains("m_ConstraintCount: 2"), "Home.unity grid must be constrained to 2 columns.");
            Assert.IsTrue(homeSceneContent.Contains("m_CellSize: {x: 236, y: 330}"), "Home.unity grid must have enlarged cell size {x: 236, y: 330}.");

            string prefabPath = Path.Combine(projectRoot, "Assets/_Project/Prefabs/CardGridItem.prefab");
            Assert.IsTrue(File.Exists(prefabPath), "CardGridItem.prefab must exist.");
            string prefabContent = File.ReadAllText(prefabPath);
            Assert.IsTrue(prefabContent.Contains("m_SizeDelta: {x: 236, y: 330}"), "CardGridItem.prefab must have root size 236x330.");
            Assert.IsTrue(prefabContent.Contains("m_SizeDelta: {x: 56, y: 56}"), "CardGridItem.prefab lock icon must be enlarged to 56x56.");
        }

        [Test]
        public void DynamicCardAssembler_Main3DCard_UsesOswaldBold_AndOptimizedLayout()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            // 1. Verify Oswald Bold SDF font asset exists on disk
            string fontAssetPath = Path.Combine(projectRoot, "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset");
            Assert.IsTrue(File.Exists(fontAssetPath), "Oswald Bold SDF font asset must exist at: " + fontAssetPath);

            string fontMetaPath = fontAssetPath + ".meta";
            Assert.IsTrue(File.Exists(fontMetaPath), "Oswald Bold SDF font meta must exist.");

            // 2. Verify DynamicCardAssembler implementation
            string assemblerPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Cards/DynamicCardAssembler.cs");
            Assert.IsTrue(File.Exists(assemblerPath), "DynamicCardAssembler.cs must exist.");
            string assemblerContent = File.ReadAllText(assemblerPath);

            Assert.IsTrue(assemblerContent.Contains("GetOswaldBoldFont()"), "DynamicCardAssembler must declare GetOswaldBoldFont().");
            Assert.IsTrue(assemblerContent.Contains("GetBodyFont(bool isMain3DCard"), "DynamicCardAssembler must declare GetBodyFont(bool isMain3DCard).");
            Assert.IsTrue(assemblerContent.Contains("return GetOswaldBoldFont();"), "DynamicCardAssembler must return Oswald Bold SDF when isMain3DCard is true.");
            Assert.IsTrue(assemblerContent.Contains("804f, ReferenceWidth, 886f"), "DynamicCardAssembler must expand description row bounds to 804..886 (82px) for main 3D card.");
            Assert.IsTrue(assemblerContent.Contains("892f, ReferenceWidth, 974f"), "DynamicCardAssembler must expand quote row bounds to 892..974 (82px) for main 3D card.");
            Assert.IsTrue(assemblerContent.Contains("descText.lineSpacing = 4f"), "DynamicCardAssembler must set lineSpacing to 4f for main 3D card description.");
            Assert.IsTrue(assemblerContent.Contains("descText.fontSizeMin = 18f"), "DynamicCardAssembler must set fontSizeMin to 18f for main 3D card description.");
            Assert.IsTrue(assemblerContent.Contains("quoteText.lineSpacing = 4f"), "DynamicCardAssembler must set lineSpacing to 4f for main 3D card quote.");
            Assert.IsTrue(assemblerContent.Contains("quoteText.fontSizeMin = 18f"), "DynamicCardAssembler must set fontSizeMin to 18f for main 3D card quote.");

            // 3. Verify Card3D passes isMain3DCard: true
            string card3DPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Cards/Card3D.cs");
            Assert.IsTrue(File.Exists(card3DPath), "Card3D.cs must exist.");
            string card3DContent = File.ReadAllText(card3DPath);
            Assert.IsTrue(card3DContent.Contains("isMain3DCard: true"), "Card3D.cs must pass isMain3DCard: true to DynamicCardAssembler.");
        }

        [Test]
        public void DynamicCardAssembler_UniversalCardRendering_AppliedAcrossHomeAndBattle()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            string assemblerPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Cards/DynamicCardAssembler.cs");
            Assert.IsTrue(File.Exists(assemblerPath), "DynamicCardAssembler.cs must exist.");
            string assemblerContent = File.ReadAllText(assemblerPath);

            // Default parameter isMain3DCard = true everywhere so all cards adopt Oswald Bold and optimized layout
            Assert.IsTrue(assemblerContent.Contains("bool isMain3DCard = true)"), "DynamicCardAssembler must default isMain3DCard to true across its methods.");
            Assert.IsTrue(assemblerContent.Contains("public static TMP_FontAsset GetBodyFont(bool isMain3DCard = true)"), "GetBodyFont must default isMain3DCard to true.");
            Assert.IsTrue(assemblerContent.Contains("public static Texture2D RenderCardToTexture(ShowcaseCardData cardData, bool isMain3DCard = true)"), "RenderCardToTexture must default isMain3DCard to true.");
            Assert.IsTrue(assemblerContent.Contains("public static Sprite RenderCardToSprite(ShowcaseCardData cardData, bool isMain3DCard = true)"), "RenderCardToSprite must default isMain3DCard to true.");

            // Verify Battle card presentations invoke DynamicCardAssembler
            string handViewPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Battle/BattleHandCardView.cs");
            Assert.IsTrue(File.Exists(handViewPath) && File.ReadAllText(handViewPath).Contains("DynamicCardAssembler.AssembleCard"), "BattleHandCardView must assemble cards via DynamicCardAssembler.");

            string slotViewPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Battle/BattleCardSlotView.cs");
            Assert.IsTrue(File.Exists(slotViewPath) && File.ReadAllText(slotViewPath).Contains("DynamicCardAssembler.AssembleCard"), "BattleCardSlotView must assemble cards via DynamicCardAssembler.");

            string mulliganPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Battle/BattleMulliganModal.cs");
            Assert.IsTrue(File.Exists(mulliganPath) && File.ReadAllText(mulliganPath).Contains("DynamicCardAssembler.AssembleCard"), "BattleMulliganModal must assemble cards via DynamicCardAssembler.");
            Assert.IsTrue(File.ReadAllText(mulliganPath).Contains("GetOswaldBoldFont()"), "BattleMulliganModal must use Oswald Bold font for card text.");

            // Verify Home card presentations invoke DynamicCardAssembler
            string gridItemPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/UI/CardGridItem.cs");
            Assert.IsTrue(File.Exists(gridItemPath) && File.ReadAllText(gridItemPath).Contains("DynamicCardAssembler.AssembleCard"), "CardGridItem must assemble cards via DynamicCardAssembler.");

            string deckModalPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/UI/DeckManagerModal.cs");
            Assert.IsTrue(File.Exists(deckModalPath) && File.ReadAllText(deckModalPath).Contains("DynamicCardAssembler.AssembleCard"), "DeckManagerModal must assemble cards via DynamicCardAssembler.");

            string packModalPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/UI/PackOpeningModal.cs");
            Assert.IsTrue(File.Exists(packModalPath) && File.ReadAllText(packModalPath).Contains("DynamicCardAssembler.AssembleCard"), "PackOpeningModal must assemble cards via DynamicCardAssembler.");
        }

        [Test]
        public void DynamicCardAssembler_AtkBadge_UsesSwordIcon_AndDefBadgeIsHidden()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            // 1. Verify espada2.png asset exists
            string swordAssetPath = Path.Combine(projectRoot, "Assets/_Project/Art/Home/espada2.png");
            Assert.IsTrue(File.Exists(swordAssetPath), "espada2.png asset must exist at: " + swordAssetPath);

            string swordMetaPath = swordAssetPath + ".meta";
            Assert.IsTrue(File.Exists(swordMetaPath), "espada2.png.meta must exist.");

            // 2. Verify CardArtMapper method
            Assert.AreEqual("Home/espada2.png", CardArtMapper.GetSwordIconPath(), "CardArtMapper.GetSwordIconPath() must return 'Home/espada2.png'.");

            // 3. Verify DynamicCardAssembler implementation
            string assemblerPath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Cards/DynamicCardAssembler.cs");
            Assert.IsTrue(File.Exists(assemblerPath), "DynamicCardAssembler.cs must exist.");
            string assemblerContent = File.ReadAllText(assemblerPath);

            Assert.IsTrue(assemblerContent.Contains("CardArtMapper.GetSwordIconPath()"), "DynamicCardAssembler must use CardArtMapper.GetSwordIconPath().");
            Assert.IsTrue(assemblerContent.Contains("616f, 625f, 736f, 757f"), "AtkBadge must be positioned to the right (X: 616..736) and enlarged by 20%.");
            Assert.IsTrue(assemblerContent.Contains("65f"), "AtkText must have font size increased by 20% (65f).");
            Assert.IsTrue(assemblerContent.Contains("atkOuterImg.sprite = swordSprite"), "AtkBadge outer image must use swordSprite.");
            Assert.IsTrue(assemblerContent.Contains("atkInnerImg.sprite = swordSprite"), "AtkBadge inner image must use swordSprite.");
            Assert.IsTrue(assemblerContent.Contains("atkInnerImg.color = clanColor"), "AtkBadge inner image must use clanColor.");
            Assert.IsTrue(assemblerContent.Contains("defBadgeGo.SetActive(false)"), "DefBadge must be hidden via SetActive(false).");
        }
    }
}



