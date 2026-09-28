using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Live state of a player in a Cometa Champions match.
    /// Manages player life (20 HP), deck, hand, board, graveyard, and energy inkwell.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public const int StartingHealth = 20;

        public string PlayerId { get; set; }
        public int Health { get; set; } = StartingHealth;
        public bool IsAlive => Health > 0;

        public List<CardInstance> Deck { get; } = new();
        public List<CardInstance> Hand { get; } = new();
        public List<CardInstance> Board { get; } = new();
        public List<CardInstance> Graveyard { get; } = new();
        public List<CardInstance> EnergyZone { get; } = new();

        public bool HasConvertedEnergyThisTurn { get; set; }

        public int TotalEnergy => EnergyZone.Count;
        public int AvailableEnergy => EnergyZone.Count(e => !e.IsExhausted);

        public PlayerState(string playerId)
        {
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        }

        public CardInstance? DrawCard()
        {
            if (Deck.Count == 0) return null;
            var card = Deck[0];
            Deck.RemoveAt(0);
            Hand.Add(card);
            return card;
        }

        /// <summary>
        /// Commits a card from hand face-down into the Energy Zone (Lorcana-style inkwell).
        /// Only 1 card may be converted per turn.
        /// </summary>
        public bool ConvertHandCardToEnergy(string instanceId)
        {
            if (HasConvertedEnergyThisTurn) return false;

            var card = Hand.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null) return false;

            Hand.Remove(card);
            card.IsFaceDown = true;
            card.IsExhausted = false;
            EnergyZone.Add(card);
            HasConvertedEnergyThisTurn = true;
            return true;
        }

        public bool CanAfford(int cost) => AvailableEnergy >= cost;

        public bool SpendEnergy(int amount)
        {
            if (amount <= 0) return true;
            if (!CanAfford(amount)) return false;

            int spent = 0;
            foreach (var energy in EnergyZone.Where(e => !e.IsExhausted))
            {
                energy.Exhaust();
                spent++;
                if (spent == amount) break;
            }
            return true;
        }

        public void ReadyAll()
        {
            foreach (var creature in Board) creature.Ready();
            foreach (var energy in EnergyZone) energy.Ready();
            HasConvertedEnergyThisTurn = false;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            Health = Math.Max(0, Health - damage);
        }
    }
}
