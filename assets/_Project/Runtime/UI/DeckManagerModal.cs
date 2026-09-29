using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Core.Rules;
using CometaChampions.Runtime.Cards;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Interactive Deck Manager modal in the Home scene allowing players to view,
    /// adjust, and auto-complete their 12-card combat deck strictly from their owned inventory.
    /// </summary>
    public class DeckManagerModal : MonoBehaviour
    {
        [Header("Modal UI")]
        [SerializeField] private GameObject modalRoot = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Button autoCompleteButton = null!;
        [SerializeField] private TextMeshProUGUI deckCountText = null!;
        [SerializeField] private Transform cardsGridParent = null!;

        private PlayerProfile currentProfile;
        private Action onDeckChangedCallback;
        private readonly List<GameObject> spawnedSlotObjects = new();

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (autoCompleteButton != null)
            {
                autoCompleteButton.onClick.AddListener(HandleAutoCompleteClicked);
            }
        }

        public void Show(PlayerProfile profile, Action onDeckChanged = null)
        {
            currentProfile = profile ?? SaveManager.Profile;
            onDeckChangedCallback = onDeckChanged;

            EnsureValidDeckExists();

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            RefreshGrid();
        }

        public void Hide()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void EnsureValidDeckExists()
        {
            if (currentProfile == null) return;

            if (currentProfile.SavedDecks == null)
            {
                currentProfile.SavedDecks = new List<SavedDeckData>();
            }

            // Ensure first deck exists
            if (currentProfile.SavedDecks.Count == 0)
            {
                var autoDeck = DeckValidator.CreateDeckFromInventory(currentProfile);
                var newDeck = new SavedDeckData("Mazo de Combate", ClanType.None, autoDeck.Select(c => c.Id));
                currentProfile.SavedDecks.Add(newDeck);
                SaveManager.Save();
            }
        }

        public SavedDeckData GetActiveDeck()
        {
            EnsureValidDeckExists();
            return currentProfile?.SavedDecks?.FirstOrDefault();
        }

        public void RefreshGrid()
        {
            var activeDeck = GetActiveDeck();
            if (activeDeck == null) return;

            // Clear old spawned slots
            foreach (var slot in spawnedSlotObjects)
            {
                if (slot != null) Destroy(slot);
            }
            spawnedSlotObjects.Clear();

            int cardCount = activeDeck.CardIds != null ? activeDeck.CardIds.Count : 0;
            if (deckCountText != null)
            {
                deckCountText.text = $"CARTAS: {cardCount} / {DeckValidator.ArenaRequiredDeckSize}";
                deckCountText.color = cardCount == DeckValidator.ArenaRequiredDeckSize
                    ? new Color(0.2f, 1f, 0.4f)
                    : new Color(1f, 0.85f, 0.2f);
            }

            if (cardsGridParent == null || activeDeck.CardIds == null) return;

            for (int i = 0; i < activeDeck.CardIds.Count; i++)
            {
                int cardIndex = i;
                string cardId = activeDeck.CardIds[i];
                var cardDef = MvpCardCatalog.GetCardById(cardId) ?? CardCatalog.GetCardById(cardId);
                if (cardDef == null) continue;

                var slotObj = CreateCardSlotUI(cardDef, cardIndex);
                if (slotObj != null)
                {
                    spawnedSlotObjects.Add(slotObj);
                }
            }
        }

        private GameObject CreateCardSlotUI(CardDefinition card, int indexInDeck)
        {
            var slot = new GameObject($"DeckSlot_{indexInDeck}_{card.Id}", typeof(RectTransform));
            slot.transform.SetParent(cardsGridParent, false);

            var rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110, 160);

            // Background card panel
            var bgImg = slot.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.20f, 0.95f);

            // Full dynamic card artwork container
            var artObj = new GameObject("Artwork", typeof(RectTransform));
            artObj.transform.SetParent(slot.transform, false);
            var artRect = artObj.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            DynamicCardAssembler.AssembleCard(card, artRect);

            // Remove button (Top-Right 'X')
            var remObj = new GameObject("RemoveBtn", typeof(RectTransform));
            remObj.transform.SetParent(slot.transform, false);
            var remRect = remObj.GetComponent<RectTransform>();
            remRect.anchorMin = new Vector2(1, 1);
            remRect.anchorMax = new Vector2(1, 1);
            remRect.pivot = new Vector2(1, 1);
            remRect.anchoredPosition = new Vector2(-2, -2);
            remRect.sizeDelta = new Vector2(28, 28);

            var remImg = remObj.AddComponent<Image>();
            remImg.color = new Color(0.6f, 0.1f, 0.15f, 0.95f);

            var remBtn = remObj.AddComponent<Button>();
            remBtn.onClick.AddListener(() => RemoveCardAt(indexInDeck));

            var remTextObj = new GameObject("RemLabel", typeof(RectTransform));
            remTextObj.transform.SetParent(remObj.transform, false);
            var remTextRect = remTextObj.GetComponent<RectTransform>();
            remTextRect.anchorMin = Vector2.zero;
            remTextRect.anchorMax = Vector2.one;
            remTextRect.offsetMin = Vector2.zero;
            remTextRect.offsetMax = Vector2.zero;

            var remTMP = remTextObj.AddComponent<TextMeshProUGUI>();
            remTMP.text = "X";
            remTMP.fontSize = 14;
            remTMP.fontStyle = FontStyles.Bold;
            remTMP.alignment = TextAlignmentOptions.Center;
            remTMP.color = Color.white;

            return slot;
        }

        private void RemoveCardAt(int index)
        {
            var activeDeck = GetActiveDeck();
            if (activeDeck == null || activeDeck.CardIds == null || index < 0 || index >= activeDeck.CardIds.Count) return;

            activeDeck.CardIds.RemoveAt(index);
            SaveManager.Save();

            RefreshGrid();
            onDeckChangedCallback?.Invoke();
        }

        private void HandleAutoCompleteClicked()
        {
            if (currentProfile == null) return;

            var autoDeck = DeckValidator.CreateDeckFromInventory(currentProfile);
            var activeDeck = GetActiveDeck();
            if (activeDeck != null && autoDeck != null && autoDeck.Count == DeckValidator.ArenaRequiredDeckSize)
            {
                activeDeck.CardIds = autoDeck.Select(c => c.Id).ToList();
                SaveManager.Save();

                RefreshGrid();
                onDeckChangedCallback?.Invoke();
            }
        }
    }
}
