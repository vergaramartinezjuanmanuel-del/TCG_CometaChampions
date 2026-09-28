using System;
using CometaChampions.Core.Enums;

namespace CometaChampions.Core.Models
{
    /// <summary>
    /// Represents the full authoritative state of a 1v1 match.
    /// Can be easily serialized to JSON and sent over network WebSockets or stored in replays.
    /// </summary>
    [Serializable]
    public class MatchState
    {
        public string MatchId { get; set; } = Guid.NewGuid().ToString("N");
        public PlayerState Player1 { get; set; }
        public PlayerState Player2 { get; set; }
        public string ActivePlayerId { get; set; }
        public TurnPhase CurrentPhase { get; set; } = TurnPhase.Preparation;
        public int TurnNumber { get; set; } = 1;
        public CardInstance? ActiveArena { get; set; }
        public bool IsGameOver { get; set; }
        public string? WinnerPlayerId { get; set; }

        public MatchState(PlayerState player1, PlayerState player2)
        {
            Player1 = player1 ?? throw new ArgumentNullException(nameof(player1));
            Player2 = player2 ?? throw new ArgumentNullException(nameof(player2));
            ActivePlayerId = player1.PlayerId;
        }

        public PlayerState GetActivePlayer() => ActivePlayerId == Player1.PlayerId ? Player1 : Player2;
        public PlayerState GetInactivePlayer() => ActivePlayerId == Player1.PlayerId ? Player2 : Player1;

        public void CheckWinCondition()
        {
            if (!Player1.IsAlive && !Player2.IsAlive)
            {
                IsGameOver = true;
                WinnerPlayerId = null; // Draw
            }
            else if (!Player1.IsAlive)
            {
                IsGameOver = true;
                WinnerPlayerId = Player2.PlayerId;
            }
            else if (!Player2.IsAlive)
            {
                IsGameOver = true;
                WinnerPlayerId = Player1.PlayerId;
            }
        }
    }
}
