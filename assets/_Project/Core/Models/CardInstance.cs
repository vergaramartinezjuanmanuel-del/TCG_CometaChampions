using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Represents a dynamic, mutable instance of a card in a match or player inventory.
    /// Tracks current combat stats, damage counters, exhausted status, and foil progression.
    /// </summary>
    [Serializable]
    public class CardInstance
    {
        public string InstanceId { get; private set; }
        public CardDefinition Definition { get; private set; }
        public int CurrentHealth { get; set; }
        public int AttackModifier { get; set; }
        public bool IsExhausted { get; set; }
        public bool HasSummoningSickness { get; set; }
        public bool IsFaceDown { get; set; }
        public int FoilLevel { get; set; } = 1; // 1 = Standard, 2 = Foil, 3 = Gold

        public int CurrentAttack => Math.Max(0, Definition.BaseAttack + AttackModifier);
        public bool IsAlive => !Definition.IsCreature || CurrentHealth > 0;

        public CardInstance(CardDefinition definition, string? instanceId = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            InstanceId = instanceId ?? Guid.NewGuid().ToString("N");
            CurrentHealth = definition.BaseHealth;
            AttackModifier = 0;
            IsExhausted = false;
            HasSummoningSickness = definition.IsCreature;
            IsFaceDown = false;
        }

        public void TakeDamage(int damage)
        {
            if (!Definition.IsCreature || damage <= 0) return;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }

        public void Heal(int amount)
        {
            if (!Definition.IsCreature || amount <= 0) return;
            CurrentHealth = Math.Min(Definition.BaseHealth, CurrentHealth + amount);
        }

        public void Ready()
        {
            IsExhausted = false;
            HasSummoningSickness = false;
        }

        public void Exhaust()
        {
            IsExhausted = true;
        }
    }
}
