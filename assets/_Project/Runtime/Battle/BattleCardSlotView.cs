using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Represents a single card slot on an arena lane (either for the Player or the AI).
    /// Displays card thumbnail, effective power badge, and undo pill if playable this turn.
    /// </summary>
    public class BattleCardSlotView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image cardThumbnail;
        [SerializeField] private Image slotFrame;
        [SerializeField] private GameObject powerBadgeRoot;
        [SerializeField] private TextMeshProUGUI powerBadgeText;
        [SerializeField] private GameObject undoBadgeRoot;
        [SerializeField] private Button slotButton;

        public CardDefinition CurrentCard { get; private set; }
        public int LaneIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public bool CanUndo { get; private set; }

        private Action<int, int> onSlotClicked;
        private bool wasFaceDown;
        private int lastPower = -1;

        private void Awake()
        {
            if (slotButton == null) slotButton = GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(HandleSlotClicked);
            }

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
            int effectivePower,
            bool canUndo,
            int laneIndex,
            int slotIndex,
            Action<int, int> onClickCallback,
            bool isFaceDown = false)
        {
            var prevCard = CurrentCard;
            CurrentCard = card;
            LaneIndex = laneIndex;
            SlotIndex = slotIndex;
            CanUndo = canUndo;
            onSlotClicked = onClickCallback;

            if (card == null)
            {
                wasFaceDown = false;
                lastPower = -1;
                // Empty slot appearance
                if (cardThumbnail != null)
                {
                    var existingDyn = cardThumbnail.transform.Find("DynamicCard");
                    if (existingDyn != null) existingDyn.gameObject.SetActive(false);

                    cardThumbnail.enabled = false;
                    cardThumbnail.sprite = null;
                }

                // if (slotFrame != null)
                // {
                //     slotFrame.color = new Color(1f, 1f, 1f, 0.15f);
                // }

                if (powerBadgeRoot != null) powerBadgeRoot.SetActive(false);
                if (undoBadgeRoot != null) undoBadgeRoot.SetActive(false);
            }
            else if (isFaceDown)
            {
                if (prevCard == null && !wasFaceDown)
                {
                    CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ArenaCardFacedown);
                }
                wasFaceDown = true;

                // Unrevealed / Face-down opponent card (Rule 17)
                if (cardThumbnail != null)
                {
                    var existingDyn = cardThumbnail.transform.Find("DynamicCard");
                    if (existingDyn != null) existingDyn.gameObject.SetActive(false);

                    cardThumbnail.enabled = true;
                    cardThumbnail.sprite = Card3D.GetReversoThumbnail("oro");
                    cardThumbnail.color = cardThumbnail.sprite != null
                        ? Color.white
                        : new Color(0.12f, 0.15f, 0.28f, 0.95f);
                }

                if (slotFrame != null)
                {
                    slotFrame.color = new Color(0.85f, 0.65f, 0.15f, 0.9f); // Gold mystery glow
                }

                if (powerBadgeRoot != null)
                {
                    powerBadgeRoot.SetActive(true);
                    if (powerBadgeText != null)
                    {
                        powerBadgeText.text = "?";
                    }
                }

                if (undoBadgeRoot != null) undoBadgeRoot.SetActive(false);
            }
            else
            {
                if (wasFaceDown)
                {
                    CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ArenaCardReveal);
                    wasFaceDown = false;
                }
                if (lastPower != -1 && lastPower != effectivePower)
                {
                    CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.LanePowerTick);
                }
                lastPower = effectivePower;

                // Occupied slot appearance (revealed)
                if (cardThumbnail != null)
                {
                    cardThumbnail.enabled = true;
                    cardThumbnail.sprite = null;
                    cardThumbnail.color = Color.clear;

                    var assembled = DynamicCardAssembler.AssembleCard(card, cardThumbnail.rectTransform);
                    if (assembled != null)
                    {
                        var atkTmpTrans = assembled.transform.Find("ReferenceCanvas_750x1050/NameBar/AtkBadge/AtkText");
                        if (atkTmpTrans != null)
                        {
                            var atkTmp = atkTmpTrans.GetComponent<TextMeshProUGUI>();
                            if (atkTmp != null)
                            {
                                atkTmp.text = effectivePower.ToString();
                            }
                        }
                    }
                }

                // if (slotFrame != null)
                // {
                //     slotFrame.color = canUndo
                //         ? new Color(0.3f, 0.85f, 1f, 0.9f) // Bright cyan tint if undoable
                //         : new Color(1f, 1f, 1f, 0.45f);
                // }

                if (powerBadgeRoot != null)
                {
                    powerBadgeRoot.SetActive(true);
                    if (powerBadgeText != null)
                    {
                        powerBadgeText.text = effectivePower.ToString();
                    }
                }

                if (undoBadgeRoot != null)
                {
                    undoBadgeRoot.SetActive(canUndo);
                }
            }
        }

        private void HandleSlotClicked()
        {
            if (CanUndo && onSlotClicked != null)
            {
                onSlotClicked.Invoke(LaneIndex, SlotIndex);
            }
        }
    }
}
