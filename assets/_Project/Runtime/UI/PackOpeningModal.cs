#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Interactive booster pack opening modal.
    /// Shows a single sealed pack at a time with a spinning opening effect,
    /// followed by individual 3D rotational flips for discovering cards one by one (Rule 11).
    /// </summary>
    public class PackOpeningModal : MonoBehaviour
    {
        [Header("Modal Windows")]
        [SerializeField] private GameObject modalRoot = null!;
        [SerializeField] private Button closeButton = null!;
        [SerializeField] private TextMeshProUGUI headerTitleText = null!;
        [SerializeField] private TextMeshProUGUI subtitleText = null!;

        [Header("Sealed Pack View (Single Pack at a time)")]
        [SerializeField] private GameObject sealedPackContainer = null!;
        [SerializeField] private RectTransform sealedPackWrapper = null!;
        [SerializeField] private Button sealedPackButton = null!;
        [SerializeField] private Button openPackButton = null!;
        [SerializeField] private TextMeshProUGUI openPackButtonText = null!;
        [SerializeField] private TextMeshProUGUI sealedPackTitleText = null!;
        [SerializeField] private TextMeshProUGUI sealedPackSubtitleText = null!;
        [SerializeField] private TextMeshProUGUI remainingPacksBadgeText = null!;

        [Header("Card Reveal Slots (5 Slots)")]
        [SerializeField] private Transform cardsContainer = null!;

        [Header("Action Controls")]
        [SerializeField] private Button revealNextButton = null!;
        [SerializeField] private TextMeshProUGUI revealNextButtonText = null!;
        [SerializeField] private Button revealAllButton = null!;
        [SerializeField] private Button nextPackOrDoneButton = null!;
        [SerializeField] private TextMeshProUGUI nextPackOrDoneButtonText = null!;

        [Header("Summary Panel")]
        [SerializeField] private GameObject summaryPanelRoot = null!;
        [SerializeField] private TextMeshProUGUI summaryText = null!;

        private readonly List<CardPackResult> currentPacks = new();
        private int currentPackIndex = 0;
        private int revealedCardsInCurrentPack = 0;
        private bool isAnimating = false;
        private Action? onCompleteCallback;

        private readonly List<GameObject> spawnedCardSlots = new();

        private void Awake()
        {
            EnsureButtonListeners();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isAnimating = false;
        }

        private void EnsureButtonListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (sealedPackButton != null)
            {
                sealedPackButton.onClick.RemoveAllListeners();
                sealedPackButton.onClick.AddListener(TriggerOpenCurrentPack);
            }

            if (openPackButton != null)
            {
                openPackButton.onClick.RemoveAllListeners();
                openPackButton.onClick.AddListener(TriggerOpenCurrentPack);
            }

            if (revealNextButton != null)
            {
                revealNextButton.onClick.RemoveAllListeners();
                revealNextButton.onClick.AddListener(RevealNextCard);
            }

            if (revealAllButton != null)
            {
                revealAllButton.onClick.RemoveAllListeners();
                revealAllButton.onClick.AddListener(RevealAllCardsInPack);
            }

            if (nextPackOrDoneButton != null)
            {
                nextPackOrDoneButton.onClick.RemoveAllListeners();
                nextPackOrDoneButton.onClick.AddListener(HandleNextPackOrDoneClicked);
            }
        }

        /// <summary>
        /// Displays the pack opening modal for a single pack.
        /// </summary>
        public void Show(CardPackResult pack, Action? onComplete = null)
        {
            if (pack == null) return;
            Show(new List<CardPackResult> { pack }, onComplete);
        }

        /// <summary>
        /// Displays the pack opening modal for a batch of packs (e.g. multi-pack purchase).
        /// </summary>
        public void Show(IEnumerable<CardPackResult> packs, Action? onComplete = null)
        {
            currentPacks.Clear();
            if (packs != null)
            {
                currentPacks.AddRange(packs);
            }

            if (currentPacks.Count == 0) return;

            currentPackIndex = 0;
            onCompleteCallback = onComplete;

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            EnsureButtonListeners();

            if (summaryPanelRoot != null)
            {
                summaryPanelRoot.SetActive(false);
            }

            SetupCurrentPackUI();
        }

        public void Hide()
        {
            StopAllCoroutines();
            isAnimating = false;

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            ClearSpawnedSlots();
            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
        }

        private void SetupCurrentPackUI()
        {
            if (currentPackIndex >= currentPacks.Count)
            {
                ShowBatchSummary();
                return;
            }

            var pack = currentPacks[currentPackIndex];
            revealedCardsInCurrentPack = 0;
            isAnimating = false;

            // Reset UI states: show single sealed pack, hide cards and reveal buttons
            if (sealedPackContainer != null)
            {
                sealedPackContainer.SetActive(true);
            }

            if (sealedPackWrapper != null)
            {
                sealedPackWrapper.localScale = Vector3.one;
                sealedPackWrapper.localRotation = Quaternion.identity;
            }

            if (cardsContainer != null)
            {
                cardsContainer.gameObject.SetActive(false);
            }

            bool isRewardPack = pack.PackType == CardPackType.LevelReward || pack.Cards.Count == 3;

            if (headerTitleText != null)
            {
                if (isRewardPack)
                {
                    headerTitleText.text = currentPacks.Count > 1
                        ? $"SOBRE DE RECOMPENSA {currentPackIndex + 1} DE {currentPacks.Count}"
                        : "SOBRE DE RECOMPENSA DE NIVEL";
                }
                else
                {
                    headerTitleText.text = currentPacks.Count > 1
                        ? $"SOBRE {currentPackIndex + 1} DE {currentPacks.Count}"
                        : "SOBRE TOMO 1";
                }
            }

            if (subtitleText != null)
            {
                subtitleText.text = isRewardPack
                    ? "3 cartas • Recompensa de Nivel • Progreso Foil Bicolor > Color > Oro"
                    : "Toca el sobre para abrirlo y descubrir tus cartas";
            }

            if (sealedPackTitleText != null)
            {
                sealedPackTitleText.text = isRewardPack
                    ? "<size=20><b>COMETA\nCHAMPIONS</b></size>\n<size=11><color=#00E5FF>RECOMPENSA DE NIVEL</color></size>\n\n<size=22><b>3 CARTAS</b></size>\n\n<size=11>• PROGRESIÓN FOIL •</size>"
                    : "<size=20><b>COMETA\nCHAMPIONS</b></size>\n<size=11><color=#FFD700>SET BASE</color></size>\n\n<size=26><b>TOMO 1</b></size>\n\n<size=11>• 1 CARTA NUEVA\nGARANTIZADA •</size>";
            }

            if (sealedPackSubtitleText != null)
            {
                sealedPackSubtitleText.text = isRewardPack
                    ? "3 CARTAS • PROGRESIÓN FOIL"
                    : "5 CARTAS • 1 NUEVA GARANTIZADA";
            }

            if (remainingPacksBadgeText != null)
            {
                int remaining = currentPacks.Count - (currentPackIndex + 1);
                remainingPacksBadgeText.gameObject.SetActive(remaining > 0);
                remainingPacksBadgeText.text = $"+{remaining} sobres pendientes";
            }

            if (openPackButton != null)
            {
                openPackButton.gameObject.SetActive(true);
                openPackButton.interactable = true;
            }

            if (openPackButtonText != null)
            {
                openPackButtonText.text = "ABRIR SOBRE";
            }

            if (revealNextButton != null)
            {
                revealNextButton.gameObject.SetActive(false);
            }

            if (revealAllButton != null)
            {
                revealAllButton.gameObject.SetActive(false);
            }

            if (nextPackOrDoneButton != null)
            {
                nextPackOrDoneButton.gameObject.SetActive(false);
                nextPackOrDoneButton.onClick.RemoveAllListeners();
                nextPackOrDoneButton.onClick.AddListener(HandleNextPackOrDoneClicked);
            }

            SpawnPackCardSlots(pack);
        }

        private void TriggerOpenCurrentPack()
        {
            if (isAnimating) return;
            StartCoroutine(AnimatePackOpening());
        }

        private IEnumerator AnimatePackOpening()
        {
            isAnimating = true;

            try
            {
                if (openPackButton != null)
                {
                    openPackButton.interactable = false;
                }

                // Rotational spin opening effect on the sealed pack
                if (sealedPackWrapper != null)
                {
                    float duration = 0.55f;
                    float elapsed = 0f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);

                        // Cubic spin acceleration
                        float angleY = Mathf.Lerp(0f, 360f, t * t);
                        sealedPackWrapper.localRotation = Quaternion.Euler(0f, angleY, 0f);

                        // Pulse scale up to 1.15 then back down
                        float scale = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
                        sealedPackWrapper.localScale = new Vector3(scale, scale, scale);

                        yield return null;
                    }

                    sealedPackWrapper.localRotation = Quaternion.identity;
                    sealedPackWrapper.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.08f);

                // Transition: hide sealed pack, show the 5 face-down cards
                if (sealedPackContainer != null)
                {
                    sealedPackContainer.SetActive(false);
                }

                if (cardsContainer != null)
                {
                    cardsContainer.gameObject.SetActive(true);
                    if (cardsContainer is RectTransform rt)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                    }
                }

                if (headerTitleText != null)
                {
                    headerTitleText.text = currentPacks.Count > 1
                        ? $"SOBRE {currentPackIndex + 1} DE {currentPacks.Count} (DESCUBRE TUS CARTAS)"
                        : "DESCUBRE TUS CARTAS";
                }

                if (subtitleText != null)
                {
                    subtitleText.text = "Haz clic en cada tarjeta para descubrirla o usa 'Descubrir Siguiente'";
                }

                if (revealNextButton != null)
                {
                    revealNextButton.gameObject.SetActive(true);
                    revealNextButton.interactable = true;
                    UpdateRevealButtonText();
                }

                if (revealAllButton != null)
                {
                    revealAllButton.gameObject.SetActive(true);
                    revealAllButton.interactable = true;
                }
            }
            finally
            {
                isAnimating = false;
            }
        }

        private void SpawnPackCardSlots(CardPackResult pack)
        {
            ClearSpawnedSlots();
            if (cardsContainer == null) return;

            for (int i = 0; i < pack.Cards.Count; i++)
            {
                int slotIndex = i;
                var cardResult = pack.Cards[i];
                var slotObj = CreateCardSlotUI(cardResult, slotIndex);
                spawnedCardSlots.Add(slotObj);
            }
        }

        private GameObject CreateCardSlotUI(PackOpeningCardResult result, int index)
        {
            var slotObj = new GameObject($"Slot_{index}_{result.Card.Id}", typeof(RectTransform));
            slotObj.transform.SetParent(cardsContainer, false);

            var rect = slotObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(175, 275);

            var le = slotObj.AddComponent<LayoutElement>();
            le.preferredWidth = 175;
            le.preferredHeight = 275;
            le.minWidth = 175;
            le.minHeight = 275;

            var btn = slotObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (!isAnimating && index >= revealedCardsInCurrentPack)
                {
                    RevealNextCard();
                }
            });

            // Card Frame Background
            var frameObj = new GameObject("Frame", typeof(RectTransform));
            frameObj.transform.SetParent(slotObj.transform, false);
            var fRect = frameObj.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.color = new Color(0.10f, 0.12f, 0.18f, 1f);
            btn.targetGraphic = frameImg;

            // Back Face (Cometa Champions cardback)
            var backFace = new GameObject("BackFace", typeof(RectTransform));
            backFace.transform.SetParent(slotObj.transform, false);
            var bRect = backFace.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = Vector2.zero;
            var backImg = backFace.AddComponent<Image>();
            backImg.color = new Color(0.14f, 0.20f, 0.32f, 1f);

            var backTextObj = new GameObject("BackText", typeof(RectTransform));
            backTextObj.transform.SetParent(backFace.transform, false);
            var btRect = backTextObj.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var btTMP = backTextObj.AddComponent<TextMeshProUGUI>();
            btTMP.text = "<size=18><b>COMETA\nCHAMPIONS</b></size>\n<size=14>?</size>";
            btTMP.alignment = TextAlignmentOptions.Center;
            btTMP.color = new Color(1f, 0.82f, 0.2f);
            btTMP.raycastTarget = false;

            // Front Face (Initially hidden)
            var frontFace = new GameObject("FrontFace", typeof(RectTransform));
            frontFace.transform.SetParent(slotObj.transform, false);
            var frRect = frontFace.GetComponent<RectTransform>();
            frRect.anchorMin = Vector2.zero;
            frRect.anchorMax = Vector2.one;
            frRect.sizeDelta = Vector2.zero;
            frRect.offsetMin = new Vector2(3, 3);
            frRect.offsetMax = new Vector2(-3, -3);
            var frImg = frontFace.AddComponent<Image>();
            frImg.color = GetRarityColor(result.Card.Rarity);
            frImg.raycastTarget = false;
            frontFace.SetActive(false);

            // Card Inner Artwork
            var artObj = new GameObject("ArtThumbnail", typeof(RectTransform));
            artObj.transform.SetParent(frontFace.transform, false);
            var aRect = artObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.05f, 0.27f);
            aRect.anchorMax = new Vector2(0.95f, 0.89f);
            aRect.sizeDelta = Vector2.zero;
            var artImg = artObj.AddComponent<Image>();
            var sprite = Card3D.GetCardThumbnail(result.Card) ?? Card3D.GetReversoThumbnail("oro");
            if (sprite != null)
            {
                artImg.sprite = sprite;
                artImg.preserveAspect = true;
            }
            artImg.raycastTarget = false;

            // Energy Cost Badge (Top-Left)
            var costObj = new GameObject("CostBadge", typeof(RectTransform));
            costObj.transform.SetParent(frontFace.transform, false);
            var costRect = costObj.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 1);
            costRect.anchorMax = new Vector2(0, 1);
            costRect.pivot = new Vector2(0, 1);
            costRect.anchoredPosition = new Vector2(5, -5);
            costRect.sizeDelta = new Vector2(28, 28);
            var costBg = costObj.AddComponent<Image>();
            costBg.color = new Color(0.12f, 0.65f, 0.28f, 0.95f);
            costBg.raycastTarget = false;

            var costTextObj = new GameObject("CostVal", typeof(RectTransform));
            costTextObj.transform.SetParent(costObj.transform, false);
            var costTextRect = costTextObj.GetComponent<RectTransform>();
            costTextRect.anchorMin = Vector2.zero;
            costTextRect.anchorMax = Vector2.one;
            costTextRect.sizeDelta = Vector2.zero;
            var costTMP = costTextObj.AddComponent<TextMeshProUGUI>();
            costTMP.text = result.Card.EnergyCost.ToString();
            costTMP.fontSize = 15;
            costTMP.fontStyle = FontStyles.Bold;
            costTMP.alignment = TextAlignmentOptions.Center;
            costTMP.color = Color.white;
            costTMP.raycastTarget = false;

            // Power Badge (Top-Right)
            var pwrObj = new GameObject("PowerBadge", typeof(RectTransform));
            pwrObj.transform.SetParent(frontFace.transform, false);
            var pwrRect = pwrObj.GetComponent<RectTransform>();
            pwrRect.anchorMin = new Vector2(1, 1);
            pwrRect.anchorMax = new Vector2(1, 1);
            pwrRect.pivot = new Vector2(1, 1);
            pwrRect.anchoredPosition = new Vector2(-5, -5);
            pwrRect.sizeDelta = new Vector2(28, 28);
            var pwrBg = pwrObj.AddComponent<Image>();
            pwrBg.color = new Color(0.85f, 0.22f, 0.22f, 0.95f);
            pwrBg.raycastTarget = false;

            var pwrTextObj = new GameObject("PowerVal", typeof(RectTransform));
            pwrTextObj.transform.SetParent(pwrObj.transform, false);
            var pwrTextRect = pwrTextObj.GetComponent<RectTransform>();
            pwrTextRect.anchorMin = Vector2.zero;
            pwrTextRect.anchorMax = Vector2.one;
            pwrTextRect.sizeDelta = Vector2.zero;
            var pwrTMP = pwrTextObj.AddComponent<TextMeshProUGUI>();
            pwrTMP.text = result.Card.Power.ToString();
            pwrTMP.fontSize = 15;
            pwrTMP.fontStyle = FontStyles.Bold;
            pwrTMP.alignment = TextAlignmentOptions.Center;
            pwrTMP.color = Color.white;
            pwrTMP.raycastTarget = false;

            // Name text
            var nameObj = new GameObject("NameText", typeof(RectTransform));
            nameObj.transform.SetParent(frontFace.transform, false);
            var nRect = nameObj.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.04f, 0.14f);
            nRect.anchorMax = new Vector2(0.96f, 0.26f);
            nRect.sizeDelta = Vector2.zero;
            var nTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nTMP.text = $"<b>{result.Card.Name}</b>";
            nTMP.fontSize = 12;
            nTMP.color = Color.white;
            nTMP.alignment = TextAlignmentOptions.Center;
            nTMP.raycastTarget = false;

            // Rarity / Status Badge (NEW CARD! vs DUPLICATE)
            var badgeObj = new GameObject("StatusBadge", typeof(RectTransform));
            badgeObj.transform.SetParent(frontFace.transform, false);
            var bgRect = badgeObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.04f, 0.02f);
            bgRect.anchorMax = new Vector2(0.96f, 0.13f);
            bgRect.sizeDelta = Vector2.zero;

            var bgImg = badgeObj.AddComponent<Image>();
            bgImg.raycastTarget = false;

            var badgeTextObj = new GameObject("StatusText", typeof(RectTransform));
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            var btRect2 = badgeTextObj.GetComponent<RectTransform>();
            btRect2.anchorMin = Vector2.zero;
            btRect2.anchorMax = Vector2.one;
            btRect2.sizeDelta = Vector2.zero;

            var badgeTMP = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeTMP.alignment = TextAlignmentOptions.Center;
            badgeTMP.fontSize = 11;
            badgeTMP.fontStyle = FontStyles.Bold;
            badgeTMP.raycastTarget = false;

            if (result.IsNew)
            {
                // Vibrant Gold badge for NEW CARD! (Rule 11)
                bgImg.color = new Color(0.95f, 0.75f, 0.1f, 0.95f);
                badgeTMP.color = new Color(0.05f, 0.05f, 0.05f);
                badgeTMP.text = "¡NUEVA CARTA!";
            }
            else
            {
                // Silver badge for DUPLICATE (Rule 11) + foil progress
                bgImg.color = new Color(0.25f, 0.32f, 0.45f, 0.95f);
                badgeTMP.color = Color.white;
                badgeTMP.text = $"+1 DUPLICADA ({result.DuplicateCopiesAfter}/5 Foil)";
            }

            return slotObj;
        }

        private static Color GetRarityColor(CardRarity rarity) => rarity switch
        {
            CardRarity.Common => new Color(0.18f, 0.20f, 0.26f),
            CardRarity.Rare => new Color(0.12f, 0.35f, 0.55f),
            CardRarity.Epic => new Color(0.40f, 0.18f, 0.55f),
            CardRarity.Legendary => new Color(0.65f, 0.45f, 0.10f),
            _ => new Color(0.2f, 0.2f, 0.25f)
        };

        private void UpdateRevealButtonText()
        {
            if (revealNextButtonText == null) return;

            if (currentPackIndex < currentPacks.Count)
            {
                var pack = currentPacks[currentPackIndex];
                int nextIndex = revealedCardsInCurrentPack + 1;
                if (nextIndex < pack.Cards.Count)
                {
                    revealNextButtonText.text = $"DESCUBRIR CARTA ({nextIndex}/{pack.Cards.Count})";
                }
                else if (nextIndex == pack.Cards.Count)
                {
                    revealNextButtonText.text = $"DESCUBRIR ÚLTIMA CARTA ({nextIndex}/{pack.Cards.Count})";
                }
                else
                {
                    revealNextButtonText.text = "DESCUBRIR CARTA";
                }
            }
            else
            {
                revealNextButtonText.text = "DESCUBRIR CARTA";
            }
        }

        public void RevealNextCard()
        {
            if (isAnimating) return;
            if (currentPackIndex >= currentPacks.Count) return;
            var pack = currentPacks[currentPackIndex];

            if (revealedCardsInCurrentPack < pack.Cards.Count && revealedCardsInCurrentPack < spawnedCardSlots.Count)
            {
                int index = revealedCardsInCurrentPack;
                revealedCardsInCurrentPack++;
                UpdateRevealButtonText();

                StartCoroutine(RevealSlotWithSpin(index, () =>
                {
                    if (revealedCardsInCurrentPack >= pack.Cards.Count)
                    {
                        OnCurrentPackFullyRevealed();
                    }
                }));
            }
            else if (revealedCardsInCurrentPack >= pack.Cards.Count)
            {
                OnCurrentPackFullyRevealed();
            }
        }

        public void RevealAllCardsInPack()
        {
            if (isAnimating) return;
            StartCoroutine(RevealAllSequence());
        }

        private IEnumerator RevealAllSequence()
        {
            isAnimating = true;

            try
            {
                if (currentPackIndex >= currentPacks.Count) yield break;
                var pack = currentPacks[currentPackIndex];

                while (revealedCardsInCurrentPack < pack.Cards.Count && revealedCardsInCurrentPack < spawnedCardSlots.Count)
                {
                    int index = revealedCardsInCurrentPack;
                    revealedCardsInCurrentPack++;
                    UpdateRevealButtonText();

                    yield return StartCoroutine(RevealSlotWithSpin(index, null));
                    yield return new WaitForSeconds(0.06f);
                }
            }
            finally
            {
                isAnimating = false;
                OnCurrentPackFullyRevealed();
            }
        }

        private IEnumerator RevealSlotWithSpin(int index, Action? onDone)
        {
            isAnimating = true;

            try
            {
                if (index < 0 || index >= spawnedCardSlots.Count) yield break;
                var slotObj = spawnedCardSlots[index];
                if (slotObj == null) yield break;

                var backFace = slotObj.transform.Find("BackFace")?.gameObject;
                var frontFace = slotObj.transform.Find("FrontFace")?.gameObject;

                // 3D Rotational spin flip effect (Rule 11)
                float duration = 0.32f;
                float elapsed = 0f;
                Transform cardTransform = slotObj.transform;
                Vector3 originalScale = Vector3.one;

                // Phase 1: Rotate 0° -> 90° around Y (card turns edge-on to the screen)
                while (elapsed < duration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / (duration * 0.5f));
                    float angleY = Mathf.Lerp(0f, 90f, t);
                    cardTransform.localRotation = Quaternion.Euler(0f, angleY, 0f);
                    float scalePop = Mathf.Lerp(1f, 1.10f, t);
                    cardTransform.localScale = new Vector3(scalePop, scalePop, 1f);
                    yield return null;
                }

                // Swap to front face when edge-on
                if (backFace != null) backFace.SetActive(false);
                if (frontFace != null) frontFace.SetActive(true);

                // Phase 2: Rotate -90° -> 0° around Y (reveals front face)
                elapsed = 0f;
                while (elapsed < duration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / (duration * 0.5f));
                    float angleY = Mathf.Lerp(-90f, 0f, t);
                    cardTransform.localRotation = Quaternion.Euler(0f, angleY, 0f);
                    float scalePop = Mathf.Lerp(1.10f, 1f, t);
                    cardTransform.localScale = new Vector3(scalePop, scalePop, 1f);
                    yield return null;
                }

                cardTransform.localRotation = Quaternion.identity;
                cardTransform.localScale = Vector3.one;
            }
            finally
            {
                isAnimating = false;
                onDone?.Invoke();
            }
        }

        private void OnCurrentPackFullyRevealed()
        {
            if (revealNextButton != null) revealNextButton.gameObject.SetActive(false);
            if (revealAllButton != null) revealAllButton.gameObject.SetActive(false);

            if (nextPackOrDoneButton != null)
            {
                nextPackOrDoneButton.gameObject.SetActive(true);
                nextPackOrDoneButton.onClick.RemoveAllListeners();
                nextPackOrDoneButton.onClick.AddListener(HandleNextPackOrDoneClicked);
                if (nextPackOrDoneButtonText != null)
                {
                    bool hasMorePacks = currentPackIndex + 1 < currentPacks.Count;
                    nextPackOrDoneButtonText.text = hasMorePacks
                        ? $"ABRIR SIGUIENTE SOBRE ({currentPackIndex + 2}/{currentPacks.Count})"
                        : "LISTO / VOLVER AL ÁLBUM";
                }
            }
        }

        private void HandleNextPackOrDoneClicked()
        {
            currentPackIndex++;
            if (currentPackIndex < currentPacks.Count)
            {
                SetupCurrentPackUI();
            }
            else
            {
                ShowBatchSummary();
            }
        }

        private void ShowBatchSummary()
        {
            int totalNew = currentPacks.Sum(p => p.NewCardsCount);
            int totalDups = currentPacks.Sum(p => p.DuplicateCardsCount);
            int totalCards = currentPacks.Sum(p => p.Cards.Count);

            if (sealedPackContainer != null) sealedPackContainer.SetActive(false);
            if (cardsContainer != null) cardsContainer.gameObject.SetActive(false);

            if (summaryPanelRoot != null)
            {
                summaryPanelRoot.SetActive(true);
            }

            if (summaryText != null)
            {
                summaryText.text = $"<b>¡Apertura Completada!</b>\n\n" +
                                   $"• Sobres Abiertos: <b>{currentPacks.Count}</b>\n" +
                                   $"• Cartas Nuevas Desbloqueadas: <b>+{totalNew}</b>\n" +
                                   $"• Duplicadas para Mejoras: <b>+{totalDups}</b>\n" +
                                   $"• Total de Cartas Recibidas: <b>{totalCards}</b>";
            }

            if (headerTitleText != null)
            {
                headerTitleText.text = "RESUMEN DE APERTURA";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Tus nuevas cartas ya están disponibles en tu Álbum y Mazo de Combate.";
            }

            if (revealNextButton != null) revealNextButton.gameObject.SetActive(false);
            if (revealAllButton != null) revealAllButton.gameObject.SetActive(false);

            if (nextPackOrDoneButton != null)
            {
                nextPackOrDoneButton.gameObject.SetActive(true);
                if (nextPackOrDoneButtonText != null)
                {
                    nextPackOrDoneButtonText.text = "VOLVER AL ÁLBUM";
                }
                nextPackOrDoneButton.onClick.RemoveAllListeners();
                nextPackOrDoneButton.onClick.AddListener(Hide);
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
        }

        private void ClearSpawnedSlots()
        {
            foreach (var slot in spawnedCardSlots)
            {
                if (slot != null) Destroy(slot);
            }
            spawnedCardSlots.Clear();
        }
    }
}
