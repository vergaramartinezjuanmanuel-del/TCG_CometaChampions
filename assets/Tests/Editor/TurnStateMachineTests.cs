using NUnit.Framework;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Tests
{
    [TestFixture]
    public class TurnStateMachineTests
    {
        private MatchState CreateMatch()
        {
            var p1 = new PlayerState("player1");
            var p2 = new PlayerState("player2");

            for (int i = 0; i < 20; i++)
            {
                p1.Deck.Add(new CardInstance(new CardDefinition($"P1_CARD_{i}", "P1 Card", CardCategory.Champion, ClanType.Milenarios, 1, 2, 2, "", "")));
                p2.Deck.Add(new CardInstance(new CardDefinition($"P2_CARD_{i}", "P2 Card", CardCategory.Champion, ClanType.Fugitivos, 1, 2, 2, "", "")));
            }

            var match = new MatchState(p1, p2);
            TurnStateMachine.InitializeMatch(match);
            return match;
        }

        [Test]
        public void InitializeMatch_SetsUpStartingHandsAndPreparationPhase()
        {
            var match = CreateMatch();

            // 5 starting cards + 1 card drawn at start of turn 1 prep = 6 cards in P1 hand
            Assert.AreEqual(6, match.Player1.Hand.Count);
            Assert.AreEqual(5, match.Player2.Hand.Count);
            Assert.AreEqual(TurnPhase.Preparation, match.CurrentPhase);
            Assert.AreEqual("player1", match.ActivePlayerId);
            Assert.AreEqual(1, match.TurnNumber);
        }

        [Test]
        public void EnergyCommit_ConvertsCardFaceDown_OncePerTurn()
        {
            var match = CreateMatch();
            var firstCard = match.Player1.Hand[0];

            bool firstCommit = TurnStateMachine.CommitEnergy(match, firstCard.InstanceId);
            Assert.IsTrue(firstCommit);
            Assert.AreEqual(1, match.Player1.TotalEnergy);
            Assert.AreEqual(1, match.Player1.AvailableEnergy);
            Assert.IsTrue(firstCard.IsFaceDown);

            // Attempting to commit a second card in the same turn should fail
            var secondCard = match.Player1.Hand[0];
            bool secondCommit = TurnStateMachine.CommitEnergy(match, secondCard.InstanceId);
            Assert.IsFalse(secondCommit);
            Assert.AreEqual(1, match.Player1.TotalEnergy);
        }

        [Test]
        public void Combat_AppliesClanAdvantageAndSimultaneousDamage()
        {
            var match = CreateMatch();

            // P1 Milenarios creature (Atk 3, HP 4)
            var milenarioCard = new CardDefinition("MIL_01", "Milenario Warrior", CardCategory.Champion, ClanType.Milenarios, 2, 3, 4, "", "");
            var attacker = new CardInstance(milenarioCard);
            attacker.Ready(); // Remove summoning sickness

            // P2 Fugitivos creature (Atk 2, HP 4)
            var fugitivoCard = new CardDefinition("FUG_01", "Fugitivo Scout", CardCategory.Champion, ClanType.Fugitivos, 2, 2, 4, "", "");
            var defender = new CardInstance(fugitivoCard);
            defender.Exhaust(); // Exposed

            match.Player1.Board.Add(attacker);
            match.Player2.Board.Add(defender);

            // Milenarios has advantage over Fugitivos: Base 3 + 1 bonus = 4 damage
            var result = CombatResolver.ResolveCreatureCombat(attacker, defender, match.Player1, match.Player2);

            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(4, result.AttackerDamageDealt, "Attacker should deal 3 base + 1 clan advantage = 4 damage.");
            Assert.AreEqual(2, result.DefenderDamageDealt, "Defender deals 2 return damage.");
            Assert.IsTrue(result.DefenderDied, "Defender with 4 HP takes 4 damage and dies.");
            Assert.IsFalse(result.AttackerDied, "Attacker with 4 HP takes 2 damage and survives with 2 HP.");
            Assert.AreEqual(2, attacker.CurrentHealth);
        }
    }
}
