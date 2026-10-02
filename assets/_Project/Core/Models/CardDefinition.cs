using System;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Immutable definition of a card in the Cometa Champions TCG.
    /// Shared between client UI, game simulation, and multiplayer server.
    /// </summary>
    [Serializable]
    public class CardDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public CardCategory Category { get; set; } = CardCategory.Champion;
        public ClanType Clan { get; set; } = ClanType.None;
        public int EnergyCost { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public string AbilityText { get; set; } = string.Empty;
        public string LoreText { get; set; } = string.Empty;
        public string IconicQuote { get; set; } = string.Empty;
        public string PhysicalSpecs { get; set; } = string.Empty;
        public CardRarity Rarity { get; set; } = CardRarity.Common;

        /// <summary>
        /// The 4 official card types according to Rule 7 (Hero, Beast, Trompo, Arena).
        /// Synchronized with Category for backward compatibility.
        /// </summary>
        public CardType Type
        {
            get => Category switch
            {
                CardCategory.Champion => CardType.Hero,
                CardCategory.KombatBeast => CardType.Beast,
                CardCategory.PhysicalTrompo => CardType.Trompo,
                CardCategory.Arena => CardType.Arena,
                _ => CardType.Hero
            };
            set => Category = value switch
            {
                CardType.Hero => CardCategory.Champion,
                CardType.Beast => CardCategory.KombatBeast,
                CardType.Trompo => CardCategory.PhysicalTrompo,
                CardType.Arena => CardCategory.Arena,
                _ => CardCategory.Champion
            };
        }

        /// <summary>
        /// Power is the primary scoring metric according to Rule 9.
        /// Synchronized with BaseAttack for backward compatibility.
        /// </summary>
        public int Power
        {
            get => BaseAttack;
            set => BaseAttack = value;
        }

        /// <summary>
        /// Structured ability definition for MVP rules (Rule 8 & 11).
        /// </summary>
        public CardAbility Ability { get; set; } = new();

        /// <summary>
        /// Relative path to high-res card artwork inside Assets/Resources/Cards/.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// True if this card participates in combat with Cost, Attack, and Health stats.
        /// All 4 categories (Champion, KombatBeast, PhysicalTrompo, Arena) share these stats uniformly.
        /// </summary>
        public bool IsCreature => true;
        public int Attack => BaseAttack;
        public int Health => BaseHealth;

        public CardDefinition() { }

        public CardDefinition(
            string id,
            string name,
            CardCategory category,
            ClanType clan,
            int energyCost,
            int baseAttack,
            int baseHealth = 0,
            string abilityText = "",
            string loreText = "",
            string iconicQuote = "",
            string physicalSpecs = "",
            CardRarity rarity = CardRarity.Common,
            string imagePath = "")
        {
            Id = id;
            Name = name;
            Category = category;
            Clan = clan;
            EnergyCost = energyCost;
            BaseAttack = baseAttack;
            BaseHealth = baseHealth;
            AbilityText = abilityText;
            LoreText = loreText;
            IconicQuote = iconicQuote;
            PhysicalSpecs = physicalSpecs;
            Rarity = rarity;
            ImagePath = imagePath;
        }

        public CardDefinition(
            string id,
            string name,
            CardType type,
            ClanType clan,
            int energyCost,
            int power,
            CardAbility ability,
            string imagePath = "",
            string loreText = "",
            string iconicQuote = "",
            CardRarity rarity = CardRarity.Common)
        {
            Id = id;
            Name = name;
            Type = type;
            Clan = clan;
            EnergyCost = energyCost;
            BaseAttack = power;
            BaseHealth = power;
            Ability = ability ?? new CardAbility();
            AbilityText = ability?.Description ?? string.Empty;
            ImagePath = imagePath;
            LoreText = loreText;
            IconicQuote = iconicQuote;
            Rarity = rarity;
        }
    }
}
