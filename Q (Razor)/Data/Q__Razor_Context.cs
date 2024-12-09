using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Q__Razor_.Models;

namespace Q__Razor_.Data
{
    public class Q__Razor_Context : DbContext
    {
        public Q__Razor_Context(DbContextOptions<Q__Razor_Context> options)
            : base(options)
        {
        }
       
        public DbSet<TblGameState> TblGameState { get; set; }
        public DbSet<TblMoves> TblMoves { get; set; }
        public DbSet<Q__Razor_.Models.TblPlayers> TblPlayers { get; set; } = default!;

        public DbSet<Q__Razor_.Models.TblGames>? TblGames { get; set; }
    }
}
