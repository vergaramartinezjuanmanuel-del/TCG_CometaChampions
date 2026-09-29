using System;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Calculated transform attributes for a card in the rounded hand fan.
    /// </summary>
    public struct CardFanTransform
    {
        public float X;
        public float Y;
        public float ZAngle;
        public float Scale;
    }

    /// <summary>
    /// Pure mathematical calculator that positions, curves, and rotates cards in the player's hand
    /// into an ergonomic rounded fan arch, matching the official visual redesign.
    /// </summary>
    public static class HandLayoutCalculator
    {
        public const float DefaultSpacing = 100f;
        public const float DefaultArchHeight = 28f;
        public const float DefaultMaxAngle = 8.5f;
        public const float DefaultSelectedElevation = 32f;

        /// <summary>
        /// Computes the (X, Y, ZAngle, Scale) transform for card i in a hand of totalCards.
        /// Center cards are highest in Y and vertical, while outer cards curve downward in Y
        /// and rotate smoothly outward.
        /// </summary>
        public static CardFanTransform CalculateTransform(
            int cardIndex,
            int totalCards,
            bool isSelected,
            float spacing = DefaultSpacing,
            float archHeight = DefaultArchHeight,
            float maxAngle = DefaultMaxAngle,
            float selectedElevation = DefaultSelectedElevation)
        {
            if (totalCards <= 0 || cardIndex < 0 || cardIndex >= totalCards)
            {
                return new CardFanTransform { X = 0f, Y = 0f, ZAngle = 0f, Scale = 1f };
            }

            if (totalCards == 1)
            {
                float yOne = isSelected ? selectedElevation : 0f;
                float scaleOne = isSelected ? 1.06f : 1f;
                return new CardFanTransform { X = 0f, Y = yOne, ZAngle = 0f, Scale = scaleOne };
            }

            float centerIndex = (totalCards - 1) * 0.5f;
            float offsetFromCenter = cardIndex - centerIndex;
            float normalized = offsetFromCenter / centerIndex; // in [-1.0, 1.0]

            // 1. Horizontal X position
            float x = offsetFromCenter * spacing;

            // 2. Vertical Y position along parabolic arch (highest at center, lowest at edges)
            float baseArcY = archHeight * (1f - (normalized * normalized));
            float y = baseArcY + (isSelected ? selectedElevation : 0f);

            // 3. Rotation Z: counter-clockwise on left (positive), clockwise on right (negative)
            float angle = -normalized * maxAngle;
            if (isSelected)
            {
                angle *= 0.35f; // Straighten up slightly when selected for visual clarity
            }

            float scale = isSelected ? 1.06f : 1f;

            return new CardFanTransform
            {
                X = x,
                Y = y,
                ZAngle = angle,
                Scale = scale
            };
        }
    }
}
