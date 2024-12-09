using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Q__Razor_.Models;
using Q__Razor_.Data;
using System.Text.Json;

namespace ChessGameServer.Services
{
    public class GameService
    {
        private readonly Q__Razor_Context _context;

        public GameService(Q__Razor_Context context)
        {
            _context = context;
        }

        // Starts a new game for a player and initializes the board
        public async Task<TblGameState> StartGameAsync(TblGames game)
        {
            try
            {
                var TblGameState = new TblGameState
                {
                    GameId = game.GameId,
                    Board = InitializeBoardArray(),
                    ClientTurn = true,
                    IsNewGame = true   // Mark this as a new game
                };

                _context.TblGameState.Add(TblGameState);
                await _context.SaveChangesAsync();
                return TblGameState;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DbUpdateException: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }

                throw new Exception("An error occurred while saving the game state. See inner exception for details.", dbEx);
            }
        }


        // Processes a move by validating and updating the game state
        public async Task<TblGameState> MakeMoveAsync(int gameId, int fromX, int fromY, int toX, int toY)
        {
            try
            {
                Console.WriteLine($"Processing move for GameId: {gameId}, from ({fromX},{fromY}) to ({toX},{toY})");

                var currentGameState = await _context.TblGameState
                   .Where(gs => gs.GameId == gameId)
                   .OrderByDescending(gs => gs.GameStateId) // Get the latest state
                   .FirstOrDefaultAsync();


                if (currentGameState == null)
                {
                    Console.WriteLine($"GameState not found for GameId={gameId}");
                    throw new Exception("Game not found.");
                }

                Console.WriteLine($"GameState found: GameId={gameId}, ClientTurn={currentGameState.ClientTurn}");


                int[][] board = JsonSerializer.Deserialize<int[][]>(currentGameState.Board);
                Console.WriteLine($"Current board state: {currentGameState.Board}");

                int piece = board[fromY][fromX];
                int targetPiece = board[toY][toX];

                if (((currentGameState.ClientTurn == true) && (piece < 5)) || ((currentGameState.ClientTurn == false) && (piece > 4)))
                {
                    throw new Exception($"Invalid move: It's not the correct turn to move piece {piece}. CurrentTurn: {(currentGameState.ClientTurn ? "Client" : "Server")}");
                }
                // Perform validation
                if (!IsValidMovement(piece, fromX, fromY, toX, toY, board))
                {
                    throw new Exception($"Invalid move: Movement does not follow piece rules: piece:{piece}, fromX:{fromX}, fromY:{fromY}, toX:{toX}, toY:{toY}, board:{board}");
                }

                // Update the board after validation
                board[toY][toX] = piece;
                board[fromY][fromX] = -1;

                bool isChessMat = false;
                bool isChess=false;

                if (currentGameState.ClientTurn == true)
                    isChess = IsKingInCheck(8, board);
                else
                    isChess = IsKingInCheck(1, board);

                if (isChess == true) { 

                    if (currentGameState.ClientTurn == true)
                        isChessMat = IsKingInCheckmate(8, board);
                    else
                        isChessMat = IsKingInCheckmate(1, board);
               }



                string newBoardState = JsonSerializer.Serialize(board); // Serialize back to JSON

                var newGameState = new TblGameState
                {
                    GameId = gameId,
                    Board = newBoardState,
                    ClientTurn = !currentGameState.ClientTurn, // Switch turns
                    IsNewGame = false, // Mark as not a new game
                    IsChess = isChess,
                    IsChessMat = isChessMat
                };


                _context.TblGameState.Add(newGameState); // Add the new game state to the database
                await _context.SaveChangesAsync();

                Console.WriteLine($"New game state created with ID: {newGameState.GameStateId}");
                return newGameState; // Return the new game state


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MakeMoveAsync: {ex.Message}");
                throw; // Rethrow to allow the controller to catch and log the error
            }
        }



        // Retrieves the current game state by GameId
        public async Task<TblGameState> GetTblGameStateAsync(int gameId)
        {
            var TblGameStateBoard = await _context.TblGameState.Include(g => g.Board)
                                                     .FirstOrDefaultAsync(g => g.GameId == gameId);
            if (TblGameStateBoard == null)
            {
                throw new Exception("Game not found.");
            }
            return TblGameStateBoard;
        }

        // Initializes the board in JSON format for storage in the TblGameState
        private string InitializeBoardArray()
        {

            int[][] initialBoard = new int[][]
            {
                new int[] { 1, 3, 0, 2 },
                new int[] { 4, 4, 4, 4 },
                new int[] { -1, -1, -1, -1 },
                new int[] { -1, -1, -1, -1 },
                new int[] { -1, -1, -1, -1 },
                new int[] { -1, -1, -1, -1 },
                new int[] { 9, 9, 9, 9 },
                new int[] { 8, 7, 5, 6 }
            };
            return JsonSerializer.Serialize(initialBoard);
        }

        // Validates a move based on basic rules and game state
        private bool ValidateMove(TblGameState TblGameState, int fromX, int fromY, int toX, int toY)
        {

            int[][] board = JsonSerializer.Deserialize<int[][]>(TblGameState.Board);

            int piece = board[fromY][fromX];
            int targetPiece = board[toY][toX];

            if ((piece >= 5 && TblGameState.ClientTurn) || (piece <= 4 && !TblGameState.ClientTurn))
            {
                Console.WriteLine("Not player's turn");
                return false; // Not player's turn
            }
            if (targetPiece != -1 && ((piece <= 4 && targetPiece <= 4) || (piece >= 5 && targetPiece >= 5)))
            {
                Console.WriteLine("Cannot capture own piece");
                return false; // Cannot capture own piece
            }

           
            if ((piece <= 4 && targetPiece <= 4 && targetPiece >= 0) || (piece >= 5 && targetPiece >= 5))
            {
                Console.WriteLine($"Cannot capture own piece: Piece {piece} at ({fromX}, {fromY}) targeting {targetPiece} at ({toX}, {toY})");
                return false;
            }  

            //Checke whose turn it is
            if ((piece >= 5 && piece <= 9 && TblGameState.ClientTurn == false) || (piece <= 4 && piece >= 0 && TblGameState.ClientTurn == true))
            {
                Console.WriteLine("Not your turn!");
                return false;
               
            }
            return IsValidMovement(piece, fromX, fromY, toX, toY, board);
            
        }


        private bool IsValidMovement(int piece, int fromX, int fromY, int toX, int toY, int[][] board)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            int targetPiece = board[toY][toX];

            // Prevent capturing own pieces
            if ((piece <= 4 && targetPiece <= 4 && targetPiece >= 0) || (piece >= 5 && targetPiece >= 5))
            {
                Console.WriteLine($"Cannot capture own piece: Piece {piece} at ({fromX}, {fromY}) targeting {targetPiece} at ({toX}, {toY})");
                return false;
            }

            switch (piece)
            {
                case 0: // Green knight
                case 5: // Blue knight
                    return (dx == 2 && dy == 1) || (dx == 1 && dy == 2);

                case 1: // Green king
                case 8: // Blue king
                    return dx <= 1 && dy <= 1 && !IsPathObstructed(fromX, fromY, toX, toY, board);

                case 2: // Green rook
                case 6: // Blue rook
                    return (dx == 0 || dy == 0) && !IsPathObstructed(fromX, fromY, toX, toY, board);

                case 3: // Green bishop
                case 7: // Blue bishop
                    return dx == dy && !IsPathObstructed(fromX, fromY, toX, toY, board);

                case 4: // Green pawn
                case 9: // Blue pawn
                    return ValidatePawnMove(piece, fromX, fromY, toX, toY, board);

                default:
                    Console.WriteLine($"Unknown piece type: {piece}");
                    return false; // Invalid piece type
            }
        }


        private bool ValidatePawnMove(int piece, int fromX, int fromY, int toX, int toY, int[][] board)
        {
            int targetPiece = board[toY][toX];
            int dx = Math.Abs(toX - fromX);
            int dy = toY - fromY;

            if (piece == 4) // Green pawn moving forward (downwards)
            {
                if (targetPiece == -1 && dx == 0 && dy == 1) return true; // One square forward
                if (fromY == 1 && targetPiece == -1 && dx == 0 && dy == 2 && board[fromY + 1][fromX] == -1) return true;
                if (targetPiece >= 5 && dx == 1 && dy == 1) return true; // Diagonal capture
                if (dx == 1 && dy == 0 && targetPiece == -1) return true;
            }
            else if (piece == 9) // Blue pawn moving forward (upwards)
            {
                if (targetPiece == -1 && dx == 0 && dy == -1) return true; // One square forward
                if (targetPiece == -1 && fromY == 6 && dx == 0 && dy == -2 && board[fromY - 1][fromX] == -1) return true; // First double move
                if (targetPiece <= 4 && dx == 1 && dy == -1) return true; // Diagonal capture
                if (dx == 1 && dy == 0 && targetPiece == -1) return true;
            }

            Console.WriteLine("pawn invalid move");
            return false;
        }


        private bool IsPathObstructed(int fromX, int fromY, int toX, int toY, int[][] board)
        {
            int dx = Math.Sign(toX - fromX);
            int dy = Math.Sign(toY - fromY);

            int x = fromX + dx, y = fromY + dy;
            while (x != toX || y != toY)
            {
                if (board[y][x] != -1) return true; // Square is occupied
                x += dx;
                y += dy;
            }

            return false; // No obstruction
        }

        public async Task<List<TblMoves>> GetAllValidMovesAsync(int gameId)
        {
            var gameState = await _context.TblGameState
                .OrderByDescending(gs => gs.GameStateId) // Get the latest state
                .FirstOrDefaultAsync(gs => gs.GameId == gameId);

            if (gameState == null)
            {
                Console.WriteLine("Game state not found.");
                throw new Exception("Game not found.");
            }

            int[][] board = JsonSerializer.Deserialize<int[][]>(gameState.Board);
            bool clientTurn = gameState.ClientTurn;


            var validMoves = new List<TblMoves>();


            if (clientTurn == true)
            {
                validMoves = GetAllUserValidMoves(gameId, board);
           
            }
            else
            {
                validMoves = GetAllServerValidMoves(gameId, board);
         
            }

            
            return validMoves;

            

        }

        private List<TblMoves> GetAllServerValidMoves(int gameId, int[][]? board)
        {
            var validMoves = new List<TblMoves>();
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    int piece = board[row][col];

                    if (piece >= 0 && piece <= 4)
                    {
                        Console.WriteLine($"Checking moves for piece {piece} at ({col}, {row}).");

                        for (int targetRow = 0; targetRow < board.Length; targetRow++)
                        {
                            for (int targetCol = 0; targetCol < board[targetRow].Length; targetCol++)
                            {
                                if (IsValidMovement(piece, col, row, targetCol, targetRow, board))
                                {
                                    validMoves.Add(new TblMoves
                                    {
                                        GameId = gameId,
                                        FromX = col,
                                        FromY = row,
                                        ToX = targetCol,
                                        ToY = targetRow,
                                        MoveTimestamp = DateTime.Now
                                    });

                                    Console.WriteLine($"Valid move added: ({col}, {row}) -> ({targetCol}, {targetRow}).");
                                }
                            }
                        }
                    }
                }
            }
            return validMoves;
        }

        private List<TblMoves> GetAllUserValidMoves(int gameId, int[][] board)
        {
            var validMoves = new List<TblMoves>();
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    int piece = board[row][col];

                    if (piece >= 5 && piece <= 9) 
                    {
                        Console.WriteLine($"Checking moves for piece {piece} at ({col}, {row}).");

                        for (int targetRow = 0; targetRow < board.Length; targetRow++)
                        {
                            for (int targetCol = 0; targetCol < board[targetRow].Length; targetCol++)
                            {
                                if (IsValidMovement(piece, col, row, targetCol, targetRow, board))
                                {
                                    validMoves.Add(new TblMoves
                                    {
                                        GameId = gameId,
                                        FromX = col,
                                        FromY = row,
                                        ToX = targetCol,
                                        ToY = targetRow,
                                        MoveTimestamp = DateTime.Now
                                    });

                                    Console.WriteLine($"Valid move added: ({col}, {row}) -> ({targetCol}, {targetRow}).");
                                }
                            }
                        }
                    }
                }
            }
            return validMoves;
        }



        // Updates the board in the game state after a valid move
        private void UpdateBoard(TblGameState TblGameState, int fromX, int fromY, int toX, int toY)
        {
            int[][] board = JsonSerializer.Deserialize<int[][]>(TblGameState.Board);
            int piece = board[fromY][fromX];
            board[toY][toX] = piece;
            board[fromY][fromX] = -1;

            // Serialize the updated board back to JSON and save in the TblGameState
            TblGameState.Board = JsonSerializer.Serialize(board);
        }

        private bool IsKingInCheck(int kingPiece, int[][] board)
        {
            // Find the king's position
            int kingX = -1, kingY = -1;
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    if (board[row][col] == kingPiece)
                    {
                        kingX = col;
                        kingY = row;
                        break;
                    }
                }
                if (kingX != -1) break;
            }

            if (kingX == -1 || kingY == -1)
            {
                Console.WriteLine($"Error: King piece {kingPiece} not found on the board!");
                return false; // King not found
            }

            // Check if any opposing piece can move to the king's position
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    int piece = board[row][col];
                    if ((kingPiece <= 4 && piece >= 5) || (kingPiece >= 5 && piece <= 4)) // Opposing piece
                    {
                        if (IsValidMovement(piece, col, row, kingX, kingY, board))
                        {
                            return true; // King is in check
                        }
                    }
                }
            }

            return false; // King is safe
        }

        private bool IsKingInCheckmate(int kingPiece, int[][] board)
        {
            // Check if the king is in check
            if (!IsKingInCheck(kingPiece, board)) return false;

            // Try all possible moves for the king
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    if (board[row][col] == kingPiece)
                    {
                        for (int targetRow = 0; targetRow < board.Length; targetRow++)
                        {
                            for (int targetCol = 0; targetCol < board[targetRow].Length; targetCol++)
                            {
                                if (IsValidMovement(kingPiece, col, row, targetCol, targetRow, board))
                                {
                                    // Simulate the move
                                    int temp = board[targetRow][targetCol];
                                    board[targetRow][targetCol] = kingPiece;
                                    board[row][col] = -1;

                                    // Check if the king is still in check
                                    bool stillInCheck = IsKingInCheck(kingPiece, board);

                                    // Undo the move
                                    board[row][col] = kingPiece;
                                    board[targetRow][targetCol] = temp;

                                    if (!stillInCheck)
                                    {
                                        return false; // King can escape check
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Check if any allied piece can block or capture the threatening piece
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    int piece = board[row][col];
                    if ((kingPiece <= 4 && piece <= 4 && piece >= 0) || (kingPiece >= 5 && piece >= 5))
                    {
                        for (int targetRow = 0; targetRow < board.Length; targetRow++)
                        {
                            for (int targetCol = 0; targetCol < board[targetRow].Length; targetCol++)
                            {
                                if (IsValidMovement(piece, col, row, targetCol, targetRow, board))
                                {
                                    // Simulate the move
                                    int temp = board[targetRow][targetCol];
                                    board[targetRow][targetCol] = piece;
                                    board[row][col] = -1;

                                    // Check if the king is still in check
                                    bool stillInCheck = IsKingInCheck(kingPiece, board);

                                    // Undo the move
                                    board[row][col] = piece;
                                    board[targetRow][targetCol] = temp;

                                    if (!stillInCheck)
                                    {
                                        return false; // Check can be blocked or attacker captured
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true; // King is in checkmate
        }

        public async Task<TblGames> UpdateGameDurationAsync(int gameId, int duration)
        {
            // Find the game by ID
            var game = await _context.TblGames.FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game == null)
            {
                throw new Exception("Game not found.");
            }
            Console.WriteLine($"tring to update game duration. game found with ID:{game.GameId}");

            // Update the duration

            game.GameDuration = duration;

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return the updated game
            return game;
        }


    }

}

