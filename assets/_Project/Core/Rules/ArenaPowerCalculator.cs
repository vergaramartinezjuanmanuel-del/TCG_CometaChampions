using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# calculator for card and lane power in the 3-Arena battle system.
    /// Incorporates Base Power, Trompo support power, Arena Clan Affinity,
    /// specific Arena mechanics, Círculo Cometa (+1 Elemental Advantage),
    /// passive card synergy abilities, and dynamic power modifiers.
    /// </summary>
    public static class ArenaPowerCalculator
    {
        public const int ElementalAdvantageBonus = 1;
        public const int ClanAffinityBonus = 1;

        /// <summary>
        /// Calculates the effective power of a card deployed in a specific arena against opposing cards.
        /// Overload for pure CardDefinition (legacy / preview).
        /// </summary>
        public static int CalculateCardEffectivePower(
            CardDefinition card,
            CardDefinition? arena,
            IReadOnlyList<CardDefinition>? opposingLaneCards)
        {
            if (card == null) return 0;

            int power = card.Power;

            // Fallback only if a card definition was initialized with Power <= 0
            if (power <= 0 && card.EnergyCost > 0)
            {
                power = Math.Max(2, card.EnergyCost + 1);
            }

            if (arena != null)
            {
                // 1. Arena Clan Affinity (+1 if arena clan matches card clan)
                if (arena.Clan != ClanType.None && arena.Clan != ClanType.Neutral && arena.Clan == card.Clan)
                {
                    power += ClanAffinityBonus;
                }

                // 2. Specific Arena Rules
                string arenaName = arena.Name ?? string.Empty;
                if (arenaName.Contains("Llanuras del Tirano", StringComparison.OrdinalIgnoreCase))
                {
                    power += 2;
                }
                else if ((arenaName.Contains("La Pista de Carreras", StringComparison.OrdinalIgnoreCase) ||
                          arenaName.Contains("Pista Draconia", StringComparison.OrdinalIgnoreCase)) &&
                         (card.Clan == ClanType.Fugitivos || (card.Name?.Contains("Turbo", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    power += 2;
                }
                else if (arenaName.Contains("Templo Milenario", StringComparison.OrdinalIgnoreCase) &&
                         card.Clan == ClanType.Milenarios)
                {
                    power += 1;
                }
                else if (arenaName.Contains("Campamento Panther", StringComparison.OrdinalIgnoreCase) &&
                         card.Clan == ClanType.Depredadores)
                {
                    power += 1;
                }
            }

            // 3. Círculo Cometa (+1 Elemental Advantage)
            if (opposingLaneCards != null && opposingLaneCards.Count > 0)
            {
                bool hasElementalAdvantage = opposingLaneCards.Any(op =>
                    op != null && ClanAdvantage.HasAdvantage(card.Clan, op.Clan));

                if (hasElementalAdvantage)
                {
                    power += ElementalAdvantageBonus;
                }
            }

            return Math.Max(0, power);
        }

        /// <summary>
        /// Calculates the effective power of a LaneCardInstance on the battlefield,
        /// including dynamic modifiers, passive synergies, and arena rules.
        /// </summary>
        public static int CalculateInstanceEffectivePower(
            LaneCardInstance instance,
            CardDefinition? arena,
            IReadOnlyList<LaneCardInstance>? friendlyLane,
            IReadOnlyList<LaneCardInstance>? opposingLane)
        {
            if (instance == null || instance.Card == null) return 0;
            if (!instance.IsRevealed) return 0; // Unrevealed cards contribute 0 power until turn resolution

            var card = instance.Card;
            int basePower = card.Power > 0 || card.EnergyCost <= 0
                ? card.Power
                : Math.Max(2, card.EnergyCost + 1);
            int power = basePower + instance.PowerModifier;

            // 1. Arena rules & Affinity
            if (arena != null)
            {
                if (arena.Clan != ClanType.None && arena.Clan != ClanType.Neutral && arena.Clan == card.Clan)
                {
                    power += ClanAffinityBonus;
                }

                string arenaName = arena.Name ?? string.Empty;
                if (arenaName.Contains("Llanuras del Tirano", StringComparison.OrdinalIgnoreCase))
                {
                    power += 2;
                }
                else if ((arenaName.Contains("La Pista de Carreras", StringComparison.OrdinalIgnoreCase) ||
                          arenaName.Contains("Pista Draconia", StringComparison.OrdinalIgnoreCase)) &&
                         (card.Clan == ClanType.Fugitivos || (card.Name?.Contains("Turbo", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    power += 2;
                }
                else if (arenaName.Contains("Templo Milenario", StringComparison.OrdinalIgnoreCase) &&
                         card.Clan == ClanType.Milenarios)
                {
                    power += 1;
                }
                else if (arenaName.Contains("Campamento Panther", StringComparison.OrdinalIgnoreCase) &&
                         card.Clan == ClanType.Depredadores)
                {
                    power += 1;
                }
            }

            // 2. Círculo Cometa (+1 Elemental Advantage)
            if (opposingLane != null && opposingLane.Count > 0)
            {
                bool hasAdvantage = opposingLane.Any(op =>
                    op != null && op.IsRevealed && ClanAdvantage.HasAdvantage(card.Clan, op.Card.Clan));

                if (hasAdvantage)
                {
                    power += ElementalAdvantageBonus;
                }
            }

            // 3. Passive Synergies from Friendly Cards in this Arena
            if (friendlyLane != null)
            {
                foreach (var ally in friendlyLane)
                {
                    if (ally == null || ally == instance || !ally.IsRevealed) continue;

                    // SynergyPower: e.g. Orión (Trompos gain +2), Trompo Dragón (Heroes gain +2)
                    if (ally.Card.Ability?.Trigger == AbilityTrigger.Passive &&
                        ally.Card.Ability.EffectType == AbilityEffectType.SynergyPower)
                    {
                        if (ally.Card.Ability.TargetTypeCondition.HasValue &&
                            card.Type == ally.Card.Ability.TargetTypeCondition.Value)
                        {
                            power += ally.Card.Ability.Value;
                        }
                    }
                }

                // Self Passive / Combo abilities:
                if (card.Ability != null)
                {
                    // Rex Dracónico: +2 if no enemy cards here
                    if (card.Ability.Trigger == AbilityTrigger.Passive &&
                        card.Id == "BEAST_REX")
                    {
                        if (opposingLane == null || opposingLane.Count(op => op != null && op.IsRevealed) == 0)
                        {
                            power += card.Ability.Value;
                        }
                    }
                    // Panther Cazadora: +1 if 3+ friendly cards here
                    else if (card.Ability.Trigger == AbilityTrigger.Passive &&
                             card.Id == "BEAST_PANTHER")
                    {
                        if (friendlyLane.Count(f => f != null && f.IsRevealed) >= 3)
                        {
                            power += card.Ability.Value;
                        }
                    }
                    // Cometín: Combo +2 if another Milenario here
                    else if (card.Ability.Trigger == AbilityTrigger.Combo &&
                             card.Id == "HERO_COMETIN")
                    {
                        if (friendlyLane.Any(f => f != null && f != instance && f.IsRevealed && f.Card.Clan == ClanType.Milenarios))
                        {
                            power += card.Ability.Value;
                        }
                    }
                    // Azteca: Combo +3 if a Trompo is in this arena
                    else if (card.Ability.Trigger == AbilityTrigger.Combo &&
                             card.Id == "HERO_AZTECA")
                    {
                        if (friendlyLane.Any(f => f != null && f != instance && f.IsRevealed && f.Card.Type == CardType.Trompo))
                        {
                            power += card.Ability.Value;
                        }
                    }
                }
            }

            return Math.Max(0, power);
        }

        /// <summary>
        /// Calculates the total power of all cards on one side of a lane using CardDefinition (legacy).
        /// </summary>
        public static int CalculateLanePower(
            IReadOnlyList<CardDefinition> laneCards,
            CardDefinition? arena,
            IReadOnlyList<CardDefinition>? opposingLaneCards)
        {
            if (laneCards == null || laneCards.Count == 0) return 0;

            int total = 0;
            for (int i = 0; i < laneCards.Count; i++)
            {
                total += CalculateCardEffectivePower(laneCards[i], arena, opposingLaneCards);
            }
            return total;
        }

        /// <summary>
        /// Calculates total power of a lane using LaneCardInstance (authoritative).
        /// </summary>
        public static int CalculateLanePower(
            IReadOnlyList<LaneCardInstance> laneCards,
            CardDefinition? arena,
            IReadOnlyList<LaneCardInstance>? opposingLaneCards)
        {
            if (laneCards == null || laneCards.Count == 0) return 0;

            int total = 0;
            for (int i = 0; i < laneCards.Count; i++)
            {
                total += CalculateInstanceEffectivePower(laneCards[i], arena, laneCards, opposingLaneCards);
            }
            return total;
        }

        /// <summary>
        /// Compares lane powers and returns:
        /// 1 = Player Leads, 2 = AI Leads, 0 = Tied.
        /// </summary>
        public static int GetLaneLeader(int playerPower, int aiPower)
        {
            if (playerPower > aiPower) return 1;
            if (aiPower > playerPower) return 2;
            return 0;
        }
    }
}
