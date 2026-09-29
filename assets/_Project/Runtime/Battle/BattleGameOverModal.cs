using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// Game Over Modal dialog shown after Turn 6 is concluded.
    /// Highlights victory/defeat, breakdown of lanes and total power, and rewards credited.
    /// </summary>
    public class BattleGameOverModal : MonoBehaviour
    {
        [Header("Roots & Panels")]
        [SerializeField] private GameObject modalRoot;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI playerStatsText;
        [SerializeField] private TextMeshProUGUI aiStatsText;
        [SerializeField] private TextMeshProUGUI rewardsText;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button homeButton;

        private Action onPlayAgainCallback;
        private Action onHomeCallback;

        private void Awake()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(() =>
                {
                    Hide();
                    onPlayAgainCallback?.Invoke();
                });
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(() =>
                {
                    Hide();
                    onHomeCallback?.Invoke();
                });
            }
        }

        public void Show(
            ArenaBattleMatchState state,
            int goldEarned,
            int xpEarned,
            Action onPlayAgain,
            Action onHome)
        {
            onPlayAgainCallback = onPlayAgain;
            onHomeCallback = onHome;

            if (state == null) return;

            if (titleText != null)
            {
                switch (state.Winner)
                {
                    case BattleWinner.Player:
                        titleText.text = "¡VICTORIA ABSOLUTA!";
                        titleText.color = new Color(0.2f, 0.85f, 0.35f, 1f); // Vibrant green
                        break;
                    case BattleWinner.Ai:
                        titleText.text = "DERROTA";
                        titleText.color = new Color(1f, 0.28f, 0.24f, 1f); // Vibrant red
                        break;
                    case BattleWinner.Tie:
                    default:
                        titleText.text = "EMPATE HEROICO";
                        titleText.color = new Color(0.96f, 0.77f, 0.19f, 1f); // Vibrant gold
                        break;
                }
            }

            if (descriptionText != null)
            {
                switch (state.Winner)
                {
                    case BattleWinner.Player:
                        descriptionText.text = "¡Has dominado el tablero y conquistado las arenas legendarias de Cometa Champions!";
                        break;
                    case BattleWinner.Ai:
                        descriptionText.text = "El Rival Cometa ha asegurado el control de las zonas. ¡Ajusta tu estrategia para la revancha!";
                        break;
                    case BattleWinner.Tie:
                    default:
                        descriptionText.text = "Ambos estrategas igualaron el poder en el campo de batalla con honor.";
                        break;
                }
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = $"<size=38><b>{state.PlayerLanesWon}</b></size>\n<color=#8e8e93>Tus Arenas ({state.PlayerTotalPower} Poder)</color>";
            }

            if (aiStatsText != null)
            {
                aiStatsText.text = $"<size=38><b>{state.AiLanesWon}</b></size>\n<color=#8e8e93>Arenas Rival ({state.AiTotalPower} Poder)</color>";
            }

            if (rewardsText != null)
            {
                rewardsText.text = $"Recompensas:  <color=#f5c518>+{goldEarned} Oro</color>   •   <color=#30d158>+{xpEarned} XP</color>";
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }
    }
}
