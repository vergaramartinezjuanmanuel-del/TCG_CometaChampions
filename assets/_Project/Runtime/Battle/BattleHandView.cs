using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Controller managing the player's bottom hand tray.
    /// Arranges playable cards into a rounded fan arch (forma redondeada) matching the official UI redesign,
    /// and instantiates or recycles BattleHandCardView items.
    /// </summary>
    public class BattleHandView : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private BattleHandCardView cardPrefab;
        [SerializeField] private GameObject emptyHandLabel;

        [Header("Curved Fan Settings")]
        [SerializeField] private float cardSpacing = 105f;
        [SerializeField] private float fanArchHeight = 28f;
        [SerializeField] private float maxFanAngle = 8.5f;
        [SerializeField] private float selectedElevation = 32f;

        public event Action<int> OnCardSelected;

        private readonly List<BattleHandCardView> spawnedCardViews = new();

        private void Awake()
        {
            InitializeViews();
        }

        private void InitializeViews()
        {
            // 1. Setup Tray Background with official cards_holder sprite
            var trayImg = GetComponent<Image>();
            if (trayImg != null)
            {
                var holderSprite = Resources.Load<Sprite>("UI/cards_holder");
                if (holderSprite != null)
                {
                    trayImg.sprite = holderSprite;
                    trayImg.color = Color.white;
                    trayImg.preserveAspect = false;
                }
            }

            // 2. Configure CardsContainer: disable automatic HorizontalLayoutGroup to allow curved fan arc
            if (cardsContainer != null)
            {
                var layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                }

                var cRect = cardsContainer.GetComponent<RectTransform>();
                if (cRect != null)
                {
                    cRect.anchorMin = new Vector2(0.5f, 0f);
                    cRect.anchorMax = new Vector2(0.5f, 0f);
                    cRect.pivot = new Vector2(0.5f, 0f);
                    cRect.anchoredPosition = new Vector2(0f, 15f);
                    cRect.sizeDelta = new Vector2(1000f, 200f);
                }

                if (spawnedCardViews.Count == 0)
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

            // Setup and position each active card in the rounded fan arc
            for (int i = 0; i < spawnedCardViews.Count; i++)
            {
                if (i < cardCount)
                {
                    var card = handCards[i];
                    bool isSelected = selectedIndex.HasValue && selectedIndex.Value == i;
                    bool isAffordable = playerEnergy >= card.EnergyCost;

                    var cardView = spawnedCardViews[i];
                    cardView.gameObject.SetActive(true);
                    cardView.Setup(card, i, isSelected, isAffordable, HandleCardClicked);

                    // Apply mathematical rounded fan transform
                    var rect = cardView.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        var t = HandLayoutCalculator.CalculateTransform(
                            i,
                            cardCount,
                            isSelected,
                            cardSpacing,
                            fanArchHeight,
                            maxFanAngle,
                            selectedElevation
                        );

                        rect.anchoredPosition = new Vector2(t.X, t.Y);
                        rect.localEulerAngles = new Vector3(0f, 0f, t.ZAngle);
                        rect.localScale = new Vector3(t.Scale, t.Scale, 1f);
                    }
                }
                else
                {
                    spawnedCardViews[i].gameObject.SetActive(false);
                }
            }

            // Maintain natural left-to-right overlapping hierarchy, bringing selected card to the front
            for (int i = 0; i < cardCount; i++)
            {
                if (!selectedIndex.HasValue || selectedIndex.Value != i)
                {
                    spawnedCardViews[i].transform.SetSiblingIndex(i);
                }
            }

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < cardCount)
            {
                spawnedCardViews[selectedIndex.Value].transform.SetAsLastSibling();
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
