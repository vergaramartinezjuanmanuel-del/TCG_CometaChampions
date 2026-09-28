using System;
using System.Collections.Generic;
using System.Linq;

namespace CometaChampions.Core.Models
{
    public enum BattleWinner
    {
        None,
        Player,
        Ai,
        Tie
    }

    public class TurnPlayAction
    {
        public CardDefinition Card { get; set; }
        public int LaneIndex { get; set; }

        public TurnPlayAction(CardDefinition card, int laneIndex)
        {
            Card = card;
            LaneIndex = laneIndex;
        }
    }

    /// <summary>
    /// Pure C# authoritative state representation for a 3-Arena simultaneous match.
    /// Inspired by MARVEL SNAP & Pokémon TCG Pocket.
    /// </summary>
    [Serializable]
    public class ArenaBattleMatchState
    {
        public const int MaxLanes = 3;
        public const int MaxCardsPerLane = 4;
        public const int MaxHandSize = 7;
        public const int DefaultStartingHandSize = 4;
        public const int TotalTurns = 6;

        public int CurrentTurn { get; set; } = 1;
        public int MaxTurn { get; set; } = TotalTurns;

        public int PlayerEnergy { get; set; } = 1;
        public int PlayerMaxEnergy { get; set; } = 1;

        public int AiEnergy { get; set; } = 1;
        public int AiMaxEnergy { get; set; } = 1;

        public bool IsMulliganPhase { get; set; } = true;
        public bool PlayerMulliganDone { get; set; } = false;
        public bool AiMulliganDone { get; set; } = false;

        public CardDefinition[] Arenas { get; set; } = new CardDefinition[MaxLanes];

        public List<LaneCardInstance>[] PlayerLaneCards { get; set; } = new List<LaneCardInstance>[MaxLanes]
        {
            new(), new(), new()
        };

        public List<LaneCardInstance>[] AiLaneCards { get; set; } = new List<LaneCardInstance>[MaxLanes]
        {
            new(), new(), new()
        };

        public List<CardDefinition>[] PlayerLanes { get; set; } = new List<CardDefinition>[MaxLanes]
        {
            new(), new(), new()
        };

        public List<CardDefinition>[] AiLanes { get; set; } = new List<CardDefinition>[MaxLanes]
        {
            new(), new(), new()
        };

        public List<TurnPlayAction> PlayerTurnPlays { get; set; } = new();
        public List<TurnPlayAction> AiTurnPlays { get; set; } = new();

        public List<CardDefinition> PlayerDeck { get; set; } = new();
        public List<CardDefinition> PlayerHand { get; set; } = new();

        public List<CardDefinition> AiDeck { get; set; } = new();
        public List<CardDefinition> AiHand { get; set; } = new();

        public int? SelectedHandIndex { get; set; } = null;

        public bool IsGameOver { get; set; } = false;
        public BattleWinner Winner { get; set; } = BattleWinner.None;

        public int PlayerLanesWon { get; set; } = 0;
        public int AiLanesWon { get; set; } = 0;
        public int PlayerTotalPower { get; set; } = 0;
        public int AiTotalPower { get; set; } = 0;

        public List<string> ResolutionLog { get; set; } = new();

        public void SyncLegacyLanes()
        {
            for (int i = 0; i < MaxLanes; i++)
            {
                PlayerLanes[i] = PlayerLaneCards[i].Select(c => c.Card).ToList();
                AiLanes[i] = AiLaneCards[i].Select(c => c.Card).ToList();
            }
        }
    }
}
