using System;
using System.Linq;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# state machine controlling match flow and turn progression.
    /// </summary>
    public static class TurnStateMachine
    {
        public const int StartingHandSize = 5;

        public static void InitializeMatch(MatchState match)
        {
            // Draw starting hands
            for (int i = 0; i < StartingHandSize; i++)
            {
                match.Player1.DrawCard();
                match.Player2.DrawCard();
            }

            match.CurrentPhase = TurnPhase.Preparation;
            match.ActivePlayerId = match.Player1.PlayerId;
            match.TurnNumber = 1;

            StartPreparationPhase(match);
        }

        public static void StartPreparationPhase(MatchState match)
        {
            if (match.IsGameOver) return;

            match.CurrentPhase = TurnPhase.Preparation;
            var activePlayer = match.GetActivePlayer();

            // 1. Ready all board cards and energy
            activePlayer.ReadyAll();

            // 2. Draw 1 card for the turn
            activePlayer.DrawCard();
        }

        public static bool CommitEnergy(MatchState match, string instanceId)
        {
            if (match.IsGameOver) return false;
            if (match.CurrentPhase != TurnPhase.Preparation && match.CurrentPhase != TurnPhase.Action)
            {
                return false;
            }

            var activePlayer = match.GetActivePlayer();
            return activePlayer.ConvertHandCardToEnergy(instanceId);
        }

        public static void TransitionToActionPhase(MatchState match)
        {
            if (match.IsGameOver || match.CurrentPhase != TurnPhase.Preparation) return;
            match.CurrentPhase = TurnPhase.Action;
        }

        public static bool PlayCard(MatchState match, string cardInstanceId, out string failureReason)
        {
            failureReason = string.Empty;
            if (match.IsGameOver)
            {
                failureReason = "Match is already over.";
                return false;
            }

            if (match.CurrentPhase != TurnPhase.Action)
            {
                failureReason = "Cards can only be played during the Action phase.";
                return false;
            }

            var activePlayer = match.GetActivePlayer();
            var card = activePlayer.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId);
            if (card == null)
            {
                failureReason = "Card not found in hand.";
                return false;
            }

            if (!activePlayer.CanAfford(card.Definition.EnergyCost))
            {
                failureReason = $"Not enough energy. Requires {card.Definition.EnergyCost}, available {activePlayer.AvailableEnergy}.";
                return false;
            }

            // Pay cost
            activePlayer.SpendEnergy(card.Definition.EnergyCost);
            activePlayer.Hand.Remove(card);

            switch (card.Definition.Category)
            {
                case CardCategory.Champion:
                case CardCategory.KombatBeast:
                    card.HasSummoningSickness = true;
                    card.IsExhausted = false;
                    activePlayer.Board.Add(card);
                    break;

                case CardCategory.Arena:
                    if (match.ActiveArena != null)
                    {
                        // Send existing arena to its owner's graveyard
                        activePlayer.Graveyard.Add(match.ActiveArena);
                    }
                    match.ActiveArena = card;
                    break;

                case CardCategory.PhysicalTrompo:
                    // Instant / spell effect resolves, then goes to graveyard
                    activePlayer.Graveyard.Add(card);
                    break;
            }

            return true;
        }

        public static void EndTurn(MatchState match)
        {
            if (match.IsGameOver) return;

            match.CurrentPhase = TurnPhase.Ending;
            match.CheckWinCondition();

            if (match.IsGameOver) return;

            // Switch active player
            match.ActivePlayerId = match.ActivePlayerId == match.Player1.PlayerId
                ? match.Player2.PlayerId
                : match.Player1.PlayerId;

            // Increment turn count when cycling back to Player 1
            if (match.ActivePlayerId == match.Player1.PlayerId)
            {
                match.TurnNumber++;
            }

            StartPreparationPhase(match);
        }
    }
}
