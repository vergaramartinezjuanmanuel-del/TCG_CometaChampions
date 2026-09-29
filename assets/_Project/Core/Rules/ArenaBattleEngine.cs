using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# authoritative engine managing the lifecycle of an Arena Battle match.
    /// Implements all 26 MVP Core Game Rules:
    /// - 12-card decks, max 2 copies
    /// - 4-card starting hand with optional Mulligan before Turn 1
    /// - 6 turns, Energy 1 to 6, draw 1 card each turn (including Turn 1, seeing 10 cards)
    /// - 3 Arenas, max 4 cards per lane
    /// - Simultaneous hidden actions and 8-step resolution order
    /// - Victory by 2 of 3 arenas or total power tie-breaker
    /// </summary>
    public static class ArenaBattleEngine
    {
        public static ArenaBattleMatchState CreateMatch(
            IEnumerable<CardDefinition> playerDeckCards,
            IEnumerable<CardDefinition> aiDeckCards,
            IEnumerable<CardDefinition> selectedArenas,
            Random? rng = null)
        {
            var random = rng ?? new Random();
            var state = new ArenaBattleMatchState();

            // Set Arenas (exactly 3)
            var arenaList = selectedArenas?.Take(ArenaBattleMatchState.MaxLanes).ToList() ?? new List<CardDefinition>();
            for (int i = 0; i < ArenaBattleMatchState.MaxLanes; i++)
            {
                if (i < arenaList.Count)
                {
                    state.Arenas[i] = arenaList[i];
                }
            }

            // Shuffle and assign Decks (Rule 1)
            state.PlayerDeck = Shuffle(playerDeckCards, random);
            state.AiDeck = Shuffle(aiDeckCards, random);

            // Draw starting hands (4 cards each - Rule 2)
            for (int i = 0; i < ArenaBattleMatchState.DefaultStartingHandSize; i++)
            {
                DrawCard(state, isPlayer: true);
                DrawCard(state, isPlayer: false);
            }

            // Turn 1 setup
            state.CurrentTurn = 1;
            state.MaxTurn = ArenaBattleMatchState.TotalTurns;
            state.PlayerMaxEnergy = 1;
            state.PlayerEnergy = 1;
            state.AiMaxEnergy = 1;
            state.AiEnergy = 1;

            // Mulligan phase is active before Turn 1 action phase (Rule 2)
            state.IsMulliganPhase = true;
            state.PlayerMulliganDone = false;
            state.AiMulliganDone = false;

            state.SyncLegacyLanes();
            RecalculateScores(state);
            return state;
        }

        /// <summary>
        /// Executes a Mulligan for the player or AI according to Rule 2:
        /// - Select any number of cards (0 to 4) from the starting hand.
        /// - Replace each selected card with a new random card from deck.
        /// - Replaced cards are returned to the deck and the deck is shuffled.
        /// </summary>
        public static bool ExecuteMulligan(
            ArenaBattleMatchState state,
            bool isPlayer,
            IEnumerable<int> handIndicesToReplace,
            Random? rng = null)
        {
            if (state == null) return false;
            if (!state.IsMulliganPhase) return false;

            var random = rng ?? new Random();
            var hand = isPlayer ? state.PlayerHand : state.AiHand;
            var deck = isPlayer ? state.PlayerDeck : state.AiDeck;

            if (isPlayer && state.PlayerMulliganDone) return false;
            if (!isPlayer && state.AiMulliganDone) return false;

            var distinctIndices = (handIndicesToReplace ?? Enumerable.Empty<int>())
                .Distinct()
                .Where(idx => idx >= 0 && idx < hand.Count)
                .OrderByDescending(idx => idx)
                .ToList();

            if (distinctIndices.Count > 0 && deck.Count > 0)
            {
                var replacedCards = new List<CardDefinition>();

                foreach (int idx in distinctIndices)
                {
                    if (deck.Count == 0) break;

                    var oldCard = hand[idx];
                    replacedCards.Add(oldCard);

                    // Draw new card from top of deck
                    var newCard = deck[0];
                    deck.RemoveAt(0);
                    hand[idx] = newCard;
                }

                // Return replaced cards to deck and shuffle
                deck.AddRange(replacedCards);
                var shuffled = Shuffle(deck, random);
                deck.Clear();
                deck.AddRange(shuffled);
            }

            if (isPlayer) state.PlayerMulliganDone = true;
            else state.AiMulliganDone = true;

            return true;
        }

        /// <summary>
        /// Completes the Mulligan phase, draws the Turn 1 card (Rule 22),
        /// plans the AI's simultaneous secret turn plays (Rule 17), and begins Turn 1 action phase.
        /// </summary>
        public static void CompleteMulligan(ArenaBattleMatchState state, Random? rng = null)
        {
            if (state == null) return;

            // Ensure AI completes its mulligan if not done
            if (!state.AiMulliganDone)
            {
                BattleAiController.DecideMulligan(state, rng);
            }

            state.IsMulliganPhase = false;

            // Turn 1 draw: 1 card each (Rule 3 & 22) -> Hand size becomes 5 cards!
            DrawCard(state, isPlayer: true);
            DrawCard(state, isPlayer: false);

            // Ensure starting hand playability for Turn 1
            EnsureStartingHandPlayable(state.PlayerHand, state.PlayerDeck, 1);
            EnsureStartingHandPlayable(state.AiHand, state.AiDeck, 1);

            // AI plans its simultaneous hidden plays for Turn 1 (Rule 17)
            BattleAiController.PlanAiTurn(state);

            state.SyncLegacyLanes();
            RecalculateScores(state);
        }

        /// <summary>
        /// Plays a card from the player's hand into an Arena (Rule 6).
        /// Deducts energy immediately and adds to lane instance tracking.
        /// </summary>
        public static bool PlayPlayerCard(
            ArenaBattleMatchState state,
            int handIndex,
            int laneIndex,
            out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "El estado de la partida no es válido.";
                return false;
            }

            if (state.IsGameOver)
            {
                error = "La partida ya ha finalizado.";
                return false;
            }

            if (state.IsMulliganPhase)
            {
                state.IsMulliganPhase = false;
            }

            if (handIndex < 0 || handIndex >= state.PlayerHand.Count)
            {
                error = "Índice de carta en mano no válido.";
                return false;
            }

            if (laneIndex < 0 || laneIndex >= ArenaBattleMatchState.MaxLanes)
            {
                error = "Arena de combate no válida.";
                return false;
            }

            var card = state.PlayerHand[handIndex];
            if (card == null)
            {
                error = "Carta inexistente.";
                return false;
            }

            if (state.PlayerEnergy < card.EnergyCost)
            {
                error = $"No tienes suficiente energía ({state.PlayerEnergy} disponible, requiere {card.EnergyCost}).";
                return false;
            }

            int currentCount = Math.Max(state.PlayerLaneCards[laneIndex].Count, state.PlayerLanes[laneIndex].Count);
            if (currentCount >= ArenaBattleMatchState.MaxCardsPerLane)
            {
                error = "Esta arena ya está llena (máximo 4 cartas).";
                return false;
            }

            // Deduct energy immediately
            state.PlayerEnergy -= card.EnergyCost;
            state.PlayerHand.RemoveAt(handIndex);

            // Create battlefield instance (revealed for player)
            var instance = new LaneCardInstance(card, ownerIsPlayer: true, laneIndex, isRevealed: true);
            state.PlayerLaneCards[laneIndex].Add(instance);

            // Track for potential undo in this turn
            state.PlayerTurnPlays.Add(new TurnPlayAction(card, laneIndex));
            state.SelectedHandIndex = null;

            state.SyncLegacyLanes();
            RecalculateScores(state);
            return true;
        }

        /// <summary>
        /// Undoes a card played by the player during the current turn (Rule 16).
        /// Refunds energy and returns card to hand.
        /// </summary>
        public static bool UndoPlayerPlay(
            ArenaBattleMatchState state,
            int laneIndex,
            int cardSlotIndex)
        {
            if (state == null || state.IsGameOver) return false;

            if (laneIndex < 0 || laneIndex >= ArenaBattleMatchState.MaxLanes) return false;
            var laneCards = state.PlayerLaneCards[laneIndex];
            if (cardSlotIndex < 0 || cardSlotIndex >= laneCards.Count) return false;

            var instance = laneCards[cardSlotIndex];
            if (instance == null || instance.Card == null) return false;

            // Verify that this card was played during the current turn
            int playIdx = state.PlayerTurnPlays.FindIndex(p => p.Card == instance.Card && p.LaneIndex == laneIndex);
            if (playIdx == -1)
            {
                // Locked from previous turns
                return false;
            }

            // Refund energy and return to hand
            state.PlayerTurnPlays.RemoveAt(playIdx);
            laneCards.RemoveAt(cardSlotIndex);
            state.PlayerEnergy += instance.Card.EnergyCost;
            state.PlayerHand.Add(instance.Card);

            state.SyncLegacyLanes();
            RecalculateScores(state);
            return true;
        }

        /// <summary>
        /// Ends the current turn and executes the official 8-Step Resolution Order (Rule 18).
        /// 1. Reveal played cards
        /// 2. Resolve Arena effects
        /// 3. Resolve On Play abilities
        /// 4. Resolve triggered abilities (Combos)
        /// 5. Apply Power modifications
        /// 6. Calculate current Power in all 3 Arenas
        /// 7. Update the battlefield
        /// 8. Begin the next turn (or resolve Game Over at Turn 6)
        /// </summary>
        public static void EndTurn(ArenaBattleMatchState state)
        {
            if (state == null || state.IsGameOver) return;

            state.ResolutionLog.Clear();

            // STEP 1: Reveal played cards (Rule 18.1)
            for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
            {
                foreach (var aiCard in state.AiLaneCards[l])
                {
                    if (!aiCard.IsRevealed)
                    {
                        aiCard.IsRevealed = true;
                        state.ResolutionLog.Add($"• Rival reveló {aiCard.Card.Name} en {state.Arenas[l]?.Name ?? $"Arena {l + 1}"}.");
                    }
                }
            }

            // STEP 2: Resolve Arena effects (Rule 18.2)
            // (Continuous Arena bonuses are factored in ArenaPowerCalculator)

            // STEP 3: Resolve On Play abilities for newly placed cards (Rule 18.3)
            ResolveOnPlayAbilities(state);

            // STEP 4 & 5: Resolve triggered abilities and apply Power modifications (Rule 18.4 & 18.5)
            ResolveTriggeredAbilities(state);

            // STEP 6: Calculate current Power in all 3 Arenas (Rule 18.6)
            state.SyncLegacyLanes();
            RecalculateScores(state);

            // STEP 7: Update battlefield and lock actions (Rule 18.7)
            state.PlayerTurnPlays.Clear();
            state.AiTurnPlays.Clear();

            // STEP 8: Begin next turn or Game Over (Rule 18.8)
            if (state.CurrentTurn >= state.MaxTurn)
            {
                ResolveGameOver(state);
                return;
            }

            // Advance to next turn (Rule 3)
            state.CurrentTurn += 1;
            state.PlayerMaxEnergy = state.CurrentTurn;
            state.PlayerEnergy = state.PlayerMaxEnergy;
            state.AiMaxEnergy = state.CurrentTurn;
            state.AiEnergy = state.AiMaxEnergy;

            // Draw 1 card each at beginning of turn (Rule 3 & 22)
            DrawCard(state, isPlayer: true);
            DrawCard(state, isPlayer: false);

            // AI plans its simultaneous turn plays face down (Rule 17)
            BattleAiController.PlanAiTurn(state);

            state.SyncLegacyLanes();
            RecalculateScores(state);
        }

        private static void ResolveOnPlayAbilities(ArenaBattleMatchState state)
        {
            // Collect all unexecuted OnPlay cards across both sides
            var allInstances = new List<LaneCardInstance>();
            for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
            {
                allInstances.AddRange(state.PlayerLaneCards[l].Where(c => !c.OnPlayExecuted));
                allInstances.AddRange(state.AiLaneCards[l].Where(c => !c.OnPlayExecuted));
            }

            foreach (var instance in allInstances)
            {
                instance.OnPlayExecuted = true;
                var ability = instance.Card.Ability;
                if (ability == null || ability.Trigger != AbilityTrigger.OnPlay) continue;

                int lane = instance.LaneIndex;
                var friendlyCards = instance.OwnerIsPlayer ? state.PlayerLaneCards[lane] : state.AiLaneCards[lane];
                var enemyCards = instance.OwnerIsPlayer ? state.AiLaneCards[lane] : state.PlayerLaneCards[lane];

                switch (ability.EffectType)
                {
                    case AbilityEffectType.BuffFriendlyPower:
                        var otherAlly = friendlyCards.FirstOrDefault(c => c != instance);
                        if (otherAlly != null)
                        {
                            otherAlly.ApplyPowerModifier(ability.Value);
                            state.ResolutionLog.Add($"• {instance.Card.Name} otorgó +{ability.Value} Poder a {otherAlly.Card.Name}.");
                        }
                        break;

                    case AbilityEffectType.DebuffEnemyPower:
                        var targetEnemy = enemyCards.OrderByDescending(c => c.CurrentPower).FirstOrDefault();
                        if (targetEnemy != null)
                        {
                            targetEnemy.ApplyPowerModifier(-ability.Value);
                            state.ResolutionLog.Add($"• {instance.Card.Name} redujo en -{ability.Value} Poder a {targetEnemy.Card.Name}.");
                        }
                        break;

                    case AbilityEffectType.BuffAllFriendlyInLane:
                        int countBuffed = 0;
                        foreach (var ally in friendlyCards)
                        {
                            if (ally != instance)
                            {
                                ally.ApplyPowerModifier(ability.Value);
                                countBuffed++;
                            }
                        }
                        if (countBuffed > 0)
                        {
                            state.ResolutionLog.Add($"• {instance.Card.Name} otorgó +{ability.Value} Poder a {countBuffed} aliados en su arena.");
                        }
                        break;

                    case AbilityEffectType.MoveSelf:
                        // Move to adjacent arena if space permits
                        int targetLane = (lane + 1) % ArenaBattleMatchState.MaxLanes;
                        var targetList = instance.OwnerIsPlayer ? state.PlayerLaneCards[targetLane] : state.AiLaneCards[targetLane];
                        if (targetList.Count < ArenaBattleMatchState.MaxCardsPerLane)
                        {
                            friendlyCards.Remove(instance);
                            instance.LaneIndex = targetLane;
                            targetList.Add(instance);
                            state.ResolutionLog.Add($"• {instance.Card.Name} se movió a {state.Arenas[targetLane]?.Name ?? $"Arena {targetLane + 1}"}.");
                        }
                        break;

                    case AbilityEffectType.MoveTargetFriendly:
                        var allyToMove = friendlyCards.FirstOrDefault(c => c != instance);
                        if (allyToMove != null)
                        {
                            int destLane = (lane + 1) % ArenaBattleMatchState.MaxLanes;
                            var destList = instance.OwnerIsPlayer ? state.PlayerLaneCards[destLane] : state.AiLaneCards[destLane];
                            if (destList.Count < ArenaBattleMatchState.MaxCardsPerLane)
                            {
                                friendlyCards.Remove(allyToMove);
                                allyToMove.LaneIndex = destLane;
                                destList.Add(allyToMove);
                                state.ResolutionLog.Add($"• {instance.Card.Name} movió a {allyToMove.Card.Name} a {state.Arenas[destLane]?.Name ?? $"Arena {destLane + 1}"}.");
                            }
                        }
                        break;
                }
            }
        }

        private static void ResolveTriggeredAbilities(ArenaBattleMatchState state)
        {
            // EndOfTurn abilities: e.g. Fénix Ave Ígnea (+1 if winning this arena)
            for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
            {
                var pCards = state.PlayerLaneCards[l];
                var aiCards = state.AiLaneCards[l];
                var arena = state.Arenas[l];

                int pPower = ArenaPowerCalculator.CalculateLanePower(pCards, arena, aiCards);
                int aiPower = ArenaPowerCalculator.CalculateLanePower(aiCards, arena, pCards);

                foreach (var card in pCards)
                {
                    if (card.Card.Ability?.Trigger == AbilityTrigger.EndOfTurn &&
                        card.Card.Id == "BEAST_FENIX" && pPower > aiPower)
                    {
                        card.ApplyPowerModifier(card.Card.Ability.Value);
                        state.ResolutionLog.Add($"• {card.Card.Name} se fortaleció (+1 Poder por liderar su arena).");
                    }
                }

                foreach (var card in aiCards)
                {
                    if (card.Card.Ability?.Trigger == AbilityTrigger.EndOfTurn &&
                        card.Card.Id == "BEAST_FENIX" && aiPower > pPower)
                    {
                        card.ApplyPowerModifier(card.Card.Ability.Value);
                        state.ResolutionLog.Add($"• {card.Card.Name} rival se fortaleció (+1 Poder por liderar su arena).");
                    }
                }
            }
        }

        /// <summary>
        /// Resolves Game Over conditions according to Rules 19, 20, and 21.
        /// Win: Win at least 2 of 3 Arenas.
        /// Tie Breaker: Total accumulated Power across all 3 Arenas.
        /// </summary>
        public static void ResolveGameOver(ArenaBattleMatchState state)
        {
            if (state == null) return;

            state.IsGameOver = true;
            RecalculateScores(state);

            if (state.PlayerLanesWon > state.AiLanesWon)
            {
                state.Winner = BattleWinner.Player;
            }
            else if (state.AiLanesWon > state.PlayerLanesWon)
            {
                state.Winner = BattleWinner.Ai;
            }
            else
            {
                // Tie breaker: compare total accumulated power (Rule 21)
                if (state.PlayerTotalPower > state.AiTotalPower)
                {
                    state.Winner = BattleWinner.Player;
                }
                else if (state.AiTotalPower > state.PlayerTotalPower)
                {
                    state.Winner = BattleWinner.Ai;
                }
                else
                {
                    state.Winner = BattleWinner.Tie;
                }
            }
        }

        /// <summary>
        /// Draws a card from deck to hand (Rule 4 & 22). Max hand size = 7.
        /// </summary>
        public static void DrawCard(ArenaBattleMatchState state, bool isPlayer)
        {
            if (state == null) return;

            var deck = isPlayer ? state.PlayerDeck : state.AiDeck;
            var hand = isPlayer ? state.PlayerHand : state.AiHand;

            if (deck.Count > 0 && hand.Count < ArenaBattleMatchState.MaxHandSize)
            {
                var drawn = deck[0];
                deck.RemoveAt(0);
                hand.Add(drawn);
            }
        }

        public static void RecalculateScores(ArenaBattleMatchState state)
        {
            if (state == null) return;

            int pWon = 0;
            int aWon = 0;
            int pTotal = 0;
            int aTotal = 0;

            for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
            {
                var arena = state.Arenas[l];
                var pInstances = state.PlayerLaneCards[l];
                var aInstances = state.AiLaneCards[l];

                int pPower = 0;
                int aPower = 0;

                // If LaneCardInstances exist, calculate authoritatively with modifiers & synergies
                if (pInstances.Count > 0 || aInstances.Count > 0)
                {
                    pPower = ArenaPowerCalculator.CalculateLanePower(pInstances, arena, aInstances);
                    aPower = ArenaPowerCalculator.CalculateLanePower(aInstances, arena, pInstances);
                }
                else
                {
                    // Fallback to legacy PlayerLanes / AiLanes for tests that inject CardDefinition directly
                    var pCards = state.PlayerLanes[l];
                    var aCards = state.AiLanes[l];
                    pPower = ArenaPowerCalculator.CalculateLanePower(pCards, arena, aCards);
                    aPower = ArenaPowerCalculator.CalculateLanePower(aCards, arena, pCards);
                }

                pTotal += pPower;
                aTotal += aPower;

                int leader = ArenaPowerCalculator.GetLaneLeader(pPower, aPower);
                if (leader == 1) pWon++;
                else if (leader == 2) aWon++;
            }

            state.PlayerLanesWon = pWon;
            state.AiLanesWon = aWon;
            state.PlayerTotalPower = pTotal;
            state.AiTotalPower = aTotal;
        }

        private static void EnsureStartingHandPlayable(List<CardDefinition> hand, List<CardDefinition> deck, int maxTurnCost)
        {
            if (hand == null || deck == null || hand.Count == 0) return;
            if (hand.Any(c => c.EnergyCost <= maxTurnCost)) return;

            int deckMatchIdx = deck.FindIndex(c => c.EnergyCost <= maxTurnCost);
            if (deckMatchIdx != -1)
            {
                int highestHandIdx = 0;
                int maxCost = hand[0].EnergyCost;
                for (int i = 1; i < hand.Count; i++)
                {
                    if (hand[i].EnergyCost > maxCost)
                    {
                        maxCost = hand[i].EnergyCost;
                        highestHandIdx = i;
                    }
                }

                var temp = hand[highestHandIdx];
                hand[highestHandIdx] = deck[deckMatchIdx];
                deck[deckMatchIdx] = temp;
            }
        }

        private static List<CardDefinition> Shuffle(IEnumerable<CardDefinition> cards, Random random)
        {
            var list = cards?.ToList() ?? new List<CardDefinition>();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
