using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Official MVP 20-card catalog defined in Rule 24:
    /// 6 Heroes, 6 Beasts, 5 Trompos, 3 Arenas.
    /// </summary>
    public static class MvpCardCatalog
    {
        private static readonly Dictionary<string, CardDefinition> CardsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<CardDefinition> AllCardsList = new();

        public static IReadOnlyList<CardDefinition> AllCards => AllCardsList;

        static MvpCardCatalog()
        {
            RegisterHeroes();
            RegisterBeasts();
            RegisterTrompos();
            RegisterArenas();
        }

        private static void Register(CardDefinition card)
        {
            CardsById[card.Id] = card;
            AllCardsList.Add(card);
        }

        public static CardDefinition? GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (CardsById.TryGetValue(id, out var card))
                return card;

            // Alias check for CHAMP_ <-> HERO_
            if (id.StartsWith("CHAMP_", StringComparison.OrdinalIgnoreCase))
            {
                var heroId = "HERO_" + id.Substring(6);
                if (CardsById.TryGetValue(heroId, out card))
                    return card;
            }
            else if (id.StartsWith("HERO_", StringComparison.OrdinalIgnoreCase))
            {
                var champId = "CHAMP_" + id.Substring(5);
                if (CardsById.TryGetValue(champId, out card))
                    return card;
            }

            // Alias check for BEAST_DIAMANTE <-> BEAST_LEVIATAN
            if (id.Equals("BEAST_DIAMANTE", StringComparison.OrdinalIgnoreCase))
            {
                if (CardsById.TryGetValue("BEAST_LEVIATAN", out card))
                    return card;
            }
            else if (id.Equals("BEAST_LEVIATAN", StringComparison.OrdinalIgnoreCase))
            {
                if (CardsById.TryGetValue("BEAST_DIAMANTE", out card))
                    return card;
            }

            // Alias check for Arenas
            if (id.Equals("ARENA_TEMPLO_MILENARIO", StringComparison.OrdinalIgnoreCase))
            {
                if (CardsById.TryGetValue("ARENA_TEMPLO", out card))
                    return card;
            }
            else if (id.Equals("ARENA_PISTA_CARRERAS", StringComparison.OrdinalIgnoreCase))
            {
                if (CardsById.TryGetValue("ARENA_PISTA", out card))
                    return card;
            }
            else if (id.Equals("ARENA_LLANURAS_TIRANO", StringComparison.OrdinalIgnoreCase))
            {
                if (CardsById.TryGetValue("ARENA_LLANURAS", out card))
                    return card;
            }

            return null;
        }

        public static IEnumerable<CardDefinition> GetByType(CardType type) =>
            AllCardsList.Where(c => c.Type == type);

        public static IEnumerable<CardDefinition> GetByClan(ClanType clan) =>
            AllCardsList.Where(c => c.Clan == clan);

        private static void RegisterHeroes()
        {
            // 1. Avispón (Depredadores, Cost 1, Power 2) - OnPlay: +1 to another friendly card
            Register(new CardDefinition(
                "HERO_AVISPON",
                "Avispón",
                CardType.Hero,
                ClanType.Depredadores,
                energyCost: 1,
                power: 2,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.BuffFriendlyPower,
                    value: 1,
                    description: "Al desplegar: Otorga +1 Poder a otra carta aliada en esta arena."
                ),
                imagePath: "CC_Export/Avispon.png",
                loreText: "Velocista implacable del clan Depredadores.",
                iconicQuote: "¡Nadie escapa de mi aguijón!",
                rarity: CardRarity.Common
            ));

            // 2. Draky (Fugitivos, Cost 2, Power 3) - OnPlay: Move self
            Register(new CardDefinition(
                "HERO_DRAKY",
                "Draky",
                CardType.Hero,
                ClanType.Fugitivos,
                energyCost: 2,
                power: 3,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.MoveSelf,
                    value: 0,
                    description: "Al desplegar: Se mueve a una arena adyacente tras este turno."
                ),
                imagePath: "CC_Export/Draky.png",
                loreText: "Exploradora audaz que domina el terreno cambiante.",
                iconicQuote: "¡La velocidad es mi mejor defensa!",
                rarity: CardRarity.Rare
            ));

            // 3. Cometín (Milenarios, Cost 2, Power 2) - Combo: +2 if another Milenario present
            Register(new CardDefinition(
                "HERO_COMETIN",
                "Cometín",
                CardType.Hero,
                ClanType.Milenarios,
                energyCost: 2,
                power: 2,
                ability: new CardAbility(
                    AbilityTrigger.Combo,
                    AbilityEffectType.ConditionalBuff,
                    value: 2,
                    description: "Combo: Si tienes otro Milenario aquí, obtiene +2 Poder.",
                    clanCondition: ClanType.Milenarios
                ),
                imagePath: "CC_Export/Cometin.png",
                loreText: "Joven prodigio inspirado por las tradiciones ancestrales.",
                iconicQuote: "¡El cometa guía mi destino!",
                rarity: CardRarity.Common
            ));

            // 4. Orión (Míticos, Cost 3, Power 4) - Passive: Trompos here gain +2 Power
            Register(new CardDefinition(
                "HERO_ORION",
                "Orión",
                CardType.Hero,
                ClanType.Miticos,
                energyCost: 3,
                power: 4,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.SynergyPower,
                    value: 2,
                    description: "Pasiva: Los Trompos en esta arena obtienen +2 Poder.",
                    targetTypeCondition: CardType.Trompo
                ),
                imagePath: "CC_Export/Orion.png",
                loreText: "Estratega cósmica que canaliza energía en artefactos místicos.",
                iconicQuote: "¡Todo está en la estrategia estelar!",
                rarity: CardRarity.Epic
            ));

            // 5. Turbo Cobra (Fugitivos, Cost 4, Power 5) - OnPlay: Reduce highest enemy card by -2
            Register(new CardDefinition(
                "HERO_TURBO_COBRA",
                "Turbo Cobra",
                CardType.Hero,
                ClanType.Fugitivos,
                energyCost: 4,
                power: 5,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.DebuffEnemyPower,
                    value: 2,
                    description: "Al desplegar: Reduce en -2 el Poder de la carta rival más fuerte aquí."
                ),
                imagePath: "CC_Export/Turbo Cobra.png",
                loreText: "Guerrero de alta velocidad que desestabiliza las líneas rivales.",
                iconicQuote: "¡Siente la mordida del rayo!",
                rarity: CardRarity.Rare
            ));

            // 6. Azteca (Milenarios, Cost 5, Power 7) - Combo: +3 if a Trompo is in this arena
            Register(new CardDefinition(
                "HERO_AZTECA",
                "Azteca",
                CardType.Hero,
                ClanType.Milenarios,
                energyCost: 5,
                power: 7,
                ability: new CardAbility(
                    AbilityTrigger.Combo,
                    AbilityEffectType.ConditionalBuff,
                    value: 3,
                    description: "Combo: Si tienes un Trompo en esta arena, obtiene +3 Poder.",
                    targetTypeCondition: CardType.Trompo
                ),
                imagePath: "CC_Export/Azteca.png",
                loreText: "Guardián legendario de templos y reliquias milenarias.",
                iconicQuote: "¡La historia y el poder de los ancestros están conmigo!",
                rarity: CardRarity.Legendary
            ));
        }

        private static void RegisterBeasts()
        {
            // 7. Panther Cazadora (Depredadores, Cost 2, Power 4) - Passive: +1 if 3+ friendly cards here
            Register(new CardDefinition(
                "BEAST_PANTHER",
                "Panther Cazadora",
                CardType.Beast,
                ClanType.Depredadores,
                energyCost: 2,
                power: 4,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.ConditionalBuff,
                    value: 1,
                    description: "Pasiva: Si tienes 3 o más cartas en esta arena, obtiene +1 Poder."
                ),
                imagePath: "CK_Export/Panther.png",
                loreText: "Cazadora sigilosa que lidera los ataques de manada.",
                iconicQuote: "¡La presa nunca ve venir el zarpazo!",
                rarity: CardRarity.Common
            ));

            // 8. Diamante Leviatán (Fugitivos, Cost 3, Power 5) - OnPlay: -1 to an enemy card
            Register(new CardDefinition(
                "BEAST_LEVIATAN",
                "Diamante Leviatán",
                CardType.Beast,
                ClanType.Fugitivos,
                energyCost: 3,
                power: 5,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.DebuffEnemyPower,
                    value: 1,
                    description: "Al desplegar: Reduce en -1 el Poder de una carta rival en esta arena."
                ),
                imagePath: "CK_Export/Diamante.png",
                loreText: "Bestia de diamante que rompe armaduras enemigas.",
                iconicQuote: "¡Inquebrantable e indomable!",
                rarity: CardRarity.Rare
            ));

            // 9. Fénix Ave Ígnea (Míticos, Cost 4, Power 6) - EndOfTurn: +1 if winning this arena
            Register(new CardDefinition(
                "BEAST_FENIX",
                "Fénix Ave Ígnea",
                CardType.Beast,
                ClanType.Miticos,
                energyCost: 4,
                power: 6,
                ability: new CardAbility(
                    AbilityTrigger.EndOfTurn,
                    AbilityEffectType.ConditionalBuff,
                    value: 1,
                    description: "Final de turno: Si vas ganando esta arena, obtiene +1 Poder."
                ),
                imagePath: "CK_Export/Fenix_.png",
                loreText: "Ave cósmica que renace más ardiente con cada victoria.",
                iconicQuote: "¡De las cenizas surge el fuego supremo!",
                rarity: CardRarity.Epic
            ));

            // 10. Rex Dracónico (Míticos, Cost 5, Power 8) - Passive: +2 if no enemy cards here
            Register(new CardDefinition(
                "BEAST_REX",
                "Rex Dracónico",
                CardType.Beast,
                ClanType.Miticos,
                energyCost: 5,
                power: 8,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.ConditionalBuff,
                    value: 2,
                    description: "Pasiva: Obtiene +2 Poder si no hay cartas rivales en esta arena."
                ),
                imagePath: "CK_Export/Rex.png",
                loreText: "Coloso supremo del clan Míticos, domina territorios vírgenes.",
                iconicQuote: "¡El rugido de las estrellas rige el campo!",
                rarity: CardRarity.Legendary
            ));

            // 11. Gólem Azteca (Milenarios, Cost 5, Power 7) - OnKnockout: Return to hand
            Register(new CardDefinition(
                "BEAST_GOLEM",
                "Gólem Azteca",
                CardType.Beast,
                ClanType.Milenarios,
                energyCost: 5,
                power: 7,
                ability: new CardAbility(
                    AbilityTrigger.OnKnockout,
                    AbilityEffectType.ReturnToHandOnKnockout,
                    value: 0,
                    description: "Al ser noqueado: Regresa a tu mano en lugar de ser destruido."
                ),
                imagePath: "CK_Export/Azteca.png",
                loreText: "Piedra viva que se reconstruye ante cualquier impacto letal.",
                iconicQuote: "¡La roca milenaria jamás perece!",
                rarity: CardRarity.Epic
            ));

            // 12. Jumbo King Cobra (Fugitivos, Cost 6, Power 10) - OnPlay: +1 to all friendly cards here
            Register(new CardDefinition(
                "BEAST_JUMBO_KING",
                "Jumbo King Cobra",
                CardType.Beast,
                ClanType.Fugitivos,
                energyCost: 6,
                power: 10,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.BuffAllFriendlyInLane,
                    value: 1,
                    description: "Al desplegar: Otorga +1 Poder a todas las demás cartas aliadas aquí."
                ),
                imagePath: "CK_Export/King Cobra.png",
                loreText: "Titán definitivo de las arenas fugitivas, rey indiscutible de la fuerza.",
                iconicQuote: "¡Mi presencia impone la ley del trueno!",
                rarity: CardRarity.Legendary
            ));
        }

        private static void RegisterTrompos()
        {
            // 13. Trompo Clásico (Neutral, Cost 1, Power 2) - OnPlay: Give another friendly card +2 Power
            Register(new CardDefinition(
                "TROMPO_CLASICO",
                "Trompo Clásico",
                CardType.Trompo,
                ClanType.Neutral,
                energyCost: 1,
                power: 2,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.BuffFriendlyPower,
                    value: 2,
                    description: "Al desplegar: Otorga +2 Poder a otra carta aliada en esta arena."
                ),
                imagePath: "Trompos_Export/Silver.png",
                loreText: "El trompo tradicional de madera, confiable y noble.",
                iconicQuote: "¡El giro clásico nunca falla!",
                rarity: CardRarity.Common
            ));

            // 14. Trompo Turbo Cobra (Fugitivos, Cost 2, Power 3) - OnPlay: Give all friendly cards here +1 Power
            Register(new CardDefinition(
                "TROMPO_TURBO_COBRA",
                "Trompo Turbo Cobra",
                CardType.Trompo,
                ClanType.Fugitivos,
                energyCost: 2,
                power: 3,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.BuffAllFriendlyInLane,
                    value: 1,
                    description: "Al desplegar: Otorga +1 Poder a todas las cartas aliadas en esta arena."
                ),
                imagePath: "Trompos_Export/Turbo Cobra_.png",
                loreText: "Trompo aerodinámico con punta de diamante de alta fricción.",
                iconicQuote: "¡Giro supersónico!",
                rarity: CardRarity.Common
            ));

            // 15. Trompo Cometa Fuego (Depredadores, Cost 3, Power 4) - OnPlay: Reduce an enemy card's Power by 3
            Register(new CardDefinition(
                "TROMPO_FUEGO",
                "Trompo Cometa Fuego",
                CardType.Trompo,
                ClanType.Depredadores,
                energyCost: 3,
                power: 4,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.DebuffEnemyPower,
                    value: 3,
                    description: "Al desplegar: Reduce en -3 el Poder de una carta rival en esta arena."
                ),
                imagePath: "Trompos_Export/Fénix.png",
                loreText: "Cuerpo incandescente que desintegra la concentración rival.",
                iconicQuote: "¡Quemando la arena con furia!",
                rarity: CardRarity.Rare
            ));

            // 16. Trompo Dragón Solar (Míticos, Cost 3, Power 3) - Passive: Heroes in this Arena gain +2 Power
            Register(new CardDefinition(
                "TROMPO_DRAGON",
                "Trompo Dragón Solar",
                CardType.Trompo,
                ClanType.Miticos,
                energyCost: 3,
                power: 3,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.SynergyPower,
                    value: 2,
                    description: "Pasiva: Los Héroes en esta arena obtienen +2 Poder.",
                    targetTypeCondition: CardType.Hero
                ),
                imagePath: "Trompos_Export/Turbo Dragón.png",
                loreText: "Reliquia solar que irradia bendiciones a los campeones que lo portan.",
                iconicQuote: "¡Luz solar que empodera a los héroes!",
                rarity: CardRarity.Epic
            ));

            // 17. Trompo Vórtice (Neutral, Cost 4, Power 4) - OnPlay: Move friendly card to another Arena
            Register(new CardDefinition(
                "TROMPO_VORTICE",
                "Trompo Vórtice",
                CardType.Trompo,
                ClanType.Neutral,
                energyCost: 4,
                power: 4,
                ability: new CardAbility(
                    AbilityTrigger.OnPlay,
                    AbilityEffectType.MoveTargetFriendly,
                    value: 0,
                    description: "Al desplegar: Mueve otra carta aliada de esta arena a otra arena."
                ),
                imagePath: "Trompos_Export/Espectro.png",
                loreText: "Giro magnético que dobla el espacio y reposiciona aliados.",
                iconicQuote: "¡El vórtice abre nuevos caminos!",
                rarity: CardRarity.Rare
            ));
        }

        private static void RegisterArenas()
        {
            // 18. Templo Milenario (Milenarios, Cost 4, Power 4)
            Register(new CardDefinition(
                "ARENA_TEMPLO",
                "Templo Milenario",
                CardType.Arena,
                ClanType.Milenarios,
                energyCost: 4,
                power: 4,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.ClanSynergyBuff,
                    value: 1,
                    description: "Efecto de Arena: Cartas Milenarios obtienen +1 Poder adicional (+2 total con afinidad)."
                ),
                imagePath: "Arena_Export/Templo Milenario.png",
                loreText: "Ruinas sagradas impregnadas de poder milenario.",
                iconicQuote: "La sabiduría de mil años protege esta tierra.",
                rarity: CardRarity.Epic
            ));

            // 19. Pista Draconia (Fugitivos, Cost 2, Power 3)
            Register(new CardDefinition(
                "ARENA_PISTA",
                "Pista Draconia",
                CardType.Arena,
                ClanType.Fugitivos,
                energyCost: 2,
                power: 3,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.ClanSynergyBuff,
                    value: 2,
                    description: "Efecto de Arena: Cartas Fugitivos o con 'Turbo' obtienen +2 Poder."
                ),
                imagePath: "Arena_Export/La Pista de Carreras.png",
                loreText: "Circuito de alta velocidad donde los más rápidos dominan.",
                iconicQuote: "¡Solo los veloces reclaman la victoria!",
                rarity: CardRarity.Common
            ));

            // 20. Llanuras del Tirano (Depredadores, Cost 6, Power 6)
            Register(new CardDefinition(
                "ARENA_LLANURAS",
                "Llanuras del Tirano",
                CardType.Arena,
                ClanType.Depredadores,
                energyCost: 6,
                power: 6,
                ability: new CardAbility(
                    AbilityTrigger.Passive,
                    AbilityEffectType.ConditionalBuff,
                    value: 2,
                    description: "Efecto de Arena: La carta con mayor Poder en esta arena obtiene +2 Poder."
                ),
                imagePath: "Arena_Export/Llanuras del Tirano.png",
                loreText: "Tierras salvajes donde los depredadores cazan sin tregua.",
                iconicQuote: "¡El más fuerte reina en estas llanuras!",
                rarity: CardRarity.Epic
            ));
        }

        /// <summary>
        /// Creates a legal 12-card deck compliant with Rule 1:
        /// - Exactly 12 cards
        /// - Maximum 2 copies per card
        /// - Balanced curve for Turns 1 to 6
        /// </summary>
        public static List<CardDefinition> CreateBalancedMvpDeck(ClanType primaryClan = ClanType.None, Random? rng = null)
        {
            var random = rng ?? new Random();
            var deck = new List<CardDefinition>();

            var playablePool = AllCardsList.ToList();

            // Desired curve:
            // 2x Cost 1
            // 3x Cost 2
            // 3x Cost 3
            // 2x Cost 4
            // 2x Cost 5/6

            void AddCardsWithCost(int cost, int count)
            {
                var candidates = playablePool.Where(c => c.EnergyCost == cost).ToList();
                if (primaryClan != ClanType.None && primaryClan != ClanType.Neutral)
                {
                    candidates = candidates
                        .OrderByDescending(c => c.Clan == primaryClan ? 1 : 0)
                        .ThenBy(_ => random.Next())
                        .ToList();
                }
                else
                {
                    candidates = candidates.OrderBy(_ => random.Next()).ToList();
                }

                int added = 0;
                while (added < count)
                {
                    foreach (var card in candidates)
                    {
                        if (added >= count) break;
                        int currentCopies = deck.Count(c => c.Id == card.Id);
                        if (currentCopies < 2)
                        {
                            deck.Add(card);
                            added++;
                        }
                    }

                    // Fallback to any cards if candidates exhausted
                    if (added < count)
                    {
                        var fallback = playablePool.FirstOrDefault(c => deck.Count(d => d.Id == c.Id) < 2);
                        if (fallback != null)
                        {
                            deck.Add(fallback);
                            added++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            AddCardsWithCost(1, 2); // 2 cards of cost 1
            AddCardsWithCost(2, 3); // 3 cards of cost 2
            AddCardsWithCost(3, 3); // 3 cards of cost 3
            AddCardsWithCost(4, 2); // 2 cards of cost 4

            // 2 cards of cost 5 or 6
            var highCostCandidates = playablePool.Where(c => c.EnergyCost >= 5).OrderBy(_ => random.Next()).ToList();
            int highAdded = 0;
            foreach (var card in highCostCandidates)
            {
                if (highAdded >= 2) break;
                if (deck.Count(c => c.Id == card.Id) < 2)
                {
                    deck.Add(card);
                    highAdded++;
                }
            }
            while (highAdded < 2 && deck.Count < 12)
            {
                var card = playablePool.FirstOrDefault(c => deck.Count(d => d.Id == c.Id) < 2);
                if (card != null)
                {
                    deck.Add(card);
                    highAdded++;
                }
                else break;
            }

            // Shuffle the constructed 12-card deck
            return deck.OrderBy(_ => random.Next()).ToList();
        }

        public static List<CardDefinition> GetMvpArenas() =>
            AllCardsList.Where(c => c.Type == CardType.Arena).ToList();
    }
}
