using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Q__Razor_.Data;
using Q__Razor_.Models;

namespace Q__Razor_.Pages.Products
{
    public class IndexModel : PageModel
    {
        public IList<TblPlayers> TblPlayers { get; set; } = default!;

        private readonly Q__Razor_.Data.Q__Razor_Context _context;

        public IndexModel(Q__Razor_.Data.Q__Razor_Context context)
        {
            _context = context;
            PlayersNames = _context.TblPlayers.Select(p => p.Name).OrderBy(n => n).Distinct().ToList();
        }

        public async Task OnGetAsync()
        {
            if (_context.TblPlayers != null)
            {
                // Fetch all player names, normalize to lowercase, and sort alphabetically
                PlayersNames = _context.TblPlayers
                 .Select(p => p.Name.Trim().ToLower()) // Normalize case and trim spaces
                 .Distinct()
                 .OrderBy(name => name)
                 .ToList();


                TblPlayers = await _context.TblPlayers.ToListAsync();
                ViewData["ShowTable"] = "PlayersByID"; // Default to first table
            }
        }


        public IActionResult OnPostPlayersByID()
        {

            TblPlayers = _context.TblPlayers
                .OrderBy(p => p.ID)
                .ToList();

            ViewData["ShowTable"] = "PlayersByID"; // Indicate first table should be shown
            return Page();
        }
        public IActionResult OnPostPlayersByName()
        {
            TblPlayers = _context.TblPlayers
                .OrderBy(p => p.Name)          // Sort by Name
                .ToList();

            ViewData["ShowTable"] = "PlayersByID"; // Indicate first table should be shown
            return Page();
        }

        public IActionResult OnPostByNamesWithLastGame()
        {
            // Fetch player names and their last game date
            var playersWithLastGames = _context.TblPlayers
                .OrderBy(p => p.Name) // Sort players by Name
                .Select(player => new
                {
                    player.Name,
                    // Fetch the last game date for each player
                    LastGameDate = _context.TblGames
                        .Where(game => game.PlayerId == player.ID) // Match games for the current player
                        .OrderByDescending(game => game.GameStartTime) // Order by GameStartTime descending
                         .Select(game => game.GameStartTime.ToString("yyyy-MM-dd HH:mm"))  // Select the date
                        .FirstOrDefault() // Take the most recent date
                })
                .ToList();

            // Pass the result to ViewData for rendering
            ViewData["PlayersWithLastGames"] = playersWithLastGames;
            ViewData["ShowTable"] = "ByNamesWithLastGame"; // Indicate third table should be shown

            return Page();
        }

        public IActionResult OnPostGamesById()
        {

            TblGames = _context.TblGames
                .OrderBy(p => p.GameId)
                .ToList();

            ViewData["ShowTable"] = "GamesByID"; // Indicate first table should be shown
            return Page();
        }

        public IActionResult OnPostPlayerByCountry()
        {
            // Fetch all players from the database
            var players = _context.TblPlayers
                .ToList(); // Load all players into memory

            // Process data in memory: group by country, then select the first player by name
            var playersByCountry = players
                .GroupBy(player => player.Country) // Group players by country
                .Select(group => group.OrderBy(player => player.Name).FirstOrDefault()) // Take the first player in each group
                .Select(player => new
                {
                    player.ID,
                    player.Name,
                    player.Country
                }) // Select only the required fields
                .ToList();

            // Pass the data to ViewData
            ViewData["PlayersByCountry"] = playersByCountry;
            ViewData["ShowTable"] = "PlayersByCountry";

            return Page();
        }


        public IActionResult OnPost()
        {
            string selectedPlayer = Request.Form["selectedPlayer"]; // Get the selected player name
            Console.WriteLine($"Selected Player from dropdown: {selectedPlayer}"); // Debug log

            if (!string.IsNullOrEmpty(selectedPlayer))
            {
                // Fetch players matching the selected name (case-insensitive and trimmed)
                var matchingPlayers = _context.TblPlayers
                    .Where(p => p.Name.Trim().ToLower() == selectedPlayer.Trim().ToLower())
                    .ToList();


                foreach (var player in matchingPlayers)
                {
                    Console.WriteLine($"Player ID: {player.ID}, Name: {player.Name}");
                }

                // If no players were matched, log and exit
                if (!matchingPlayers.Any())
                {
                    Console.WriteLine("No matching players found in the database.");
                    ViewData["SelectedPlayerGames"] = null;
                }
                else
                {
                    // Fetch all games played by the selected player
                    var selectedPlayerGames = matchingPlayers
                        .Join(
                            _context.TblGames,
                            player => player.ID,
                            game => game.PlayerId,
                            (player, game) => new
                            {
                                game.GameId,
                                game.GameDuration,
                                game.GameStartTime
                            }
                        )
                        .ToList();


                    foreach (var game in selectedPlayerGames)
                    {
                        Console.WriteLine($"Game ID: {game.GameId}, Duration: {game.GameDuration}, StartTime: {game.GameStartTime}");
                    }

                    // Pass the games and selected player to ViewData
                    ViewData["SelectedPlayer"] = selectedPlayer;
                    ViewData["SelectedPlayerGames"] = selectedPlayerGames;


                }
            }


            // Repopulate the combobox
            PlayersNames = _context.TblPlayers
                .Select(p => p.Name.Trim().ToLower()) // Normalize case and trim spaces
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            return Page();
        }

        public IActionResult OnPostCountByPlayer()
        {
            // Fetch player names and their last game date
            var playersWithGamesCount = _context.TblPlayers
              .OrderBy(p => p.Name) // Sort players by Name
              .Select(player => new
              {
                  player.Name,
                  // Count the number of games for each player
                  CountOfGames = _context.TblGames
                      .Where(game => game.PlayerId == player.ID) // Match games for the current player
                      .Count() // Count the games
              })
              .ToList();

            // Pass the result to ViewData for rendering
            ViewData["playersWithGamesCount"] = playersWithGamesCount;
            ViewData["ShowTable"] = "ByPlayersWithGamesCount"; // Indicate third table should be shown

            return Page();
        }

        public IActionResult OnPostPlayersByGamesCount()
        {
            // Group players by the number of games they played
            var playersGroupedByGames = _context.TblPlayers
                .Select(player => new
                {
                    player.Name,
                    GameCount = _context.TblGames
                        .Where(game => game.PlayerId == player.ID)
                        .Count() // Count the number of games for the player
                })
                .GroupBy(p => p.GameCount) // Group by GameCount
                .Select(group => new
                {
                    GameCount = group.Key, // The number of games played
                    PlayerNames = string.Join(", ", group.Select(p => p.Name)) // Combine player names into a string
                })
                .OrderByDescending(g => g.GameCount) // Order by game count descending
                .ToList();

            // Pass the result to ViewData for rendering
            ViewData["PlayersGroupedByGames"] = playersGroupedByGames;
            ViewData["ShowTable"] = "PlayersGroupedByGames"; // Indicate this table should be shown

            return Page();
        }
        public IActionResult OnPostPlayersByCountry()
        {
            // Group players by country
            var playersGroupedByCountry = _context.TblPlayers
                .GroupBy(player => player.Country) // Group by Country
                .Select(group => new
                {
                    Country = group.Key, // The country name
                    PlayerNames = string.Join(", ", group.Select(p => p.Name)) // Combine player names into a string
                })
                .OrderBy(g => g.Country) // Order by country name
                .ToList();

            // Pass the result to ViewData for rendering
            ViewData["PlayersGroupedByCountry"] = playersGroupedByCountry;
            ViewData["ShowTable"] = "PlayersGroupedByCountry"; // Indicate this table should be shown

            return Page();
        }

        public List<string?> PlayersNames { get; set; }
        public List<TblGames> TblGames { get; private set; }
    }
}
