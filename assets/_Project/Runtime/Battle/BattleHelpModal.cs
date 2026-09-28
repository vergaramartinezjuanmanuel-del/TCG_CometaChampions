using UnityEngine;
using UnityEngine.UI;

namespace CometaChampions.Runtime.Battle
{
    /// <summary>
    /// In-game Rules & Combat Manual modal replicating the web version instructions.
    /// Explains 3 Arenas objective, progressive energy 1-6, Círculo Cometa, deployment, and undo.
    /// </summary>
    public class BattleHelpModal : MonoBehaviour
    {
        [Header("Roots & Panels")]
        [SerializeField] private GameObject modalRoot;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button gotItButton;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (gotItButton != null) gotItButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
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
