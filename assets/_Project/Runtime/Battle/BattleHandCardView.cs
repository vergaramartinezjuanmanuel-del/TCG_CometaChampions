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
