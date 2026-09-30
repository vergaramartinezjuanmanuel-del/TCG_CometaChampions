using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Visual and interactive representation of an Arena Lane (1 of 3 columns).
    /// Features:
    /// - Central Arena Header (Banner, name, effect, power score clash meter).
    /// - Top Zone: 4 AI card slots.
    /// - Bottom Zone: 4 Player card slots with undo support.
    /// - Interactive Deploy button when a hand card is selected.
    /// </summary>
    public class BattleLaneView : MonoBehaviour
    {
        [Header("Lane Index")]
        [SerializeField] private int laneIndex;

        [Header("Arena Center")]
        [SerializeField] private Image arenaBackground;
        [SerializeField] private TextMeshProUGUI arenaNameText;
        [SerializeField] private TextMeshProUGUI arenaClanText;
        [SerializeField] private TextMeshProUGUI arenaEffectText;
        [SerializeField] private TextMeshProUGUI playerPowerText;
        [SerializeField] private TextMeshProUGUI aiPowerText;
        [SerializeField] private GameObject playerLeaderGlow;
        [SerializeField] private GameObject aiLeaderGlow;
        [SerializeField] private TextMeshProUGUI laneStatusText;
        [SerializeField] private Button deployButton;
        [SerializeField] private TextMeshProUGUI deployButtonText;
        [SerializeField] private Image deployButtonImage;
        [SerializeField] private Button columnClickButton;

        [Header("Card Slots")]
        [SerializeField] private BattleCardSlotView[] aiSlots = new BattleCardSlotView[4];
        [SerializeField] private BattleCardSlotView[] playerSlots = new BattleCardSlotView[4];

        public int LaneIndex => laneIndex;

        public event Action<int> OnDeployClicked;
        public event Action<int, int> OnUndoSlotClicked;

        private int lastLeaderState = 0; // 0 = Tie, 1 = Player, -1 = AI

        private void Awake()
        {
            if (deployButton != null)
            {
                deployButton.onClick.AddListener(() =>
                {
                    CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
                    OnDeployClicked?.Invoke(laneIndex);
                });
                if (deployButtonImage == null) deployButtonImage = deployButton.GetComponent<Image>();
                if (deployButtonText == null) deployButtonText = deployButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (columnClickButton != null)
            {
                columnClickButton.onClick.AddListener(() =>
                {
                    CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
                    OnDeployClicked?.Invoke(laneIndex);
                });
            }
        }

        public void SetupArena(CardDefinition arena)
        {
            if (arena == null) return;

            if (arenaNameText != null)
            {
                arenaNameText.text = arena.Name;
            }

            if (arenaClanText != null)
            {
                if (arena.Clan != ClanType.None && arena.Clan != ClanType.Neutral)
                {
                    arenaClanText.text = $"{arena.Clan}";
                    arenaClanText.gameObject.SetActive(true);
                }
                else
                {
                    arenaClanText.gameObject.SetActive(false);
                }
            }

            if (arenaEffectText != null)
            {
                arenaEffectText.text = !string.IsNullOrEmpty(arena.AbilityText)
                    ? arena.AbilityText
                    : (!string.IsNullOrEmpty(arena.LoreText) ? arena.LoreText : "Arena oficial de combate");
            }

            if (arenaBackground != null)
            {
                var sprite = Card3D.GetFlatArtSprite(arena);
                if (sprite != null)
                {
                    arenaBackground.sprite = sprite;
                    arenaBackground.color = new Color(0.35f, 0.40f, 0.50f, 0.9f);
                }
            }
        }

        public void UpdateLane(
            CardDefinition arena,
            IReadOnlyList<CardDefinition> playerCards,
            IReadOnlyList<CardDefinition> aiCards,
            IReadOnlyList<TurnPlayAction> playerTurnPlays,
            CardDefinition selectedCard,
            int playerEnergy,
            IReadOnlyList<LaneCardInstance> playerInstances = null,
            IReadOnlyList<LaneCardInstance> aiInstances = null)
        {
            int pCount = playerInstances?.Count ?? playerCards?.Count ?? 0;
            int aCount = aiInstances?.Count ?? aiCards?.Count ?? 0;

            // 1. Calculate Powers
            int pPower = (playerInstances != null && aiInstances != null)
                ? ArenaPowerCalculator.CalculateLanePower(playerInstances, arena, aiInstances)
                : ArenaPowerCalculator.CalculateLanePower(playerCards, arena, aiCards);

            int aPower = (playerInstances != null && aiInstances != null)
                ? ArenaPowerCalculator.CalculateLanePower(aiInstances, arena, playerInstances)
                : ArenaPowerCalculator.CalculateLanePower(aiCards, arena, playerCards);

            // 2. Update Meters & Liderazgo
            if (playerPowerText != null) playerPowerText.text = pPower.ToString();
            if (aiPowerText != null) aiPowerText.text = aPower.ToString();

            bool playerLeads = pPower > aPower;
            bool aiLeads = aPower > pPower;
            int currentLeaderState = playerLeads ? 1 : (aiLeads ? -1 : 0);
            if (currentLeaderState != lastLeaderState && (pPower > 0 || aPower > 0))
            {
                lastLeaderState = currentLeaderState;
                var sfx = currentLeaderState == 1
                    ? SfxType.LaneLeaderPlayer
                    : (currentLeaderState == -1 ? SfxType.LaneLeaderAi : SfxType.LaneLeaderTie);
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(sfx);
            }

            if (playerLeaderGlow != null) playerLeaderGlow.SetActive(playerLeads);
            if (aiLeaderGlow != null) aiLeaderGlow.SetActive(aiLeads);

            if (laneStatusText != null)
            {
                if (playerLeads)
                {
                    laneStatusText.text = "¡Lideras esta zona!";
                    laneStatusText.color = new Color(0.2f, 0.85f, 0.35f, 1f); // Vibrant green
                }
                else if (aiLeads)
                {
                    laneStatusText.text = "Rival en ventaja";
                    laneStatusText.color = new Color(1f, 0.35f, 0.3f, 1f); // Vibrant red
                }
                else
                {
                    laneStatusText.text = "Empate";
                    laneStatusText.color = new Color(0.9f, 0.85f, 0.3f, 1f); // Gold
                }
            }

            // 3. Update Deploy button
            if (deployButton != null)
            {
                if (selectedCard != null && pCount < ArenaBattleMatchState.MaxCardsPerLane)
                {
                    deployButton.gameObject.SetActive(true);
                    bool isAffordable = playerEnergy >= selectedCard.EnergyCost;

                    if (deployButtonText != null)
                    {
                        deployButtonText.text = isAffordable 
                            ? $"+ Desplegar ({selectedCard.EnergyCost})" 
                            : $"Requiere {selectedCard.EnergyCost} Energía";
                    }

                    if (deployButtonImage != null)
                    {
                        deployButtonImage.color = isAffordable
                            ? new Color(0.12f, 0.65f, 0.35f) // Emerald green
                            : new Color(0.55f, 0.20f, 0.20f); // Muted warning red
                    }
                }
                else
                {
                    deployButton.gameObject.SetActive(false);
                }
            }

            // 4. Update AI slots (0 to 3)
            for (int i = 0; i < aiSlots.Length; i++)
            {
                if (aiSlots[i] == null) continue;

                if (aiInstances != null && i < aiInstances.Count)
                {
                    var inst = aiInstances[i];
                    bool isHidden = !inst.IsRevealed;
                    int effPower = isHidden ? 0 : ArenaPowerCalculator.CalculateInstanceEffectivePower(inst, arena, aiInstances, playerInstances);
                    aiSlots[i].Setup(inst.Card, effPower, canUndo: false, laneIndex, i, null, isFaceDown: isHidden);
                }
                else if (aiCards != null && i < aiCards.Count)
                {
                    var card = aiCards[i];
                    int effPower = ArenaPowerCalculator.CalculateCardEffectivePower(card, arena, playerCards);
                    aiSlots[i].Setup(card, effPower, canUndo: false, laneIndex, i, null, isFaceDown: false);
                }
                else
                {
                    aiSlots[i].Setup(null, 0, canUndo: false, laneIndex, i, null);
                }
            }

            // 5. Update Player slots (0 to 3)
            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] == null) continue;

                if (playerInstances != null && i < playerInstances.Count)
                {
                    var inst = playerInstances[i];
                    int effPower = ArenaPowerCalculator.CalculateInstanceEffectivePower(inst, arena, playerInstances, aiInstances);
                    bool canUndo = playerTurnPlays != null &&
                        playerTurnPlays.Count > 0 &&
                        System.Linq.Enumerable.Any(playerTurnPlays, p => p.Card == inst.Card && p.LaneIndex == laneIndex);

                    playerSlots[i].Setup(inst.Card, effPower, canUndo, laneIndex, i, HandleUndoSlotClicked, isFaceDown: false);
                }
                else if (playerCards != null && i < playerCards.Count)
                {
                    var card = playerCards[i];
                    int effPower = ArenaPowerCalculator.CalculateCardEffectivePower(card, arena, aiCards);
                    bool canUndo = playerTurnPlays != null &&
                        playerTurnPlays.Count > 0 &&
                        System.Linq.Enumerable.Any(playerTurnPlays, p => p.Card == card && p.LaneIndex == laneIndex);

                    playerSlots[i].Setup(card, effPower, canUndo, laneIndex, i, HandleUndoSlotClicked, isFaceDown: false);
                }
                else
                {
                    // Empty slot: clicking it triggers deploy to this lane if a card is selected
                    playerSlots[i].Setup(null, 0, canUndo: false, laneIndex, i, HandleEmptySlotClicked);
                }
            }
        }

        private void HandleEmptySlotClicked(int lIdx, int sIdx)
        {
            OnDeployClicked?.Invoke(laneIndex);
        }

        private void HandleUndoSlotClicked(int lIdx, int sIdx)
        {
            OnUndoSlotClicked?.Invoke(lIdx, sIdx);
        }
    }
}
