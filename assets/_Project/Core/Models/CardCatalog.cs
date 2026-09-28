using System;
using System.Collections.Generic;
using System.Linq;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Master catalog containing definitions for all 80 cards of Base Set 1 ("Tomo 1").
    /// </summary>
    public static class CardCatalog
    {
        private static readonly Dictionary<string, CardDefinition> CardsById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<CardDefinition> AllCardsList = new();

        public static IReadOnlyList<CardDefinition> AllCards => AllCardsList;

        static CardCatalog()
        {
            RegisterChampions();
            RegisterKombatBeasts();
            RegisterPhysicalTrompos();
            RegisterArenas();
        }

        private static void Register(CardDefinition card)
        {
            var showcase = CardShowcaseCatalog.GetCardByDefinitionIdOrName(card.Id, card.Name);
            if (showcase != null)
            {
                if (string.IsNullOrEmpty(card.ImagePath) && !string.IsNullOrEmpty(showcase.Image))
                {
                    card.ImagePath = showcase.Image;
                }

                if (card.Rarity == CardRarity.Common && !string.IsNullOrEmpty(showcase.Rarity))
                {
                    card.Rarity = ParseRarity(showcase.Rarity);
                }
            }

            CardsById[card.Id] = card;
            AllCardsList.Add(card);
        }

        private static CardRarity ParseRarity(string rarityStr)
        {
            if (string.IsNullOrEmpty(rarityStr)) return CardRarity.Common;
            var r = rarityStr.Trim().ToLowerInvariant();
            if (r.Contains("comun") || r.Contains("común")) return CardRarity.Common;
            if (r.Contains("rara") || r.Contains("rare")) return CardRarity.Rare;
            if (r.Contains("epica") || r.Contains("épica") || r.Contains("epic")) return CardRarity.Epic;
            if (r.Contains("legendaria") || r.Contains("legendary")) return CardRarity.Legendary;
            return CardRarity.Common;
        }

        public static CardDefinition? GetCardById(string id)
        {
            CardsById.TryGetValue(id, out var card);
            return card;
        }

        public static IEnumerable<CardDefinition> GetByClan(ClanType clan) =>
            AllCardsList.Where(c => c.Clan == clan);

        public static IEnumerable<CardDefinition> GetByCategory(CardCategory category) =>
            AllCardsList.Where(c => c.Category == category);

        private static void RegisterChampions()
        {
            Register(new CardDefinition("CHAMP_AVISPON", "Avispón", CardCategory.Champion, ClanType.Depredadores, 2, 3, 1,
                "Maestro de la velocidad. Reflejos impecables.", "Velocista del enjambre.", "¡Soy más rápido que el viento!"));

            Register(new CardDefinition("CHAMP_ORION", "Orión", CardCategory.Champion, ClanType.Miticos, 4, 2, 5,
                "Sabia y estratégica. Planea antes de atacar.", "Estratega cósmica.", "¡Todo está en la estrategia!"));

            Register(new CardDefinition("CHAMP_JUMBO_COBRA", "Jumbo Cobra", CardCategory.Champion, ClanType.Fugitivos, 5, 5, 4,
                "Ágil y poderoso. Ataca con precisión letal.", "Guerrero pesado de las arenas.", "¡Siempre me levanto más fuerte!"));

            Register(new CardDefinition("CHAMP_DRAKY", "Draky", CardCategory.Champion, ClanType.Fugitivos, 3, 2, 4,
                "Curiosa, supera obstáculos con ingenio.", "Exploradora audaz.", "¡La aventura apenas comienza!"));

            Register(new CardDefinition("CHAMP_AZTECA", "Azteca", CardCategory.Champion, ClanType.Milenarios, 6, 6, 6,
                "Fuerte y sabio. Lucha con honor y tradición.", "Guardián de templos ancestrales.", "¡La historia está de mi lado!"));

            Register(new CardDefinition("CHAMP_PANTHER", "Panther", CardCategory.Champion, ClanType.Depredadores, 4, 5, 2,
                "Sigilosa y letal. Sorprende a sus rivales.", "Cazadora de sombras.", "¡Silenciosa como una pantera!"));

            Register(new CardDefinition("CHAMP_SILVER", "Silver", CardCategory.Champion, ClanType.Milenarios, 3, 3, 3,
                "Elegante y brillante. Movimientos estilizados.", "Caballero de la plata.", "¡El estilo lo es todo!"));

            Register(new CardDefinition("CHAMP_KING_COBRA", "King Cobra", CardCategory.Champion, ClanType.Depredadores, 7, 6, 7,
                "Líder natural. Gran fuerza y control.", "Monarca temido de la arena.", "¡El rey nunca pierde!"));

            Register(new CardDefinition("CHAMP_TURBO_COBRA", "Turbo Cobra", CardCategory.Champion, ClanType.Milenarios, 1, 2, 1,
                "Pura velocidad imparable.", "Iniciador relámpago.", "¡A toda velocidad!"));

            Register(new CardDefinition("CHAMP_ESPECTRO", "Espectro", CardCategory.Champion, ClanType.Miticos, 4, 4, 3,
                "Misteriosa. Ataca desde las sombras.", "Aparición etérea.", "¡El misterio es mi fuerza!"));

            Register(new CardDefinition("CHAMP_TIGRE", "Tigre", CardCategory.Champion, ClanType.Depredadores, 5, 6, 4,
                "Valiente y feroz felino. Nunca retrocede.", "Luchador indomable.", "¡Soy una fuerza imparable!"));

            Register(new CardDefinition("CHAMP_FENIX", "Fénix", CardCategory.Champion, ClanType.Miticos, 6, 4, 6,
                "Cuando cae, se levanta renaciendo.", "Ave mística inmortal.", "¡El fuego me guía!"));

            Register(new CardDefinition("CHAMP_REX", "Rex", CardCategory.Champion, ClanType.Miticos, 8, 8, 8,
                "Fuerza bruta imparable. Nunca se rinde.", "El titán prehistórico.", "¡El poder está en mis manos!"));

            Register(new CardDefinition("CHAMP_COMETIN", "Cometín", CardCategory.Champion, ClanType.Milenarios, 2, 2, 2,
                "Energía inagotable para las aventuras.", "El corazón alegre de los Milenarios.", "¡Nunca me detengo!"));

            Register(new CardDefinition("CHAMP_DIAMANTE", "Diamante", CardCategory.Champion, ClanType.Fugitivos, 4, 1, 7,
                "Resistente como una roca. Defenderá al equipo.", "Baluarte inquebrantable.", "¡Fuerte como un diamante!"));

            Register(new CardDefinition("CHAMP_TURBO_KING", "Turbo King", CardCategory.Champion, ClanType.Fugitivos, 6, 5, 5,
                "Combina velocidad supersónica con fuerza bruta.", "El rey del turbo.", "¡El rey del turbo está aquí!"));

            Register(new CardDefinition("CHAMP_TURBO_FLASH", "Turbo Flash", CardCategory.Champion, ClanType.Miticos, 3, 4, 2,
                "El más rápido de todos. Rayo de la arena.", "Velocista de la luz.", "¡Soy más rápido que un rayo!"));

            Register(new CardDefinition("CHAMP_SPIDER", "Spider", CardCategory.Champion, ClanType.Fugitivos, 3, 1, 4,
                "Astuto, espera en su red el momento perfecto.", "Tejedor de trampas.", "¡La paciencia es clave!"));

            Register(new CardDefinition("CHAMP_TURBO_DRAGON", "Turbo Dragón", CardCategory.Champion, ClanType.Depredadores, 7, 7, 5,
                "Espíritu ardiente y ataques de alto impacto.", "El dragón llameante.", "¡El dragón nunca se rinde!"));

            Register(new CardDefinition("CHAMP_DIAMANTIN", "Diamantín", CardCategory.Champion, ClanType.Milenarios, 1, 1, 2,
                "Pequeña pero valiente y llena de coraje.", "Pequeña chispa defensiva.", "¡Brillante y fuerte!"));
        }

        private static void RegisterKombatBeasts()
        {
            Register(new CardDefinition("BEAST_AVISPON", "Avispón (Enjambre)", CardCategory.KombatBeast, ClanType.Depredadores, 3, 4, 2,
                "Domina los cielos de la arena a un nivel supersónico.", "Encarnación pura del zumbido ancestral."));

            Register(new CardDefinition("BEAST_ORION", "Orión (Entidad)", CardCategory.KombatBeast, ClanType.Miticos, 5, 3, 6,
                "Ser cósmico que canaliza pura estrategia ancestral.", "Entidad astral de Kamino."));

            Register(new CardDefinition("BEAST_JUMBO_COBRA", "Jumbo Cobra (Sierpe)", CardCategory.KombatBeast, ClanType.Fugitivos, 6, 6, 5,
                "Serpiente colosal capaz de aplastar cualquier obstáculo.", "La gran serpiente de las profundidades."));

            Register(new CardDefinition("BEAST_DRAKY", "Draky (Aparecido)", CardCategory.KombatBeast, ClanType.Fugitivos, 4, 3, 5,
                "Dragón nacido directamente de la energía mágica fugitiva.", "Draco espectral indomable."));

            Register(new CardDefinition("BEAST_AZTECA", "Azteca (Guardián)", CardCategory.KombatBeast, ClanType.Milenarios, 7, 7, 7,
                "El gólem de piedra ancestral que defiende los templos.", "El monolito eterno de los Milenarios."));

            Register(new CardDefinition("BEAST_PANTHER", "Panther (Cazadora)", CardCategory.KombatBeast, ClanType.Depredadores, 5, 6, 3,
                "La bestia nocturna más temida de las junglas.", "Furia silenciosa de la noche."));

            Register(new CardDefinition("BEAST_SILVER", "Silver (Metálico)", CardCategory.KombatBeast, ClanType.Milenarios, 4, 4, 4,
                "Bestia cubierta de plata líquida, deslumbrante y letal.", "Escultura viviente de plata pura."));

            Register(new CardDefinition("BEAST_KING_COBRA", "King Cobra (Rey Bestia)", CardCategory.KombatBeast, ClanType.Depredadores, 8, 7, 8,
                "La serpiente real primaria, origen de las leyendas.", "El patriarca letal de los colmillos."));

            Register(new CardDefinition("BEAST_TURBO_COBRA", "Turbo Cobra (Víbora)", CardCategory.KombatBeast, ClanType.Milenarios, 2, 3, 1,
                "Víbora pequeña capaz de atravesar barreras de sonido.", "El dardo viviente de los valles."));

            Register(new CardDefinition("BEAST_ESPECTRO", "Espectro (Sombra)", CardCategory.KombatBeast, ClanType.Miticos, 5, 5, 3,
                "Espíritu puro que caza desde la dimensión oculta.", "La manifestación del vacío estelar."));

            Register(new CardDefinition("BEAST_TIGRE", "Tigre (Dientes de Sable)", CardCategory.KombatBeast, ClanType.Depredadores, 6, 7, 5,
                "Felino prehistórico con garras que destrozan acero.", "Fuerza salvaje de la era de hielo."));

            Register(new CardDefinition("BEAST_FENIX", "Fénix (Ave Ígnea)", CardCategory.KombatBeast, ClanType.Miticos, 7, 5, 7,
                "El pájaro de fuego inmortal de Kamino.", "Llama que nunca se extingue."));

            Register(new CardDefinition("BEAST_REX", "Rex (Tirano)", CardCategory.KombatBeast, ClanType.Miticos, 9, 9, 9,
                "El dinosaurio rey indiscutible, amenaza divina.", "Cúspide de la escala alimenticia."));

            Register(new CardDefinition("BEAST_COMETIN", "Cometín (Rayo Astral)", CardCategory.KombatBeast, ClanType.Milenarios, 3, 3, 3,
                "Encarnación pura de una estrella veloz.", "Centella cósmica guiadora."));

            Register(new CardDefinition("BEAST_DIAMANTE", "Diamante (Leviatán Terrestre)", CardCategory.KombatBeast, ClanType.Fugitivos, 5, 2, 8,
                "Criatura de roca cristalizada inquebrantable.", "Muralla viviente impenetrable."));

            Register(new CardDefinition("BEAST_TURBO_KING", "Turbo King (Monarca)", CardCategory.KombatBeast, ClanType.Fugitivos, 7, 6, 6,
                "Líder bestial de las llanuras, masa inercial pura.", "Soberano de las estampidas."));

            Register(new CardDefinition("BEAST_TURBO_FLASH", "Turbo Flash (Relámpago)", CardCategory.KombatBeast, ClanType.Miticos, 4, 5, 2,
                "Entidad de electricidad pura que surca los cielos.", "El rayo viviente de la tormenta."));

            Register(new CardDefinition("BEAST_SPIDER", "Spider (Viuda Escurridiza)", CardCategory.KombatBeast, ClanType.Fugitivos, 4, 2, 5,
                "Arácnido gigante que teje redes de cristal cometa.", "La arquitecta silenciosa de emboscadas."));

            Register(new CardDefinition("BEAST_TURBO_DRAGON", "Turbo Dragón (Wyvern)", CardCategory.KombatBeast, ClanType.Depredadores, 8, 8, 6,
                "El dragón feroz que reina los cielos de los Depredadores.", "Calamidad alada de fuego morado."));

            Register(new CardDefinition("BEAST_DIAMANTIN", "Diamantín (Cristalino)", CardCategory.KombatBeast, ClanType.Milenarios, 2, 2, 3,
                "Pequeño gólem brillante que proyecta escudos defensivos.", "Gema animada protectora."));
        }

        private static void RegisterPhysicalTrompos()
        {
            Register(new CardDefinition("TROMPO_ESPECTRO", "Trompo Espectro", CardCategory.PhysicalTrompo, ClanType.Miticos, 3, 0, 0,
                "Otorga a un personaje aliado 'Invisibilidad' (No puede ser atacado durante 1 turno).",
                "Punta de metal. Diseño aerodinámico oscuro.", "", "Punta metálica, peso 54g"));

            Register(new CardDefinition("TROMPO_ORION", "Trompo Orión", CardCategory.PhysicalTrompo, ClanType.Miticos, 2, 0, 0,
                "Permite al jugador robar 2 cartas extra de su mazo.",
                "Punta giratoria. El clásico ideal para tácticas pensadas.", "", "Punta de rodamiento"));

            Register(new CardDefinition("TROMPO_KING_COBRA", "Trompo King Cobra", CardCategory.PhysicalTrompo, ClanType.Depredadores, 5, 0, 0,
                "Lanza un giro explosivo que hace 5 de daño a una carta enemiga al azar.",
                "Diseño ancho. El rey indiscutible de torneos de impacto.", "", "Cuerpo ancho reforzado"));

            Register(new CardDefinition("TROMPO_AZTECA", "Trompo Azteca", CardCategory.PhysicalTrompo, ClanType.Milenarios, 4, 0, 0,
                "Crea un 'Escudo Giratorio' en la arena que absorbe 4 puntos de daño.",
                "Edición clásica con majestuosas grecas del folclore.", "", "Punta fija clásica"));

            Register(new CardDefinition("TROMPO_COMETIN", "Trompo Cometín", CardCategory.PhysicalTrompo, ClanType.Milenarios, 1, 0, 0,
                "Un aliado obtiene +1 de Fuerza inmediata en este turno.",
                "Trompo brillante y sumamente ligero. Excelente para empezar.", "", "Plástico ultraligero"));

            Register(new CardDefinition("TROMPO_DIAMANTIN", "Trompo Diamantín", CardCategory.PhysicalTrompo, ClanType.Milenarios, 2, 0, 0,
                "Elimina todo el daño que ha sufrido una carta aliada en el campo.",
                "Plástico translúcido premium.", "", "Cuerpo translúcido"));

            Register(new CardDefinition("TROMPO_JUMBO_COBRA", "Trompo Jumbo Cobra", CardCategory.PhysicalTrompo, ClanType.Fugitivos, 6, 0, 0,
                "Equípalo a un aliado (+4 de Ataque). Si muere, el trompo regresa a tu mano.",
                "Tamaño extragrande. Genera inercia brutal.", "", "Tamaño Jumbo XL"));

            Register(new CardDefinition("TROMPO_REX", "Trompo Rex", CardCategory.PhysicalTrompo, ClanType.Miticos, 5, 0, 0,
                "Aplasta la tierra al girar: todos los personajes enemigos pierden 2 de Vida.",
                "Plástico grueso texturizado en verde. Domina el suelo duro.", "", "Corona texturizada"));

            Register(new CardDefinition("TROMPO_AVISPON", "Trompo Avispón", CardCategory.PhysicalTrompo, ClanType.Depredadores, 2, 0, 0,
                "Permite que un personaje que acaba de entrar ataque inmediatamente este turno.",
                "Textura rayada. Corta el viento con zumbido agudo.", "", "Punta fina de velocidad"));

            Register(new CardDefinition("TROMPO_DRAKY", "Trompo Draky", CardCategory.PhysicalTrompo, ClanType.Fugitivos, 3, 0, 0,
                "Obliga al oponente a mostrar su mano y descartar la carta de menor costo.",
                "Compacto y travieso. Rebota de formas impredecibles.", "", "Punta de rebote"));

            Register(new CardDefinition("TROMPO_PANTHER", "Trompo Panther", CardCategory.PhysicalTrompo, ClanType.Depredadores, 4, 0, 0,
                "Emboscada: Puedes jugar este trompo en el turno del rival para bloquear su ataque.",
                "Color negro mate. Diseñado para atacar sin ser visto.", "", "Acabado mate sigiloso"));

            Register(new CardDefinition("TROMPO_SILVER", "Trompo Silver", CardCategory.PhysicalTrompo, ClanType.Milenarios, 3, 0, 0,
                "Refleja el próximo daño mágico que reciba tu héroe hacia el enemigo.",
                "Pintura metalizada especial que destella con el sol.", "", "Cromado plateado"));

            Register(new CardDefinition("TROMPO_TURBO_COBRA", "Trompo Turbo Cobra", CardCategory.PhysicalTrompo, ClanType.Milenarios, 1, 0, 0,
                "Roba 1 carta y descarta 1 carta para ciclar tu mazo rápidamente.",
                "Diseño hiper-estilizado para minimizar fricción.", "", "Punta de baja fricción"));

            Register(new CardDefinition("TROMPO_TIGRE", "Trompo Tigre", CardCategory.PhysicalTrompo, ClanType.Depredadores, 5, 0, 0,
                "Equípalo a un aliado para ganar 'Golpe Penetrante' (Ignora escudos enemigos).",
                "Con patrones atigrados naranjas.", "", "Estrías de agarre"));

            Register(new CardDefinition("TROMPO_FENIX", "Trompo Fénix", CardCategory.PhysicalTrompo, ClanType.Miticos, 6, 0, 0,
                "Resurrección: Elige un aliado derrotado de tu cementerio y devuélvelo a tu mano.",
                "Tonos cálidos rojizos. Levanta al jugador caído.", "", "Polímero de alta resistencia"));

            Register(new CardDefinition("TROMPO_DIAMANTE", "Trompo Diamante", CardCategory.PhysicalTrompo, ClanType.Fugitivos, 4, 0, 0,
                "Otorga a todos tus personajes Fugitivos +2 de Vida máxima permanente.",
                "Translúcido cristalino, inquebrantable.", "", "Policarbonato blindado"));

            Register(new CardDefinition("TROMPO_TURBO_KING", "Trompo Turbo King", CardCategory.PhysicalTrompo, ClanType.Fugitivos, 6, 0, 0,
                "Haz daño igual a la suma del costo de tus personajes Fugitivos en el campo.",
                "Coronado con detalles dorados para verdaderos líderes.", "", "Corona dorada"));

            Register(new CardDefinition("TROMPO_TURBO_FLASH", "Trompo Turbo Flash", CardCategory.PhysicalTrompo, ClanType.Miticos, 3, 0, 0,
                "Devuelve un personaje enemigo con costo 3 o menor a la mano de su dueño.",
                "Pintura ultrabrillante. Desaparece a la vista.", "", "Rodamiento cerámico"));

            Register(new CardDefinition("TROMPO_SPIDER", "Trompo Spider", CardCategory.PhysicalTrompo, ClanType.Fugitivos, 3, 0, 0,
                "Trampa: Juega boca abajo. El primer enemigo que ataque este turno queda Enredado.",
                "Diseño con telarañas grabadas en el cuerpo.", "", "Relieve táctil"));

            Register(new CardDefinition("TROMPO_TURBO_DRAGON", "Trompo Turbo Dragón", CardCategory.PhysicalTrompo, ClanType.Depredadores, 7, 0, 0,
                "Destruye todas las cartas de Arena activas y hace 3 de daño al líder rival.",
                "Edición especial con escamas laterales aerodinámicas.", "", "Escamas aerodinámicas"));
        }

        private static void RegisterArenas()
        {
            Register(new CardDefinition("ARENA_TEMPLO_MILENARIO", "Templo Milenario", CardCategory.Arena, ClanType.Milenarios, 4, 0, 0,
                "Al final del turno, cura 1 de Vida a todos los campeones Milenarios vivos.",
                "Antiguo altar de piedra rodeado de robles gigantescos."));

            Register(new CardDefinition("ARENA_RUINAS_DRACONIA", "Ruinas de Draconia", CardCategory.Arena, ClanType.Depredadores, 3, 0, 0,
                "Cada líder enemigo pierde 1 de Vida al inicio del turno si no tiene escudo.",
                "Castillo destruido con dragones de piedra y fuego morado."));

            Register(new CardDefinition("ARENA_VALLE_PRUEBAS", "Valle de Pruebas", CardCategory.Arena, ClanType.Milenarios, 2, 0, 0,
                "Los personajes de Costo 3 o menos ganan +1 de Vida máxima.",
                "Círculo de combate ancestral de los Milenarios."));

            Register(new CardDefinition("ARENA_NIDO_WYVERN", "Nido del Wyvern", CardCategory.Arena, ClanType.Depredadores, 5, 0, 0,
                "Los ataques de los personajes Depredadores no pueden ser contraatacados.",
                "Cima montañosa llena de criaturas agresivas."));

            Register(new CardDefinition("ARENA_CIUDADELA_PLATEADA", "Ciudadela Plateada", CardCategory.Arena, ClanType.Milenarios, 4, 0, 0,
                "Todo el daño Mágico que entra a la ciudadela se reduce en 2.",
                "Fortaleza mística forjada en plata líquida."));

            Register(new CardDefinition("ARENA_SANTUARIO_SOL", "Santuario del Sol", CardCategory.Arena, ClanType.Miticos, 3, 0, 0,
                "Genera 1 Energía extra cada turno para el jugador que controle la arena.",
                "Ruinas mayas iluminadas perennemente por el sol."));

            Register(new CardDefinition("ARENA_RED_VIUDA", "Red de la Viuda", CardCategory.Arena, ClanType.Fugitivos, 4, 0, 0,
                "Los personajes enemigos entran en juego Enredados (Agotados 1 turno).",
                "Bosque denso plagado de telas de araña mágicas."));

            Register(new CardDefinition("ARENA_LLANURAS_TIRANO", "Llanuras del Tirano", CardCategory.Arena, ClanType.Miticos, 6, 0, 0,
                "Aumenta el Ataque base de todos los personajes en el campo en +2.",
                "Tierra árida donde reinó Rex. Promueve la violencia bruta."));

            Register(new CardDefinition("ARENA_CAIDAS_KAMINO", "Caídas de Kamino", CardCategory.Arena, ClanType.Fugitivos, 3, 0, 0,
                "Una vez por turno, puedes mandar tu propia carta al Cementerio para robar otra.",
                "Cataratas místicas que funcionan como portales interdimensionales."));

            Register(new CardDefinition("ARENA_CIMIENTOS_CRISTAL", "Cimientos de Cristal", CardCategory.Arena, ClanType.Miticos, 5, 0, 0,
                "El primer personaje aliado que juegues cada turno cuesta 1 menos de Energía.",
                "Caverna subterránea brillante, origen del Cristal Cometa."));

            Register(new CardDefinition("ARENA_ARENAS_MOVEDIZAS", "Arenas Movedizas", CardCategory.Arena, ClanType.Fugitivos, 2, 0, 0,
                "Al final del turno, empuja a un campeón enemigo de vuelta a la mano de su dueño.",
                "Desierto impenetrable que rodea a los Fugitivos."));

            Register(new CardDefinition("ARENA_VOLCAN_SANGRIENTO", "Volcán Sangriento", CardCategory.Arena, ClanType.Depredadores, 6, 0, 0,
                "Inflige daño igual a la Fuerza de tu campo de batalla a un solo objetivo.",
                "Caldera activa lista para estallar; hogar de clanes letales."));

            Register(new CardDefinition("ARENA_TORREON_ESPECTRO", "Torreón de Espectro", CardCategory.Arena, ClanType.Miticos, 4, 0, 0,
                "Mientras esté activa, tu mano de Invocación está oculta para efectos rivales.",
                "Biblioteca infinita que flota entre realidades sombrías."));

            Register(new CardDefinition("ARENA_LABORATORIO_REY", "Laboratorio del Rey", CardCategory.Arena, ClanType.Fugitivos, 4, 0, 0,
                "Permite buscar y añadir una carta Trompo Físico directamente de tu mazo.",
                "Guarida científica llena de piezas de trompos y energía errática."));

            Register(new CardDefinition("ARENA_PISTA_CARRERAS", "La Pista de Carreras", CardCategory.Arena, ClanType.Milenarios, 2, 0, 0,
                "Todos los campeones con la palabra 'Turbo' ganan +2 de Ataque Inmediato.",
                "Desfiladero de piedra modificado para correr a velocidad sónica."));

            Register(new CardDefinition("ARENA_POZO_DEPREDADORES", "Pozo de los Depredadores", CardCategory.Arena, ClanType.Depredadores, 3, 0, 0,
                "El jugador con menos Vida puede sacrificar un campeón para ganar poder.",
                "Arena de combate clandestina en las cloacas."));

            Register(new CardDefinition("ARENA_CEMENTERIO_JUGUETES", "Cementerio de Juguetes", CardCategory.Arena, ClanType.Milenarios, 5, 0, 0,
                "Invoca un 'Espíritu Cometa' (1/1) cada vez que un aliado muera.",
                "Valle de trompos antiguos, oxidados y venerados."));

            Register(new CardDefinition("ARENA_CAMPAMENTO_PANTHER", "Campamento Panther", CardCategory.Arena, ClanType.Depredadores, 3, 0, 0,
                "Tus emboscadas no tienen costo de energía la primera vez.",
                "Asentamiento nómada escondido en la penumbra del bosque."));

            Register(new CardDefinition("ARENA_BOVEDA_COMETA", "Bóveda del Cometa", CardCategory.Arena, ClanType.Fugitivos, 6, 0, 0,
                "Ningún jugador puede modificar sus Puntos de Vida Base.",
                "Fortaleza de máxima seguridad donde se esconde el polvo mágico."));

            Register(new CardDefinition("ARENA_ARENA_CAMPEONES", "La Arena Campeones", CardCategory.Arena, ClanType.Neutral, 7, 0, 0,
                "Carta Épica: Todos los personajes recuperan la vida completa al entrar.",
                "El coliseo brillante y definitivo donde convergen los 4 clanes.", "", "", CardRarity.Epic));
        }
    }
}
