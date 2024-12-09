using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Q__Razor_.Models
{
    public class TblMoves
    {
        [Key]
        public int MoveId { get; set; }

        [ForeignKey("GameId")]
        public int GameId { get; set; }

        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public DateTime MoveTimestamp { get; set; } = DateTime.Now;

       // public TblGameState GameState { get; set; } // Navigation property
    }
}
