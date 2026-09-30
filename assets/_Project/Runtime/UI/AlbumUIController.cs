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

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Master UI controller for the 3D Album screen ("ÁLBUM DE CARTAS").
    /// Matches the exact layout from UI_test.png:
    /// - Category tabs (Héroes, Bestias, Trompos, Arenas)
    /// - Clan filter dropdown
    /// - Left grid of card entries with locked/unlocked states
    /// - Central 3D Card Viewport
    /// - Right info panel (Stats, Rules, Lore, Clan advantage, and Foil Upgrade progress)
    /// </summary>
    public class AlbumUIController : MonoBehaviour
    {
        [Header("3D Card Viewport")]
        [SerializeField] private CardView3D cardView3D;

        [Header("Top Bar & Currency")]
        [SerializeField] private TextMeshProUGUI goldCoinsText;
        [SerializeField] private int playerGoldCoins = 1256;

        [Header("Category Navigation Buttons")]
        [SerializeField] private Button heroesTabButton;
        [SerializeField] private Button bestiasTabButton;
        [SerializeField] private Button tromposTabButton;
        [SerializeField] private Button arenasTabButton;

        [Header("Clan Filter")]
        [SerializeField] private TMP_Dropdown clanFilterDropdown;

        [Header("Right Info Panel")]
        [SerializeField] private TextMeshProUGUI cardTitleText;
        [SerializeField] private TextMeshProUGUI cardLevelText;
        [SerializeField] private TextMeshProUGUI cardClanText;
        [SerializeField] private TextMeshProUGUI clanAdvantageTipText;
        [SerializeField] private TextMeshProUGUI costStatText;
        [SerializeField] private TextMeshProUGUI attackStatText;
        [SerializeField] private TextMeshProUGUI healthStatText;
        [SerializeField] private GameObject combatStatsContainer;
        [SerializeField] private TextMeshProUGUI rulesText;
        [SerializeField] private TextMeshProUGUI activeAbilityText;
        [SerializeField] private TextMeshProUGUI loreText;

        [Header("Foil Progression")]
        [SerializeField] private Slider foilProgressSlider;
        [SerializeField] private TextMeshProUGUI foilProgressLabel;
        [SerializeField] private Button upgradeFoilButton;
        [SerializeField] private TextMeshProUGUI upgradeCostText;

        private CardCategory selectedCategory = CardCategory.Champion;
        private ClanType selectedClanFilter = ClanType.None;
        private CardDefinition currentCard;
        private int currentFoilLevel = 1;
        private int cardsCollectedForFoil = 2;
        private const int RequiredCardsForFoil = 5;
        private const int FoilUpgradeCost = 120;

        private void Start()
        {
            SetupNavigation();
            UpdateCurrencyUI();
            SelectCategory(CardCategory.Champion);
        }

        private void SetupNavigation()
        {
            if (heroesTabButton != null) heroesTabButton.onClick.AddListener(() => SelectCategory(CardCategory.Champion));
            if (bestiasTabButton != null) bestiasTabButton.onClick.AddListener(() => SelectCategory(CardCategory.KombatBeast));
            if (tromposTabButton != null) tromposTabButton.onClick.AddListener(() => SelectCategory(CardCategory.PhysicalTrompo));
            if (arenasTabButton != null) arenasTabButton.onClick.AddListener(() => SelectCategory(CardCategory.Arena));

            if (clanFilterDropdown != null)
            {
                clanFilterDropdown.ClearOptions();
                clanFilterDropdown.AddOptions(new List<string> { "Todos los Clanes", "Milenarios", "Fugitivos", "Depredadores", "Míticos" });
                clanFilterDropdown.onValueChanged.AddListener(OnClanFilterChanged);
            }

            if (upgradeFoilButton != null)
            {
                upgradeFoilButton.onClick.AddListener(OnUpgradeFoilClicked);
            }
        }

        public void SelectCategory(CardCategory category)
        {
            CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            selectedCategory = category;
            RefreshCardList();
        }

        private void OnClanFilterChanged(int index)
        {
            CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            selectedClanFilter = index switch
            {
                1 => ClanType.Milenarios,
                2 => ClanType.Fugitivos,
                3 => ClanType.Depredadores,
                4 => ClanType.Miticos,
                _ => ClanType.None
            };
            RefreshCardList();
        }

        private void RefreshCardList()
        {
            var cards = CardCatalog.GetByCategory(selectedCategory);
            if (selectedClanFilter != ClanType.None)
            {
                cards = cards.Where(c => c.Clan == selectedClanFilter);
            }

            var cardList = cards.ToList();
            if (cardList.Count > 0)
            {
                DisplayCard(cardList[0]);
            }
        }

        public void DisplayCard(CardDefinition card)
        {
            if (card == null) return;
            currentCard = card;

            // 1. Bind to 3D rotating card
            if (cardView3D != null)
            {
                cardView3D.Bind(card, currentFoilLevel);
            }

            // 2. Populate Header
            if (cardTitleText != null) cardTitleText.text = card.Name.ToUpper();
            if (cardLevelText != null) cardLevelText.text = "LVL 01";
            if (cardClanText != null) cardClanText.text = $"CLAN: {card.Clan}";

            // 3. Show Clan Advantage tip
            if (clanAdvantageTipText != null)
            {
                var targetClan = ClanAdvantage.GetAdvantageAgainst(card.Clan);
                if (targetClan != ClanType.None)
                {
                    clanAdvantageTipText.text = $"<color=#00FF88>• Ventaja contra: {targetClan} (+1 Daño)</color>";
                }
                else
                {
                    clanAdvantageTipText.text = string.Empty;
                }
            }

            // 4. Populate Stats
            if (costStatText != null) costStatText.text = card.EnergyCost.ToString();

            bool isCreature = card.IsCreature;
            if (combatStatsContainer != null) combatStatsContainer.SetActive(isCreature);
            if (isCreature)
            {
                if (attackStatText != null) attackStatText.text = card.BaseAttack.ToString();
                if (healthStatText != null) healthStatText.text = card.BaseHealth.ToString();
            }

            // 5. Populate Rules & Lore
            if (rulesText != null)
            {
                rulesText.text = !string.IsNullOrEmpty(card.AbilityText)
                    ? $"<b>REGLAS TCG:</b> {card.AbilityText}"
                    : "<b>REGLAS TCG:</b> Carta estándar de combate.";
            }

            if (loreText != null)
            {
                loreText.text = !string.IsNullOrEmpty(card.LoreText)
                    ? $"<b>Texto de Lore:</b> {card.LoreText}"
                    : string.Empty;
            }

            // 6. Update Foil Progress
            UpdateFoilProgressUI();
        }

        private void UpdateFoilProgressUI()
        {
            if (foilProgressSlider != null)
            {
                foilProgressSlider.maxValue = RequiredCardsForFoil;
                foilProgressSlider.value = cardsCollectedForFoil;
            }

            if (foilProgressLabel != null)
            {
                foilProgressLabel.text = $"Cartas obtenidas: {cardsCollectedForFoil}/{RequiredCardsForFoil} para versión Foil";
            }

            if (upgradeCostText != null)
            {
                upgradeCostText.text = FoilUpgradeCost.ToString();
            }

            if (upgradeFoilButton != null)
            {
                bool canUpgrade = cardsCollectedForFoil >= RequiredCardsForFoil && playerGoldCoins >= FoilUpgradeCost;
                upgradeFoilButton.interactable = canUpgrade;
            }
        }

        private void OnUpgradeFoilClicked()
        {
            if (cardsCollectedForFoil < RequiredCardsForFoil || playerGoldCoins < FoilUpgradeCost)
            {
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.UpgradeFoilFail);
                return;
            }

            CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(SfxType.UpgradeFoilSuccess);
            playerGoldCoins -= FoilUpgradeCost;
            cardsCollectedForFoil = 0;
            currentFoilLevel = Math.Min(3, currentFoilLevel + 1);

            if (cardView3D != null)
            {
                cardView3D.ApplyFoilLevel(currentFoilLevel);
            }

            UpdateCurrencyUI();
            UpdateFoilProgressUI();
        }

        private void UpdateCurrencyUI()
        {
            if (goldCoinsText != null)
            {
                goldCoinsText.text = playerGoldCoins.ToString("N0");
            }
        }
    }
}
