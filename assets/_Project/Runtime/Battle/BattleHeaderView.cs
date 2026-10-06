using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Top HUD Bar displaying Turn, Energy, Score indicator, and primary battle action buttons.
    /// </summary>
    public class BattleHeaderView : MonoBehaviour
    {
        [Header("Information Labels")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI statusNotificationText;

        [Header("Action Buttons")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        public event Action OnEndTurnClicked;
        public event Action OnHelpClicked;
        public event Action OnRestartClicked;
        public event Action OnHomeClicked;

        private void Awake()
        {
            if (endTurnButton != null) endTurnButton.onClick.AddListener(() =>
            {
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ButtonClick);
                OnEndTurnClicked?.Invoke();
            });
            if (helpButton != null) helpButton.onClick.AddListener(() =>
            {
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ButtonClick);
                OnHelpClicked?.Invoke();
            });
            if (restartButton != null) restartButton.onClick.AddListener(() =>
            {
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ButtonClick);
                OnRestartClicked?.Invoke();
            });
            if (homeButton != null) homeButton.onClick.AddListener(() =>
            {
                CometaChampions.Runtime.Services.AudioManager.Instance.PlaySfx(CometaChampions.Core.Enums.SfxType.ButtonClick);
                OnHomeClicked?.Invoke();
            });
        }

        public void UpdateHeader(
            int currentTurn,
            int maxTurn,
            int playerEnergy,
            int playerMaxEnergy,
            int playerLanesWon,
            int aiLanesWon)
        {
            if (turnText != null)
            {
                turnText.text = $"Turno <color=#f5c518><b>{currentTurn}</b></color> de <b>{maxTurn}</b>";
            }

            if (energyText != null)
            {
                energyText.text = $"Energía <color=#30d158><b>{playerEnergy}</b></color> / {playerMaxEnergy}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"<color=#30d158><b>{playerLanesWon}</b></color>  <color=#8e8e93>vs</color>  <color=#ff453a><b>{aiLanesWon}</b></color>";
            }
        }

        public void SetStatusNotification(string message, Color? color = null)
        {
            if (statusNotificationText != null)
            {
                statusNotificationText.text = message ?? "";
                if (color.HasValue)
                {
                    statusNotificationText.color = color.Value;
                }
                statusNotificationText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        public void SetEndTurnInteractable(bool interactable)
        {
            if (endTurnButton != null)
            {
                endTurnButton.interactable = interactable;
            }
        }
    }
}
