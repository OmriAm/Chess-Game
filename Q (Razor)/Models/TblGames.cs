using System.ComponentModel.DataAnnotations;
namespace Q__Razor_.Models
{
    public class TblGames
    {
        [Key]
        public int GameId { get; set; }

        public int GameDuration { get; set; } = 0;
        public DateTime GameStartTime { get; set; } = DateTime.Now;
        public int PlayerId { get; set; }
        
    }
}
