using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientChessBoard.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public DateTime GameStartTime { get; set; }
        public int GameDuration { get; set; }
    }
}
