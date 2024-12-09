using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientChessBoard.Models
{
    internal class Moves
    {
        public int MoveId { get; set; }
        public int GameId { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public DateTime MoveTimestamp { get; set; }

    }
}
