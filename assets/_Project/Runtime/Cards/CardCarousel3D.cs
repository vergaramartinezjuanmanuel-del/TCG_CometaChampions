using System;
using System.Collections.Generic;
using UnityEngine;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Cards
{
    /// <summary>
    /// 3D Circular Orbiting Card Carousel.
    /// Displays cards arranged evenly along a horizontal circle, matching the exact
    /// dimensions and rendering of Center_Card3D_Viewport (CardWidth = 2.5f, CardHeight = 3.5f).
    /// As cards rotate around the center, the front-most card faces the camera directly,
    /// while side and back cards show 3D perspective and the official Trompos Cometa reverso.
    /// </summary>
    public class CardCarousel3D : MonoBehaviour
    {
        [Header("Carousel Orbit Settings")]
        [SerializeField] private float radius = 3.6f;
        [SerializeField] private float orbitSpeed = 45f; // degrees per second
        [SerializeField] private float verticalBobbingSpeed = 2f;
        [SerializeField] private float verticalBobbingAmount = 0.08f;
        [SerializeField] private float inwardTiltAngle = 4f; // degrees tilted inward
        [SerializeField] private float carouselCenterZ = -0.9f; // puts front card at Z = -4.5f

        public static readonly string[] DefaultIconicCardIds = new[]
        {
            "CHAMP_DRAKY",        // Fugitivos
            "CHAMP_COMETIN",      // Milenarios
            "CHAMP_AVISPON",      // Depredadores
            "CHAMP_ESPECTRO",     // Míticos
            "CHAMP_AZTECA",       // Milenarios
            "CHAMP_JUMBO_COBRA"   // Fugitivos
        };

        private readonly List<Card3D> spawnedCard3Ds = new();
        private readonly List<Transform> cardHolderTransforms = new();
        private float currentAngle = 0f;
        private bool isInitialized = false;

        public float Radius => radius;
        public float OrbitSpeed { get => orbitSpeed; set => orbitSpeed = value; }
        public int CardCount => spawnedCard3Ds.Count;

        private void Awake()
        {
            if (!isInitialized)
            {
                InitializeCarousel(DefaultIconicCardIds);
            }
        }

        private void Update()
        {
            if (cardHolderTransforms.Count == 0) return;

            // Advance orbit angle smoothly using unscaled delta time
            currentAngle = (currentAngle + orbitSpeed * Time.unscaledDeltaTime) % 360f;
            UpdateCardPositions(currentAngle);
        }

        /// <summary>
        /// Initializes the circular carousel with the specified card IDs or definitions.
        /// </summary>
        public void InitializeCarousel(IReadOnlyList<string> cardIds = null)
        {
            ClearExistingCards();

            var idsToUse = cardIds != null && cardIds.Count > 0 ? cardIds : DefaultIconicCardIds;
            int count = idsToUse.Count;

            for (int i = 0; i < count; i++)
            {
                string cardId = idsToUse[i];
                var cardData = ResolveShowcaseData(cardId);
                if (cardData == null) continue;

                // 1. Create Holder Anchor for smooth orbit positioning & rotation
                var holderGo = new GameObject($"CarouselCard_{i}_{cardData.Name}");
                holderGo.transform.SetParent(transform, false);

                // 2. Attach Card3D with exact dimensions matching Center_Card3D_Viewport
                var card3D = holderGo.AddComponent<Card3D>();
                card3D.EnsureHierarchy();
                card3D.SetCard(cardData, foilLevel: 1);
                card3D.SetReverso("oro");

                cardHolderTransforms.Add(holderGo.transform);
                spawnedCard3Ds.Add(card3D);
            }

            isInitialized = true;
            UpdateCardPositions(currentAngle);
        }

        /// <summary>
        /// Updates the 3D position and orientation of every card along the circular orbit.
        /// When angle = 0, the card is at the front (closest to camera, facing directly forward).
        /// </summary>
        public void UpdateCardPositions(float baseAngle)
        {
            int count = cardHolderTransforms.Count;
            if (count == 0) return;

            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                var holder = cardHolderTransforms[i];
                if (holder == null) continue;

                // Card angle along the circle
                float cardAngleDeg = (baseAngle + i * angleStep) % 360f;
                float cardAngleRad = cardAngleDeg * Mathf.Deg2Rad;

                // X = R * sin(angle), Z = CenterZ - R * cos(angle)
                // At angle = 0: X = 0, Z = CenterZ - R (closest to camera at Z = -4.5f)
                float x = radius * Mathf.Sin(cardAngleRad);
                float z = carouselCenterZ - radius * Mathf.Cos(cardAngleRad);

                // Gentle vertical wave bobbing
                float bob = Mathf.Sin(Time.unscaledTime * verticalBobbingSpeed + i * 1.05f) * verticalBobbingAmount;

                holder.localPosition = new Vector3(x, bob, z);

                // Rotation: card faces outward from orbit center, plus slight inward tilt
                holder.localRotation = Quaternion.Euler(inwardTiltAngle, cardAngleDeg, 0f);
            }
        }

        private ShowcaseCardData ResolveShowcaseData(string cardId)
        {
            var showcase = CardShowcaseCatalog.GetCardByDefinitionIdOrName(cardId, cardId)
                           ?? CardShowcaseCatalog.GetCardById(cardId);
            if (showcase != null) return showcase;

            var cardDef = CardCatalog.GetCardById(cardId) ?? MvpCardCatalog.GetCardById(cardId);
            if (cardDef != null)
            {
                return DynamicCardAssembler.ResolveShowcaseCardData(cardDef);
            }

            return null;
        }

        private void ClearExistingCards()
        {
            foreach (var holder in cardHolderTransforms)
            {
                if (holder != null)
                {
                    if (Application.isPlaying) Destroy(holder.gameObject);
                    else DestroyImmediate(holder.gameObject);
                }
            }
            cardHolderTransforms.Clear();
            spawnedCard3Ds.Clear();
        }

        private void OnDestroy()
        {
            ClearExistingCards();
        }
    }
}
