using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Q__Razor_.Models
{
    public class TblGameState
    {

        [Key]
        public int GameStateId { get; set; }

        [ForeignKey("GameId")]
        public int GameId { get; internal set; }

        public string Board { get; set; } // Store as JSON string or another format
        public bool ClientTurn { get; set; }

        public bool IsNewGame { get; set; } // Flag to indicate if the game is new

        public bool IsChessMat { get; set; }
        public bool IsChess { get; set; }

    }
}
