using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Represents a playable card in the player's bottom hand tray.
    /// Displays Energy Cost, Power/Attack, card artwork, name, and selected/affordable states.
    /// </summary>
    public class BattleHandCardView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image cardThumbnail;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject selectedBorder;
        [SerializeField] private GameObject unaffordableDimmer;
        [SerializeField] private Button cardButton;

        public CardDefinition CardData { get; private set; }
        public int HandIndex { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsAffordable { get; private set; }

        private Action<int> onClickCallback;

        private void Awake()
        {
            if (cardButton == null) cardButton = GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(() => onClickCallback?.Invoke(HandIndex));
            }

            ApplyRoundedCardStyle();

            // Ensure child graphics do not block clicks intended for the Button
            var rootGraphic = GetComponent<Graphic>();
            foreach (var g in GetComponentsInChildren<Graphic>(true))
            {
                if (g != rootGraphic)
                {
                    g.raycastTarget = false;
                }
            }
        }

        private void ApplyRoundedCardStyle()
        {
            var maskSprite = Resources.Load<Sprite>("UI/rounded_card_mask");
            var borderSprite = Resources.Load<Sprite>("UI/rounded_card_border");

            var rootImg = GetComponent<Image>();
            if (rootImg != null && maskSprite != null)
            {
                rootImg.sprite = maskSprite;
                rootImg.type = Image.Type.Sliced;
                rootImg.color = new Color(0.10f, 0.13f, 0.19f, 1f);
            }

            var mask = GetComponent<Mask>();
            if (mask == null)
            {
                mask = gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            // Normal border
            var borderTrans = transform.Find("Border");
            Image borderImg = null;
            if (borderTrans != null)
            {
                borderImg = borderTrans.GetComponent<Image>();
            }
            else if (borderSprite != null)
            {
                var borderObj = new GameObject("Border");
                borderObj.transform.SetParent(transform, false);
                var bRect = borderObj.AddComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero;
                bRect.anchorMax = Vector2.one;
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;
                borderImg = borderObj.AddComponent<Image>();
                borderImg.raycastTarget = false;
            }

            if (borderImg != null && borderSprite != null)
            {
                borderImg.sprite = borderSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.color = new Color(0.35f, 0.45f, 0.65f, 0.8f);
                borderImg.transform.SetAsLastSibling();
            }

            // Selected border
            if (selectedBorder != null && borderSprite != null)
            {
                var selImg = selectedBorder.GetComponent<Image>();
                if (selImg != null)
                {
                    selImg.sprite = borderSprite;
                    selImg.type = Image.Type.Sliced;
                    selImg.color = new Color(0.16f, 0.85f, 0.95f, 1f); // Vibrant cyan glow
                }
            }
        }

        public void Setup(
            CardDefinition card,
            int handIndex,
            bool isSelected,
            bool isAffordable,
            Action<int> onSelectedCallback)
        {
            CardData = card ?? throw new ArgumentNullException(nameof(card));
            HandIndex = handIndex;
            IsSelected = isSelected;
            IsAffordable = isAffordable;
            onClickCallback = onSelectedCallback;

            if (cardThumbnail != null)
            {
                var sprite = Card3D.GetCardThumbnail(card) ?? Card3D.GetReversoThumbnail("oro");
                cardThumbnail.sprite = sprite;
                // Keep artwork clearly readable, slightly softer if unaffordable
                cardThumbnail.color = isAffordable ? Color.white : new Color(0.75f, 0.75f, 0.80f, 0.95f);
                cardThumbnail.raycastTarget = false;
            }

            if (costText != null)
            {
                costText.text = isAffordable 
                    ? $"{card.EnergyCost}" 
                    : $"<color=#ff453a>{card.EnergyCost}</color>";
                costText.raycastTarget = false;
            }

            if (attackText != null)
            {
                int pwr = card.Category == CardCategory.PhysicalTrompo
                    ? Math.Max(2, card.EnergyCost + 1)
                    : card.Power;
                attackText.text = $"{pwr}";
                attackText.raycastTarget = false;
            }

            if (nameText != null)
            {
                nameText.text = card.Name;
                nameText.raycastTarget = false;
            }

            if (selectedBorder != null)
            {
                selectedBorder.SetActive(isSelected);
                var selImg = selectedBorder.GetComponent<Graphic>();
                if (selImg != null) selImg.raycastTarget = false;
            }

            if (unaffordableDimmer != null)
            {
                // Soft translucent indicator instead of complete blackout
                unaffordableDimmer.SetActive(!isAffordable);
                var dimImg = unaffordableDimmer.GetComponent<Image>();
                if (dimImg != null)
                {
                    dimImg.color = new Color(0.02f, 0.04f, 0.08f, 0.35f); // Soft tint
                    dimImg.raycastTarget = false; // NEVER block clicks
                }
            }
        }
    }
}
