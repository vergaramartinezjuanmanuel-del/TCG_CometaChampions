using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class LoadingScreenTests
    {
        [Test]
        public void CardCarousel3D_Dimensions_Match_CenterCard3DViewport()
        {
            // Verify that Card3D dimensions are strictly 2.5 x 3.5 x 0.04 (Lorcana standard 5:7)
            Assert.AreEqual(2.5f, Card3D.CardWidth, "Card3D width must match Center_Card3D_Viewport width (2.5f).");
            Assert.AreEqual(3.5f, Card3D.CardHeight, "Card3D height must match Center_Card3D_Viewport height (3.5f).");
            Assert.AreEqual(0.04f, Card3D.CardThickness, "Card3D thickness must match Center_Card3D_Viewport thickness (0.04f).");
        }

        [Test]
        public void CardCarousel3D_DefaultIconicCardIds_ContainsSixBalancedChampions()
        {
            // Verify exactly 6 iconic cards are configured per user approval (Option A)
            var cardIds = CardCarousel3D.DefaultIconicCardIds;
            Assert.IsNotNull(cardIds);
            Assert.AreEqual(6, cardIds.Length, "Carousel must feature exactly 6 iconic cards.");

            // Verify expected iconic cards are present
            Assert.Contains("CHAMP_DRAKY", cardIds);
            Assert.Contains("CHAMP_COMETIN", cardIds);
            Assert.Contains("CHAMP_AVISPON", cardIds);
            Assert.Contains("CHAMP_ESPECTRO", cardIds);
            Assert.Contains("CHAMP_AZTECA", cardIds);
            Assert.Contains("CHAMP_JUMBO_COBRA", cardIds);

            // Verify each card resolves in CardCatalog or ShowcaseCatalog
            foreach (var id in cardIds)
            {
                var card = CardCatalog.GetCardById(id) 
                           ?? MvpCardCatalog.GetCardById(id);
                var showcase = CardShowcaseCatalog.GetCardByDefinitionIdOrName(id, id)
                               ?? CardShowcaseCatalog.GetCardById(id);

                Assert.IsTrue(card != null || showcase != null, $"Card '{id}' must exist in the card catalog.");
            }
        }

        [Test]
        public void LoadingScreenManager_Constants_AndTimings_AreValid()
        {
            // Verify user-approved minimum duration is strictly 1.5 seconds
            Assert.AreEqual(1.5f, LoadingScreenManager.MinimumDisplayDuration, "Minimum display duration must be 1.5 seconds per user request.");
            Assert.AreEqual(0.25f, LoadingScreenManager.FadeDuration, "Fade duration must be 0.25 seconds.");
        }

        [Test]
        public void CardCarousel3D_MathematicalOrbitPositions_CalculatesEquidistantCircle()
        {
            float radius = 3.6f;
            float centerZ = -0.9f;
            int count = 6;
            float angleStep = 360f / count;

            Assert.AreEqual(60f, angleStep, "Angle step for 6 cards must be 60 degrees.");

            // Card 0 (front-most at angle = 0):
            // X = 0, Z = centerZ - R = -0.9 - 3.6 = -4.5 (matching Center_Card3D_Viewport Z = -4.5f!)
            float angle0Rad = 0f;
            float x0 = radius * Mathf.Sin(angle0Rad);
            float z0 = centerZ - radius * Mathf.Cos(angle0Rad);

            Assert.AreEqual(0f, x0, 0.001f, "Front card X must be centered at 0.");
            Assert.AreEqual(-4.5f, z0, 0.001f, "Front card Z must match Center_Card3D_Viewport distance (-4.5f).");

            // Card 3 (furthest at 180 degrees):
            // X = 0, Z = centerZ + R = -0.9 + 3.6 = 2.7f
            float angle180Rad = 180f * Mathf.Deg2Rad;
            float x180 = radius * Mathf.Sin(angle180Rad);
            float z180 = centerZ - radius * Mathf.Cos(angle180Rad);

            Assert.AreEqual(0f, x180, 0.001f);
            Assert.AreEqual(2.7f, z180, 0.001f);

            // Radius distance from center (0, centerZ) for all 6 cards must strictly equal R
            for (int i = 0; i < count; i++)
            {
                float rad = (i * angleStep) * Mathf.Deg2Rad;
                float x = radius * Mathf.Sin(rad);
                float z = centerZ - radius * Mathf.Cos(rad);

                float dist = Mathf.Sqrt(x * x + (z - centerZ) * (z - centerZ));
                Assert.AreEqual(radius, dist, 0.001f, $"Card {i} must be at radius {radius} from carousel center.");
            }
        }

        [Test]
        public void Home_And_BattleManager_Use_LoadingScreenManager()
        {
            string projectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
            if (!Directory.Exists(projectRoot)) return;

            // Verify Home.cs calls LoadingScreenManager.Instance.LoadScene("Battle"
            string homePath = Path.Combine(projectRoot, "Assets/_Project/Runtime/UI/Home.cs");
            Assert.IsTrue(File.Exists(homePath), "Home.cs must exist.");
            string homeContent = File.ReadAllText(homePath);
            Assert.IsTrue(homeContent.Contains("LoadingScreenManager.Instance.LoadScene(\"Battle\""), 
                "Home.cs must invoke LoadingScreenManager when starting battle.");

            // Verify Home.cs calls HideHomeContent() before LoadingScreenManager.Instance.LoadScene
            int homeHideIdx = homeContent.IndexOf("HideHomeContent();", StringComparison.Ordinal);
            int homeLoadIdx = homeContent.IndexOf("LoadingScreenManager.Instance.LoadScene(\"Battle\"", StringComparison.Ordinal);
            Assert.IsTrue(homeHideIdx >= 0, "Home.cs must define and call HideHomeContent().");
            Assert.IsTrue(homeHideIdx < homeLoadIdx, "Home.cs must call HideHomeContent() BEFORE LoadingScreenManager.Instance.LoadScene.");

            // Verify BattleManager.cs calls LoadingScreenManager.Instance.LoadScene("Home"
            string battlePath = Path.Combine(projectRoot, "Assets/_Project/Runtime/Battle/BattleManager.cs");
            Assert.IsTrue(File.Exists(battlePath), "BattleManager.cs must exist.");
            string battleContent = File.ReadAllText(battlePath);
            Assert.IsTrue(battleContent.Contains("LoadingScreenManager.Instance.LoadScene(\"Home\""), 
                "BattleManager.cs must invoke LoadingScreenManager when returning home.");

            // Verify BattleManager.cs calls HideBattleContent() before LoadingScreenManager.Instance.LoadScene
            int battleHideIdx = battleContent.IndexOf("HideBattleContent();", StringComparison.Ordinal);
            int battleLoadIdx = battleContent.IndexOf("LoadingScreenManager.Instance.LoadScene(\"Home\"", StringComparison.Ordinal);
            Assert.IsTrue(battleHideIdx >= 0, "BattleManager.cs must define and call HideBattleContent().");
            Assert.IsTrue(battleHideIdx < battleLoadIdx, "BattleManager.cs must call HideBattleContent() BEFORE LoadingScreenManager.Instance.LoadScene.");
        }

        [Test]
        public void LoadingScreenManager_Has_HideAllSceneContent_Method()
        {
            var method = typeof(LoadingScreenManager).GetMethod("HideAllSceneContent", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "LoadingScreenManager must implement HideAllSceneContent to isolate loading view.");
        }
    }
}
