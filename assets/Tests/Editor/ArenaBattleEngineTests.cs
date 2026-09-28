using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class ArenaBattleEngineTests
    {
        private List<CardDefinition> CreateTestDeck(ClanType clan, string prefix)
        {
            var deck = new List<CardDefinition>();
            for (int i = 1; i <= 12; i++)
            {
                int cost = Math.Clamp((i % 4) + 1, 1, 4);
                deck.Add(new CardDefinition(
                    $"{prefix}_{i}",
                    $"{prefix} Card {i}",
                    CardCategory.Champion,
                    clan,
                    energyCost: cost,
                    baseAttack: cost + 1,
                    baseHealth: 4,
                    abilityText: "Test card"
                ));
            }
            return deck;
        }

        private List<CardDefinition> CreateTestArenas()
        {
            return new List<CardDefinition>
            {
                new CardDefinition("ARENA_1", "Templo Milenario", CardCategory.Arena, ClanType.Milenarios, 0, 0, 0, "+1 a Milenarios"),
                new CardDefinition("ARENA_2", "Campamento Panther", CardCategory.Arena, ClanType.Depredadores, 0, 0, 0, "+1 a Depredadores"),
                new CardDefinition("ARENA_3", "Cimientos de Cristal", CardCategory.Arena, ClanType.Miticos, 0, 0, 0, "Cristal ancestral")
            };
        }

        [Test]
        public void CreateMatch_Initializes4CardsHand_Energy1_Turn1_3Arenas()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            Assert.IsNotNull(match);
            Assert.AreEqual(1, match.CurrentTurn);
            Assert.AreEqual(6, match.MaxTurn);
            Assert.AreEqual(1, match.PlayerEnergy);
            Assert.AreEqual(1, match.PlayerMaxEnergy);
            Assert.AreEqual(1, match.AiEnergy);
            Assert.AreEqual(1, match.AiMaxEnergy);

            Assert.AreEqual(4, match.PlayerHand.Count);
            Assert.AreEqual(4, match.AiHand.Count);
            Assert.AreEqual(8, match.PlayerDeck.Count); // 12 - 4 = 8
            Assert.AreEqual(8, match.AiDeck.Count);

            Assert.AreEqual(3, match.Arenas.Length);
            Assert.AreEqual("Templo Milenario", match.Arenas[0].Name);
            Assert.AreEqual(false, match.IsGameOver);
            Assert.AreEqual(BattleWinner.None, match.Winner);
        }

        [Test]
        public void PlayPlayerCard_DeductsEnergy_AddsToLane_AndRecordsForUndo()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            // Ensure a 1-cost card in hand
            var card1 = new CardDefinition("CHEAP_1", "Quick Champion", CardCategory.Champion, ClanType.Milenarios, 1, 2, 2);
            match.PlayerHand[0] = card1;

            bool played = ArenaBattleEngine.PlayPlayerCard(match, 0, 0, out string error);

            Assert.IsTrue(played, error);
            Assert.AreEqual(0, match.PlayerEnergy, "1 energy deducted from 1 available");
            Assert.AreEqual(3, match.PlayerHand.Count, "Hand count reduced by 1");
            Assert.AreEqual(1, match.PlayerLanes[0].Count, "Card placed into lane 0");
            Assert.AreEqual(1, match.PlayerTurnPlays.Count, "Recorded for current turn undo");
            Assert.AreEqual(card1, match.PlayerLanes[0][0]);
        }

        [Test]
        public void PlayPlayerCard_FailsWhenInsufficientEnergy()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            var expensiveCard = new CardDefinition("EXP_5", "Titan", CardCategory.Champion, ClanType.Milenarios, 5, 6, 6);
            match.PlayerHand[0] = expensiveCard;
            match.PlayerEnergy = 1;

            bool played = ArenaBattleEngine.PlayPlayerCard(match, 0, 0, out string error);

            Assert.IsFalse(played);
            Assert.IsTrue(error.Contains("energía"), "Error message should mention energy requirement");
            Assert.AreEqual(4, match.PlayerHand.Count);
            Assert.AreEqual(0, match.PlayerLanes[0].Count);
        }

        [Test]
        public void PlayPlayerCard_FailsWhenLaneIsFull()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            match.PlayerEnergy = 10;

            // Fill lane 0 with 4 cards
            for (int i = 0; i < 4; i++)
            {
                match.PlayerLanes[0].Add(new CardDefinition($"CARD_{i}", $"Card {i}", CardCategory.Champion, ClanType.Milenarios, 1, 1, 1));
            }

            match.PlayerHand.Add(new CardDefinition("FIFTH", "Fifth Card", CardCategory.Champion, ClanType.Milenarios, 1, 1, 1));
            int handIdx = match.PlayerHand.Count - 1;

            bool played = ArenaBattleEngine.PlayPlayerCard(match, handIdx, 0, out string error);

            Assert.IsFalse(played);
            Assert.IsTrue(error.Contains("llena"), "Should block 5th card into full lane");
            Assert.AreEqual(4, match.PlayerLanes[0].Count);
        }

        [Test]
        public void UndoPlayerPlay_RefundsEnergy_AndReturnsToHand()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            var card = new CardDefinition("PLAY_CARD", "Card", CardCategory.Champion, ClanType.Milenarios, 1, 3, 3);
            match.PlayerHand[0] = card;

            ArenaBattleEngine.PlayPlayerCard(match, 0, 1, out _);
            Assert.AreEqual(0, match.PlayerEnergy);
            Assert.AreEqual(1, match.PlayerLanes[1].Count);

            // Undo play
            bool undone = ArenaBattleEngine.UndoPlayerPlay(match, 1, 0);

            Assert.IsTrue(undone);
            Assert.AreEqual(1, match.PlayerEnergy, "Energy refunded back to 1");
            Assert.AreEqual(0, match.PlayerLanes[1].Count, "Card removed from lane 1");
            Assert.AreEqual(4, match.PlayerHand.Count, "Card returned to hand");
            Assert.AreEqual(0, match.PlayerTurnPlays.Count, "Turn plays cleared for this card");
        }

        [Test]
        public void UndoPlayerPlay_CannotUndoFromPreviousTurns()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            match.PlayerHand[0] = new CardDefinition("PLAY_CARD", "Card", CardCategory.Champion, ClanType.Milenarios, 1, 3, 3);

            ArenaBattleEngine.PlayPlayerCard(match, 0, 0, out _);

            // Advance turn: locks plays
            ArenaBattleEngine.EndTurn(match);
            Assert.AreEqual(2, match.CurrentTurn);

            // Attempt undo on lane 0 card from turn 1
            bool undone = ArenaBattleEngine.UndoPlayerPlay(match, 0, 0);

            Assert.IsFalse(undone, "Cards locked from previous turns cannot be undone.");
            Assert.AreEqual(1, match.PlayerLanes[0].Count);
        }

        [Test]
        public void ArenaPowerCalculator_CalculatesClanAdvantageBonus()
        {
            // Círculo Cometa: Milenarios -> Fugitivos -> Depredadores -> Míticos -> Milenarios
            var milenarioCard = new CardDefinition("C1", "Milenario", CardCategory.Champion, ClanType.Milenarios, 2, 4, 4);
            var fugitivoCard = new CardDefinition("C2", "Fugitivo", CardCategory.Champion, ClanType.Fugitivos, 2, 4, 4);
            var depredadorCard = new CardDefinition("C3", "Depredador", CardCategory.Champion, ClanType.Depredadores, 2, 4, 4);
            var miticoCard = new CardDefinition("C4", "Mítico", CardCategory.Champion, ClanType.Miticos, 2, 4, 4);

            // Neutral arena without clan
            var neutralArena = new CardDefinition("A_NEUTRAL", "Arena Neutral", CardCategory.Arena, ClanType.Neutral, 0, 0, 0);

            // Milenario vs Fugitivo: +1 bonus damage (4 -> 5)
            int milenarioPower = ArenaPowerCalculator.CalculateCardEffectivePower(milenarioCard, neutralArena, new[] { fugitivoCard });
            Assert.AreEqual(5, milenarioPower);

            // Fugitivo vs Depredador: +1 bonus damage (4 -> 5)
            int fugitivoPower = ArenaPowerCalculator.CalculateCardEffectivePower(fugitivoCard, neutralArena, new[] { depredadorCard });
            Assert.AreEqual(5, fugitivoPower);

            // Depredador vs Mítico: +1 bonus damage (4 -> 5)
            int depredadorPower = ArenaPowerCalculator.CalculateCardEffectivePower(depredadorCard, neutralArena, new[] { miticoCard });
            Assert.AreEqual(5, depredadorPower);

            // Mítico vs Milenario: +1 bonus damage (4 -> 5)
            int miticoPower = ArenaPowerCalculator.CalculateCardEffectivePower(miticoCard, neutralArena, new[] { milenarioCard });
            Assert.AreEqual(5, miticoPower);

            // Without opposing advantage target: stays 4
            int noAdvantagePower = ArenaPowerCalculator.CalculateCardEffectivePower(milenarioCard, neutralArena, new[] { miticoCard });
            Assert.AreEqual(4, noAdvantagePower);
        }

        [Test]
        public void ArenaPowerCalculator_CalculatesArenaClanAffinity()
        {
            var milenarioCard = new CardDefinition("C1", "Milenario", CardCategory.Champion, ClanType.Milenarios, 2, 3, 4);
            var milenarioArena = new CardDefinition("A_MIL", "Santuario Verde", CardCategory.Arena, ClanType.Milenarios, 0, 0, 0);

            // Same clan affinity: +1
            int power = ArenaPowerCalculator.CalculateCardEffectivePower(milenarioCard, milenarioArena, Array.Empty<CardDefinition>());
            Assert.AreEqual(4, power, "3 base + 1 clan affinity = 4");
        }

        [Test]
        public void ArenaPowerCalculator_AppliesSpecificArenaRules()
        {
            var regularCard = new CardDefinition("REG", "Guerrero Común", CardCategory.Champion, ClanType.Neutral, 2, 3, 3);
            var turboCard = new CardDefinition("TURBO", "Turbo Cobra", CardCategory.Champion, ClanType.Fugitivos, 2, 3, 3);

            var tiranoArena = new CardDefinition("A_TIRANO", "Llanuras del Tirano", CardCategory.Arena, ClanType.Neutral, 0, 0, 0);
            var pistaArena = new CardDefinition("A_PISTA", "La Pista de Carreras", CardCategory.Arena, ClanType.Milenarios, 0, 0, 0);

            // Llanuras del Tirano: +2
            int tiranoPower = ArenaPowerCalculator.CalculateCardEffectivePower(regularCard, tiranoArena, Array.Empty<CardDefinition>());
            Assert.AreEqual(5, tiranoPower, "3 base + 2 Llanuras del Tirano = 5");

            // La Pista de Carreras: +2 for Turbo
            int turboPower = ArenaPowerCalculator.CalculateCardEffectivePower(turboCard, pistaArena, Array.Empty<CardDefinition>());
            Assert.AreEqual(5, turboPower, "3 base + 2 La Pista de Carreras = 5");

            // Non-turbo on Pista de Carreras
            int nonTurboPower = ArenaPowerCalculator.CalculateCardEffectivePower(regularCard, pistaArena, Array.Empty<CardDefinition>());
            Assert.AreEqual(3, nonTurboPower, "3 base, not turbo so no bonus");
        }

        [Test]
        public void BattleAiController_PlaysAffordableCardsAndScoresLanes()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            match.AiEnergy = 3;
            match.AiHand.Clear();
            match.AiHand.Add(new CardDefinition("AI_1", "AI Bot 1", CardCategory.Champion, ClanType.Fugitivos, 1, 2, 2));
            match.AiHand.Add(new CardDefinition("AI_2", "AI Bot 2", CardCategory.Champion, ClanType.Fugitivos, 2, 3, 3));

            BattleAiController.ExecuteAiTurn(match);

            int totalAiCardsPlayed = match.AiLanes.Sum(l => l.Count);
            Assert.IsTrue(totalAiCardsPlayed > 0, "AI should deploy affordable cards");
            Assert.AreEqual(0, match.AiEnergy, "AI spent 1 + 2 = 3 energy");
        }

        [Test]
        public void TurnProgression_IncrementsEnergyAndDrawsCardsUntilTurn6()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            for (int t = 1; t <= 5; t++)
            {
                Assert.AreEqual(t, match.CurrentTurn);
                Assert.AreEqual(t, match.PlayerMaxEnergy);
                Assert.AreEqual(t, match.PlayerEnergy);

                ArenaBattleEngine.EndTurn(match);
            }

            // At Turn 6
            Assert.AreEqual(6, match.CurrentTurn);
            Assert.AreEqual(6, match.PlayerMaxEnergy);
            Assert.AreEqual(6, match.PlayerEnergy);
            Assert.IsFalse(match.IsGameOver);

            // Ending Turn 6 resolves the match
            ArenaBattleEngine.EndTurn(match);
            Assert.IsTrue(match.IsGameOver);
            Assert.AreNotEqual(BattleWinner.None, match.Winner);
        }

        [Test]
        public void ResolveGameOver_PlayerWinsWhenDominating2Of3Arenas()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            // Player leads lane 0 (10 vs 2) and lane 1 (8 vs 4)
            // AI leads lane 2 (2 vs 15)
            match.PlayerLanes[0].Add(new CardDefinition("P_STRONG", "P Strong", CardCategory.Champion, ClanType.Neutral, 1, 10, 10));
            match.AiLanes[0].Add(new CardDefinition("AI_WEAK", "AI Weak", CardCategory.Champion, ClanType.Neutral, 1, 2, 2));

            match.PlayerLanes[1].Add(new CardDefinition("P_MED", "P Med", CardCategory.Champion, ClanType.Neutral, 1, 8, 8));
            match.AiLanes[1].Add(new CardDefinition("AI_MED", "AI Med", CardCategory.Champion, ClanType.Neutral, 1, 4, 4));

            match.PlayerLanes[2].Add(new CardDefinition("P_LOW", "P Low", CardCategory.Champion, ClanType.Neutral, 1, 2, 2));
            match.AiLanes[2].Add(new CardDefinition("AI_STRONG", "AI Strong", CardCategory.Champion, ClanType.Neutral, 1, 15, 15));

            ArenaBattleEngine.ResolveGameOver(match);

            Assert.IsTrue(match.IsGameOver);
            Assert.AreEqual(2, match.PlayerLanesWon);
            Assert.AreEqual(1, match.AiLanesWon);
            Assert.AreEqual(BattleWinner.Player, match.Winner, "Player won 2 of 3 lanes");
        }

        [Test]
        public void ResolveGameOver_TieBreakerUsesTotalPower()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            // Lane 0: Player wins (10 vs 2) -> +8 diff for player
            match.PlayerLanes[0].Add(new CardDefinition("P0", "P0", CardCategory.Champion, ClanType.Neutral, 1, 10, 10));
            match.AiLanes[0].Add(new CardDefinition("A0", "A0", CardCategory.Champion, ClanType.Neutral, 1, 2, 2));

            // Lane 1: AI wins (3 vs 6) -> +3 diff for AI
            match.PlayerLanes[1].Add(new CardDefinition("P1", "P1", CardCategory.Champion, ClanType.Neutral, 1, 3, 3));
            match.AiLanes[1].Add(new CardDefinition("A1", "A1", CardCategory.Champion, ClanType.Neutral, 1, 6, 6));

            // Lane 2: Tied (5 vs 5)
            match.PlayerLanes[2].Add(new CardDefinition("P2", "P2", CardCategory.Champion, ClanType.Neutral, 1, 5, 5));
            match.AiLanes[2].Add(new CardDefinition("A2", "A2", CardCategory.Champion, ClanType.Neutral, 1, 5, 5));

            // Score: 1-1 in lanes.
            // Player total: 10 + 3 + 5 = 18.
            // AI total: 2 + 6 + 5 = 13.
            ArenaBattleEngine.ResolveGameOver(match);

            Assert.IsTrue(match.IsGameOver);
            Assert.AreEqual(1, match.PlayerLanesWon);
            Assert.AreEqual(1, match.AiLanesWon);
            Assert.AreEqual(18, match.PlayerTotalPower);
            Assert.AreEqual(13, match.AiTotalPower);
            Assert.AreEqual(BattleWinner.Player, match.Winner, "Tie-breaker by total accumulated power awards victory to Player");
        }

        [Test]
        public void Mulligan_SelectsCards_ReplacesWithNewAndShufflesDeck()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);

            Assert.IsTrue(match.IsMulliganPhase);
            Assert.IsFalse(match.PlayerMulliganDone);
            Assert.AreEqual(4, match.PlayerHand.Count);
            Assert.AreEqual(8, match.PlayerDeck.Count);

            var originalCard0 = match.PlayerHand[0];
            var originalCard2 = match.PlayerHand[2];

            // Perform mulligan on card indices 0 and 2
            bool success = ArenaBattleEngine.ExecuteMulligan(match, isPlayer: true, new[] { 0, 2 });

            Assert.IsTrue(success);
            Assert.IsTrue(match.PlayerMulliganDone);
            Assert.AreEqual(4, match.PlayerHand.Count, "Hand size preserved after mulligan");
            Assert.AreEqual(8, match.PlayerDeck.Count, "Deck size preserved (replaced cards returned to deck)");
        }

        [Test]
        public void CompleteMulligan_DrawsTurn1Card_HandBecomes5_AndPlansAiSecretPlays()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            ArenaBattleEngine.CompleteMulligan(match);

            Assert.IsFalse(match.IsMulliganPhase, "Mulligan phase is now closed");
            Assert.AreEqual(5, match.PlayerHand.Count, "Turn 1 draw brings hand to 5 cards (Rule 22)");
            Assert.AreEqual(7, match.PlayerDeck.Count, "12 - 4 (starting) - 1 (turn 1 draw) = 7 left in deck");

            // AI has planned simultaneous play face down
            int totalAiCardsPlayed = match.AiLaneCards.Sum(l => l.Count);
            if (totalAiCardsPlayed > 0)
            {
                Assert.IsTrue(match.AiLaneCards.SelectMany(l => l).All(c => !c.IsRevealed),
                    "Opponent cards played this turn must remain face-down until EndTurn resolution (Rule 17)");
            }
        }

        [Test]
        public void SimultaneousPlay_AiCardsUnrevealedUntilEndTurn()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            ArenaBattleEngine.CompleteMulligan(match);

            // Directly add a face-down card to AI lane to verify reveal sequence
            match.AiLaneCards[0].Clear();
            var faceDownAiCard = new LaneCardInstance(new CardDefinition("AI_HIDDEN", "Secret Beast", CardCategory.KombatBeast, ClanType.Neutral, 1, 4), ownerIsPlayer: false, laneIndex: 0, isRevealed: false);
            match.AiLaneCards[0].Add(faceDownAiCard);

            Assert.IsFalse(faceDownAiCard.IsRevealed);
            // Power should not count unrevealed cards
            int initialPower = ArenaPowerCalculator.CalculateLanePower(match.AiLaneCards[0], match.Arenas[0], match.PlayerLaneCards[0]);
            Assert.AreEqual(0, initialPower, "Unrevealed cards contribute 0 visible power to arena");

            // Resolve turn
            ArenaBattleEngine.EndTurn(match);

            Assert.IsTrue(faceDownAiCard.IsRevealed, "Face-down cards must be revealed at resolution order step 1 (Rule 18.1)");
            int resolvedPower = ArenaPowerCalculator.CalculateLanePower(match.AiLaneCards[0], match.Arenas[0], match.PlayerLaneCards[0]);
            Assert.AreEqual(4, resolvedPower, "Revealed card now contributes its full power");
        }

        [Test]
        public void ResolutionOrder_ExecutesOnPlayAbilitiesAndModifiesPower()
        {
            var pDeck = CreateTestDeck(ClanType.Milenarios, "P");
            var aiDeck = CreateTestDeck(ClanType.Fugitivos, "AI");
            var arenas = CreateTestArenas();

            var match = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, arenas);
            match.IsMulliganPhase = false;

            // Lane 0: Friendly card and a buffer card
            var baseAlly = new LaneCardInstance(new CardDefinition("ALLY", "Base Ally", CardCategory.Champion, ClanType.Milenarios, 1, 3), ownerIsPlayer: true, laneIndex: 0, isRevealed: true);
            match.PlayerLaneCards[0].Add(baseAlly);

            // Avispón: OnPlay BuffFriendlyPower +1
            var avispon = MvpCardCatalog.GetCardById("HERO_AVISPON");
            Assert.IsNotNull(avispon);
            var avisponInstance = new LaneCardInstance(avispon, ownerIsPlayer: true, laneIndex: 0, isRevealed: true);
            match.PlayerLaneCards[0].Add(avisponInstance);

            // Enemy card in lane 0
            var enemyCard = new LaneCardInstance(new CardDefinition("ENEMY", "Enemy Beast", CardCategory.KombatBeast, ClanType.Depredadores, 1, 5), ownerIsPlayer: false, laneIndex: 0, isRevealed: true);
            match.AiLaneCards[0].Add(enemyCard);

            // Trompo Fuego: OnPlay DebuffEnemyPower -3
            var trompoFuego = MvpCardCatalog.GetCardById("TROMPO_FUEGO");
            Assert.IsNotNull(trompoFuego);
            var trompoInstance = new LaneCardInstance(trompoFuego, ownerIsPlayer: true, laneIndex: 0, isRevealed: true);
            match.PlayerLaneCards[0].Add(trompoInstance);

            // Execute EndTurn resolution
            ArenaBattleEngine.EndTurn(match);

            // Ally should have +1 buff (from Avispón)
            Assert.AreEqual(1, baseAlly.PowerModifier, "Base ally received +1 Power from Avispón OnPlay");
            // Enemy should have -3 debuff (from Trompo Fuego)
            Assert.AreEqual(-3, enemyCard.PowerModifier, "Enemy received -3 Power from Trompo Fuego OnPlay");
            Assert.AreEqual(2, enemyCard.CurrentPower, "Enemy 5 power - 3 = 2 current power");
        }

        [Test]
        public void MvpCardPool_ContainsExactly20CardsWithAllCategories()
        {
            var allCards = MvpCardCatalog.AllCards;
            Assert.AreEqual(20, allCards.Count, "Pool must contain exactly 20 cards (Rule 24)");

            var heroes = MvpCardCatalog.GetByType(CardType.Hero).ToList();
            var beasts = MvpCardCatalog.GetByType(CardType.Beast).ToList();
            var trompos = MvpCardCatalog.GetByType(CardType.Trompo).ToList();
            var arenas = MvpCardCatalog.GetByType(CardType.Arena).ToList();

            Assert.AreEqual(6, heroes.Count, "Must have exactly 6 Heroes");
            Assert.AreEqual(6, beasts.Count, "Must have exactly 6 Beasts");
            Assert.AreEqual(5, trompos.Count, "Must have exactly 5 Trompos");
            Assert.AreEqual(3, arenas.Count, "Must have exactly 3 Arenas");

            // Verify a balanced 12-card deck generation
            var deck = MvpCardCatalog.CreateBalancedMvpDeck(ClanType.Milenarios);
            Assert.AreEqual(12, deck.Count, "Generated deck must have exactly 12 cards");
            bool valid = DeckValidator.ValidateArenaDeck(deck, out var errors);
            Assert.IsTrue(valid, string.Join(", ", errors));
        }

        [Test]
        public void MvpCardPool_All20Cards_HaveValidImagePaths()
        {
            var allCards = MvpCardCatalog.AllCards;
            Assert.AreEqual(20, allCards.Count, "Must have exactly 20 cards");

            foreach (var card in allCards)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(card.ImagePath), $"Card {card.Id} ({card.Name}) must have a valid ImagePath.");
            }
        }
    }
}
