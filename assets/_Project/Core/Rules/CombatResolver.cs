using System;
using System.Collections.Generic;
using CometaChampions.Core.Models;

namespace CometaChampions.Core.Rules
{
    public class CombatResult
    {
        public bool IsSuccessful { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public int AttackerDamageDealt { get; set; }
        public int DefenderDamageDealt { get; set; }
        public bool AttackerDied { get; set; }
        public bool DefenderDied { get; set; }
        public bool AttackedPlayerDirectly { get; set; }
    }

    /// <summary>
    /// Resolves combat encounters following Cometa Champions TCG rules.
    /// </summary>
    public static class CombatResolver
    {
        /// <summary>
        /// Resolves an attack from an attacking creature to a defending creature.
        /// Requirements:
        /// - Attacker must not have summoning sickness and must not be exhausted.
        /// - Defender must be exhausted (exposed to attack).
        /// - Both deal simultaneous damage.
        /// - Clan advantage (+1 ⚔️) is applied to attacker.
        /// </summary>
        public static CombatResult ResolveCreatureCombat(
            CardInstance attacker,
            CardInstance defender,
            PlayerState attackerPlayer,
            PlayerState defenderPlayer)
        {
            if (attacker == null || defender == null)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker or defender is null." };
            }

            if (!attacker.Definition.IsCreature || !defender.Definition.IsCreature)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Both combatants must be creatures (Champions or Beasts)." };
            }

            if (attacker.HasSummoningSickness)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker has summoning sickness and cannot attack this turn." };
            }

            if (attacker.IsExhausted)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker is already exhausted." };
            }

            if (!defender.IsExhausted)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Defender is not exhausted (only exhausted/exposed targets can be challenged)." };
            }

            // Exhaust the attacker for declaring combat
            attacker.Exhaust();

            // Calculate damage dealt
            int attackerDamage = ClanAdvantage.CalculateAttackDamage(
                attacker.CurrentAttack,
                attacker.Definition.Clan,
                defender.Definition.Clan);

            int defenderDamage = defender.CurrentAttack;

            // Apply simultaneous damage
            defender.TakeDamage(attackerDamage);
            attacker.TakeDamage(defenderDamage);

            bool defenderDied = !defender.IsAlive;
            bool attackerDied = !attacker.IsAlive;

            if (defenderDied)
            {
                defenderPlayer.Board.Remove(defender);
                defenderPlayer.Graveyard.Add(defender);
            }

            if (attackerDied)
            {
                attackerPlayer.Board.Remove(attacker);
                attackerPlayer.Graveyard.Add(attacker);
            }

            return new CombatResult
            {
                IsSuccessful = true,
                AttackerDamageDealt = attackerDamage,
                DefenderDamageDealt = defenderDamage,
                AttackerDied = attackerDied,
                DefenderDied = defenderDied,
                AttackedPlayerDirectly = false
            };
        }

        /// <summary>
        /// Resolves a direct attack onto the opposing player's life total.
        /// </summary>
        public static CombatResult ResolveDirectAttack(
            CardInstance attacker,
            PlayerState defenderPlayer)
        {
            if (attacker == null || defenderPlayer == null)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker or defender player is null." };
            }

            if (!attacker.Definition.IsCreature)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Only creatures can attack." };
            }

            if (attacker.HasSummoningSickness)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker has summoning sickness." };
            }

            if (attacker.IsExhausted)
            {
                return new CombatResult { IsSuccessful = false, FailureReason = "Attacker is already exhausted." };
            }

            attacker.Exhaust();
            int damage = attacker.CurrentAttack;
            defenderPlayer.TakeDamage(damage);

            return new CombatResult
            {
                IsSuccessful = true,
                AttackerDamageDealt = damage,
                DefenderDamageDealt = 0,
                AttackerDied = false,
                DefenderDied = false,
                AttackedPlayerDirectly = true
            };
        }
    }
}
