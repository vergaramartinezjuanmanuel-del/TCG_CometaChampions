#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Interactive Mulligan Modal according to Rule 2:
    /// - Displays the 4-card starting hand before Turn 1.
    /// - Allows selecting any number of cards (0 to 4) to replace.
    /// - Animates replaced cards with a 3D spin flip effect, showing the newly drawn cards.
    /// - Waits a brief preview duration before closing to begin Turn 1.
    /// </summary>
    public class BattleMulliganModal : MonoBehaviour
    {
        [Header("Modal UI")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Starting Hand Card Slots (4)")]
        [SerializeField] private Button[] cardButtons = Array.Empty<Button>();
        [SerializeField] private Image[] cardArtImages = Array.Empty<Image>();
        [SerializeField] private TextMeshProUGUI[] cardNameTexts = Array.Empty<TextMeshProUGUI>();
        [SerializeField] private TextMeshProUGUI[] cardCostTexts = Array.Empty<TextMeshProUGUI>();
        [SerializeField] private GameObject[] cardSelectedOverlays = Array.Empty<GameObject>();

        [Header("Action Buttons")]
        [SerializeField] private Button keepAllButton = null!;
        [SerializeField] private Button confirmMulliganButton = null!;
        [SerializeField] private TextMeshProUGUI confirmButtonText = null!;

        [Header("Animation Settings")]
        [SerializeField] private float cardSpinDuration = 0.44f;
        [SerializeField] private float cardStaggerDelay = 0.12f;
        [SerializeField] private float previewDurationSeconds = 2.0f;

        private readonly HashSet<int> selectedIndices = new();
        private IReadOnlyList<CardDefinition>? currentHand;
        private bool isAnimating = false;

        public event Action<List<int>>? OnMulliganConfirmed;

        private void Awake()
        {
            if (keepAllButton != null)
            {
                keepAllButton.onClick.AddListener(HandleKeepAllClicked);
            }

            if (confirmMulliganButton != null)
            {
                confirmMulliganButton.onClick.AddListener(HandleConfirmClicked);
            }

            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;
                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.AddListener(() => ToggleCardSelection(index));
                }
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isAnimating = false;
            ResetSlotTransforms();
        }

        public void Show(IReadOnlyList<CardDefinition> startingHand)
        {
            StopAllCoroutines();
            isAnimating = false;
            currentHand = startingHand;
            selectedIndices.Clear();

            ResetSlotTransforms();
            SetButtonsInteractable(true);

            if (modalRoot != null) modalRoot.SetActive(true);
            else gameObject.SetActive(true);

            if (titleText != null) titleText.text = "FASE DE MULLIGAN";
            if (subtitleText != null)
            {
                subtitleText.text = "Toca las cartas que deseas cambiar de tu mano inicial (0 a 4) y presiona confirmar, o conserva tu mano.";
            }

            RefreshCardViews();
            UpdateConfirmButtonState();
        }

        public void Hide()
        {
            StopAllCoroutines();
            isAnimating = false;
            ResetSlotTransforms();

            if (modalRoot != null) modalRoot.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void ResetSlotTransforms()
        {
            if (cardButtons == null) return;
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].transform.localRotation = Quaternion.identity;
                    cardButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (keepAllButton != null) keepAllButton.interactable = interactable;
            if (confirmMulliganButton != null) confirmMulliganButton.interactable = interactable;

            if (cardButtons != null)
            {
                for (int i = 0; i < cardButtons.Length; i++)
                {
                    if (cardButtons[i] != null) cardButtons[i].interactable = interactable;
                }
            }
        }

        private void ToggleCardSelection(int index)
        {
            if (isAnimating) return;
            if (index < 0 || currentHand == null || index >= currentHand.Count) return;

            if (selectedIndices.Contains(index))
            {
                selectedIndices.Remove(index);
            }
            else
            {
                selectedIndices.Add(index);
            }

            RefreshCardViews();
            UpdateConfirmButtonState();
        }

        private void RefreshCardViews()
        {
            if (cardButtons == null) return;

            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (currentHand != null && i < currentHand.Count)
                {
                    var card = currentHand[i];
                    if (cardButtons[i] != null) cardButtons[i].gameObject.SetActive(true);

                    if (cardArtImages != null && i < cardArtImages.Length && cardArtImages[i] != null)
                    {
                        var sprite = Card3D.GetCardThumbnail(card) ?? Card3D.GetReversoThumbnail("oro");
                        cardArtImages[i].sprite = sprite;
                        cardArtImages[i].color = sprite != null ? Color.white : Color.clear;
                    }

                    if (cardNameTexts != null && i < cardNameTexts.Length && cardNameTexts[i] != null)
                    {
                        cardNameTexts[i].text = card.Name;
                    }

                    if (cardCostTexts != null && i < cardCostTexts.Length && cardCostTexts[i] != null)
                    {
                        cardCostTexts[i].text = $"<color=#30d158>COST {card.EnergyCost}</color>  |  <color=#ffd60a>PWR {card.Power}</color>";
                    }

                    bool isSelected = selectedIndices.Contains(i);
                    if (cardSelectedOverlays != null && i < cardSelectedOverlays.Length && cardSelectedOverlays[i] != null)
                    {
                        cardSelectedOverlays[i].SetActive(isSelected);
                    }
                }
                else
                {
                    if (cardButtons[i] != null) cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateConfirmButtonState()
        {
            int count = selectedIndices.Count;
            if (confirmButtonText != null)
            {
                confirmButtonText.text = count > 0 
                    ? $"CAMBIAR ({count})" 
                    : "CAMBIAR NINGUNA";
            }
        }

        private void HandleKeepAllClicked()
        {
            if (isAnimating) return;
            Hide();
            OnMulliganConfirmed?.Invoke(new List<int>());
        }

        private void HandleConfirmClicked()
        {
            if (isAnimating) return;

            if (selectedIndices.Count == 0)
            {
                Hide();
                OnMulliganConfirmed?.Invoke(new List<int>());
                return;
            }

            // Cards were selected for mulligan swap
            isAnimating = true;
            SetButtonsInteractable(false);

            if (confirmButtonText != null)
            {
                confirmButtonText.text = "CAMBIANDO...";
            }

            OnMulliganConfirmed?.Invoke(new List<int>(selectedIndices));
        }

        /// <summary>
        /// Animates the newly drawn cards with a 3D spinning flip effect, reveals them,
        /// displays them for a few seconds, then closes the modal and executes onComplete.
        /// </summary>
        public void AnimateAndRevealNewCards(IReadOnlyList<CardDefinition> newHand, List<int> swappedIndices, Action onComplete)
        {
            currentHand = newHand;
            StartCoroutine(AnimateCardSwapSequence(newHand, swappedIndices, onComplete));
        }

        private IEnumerator AnimateCardSwapSequence(IReadOnlyList<CardDefinition> newHand, List<int> swappedIndices, Action onComplete)
        {
            isAnimating = true;
            SetButtonsInteractable(false);

            if (titleText != null) titleText.text = "✨ ¡NUEVAS CARTAS OBTENIDAS! ✨";
            if (subtitleText != null)
            {
                subtitleText.text = "Descubriendo las nuevas cartas de tu mano inicial...";
            }
            if (confirmButtonText != null)
            {
                confirmButtonText.text = "REVELANDO...";
            }

            // Launch spin animation for each swapped card
            for (int i = 0; i < swappedIndices.Count; i++)
            {
                int slotIndex = swappedIndices[i];
                if (slotIndex >= 0 && slotIndex < cardButtons.Length && slotIndex < newHand.Count)
                {
                    var newCard = newHand[slotIndex];
                    StartCoroutine(SpinCardSlotCoroutine(slotIndex, newCard));
                    if (i < swappedIndices.Count - 1 && cardStaggerDelay > 0f)
                    {
                        yield return new WaitForSeconds(cardStaggerDelay);
                    }
                }
            }

            // Wait for all card spin animations to complete
            float totalSpinWait = cardSpinDuration + (swappedIndices.Count > 1 ? cardStaggerDelay * 0.5f : 0f);
            yield return new WaitForSeconds(totalSpinWait);

            // Update title and subtitle during the preview period
            if (titleText != null) titleText.text = "🃏 ¡MANO INICIAL LISTA!";
            if (subtitleText != null)
            {
                subtitleText.text = "<color=#30d158>★ ¡Cartas actualizadas!</color> Iniciando la partida...";
            }
            if (confirmButtonText != null)
            {
                confirmButtonText.text = "INICIANDO...";
            }

            // Display new cards for a few seconds so player can view them
            yield return new WaitForSeconds(previewDurationSeconds);

            Hide();
            isAnimating = false;
            onComplete?.Invoke();
        }

        private IEnumerator SpinCardSlotCoroutine(int slotIndex, CardDefinition newCard)
        {
            if (slotIndex < 0 || slotIndex >= cardButtons.Length) yield break;

            var button = cardButtons[slotIndex];
            if (button == null) yield break;

            Transform slotTransform = button.transform;
            float halfDuration = Mathf.Max(0.1f, cardSpinDuration * 0.5f);
            float elapsed = 0f;

            // Phase 1: Rotate 0° -> 90° around Y (card turns edge-on) + slight scale pop
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float angleY = Mathf.Lerp(0f, 90f, t);
                slotTransform.localRotation = Quaternion.Euler(0f, angleY, 0f);
                float scale = Mathf.Lerp(1f, 1.15f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Midpoint: Card is edge-on to the screen. Swap visuals to the NEW card!
            if (cardSelectedOverlays != null && slotIndex < cardSelectedOverlays.Length && cardSelectedOverlays[slotIndex] != null)
            {
                cardSelectedOverlays[slotIndex].SetActive(false);
            }

            if (cardArtImages != null && slotIndex < cardArtImages.Length && cardArtImages[slotIndex] != null)
            {
                var sprite = Card3D.GetCardThumbnail(newCard) ?? Card3D.GetReversoThumbnail("oro");
                cardArtImages[slotIndex].sprite = sprite;
                cardArtImages[slotIndex].color = sprite != null ? Color.white : Color.clear;
            }

            if (cardNameTexts != null && slotIndex < cardNameTexts.Length && cardNameTexts[slotIndex] != null)
            {
                cardNameTexts[slotIndex].text = $"✨ {newCard.Name}";
            }

            if (cardCostTexts != null && slotIndex < cardCostTexts.Length && cardCostTexts[slotIndex] != null)
            {
                cardCostTexts[slotIndex].text = $"<color=#30d158>COST {newCard.EnergyCost}</color>  |  <color=#ffd60a>PWR {newCard.Power}</color>";
            }

            // Highlight background on the new card
            var cBg = button.GetComponent<Image>();
            if (cBg != null)
            {
                cBg.color = new Color(0.12f, 0.32f, 0.45f, 0.98f); // Radiant teal highlight
            }

            // Phase 2: Rotate -90° -> 0° around Y (revealing new card face) + return scale
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float angleY = Mathf.Lerp(-90f, 0f, t);
                slotTransform.localRotation = Quaternion.Euler(0f, angleY, 0f);
                float scale = Mathf.Lerp(1.15f, 1f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            slotTransform.localRotation = Quaternion.identity;
            slotTransform.localScale = Vector3.one;

            // Smoothly restore default background tint
            if (cBg != null)
            {
                cBg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);
            }
        }
    }
}
