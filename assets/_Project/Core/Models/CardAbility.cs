using System;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// The 4 official card types according to Rule 7:
    /// HERO, BEAST, TROMPO, ARENA.
    /// </summary>
    public enum CardType
    {
        Hero = 1,
        Beast = 2,
        Trompo = 3,
        Arena = 4
    }

    /// <summary>
    /// Supported ability triggers according to Rule 11:
    /// On Play, Passive, End of Turn, On Knockout, Combo.
    /// </summary>
    public enum AbilityTrigger
    {
        None = 0,
        OnPlay = 1,
        Passive = 2,
        EndOfTurn = 3,
        OnKnockout = 4,
        Combo = 5
    }

    /// <summary>
    /// Specific ability effect types implemented for the MVP rules (Rule 12 & 13).
    /// </summary>
    public enum AbilityEffectType
    {
        None = 0,
        BuffFriendlyPower = 1,      // Give another friendly card +X Power
        DebuffEnemyPower = 2,       // Reduce an enemy card's Power by X
        BuffAllFriendlyInLane = 3,  // Give all friendly cards in this Arena +X Power
        MoveSelf = 4,               // Move this card to another Arena
        MoveTargetFriendly = 5,     // Move another friendly card to another Arena
        KnockoutTarget = 6,         // Remove target card from Arena
        ReturnToHandOnKnockout = 7, // If knocked out, return to hand
        ConditionalBuff = 8,        // Combo / condition: if clan or type present, gain +X Power
        SynergyPower = 9,           // Passive: other cards of specified type/clan gain +X Power
        ClanSynergyBuff = 10        // Arena/clan passive bonus
    }

    /// <summary>
    /// Structured ability definition for MVP cards according to Rule 8 & 11.
    /// </summary>
    [Serializable]
    public class CardAbility
    {
        public AbilityTrigger Trigger { get; set; } = AbilityTrigger.None;
        public AbilityEffectType EffectType { get; set; } = AbilityEffectType.None;
        public int Value { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public CardType? TargetTypeCondition { get; set; } = null;
        public ClanType? ClanCondition { get; set; } = null;

        public CardAbility() { }

        public CardAbility(
            AbilityTrigger trigger,
            AbilityEffectType effectType,
            int value = 0,
            string description = "",
            CardType? targetTypeCondition = null,
            ClanType? clanCondition = null)
        {
            Trigger = trigger;
            EffectType = effectType;
            Value = value;
            Description = description;
            TargetTypeCondition = targetTypeCondition;
            ClanCondition = clanCondition;
        }

        public static CardAbility None => new(AbilityTrigger.None, AbilityEffectType.None);
    }
}
