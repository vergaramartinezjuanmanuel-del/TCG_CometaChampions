using System;
using UnityEngine;
using TMPro;
using CometaChampions.Core.Models;
using CometaChampions.Core.Enums;

namespace CometaChampions.Runtime.Cards
{
    /// <summary>
    /// Controls the 3D card representation, including dynamic text/stat binding,
    /// horizontal swipe-to-rotate interaction, and foil/metallic material parameters.
    /// </summary>
    public class CardView3D : MonoBehaviour
    {
        [Header("Card Data")]
        [SerializeField] private string currentCardId = string.Empty;

        [Header("UI & Text Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro costText;
        [SerializeField] private TextMeshPro attackText;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private TextMeshPro abilityText;
        [SerializeField] private GameObject attackBadge;
        [SerializeField] private GameObject healthBadge;
        [SerializeField] private SpriteRenderer artworkRenderer;
        [SerializeField] private SpriteRenderer clanIconRenderer;
        [SerializeField] private SpriteRenderer cardBackRenderer;

        [Header("3D Rotation Settings")]
        [SerializeField] private Transform cardTransform;
        [SerializeField] private float rotationSpeed = 0.4f;
        [SerializeField] private float autoReturnSpeed = 5f;
        [SerializeField] private bool allowFreeRotation = true;

        [Header("Foil & Shader Settings")]
        [SerializeField] private MeshRenderer frontMeshRenderer;
        private MaterialPropertyBlock propBlock = new();

        private float targetYRotation;
        private float currentYRotation;
        private bool isDragging;
        private Vector2 lastPointerPos;

        public CardDefinition CurrentDefinition { get; private set; }

        private void Awake()
        {
            if (cardTransform == null) cardTransform = transform;
            propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            HandleRotation();
        }

        public void Bind(CardDefinition definition, int foilLevel = 1)
        {
            CurrentDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
            currentCardId = definition.Id;

            if (nameText != null) nameText.text = definition.Name;
            if (costText != null) costText.text = definition.EnergyCost.ToString();

            if (attackBadge != null) attackBadge.SetActive(true);
            if (healthBadge != null) healthBadge.SetActive(true);
            if (attackText != null) attackText.text = definition.BaseAttack.ToString();
            if (healthText != null) healthText.text = definition.BaseHealth.ToString();

            if (abilityText != null)
            {
                abilityText.text = !string.IsNullOrEmpty(definition.AbilityText)
                    ? definition.AbilityText
                    : definition.LoreText;
            }

            ApplyFoilLevel(foilLevel);
            ResetRotation();
        }

        public void ApplyFoilLevel(int level)
        {
            if (frontMeshRenderer == null) return;

            propBlock ??= new MaterialPropertyBlock();
            frontMeshRenderer.GetPropertyBlock(propBlock);

            // Shader properties for URP Metallic / Smoothness / Iridescence
            switch (level)
            {
                case 1: // Standard Card (Matte)
                    propBlock.SetFloat("_Metallic", 0.0f);
                    propBlock.SetFloat("_Smoothness", 0.2f);
                    propBlock.SetFloat("_IridescenceIntensity", 0.0f);
                    break;
                case 2: // Foil (Metallic sheen & iridescent shimmer)
                    propBlock.SetFloat("_Metallic", 0.7f);
                    propBlock.SetFloat("_Smoothness", 0.85f);
                    propBlock.SetFloat("_IridescenceIntensity", 0.6f);
                    break;
                case 3: // Pure Gold
                    propBlock.SetFloat("_Metallic", 1.0f);
                    propBlock.SetFloat("_Smoothness", 0.95f);
                    propBlock.SetFloat("_IridescenceIntensity", 0.9f);
                    break;
            }

            frontMeshRenderer.SetPropertyBlock(propBlock);
        }

        public void OnBeginDrag(Vector2 screenPosition)
        {
            if (!allowFreeRotation) return;
            isDragging = true;
            lastPointerPos = screenPosition;
        }

        public void OnDrag(Vector2 screenPosition)
        {
            if (!isDragging || !allowFreeRotation) return;

            float deltaX = screenPosition.x - lastPointerPos.x;
            targetYRotation -= deltaX * rotationSpeed;
            lastPointerPos = screenPosition;
        }

        public void OnEndDrag()
        {
            isDragging = false;
        }

        public void ResetRotation()
        {
            targetYRotation = 0f;
            currentYRotation = 0f;
            if (cardTransform != null)
            {
                cardTransform.localRotation = Quaternion.identity;
            }
        }

        private void HandleRotation()
        {
            if (cardTransform == null) return;

            if (!isDragging)
            {
                // Smoothly return towards front angle if idle or wrap around
                targetYRotation = Mathf.Lerp(targetYRotation, 0f, Time.deltaTime * autoReturnSpeed);
            }

            currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, Time.deltaTime * 15f);
            cardTransform.localRotation = Quaternion.Euler(0f, currentYRotation, 0f);
        }
    }
}
