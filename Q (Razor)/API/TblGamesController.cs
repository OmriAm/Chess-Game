using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessGameServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q__Razor_.Data;
using Q__Razor_.Models;



namespace Q__Razor_.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TblGamesController : ControllerBase
    {
        private readonly Q__Razor_Context _context;

        private readonly GameService _gameService;

        public TblGamesController(Q__Razor_Context context, GameService gameService)
        {
            _context = context;
            _gameService = gameService; // Injected game logic service
        }

     //   public TblGamesController(Q__Razor_Context context)
      //  {
      //      _context = context;
       // }

        // GET: api/TblGames
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblGames>>> GetTblGames()
        {
          if (_context.TblGames == null)
          {
              return NotFound();
          }
            return await _context.TblGames.ToListAsync();
        }

        // GET: api/allGames/{playerId}
        [HttpGet("allGames/{playerId}")]
        public async Task<ActionResult<IEnumerable<TblGames>>> GetAllPlayerGamesAsync(int playerId)
        {
            // Ensure _context is not null (null-check for DbContext)
            if (_context.TblGames == null)
            {
                return NotFound("Games table not found in the database.");
            }

            try
            {
                // Query games where playerId matches
                var games = await _context.TblGames
                                          .Where(game => game.PlayerId == playerId) // Filter by player ID
                                          .ToListAsync();

                // Check if any games were found
                if (games == null || games.Count == 0)
                {
                    return NotFound($"No games found for player ID: {playerId}");
                }

                // Return the list of games
                return Ok(games);
            }
            catch (Exception ex)
            {
                // Return a 500 error if something unexpected happens
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // GET: api/TblGames/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TblGames>> GetTblGames(int id)
        {
          if (_context.TblGames == null)
          {
              return NotFound();
          }
            var tblGames = await _context.TblGames.FindAsync(id);

            if (tblGames == null)
            {
                return NotFound();
            }

            return tblGames;
        }

        // PUT: api/TblGames/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTblGames(int id, TblGames tblGames)
        {
            if (id != tblGames.GameId)
            {
                return BadRequest();
            }

            _context.Entry(tblGames).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TblGamesExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TblGames
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TblGames>> PostTblGames(TblGames tblGames)
        {
          if (_context.TblGames == null)
          {
              return Problem("Entity set 'Q__Razor_Context.TblGames'  is null.");
          }
            _context.TblGames.Add(tblGames);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTblGames", new { id = tblGames.GameId }, tblGames);
        }

        // DELETE: api/TblGames/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTblGames(int id)
        {
            if (_context.TblGames == null)
            {
                return NotFound();
            }
            var tblGames = await _context.TblGames.FindAsync(id);
            if (tblGames == null)
            {
                return NotFound();
            }

            _context.TblGames.Remove(tblGames);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TblGamesExists(int id)
        {
            return (_context.TblGames?.Any(e => e.GameId == id)).GetValueOrDefault();
        }

        [HttpPost("start")]
        public async Task<ActionResult<TblGameState>> StartGame([FromBody] int playerId)
        {
            try
            {
                // Log the incoming request
                Console.WriteLine($"Received request to start a new game for PlayerID: {playerId}");

                // Create a new game with the provided PlayerID
                var newGame = new TblGames
                {
                    PlayerId = playerId,
                };

                // Add the new game to the database
                _context.TblGames.Add(newGame);
                await _context.SaveChangesAsync(); // This will assign a unique GameId to newGame

                Console.WriteLine($"New game created with ID: {newGame.GameId}");

                // Start the game using the generated GameId
                var gameState = await _gameService.StartGameAsync(newGame);

                return gameState;
            }
            catch (Exception ex)
            {
                // Log the full error
                Console.WriteLine($"Error starting new game: {ex.Message} \n {ex.StackTrace}");

                // Return a JSON error response to the client with a detailed message
                return StatusCode(500, new { error = ex.Message });
            }
        }



        [HttpPost("move")]
        public async Task<ActionResult<TblGameState>> MakeMove([FromBody] TblMoves move)
        {
            try
            {
                // Verify that the GameId exists in TblGames
                bool gameExists = await _context.TblGames.AnyAsync(g => g.GameId == move.GameId);
                Console.WriteLine($"GameId : {move.GameId},{ gameExists} ");
                if (!gameExists)
                {
                    Console.WriteLine($"GameId {move.GameId} not found in TblGames.");
                    return NotFound(new { Message = $"Game with ID {move.GameId} not found." });
                }

                // Proceed with making the move if the GameId exists
                var gameState = await _gameService.MakeMoveAsync(move.GameId, move.FromX, move.FromY, move.ToX, move.ToY);

                Console.WriteLine("Move processed successfully.");
                return Ok(gameState); // Return success response with the updated game state
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Move processing failed: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return BadRequest(new { Message = ex.Message }); // Return error response with the exception message
            }
        }




        [HttpGet("state/{gameId}")]
        public async Task<ActionResult<TblGameState>> GetGameState(int gameId)
        {
            var gameState = await _gameService.GetTblGameStateAsync(gameId);
            return Ok(gameState);
        }

        [HttpGet("validMoves/{gameId}")]
        public async Task<ActionResult<List<TblMoves>>> GetAllValidMoves(int gameId)
        {
            try
            {

                var validMoves = await _gameService.GetAllValidMovesAsync(gameId);
                return Ok(validMoves);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching valid moves: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update-duration/{gameId}")]
        public async Task<ActionResult<TblGames>> UpdateGameDuration(int gameId, [FromBody] int duration)
        {
            try
            {
                if (gameId <= 0 || duration < 0)
                {
                    return BadRequest(new { Message = "Invalid GameId or Duration." });
                }
              

                // Call the service to update the game duration
                var updatedGame = await _gameService.UpdateGameDurationAsync(gameId, duration);

                if (updatedGame == null)
                {
                    return NotFound(new { Message = "Game not found." });
                }

                return Ok(updatedGame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating game duration: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }







    }
}
