using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Rules
{
    /// <summary>
    /// Pure C# rule resolver for "El Círculo Cometa" advantage wheel.
    /// Milenarios (🌳) -> Fugitivos (⛓️) -> Depredadores (🪓) -> Míticos (🐉) -> Milenarios (🌳)
    /// </summary>
    public static class ClanAdvantage
    {
        public const int AdvantageBonusDamage = 1;

        /// <summary>
        /// Determines if the attacking clan holds a tactical advantage over the defending clan.
        /// </summary>
        public static bool HasAdvantage(ClanType attacker, ClanType defender)
        {
            if (attacker == ClanType.Neutral || defender == ClanType.Neutral ||
                attacker == ClanType.None || defender == ClanType.None)
            {
                return false;
            }

            return (attacker == ClanType.Milenarios && defender == ClanType.Fugitivos) ||
                   (attacker == ClanType.Fugitivos && defender == ClanType.Depredadores) ||
                   (attacker == ClanType.Depredadores && defender == ClanType.Miticos) ||
                   (attacker == ClanType.Miticos && defender == ClanType.Milenarios);
        }

        /// <summary>
        /// Returns the clan that the specified clan has an offensive advantage over.
        /// </summary>
        public static ClanType GetAdvantageAgainst(ClanType clan)
        {
            return clan switch
            {
                ClanType.Milenarios => ClanType.Fugitivos,
                ClanType.Fugitivos => ClanType.Depredadores,
                ClanType.Depredadores => ClanType.Miticos,
                ClanType.Miticos => ClanType.Milenarios,
                _ => ClanType.None
            };
        }

        /// <summary>
        /// Returns the clan that has an offensive advantage over the specified clan.
        /// </summary>
        public static ClanType GetWeakAgainst(ClanType clan)
        {
            return clan switch
            {
                ClanType.Milenarios => ClanType.Miticos,
                ClanType.Fugitivos => ClanType.Milenarios,
                ClanType.Depredadores => ClanType.Fugitivos,
                ClanType.Miticos => ClanType.Depredadores,
                _ => ClanType.None
            };
        }

        /// <summary>
        /// Calculates total attack damage inflicted by attacker onto defender,
        /// including the +1 Clan Advantage bonus if applicable.
        /// </summary>
        public static int CalculateAttackDamage(int baseAttack, ClanType attackerClan, ClanType defenderClan)
        {
            int damage = baseAttack;
            if (HasAdvantage(attackerClan, defenderClan))
            {
                damage += AdvantageBonusDamage;
            }
            return damage;
        }
    }
}
