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
    /// Main controller for the Home / Album screen ("ÁLBUM DE CARTAS").
    /// Faithful recreation of the UI_test.png mockup:
    /// - Header: Back button, Title, Currency indicator
    /// - Filter bar: 4 Category tabs (Héroes, Bestias, Trompos, Arenas) and Clan dropdown
    /// - Left Panel: "MIS CARTAS" scrollable thumbnail grid (locked & unlocked cards)
    /// - Center Viewport: Interactive 3D Card with swipe-to-rotate & dynamic URP foil shaders
    /// - Right Panel: Detailed inspector (Stats, Clan advantage, Lore, Foil upgrade progress)
    /// </summary>
    public class Home : MonoBehaviour
    {
        [Header("3D Card Viewport & 360 Orbit")]
        [SerializeField] private Card3D card3D;
        [SerializeField] private Card3DViewer card3DViewer;
        [SerializeField] private CardView3D cardView3D;
        [SerializeField] private GameObject swipeToRotateIndicator;

        [Header("Center 3D Controls (360 Inspection)")]
        [SerializeField] private Button flipCardButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private TextMeshProUGUI autoSpinButtonText;
        [SerializeField] private Button resetFrontButton;
        [SerializeField] private Button reversoOroButton;
        [SerializeField] private Button reversoColorButton;
        [SerializeField] private Button reversoBiButton;

        [Header("Header Bar")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldCoinsText;
        [SerializeField] private TextMeshProUGUI esenciaDustText;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TextMeshProUGUI accountLevelText;
        [SerializeField] private Slider accountLevelProgressBar;
        [SerializeField] private TextMeshProUGUI accountLevelXpText;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button showcase3DButton;
        [SerializeField] private Button battleButton;

        [Header("Deck Management Header")]
        [SerializeField] private Button deckButton;
        [SerializeField] private TextMeshProUGUI deckButtonText;
        [SerializeField] private DeckManagerModal deckModal;

        [Header("Card Packs & Shop (Rules 1 & 11)")]
        [SerializeField] private Button packsButton;
        [SerializeField] private PackShopModal packShopModal;
        [SerializeField] private PackOpeningModal packOpeningModal;

        [Header("Player Avatar & Identity")]
        [SerializeField] private Button playerAvatarButton;
        [SerializeField] private Image playerAvatarImage;
        [SerializeField] private AvatarSelectionModal avatarModal;

        [Header("Daily Rewards")]
        [SerializeField] private Button dailyRewardButton;
        [SerializeField] private GameObject dailyRewardBadge;
        [SerializeField] private DailyRewardModal dailyRewardModal;

        [Header("Shop Modal & Monetization")]
        [SerializeField] private GameObject shopModalRoot;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Button buyPack1000Button;
        [SerializeField] private Button buyPack5000Button;
        [SerializeField] private Button buyPack10000Button;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TextMeshProUGUI modalGoldBalanceText;
        [SerializeField] private TextMeshProUGUI shopStatusText;

        [Header("Category Navigation Tabs")]
        [SerializeField] private Button heroesTabButton;
        [SerializeField] private Button bestiasTabButton;
        [SerializeField] private Button tromposTabButton;
        [SerializeField] private Button arenasTabButton;

        [Header("Clan Filter")]
        [SerializeField] private TMP_Dropdown clanFilterDropdown;

        [Header("Left Panel: 'MIS CARTAS' Grid")]
        [SerializeField] private Transform gridContentParent;
        [SerializeField] private GameObject cardGridItemPrefab;
        [SerializeField] private TextMeshProUGUI gridHeaderTitle;

        [Header("Right Panel: Card Details")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardLevelText;
        [SerializeField] private TextMeshProUGUI cardClanText;
        [SerializeField] private TextMeshProUGUI clanAdvantageText;

        [Header("Stats Row")]
        [SerializeField] private GameObject statsContainer;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Rules & Lore Section")]
        [SerializeField] private TextMeshProUGUI rulesText;
        [SerializeField] private TextMeshProUGUI activeAbilityText;
        [SerializeField] private TextMeshProUGUI loreText;

        [Header("Foil Progression Section")]
        [SerializeField] private Slider foilProgressBar;
        [SerializeField] private TextMeshProUGUI foilProgressLabel;
        [SerializeField] private Button upgradeFoilButton;
        [SerializeField] private TextMeshProUGUI upgradeCostText;

        [Header("Deck Customization (Card Details)")]
        [SerializeField] private Button toggleDeckCardButton;
        [SerializeField] private TextMeshProUGUI toggleDeckCardButtonText;

        [Header("Card Crafting (Option A - Esencia Cometa)")]
        [SerializeField] private Button craftCardButton;
        [SerializeField] private TextMeshProUGUI craftCardButtonText;

        // Runtime State
        private CardCategory currentCategory = CardCategory.Champion;
        private ClanType currentClanFilter = ClanType.None;
        private CardDefinition? selectedCard;
        private readonly List<CardGridItem> spawnedGridItems = new();

        private PlayerProfile Profile => SaveManager.Instance.CurrentProfile;
        private const int RequiredCopiesForFoil = PlayerProfile.RequiredCopiesForFoilUpgrade;
        private const int FoilUpgradeGoldCost = PlayerProfile.GoldCostPerFoilUpgrade;

        private void Awake()
        {
            EnsureClear3DViewport();
            EnsureStudioLighting();
        }

        private void EnsureClear3DViewport()
        {
            // 1. Disable full-screen blocking Background if present on the Canvas
            var bg = transform.Find("Background");
            if (bg != null)
            {
                var bgImg = bg.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.enabled = false;
                    bgImg.raycastTarget = false;
                }
            }

            // 2. Ensure Main Camera renders dark background and is clear
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            }

            // 3. Ensure UI side panels have their own dark backgrounds so grid/details are legible
            var lp = transform.Find("LeftPanel_Cards");
            if (lp != null && lp.GetComponent<Image>() == null)
            {
                var lpImg = lp.gameObject.AddComponent<Image>();
                lpImg.color = new Color(0.10f, 0.11f, 0.16f, 0.95f);
            }

            var hb = transform.Find("HeaderBar");
            if (hb != null && hb.GetComponent<Image>() == null)
            {
                var hbImg = hb.gameObject.AddComponent<Image>();
                hbImg.color = new Color(0.07f, 0.08f, 0.12f, 0.95f);
            }

            var tabs = transform.Find("CategoryTabs");
            if (tabs != null && tabs.GetComponent<Image>() == null)
            {
                var tabsImg = tabs.gameObject.AddComponent<Image>();
                tabsImg.color = new Color(0.08f, 0.09f, 0.13f, 0.90f);
            }

            // 4. Ensure Center_Card3D_Viewport exists & is properly positioned in the middle
            var centerObj = GameObject.Find("Center_Card3D_Viewport");
            if (centerObj == null)
            {
                centerObj = new GameObject("Center_Card3D_Viewport");
                centerObj.transform.position = new Vector3(0.18f, 0.15f, -4.5f);
            }
            else
            {
                centerObj.transform.position = new Vector3(0.18f, 0.15f, -4.5f);
            }

            if (card3D == null)
            {
                card3D = centerObj.GetComponent<Card3D>() ?? centerObj.AddComponent<Card3D>();
            }
            card3D.EnsureHierarchy();

            if (card3DViewer == null)
            {
                card3DViewer = centerObj.GetComponent<Card3DViewer>() ?? centerObj.AddComponent<Card3DViewer>();
            }

            // 5. Ensure Showcase_Pedestal is stationary and unparented from rotating anchor
            var pedestal = GameObject.Find("Showcase_Pedestal");
            if (pedestal != null)
            {
                if (pedestal.transform.parent != null)
                {
                    pedestal.transform.SetParent(null, true);
                }
                pedestal.transform.position = new Vector3(0.18f, -1.95f, -4.5f);
                pedestal.transform.localScale = new Vector3(3.8f, 0.07f, 3.8f);
            }
        }

        private void EnsureStudioLighting()
        {
            var keyLight = GameObject.Find("Key_Light");
            if (keyLight == null)
            {
                var keyObj = new GameObject("Key_Light");
                var light = keyObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.97f, 0.90f);
                keyObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            }

            var fillLight = GameObject.Find("Fill_Light");
            if (fillLight == null)
            {
                var fillObj = new GameObject("Fill_Light");
                var light = fillObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.7f;
                light.color = new Color(0.65f, 0.8f, 1f);
                fillObj.transform.rotation = Quaternion.Euler(15f, 60f, 0f);
            }

            var rimLight = GameObject.Find("Rim_Light");
            if (rimLight == null)
            {
                var rimObj = new GameObject("Rim_Light");
                var light = rimObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                light.color = new Color(1f, 0.85f, 0.55f);
                rimObj.transform.rotation = Quaternion.Euler(-25f, 160f, 0f);
            }
        }

        private void OnEnable()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnProfileLoaded += HandleProfileLoaded;
                SaveManager.Instance.OnProfileSaved += UpdateCurrencyDisplay;
            }
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnProfileLoaded -= HandleProfileLoaded;
                SaveManager.Instance.OnProfileSaved -= UpdateCurrencyDisplay;
            }

            if (avatarModal != null)
            {
                avatarModal.OnAvatarChanged -= HandleAvatarChanged;
            }

            if (dailyRewardModal != null)
            {
                dailyRewardModal.OnDailyRewardClaimed -= HandleDailyRewardClaimed;
            }
        }

        private void HandleProfileLoaded()
        {
            UpdateCurrencyDisplay();
            UpdateAvatarDisplay();
            UpdateDailyRewardBadge();
            if (playerNameInput != null && Profile != null)
            {
                playerNameInput.text = Profile.PlayerName;
            }
            RefreshGrid();
        }

        private void Start()
        {
            // Ensure SaveManager has initialized and loaded the profile from disk
            _ = SaveManager.Instance;

            SetupHeaderAndCurrency();
            SetupNavigation();
            SetupClanDropdown();
            Setup3DControls();
            SelectCategory(CardCategory.Champion);
            UpdateDeckUI();

            // Guarantee a card is visible on initial load if not already set by category selection
            if (card3D != null && card3D.CurrentCardData == null)
            {
                var initialCard = CardShowcaseCatalog.Cards.Count > 0 ? CardShowcaseCatalog.Cards[0] : null;
                if (initialCard != null)
                {
                    card3D.SetCard(initialCard, 1);
                    card3D.SetReverso(currentReversoId);
                }
            }
        }

        private void SetupHeaderAndCurrency()
        {
            if (titleText != null) titleText.text = "ÁLBUM DE CARTAS";
            if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
            UpdateCurrencyDisplay();
            SetupPlayerNameInput();
            SetupPlayerAvatar();
            SetupDailyRewards();
            SetupPackShop();
            SetupShopModal();
        }

        private void SetupPackShop()
        {
            if (packsButton != null)
            {
                packsButton.onClick.RemoveAllListeners();
                packsButton.onClick.AddListener(OnPacksButtonClicked);
            }
        }

        private void OnPacksButtonClicked()
        {
            if (packShopModal != null)
            {
                packShopModal.Show(onInventoryUpdated: () =>
                {
                    UpdateCurrencyDisplay();
                    RefreshGrid();
                    UpdateDeckUI();
                });
            }
        }

        private void SetupPlayerAvatar()
        {
            if (playerAvatarButton != null)
            {
                playerAvatarButton.onClick.RemoveAllListeners();
                playerAvatarButton.onClick.AddListener(OnAvatarButtonClicked);
            }

            if (avatarModal != null)
            {
                avatarModal.OnAvatarChanged -= HandleAvatarChanged;
                avatarModal.OnAvatarChanged += HandleAvatarChanged;
            }

            UpdateAvatarDisplay();
        }

        public void UpdateAvatarDisplay()
        {
            if (playerAvatarImage != null && Profile != null)
            {
                var sprite = AvatarSpriteProvider.GetAvatarSprite(Profile.SelectedAvatarId);
                if (sprite != null)
                {
                    playerAvatarImage.sprite = sprite;
                    playerAvatarImage.preserveAspect = true;
                }
            }
        }

        private void OnAvatarButtonClicked()
        {
            if (avatarModal != null)
            {
                avatarModal.OpenModal();
            }
        }

        private void HandleAvatarChanged(string avatarId)
        {
            UpdateAvatarDisplay();
            UpdateCurrencyDisplay();
        }

        private void SetupDailyRewards()
        {
            if (dailyRewardButton != null)
            {
                dailyRewardButton.onClick.RemoveAllListeners();
                dailyRewardButton.onClick.AddListener(OnDailyRewardButtonClicked);
            }

            if (dailyRewardModal != null)
            {
                dailyRewardModal.OnDailyRewardClaimed -= HandleDailyRewardClaimed;
                dailyRewardModal.OnDailyRewardClaimed += HandleDailyRewardClaimed;
            }

            UpdateDailyRewardBadge();
        }

        private void OnDailyRewardButtonClicked()
        {
            if (dailyRewardModal != null)
            {
                dailyRewardModal.OpenModal();
            }
        }

        private void HandleDailyRewardClaimed(DailyRewardResult result)
        {
            UpdateCurrencyDisplay();
            UpdateAvatarDisplay();
            RefreshGrid();
            UpdateDailyRewardBadge();
        }

        public void UpdateDailyRewardBadge()
        {
            if (dailyRewardBadge != null && Profile != null)
            {
                dailyRewardBadge.SetActive(Profile.CanClaimDailyReward(DateTime.UtcNow));
            }
        }

        private void SetupPlayerNameInput()
        {
            if (playerNameInput == null) return;

            playerNameInput.characterLimit = 16;
            playerNameInput.text = Profile != null ? Profile.PlayerName : PlayerProfile.GenerateDefaultPlayerName();
            playerNameInput.onEndEdit.RemoveAllListeners();
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
        }

        private void OnPlayerNameEndEdit(string newName)
        {
            if (Profile == null) return;

            if (Profile.TrySetPlayerName(newName, out var failureReason))
            {
                SaveManager.Instance.SaveProfile();
                playerNameInput.text = Profile.PlayerName;
                Debug.Log($"[Home] Player nickname updated and saved: {Profile.PlayerName}");
            }
            else
            {
                Debug.LogWarning($"[Home] Invalid player nickname: {failureReason}");
                playerNameInput.text = Profile.PlayerName; // Revert to valid name
            }
        }

        private void SetupNavigation()
        {
            if (heroesTabButton != null) heroesTabButton.onClick.AddListener(() => SelectCategory(CardCategory.Champion));
            if (bestiasTabButton != null) bestiasTabButton.onClick.AddListener(() => SelectCategory(CardCategory.KombatBeast));
            if (tromposTabButton != null) tromposTabButton.onClick.AddListener(() => SelectCategory(CardCategory.PhysicalTrompo));
            if (arenasTabButton != null) arenasTabButton.onClick.AddListener(() => SelectCategory(CardCategory.Arena));

            if (upgradeFoilButton != null)
            {
                upgradeFoilButton.onClick.AddListener(OnUpgradeFoilClicked);
            }

            if (craftCardButton != null)
            {
                craftCardButton.onClick.AddListener(OnCraftCardClicked);
            }

            if (showcase3DButton != null)
            {
                showcase3DButton.onClick.AddListener(OnShowcase3DClicked);
            }

            if (battleButton != null)
            {
                battleButton.onClick.AddListener(OnBattleClicked);
            }

            if (deckButton != null)
            {
                deckButton.onClick.AddListener(OnDeckButtonClicked);
            }

            if (toggleDeckCardButton != null)
            {
                toggleDeckCardButton.onClick.AddListener(OnToggleDeckCardClicked);
            }
        }

        public SavedDeckData GetActiveCombatDeck()
        {
            if (Profile == null) return null;
            if (Profile.SavedDecks == null) Profile.SavedDecks = new List<SavedDeckData>();

            var deck = Profile.SavedDecks.FirstOrDefault();
            if (deck == null)
            {
                var autoDeck = DeckValidator.CreateDeckFromInventory(Profile);
                deck = new SavedDeckData("Mazo de Combate", ClanType.None, autoDeck.Select(c => c.Id));
                Profile.SavedDecks.Add(deck);
                SaveManager.Save();
            }
            return deck;
        }

        public void UpdateDeckUI()
        {
            var activeDeck = GetActiveCombatDeck();
            int count = activeDeck?.CardIds?.Count ?? 0;

            if (deckButtonText != null)
            {
                deckButtonText.text = $"🃏 MAZO ({count}/{DeckValidator.ArenaRequiredDeckSize})";
            }

            if (toggleDeckCardButton != null)
            {
                if (selectedCard == null || selectedCard.Category == CardCategory.Arena || selectedCard.Type == CardType.Arena)
                {
                    toggleDeckCardButton.gameObject.SetActive(false);
                }
                else
                {
                    bool isUnlocked = Profile != null && DeckValidator.IsCardUnlockedInProfile(Profile, selectedCard.Id);
                    if (!isUnlocked)
                    {
                        toggleDeckCardButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        toggleDeckCardButton.gameObject.SetActive(true);
                        int copiesInDeck = activeDeck?.CardIds != null
                            ? activeDeck.CardIds.Count(id => string.Equals(id, selectedCard.Id, StringComparison.OrdinalIgnoreCase))
                            : 0;

                        if (copiesInDeck == 0)
                        {
                            if (toggleDeckCardButtonText != null)
                                toggleDeckCardButtonText.text = "➕ AÑADIR AL MAZO (0/2)";
                            toggleDeckCardButton.interactable = count < DeckValidator.ArenaRequiredDeckSize;
                        }
                        else if (copiesInDeck == 1)
                        {
                            if (toggleDeckCardButtonText != null)
                                toggleDeckCardButtonText.text = count < DeckValidator.ArenaRequiredDeckSize ? "➕ AÑADIR COPIA (1/2)" : "➖ QUITAR COPIA (1/2)";
                            toggleDeckCardButton.interactable = true;
                        }
                        else
                        {
                            if (toggleDeckCardButtonText != null)
                                toggleDeckCardButtonText.text = "➖ QUITAR DEL MAZO (2/2)";
                            toggleDeckCardButton.interactable = true;
                        }
                    }
                }
            }
        }

        private void OnDeckButtonClicked()
        {
            if (deckModal != null)
            {
                deckModal.Show(Profile, UpdateDeckUI);
            }
        }

        private void OnToggleDeckCardClicked()
        {
            if (selectedCard == null || Profile == null) return;
            var activeDeck = GetActiveCombatDeck();
            if (activeDeck == null) return;

            if (activeDeck.CardIds == null) activeDeck.CardIds = new List<string>();

            int copiesInDeck = activeDeck.CardIds.Count(id => string.Equals(id, selectedCard.Id, StringComparison.OrdinalIgnoreCase));

            if (copiesInDeck < DeckValidator.ArenaMaxCopiesPerCard && activeDeck.CardIds.Count < DeckValidator.ArenaRequiredDeckSize)
            {
                activeDeck.CardIds.Add(selectedCard.Id);
            }
            else if (copiesInDeck > 0)
            {
                activeDeck.CardIds.Remove(selectedCard.Id);
            }

            SaveManager.Save();
            UpdateDeckUI();
            if (deckModal != null)
            {
                deckModal.RefreshGrid();
            }
        }

        private void OnBattleClicked()
        {
            var activeDeck = GetActiveCombatDeck();
            if (activeDeck == null || activeDeck.CardIds == null || activeDeck.CardIds.Count < DeckValidator.ArenaRequiredDeckSize)
            {
                var autoDeck = DeckValidator.CreateDeckFromInventory(Profile);
                if (autoDeck != null && autoDeck.Count == DeckValidator.ArenaRequiredDeckSize)
                {
                    if (activeDeck != null)
                    {
                        activeDeck.CardIds = autoDeck.Select(c => c.Id).ToList();
                        SaveManager.Save();
                    }
                }
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
        }

        private void OnShowcase3DClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CardShowcase");
        }

        private void SetupClanDropdown()
        {
            Debug.Log("Setting up clan dropdown");
            if (clanFilterDropdown == null) 
            {
                Debug.LogWarning("Clan filter dropdown is not assigned.");
                return;
            }

            clanFilterDropdown.ClearOptions();
            var options = new List<string>
            {
                "Todos los Clanes",
                "Milenarios (🌳)",
                "Fugitivos (⛓️)",
                "Depredadores (🪓)",
                "Míticos (🐉)"
            };
            clanFilterDropdown.AddOptions(options);
            clanFilterDropdown.onValueChanged.AddListener(OnClanFilterChanged);
        }

        public void SelectCategory(CardCategory category)
        {
            currentCategory = category;
            RefreshGrid();
        }

        private void OnClanFilterChanged(int index)
        {
            currentClanFilter = index switch
            {
                1 => ClanType.Milenarios,
                2 => ClanType.Fugitivos,
                3 => ClanType.Depredadores,
                4 => ClanType.Miticos,
                _ => ClanType.None
            };
            RefreshGrid();
        }

        public void RefreshGrid()
        {
            // Clear existing spawned items
            foreach (var item in spawnedGridItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            spawnedGridItems.Clear();

            // Filter catalog
            var cards = CardCatalog.GetByCategory(currentCategory);
            if (currentClanFilter != ClanType.None)
            {
                cards = cards.Where(c => c.Clan == currentClanFilter);
            }

            var cardList = cards.ToList();
            if (cardList.Count == 0) return;

            // Determine target card to display in details & 3D view:
            // 1. Preserve current selection if valid in this category/filter
            // 2. Otherwise prefer Espectro if available (showcase character from UI_test.png)
            // 3. Otherwise first unlocked card in category
            CardDefinition? targetSelection = null;
            if (selectedCard != null && cardList.Any(c => c.Id == selectedCard.Id))
            {
                targetSelection = selectedCard;
            }
            else
            {
                targetSelection = cardList.FirstOrDefault(c => c.Id == "CHAMP_ESPECTRO" && Profile != null && Profile.IsCardUnlocked(c.Id))
                    ?? cardList.FirstOrDefault(c => Profile != null && Profile.IsCardUnlocked(c.Id))
                    ?? cardList[0];
            }

            // Populate grid
            foreach (var card in cardList)
            {
                bool isUnlocked = Profile != null && Profile.IsCardUnlocked(card.Id);
                bool isSelected = targetSelection != null && targetSelection.Id == card.Id;

                if (gridContentParent != null && cardGridItemPrefab != null)
                {
                    var go = Instantiate(cardGridItemPrefab, gridContentParent);
                    var gridItem = go.GetComponent<CardGridItem>();
                    if (gridItem != null)
                    {
                        gridItem.Setup(card, isUnlocked, isSelected, OnCardItemClicked);
                        spawnedGridItems.Add(gridItem);
                    }
                }
            }

            if (targetSelection != null)
            {
                SelectCard(targetSelection);
            }
        }

        private void OnCardItemClicked(CardDefinition card)
        {
            SelectCard(card);
        }

        public void SelectCard(CardDefinition card)
        {
            if (card == null) return;
            selectedCard = card;

            // Highlight selected item in grid
            foreach (var item in spawnedGridItems)
            {
                if (item != null)
                {
                    item.SetSelected(item.CardData != null && item.CardData.Id == card.Id);
                }
            }

            int currentFoil = Profile != null ? Profile.GetFoilLevel(card.Id) : 1;

            // 1. Update Central 3D Card
            if (card3D != null)
            {
                card3D.SetCard(card, currentFoil);
                card3D.SetReverso(currentReversoId);
            }
            if (cardView3D != null)
            {
                cardView3D.Bind(card, currentFoil);
            }

            // 2. Update Details Panel Header
            if (cardNameText != null) cardNameText.text = card.Name.ToUpper();
            if (cardLevelText != null) cardLevelText.text = $"LVL {currentFoil:00}";
            if (cardClanText != null) cardClanText.text = $"CLAN: {card.Clan}";

            // 3. Dynamic Clan Advantage Tip
            if (clanAdvantageText != null)
            {
                var targetClan = ClanAdvantage.GetAdvantageAgainst(card.Clan);
                clanAdvantageText.text = targetClan != ClanType.None
                    ? $"<color=#00FF88>★ Ventaja contra: {targetClan} (+1 Daño)</color>"
                    : string.Empty;
            }

            // 4. Stats Row (Hide for Trompos / Arenas)
            bool isCreature = card.IsCreature;
            if (statsContainer != null) statsContainer.SetActive(isCreature);

            if (costText != null) costText.text = card.EnergyCost.ToString();
            if (isCreature)
            {
                if (attackText != null) attackText.text = card.BaseAttack.ToString();
                if (healthText != null) healthText.text = card.BaseHealth.ToString();
            }

            // 5. Rules & Lore
            if (rulesText != null)
            {
                rulesText.text = !string.IsNullOrEmpty(card.AbilityText)
                    ? $"<b>REGLAS TCG:</b> {card.AbilityText}"
                    : "<b>REGLAS TCG:</b> Sin habilidades especiales de entrada.";
            }

            if (loreText != null)
            {
                loreText.text = !string.IsNullOrEmpty(card.LoreText)
                    ? $"<b>Texto de Lore:</b> {card.LoreText}"
                    : string.Empty;
            }

            if (activeAbilityText != null)
            {
                activeAbilityText.text = !string.IsNullOrEmpty(card.IconicQuote)
                    ? $"<i>\"{card.IconicQuote}\"</i>"
                    : string.Empty;
            }

            // 6. Foil Upgrade Progress
            UpdateFoilProgressUI(card);

            // 7. Deck Status & Customization
            UpdateDeckUI();
        }

        private void UpdateFoilProgressUI(CardDefinition card)
        {
            bool isUnlocked = Profile != null && Profile.IsCardUnlocked(card.Id);
            int currentCopies = Profile != null ? Profile.GetDuplicateCopies(card.Id) : 0;
            int currentGold = Profile != null ? Profile.GoldCoins : 0;
            int currentFoil = Profile != null ? Profile.GetFoilLevel(card.Id) : 1;
            int currentDust = Profile != null ? Profile.EsenciaCometa : 0;
            int craftCost = CardCraftingConfig.GetCraftCost(card.Rarity);
            bool canAffordCraft = currentDust >= craftCost;

            if (foilProgressBar != null)
            {
                foilProgressBar.maxValue = RequiredCopiesForFoil;
                foilProgressBar.value = isUnlocked ? currentCopies : 0;
            }

            if (!isUnlocked)
            {
                if (foilProgressLabel != null)
                {
                    foilProgressLabel.text = $"🔒 Carta bloqueada • Fabrícala con {craftCost} ✨ Esencia";
                }

                if (upgradeFoilButton != null)
                {
                    upgradeFoilButton.gameObject.SetActive(false);
                }

                if (toggleDeckCardButton != null)
                {
                    toggleDeckCardButton.gameObject.SetActive(false);
                }

                if (craftCardButton != null)
                {
                    craftCardButton.gameObject.SetActive(true);
                    craftCardButton.interactable = canAffordCraft;
                    var rt = craftCardButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(205, -630);
                }

                if (craftCardButtonText != null)
                {
                    craftCardButtonText.text = $"✨ CRAFTEAR CARTA ({craftCost} ✨)";
                }
            }
            else
            {
                if (foilProgressLabel != null)
                {
                    if (currentFoil >= CardInventoryEntry.MaxFoilLevel)
                    {
                        foilProgressLabel.text = "¡Nivel Foil Máximo alcanzado!";
                    }
                    else
                    {
                        foilProgressLabel.text = $"Cartas obtenidas: {currentCopies}/{RequiredCopiesForFoil} para versión Foil";
                    }
                }

                if (upgradeCostText != null)
                {
                    upgradeCostText.text = FoilUpgradeGoldCost.ToString();
                }

                if (upgradeFoilButton != null)
                {
                    upgradeFoilButton.gameObject.SetActive(true);
                    bool canUpgrade = currentFoil < CardInventoryEntry.MaxFoilLevel 
                        && currentCopies >= RequiredCopiesForFoil 
                        && currentGold >= FoilUpgradeGoldCost;
                    upgradeFoilButton.interactable = canUpgrade;
                }

                if (craftCardButton != null)
                {
                    if (currentFoil < CardInventoryEntry.MaxFoilLevel)
                    {
                        craftCardButton.gameObject.SetActive(true);
                        craftCardButton.interactable = canAffordCraft;
                        var rt = craftCardButton.GetComponent<RectTransform>();
                        if (rt != null) rt.anchoredPosition = new Vector2(205, -770);

                        if (craftCardButtonText != null)
                        {
                            craftCardButtonText.text = $"✨ CREAR COPIA (+1 COPIA • {craftCost} ✨)";
                        }
                    }
                    else
                    {
                        craftCardButton.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnUpgradeFoilClicked()
        {
            if (selectedCard == null || Profile == null) return;

            if (Profile.TryUpgradeFoil(selectedCard.Id, out var failureReason))
            {
                SaveManager.Instance.SaveProfile();

                int nextFoilLevel = Profile.GetFoilLevel(selectedCard.Id);
                if (card3D != null)
                {
                    card3D.SetFoilLevel(nextFoilLevel);
                }
                if (cardView3D != null)
                {
                    cardView3D.ApplyFoilLevel(nextFoilLevel);
                }

                UpdateCurrencyDisplay();
                UpdateFoilProgressUI(selectedCard);

                if (cardLevelText != null)
                {
                    cardLevelText.text = $"LVL {nextFoilLevel:00}";
                }
                Debug.Log($"[Home] Successfully upgraded {selectedCard.Name} to Foil Level {nextFoilLevel}!");
            }
            else
            {
                Debug.LogWarning($"[Home] Cannot upgrade foil: {failureReason}");
            }
        }

        private void OnCraftCardClicked()
        {
            if (selectedCard == null || Profile == null) return;

            bool wasLocked = !Profile.IsCardUnlocked(selectedCard.Id);
            if (Profile.TryCraftCard(selectedCard, out var failureReason))
            {
                SaveManager.Instance.SaveProfile();

                UpdateCurrencyDisplay();
                RefreshGrid();
                SelectCard(selectedCard);

                if (wasLocked)
                {
                    Debug.Log($"[Home] ¡Carta {selectedCard.Name} ({selectedCard.Rarity}) crafteada y desbloqueada con éxito!");
                }
                else
                {
                    Debug.Log($"[Home] ¡Copia adicional de {selectedCard.Name} creada con éxito!");
                }
            }
            else
            {
                Debug.LogWarning($"[Home] No se pudo craftear la carta: {failureReason}");
            }
        }

        private void UpdateCurrencyDisplay()
        {
            if (goldCoinsText != null)
            {
                int gold = Profile != null ? Profile.GoldCoins : 0;
                goldCoinsText.text = gold.ToString("N0");
            }
            if (esenciaDustText != null)
            {
                int dust = Profile != null ? Profile.EsenciaCometa : 0;
                esenciaDustText.text = $"✨ {dust:N0}";
            }
            UpdateAccountLevelDisplay();
            UpdateModalBalance();
            UpdateDailyRewardBadge();
        }

        private void SetupShopModal()
        {
            if (openShopButton != null)
            {
                openShopButton.onClick.RemoveAllListeners();
                openShopButton.onClick.AddListener(OpenShopModal);
            }

            if (closeShopButton != null)
            {
                closeShopButton.onClick.RemoveAllListeners();
                closeShopButton.onClick.AddListener(CloseShopModal);
            }

            if (buyPack1000Button != null)
            {
                buyPack1000Button.onClick.RemoveAllListeners();
                buyPack1000Button.onClick.AddListener(() => OnBuyPackClicked(CoinPackType.Pack1000));
            }

            if (buyPack5000Button != null)
            {
                buyPack5000Button.onClick.RemoveAllListeners();
                buyPack5000Button.onClick.AddListener(() => OnBuyPackClicked(CoinPackType.Pack5000));
            }

            if (buyPack10000Button != null)
            {
                buyPack10000Button.onClick.RemoveAllListeners();
                buyPack10000Button.onClick.AddListener(() => OnBuyPackClicked(CoinPackType.Pack10000));
            }

            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveAllListeners();
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            }

            if (shopModalRoot != null)
            {
                shopModalRoot.SetActive(false);
            }
        }

        public void OpenShopModal()
        {
            if (shopModalRoot != null) shopModalRoot.SetActive(true);
            if (shopStatusText != null) shopStatusText.text = string.Empty;
            UpdateModalBalance();
        }

        public void CloseShopModal()
        {
            if (shopModalRoot != null) shopModalRoot.SetActive(false);
        }

        private void UpdateModalBalance()
        {
            if (modalGoldBalanceText != null && Profile != null)
            {
                modalGoldBalanceText.text = $"Saldo actual: <color=#FFD700>{Profile.GoldCoins:N0} Monedas</color>";
            }
        }

        private void OnBuyPackClicked(CoinPackType packType)
        {
            if (shopStatusText != null) shopStatusText.text = "Procesando compra...";
            MonetizationService.Instance.BuyCoinPack(packType, (success, message) =>
            {
                if (shopStatusText != null)
                {
                    shopStatusText.text = success
                        ? $"<color=#00FF88>{message}</color>"
                        : $"<color=#FF4444>{message}</color>";
                }
                UpdateCurrencyDisplay();
            });
        }

        private void OnWatchAdClicked()
        {
            if (shopStatusText != null) shopStatusText.text = "Cargando anuncio de Unity Ads...";
            MonetizationService.Instance.ShowRewardedVideo((success, message) =>
            {
                if (shopStatusText != null)
                {
                    shopStatusText.text = success
                        ? $"<color=#00FF88>{message}</color>"
                        : $"<color=#FF4444>{message}</color>";
                }
                UpdateCurrencyDisplay();
            });
        }

        private void UpdateAccountLevelDisplay()
        {
            if (Profile == null) return;

            if (accountLevelText != null)
            {
                accountLevelText.text = Profile.IsMaxAccountLevel
                    ? "LVL 20 <color=#FFD700>(MAX)</color>"
                    : $"LVL {Profile.AccountLevel:00}";
            }

            int neededXp = PlayerProfile.GetXpRequiredForNextLevel(Profile.AccountLevel);

            if (accountLevelProgressBar != null)
            {
                if (Profile.IsMaxAccountLevel)
                {
                    accountLevelProgressBar.maxValue = 1;
                    accountLevelProgressBar.value = 1;
                }
                else
                {
                    accountLevelProgressBar.maxValue = Math.Max(1, neededXp);
                    accountLevelProgressBar.value = Math.Clamp(Profile.CurrentLevelXp, 0, neededXp);
                }
            }

            if (accountLevelXpText != null)
            {
                accountLevelXpText.text = Profile.IsMaxAccountLevel
                    ? "NIVEL MÁXIMO"
                    : $"{Profile.CurrentLevelXp} / {neededXp} XP";
            }
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("[Home] Back button clicked.");
        }

        private string currentReversoId = "oro";

        private void Setup3DControls()
        {
            if (card3D == null) card3D = FindAnyObjectByType<Card3D>();
            if (card3DViewer == null) card3DViewer = FindAnyObjectByType<Card3DViewer>();

            // Self-heal pedestal shader if needed
            var pedestal = GameObject.Find("Showcase_Pedestal");
            if (pedestal != null)
            {
                var pedMr = pedestal.GetComponent<MeshRenderer>();
                if (pedMr != null)
                {
                    var curShader = pedMr.sharedMaterial != null ? pedMr.sharedMaterial.shader : null;
                    if (curShader == null || !curShader.isSupported || (curShader.name.Contains("Universal Render Pipeline") && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null))
                    {
                        var pedShader = Card3D.GetAppropriateShader(false);
                        var pedMat = new Material(pedShader) { name = "Pedestal_Mat" };
                        if (pedMat.HasProperty("_Color")) pedMat.SetColor("_Color", new Color(0.10f, 0.11f, 0.15f));
                        if (pedMat.HasProperty("_BaseColor")) pedMat.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.15f));
                        if (pedMat.HasProperty("_Metallic")) pedMat.SetFloat("_Metallic", 0.6f);
                        if (pedMat.HasProperty("_Glossiness")) pedMat.SetFloat("_Glossiness", 0.7f);
                        if (pedMat.HasProperty("_Smoothness")) pedMat.SetFloat("_Smoothness", 0.7f);
                        pedMr.material = pedMat;
                    }
                }
            }

            if (flipCardButton != null) flipCardButton.onClick.AddListener(OnFlipCardClicked);
            if (autoSpinButton != null) autoSpinButton.onClick.AddListener(OnAutoSpinClicked);
            if (resetFrontButton != null) resetFrontButton.onClick.AddListener(OnResetFrontClicked);

            if (reversoOroButton != null) reversoOroButton.onClick.AddListener(() => SetReverso("oro"));
            if (reversoColorButton != null) reversoColorButton.onClick.AddListener(() => SetReverso("color"));
            if (reversoBiButton != null) reversoBiButton.onClick.AddListener(() => SetReverso("bi"));

            UpdateReversoButtonsVisual();
            UpdateAutoSpinButtonVisual();
        }

        private void OnFlipCardClicked()
        {
            if (card3DViewer != null) card3DViewer.FlipCard180();
        }

        private void OnAutoSpinClicked()
        {
            if (card3DViewer != null)
            {
                card3DViewer.ToggleAutoSpin();
                UpdateAutoSpinButtonVisual();
            }
        }

        private void OnResetFrontClicked()
        {
            if (card3DViewer != null)
            {
                card3DViewer.ResetToFront();
                UpdateAutoSpinButtonVisual();
            }
        }

        public void SetReverso(string reversoId)
        {
            currentReversoId = reversoId;
            if (card3D != null)
            {
                card3D.SetReverso(reversoId);
            }
            UpdateReversoButtonsVisual();
        }

        private void UpdateAutoSpinButtonVisual()
        {
            if (autoSpinButtonText != null && card3DViewer != null)
            {
                autoSpinButtonText.text = card3DViewer.IsAutoSpinning ? "⏸️ Detener" : "💫 Auto-Giro";
            }
        }

        private void UpdateReversoButtonsVisual()
        {
            SetReversoBtnHighlight(reversoOroButton, currentReversoId == "oro");
            SetReversoBtnHighlight(reversoColorButton, currentReversoId == "color");
            SetReversoBtnHighlight(reversoBiButton, currentReversoId == "bi");
        }

        private void SetReversoBtnHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active ? new Color(0.95f, 0.75f, 0.2f, 1f) : new Color(0.18f, 0.2f, 0.28f, 0.85f);
            }
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = active ? Color.black : Color.white;
            }
        }
    }
}
