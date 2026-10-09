using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Services;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Modal dialog controller for the 7-day Daily Login Reward Packs.
    /// Manages streak progression, calendar cards, countdown timer, and reward reveal celebration.
    /// </summary>
    public class DailyRewardModal : MonoBehaviour
    {
        [Header("Modal Windows")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI streakTitleText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Button claimButton;
        [SerializeField] private TextMeshProUGUI claimButtonText;
        [SerializeField] private TMP_FontAsset font;
        [Header("7-Day Calendar Grid")]
        [SerializeField] private Transform calendarContainer;

        [Header("Reward Reveal Dialog")]
        [SerializeField] private GameObject revealDialogRoot;
        [SerializeField] private TextMeshProUGUI revealTitleText;
        [SerializeField] private TextMeshProUGUI revealSubtitleText;
        [SerializeField] private Transform revealItemsContainer;
        [SerializeField] private Button revealCloseButton;

        [Header("Reward Reveal Sprites (_Project/Art/Home)")]
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite esenciaIcon;
        [SerializeField] private Sprite sobreIcon;
        [SerializeField] private Sprite xpIcon;
        [SerializeField] private Sprite botonGrisSprite;

        public Sprite GetGoldIcon()
        {
            if (goldIcon != null) return goldIcon;
#if UNITY_EDITOR
            goldIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Home/gold.png");
            if (goldIcon != null) return goldIcon;
#endif
            return Resources.Load<Sprite>("Home/gold");
        }

        public Sprite GetEsenciaIcon()
        {
            if (esenciaIcon != null) return esenciaIcon;
#if UNITY_EDITOR
            esenciaIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Home/esencia.png");
            if (esenciaIcon != null) return esenciaIcon;
#endif
            return Resources.Load<Sprite>("Home/esencia");
        }

        public Sprite GetSobreIcon()
        {
            if (sobreIcon != null) return sobreIcon;
#if UNITY_EDITOR
            sobreIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Home/sobre_new.png");
            if (sobreIcon != null) return sobreIcon;
#endif
            return Resources.Load<Sprite>("Home/sobre_new");
        }

        public Sprite GetXpIcon()
        {
            if (xpIcon != null) return xpIcon;
#if UNITY_EDITOR
            xpIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Home/xp.png");
            if (xpIcon != null) return xpIcon;
#endif
            return Resources.Load<Sprite>("Home/xp");
        }

        public Sprite GetBotonGrisSprite()
        {
            if (botonGrisSprite != null) return botonGrisSprite;
#if UNITY_EDITOR
            botonGrisSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Home/boton-rectangular-gris.png");
            if (botonGrisSprite != null) return botonGrisSprite;
#endif
            return Resources.Load<Sprite>("Home/boton-rectangular-gris");
        }

        public event Action<DailyRewardResult> OnDailyRewardClaimed;

        private readonly List<GameObject> spawnedDayCards = new();
        private readonly List<GameObject> spawnedRevealItems = new();
        private TextMeshProUGUI tomorrowBadgeTMP;
        private float timerTick = 0f;

        private PlayerProfile Profile => SaveManager.Instance != null ? SaveManager.Instance.CurrentProfile : null;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }

            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(OnClaimButtonClicked);
            }

            if (revealCloseButton != null)
            {
                revealCloseButton.onClick.RemoveAllListeners();
                revealCloseButton.onClick.AddListener(CloseRevealDialog);

                var btnImg = revealCloseButton.GetComponent<Image>();
                var gris = GetBotonGrisSprite();
                if (btnImg != null && gris != null)
                {
                    btnImg.sprite = gris;
                    btnImg.color = Color.white;
                }
            }

            if (revealDialogRoot != null)
            {
                revealDialogRoot.SetActive(false);
            }
        }

        private void Update()
        {
            // Update countdown timer every second when modal is active
            if (modalRoot != null && modalRoot.activeInHierarchy)
            {
                timerTick += Time.deltaTime;
                if (timerTick >= 1f)
                {
                    timerTick = 0f;
                    UpdateCountdown();
                }
            }
        }

        public void OpenModal()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalOpen);
            if (modalRoot != null) modalRoot.SetActive(true);
            if (revealDialogRoot != null) revealDialogRoot.SetActive(false);

            RefreshCalendarUI();
            UpdateCountdown();
        }

        public void CloseModal()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalClose);
            if (modalRoot != null) modalRoot.SetActive(false);
            if (revealDialogRoot != null) revealDialogRoot.SetActive(false);
            ClearRevealContainer();
        }

        private void CloseRevealDialog()
        {
            AudioManager.Instance.PlaySfx(SfxType.ModalClose);
            if (revealDialogRoot != null) revealDialogRoot.SetActive(false);
            ClearRevealContainer();
            RefreshCalendarUI();
            UpdateCountdown();
        }

        private void ClearCalendarContainer()
        {
            if (calendarContainer != null)
            {
                for (int i = calendarContainer.childCount - 1; i >= 0; i--)
                {
                    var child = calendarContainer.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
            }
            spawnedDayCards.Clear();
            tomorrowBadgeTMP = null;
        }

        private void ClearRevealContainer()
        {
            if (revealItemsContainer != null)
            {
                for (int i = revealItemsContainer.childCount - 1; i >= 0; i--)
                {
                    var child = revealItemsContainer.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
            }
            spawnedRevealItems.Clear();
        }

        public void RefreshCalendarUI()
        {
            ClearCalendarContainer();

            if (Profile == null || calendarContainer == null) return;

            DateTime now = DateTime.UtcNow;
            bool canClaim = Profile.CanClaimDailyReward(now);
            int activeDayIndex = Profile.GetCurrentDailyRewardDayIndex(now);

            if (streakTitleText != null)
            {
                if (canClaim)
                {
                    streakTitleText.text = $"Racha Actual: <color=#00E5FF>Día {activeDayIndex} de 7</color> • ¡Reclama tu pack de hoy!";
                }
                else
                {
                    int nextDay = (activeDayIndex % 7) + 1;
                    streakTitleText.text = $"Racha Actual: <color=#00E5FF>Día {activeDayIndex} de 7 completado</color> • ¡Vuelve mañana para el Día {nextDay}!";
                }
            }

            // Update Claim Button State
            if (claimButton != null)
            {
                claimButton.interactable = canClaim;
                var btnImg = claimButton.GetComponent<Image>();
                if (canClaim)
                {
                    if (btnImg != null) btnImg.color = new Color(0.95f, 0.70f, 0.15f);
                    if (claimButtonText != null)
                    {
                        claimButtonText.text = $"¡RECLAMAR PACK DÍA {activeDayIndex}!";
                        claimButtonText.color = new Color(0.1f, 0.1f, 0.1f);
                    }
                }
                else
                {
                    if (btnImg != null) btnImg.color = new Color(0.25f, 0.25f, 0.32f);
                    if (claimButtonText != null)
                    {
                        claimButtonText.text = "RECLAMADO HOY";
                        claimButtonText.color = new Color(0.65f, 0.65f, 0.70f);
                    }
                }
            }

            // Populate all 7 day cards
            var allPacks = DailyRewardCatalog.GetAllDays();
            foreach (var pack in allPacks)
            {
                var cardObj = CreateDayCard(pack, activeDayIndex, canClaim, calendarContainer);
                spawnedDayCards.Add(cardObj);
            }
        }

        private GameObject CreateDayCard(DailyRewardPackDefinition pack, int activeDayIndex, bool canClaimToday, Transform parent)
        {
            bool isCurrent = pack.DayIndex == activeDayIndex;
            bool isPast = canClaimToday ? (pack.DayIndex < activeDayIndex) : (pack.DayIndex <= activeDayIndex);
            bool isTomorrow = !canClaimToday && (pack.DayIndex == ((activeDayIndex % 7) + 1));

            var cardObj = new GameObject($"DayCard_{pack.DayIndex}");
            cardObj.transform.SetParent(parent, false);
            var cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(180, 230);

            var bgImg = cardObj.AddComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("UI/daily_reward_card_bg");
            
            if (!isCurrent && !isPast && !isTomorrow)
            {
                bgImg.color = new Color(1f, 1f, 1f, 0.5f);
            }

            // if (isCurrent && canClaimToday)
            // {
            //     bgImg.color = new Color(0.22f, 0.28f, 0.45f, 0.98f);
            // }
            // else if (isPast)
            // {
            //     bgImg.color = new Color(0.10f, 0.16f, 0.14f, 0.90f);
            // }
            // else if (isTomorrow)
            // {
            //     bgImg.color = new Color(0.14f, 0.16f, 0.25f, 0.95f);
            // }
            // else
            // {
            //     bgImg.color = new Color(0.09f, 0.10f, 0.15f, 0.85f);
            // }

            // Outer Highlight Border for Active Day
            // if (isCurrent && canClaimToday)
            // {
            //     var borderObj = new GameObject("Border");
            //     borderObj.transform.SetParent(cardObj.transform, false);
            //     var bRect = borderObj.AddComponent<RectTransform>();
            //     bRect.anchorMin = Vector2.zero;
            //     bRect.anchorMax = Vector2.one;
            //     bRect.sizeDelta = Vector2.zero;
            //     var bImg = borderObj.AddComponent<Image>();
            //     bImg.color = new Color(1f, 0.85f, 0.2f, 0.75f);
            //     bImg.raycastTarget = false;
            // }

            // Header Day Label ("DÍA 1", "DÍA 7 MEGA")
            var dayLabelObj = new GameObject("DayLabel");
            dayLabelObj.transform.SetParent(cardObj.transform, false);
            var dlRect = dayLabelObj.AddComponent<RectTransform>();
            dlRect.anchorMin = new Vector2(0.5f, 1);
            dlRect.anchorMax = new Vector2(0.5f, 1);
            dlRect.pivot = new Vector2(0.5f, 1);
            dlRect.anchoredPosition = new Vector2(-3, -67);
            dlRect.sizeDelta = new Vector2(22, 22);

            var dlTMP = dayLabelObj.AddComponent<TextMeshProUGUI>();
            //dlTMP.text = pack.DayIndex == 7 ? "DÍA 7 MEGA" : $"DÍA {pack.DayIndex}";
            dlTMP.text = $"{pack.DayIndex}";
            dlTMP.font = font;
            dlTMP.fontSize = 40;
            dlTMP.fontStyle = FontStyles.Bold;
            dlTMP.alignment = TextAlignmentOptions.Center;
            // dlTMP.color = pack.DayIndex == 7
            //     ? new Color(1f, 0.85f, 0.2f)
            //     : ((isCurrent && canClaimToday) ? new Color(0.2f, 0.85f, 1f) : Color.white);

            // Icon Container / Badge
            var iconBoxObj = new GameObject("Title");
            iconBoxObj.transform.SetParent(cardObj.transform, false);
            var ibRect = iconBoxObj.AddComponent<RectTransform>();
            ibRect.anchorMin = new Vector2(0.5f, 1);
            ibRect.anchorMax = new Vector2(0.5f, 1);
            ibRect.pivot = new Vector2(0.5f, 1);
            ibRect.anchoredPosition = new Vector2(0, 5);
            ibRect.sizeDelta = new Vector2(122, 70);

            var itTMP = iconBoxObj.AddComponent<TextMeshProUGUI>();
            itTMP.text = pack.HasAvatarReward
                ? "<color=#ffffff>AVATAR</color>"
                : (pack.BoosterPacks > 0
                    ? "<color=#ffffff>SOBRE</color>"
                    : (pack.CardCount > 1
                        ? $"<color=#ffffff>x{pack.CardCount} CARTAS</color>"
                        : $"<color=#ffffff>+{pack.GoldCoins} ORO</color>"));
            dlTMP.font = font;
            itTMP.fontSize = 18;
            itTMP.fontStyle = FontStyles.Bold;
            itTMP.alignment = TextAlignmentOptions.Center;

            // var ibImg = iconBoxObj.AddComponent<Image>();
            // ibImg.color = new Color(0.06f, 0.07f, 0.11f, 0.90f);

            // var iconTextObj = new GameObject("IconEmoji");
            // iconTextObj.transform.SetParent(iconBoxObj.transform, false);
            // var itRect = iconTextObj.AddComponent<RectTransform>();
            // itRect.anchorMin = Vector2.zero;
            // itRect.anchorMax = Vector2.one;
            // itRect.sizeDelta = Vector2.zero;

            // var itTMP = iconTextObj.AddComponent<TextMeshProUGUI>();
            // itTMP.text = pack.HasAvatarReward
            //     ? "<color=#00E5FF>AVATAR</color>"
            //     : (pack.BoosterPacks > 0
            //         ? "<color=#FFD700>SOBRE</color>"
            //         : (pack.CardCount > 1
            //             ? $"<color=#00FF88>x{pack.CardCount}\nCARTAS</color>"
            //             : $"<color=#FFD700>+{pack.GoldCoins}\nORO</color>"));
            // itTMP.fontSize = 14;
            // itTMP.fontStyle = FontStyles.Bold;
            // itTMP.alignment = TextAlignmentOptions.Center;

            var descObj = new GameObject("DescBox");
            descObj.transform.SetParent(cardObj.transform, false);
            var dsRect = descObj.AddComponent<RectTransform>();
            dsRect.anchorMin = new Vector2(0.5f, 1);
            dsRect.anchorMax = new Vector2(0.5f, 1);
            dsRect.pivot = new Vector2(0.5f, 1);
            dsRect.anchoredPosition = new Vector2(0, -113);
            dsRect.sizeDelta = new Vector2(110, 80);

            var ibImg = descObj.AddComponent<Image>();
            ibImg.color = new Color(0f, 0f, 0f, 0.5f);

            // Reward Details List
            var rewardsObj = new GameObject("RewardsText");
            rewardsObj.transform.SetParent(descObj.transform, false);
            var rwRect = rewardsObj.AddComponent<RectTransform>();
            rwRect.anchorMin = new Vector2(0.5f, 0);
            rwRect.anchorMax = new Vector2(0.5f, 0);
            rwRect.pivot = new Vector2(0.5f, 0);
            rwRect.anchoredPosition = new Vector2(0, 0);
            rwRect.sizeDelta = new Vector2(105, 80);

            var rwTMP = rewardsObj.AddComponent<TextMeshProUGUI>();
            rwTMP.fontSize = 16;
            rwTMP.alignment = TextAlignmentOptions.Center;

            var rewardLines = new List<string> { $"<color=#FFD700>+{pack.GoldCoins} Oro</color>" };
            if (pack.CardCount > 0) rewardLines.Add($"+{pack.CardCount} Carta{(pack.CardCount > 1 ? "s" : "")}");
            if (pack.EsenciaCometa > 0) rewardLines.Add($"+{pack.EsenciaCometa} Esencia");
            if (pack.BoosterPacks > 0) rewardLines.Add($"+{pack.BoosterPacks} Sobre");
            if (pack.HasAvatarReward) rewardLines.Add("<color=#00E5FF>¡Avatar!</color>");

            rwTMP.text = string.Join("\n", rewardLines);

            // Status Badge at Bottom (Image on badgeObj, Text on child badgeTextObj to prevent Graphic conflict)
            var badgeObj = new GameObject("StatusBadge");
            badgeObj.transform.SetParent(cardObj.transform, false);
            var bdRect = badgeObj.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.5f, 0);
            bdRect.anchorMax = new Vector2(0.5f, 0);
            bdRect.pivot = new Vector2(0.5f, 0);
            bdRect.anchoredPosition = new Vector2(0, 8);
            bdRect.sizeDelta = new Vector2(104, 24);

            var bdImg = badgeObj.AddComponent<Image>();

            var badgeTextObj = new GameObject("StatusText");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            var btRect = badgeTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            var bdTMP = badgeTextObj.AddComponent<TextMeshProUGUI>();
            bdTMP.fontSize = 10;
            bdTMP.fontStyle = FontStyles.Bold;
            bdTMP.alignment = TextAlignmentOptions.Center;

            if (isPast)
            {
                bdImg.color = new Color(0.12f, 0.45f, 0.20f, 0.95f);
                bdTMP.text = "RECLAMADO";
                bdTMP.color = new Color(0.7f, 1f, 0.7f);
            }
            else if (isCurrent && canClaimToday)
            {
                bdImg.color = new Color(0.95f, 0.70f, 0.15f, 1f);
                bdTMP.text = "¡DISPONIBLE!";
                bdTMP.color = new Color(0.1f, 0.1f, 0.1f);
            }
            else if (isTomorrow)
            {
                bdImg.color = new Color(0.20f, 0.25f, 0.38f, 0.95f);
                bdTMP.color = new Color(1f, 0.85f, 0.2f);
                tomorrowBadgeTMP = bdTMP;
            }
            else
            {
                bdImg.color = new Color(0.18f, 0.19f, 0.25f, 0.85f);
                bdTMP.text = "BLOQUEADO";
                bdTMP.color = new Color(0.55f, 0.55f, 0.60f);
            }

            return cardObj;
        }

        private void UpdateCountdown()
        {
            if (Profile == null) return;

            DateTime now = DateTime.UtcNow;
            bool canClaim = Profile.CanClaimDailyReward(now);

            if (canClaim)
            {
                if (countdownText != null)
                {
                    int activeDay = Profile.GetCurrentDailyRewardDayIndex(now);
                    countdownText.text = $"<color=#00FF88>¡Tu pack del Día {activeDay} está listo para reclamar!</color>";
                }
            }
            else
            {
                // Next reward becomes available at 00:00:00 UTC tomorrow
                DateTime nextResetUtc = now.Date.AddDays(1);
                TimeSpan remaining = nextResetUtc - now;

                if (remaining.TotalSeconds <= 0)
                {
                    // Reached midnight UTC! Refresh calendar live!
                    RefreshCalendarUI();
                    return;
                }

                string timeFormatted = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                string longTimeFormatted = $"{(int)remaining.TotalHours:D2}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";

                if (countdownText != null)
                {
                    int nextDay = (Profile.GetCurrentDailyRewardDayIndex(now) % 7) + 1;
                    countdownText.text = $"Próximo pack (<color=#00E5FF>Día {nextDay}</color>) disponible en: <color=#FFD700>{longTimeFormatted}</color>";
                }

                if (tomorrowBadgeTMP != null)
                {
                    tomorrowBadgeTMP.text = timeFormatted;
                }
            }
        }

        private void OnClaimButtonClicked()
        {
            if (Profile == null) return;
            DateTime now = DateTime.UtcNow;

            if (!Profile.CanClaimDailyReward(now))
            {
                UpdateCountdown();
                return;
            }

            try
            {
                AudioManager.Instance.PlaySfx(SfxType.DailyRewardClaim);
                var result = Profile.ClaimDailyReward(now);
                SaveManager.Instance.SaveProfile();

                // Open Reward Reveal Celebration
                ShowRewardReveal(result);

                // Refresh calendar in background so it immediately reflects claimed state
                RefreshCalendarUI();
                UpdateCountdown();

                OnDailyRewardClaimed?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyRewardModal] Error claiming daily reward: {ex.Message}");
            }
        }

        private void ShowRewardReveal(DailyRewardResult result)
        {
            ClearRevealContainer();
            AudioManager.Instance.PlaySfx(SfxType.DailyRewardItemReveal);

            if (revealDialogRoot != null)
            {
                revealDialogRoot.SetActive(true);
            }

            if (revealCloseButton != null)
            {
                var btnImg = revealCloseButton.GetComponent<Image>();
                var gris = GetBotonGrisSprite();
                if (btnImg != null && gris != null)
                {
                    btnImg.sprite = gris;
                    btnImg.color = Color.white;
                }
            }

            if (revealTitleText != null)
            {
                revealTitleText.text = $"¡PACK DÍA {result.DayIndex} RECLAMADO!";
            }

            if (revealSubtitleText != null)
            {
                revealSubtitleText.text = "¡Has recibido las siguientes recompensas para tu colección!";
            }

            if (revealItemsContainer == null) return;

            // 1. Cards Awarded (assembled dynamically with official frame, stats & illustrations)
            foreach (var cardId in result.AwardedCardIds)
            {
                var cardDef = CardCatalog.GetCardById(cardId);
                if (cardDef != null)
                {
                    var itemObj = CreateRevealCardItem(cardDef, revealItemsContainer);
                    spawnedRevealItems.Add(itemObj);
                }
            }

            // 2. Avatar Awarded (if any!)
            if (!string.IsNullOrEmpty(result.AwardedAvatarId))
            {
                var avatarDef = AvatarCatalog.GetAvatarById(result.AwardedAvatarId);
                if (avatarDef != null)
                {
                    var avatarObj = CreateRevealAvatarItem(avatarDef, revealItemsContainer);
                    spawnedRevealItems.Add(avatarObj);
                }
            }
            else if (result.AvatarConvertedToGold)
            {
                var bonusObj = CreateRevealResourceItem(GetGoldIcon(), "+300 Oro (Avatar Duplicado)", "Compensación", revealItemsContainer);
                spawnedRevealItems.Add(bonusObj);
            }

            // 3. Gold Coins
            if (result.GoldCoins > 0)
            {
                var goldObj = CreateRevealResourceItem(GetGoldIcon(), $"+{result.GoldCoins:N0} Monedas", "Oro para mejoras", revealItemsContainer);
                spawnedRevealItems.Add(goldObj);
            }

            // 4. Esencia Cometa
            if (result.EsenciaCometa > 0)
            {
                var dustObj = CreateRevealResourceItem(GetEsenciaIcon(), $"+{result.EsenciaCometa} Esencia", "Polvo de crafteo", revealItemsContainer);
                spawnedRevealItems.Add(dustObj);
            }

            // 5. Booster Packs
            if (result.BoosterPacks > 0)
            {
                var packObj = CreateRevealResourceItem(GetSobreIcon(), $"+{result.BoosterPacks} Sobre", "Pack de refuerzo", revealItemsContainer);
                spawnedRevealItems.Add(packObj);
            }

            // 6. Account XP
            if (result.AccountXp > 0)
            {
                var xpObj = CreateRevealResourceItem(GetXpIcon(), $"+{result.AccountXp} XP", "Progreso de cuenta", revealItemsContainer);
                spawnedRevealItems.Add(xpObj);
            }
        }

        private GameObject CreateRevealCardItem(CardDefinition card, Transform parent)
        {
            var itemObj = new GameObject($"RevealCard_{card.Id}", typeof(RectTransform));
            itemObj.transform.SetParent(parent, false);
            var rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 168);

            DynamicCardAssembler.AssembleCard(card, rect, 120, 168, false);

            return itemObj;
        }

        private GameObject CreateRevealAvatarItem(PlayerAvatarDefinition avatar, Transform parent)
        {
            var itemObj = new GameObject($"RevealAvatar_{avatar.Id}");
            itemObj.transform.SetParent(parent, false);
            var rect = itemObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110, 160);

            var bgImg = itemObj.AddComponent<Image>();
            bgImg.color = new Color(0.20f, 0.18f, 0.32f, 0.98f);

            // Golden Border
            var borderObj = new GameObject("GoldBorder");
            borderObj.transform.SetParent(itemObj.transform, false);
            var bRect = borderObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = Vector2.zero;
            var bImg = borderObj.AddComponent<Image>();
            bImg.color = new Color(1f, 0.85f, 0.2f, 0.75f);
            bImg.raycastTarget = false;

            // Portrait
            var portraitObj = new GameObject("AvatarPortrait");
            portraitObj.transform.SetParent(itemObj.transform, false);
            var pRect = portraitObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 1);
            pRect.anchorMax = new Vector2(0.5f, 1);
            pRect.pivot = new Vector2(0.5f, 1);
            pRect.anchoredPosition = new Vector2(0, -8);
            pRect.sizeDelta = new Vector2(90, 90);

            var pImg = portraitObj.AddComponent<Image>();
            var sprite = AvatarSpriteProvider.GetAvatarSprite(avatar);
            if (sprite != null)
            {
                pImg.sprite = sprite;
                pImg.preserveAspect = true;
            }

            // Label
            var labelObj = new GameObject("AvatarLabel");
            labelObj.transform.SetParent(itemObj.transform, false);
            var lRect = labelObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0);
            lRect.anchorMax = new Vector2(0.5f, 0);
            lRect.pivot = new Vector2(0.5f, 0);
            lRect.anchoredPosition = new Vector2(0, 6);
            lRect.sizeDelta = new Vector2(105, 48);

            var lTMP = labelObj.AddComponent<TextMeshProUGUI>();
            lTMP.text = $"<color=#00E5FF>¡NUEVO AVATAR!</color>\n<color=#FFD700>{avatar.Name}</color>";
            lTMP.fontSize = 11;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.alignment = TextAlignmentOptions.Center;

            return itemObj;
        }

        private GameObject CreateRevealResourceItem(Sprite iconSprite, string mainText, string subText, Transform parent)
        {
            var itemObj = new GameObject($"RevealRes_{mainText}", typeof(RectTransform));
            itemObj.transform.SetParent(parent, false);
            var rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110, 160);

            var bgImg = itemObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(itemObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.5f, 1);
            iRect.anchorMax = new Vector2(0.5f, 1);
            iRect.pivot = new Vector2(0.5f, 1);
            iRect.anchoredPosition = new Vector2(0, -14);
            iRect.sizeDelta = new Vector2(72, 72);

            if (iconSprite != null)
            {
                var iImg = iconObj.AddComponent<Image>();
                iImg.sprite = iconSprite;
                iImg.preserveAspect = true;
                iImg.color = Color.white;
            }

            var textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(itemObj.transform, false);
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0);
            tRect.anchorMax = new Vector2(0.5f, 0);
            tRect.pivot = new Vector2(0.5f, 0);
            tRect.anchoredPosition = new Vector2(0, 12);
            tRect.sizeDelta = new Vector2(105, 52);

            var tTMP = textObj.AddComponent<TextMeshProUGUI>();
            tTMP.text = $"<color=#FFD700><b>{mainText}</b></color>\n<size=10><color=#AAAAAA>{subText}</color></size>";
            tTMP.fontSize = 12;
            tTMP.alignment = TextAlignmentOptions.Center;

            return itemObj;
        }
    }
}
