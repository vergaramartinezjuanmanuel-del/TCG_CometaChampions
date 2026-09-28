using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# bot decision engine for the AI opponent, inspired by MARVEL SNAP smart bots.
    /// Evaluates hand cards by cost curve and scores lanes to flip leads or consolidate advantages.
    /// Follows strictly the exact same Energy, Deck, Hand, Card Cost, and Arena Capacity rules (Rule 23).
    /// </summary>
    public static class BattleAiController
    {
        /// <summary>
        /// AI Mulligan decision according to Rule 2 & 23:
        /// Replaces any cards of cost >= 4 or if no cards <= 2 are present.
        /// </summary>
        public static void DecideMulligan(ArenaBattleMatchState state, Random? rng = null)
        {
            if (state == null || state.AiMulliganDone) return;

            var indicesToReplace = new List<int>();
            bool hasEarlyPlay = state.AiHand.Any(c => c.EnergyCost <= 2);

            for (int i = 0; i < state.AiHand.Count; i++)
            {
                var card = state.AiHand[i];
                if (card.EnergyCost >= 4 || (!hasEarlyPlay && card.EnergyCost >= 3))
                {
                    indicesToReplace.Add(i);
                }
            }

            ArenaBattleEngine.ExecuteMulligan(state, isPlayer: false, indicesToReplace, rng);
        }

        /// <summary>
        /// Plans AI plays simultaneously during the turn.
        /// Cards are placed face-down (IsRevealed = false) until turn resolution (Rule 17).
        /// </summary>
        public static void PlanAiTurn(ArenaBattleMatchState state)
        {
            if (state == null || state.IsGameOver) return;

            // Clear any previously recorded turn plays for this turn
            int energyLeft = state.AiEnergy;
            var availableHand = new List<CardDefinition>(state.AiHand);

            // Sort hand by cost descending to prioritize impactful plays
            availableHand.Sort((a, b) => b.EnergyCost.CompareTo(a.EnergyCost));

            for (int i = availableHand.Count - 1; i >= 0; i--)
            {
                var card = availableHand[i];
                if (card.EnergyCost <= energyLeft)
                {
                    int bestLane = -1;
                    int highestScore = -900;

                    for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
                    {
                        if (state.AiLaneCards[l].Count >= ArenaBattleMatchState.MaxCardsPerLane)
                        {
                            continue;
                        }

                        var arena = state.Arenas[l];
                        var pCards = state.PlayerLaneCards[l];
                        var aiCards = state.AiLaneCards[l];

                        int pPower = ArenaPowerCalculator.CalculateLanePower(pCards, arena, aiCards);
                        int aiPower = ArenaPowerCalculator.CalculateLanePower(aiCards, arena, pCards);
                        int effPower = ArenaPowerCalculator.CalculateCardEffectivePower(card, arena, state.PlayerLanes[l]);

                        int score = 10;
                        if (aiPower <= pPower && aiPower + effPower > pPower)
                        {
                            score += 50; // Winning swing: flips lead to AI!
                        }
                        else if (aiPower > pPower)
                        {
                            score += 15; // Solidifying existing lead
                        }

                        // Clan match bonus with arena
                        if (arena != null && arena.Clan != ClanType.None &&
                            arena.Clan != ClanType.Neutral && arena.Clan == card.Clan)
                        {
                            score += 10;
                        }

                        // Distribute presence across emptier lanes
                        score += (ArenaBattleMatchState.MaxCardsPerLane - aiCards.Count);

                        if (score > highestScore)
                        {
                            highestScore = score;
                            bestLane = l;
                        }
                    }

                    if (bestLane >= 0 && highestScore > -900)
                    {
                        energyLeft -= card.EnergyCost;
                        state.AiHand.Remove(card);

                        // Place card face-down (IsRevealed = false) until turn resolution
                        var instance = new LaneCardInstance(card, ownerIsPlayer: false, bestLane, isRevealed: false);
                        state.AiLaneCards[bestLane].Add(instance);
                        state.AiTurnPlays.Add(new TurnPlayAction(card, bestLane));
                        availableHand.RemoveAt(i);
                    }
                }
            }

            state.AiEnergy = energyLeft;
            state.SyncLegacyLanes();
        }

        /// <summary>
        /// Legacy / direct execution method for backwards compatibility with tests.
        /// </summary>
        public static void ExecuteAiTurn(ArenaBattleMatchState state)
        {
            PlanAiTurn(state);
        }
    }
}
