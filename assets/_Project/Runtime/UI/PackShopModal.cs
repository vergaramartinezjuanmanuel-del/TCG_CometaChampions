#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Dedicated Pack Shop section in Home (Rule 1).
    /// Allows players to purchase 5-card packs for 500 gold coins (single or multi-pack),
    /// open saved booster packs, and guarantees at least 1 new unowned card per pack.
    /// </summary>
    public class PackShopModal : MonoBehaviour
    {
        [Header("Modal Windows")]
        [SerializeField] private GameObject modalRoot = null!;
        [SerializeField] private Button closeButton = null!;

        [Header("Currencies & Balances")]
        [SerializeField] private TextMeshProUGUI goldBalanceText = null!;
        [SerializeField] private TextMeshProUGUI boosterPacksBalanceText = null!;
        [SerializeField] private TextMeshProUGUI statusMessageText = null!;

        [Header("Direct Buy Buttons")]
        [SerializeField] private Button buy1PackButton = null!;
        [SerializeField] private TextMeshProUGUI buy1PackButtonText = null!;
        [SerializeField] private Button buy5PacksButton = null!;
        [SerializeField] private TextMeshProUGUI buy5PacksButtonText = null!;

        [Header("Custom Quantity Stepper")]
        [SerializeField] private Button decreaseQtyButton = null!;
        [SerializeField] private Button increaseQtyButton = null!;
        [SerializeField] private Button maxQtyButton = null!;
        [SerializeField] private TextMeshProUGUI quantityText = null!;
        [SerializeField] private Button buyCustomButton = null!;
        [SerializeField] private TextMeshProUGUI buyCustomButtonText = null!;

        [Header("Saved Booster Pack Opening")]
        [SerializeField] private GameObject savedBoosterSection = null!;
        [SerializeField] private Button openBoosterButton = null!;
        [SerializeField] private TextMeshProUGUI openBoosterButtonText = null!;
        [SerializeField] private Button openRewardPackButton = null!;
        [SerializeField] private TextMeshProUGUI openRewardPackButtonText = null!;

        [Header("Reveal Modal Integration")]
        [SerializeField] private PackOpeningModal packOpeningModal = null!;

        private int customPackQuantity = 1;
        private Action? onInventoryUpdatedCallback;

        private PlayerProfile Profile => SaveManager.Instance != null ? SaveManager.Instance.CurrentProfile : null!;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            if (buy1PackButton != null)
            {
                buy1PackButton.onClick.RemoveAllListeners();
                buy1PackButton.onClick.AddListener(() => PurchasePacks(1));
            }

            if (buy5PacksButton != null)
            {
                buy5PacksButton.onClick.RemoveAllListeners();
                buy5PacksButton.onClick.AddListener(() => PurchasePacks(5));
            }

            if (decreaseQtyButton != null)
            {
                decreaseQtyButton.onClick.RemoveAllListeners();
                decreaseQtyButton.onClick.AddListener(OnDecreaseQtyClicked);
            }

            if (increaseQtyButton != null)
            {
                increaseQtyButton.onClick.RemoveAllListeners();
                increaseQtyButton.onClick.AddListener(OnIncreaseQtyClicked);
            }

            if (maxQtyButton != null)
            {
                maxQtyButton.onClick.RemoveAllListeners();
                maxQtyButton.onClick.AddListener(OnMaxQtyClicked);
            }

            if (buyCustomButton != null)
            {
                buyCustomButton.onClick.RemoveAllListeners();
                buyCustomButton.onClick.AddListener(() => PurchasePacks(customPackQuantity));
            }

            if (openBoosterButton != null)
            {
                openBoosterButton.onClick.RemoveAllListeners();
                openBoosterButton.onClick.AddListener(OpenSavedBoosterPack);
            }

            if (openRewardPackButton != null)
            {
                openRewardPackButton.onClick.RemoveAllListeners();
                openRewardPackButton.onClick.AddListener(OpenSavedRewardPack);
            }
        }

        public void Show(Action? onInventoryUpdated = null)
        {
            onInventoryUpdatedCallback = onInventoryUpdated;
            customPackQuantity = 1;

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            if (statusMessageText != null)
            {
                statusMessageText.text = string.Empty;
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            onInventoryUpdatedCallback?.Invoke();
        }

        public void RefreshUI()
        {
            var profile = Profile;
            if (profile == null) return;

            // Balances
            if (goldBalanceText != null)
            {
                goldBalanceText.text = $"{profile.GoldCoins:N0} ORO";
            }

            if (boosterPacksBalanceText != null)
            {
                boosterPacksBalanceText.text = $"{profile.BoosterPacks} Sobres Comprados (5 cartas)  •  {profile.RewardPacks} Sobres de Recompensa (3 cartas)";
            }

            // Saved booster section
            if (savedBoosterSection != null)
            {
                bool hasPurchased = profile.BoosterPacks > 0;
                bool hasReward = profile.RewardPacks > 0;
                savedBoosterSection.SetActive(hasPurchased || hasReward);

                if (openBoosterButton != null)
                {
                    openBoosterButton.gameObject.SetActive(hasPurchased);
                    if (hasPurchased && openBoosterButtonText != null)
                    {
                        openBoosterButtonText.text = $"ABRIR SOBRE COMPRADO ({profile.BoosterPacks} DISP. • 5 CARTAS)";
                    }
                }

                if (openRewardPackButton != null)
                {
                    openRewardPackButton.gameObject.SetActive(hasReward);
                    if (hasReward && openRewardPackButtonText != null)
                    {
                        openRewardPackButtonText.text = $"ABRIR SOBRE DE RECOMPENSA ({profile.RewardPacks} DISP. • 3 CARTAS)";
                    }
                }
            }

            // Stepper controls
            if (quantityText != null)
            {
                quantityText.text = customPackQuantity.ToString();
            }

            int customCost = customPackQuantity * PlayerProfile.PackGoldCost;
            if (buyCustomButtonText != null)
            {
                buyCustomButtonText.text = $"COMPRAR {customPackQuantity} SOBRES ({customCost:N0} ORO)";
            }

            if (buy1PackButton != null)
            {
                buy1PackButton.interactable = profile.GoldCoins >= PlayerProfile.PackGoldCost;
            }

            if (buy5PacksButton != null)
            {
                buy5PacksButton.interactable = profile.GoldCoins >= (5 * PlayerProfile.PackGoldCost);
            }

            if (buyCustomButton != null)
            {
                buyCustomButton.interactable = profile.GoldCoins >= customCost && customPackQuantity > 0;
            }
        }

        private void OnDecreaseQtyClicked()
        {
            if (customPackQuantity > 1)
            {
                customPackQuantity--;
                RefreshUI();
            }
        }

        private void OnIncreaseQtyClicked()
        {
            var profile = Profile;
            int maxAffordable = profile != null ? profile.GoldCoins / PlayerProfile.PackGoldCost : 10;
            if (maxAffordable <= 0) maxAffordable = 1;

            if (customPackQuantity < Math.Max(1, maxAffordable))
            {
                customPackQuantity++;
                RefreshUI();
            }
        }

        private void OnMaxQtyClicked()
        {
            var profile = Profile;
            int maxAffordable = profile != null ? profile.GoldCoins / PlayerProfile.PackGoldCost : 1;
            customPackQuantity = Math.Max(1, maxAffordable);
            RefreshUI();
        }

        private void PurchasePacks(int count)
        {
            var profile = Profile;
            if (profile == null) return;

            if (!profile.TryPurchaseCardPacks(count, out var result, out string failureReason))
            {
                if (statusMessageText != null)
                {
                    statusMessageText.text = $"<color=#FF5555>{failureReason}</color>";
                }
                return;
            }

            // Persist save immediately (Rule 12)
            SaveManager.Save();
            RefreshUI();
            onInventoryUpdatedCallback?.Invoke();

            // Hide the shop modal so it does not overlap with pack opening
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            // Trigger opening reveal modal
            if (packOpeningModal != null)
            {
                packOpeningModal.Show(result.Packs, () =>
                {
                    if (modalRoot != null) modalRoot.SetActive(true);
                    RefreshUI();
                    onInventoryUpdatedCallback?.Invoke();
                });
            }
        }

        private void OpenSavedBoosterPack()
        {
            var profile = Profile;
            if (profile == null) return;

            if (!profile.TryOpenBoosterPack(out var result, out string failureReason))
            {
                if (statusMessageText != null)
                {
                    statusMessageText.text = $"<color=#FF5555>{failureReason}</color>";
                }
                return;
            }

            // Persist save immediately (Rule 12)
            SaveManager.Save();
            RefreshUI();
            onInventoryUpdatedCallback?.Invoke();

            // Hide the shop modal so it does not overlap with pack opening
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            // Trigger opening reveal modal
            if (packOpeningModal != null)
            {
                packOpeningModal.Show(result, () =>
                {
                    if (modalRoot != null) modalRoot.SetActive(true);
                    RefreshUI();
                    onInventoryUpdatedCallback?.Invoke();
                });
            }
        }

        private void OpenSavedRewardPack()
        {
            var profile = Profile;
            if (profile == null) return;

            if (!profile.TryOpenRewardPack(out var result, out string failureReason))
            {
                if (statusMessageText != null)
                {
                    statusMessageText.text = $"<color=#FF5555>{failureReason}</color>";
                }
                return;
            }

            // Persist save immediately
            SaveManager.Save();
            RefreshUI();
            onInventoryUpdatedCallback?.Invoke();

            // Hide the shop modal so it does not overlap with pack opening
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            // Trigger opening reveal modal
            if (packOpeningModal != null)
            {
                packOpeningModal.Show(result, () =>
                {
                    if (modalRoot != null) modalRoot.SetActive(true);
                    RefreshUI();
                    onInventoryUpdatedCallback?.Invoke();
                });
            }
        }
    }
}
