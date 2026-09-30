using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Cards;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Showcase UI manager: card catalog browser across 80 cards,
    /// 360 viewer controller integration, reverso selector, foil sheen selector,
    /// category filters, and detailed card stat/lore inspector.
    /// </summary>
    public class CardShowcaseUI : MonoBehaviour
    {
        [Header("Showcase 3D Core")]
        [SerializeField] private Card3D card3D;
        [SerializeField] private Card3DViewer cardViewer;

        [Header("Header Elements")]
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text headerTitleText;
        [SerializeField] private TMP_Text categoryIndicatorText;

        [Header("Navigation Elements")]
        [SerializeField] private Button prevCardButton;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button randomCardButton;
        [SerializeField] private Button flipCardButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private Button resetFrontButton;
        [SerializeField] private TMP_Text autoSpinButtonText;
        [SerializeField] private TMP_Text cardCounterText;

        [Header("Category Filter Buttons")]
        [SerializeField] private Button catAllButton;
        [SerializeField] private Button catChampionsButton;
        [SerializeField] private Button catTromposButton;
        [SerializeField] private Button catKombatButton;
        [SerializeField] private Button catArenasButton;

        [Header("Reverso Selector Buttons")]
        [SerializeField] private Button reversoOroButton;
        [SerializeField] private Button reversoColorButton;
        [SerializeField] private Button reversoBiButton;

        [Header("Foil Selector Buttons")]
        [SerializeField] private Button foilBaseButton;
        [SerializeField] private Button foilSilverButton;
        [SerializeField] private Button foilGoldButton;
        [SerializeField] private Button foilCosmicButton;

        [Header("Card Detail Info Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardCategoryBadgeText;
        [SerializeField] private TMP_Text cardClanBadgeText;
        [SerializeField] private TMP_Text cardStatsText;
        [SerializeField] private TMP_Text cardRarityText;
        [SerializeField] private TMP_Text cardEffectText;
        [SerializeField] private TMP_Text cardFlavorText;

        private List<ShowcaseCardData> activeCardList = new();
        private int currentCardIndex = 0;
        private string currentCategory = "all";
        private string currentReverso = "oro";
        private int currentFoilLevel = 1;

        private void Awake()
        {
            HookButtonEvents();
        }

        private void Start()
        {
            if (card3D == null) card3D = FindAnyObjectByType<Card3D>();
            if (cardViewer == null) cardViewer = FindAnyObjectByType<Card3DViewer>();

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

            SelectCategory("all");
            SelectReverso("oro");
            SelectFoil(1);
            AudioManager.Instance.PlayBgm(BgmType.Showcase);
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        private void HookButtonEvents()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            if (prevCardButton != null) prevCardButton.onClick.AddListener(ShowPreviousCard);
            if (nextCardButton != null) nextCardButton.onClick.AddListener(ShowNextCard);
            if (randomCardButton != null) randomCardButton.onClick.AddListener(ShowRandomCard);

            if (flipCardButton != null) flipCardButton.onClick.AddListener(OnFlipClicked);
            if (autoSpinButton != null) autoSpinButton.onClick.AddListener(OnAutoSpinClicked);
            if (resetFrontButton != null) resetFrontButton.onClick.AddListener(OnResetFrontClicked);

            if (catAllButton != null) catAllButton.onClick.AddListener(() => SelectCategory("all"));
            if (catChampionsButton != null) catChampionsButton.onClick.AddListener(() => SelectCategory("champions"));
            if (catTromposButton != null) catTromposButton.onClick.AddListener(() => SelectCategory("trompos"));
            if (catKombatButton != null) catKombatButton.onClick.AddListener(() => SelectCategory("kombat"));
            if (catArenasButton != null) catArenasButton.onClick.AddListener(() => SelectCategory("arenas"));

            if (reversoOroButton != null) reversoOroButton.onClick.AddListener(() => SelectReverso("oro"));
            if (reversoColorButton != null) reversoColorButton.onClick.AddListener(() => SelectReverso("color"));
            if (reversoBiButton != null) reversoBiButton.onClick.AddListener(() => SelectReverso("bi"));

            if (foilBaseButton != null) foilBaseButton.onClick.AddListener(() => SelectFoil(1));
            if (foilSilverButton != null) foilSilverButton.onClick.AddListener(() => SelectFoil(2));
            if (foilGoldButton != null) foilGoldButton.onClick.AddListener(() => SelectFoil(3));
            if (foilCosmicButton != null) foilCosmicButton.onClick.AddListener(() => SelectFoil(4));
        }

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) ShowPreviousCard();
            if (Input.GetKeyDown(KeyCode.RightArrow)) ShowNextCard();
            if (Input.GetKeyDown(KeyCode.Space)) OnFlipClicked();
            if (Input.GetKeyDown(KeyCode.R)) OnResetFrontClicked();
            if (Input.GetKeyDown(KeyCode.A)) OnAutoSpinClicked();
            if (Input.GetKeyDown(KeyCode.Escape)) OnBackClicked();
        }

        public void SelectCategory(string category)
        {
            AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            currentCategory = category;
            activeCardList = CardShowcaseCatalog.GetCardsByCategory(category).ToList();
            currentCardIndex = 0;
            DisplayCurrentCard();
            UpdateCategoryButtonsVisual();
        }

        public void SelectReverso(string reversoId)
        {
            AudioManager.Instance.PlaySfx(SfxType.ReversoSwap);
            currentReverso = reversoId;
            if (card3D != null)
            {
                card3D.SetReverso(reversoId);
            }
            UpdateReversoButtonsVisual();
        }

        public void SelectFoil(int level)
        {
            AudioManager.Instance.PlaySfx(SfxType.FoilShaderChange);
            currentFoilLevel = level;
            if (card3D != null)
            {
                card3D.SetFoilLevel(level);
            }
            UpdateFoilButtonsVisual();
        }

        public void ShowNextCard()
        {
            if (activeCardList == null || activeCardList.Count == 0) return;
            AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            currentCardIndex = (currentCardIndex + 1) % activeCardList.Count;
            DisplayCurrentCard();
        }

        public void ShowPreviousCard()
        {
            if (activeCardList == null || activeCardList.Count == 0) return;
            AudioManager.Instance.PlaySfx(SfxType.TabSwitch);
            currentCardIndex = (currentCardIndex - 1 + activeCardList.Count) % activeCardList.Count;
            DisplayCurrentCard();
        }

        public void ShowRandomCard()
        {
            if (activeCardList == null || activeCardList.Count <= 1) return;
            AudioManager.Instance.PlaySfx(SfxType.CardGridSelect);
            int nextIdx;
            do
            {
                nextIdx = UnityEngine.Random.Range(0, activeCardList.Count);
            } while (nextIdx == currentCardIndex);

            currentCardIndex = nextIdx;
            DisplayCurrentCard();
        }

        private void DisplayCurrentCard()
        {
            if (activeCardList == null || activeCardList.Count == 0) return;

            var card = activeCardList[currentCardIndex];
            if (card3D != null)
            {
                card3D.SetCard(card, currentFoilLevel);
                card3D.SetReverso(currentReverso);
            }

            // Update Counter
            if (cardCounterText != null)
            {
                cardCounterText.text = $"{currentCardIndex + 1} / {activeCardList.Count}";
            }

            // Category indicator
            if (categoryIndicatorText != null)
            {
                categoryIndicatorText.text = $"{card.CategoryName.ToUpper()}  •  {card.Id}";
            }

            // Detailed Side Panel
            if (cardNameText != null) cardNameText.text = card.Name;
            if (cardCategoryBadgeText != null) cardCategoryBadgeText.text = card.CategoryName;

            // Clan info
            var clan = CardShowcaseCatalog.GetClanById(card.Clan);
            if (cardClanBadgeText != null)
            {
                if (clan != null)
                {
                    cardClanBadgeText.text = $"{clan.Symbol} {clan.Name}";
                    if (ColorUtility.TryParseHtmlString(clan.ColorHex, out var clanColor))
                    {
                        cardClanBadgeText.color = clanColor;
                    }
                }
                else
                {
                    cardClanBadgeText.text = card.Clan.ToUpper();
                    cardClanBadgeText.color = Color.white;
                }
            }

            // Stats Chip
            if (cardStatsText != null)
            {
                if (card.Category.Equals("champions", StringComparison.OrdinalIgnoreCase))
                {
                    cardStatsText.text = $"Costo: {card.Cost}   •   ATK: {card.Attack}   •   DEF: {card.Defense}";
                }
                else if (card.Category.Equals("kombat", StringComparison.OrdinalIgnoreCase))
                {
                    cardStatsText.text = $"Tipo: {card.CardType}   •   ATK: +{card.Attack}";
                }
                else if (card.Category.Equals("trompos", StringComparison.OrdinalIgnoreCase))
                {
                    cardStatsText.text = $"Línea: {card.CardType}   •   DEF: {card.Defense}";
                }
                else
                {
                    cardStatsText.text = $"{card.CardType}";
                }
            }

            if (cardRarityText != null) cardRarityText.text = $"Rareza: {card.Rarity}";

            if (cardEffectText != null)
            {
                string desc = !string.IsNullOrEmpty(card.Effect) ? card.Effect : card.Description;
                cardEffectText.text = string.IsNullOrEmpty(desc) ? "Sin efectos especiales." : desc;
            }

            if (cardFlavorText != null)
            {
                string quote = !string.IsNullOrEmpty(card.Quote) ? $"\"{card.Quote}\"\n\n" : "";
                string flavor = !string.IsNullOrEmpty(card.RealProductFlavor) ? card.RealProductFlavor : card.Lore;
                cardFlavorText.text = quote + flavor;
            }
        }

        private void OnFlipClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.CardFlip);
            if (cardViewer != null)
            {
                cardViewer.FlipCard180();
            }
        }

        private void OnAutoSpinClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.ButtonClick);
            if (cardViewer != null)
            {
                cardViewer.ToggleAutoSpin();
                UpdateAutoSpinButtonVisual();
            }
        }

        private void OnResetFrontClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.CardSnapFront);
            if (cardViewer != null)
            {
                cardViewer.ResetToFront();
                UpdateAutoSpinButtonVisual();
            }
        }

        private void UpdateAutoSpinButtonVisual()
        {
            if (autoSpinButtonText != null && cardViewer != null)
            {
                autoSpinButtonText.text = cardViewer.IsAutoSpinning ? "Detener Giro" : "Auto-Giro";
            }
        }

        private void UpdateCategoryButtonsVisual()
        {
            SetButtonHighlight(catAllButton, currentCategory == "all");
            SetButtonHighlight(catChampionsButton, currentCategory == "champions");
            SetButtonHighlight(catTromposButton, currentCategory == "trompos");
            SetButtonHighlight(catKombatButton, currentCategory == "kombat");
            SetButtonHighlight(catArenasButton, currentCategory == "arenas");
        }

        private void UpdateReversoButtonsVisual()
        {
            SetButtonHighlight(reversoOroButton, currentReverso == "oro");
            SetButtonHighlight(reversoColorButton, currentReverso == "color");
            SetButtonHighlight(reversoBiButton, currentReverso == "bi");
        }

        private void UpdateFoilButtonsVisual()
        {
            SetButtonHighlight(foilBaseButton, currentFoilLevel == 1);
            SetButtonHighlight(foilSilverButton, currentFoilLevel == 2);
            SetButtonHighlight(foilGoldButton, currentFoilLevel == 3);
            SetButtonHighlight(foilCosmicButton, currentFoilLevel == 4);
        }

        private void SetButtonHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active ? new Color(0.95f, 0.75f, 0.2f, 1f) : new Color(0.18f, 0.2f, 0.25f, 0.85f);
            }
            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.color = active ? Color.black : Color.white;
            }
        }

        private void OnBackClicked()
        {
            AudioManager.Instance.PlaySfx(SfxType.SceneTransition);
            // Return to Home scene
            SceneManager.LoadScene("Home");
        }
    }
}
