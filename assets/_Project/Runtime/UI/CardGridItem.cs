using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Style used to represent locked (uncollected) cards in the collection grid.
    /// </summary>
    public enum LockedCardVisualMode
    {
        /// <summary>Darkened silhouette of the card illustration with the lock overlay (standard TCG preview).</summary>
        DarkSilhouette,
        /// <summary>Full-color card illustration with the semi-transparent lock overlay on top.</summary>
        FullColorUnderLock,
        /// <summary>Official card back (reverso oficial) with the lock overlay for mysterious unrevealed cards.</summary>
        OfficialReverso
    }

    /// <summary>
    /// Represents a single card entry in the "MIS CARTAS" collection grid.
    /// Handles unlocked/locked appearance, card thumbnail, name, and selection border.
    /// </summary>
    public class CardGridItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardThumbnail;
        [SerializeField] private Image frameImage;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Image lockIconImage;
        [SerializeField] private GameObject selectedBorder;
        [SerializeField] private Button clickButton;

        [Header("Locked Display Mode")]
        [SerializeField] private LockedCardVisualMode lockedVisualMode = LockedCardVisualMode.DarkSilhouette;

        public static LockedCardVisualMode GlobalLockedVisualMode { get; set; } = LockedCardVisualMode.DarkSilhouette;

        public CardDefinition CardData { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Action<CardDefinition> onSelectedCallback;

        private void Awake()
        {
            if (clickButton == null) clickButton = GetComponent<Button>();
            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnClick);
            }
        }

        public void Setup(CardDefinition card, bool isUnlocked, bool isSelected, Action<CardDefinition> onSelected, Sprite customThumbnail = null)
        {
            CardData = card ?? throw new ArgumentNullException(nameof(card));
            IsUnlocked = isUnlocked;
            onSelectedCallback = onSelected;

            if (nameLabel != null)
            {
                nameLabel.text = card.Name;
                nameLabel.gameObject.SetActive(false);
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = card.Category.ToString();
                subtitleLabel.gameObject.SetActive(false);
            }

            if (cardThumbnail != null)
            {
                // Ensure thumbnail fills the entire card container
                var rt = cardThumbnail.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                cardThumbnail.preserveAspect = false;

                if (isUnlocked)
                {
                    // Active / Unlocked: Full vivid dynamic card
                    if (customThumbnail != null)
                    {
                        var existingDyn = rt.Find("DynamicCard");
                        if (existingDyn != null) existingDyn.gameObject.SetActive(false);
                        cardThumbnail.sprite = customThumbnail;
                        cardThumbnail.color = Color.white;
                    }
                    else
                    {
                        cardThumbnail.sprite = null;
                        cardThumbnail.color = Color.clear;
                        DynamicCardAssembler.AssembleCard(card, rt);
                    }
                }
                else
                {
                    // Locked: Visual based on configured mode
                    var activeMode = lockedVisualMode != LockedCardVisualMode.DarkSilhouette 
                        ? lockedVisualMode 
                        : GlobalLockedVisualMode;

                    switch (activeMode)
                    {
                        case LockedCardVisualMode.OfficialReverso:
                            var existingDyn = rt.Find("DynamicCard");
                            if (existingDyn != null) existingDyn.gameObject.SetActive(false);
                            var reversoSprite = Card3D.GetReversoThumbnail("oro");
                            if (reversoSprite != null) cardThumbnail.sprite = reversoSprite;
                            cardThumbnail.color = new Color(0.85f, 0.85f, 0.90f, 1f);
                            break;

                        case LockedCardVisualMode.FullColorUnderLock:
                        case LockedCardVisualMode.DarkSilhouette:
                        default:
                            if (customThumbnail != null)
                            {
                                var dyn = rt.Find("DynamicCard");
                                if (dyn != null) dyn.gameObject.SetActive(false);
                                cardThumbnail.sprite = customThumbnail;
                                cardThumbnail.color = new Color(0.75f, 0.75f, 0.80f, 1f);
                            }
                            else
                            {
                                cardThumbnail.sprite = null;
                                cardThumbnail.color = Color.clear;
                                DynamicCardAssembler.AssembleCard(card, rt);
                            }
                            break;
                    }
                }
            }

            // Lock Overlay & Padlock Icon
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);

                if (!isUnlocked)
                {
                    var lockOverlayImg = lockOverlay.GetComponent<Image>();
                    if (lockOverlayImg != null)
                    {
                        lockOverlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.45f);
                        lockOverlayImg.raycastTarget = false;
                    }

                    // Self-heal or assign LockIcon Image component
                    if (lockIconImage == null)
                    {
                        var iconTransform = lockOverlay.transform.Find("LockIcon");
                        if (iconTransform != null)
                        {
                            lockIconImage = iconTransform.GetComponent<Image>() ?? iconTransform.gameObject.AddComponent<Image>();
                            var oldTMP = iconTransform.GetComponent<TextMeshProUGUI>();
                            if (oldTMP != null) oldTMP.enabled = false;
                        }
                        else
                        {
                            var iconGo = new GameObject("LockIcon", typeof(RectTransform));
                            iconGo.transform.SetParent(lockOverlay.transform, false);
                            lockIconImage = iconGo.AddComponent<Image>();
                        }
                    }

                    if (lockIconImage != null)
                    {
                        var iconRect = lockIconImage.rectTransform;
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.zero;
                        iconRect.sizeDelta = new Vector2(36, 36);

                        var lockSprite = Card3D.GetLockIconSprite();
                        if (lockSprite != null)
                        {
                            lockIconImage.sprite = lockSprite;
                            lockIconImage.preserveAspect = true;
                            lockIconImage.color = new Color(1f, 0.85f, 0.25f, 1f); // Vibrant gold
                            lockIconImage.enabled = true;
                            lockIconImage.raycastTarget = false;
                        }
                    }
                }
            }

            SetSelected(isSelected);
        }

        public void SetLockedVisualMode(LockedCardVisualMode mode)
        {
            lockedVisualMode = mode;
            if (CardData != null)
            {
                Setup(CardData, IsUnlocked, selectedBorder != null && selectedBorder.activeSelf, onSelectedCallback);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedBorder != null)
            {
                selectedBorder.SetActive(isSelected);
            }
        }

        public void SetThumbnail(Sprite sprite)
        {
            if (cardThumbnail != null && sprite != null)
            {
                cardThumbnail.sprite = sprite;
            }
        }

        private void OnClick()
        {
            CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.CardGridSelect);
            onSelectedCallback?.Invoke(CardData);
        }
    }
}
