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

        public const string DeckRulesSubtitleText =
            "Exactamente 12 cartas de tu inventario (máx 2 copias por carta; agregar 2da copia cuesta 50 de oro).\n" +
            "Para agregar cartas manualmente, selecciona cada tarjeta desde 'Mis Cartas' y agrégala al mazo.\n" +
            "'Auto-Completar' solo añade cartas desbloqueadas al azar sin duplicadas.";

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

            UpdateSubtitleNote();
        }

        private void UpdateSubtitleNote()
        {
            var subtitleTransform = transform.Find("Subtitle");
            if (subtitleTransform != null)
            {
                var subtitleTmp = subtitleTransform.GetComponent<TextMeshProUGUI>();
                if (subtitleTmp != null)
                {
                    subtitleTmp.text = DeckRulesSubtitleText;
                    subtitleTmp.fontSize = 22f;
                    subtitleTmp.textWrappingMode = TextWrappingModes.Normal;
                }

                var rt = subtitleTransform.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, -213f);
                    rt.sizeDelta = new Vector2(1100f, 120f);
                }
            }

            if (cardsGridParent != null)
            {
                var gridRt = cardsGridParent.GetComponent<RectTransform>();
                if (gridRt != null)
                {
                    gridRt.anchoredPosition = new Vector2(0f, 80f);
                    gridRt.sizeDelta = new Vector2(1044f, 440f);
                }

                var gridLayout = cardsGridParent.GetComponent<GridLayoutGroup>();
                if (gridLayout != null)
                {
                    gridLayout.cellSize = new Vector2(150f, 210f);
                    gridLayout.spacing = new Vector2(20f, 16f);
                }
            }
        }

        public void Show(PlayerProfile profile, Action onDeckChanged = null)
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
            currentProfile = profile ?? SaveManager.Profile;
            onDeckChangedCallback = onDeckChanged;

            UpdateSubtitleNote();
            EnsureValidDeckExists();

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            RefreshGrid();
        }

        public void Hide()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalClose);
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

            // Ensure first deck exists (initialized empty so starter cards are not auto-added)
            if (currentProfile.SavedDecks.Count == 0)
            {
                var newDeck = new SavedDeckData("Mazo de Combate", ClanType.None, Array.Empty<string>());
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
            rect.sizeDelta = new Vector2(150, 210);

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
            remImg.sprite = Resources.Load<Sprite>("UI/cerrar");
            //remImg.color = new Color(0.6f, 0.1f, 0.15f, 0.95f);

            var remBtn = remObj.AddComponent<Button>();
            remBtn.onClick.AddListener(() => RemoveCardAt(indexInDeck));

            // var remTextObj = new GameObject("RemLabel", typeof(RectTransform));
            // remTextObj.transform.SetParent(remObj.transform, false);
            // var remTextRect = remTextObj.GetComponent<RectTransform>();
            // remTextRect.anchorMin = Vector2.zero;
            // remTextRect.anchorMax = Vector2.one;
            // remTextRect.offsetMin = Vector2.zero;
            // remTextRect.offsetMax = Vector2.zero;

            // var remTMP = remTextObj.AddComponent<TextMeshProUGUI>();
            // remTMP.text = "X";
            // remTMP.fontSize = 14;
            // remTMP.fontStyle = FontStyles.Bold;
            // remTMP.alignment = TextAlignmentOptions.Center;
            // remTMP.color = Color.white;

            return slot;
        }

        private void RemoveCardAt(int index)
        {
            var activeDeck = GetActiveDeck();
            if (activeDeck == null || activeDeck.CardIds == null || index < 0 || index >= activeDeck.CardIds.Count) return;

            AudioManager.Instance.PlaySfx(SfxType.RemoveCardFromDeck);
            activeDeck.CardIds.RemoveAt(index);
            SaveManager.Save();

            RefreshGrid();
            onDeckChangedCallback?.Invoke();
        }

        private void HandleAutoCompleteClicked()
        {
            if (currentProfile == null) return;

            var activeDeck = GetActiveDeck();
            if (activeDeck == null) return;
            activeDeck.CardIds ??= new List<string>();

            AudioManager.Instance.PlaySfx(SfxType.AutoCompleteDeck);
            int added = DeckValidator.AutoCompleteDeckWithoutDuplicates(currentProfile, activeDeck.CardIds);
            if (added > 0)
            {
                SaveManager.Save();
            }

            RefreshGrid();
            onDeckChangedCallback?.Invoke();
        }
    }
}
