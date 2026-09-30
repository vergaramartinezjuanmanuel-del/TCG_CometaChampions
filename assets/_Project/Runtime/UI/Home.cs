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
        [Header("Scene Background")]
        [SerializeField] private Image homeBackgroundImage;
        public Image HomeBackgroundImage => homeBackgroundImage;
        public const string DefaultHomeBackground = ArenaBackgroundCatalog.Pedestal3DActive;

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

        [Header("Settings Panel")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanelRoot;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button sfxToggleButton;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private TextMeshProUGUI sfxToggleLabel;
        [SerializeField] private TextMeshProUGUI musicToggleLabel;

        // Card Viewport VFX (Rotating Vortex & Aura Particles)
        private Transform cardViewportTransform;
        private GameObject cardVfxAnchor;
        private ParticleSystem cardVortexSwirlPs;
        private ParticleSystem cardAuraOrbsPs;
        private ParticleSystem pedestalSparksPs;
        private static Material cachedParticleMaterial;
        private static Texture2D cachedParticleTexture;

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

        private void Update()
        {
            UpdateCardViewportVfxRotation();
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
                card3D = centerObj.GetComponent<Card3D>();
                if (card3D == null) card3D = centerObj.AddComponent<Card3D>();
            }
            card3D.EnsureHierarchy();

            if (card3DViewer == null)
            {
                card3DViewer = centerObj.GetComponent<Card3DViewer>();
                if (card3DViewer == null) card3DViewer = centerObj.AddComponent<Card3DViewer>();
            }

            cardViewportTransform = centerObj.transform;
            SetupCardViewportVfx(cardViewportTransform);

            // 5. Ensure Showcase_Pedestal is stationary and unparented from rotating anchor, with mesh disabled so the painted pedestal artwork is shown
            var pedestal = GameObject.Find("Showcase_Pedestal");
            if (pedestal != null)
            {
                if (pedestal.transform.parent != null)
                {
                    pedestal.transform.SetParent(null, true);
                }
                pedestal.transform.position = new Vector3(0.18f, -1.95f, -4.5f);
                pedestal.transform.localScale = new Vector3(3.8f, 0.07f, 3.8f);
                var pedMr = pedestal.GetComponent<MeshRenderer>();
                if (pedMr != null) pedMr.enabled = false;
            }

            // 6. Setup full-screen background (bg_pedestal_3d_active) on dedicated ScreenSpaceCamera Canvas
            SetupBackground(DefaultHomeBackground);
        }

        /// <summary>
        /// Configures the full-screen official background (default: bg_pedestal_3d_active)
        /// using a dedicated ScreenSpaceCamera Canvas positioned behind the 3D inspectable card.
        /// </summary>
        public void SetupBackground(string backgroundName = ArenaBackgroundCatalog.Pedestal3DActive)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            }

            // 1. Locate or construct dedicated Background_Canvas
            var bgCanvasObj = GameObject.Find("Background_Canvas");
            if (bgCanvasObj == null)
            {
                bgCanvasObj = new GameObject("Background_Canvas");
            }

            var bgCanvas = bgCanvasObj.GetComponent<Canvas>() ?? bgCanvasObj.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            bgCanvas.worldCamera = cam;
            bgCanvas.planeDistance = 25f; // Positioned behind 3D card (Z = -4.5, camera Z = -10 => dist = 5.5)
            bgCanvas.sortingOrder = -100;

            var bgScaler = bgCanvasObj.GetComponent<CanvasScaler>() ?? bgCanvasObj.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1920, 1080);
            bgScaler.matchWidthOrHeight = 0.5f;

            // 2. Locate or construct Background_Image
            Transform bgChild = bgCanvasObj.transform.Find("Background_Image") ?? bgCanvasObj.transform.Find("Background");
            GameObject bgImgObj;
            if (bgChild == null)
            {
                bgImgObj = new GameObject("Background_Image");
                bgImgObj.transform.SetParent(bgCanvasObj.transform, false);
            }
            else
            {
                bgImgObj = bgChild.gameObject;
            }

            var rect = bgImgObj.GetComponent<RectTransform>() ?? bgImgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            homeBackgroundImage = bgImgObj.GetComponent<Image>() ?? bgImgObj.AddComponent<Image>();

            var sprite = ArenaBackgroundProvider.GetBackgroundSprite(backgroundName)
                         ?? Resources.Load<Sprite>("Backgrounds/" + backgroundName)
                         ?? Resources.Load<Sprite>("Backgrounds/bg_pedestal_3d_active");
            if (sprite != null)
            {
                homeBackgroundImage.sprite = sprite;
            }
            homeBackgroundImage.color = Color.white;
            homeBackgroundImage.preserveAspect = false;
            homeBackgroundImage.raycastTarget = false;
            homeBackgroundImage.enabled = true;

            // 3. Hide primitive cylinder Showcase_Pedestal if present so the high-res pedestal artwork is displayed
            var pedObj = GameObject.Find("Showcase_Pedestal");
            if (pedObj != null)
            {
                var pedMr = pedObj.GetComponent<MeshRenderer>();
                if (pedMr != null) pedMr.enabled = false;
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
            SetupSettingsPanel();
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

            AudioManager.Instance.PlayBgm(BgmType.Home);
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
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
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
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
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
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
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
                deckButtonText.text = $"MAZO ({count}/{DeckValidator.ArenaRequiredDeckSize})";
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
                                toggleDeckCardButtonText.text = "+ AÑADIR AL MAZO (0/2)";
                            toggleDeckCardButton.interactable = count < DeckValidator.ArenaRequiredDeckSize;
                        }
                        else if (copiesInDeck == 1)
                        {
                            if (toggleDeckCardButtonText != null)
                                toggleDeckCardButtonText.text = count < DeckValidator.ArenaRequiredDeckSize ? "+ AÑADIR COPIA (1/2)" : "- QUITAR COPIA (1/2)";
                            toggleDeckCardButton.interactable = true;
                        }
                        else
                        {
                            if (toggleDeckCardButtonText != null)
                                toggleDeckCardButtonText.text = "- QUITAR DEL MAZO (2/2)";
                            toggleDeckCardButton.interactable = true;
                        }
                    }
                }
            }
        }

        private void OnDeckButtonClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
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
                AudioManager.Instance.PlaySfx(SfxType.AddCardToDeck);
            }
            else if (copiesInDeck > 0)
            {
                activeDeck.CardIds.Remove(selectedCard.Id);
                AudioManager.Instance.PlaySfx(SfxType.RemoveCardFromDeck);
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
            AudioManager.Instance.PlaySfx(SfxType.SceneTransition);
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
            AudioManager.Instance.PlaySfx(SfxType.SceneTransition);
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
                "Milenarios",
                "Fugitivos",
                "Depredadores",
                "Míticos"
            };
            clanFilterDropdown.AddOptions(options);
            clanFilterDropdown.onValueChanged.AddListener(OnClanFilterChanged);
        }

        public void SelectCategory(CardCategory category)
        {
            AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            currentCategory = category;
            RefreshGrid();
        }

        private void OnClanFilterChanged(int index)
        {
            AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
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
            AudioManager.Instance.PlaySfx(SfxType.CardGridSelect);
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
            UpdateCardVfxTheme(card, currentFoil);

            // 2. Update Details Panel Header
            if (cardNameText != null) cardNameText.text = card.Name.ToUpper();
            if (cardLevelText != null) cardLevelText.text = $"LVL {currentFoil:00}";
            if (cardClanText != null) cardClanText.text = $"CLAN: {card.Clan}";

            // 3. Dynamic Clan Advantage Tip
            if (clanAdvantageText != null)
            {
                var targetClan = ClanAdvantage.GetAdvantageAgainst(card.Clan);
                clanAdvantageText.text = targetClan != ClanType.None
                    ? $"<color=#00FF88>• Ventaja contra: {targetClan} (+1 Daño)</color>"
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
                    foilProgressLabel.text = $"Carta bloqueada • Fabrícala con {craftCost} Esencia";
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
                    craftCardButtonText.text = $"+ CRAFTEAR CARTA ({craftCost} ESENCIA)";
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
                            craftCardButtonText.text = $"+ CREAR COPIA (+1 COPIA • {craftCost} ESENCIA)";
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
                AudioManager.Instance.PlaySfx(SfxType.UpgradeFoilSuccess);
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

                UpdateCardVfxTheme(selectedCard, nextFoilLevel);
                TriggerCardVfxBurst(32);

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
                AudioManager.Instance.PlaySfx(SfxType.UpgradeFoilFail);
                Debug.LogWarning($"[Home] Cannot upgrade foil: {failureReason}");
            }
        }

        private void OnCraftCardClicked()
        {
            if (selectedCard == null || Profile == null) return;

            bool wasLocked = !Profile.IsCardUnlocked(selectedCard.Id);
            if (Profile.TryCraftCard(selectedCard, out var failureReason))
            {
                AudioManager.Instance.PlaySfx(SfxType.CraftCardSuccess);
                SaveManager.Instance.SaveProfile();

                UpdateCurrencyDisplay();
                RefreshGrid();
                SelectCard(selectedCard);
                TriggerCardVfxBurst(24);

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
                AudioManager.Instance.PlaySfx(SfxType.CraftCardFail);
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
                esenciaDustText.text = dust.ToString("N0");
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
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
            if (shopModalRoot != null) shopModalRoot.SetActive(true);
            if (shopStatusText != null) shopStatusText.text = string.Empty;
            UpdateModalBalance();
        }

        public void CloseShopModal()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalClose);
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
                AudioManager.Instance.PlaySfx(success ? SfxType.PurchaseCoins : SfxType.ErrorDenied);
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
                AudioManager.Instance.PlaySfx(success ? SfxType.WatchAdReward : SfxType.ErrorDenied);
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
            AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
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
            AudioManager.Instance.PlaySfx(SfxType.CardFlip);
            if (card3DViewer != null) card3DViewer.FlipCard180();
        }

        private void OnAutoSpinClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
            if (card3DViewer != null)
            {
                card3DViewer.ToggleAutoSpin();
                UpdateAutoSpinButtonVisual();
            }
        }

        private void OnResetFrontClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.CardSnapFront);
            if (card3DViewer != null)
            {
                card3DViewer.ResetToFront();
                UpdateAutoSpinButtonVisual();
            }
        }

        public void SetReverso(string reversoId)
        {
            AudioManager.Instance.PlaySfx(SfxType.ReversoSwap);
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
                autoSpinButtonText.text = card3DViewer.IsAutoSpinning ? "DETENER" : "AUTO-GIRO";
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

        // ============================================================
        // Settings Panel (Audio Controls: SFX & Music Toggles)
        // ============================================================

        private void SetupSettingsPanel()
        {
            if (settingsButton == null)
            {
                var header = transform.Find("HeaderBar");
                if (header == null) header = transform;
                var btnObj = new GameObject("SettingsGearButton", typeof(RectTransform));
                btnObj.transform.SetParent(header, false);
                var rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-18f, 0f);
                rt.sizeDelta = new Vector2(54f, 54f);

                var bg = btnObj.AddComponent<Image>();
                //bg.color = new Color(0.16f, 0.19f, 0.28f, 0.95f);
                bg.sprite = Resources.Load<Sprite>("UI/SettingsGearIcon");

                settingsButton = btnObj.AddComponent<Button>();

                // var lblObj = new GameObject("Label", typeof(RectTransform));
                // lblObj.transform.SetParent(btnObj.transform, false);
                // var lrt = lblObj.GetComponent<RectTransform>();
                // lrt.anchorMin = Vector2.zero;
                // lrt.anchorMax = Vector2.one;
                // lrt.sizeDelta = Vector2.zero;
                // var tmp = lblObj.AddComponent<TextMeshProUGUI>();
                // tmp.text = "⚙";
                // tmp.fontSize = 24f;
                // tmp.alignment = TextAlignmentOptions.Center;
                // tmp.color = new Color(1f, 0.85f, 0.3f);
            }

            if (settingsPanelRoot == null)
            {
                settingsPanelRoot = new GameObject("SettingsModalRoot", typeof(RectTransform));
                settingsPanelRoot.transform.SetParent(transform, false);
                var rootRt = settingsPanelRoot.GetComponent<RectTransform>();
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.sizeDelta = Vector2.zero;

                var backdrop = settingsPanelRoot.AddComponent<Image>();
                backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);

                var cardObj = new GameObject("SettingsDialog", typeof(RectTransform));
                cardObj.transform.SetParent(settingsPanelRoot.transform, false);
                var cardRt = cardObj.GetComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.sizeDelta = new Vector2(420f, 300f);

                var cardImg = cardObj.AddComponent<Image>();
                cardImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

                var titleObj = new GameObject("Title", typeof(RectTransform));
                titleObj.transform.SetParent(cardObj.transform, false);
                var trt = titleObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 1f);
                trt.anchorMax = new Vector2(1f, 1f);
                trt.pivot = new Vector2(0.5f, 1f);
                trt.anchoredPosition = new Vector2(0f, -18f);
                trt.sizeDelta = new Vector2(-40f, 40f);
                var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "CONFIGURACIÓN DE AUDIO";
                titleTmp.fontSize = 22f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.color = new Color(1f, 0.84f, 0.25f);

                // SFX Toggle Button
                var sfxBtnObj = new GameObject("SfxToggleButton", typeof(RectTransform));
                sfxBtnObj.transform.SetParent(cardObj.transform, false);
                var sfxRt = sfxBtnObj.GetComponent<RectTransform>();
                sfxRt.anchorMin = new Vector2(0.5f, 0.5f);
                sfxRt.anchorMax = new Vector2(0.5f, 0.5f);
                sfxRt.pivot = new Vector2(0.5f, 0.5f);
                sfxRt.anchoredPosition = new Vector2(0f, 25f);
                sfxRt.sizeDelta = new Vector2(320f, 52f);
                var sfxBg = sfxBtnObj.AddComponent<Image>();
                sfxBg.color = new Color(0.16f, 0.45f, 0.28f, 0.95f);
                sfxToggleButton = sfxBtnObj.AddComponent<Button>();

                var sfxLblObj = new GameObject("SfxLabel", typeof(RectTransform));
                sfxLblObj.transform.SetParent(sfxBtnObj.transform, false);
                var sfxLrt = sfxLblObj.GetComponent<RectTransform>();
                sfxLrt.anchorMin = Vector2.zero;
                sfxLrt.anchorMax = Vector2.one;
                sfxLrt.sizeDelta = Vector2.zero;
                sfxToggleLabel = sfxLblObj.AddComponent<TextMeshProUGUI>();
                sfxToggleLabel.fontSize = 18f;
                sfxToggleLabel.fontStyle = FontStyles.Bold;
                sfxToggleLabel.alignment = TextAlignmentOptions.Center;
                sfxToggleLabel.color = Color.white;

                // Music Toggle Button
                var musBtnObj = new GameObject("MusicToggleButton", typeof(RectTransform));
                musBtnObj.transform.SetParent(cardObj.transform, false);
                var musRt = musBtnObj.GetComponent<RectTransform>();
                musRt.anchorMin = new Vector2(0.5f, 0.5f);
                musRt.anchorMax = new Vector2(0.5f, 0.5f);
                musRt.pivot = new Vector2(0.5f, 0.5f);
                musRt.anchoredPosition = new Vector2(0f, -40f);
                musRt.sizeDelta = new Vector2(320f, 52f);
                var musBg = musBtnObj.AddComponent<Image>();
                musBg.color = new Color(0.16f, 0.45f, 0.28f, 0.95f);
                musicToggleButton = musBtnObj.AddComponent<Button>();

                var musLblObj = new GameObject("MusicLabel", typeof(RectTransform));
                musLblObj.transform.SetParent(musBtnObj.transform, false);
                var musLrt = musLblObj.GetComponent<RectTransform>();
                musLrt.anchorMin = Vector2.zero;
                musLrt.anchorMax = Vector2.one;
                musLrt.sizeDelta = Vector2.zero;
                musicToggleLabel = musLblObj.AddComponent<TextMeshProUGUI>();
                musicToggleLabel.fontSize = 18f;
                musicToggleLabel.fontStyle = FontStyles.Bold;
                musicToggleLabel.alignment = TextAlignmentOptions.Center;
                musicToggleLabel.color = Color.white;

                // Close Button
                var closeObj = new GameObject("CloseSettingsButton", typeof(RectTransform));
                closeObj.transform.SetParent(cardObj.transform, false);
                var crt = closeObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0f);
                crt.anchorMax = new Vector2(0.5f, 0f);
                crt.pivot = new Vector2(0.5f, 0f);
                crt.anchoredPosition = new Vector2(0f, 18f);
                crt.sizeDelta = new Vector2(180f, 42f);
                var closeBg = closeObj.AddComponent<Image>();
                closeBg.color = new Color(0.24f, 0.26f, 0.35f, 0.95f);
                closeSettingsButton = closeObj.AddComponent<Button>();

                var closeLblObj = new GameObject("CloseLabel", typeof(RectTransform));
                closeLblObj.transform.SetParent(closeObj.transform, false);
                var clrt = closeLblObj.GetComponent<RectTransform>();
                clrt.anchorMin = Vector2.zero;
                clrt.anchorMax = Vector2.one;
                clrt.sizeDelta = Vector2.zero;
                var closeTmp = closeLblObj.AddComponent<TextMeshProUGUI>();
                closeTmp.text = "CERRAR";
                closeTmp.fontSize = 16f;
                closeTmp.fontStyle = FontStyles.Bold;
                closeTmp.alignment = TextAlignmentOptions.Center;
                closeTmp.color = Color.white;
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OpenSettingsPanel);
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
            }

            if (sfxToggleButton != null)
            {
                sfxToggleButton.onClick.RemoveAllListeners();
                sfxToggleButton.onClick.AddListener(ToggleSfx);
            }

            if (musicToggleButton != null)
            {
                musicToggleButton.onClick.RemoveAllListeners();
                musicToggleButton.onClick.AddListener(ToggleMusic);
            }

            if (settingsPanelRoot != null)
            {
                settingsPanelRoot.SetActive(false);
            }

            RefreshSettingsLabels();
        }

        public void OpenSettingsPanel()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
            RefreshSettingsLabels();
            if (settingsPanelRoot != null)
            {
                settingsPanelRoot.transform.SetAsLastSibling();
                settingsPanelRoot.SetActive(true);
            }
        }

        public void CloseSettingsPanel()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalClose);
            if (settingsPanelRoot != null)
            {
                settingsPanelRoot.SetActive(false);
            }
        }

        private void ToggleSfx()
        {
            bool next = !AudioManager.Instance.IsSfxEnabled;
            AudioManager.Instance.IsSfxEnabled = next;
            if (next)
            {
                AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
            }
            RefreshSettingsLabels();
        }

        private void ToggleMusic()
        {
            AudioManager.Instance.IsMusicEnabled = !AudioManager.Instance.IsMusicEnabled;
            AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
            RefreshSettingsLabels();
        }

        private void RefreshSettingsLabels()
        {
            bool sfxOn = AudioManager.Instance.IsSfxEnabled;
            bool musicOn = AudioManager.Instance.IsMusicEnabled;

            if (sfxToggleLabel != null)
            {
                sfxToggleLabel.text = sfxOn ? "EFECTOS (SFX): ENCENDIDO" : "EFECTOS (SFX): APAGADO";
            }
            if (sfxToggleButton != null)
            {
                var img = sfxToggleButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = sfxOn ? new Color(0.16f, 0.52f, 0.28f, 0.95f) : new Color(0.42f, 0.16f, 0.18f, 0.95f);
                }
            }

            if (musicToggleLabel != null)
            {
                musicToggleLabel.text = musicOn ? "MÚSICA (BGM): ENCENDIDO" : "MÚSICA (BGM): APAGADO";
            }
            if (musicToggleButton != null)
            {
                var img = musicToggleButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = musicOn ? new Color(0.16f, 0.52f, 0.28f, 0.95f) : new Color(0.42f, 0.16f, 0.18f, 0.95f);
                }
            }
        }

        // ============================================================
        // Card Viewport VFX (Rotating Vortex & Aura Particles)
        // ============================================================

        private void SetupCardViewportVfx(Transform viewportTransform)
        {
            if (viewportTransform == null) return;

            var existingAnchor = GameObject.Find("Center_Card3D_VFX_Anchor");
            if (existingAnchor != null)
            {
                cardVfxAnchor = existingAnchor;
            }
            else
            {
                cardVfxAnchor = new GameObject("Center_Card3D_VFX_Anchor");
            }

            // Position slightly behind the 3D card (Z = -4.5 -> -4.05) and in front of Background_Canvas
            cardVfxAnchor.transform.position = viewportTransform.position + new Vector3(0f, 0f, 0.45f);
            cardVfxAnchor.transform.rotation = Quaternion.identity;

            var mat = GetOrCreateParticleMaterial();

            // 1. Rotating Vortex Swirl Ring (Stretched comet streaks orbiting the card perimeter)
            var swirlTrans = cardVfxAnchor.transform.Find("CardVortexSwirl_Particles");
            GameObject swirlObj;
            if (swirlTrans != null)
            {
                swirlObj = swirlTrans.gameObject;
            }
            else
            {
                swirlObj = new GameObject("CardVortexSwirl_Particles");
                swirlObj.transform.SetParent(cardVfxAnchor.transform, false);
            }
            swirlObj.transform.localPosition = Vector3.zero;

            cardVortexSwirlPs = swirlObj.GetComponent<ParticleSystem>();
            if (cardVortexSwirlPs == null)
            {
                cardVortexSwirlPs = swirlObj.AddComponent<ParticleSystem>();
            }
            ConfigureVortexSwirlParticles(cardVortexSwirlPs, mat);

            // 2. Counter-Rotating Floating Aura Orbs (Soft glowing energy motes around the card)
            var orbsTrans = cardVfxAnchor.transform.Find("CardAuraOrbs_Particles");
            GameObject orbsObj;
            if (orbsTrans != null)
            {
                orbsObj = orbsTrans.gameObject;
            }
            else
            {
                orbsObj = new GameObject("CardAuraOrbs_Particles");
                orbsObj.transform.SetParent(cardVfxAnchor.transform, false);
            }
            orbsObj.transform.localPosition = new Vector3(0f, 0f, 0.15f);

            cardAuraOrbsPs = orbsObj.GetComponent<ParticleSystem>();
            if (cardAuraOrbsPs == null)
            {
                cardAuraOrbsPs = orbsObj.AddComponent<ParticleSystem>();
            }
            ConfigureAuraOrbsParticles(cardAuraOrbsPs, mat);

            // 3. Pedestal Rising Energy Sparks (Spiraling upward from the pedestal base)
            var sparksTrans = cardVfxAnchor.transform.Find("PedestalRisingSparks_Particles");
            GameObject sparksObj;
            if (sparksTrans != null)
            {
                sparksObj = sparksTrans.gameObject;
            }
            else
            {
                sparksObj = new GameObject("PedestalRisingSparks_Particles");
                sparksObj.transform.SetParent(cardVfxAnchor.transform, false);
            }
            sparksObj.transform.localPosition = new Vector3(0f, -1.85f, 0.10f);

            pedestalSparksPs = sparksObj.GetComponent<ParticleSystem>();
            if (pedestalSparksPs == null)
            {
                pedestalSparksPs = sparksObj.AddComponent<ParticleSystem>();
            }
            ConfigurePedestalSparksParticles(pedestalSparksPs, mat);
        }

        private static void ConfigureVortexSwirlParticles(ParticleSystem ps, Material mat)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.maxParticles = 140;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 38f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.05f;
            shape.radiusThickness = 0.30f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(1.25f, 1.65f);
            vel.radial = new ParticleSystem.MinMaxCurve(-0.20f, 0.08f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.25f, 1.0f),
                new Keyframe(0.75f, 0.85f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ApplyColorGradient(ps, new Color(0.20f, 0.68f, 1.0f), new Color(1.0f, 0.84f, 0.28f), 0.78f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                psRenderer.velocityScale = 0.08f;
                psRenderer.lengthScale = 2.3f;
                psRenderer.sortingOrder = -10;
                if (mat != null) psRenderer.sharedMaterial = mat;
            }

            if (!ps.isPlaying) ps.Play();
        }

        private static void ConfigureAuraOrbsParticles(ParticleSystem ps, Material mat)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.35f;
            shape.radiusThickness = 0.45f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(-0.85f, -0.45f);
            vel.radial = new ParticleSystem.MinMaxCurve(-0.10f, 0.12f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.0f),
                new Keyframe(0.35f, 1.0f),
                new Keyframe(0.70f, 0.9f),
                new Keyframe(1f, 0.0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ApplyColorGradient(ps, new Color(1.0f, 0.85f, 0.32f), new Color(0.25f, 0.78f, 1.0f), 0.55f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                psRenderer.sortingOrder = -12;
                if (mat != null) psRenderer.sharedMaterial = mat;
            }

            if (!ps.isPlaying) ps.Play();
        }

        private static void ConfigurePedestalSparksParticles(ParticleSystem ps, Material mat)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.20f);
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.75f;
            shape.radiusThickness = 0.65f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            vel.y = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);
            vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(0.65f, 1.15f);
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.radial = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.25f, 1.0f),
                new Keyframe(1f, 0.0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ApplyColorGradient(ps, new Color(1.0f, 0.86f, 0.35f), new Color(0.30f, 0.85f, 1.0f), 0.72f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                psRenderer.sortingOrder = -11;
                if (mat != null) psRenderer.sharedMaterial = mat;
            }

            if (!ps.isPlaying) ps.Play();
        }

        private void UpdateCardViewportVfxRotation()
        {
            if (cardVfxAnchor != null && cardViewportTransform != null)
            {
                cardVfxAnchor.transform.position = cardViewportTransform.position + new Vector3(0f, 0f, 0.45f);
            }

            float dt = Time.deltaTime;
            if (cardVortexSwirlPs != null)
            {
                cardVortexSwirlPs.transform.Rotate(0f, 0f, -28f * dt, Space.Self);
            }

            if (cardAuraOrbsPs != null)
            {
                cardAuraOrbsPs.transform.Rotate(0f, 0f, 16f * dt, Space.Self);
            }
        }

        private void UpdateCardVfxTheme(CardDefinition card, int foilLevel)
        {
            if (card == null) return;

            Color primaryClanColor = card.Clan switch
            {
                ClanType.Milenarios => new Color(0.18f, 0.62f, 1.00f),
                ClanType.Fugitivos => new Color(0.32f, 0.92f, 0.28f),
                ClanType.Depredadores => new Color(1.00f, 0.42f, 0.10f),
                ClanType.Miticos => new Color(0.88f, 0.30f, 0.98f),
                _ => new Color(0.25f, 0.75f, 1.00f)
            };

            Color goldHighlight = new Color(1.00f, 0.85f, 0.30f);
            float foilBoost = 1f + Mathf.Clamp(foilLevel - 1, 0, 3) * 0.25f;

            if (cardVortexSwirlPs != null)
            {
                ApplyColorGradient(cardVortexSwirlPs, primaryClanColor, goldHighlight, 0.80f);
                var emission = cardVortexSwirlPs.emission;
                emission.rateOverTime = 38f * foilBoost;
            }

            if (cardAuraOrbsPs != null)
            {
                ApplyColorGradient(cardAuraOrbsPs, goldHighlight, primaryClanColor, 0.58f);
                var emission = cardAuraOrbsPs.emission;
                emission.rateOverTime = 18f * foilBoost;
            }

            if (pedestalSparksPs != null)
            {
                ApplyColorGradient(pedestalSparksPs, goldHighlight, primaryClanColor, 0.75f);
                var emission = pedestalSparksPs.emission;
                emission.rateOverTime = 16f * foilBoost;
            }
        }

        private void TriggerCardVfxBurst(int count)
        {
            if (cardVortexSwirlPs != null)
            {
                cardVortexSwirlPs.Emit(count);
            }
            if (cardAuraOrbsPs != null)
            {
                cardAuraOrbsPs.Emit(count / 2);
            }
            if (pedestalSparksPs != null)
            {
                pedestalSparksPs.Emit(count / 2);
            }
        }

        private static void ApplyColorGradient(ParticleSystem ps, Color startColor, Color endColor, float peakAlpha)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(Color.Lerp(startColor, endColor, 0.5f), 0.5f),
                    new GradientColorKey(endColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, 0.22f),
                    new GradientAlphaKey(peakAlpha * 0.85f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private static Material GetOrCreateParticleMaterial()
        {
            if (cachedParticleMaterial != null)
            {
                return cachedParticleMaterial;
            }

            if (cachedParticleTexture == null)
            {
                const int size = 64;
                cachedParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "ProceduralParticleGlow",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                float center = (size - 1) * 0.5f;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x - center) / center;
                        float dy = (y - center) / center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float t = Mathf.Clamp01(1f - dist);
                        float alpha = Mathf.Pow(t, 2.4f);
                        float core = Mathf.Pow(t, 5.0f);
                        float brightness = Mathf.Clamp01(0.75f + core * 0.25f);
                        pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                    }
                }
                cachedParticleTexture.SetPixels(pixels);
                cachedParticleTexture.Apply();
            }

            Shader shader = null;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            if (shader == null) return null;

            cachedParticleMaterial = new Material(shader)
            {
                name = "CardViewportVfxMaterial",
                mainTexture = cachedParticleTexture
            };

            if (cachedParticleMaterial.HasProperty("_BaseMap"))
            {
                cachedParticleMaterial.SetTexture("_BaseMap", cachedParticleTexture);
            }
            if (cachedParticleMaterial.HasProperty("_MainTex"))
            {
                cachedParticleMaterial.SetTexture("_MainTex", cachedParticleTexture);
            }

            return cachedParticleMaterial;
        }
    }
}

