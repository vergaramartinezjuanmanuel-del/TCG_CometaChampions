using System;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Represents an active card deployed into an Arena on the battlefield.
    /// Tracks individual power modifications, ownership, and reveal state for simultaneous resolution.
    /// </summary>
    [Serializable]
    public class LaneCardInstance
    {
        public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
        public CardDefinition Card { get; set; }
        public bool OwnerIsPlayer { get; set; }
        public int LaneIndex { get; set; }
        public bool IsRevealed { get; set; } = true;
        public int PowerModifier { get; set; } = 0;
        public bool OnPlayExecuted { get; set; } = false;

        public int BasePower => Card?.Power ?? 0;
        public int CurrentPower => Math.Max(0, BasePower + PowerModifier);

        public LaneCardInstance() { }

        public LaneCardInstance(CardDefinition card, bool ownerIsPlayer, int laneIndex, bool isRevealed = true)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            OwnerIsPlayer = ownerIsPlayer;
            LaneIndex = laneIndex;
            IsRevealed = isRevealed;
            PowerModifier = 0;
            OnPlayExecuted = false;
        }

        public void ApplyPowerModifier(int delta)
        {
            PowerModifier += delta;
        }
    }
}
