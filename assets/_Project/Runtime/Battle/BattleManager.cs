using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Master orchestrator for the 3-Arena Battle system in Unity.
    /// Bridges ArenaBattleEngine with visual lane views, hand tray, HUD, and modals.
    /// Fully implements the 26 Core MVP Game Rules.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private BattleHeaderView headerView;
        [SerializeField] private BattleLaneView[] laneViews = new BattleLaneView[3];
        [SerializeField] private BattleHandView handView;
        [SerializeField] private BattleGameOverModal gameOverModal;
        [SerializeField] private BattleHelpModal helpModal;
        [SerializeField] private BattleMulliganModal mulliganModal;

        [Header("Background")]
        [SerializeField] private Image battleBackgroundImage;

        [Header("Settings")]
        [SerializeField] private bool autoStartOnAwake = true;

        public ArenaBattleMatchState MatchState { get; private set; }
        public string CurrentArenaBackgroundName { get; private set; }
        private bool isResolvingTurn = false;

        private void Awake()
        {
            // Subscribe to header events
            if (headerView != null)
            {
                headerView.OnEndTurnClicked += HandleEndTurn;
                headerView.OnHelpClicked += HandleOpenHelp;
                headerView.OnRestartClicked += HandleRestartMatch;
                headerView.OnHomeClicked += HandleReturnHome;
            }

            // Subscribe to lane events
            if (laneViews != null)
            {
                for (int i = 0; i < laneViews.Length; i++)
                {
                    if (laneViews[i] != null)
                    {
                        laneViews[i].OnDeployClicked += HandleDeployToLane;
                        laneViews[i].OnUndoSlotClicked += HandleUndoSlot;
                    }
                }
            }

            // Subscribe to hand events
            if (handView != null)
            {
                handView.OnCardSelected += HandleHandCardSelected;
            }

            // Subscribe to mulligan modal events
            if (mulliganModal != null)
            {
                mulliganModal.OnMulliganConfirmed += HandleMulliganConfirmed;
            }
        }

        private void Start()
        {
            if (autoStartOnAwake)
            {
                StartNewMatch();
            }
        }

        public void StartNewMatch()
        {
            isResolvingTurn = false;
            var rng = new System.Random();

            // 0. Arena Background: Randomly select one of the 6 official backgrounds
            SetupRandomArenaBackground(rng);

            // 1. Arenas: 3 Arenas from MVP pool (Rule 15 & 24)
            var mvpArenas = MvpCardCatalog.GetMvpArenas();
            var selectedArenas = mvpArenas.OrderBy(_ => rng.Next()).Take(3).ToList();

            // 2. Decks: 12 cards, max 2 copies (Rule 1 & 24)
            var pDeck = GetPlayerDeck(rng);
            var aiDeck = MvpCardCatalog.CreateBalancedMvpDeck(ClanType.Depredadores, rng);

            // 3. Create Match (draws 4 starting cards each - Rule 2)
            MatchState = ArenaBattleEngine.CreateMatch(pDeck, aiDeck, selectedArenas, rng);

            // Initialize arenas in lane views
            for (int i = 0; i < laneViews.Length; i++)
            {
                if (laneViews[i] != null && i < MatchState.Arenas.Length)
                {
                    laneViews[i].SetupArena(MatchState.Arenas[i]);
                }
            }

            if (gameOverModal != null) gameOverModal.Hide();
            if (helpModal != null) helpModal.Hide();

            // 4. Trigger Mulligan Phase (Rule 2)
            if (mulliganModal != null)
            {
                mulliganModal.Show(MatchState.PlayerHand);
                if (headerView != null)
                {
                    headerView.SetEndTurnInteractable(false);
                    headerView.SetStatusNotification("FASE DE MULLIGAN: Puedes cambiar cartas de tu mano inicial antes del Turno 1.", new Color(0.9f, 0.85f, 0.3f));
                }
            }
            else
            {
                // Fallback if no modal in scene: auto-complete mulligan
                ArenaBattleEngine.CompleteMulligan(MatchState, rng);
                if (headerView != null)
                {
                    headerView.SetEndTurnInteractable(true);
                    headerView.SetStatusNotification($"Turno 1: Tienes {MatchState.PlayerEnergy} de energía. Toca una carta para jugar.", new Color(0.9f, 0.95f, 1f));
                }
            }

            RefreshAllUI();
        }

        private void HandleMulliganConfirmed(List<int> toSwap)
        {
            var rng = new System.Random();

            if (toSwap != null && toSwap.Count > 0)
            {
                // 1. Execute the mulligan swap in the authoritative engine
                ArenaBattleEngine.ExecuteMulligan(MatchState, isPlayer: true, toSwap, rng);

                if (headerView != null)
                {
                    headerView.SetStatusNotification($"Mulligan: Revelando {toSwap.Count} carta(s) nuevas...", new Color(0.95f, 0.85f, 0.3f));
                }

                // 2. Animate newly drawn cards with 3D spin flip effect, display them briefly, then finalize
                if (mulliganModal != null)
                {
                    mulliganModal.AnimateAndRevealNewCards(MatchState.PlayerHand, toSwap, () =>
                    {
                        FinalizeMulligan(rng, toSwap.Count);
                    });
                    return;
                }
            }

            FinalizeMulligan(rng, 0);
        }

        private void FinalizeMulligan(System.Random rng, int swappedCount)
        {
            if (mulliganModal != null)
            {
                mulliganModal.Hide();
            }

            if (headerView != null)
            {
                if (swappedCount > 0)
                {
                    headerView.SetStatusNotification($"Mulligan: {swappedCount} carta(s) cambiadas. ¡Inicia el Turno 1!", new Color(0.35f, 0.95f, 0.45f));
                }
                else
                {
                    headerView.SetStatusNotification("Has conservado tu mano inicial. ¡Inicia el Turno 1!", new Color(0.35f, 0.95f, 0.45f));
                }

                headerView.SetEndTurnInteractable(true);
            }

            // Complete mulligan, draw Turn 1 card (Hand = 5), and plan AI turn (Rule 17 & 22)
            ArenaBattleEngine.CompleteMulligan(MatchState, rng);

            RefreshAllUI();
        }

        private List<CardDefinition> GetPlayerDeck(System.Random rng)
        {
            var profile = SaveManager.Profile;
            if (profile != null)
            {
                // 1. Check if an active saved deck exists and validate that all cards belong to the player's inventory
                if (profile.SavedDecks != null && profile.SavedDecks.Count > 0)
                {
                    var saved = profile.SavedDecks.FirstOrDefault(d => d.CardIds != null && d.CardIds.Count == 12);
                    if (saved != null)
                    {
                        var cards = new List<CardDefinition>();
                        foreach (var id in saved.CardIds)
                        {
                            var def = MvpCardCatalog.GetCardById(id) ?? CardCatalog.GetCardById(id);
                            if (def != null && def.Category != CardCategory.Arena && def.Type != CardType.Arena)
                            {
                                cards.Add(def);
                            }
                        }

                        bool isOwnershipValid = DeckValidator.ValidateDeckOwnership(cards, profile, out var errors);
                        if (isOwnershipValid)
                        {
                            Debug.Log($"[BattleManager] Using saved combat deck '{saved.DeckName}' with 12 owned cards.");
                            return cards;
                        }
                        else
                        {
                            Debug.LogWarning($"[BattleManager] Saved deck '{saved.DeckName}' contains invalid or locked cards ({string.Join("; ", errors)}). Regenerating strictly from unlocked inventory.");
                        }
                    }
                }

                // 2. Build 12-card deck strictly and exclusively from unlocked cards in player inventory
                var inventoryDeck = DeckValidator.CreateDeckFromInventory(profile, rng);
                if (inventoryDeck != null && inventoryDeck.Count == 12)
                {
                    // Save as active combat deck so player collection and deck stay in sync
                    var savedDeck = new SavedDeckData("Mazo de Combate", ClanType.None, inventoryDeck.Select(c => c.Id));
                    if (profile.SavedDecks == null) profile.SavedDecks = new();
                    if (profile.SavedDecks.Count == 0) profile.SavedDecks.Add(savedDeck);
                    else profile.SavedDecks[0] = savedDeck;
                    SaveManager.Save();

                    Debug.Log($"[BattleManager] Auto-generated and saved 12-card combat deck strictly from inventory ({string.Join(", ", inventoryDeck.Select(c => c.Name))}).");
                    return inventoryDeck;
                }
            }

            // Fallback (only if profile is null in standalone test fixtures)
            Debug.LogWarning("[BattleManager] Profile not available or inventory empty. Falling back to default MVP pool.");
            return MvpCardCatalog.CreateBalancedMvpDeck(ClanType.Milenarios, rng);
        }

        private void HandleHandCardSelected(int handIndex)
        {
            if (MatchState == null || MatchState.IsGameOver || MatchState.IsMulliganPhase || isResolvingTurn) return;

            if (MatchState.SelectedHandIndex.HasValue && MatchState.SelectedHandIndex.Value == handIndex)
            {
                MatchState.SelectedHandIndex = null; // Toggle deselect
                if (headerView != null)
                {
                    headerView.SetStatusNotification("Toca una carta en tu mano para seleccionarla.", new Color(0.8f, 0.85f, 0.9f));
                }
            }
            else
            {
                MatchState.SelectedHandIndex = handIndex;
                if (headerView != null && handIndex >= 0 && handIndex < MatchState.PlayerHand.Count)
                {
                    var card = MatchState.PlayerHand[handIndex];
                    string typeBadge = $"[{card.Type.ToString().ToUpper()}]";
                    string abilityPreview = !string.IsNullOrEmpty(card.Ability?.Description) 
                        ? $"— <i>{card.Ability.Description}</i>" 
                        : "";

                    if (MatchState.PlayerEnergy >= card.EnergyCost)
                    {
                        headerView.SetStatusNotification($"<b>{card.Name}</b> {typeBadge} (Costo: {card.EnergyCost} | Poder: {card.Power}) {abilityPreview}", new Color(0.35f, 0.95f, 0.45f));
                    }
                    else
                    {
                        headerView.SetStatusNotification($"<b>{card.Name}</b> {typeBadge} requiere {card.EnergyCost} Energía (Disponibles: {MatchState.PlayerEnergy}) {abilityPreview}", new Color(1f, 0.65f, 0.25f));
                    }
                }
            }

            RefreshAllUI();
        }

        private void HandleDeployToLane(int laneIndex)
        {
            if (MatchState == null || MatchState.IsGameOver || MatchState.IsMulliganPhase || isResolvingTurn) return;

            if (MatchState.SelectedHandIndex.HasValue)
            {
                int hIdx = MatchState.SelectedHandIndex.Value;
                var card = (hIdx >= 0 && hIdx < MatchState.PlayerHand.Count) ? MatchState.PlayerHand[hIdx] : null;
                bool success = ArenaBattleEngine.PlayPlayerCard(MatchState, hIdx, laneIndex, out string error);
                if (!success && !string.IsNullOrEmpty(error))
                {
                    if (headerView != null)
                    {
                        headerView.SetStatusNotification($"{error}", new Color(1f, 0.4f, 0.4f));
                    }
                }
                else if (success)
                {
                    if (headerView != null)
                    {
                        headerView.SetStatusNotification($"¡<b>{card?.Name}</b> desplegada! Te quedan {MatchState.PlayerEnergy} de energía. Puedes jugar otra o finalizar turno.", new Color(0.35f, 0.95f, 0.45f));
                    }
                }
                RefreshAllUI();
            }
            else
            {
                if (headerView != null)
                {
                    headerView.SetStatusNotification("Primero toca una carta en tu mano y luego selecciona la arena donde desplegarla.", new Color(1f, 0.85f, 0.3f));
                }
            }
        }

        private void HandleUndoSlot(int laneIndex, int slotIndex)
        {
            if (MatchState == null || MatchState.IsGameOver || MatchState.IsMulliganPhase || isResolvingTurn) return;

            bool success = ArenaBattleEngine.UndoPlayerPlay(MatchState, laneIndex, slotIndex);
            if (success)
            {
                if (headerView != null)
                {
                    headerView.SetStatusNotification($"Carta recuperada a tu mano. Energía reembolsada ({MatchState.PlayerEnergy} disponible).", new Color(1f, 0.85f, 0.3f));
                }
                RefreshAllUI();
            }
        }

        private void HandleEndTurn()
        {
            if (MatchState == null || MatchState.IsGameOver || MatchState.IsMulliganPhase || isResolvingTurn) return;

            StartCoroutine(ResolveTurnSequenceCoroutine());
        }

        /// <summary>
        /// Asynchronously executes the 8-Step Resolution Order (Rule 18) with animated feedback.
        /// </summary>
        private IEnumerator ResolveTurnSequenceCoroutine()
        {
            isResolvingTurn = true;
            if (headerView != null)
            {
                headerView.SetEndTurnInteractable(false);
                headerView.SetStatusNotification("Paso 1: ¡Revelando jugadas del rival!", Color.yellow);
            }

            // Step 1: Reveal opponent cards
            for (int l = 0; l < ArenaBattleMatchState.MaxLanes; l++)
            {
                foreach (var aiCard in MatchState.AiLaneCards[l])
                {
                    aiCard.IsRevealed = true;
                }
            }
            RefreshAllUI();
            yield return new WaitForSeconds(0.6f);

            // Step 2 & 3: Resolve abilities and triggers
            if (headerView != null)
            {
                headerView.SetStatusNotification("Pasos 2-5: ¡Resolviendo habilidades de combate!", Color.cyan);
            }

            ArenaBattleEngine.EndTurn(MatchState);
            RefreshAllUI();

            // Display key resolution log message if any
            if (MatchState.ResolutionLog != null && MatchState.ResolutionLog.Count > 0)
            {
                string logSummary = string.Join(" ", MatchState.ResolutionLog.Take(2));
                if (headerView != null)
                {
                    headerView.SetStatusNotification(logSummary, new Color(0.4f, 0.95f, 0.6f));
                }
                yield return new WaitForSeconds(0.9f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            isResolvingTurn = false;

            if (MatchState.IsGameOver)
            {
                HandleGameOver();
            }
            else
            {
                if (headerView != null)
                {
                    headerView.SetEndTurnInteractable(true);
                    headerView.SetStatusNotification($"Turno {MatchState.CurrentTurn} de {MatchState.MaxTurn}: Tienes {MatchState.PlayerEnergy} de energía.", new Color(0.9f, 0.95f, 1f));
                }
                RefreshAllUI();
            }
        }

        private void HandleGameOver()
        {
            int goldEarned = 0;
            int xpEarned = 0;

            switch (MatchState.Winner)
            {
                case BattleWinner.Player:
                    goldEarned = 150;
                    xpEarned = 50;
                    break;
                case BattleWinner.Tie:
                    goldEarned = 75;
                    xpEarned = 25;
                    break;
                case BattleWinner.Ai:
                default:
                    goldEarned = 35;
                    xpEarned = 10;
                    break;
            }

            try
            {
                var profile = SaveManager.Profile;
                if (profile != null)
                {
                    profile.AddGoldCoins(goldEarned);
                    profile.AddAccountExperience(xpEarned, out _, out _, out _);
                    SaveManager.Save(profile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BattleManager] Error saving match rewards: {ex.Message}");
            }

            if (gameOverModal != null)
            {
                gameOverModal.Show(MatchState, goldEarned, xpEarned, StartNewMatch, HandleReturnHome);
            }
        }

        private void HandleOpenHelp()
        {
            if (helpModal != null) helpModal.Show();
        }

        private void HandleRestartMatch()
        {
            StartNewMatch();
        }

        private void HandleReturnHome()
        {
            SceneManager.LoadScene("Home");
        }

        /// <summary>
        /// Randomly picks one of the 6 official arena backgrounds and displays it on the canvas background.
        /// </summary>
        public void SetupRandomArenaBackground(System.Random rng = null)
        {
            if (battleBackgroundImage == null)
            {
                var bgObj = GameObject.Find("Background");
                if (bgObj != null)
                {
                    battleBackgroundImage = bgObj.GetComponent<Image>();
                }
            }

            if (battleBackgroundImage != null)
            {
                string bgName = ArenaBackgroundProvider.GetRandomBackgroundName(rng);
                var sprite = ArenaBackgroundProvider.GetBackgroundSprite(bgName);
                if (sprite != null)
                {
                    battleBackgroundImage.sprite = sprite;
                    battleBackgroundImage.color = Color.white;
                    battleBackgroundImage.preserveAspect = false;
                    CurrentArenaBackgroundName = bgName;
                    Debug.Log($"<color=#30d158>[BattleManager] Arena background set to: {ArenaBackgroundProvider.GetDisplayName(bgName)} ({bgName})</color>");
                }
            }
        }

        public void RefreshAllUI()
        {
            if (MatchState == null) return;

            // 1. Header
            if (headerView != null)
            {
                headerView.UpdateHeader(
                    MatchState.CurrentTurn,
                    MatchState.MaxTurn,
                    MatchState.PlayerEnergy,
                    MatchState.PlayerMaxEnergy,
                    MatchState.PlayerLanesWon,
                    MatchState.AiLanesWon
                );
            }

            // 2. Lanes
            CardDefinition selectedCard = null;
            if (MatchState.SelectedHandIndex.HasValue && 
                MatchState.SelectedHandIndex.Value >= 0 && 
                MatchState.SelectedHandIndex.Value < MatchState.PlayerHand.Count)
            {
                selectedCard = MatchState.PlayerHand[MatchState.SelectedHandIndex.Value];
            }

            for (int i = 0; i < laneViews.Length; i++)
            {
                if (laneViews[i] != null && i < MatchState.Arenas.Length)
                {
                    laneViews[i].UpdateLane(
                        MatchState.Arenas[i],
                        MatchState.PlayerLanes[i],
                        MatchState.AiLanes[i],
                        MatchState.PlayerTurnPlays,
                        selectedCard,
                        MatchState.PlayerEnergy,
                        MatchState.PlayerLaneCards[i],
                        MatchState.AiLaneCards[i]
                    );
                }
            }

            // 3. Hand
            if (handView != null)
            {
                handView.RefreshHand(
                    MatchState.PlayerHand,
                    MatchState.SelectedHandIndex,
                    MatchState.PlayerEnergy
                );
            }
        }
    }
}
