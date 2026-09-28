using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.Services;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Modal dialog controller for selecting, purchasing, and equipping player avatars.
    /// Lists all 20 avatars across progression tiers (Levels 1 to 20).
    /// </summary>
    public class AvatarSelectionModal : MonoBehaviour
    {
        [Header("Modal Root & Controls")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI modalTitleText;
        [SerializeField] private TextMeshProUGUI modalGoldText;
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        [Header("Grid Content")]
        [SerializeField] private Transform gridContainer;

        public event Action<string> OnAvatarChanged;

        private readonly List<GameObject> spawnedCells = new();

        private PlayerProfile Profile => SaveManager.Instance.CurrentProfile;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }
        }

        public void OpenModal()
        {
            if (modalRoot != null) modalRoot.SetActive(true);
            if (statusFeedbackText != null) statusFeedbackText.text = string.Empty;
            UpdateHeaderInfo();
            RefreshAvatarGrid();
        }

        public void CloseModal()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        public void UpdateHeaderInfo()
        {
            if (modalGoldText != null && Profile != null)
            {
                modalGoldText.text = $"Tu Oro: <color=#FFD700>{Profile.GoldCoins:N0} 🪙</color>  |  Nivel: <color=#00E5FF>{Profile.AccountLevel}</color>";
            }
        }

        public void RefreshAvatarGrid()
        {
            if (gridContainer == null || Profile == null) return;

            // Clear previous cells
            foreach (var cell in spawnedCells)
            {
                if (cell != null) Destroy(cell);
            }
            spawnedCells.Clear();

            var avatars = AvatarCatalog.GetAll();
            foreach (var avatar in avatars)
            {
                var cellObj = CreateAvatarCell(avatar, gridContainer);
                spawnedCells.Add(cellObj);
            }
        }

        private GameObject CreateAvatarCell(PlayerAvatarDefinition avatar, Transform parent)
        {
            bool isEquipped = string.Equals(Profile.SelectedAvatarId, avatar.Id, StringComparison.OrdinalIgnoreCase);
            bool isUnlocked = Profile.IsAvatarUnlocked(avatar.Id);
            bool meetsLevel = Profile.AccountLevel >= avatar.RequiredLevel;
            bool canAfford = Profile.GoldCoins >= avatar.GoldCost;

            // Cell Root
            var cellObj = new GameObject($"AvatarCell_{avatar.Id}");
            cellObj.transform.SetParent(parent, false);
            var cellRect = cellObj.AddComponent<RectTransform>();
            cellRect.sizeDelta = new Vector2(170, 230);

            var cellBg = cellObj.AddComponent<Image>();
            cellBg.color = isEquipped
                ? new Color(0.18f, 0.22f, 0.35f, 0.98f)
                : (isUnlocked
                    ? new Color(0.12f, 0.14f, 0.20f, 0.95f)
                    : new Color(0.08f, 0.08f, 0.12f, 0.90f));

            // Gold/Equipped Border Highlight
            if (isEquipped)
            {
                var borderObj = new GameObject("Border");
                borderObj.transform.SetParent(cellObj.transform, false);
                var borderRect = borderObj.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.sizeDelta = Vector2.zero;
                var borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(1f, 0.85f, 0.2f, 0.4f);
                borderImg.raycastTarget = false;
            }

            // Avatar Portrait Image (Masked rounded container)
            var portraitFrameObj = new GameObject("PortraitFrame");
            portraitFrameObj.transform.SetParent(cellObj.transform, false);
            var pfRect = portraitFrameObj.AddComponent<RectTransform>();
            pfRect.anchorMin = new Vector2(0.5f, 1);
            pfRect.anchorMax = new Vector2(0.5f, 1);
            pfRect.pivot = new Vector2(0.5f, 1);
            pfRect.anchoredPosition = new Vector2(0, -10);
            pfRect.sizeDelta = new Vector2(120, 120);

            var pfBg = portraitFrameObj.AddComponent<Image>();
            pfBg.color = new Color(0.05f, 0.06f, 0.09f, 1f);

            var portraitObj = new GameObject("PortraitImage");
            portraitObj.transform.SetParent(portraitFrameObj.transform, false);
            var pRect = portraitObj.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(4, 4);
            pRect.offsetMax = new Vector2(-4, -4);

            var portraitImg = portraitObj.AddComponent<Image>();
            var sprite = AvatarSpriteProvider.GetAvatarSprite(avatar);
            if (sprite != null)
            {
                portraitImg.sprite = sprite;
                portraitImg.preserveAspect = true;
            }

            // If locked, darken portrait and add padlock icon
            if (!isUnlocked)
            {
                portraitImg.color = meetsLevel
                    ? new Color(0.75f, 0.75f, 0.85f, 0.85f)
                    : new Color(0.35f, 0.35f, 0.40f, 0.65f);

                if (!meetsLevel)
                {
                    var lockObj = new GameObject("Padlock");
                    lockObj.transform.SetParent(portraitFrameObj.transform, false);
                    var lockRect = lockObj.AddComponent<RectTransform>();
                    lockRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lockRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lockRect.pivot = new Vector2(0.5f, 0.5f);
                    lockRect.sizeDelta = new Vector2(36, 36);
                    var lockImg = lockObj.AddComponent<Image>();
                    var lockSprite = Card3D.GetLockIconSprite();
                    if (lockSprite != null) lockImg.sprite = lockSprite;
                    lockImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                    lockImg.raycastTarget = false;
                }
            }

            // Name Label
            var nameObj = new GameObject("AvatarName");
            nameObj.transform.SetParent(cellObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -134);
            nameRect.sizeDelta = new Vector2(160, 22);

            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = avatar.Name;
            nameTMP.fontSize = 15;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = isEquipped ? new Color(1f, 0.9f, 0.4f) : Color.white;

            // Level / Requirement Label
            var reqObj = new GameObject("ReqLabel");
            reqObj.transform.SetParent(cellObj.transform, false);
            var reqRect = reqObj.AddComponent<RectTransform>();
            reqRect.anchorMin = new Vector2(0, 1);
            reqRect.anchorMax = new Vector2(1, 1);
            reqRect.pivot = new Vector2(0.5f, 1);
            reqRect.anchoredPosition = new Vector2(0, -156);
            reqRect.sizeDelta = new Vector2(160, 18);

            var reqTMP = reqObj.AddComponent<TextMeshProUGUI>();
            reqTMP.fontSize = 12;
            reqTMP.alignment = TextAlignmentOptions.Center;

            if (meetsLevel)
            {
                reqTMP.text = $"Nvl {avatar.RequiredLevel} • {avatar.Clan}";
                reqTMP.color = new Color(0.6f, 0.75f, 0.9f);
            }
            else
            {
                reqTMP.text = $"<color=#FF5555>🔒 Requiere Nvl {avatar.RequiredLevel}</color>";
            }

            // Action / Status Button
            var btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(cellObj.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 10);
            btnRect.sizeDelta = new Vector2(150, 36);

            var btnImg = btnObj.AddComponent<Image>();
            var btn = btnObj.AddComponent<Button>();

            var btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btRect = btnTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            var btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnTMP.fontSize = 13;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;

            // Configure Button State
            if (isEquipped)
            {
                btnImg.color = new Color(0.15f, 0.45f, 0.25f);
                btnTMP.text = "★ EQUIPADO";
                btnTMP.color = new Color(0.6f, 1f, 0.6f);
                btn.interactable = false;
            }
            else if (isUnlocked)
            {
                btnImg.color = new Color(0.12f, 0.50f, 0.80f);
                btnTMP.text = "EQUIPAR";
                btnTMP.color = Color.white;
                btn.interactable = true;
                btn.onClick.AddListener(() => OnEquipClicked(avatar.Id));
            }
            else if (meetsLevel)
            {
                // Level met, requires gold purchase
                if (canAfford)
                {
                    btnImg.color = new Color(0.85f, 0.65f, 0.10f);
                    btnTMP.text = $"COMPRAR 🪙 {avatar.GoldCost:N0}";
                    btnTMP.color = new Color(0.1f, 0.1f, 0.1f);
                    btn.interactable = true;
                    btn.onClick.AddListener(() => OnPurchaseClicked(avatar.Id));
                }
                else
                {
                    btnImg.color = new Color(0.35f, 0.25f, 0.10f);
                    btnTMP.text = $"🪙 {avatar.GoldCost:N0} (Falta Oro)";
                    btnTMP.color = new Color(0.8f, 0.5f, 0.5f);
                    btn.interactable = false;
                }
            }
            else
            {
                // Locked by level
                btnImg.color = new Color(0.20f, 0.20f, 0.25f);
                btnTMP.text = $"BLOQUEADO";
                btnTMP.color = new Color(0.55f, 0.55f, 0.60f);
                btn.interactable = false;
            }

            return cellObj;
        }

        private void OnEquipClicked(string avatarId)
        {
            if (Profile == null) return;

            if (Profile.TryEquipAvatar(avatarId, out var reason))
            {
                SaveManager.Instance.SaveProfile();
                if (statusFeedbackText != null)
                {
                    var def = AvatarCatalog.GetAvatarById(avatarId);
                    statusFeedbackText.text = $"<color=#00FF88>¡Avatar '{def?.Name}' equipado con éxito!</color>";
                }
                UpdateHeaderInfo();
                RefreshAvatarGrid();
                OnAvatarChanged?.Invoke(avatarId);
            }
            else
            {
                if (statusFeedbackText != null)
                {
                    statusFeedbackText.text = $"<color=#FF4444>{reason}</color>";
                }
            }
        }

        private void OnPurchaseClicked(string avatarId)
        {
            if (Profile == null) return;

            if (Profile.TryPurchaseAvatar(avatarId, out var reason))
            {
                SaveManager.Instance.SaveProfile();
                var def = AvatarCatalog.GetAvatarById(avatarId);
                if (statusFeedbackText != null)
                {
                    statusFeedbackText.text = $"<color=#00FF88>¡Avatar '{def?.Name}' desbloqueado y equipado!</color>";
                }
                UpdateHeaderInfo();
                RefreshAvatarGrid();
                OnAvatarChanged?.Invoke(avatarId);
            }
            else
            {
                if (statusFeedbackText != null)
                {
                    statusFeedbackText.text = $"<color=#FF4444>{reason}</color>";
                }
            }
        }
    }
}
