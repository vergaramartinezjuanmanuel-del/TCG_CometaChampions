using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Controller managing the player's bottom hand tray.
    /// Instantiates or recycles BattleHandCardView items and handles selection.
    /// </summary>
    public class BattleHandView : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private BattleHandCardView cardPrefab;
        [SerializeField] private GameObject emptyHandLabel;

        public event Action<int> OnCardSelected;

        private readonly List<BattleHandCardView> spawnedCardViews = new();

        private void Awake()
        {
            InitializeViews();
        }

        private void InitializeViews()
        {
            if (cardsContainer != null && spawnedCardViews.Count == 0)
            {
                var existing = cardsContainer.GetComponentsInChildren<BattleHandCardView>(true);
                foreach (var v in existing)
                {
                    if (v != null && !spawnedCardViews.Contains(v))
                    {
                        spawnedCardViews.Add(v);
                    }
                }
            }
        }

        public void RefreshHand(
            IReadOnlyList<CardDefinition> handCards,
            int? selectedIndex,
            int playerEnergy)
        {
            InitializeViews();
            int cardCount = handCards?.Count ?? 0;

            if (emptyHandLabel != null)
            {
                emptyHandLabel.SetActive(cardCount == 0);
            }

            // Ensure we have enough child views in the pool
            while (spawnedCardViews.Count < cardCount)
            {
                BattleHandCardView view = null;
                if (cardPrefab != null && cardsContainer != null)
                {
                    view = Instantiate(cardPrefab, cardsContainer);
                }
                else if (cardsContainer != null && spawnedCardViews.Count > 0)
                {
                    view = Instantiate(spawnedCardViews[0], cardsContainer);
                }

                if (view != null)
                {
                    spawnedCardViews.Add(view);
                }
                else
                {
                    break;
                }
            }

            // Setup each active card
            for (int i = 0; i < spawnedCardViews.Count; i++)
            {
                if (i < cardCount)
                {
                    var card = handCards[i];
                    bool isSelected = selectedIndex.HasValue && selectedIndex.Value == i;
                    bool isAffordable = playerEnergy >= card.EnergyCost;

                    spawnedCardViews[i].gameObject.SetActive(true);
                    spawnedCardViews[i].Setup(card, i, isSelected, isAffordable, HandleCardClicked);
                }
                else
                {
                    spawnedCardViews[i].gameObject.SetActive(false);
                }
            }
        }

        public void RegisterSpawnedView(BattleHandCardView view)
        {
            if (view != null && !spawnedCardViews.Contains(view))
            {
                spawnedCardViews.Add(view);
            }
        }

        private void HandleCardClicked(int handIndex)
        {
            OnCardSelected?.Invoke(handIndex);
        }
    }
}
