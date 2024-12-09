using ClientChessBoard.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http.Json;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace ClientChessBoard

{
    public partial class Form1 : Form
    {
        private Boolean IsView = false;
        private ClientDBEntities db = new ClientDBEntities();
        private readonly string connectionString;
        private static HttpClient client = new HttpClient();
        private const string PATH = "http://localhost:55430/";

        private const int BoardRows = 8;
        private const int BoardCols = 4;
        private Rectangle[,] boardSquares; // Matrix of Rectangles for the chessboard
        private Bitmap[] pieceImages; // Bitmap array for chess pieces
        private int[,] board;
        private int squareSize = 60; // Size of each square
        private Timer animationTimer; // Timer for animations
        private Point selectedPiece = new Point(-1, -1); // Track selected piece
       // private Point movingPiecePosition;

        private int animationFrames = 20;
        private int currentFrame = 0;

        private ComboBox timeComboBox;
        private int _currentGameId; // Current game ID for server interaction

        private bool clientTurn = true;
        private Timer countdownTimer;
        private int timeLeft;
        private Label countdownLabel;
        private Label countdownComboBoxLable;

        // Pen drawing mode
        private bool penMode = false; // Track whether pen mode is active
        private Bitmap penBitmap; // Bitmap to hold the pen drawing
        private Graphics penGraphics; // Graphics object to draw on the bitmap
        private Point lastPoint; // Last point for drawing lines

        private Point blinkingSquare = new Point(-1, -1); // Tracks the blinking square
        private bool isBlinking = false; // Toggle state for blinking
        private Timer blinkTimer; // Timer for blinking effect

        private Timer gameTimer;
        private int totalGameTimeInSeconds;

        private Player _player;
        private int? gameId;

        public Form1(Player player, Boolean isView, int? gameId)
        {
            connectionString = ConfigurationManager.ConnectionStrings["ClientDBEntities"].ConnectionString;

            _player = player;
            IsView = isView;
            this.gameId = gameId;
            Console.WriteLine($"my game ID: {this.gameId}");
            InitializeComponent();
            
            DisplayPlayerInfo();
            // initial full size form
            this.Size = new Size(squareSize * 2 * BoardCols + 25, squareSize * BoardRows + 70);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            

            this.DoubleBuffered = true;

            InitializeBoard();
            LoadPieceImages();
            InitializeAnimation();
            
            //if is new game
            if (isView == false)
            {
                // Initialize the pen bitmap and graphics
                penBitmap = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                penGraphics = Graphics.FromImage(penBitmap);
                penGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                buttonPen.Click += TogglePenMode;
                buttonClean.Click += buttonClean_Click;


                // Add ComboBox
                timeComboBox = new ComboBox();
                timeComboBox.Items.AddRange(new object[] { 10, 15, 20 });
                timeComboBox.SelectedItem = 20; // Set default to 20
                timeComboBox.Location = new Point(270, this.ClientSize.Height - 100); // Adjust location as needed
                timeComboBox.DropDownStyle = ComboBoxStyle.DropDownList; // Makes it non-editable
                timeComboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged; // Event for selection change
                this.Controls.Add(timeComboBox);


                this.MouseDown += OnMouseDownAsync;

                // Initialize timeLeft with the selected item value
                timeLeft = (int)timeComboBox.SelectedItem;

                // Initialize Countdown Timer
                countdownTimer = new Timer();
                countdownTimer.Interval = 1000; // 1 second interval
                countdownTimer.Tick += CountdownTimer_Tick;

                // Add countdown label
                countdownLabel = new Label();
                countdownLabel.Location = new Point(270, this.ClientSize.Height - 490);
                countdownLabel.AutoSize = true;
                countdownLabel.Font = new Font("Arial", 14);
                this.Controls.Add(countdownLabel);


                // Add countdown ComboBox label
                countdownComboBoxLable = new Label();
                countdownComboBoxLable.Location = new Point(270, this.ClientSize.Height - 120);
                countdownComboBoxLable.AutoSize = true;
                countdownComboBoxLable.Font = new Font("Arial", 10);
                countdownComboBoxLable.Text = "Select playtime duration:";
                this.Controls.Add(countdownComboBoxLable);

                // Initialize countdown
                ResetCountdown();
            }
            
        }

        private void DisplayPlayerInfo()
        {
            labelName.Text = _player.Name;
            labelId.Text = _player.ID.ToString();
            labelCountry.Text = _player.Country.ToString();
            labelPhone.Text = _player.Phone.ToString();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Set up HTTP client
            client.BaseAddress = new Uri("http://localhost:55430/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            TblBindingSource.DataSource = db.GameState.ToList();
            if (IsView == false)
                StartNewGame(_player.ID);
            else
                StartGameView(this.gameId);
           
        }

        private async void StartGameView(int? gameId)
        {
            if (gameId == null)
            {
                MessageBox.Show("Invalid game ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Retrieve all game states from the GameState table for the given gameId
                var gameStates = db.GameState
                    .Where(gs => gs.GameId == gameId)
                    .OrderBy(gs => gs.GameStateId) 
                    .ToList();
                Console.WriteLine($"Found {gameStates.Count} game states for gameId: {gameId}");

                if (gameStates == null || gameStates.Count == 0)
                {
                    MessageBox.Show("No game states found for the provided game ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Loop through each game state and display it on the chessboard UI
                foreach (var gameState in gameStates)
                {
                    // Update the chessboard with the current game state
                    UpdateBoardFromServer(gameState);
                    if (gameState.IsChess == true)
                    {
                        IsChessTrue(gameState);
                    }
                    // Wait for 3 seconds before showing the next game state
                    await Task.Delay(3000);
                }

                MessageBox.Show("Finished displaying all game states.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void IsChessTrue(GameState gameState)
        {

            int kingPiece = gameState.ClientTurn ? 1 : 8;
            Console.WriteLine($"kingPiece:{kingPiece}");
            int[][] board = JsonConvert.DeserializeObject<int[][]>(gameState.Board);
            for (int row = 0; row < board.Length; row++)
            {
                for (int col = 0; col < board[row].Length; col++)
                {
                    if (board[row][col] == kingPiece)
                    {
                        StartBlinkingEffect(col, row);
                        break;
                    }
                }
            }
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            timeLeft = (int)comboBox.SelectedItem; // Update timeLeft with the selected value
        }

        // Reset and start countdown whenever clientTurn changes
        private void ResetCountdown()
        {
            timeLeft = (int)timeComboBox.SelectedItem;
            countdownLabel.Text = $"Time left: {timeLeft} seconds";
            countdownTimer.Start();
        }
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                timeLeft--;
                countdownLabel.Text = $"Time left: {timeLeft} seconds";
            }
            else
            {
                countdownTimer.Stop();
                HandleTimeup(clientTurn ? "Player" : "Server");
            }
        }

        // Method to handle game-over logic
        private void HandleTimeup(string losingSide)
        {
            // Show game-over message
            string message = $"{losingSide} has lost the game due to time running out. Would you like to start a new game?";
            var result = MessageBox.Show(message, "Game Over", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            // If user chooses to start a new game, reset the game
            if (result == DialogResult.Yes)
            {
                ResetGame();
            }
            else
            {
                // Close or disable the game if they choose not to continue
                this.Close(); 
            }
        }

        private void HandleGameOver(string losingSide)
        {
            // Show game-over message
            string message = "Would you like to start a new game?";
            var result = MessageBox.Show(message, "Game Over", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            // If user chooses to start a new game, reset the game
            if (result == DialogResult.Yes)
            {
                ResetGame();
            }
            else
            {
                // Close or disable the game if they choose not to continue
                this.Close(); 
            }
        }

        private void ResetGame()
        {
            StartNewGame(_player.ID);
            // Reset the board
            InitializeBoard();

            // Reset turn state and countdown timer
            clientTurn = true;
            if(IsView==false)
                ResetCountdown();

            // Redraw the board
            Invalidate();
        }

      
        private async void OnClientTurnChange()
        {
            clientTurn = !clientTurn;
            if (IsView == false)
                ResetCountdown();

            // Check if it's the server's turn (green pieces), and if so, make a random move
            if (clientTurn)
            {
                Console.WriteLine("It's the client's turn.");
                
            }
            else
            {
                Console.WriteLine("It's the server's turn.");
                await PerformServerMoveAsync(); // Fetch and make a server move
            }
        }


        // Toggle pen mode on or off
        private void TogglePenMode(object sender, EventArgs e)
        {
            penMode = !penMode;
            if (penMode)
            {
                this.Text = "Pen Mode: ON";
                buttonClean.Enabled = true;
            }
            else
            {
                this.Text = "Pen Mode: OFF";
                buttonClean.Enabled = false;
            }
        }


        // Initialize the chessboard
        private void InitializeBoard()
        {
            boardSquares = new Rectangle[BoardRows, BoardCols];
            board = new int[BoardRows, BoardCols]; // 2D array for pieces

            int startY = this.ClientSize.Height - (BoardRows * squareSize);

            for (int row = 0; row < BoardRows; row++)
            {
                for (int col = 0; col < BoardCols; col++)
                {
                  
                    boardSquares[row, col] = new Rectangle(col * squareSize, startY + (row * squareSize), squareSize, squareSize);

                    // Initialize the pieces on the board for the first two rows and last two rows
                    if (row == 1)
                    {
                        // Row 1 is Green pawns
                        board[row, col] = 4; // Green pawn (index 4 in pieceImages array)
                    }
                    else if (row == 0)
                    {
                        // Row 0 is Green major pieces (king, rook, knight, bishop)
                        if (col == 0) board[row, col] = 1; // Green king
                        else if (col == 1) board[row, col] = 3; // Green bishop
                        else if (col == 2) board[row, col] = 0; // Green knight
                        else if (col == 3) board[row, col] = 2; // Green rook
                    }
                    else if (row == 6)
                    {
                        // Row 6 is Blue pawns
                        board[row, col] = 9; // Blue pawn (index 9 in pieceImages array)
                    }
                    else if (row == 7)
                    {
                        // Row 7 is Blue major pieces (king, rook, knight, bishop)
                        if (col == 0) board[row, col] = 8; // Blue king
                        else if (col == 1) board[row, col] = 7; // Blue bishop
                        else if (col == 2) board[row, col] = 5; // Blue knight
                        else if (col == 3) board[row, col] = 6; // Blue rook
                    }
                    else
                    {
                        // Initialize the middle squares as empty
                        board[row, col] = -1; // -1 means no piece is present in this square
                    }
                }
            }
        }

        // Load piece images into Bitmap array
        private void LoadPieceImages()
        {
            pieceImages = new Bitmap[10];

            //Greens
            pieceImages[0] = Properties.Resources.KnightGreen;
            pieceImages[0].MakeTransparent(Color.White);

            pieceImages[1] = Properties.Resources.KingGreen;
            pieceImages[1].MakeTransparent(Color.White);

            pieceImages[2] = Properties.Resources.RookGreen;
            pieceImages[2].MakeTransparent(Color.White);

            pieceImages[3] = Properties.Resources.BishopGreen;
            pieceImages[3].MakeTransparent(Color.White);

            pieceImages[4] = Properties.Resources.PawnGreen;
            pieceImages[4].MakeTransparent(Color.White);

            //Blues
            pieceImages[5] = Properties.Resources.KnightBlue;
            pieceImages[5].MakeTransparent(Color.White);

            pieceImages[6] = Properties.Resources.RookBlue;
            pieceImages[6].MakeTransparent(Color.White);

            pieceImages[7] = Properties.Resources.BishopBlue;
            pieceImages[7].MakeTransparent(Color.White);

            pieceImages[8] = Properties.Resources.KingBlue;
            pieceImages[8].MakeTransparent(Color.White);

            pieceImages[9] = Properties.Resources.PawnBlue;
            pieceImages[9].MakeTransparent(Color.White);


        }
        private async void StartNewGame(int playerId)
        {
            var response = await client.PostAsJsonAsync("api/TblGames/start", playerId);

            if (response.IsSuccessStatusCode)
            {
                var gameState = await response.Content.ReadFromJsonAsync<GameState>();
                _currentGameId = gameState.GameId;
                Console.WriteLine($"Game started with ID: {_currentGameId}");
                UpdateBoardFromServer(gameState);
                InitializeGameTimer();

                if (gameState.ClientTurn)
                {
                    Console.WriteLine("It's the client's turn to play.");
                    
                    ResetCountdown(); // Start countdown for the client's turn
                }
                else
                {
                    Console.WriteLine("It's the server's turn to play.");
                    await PerformServerMoveAsync(); // Fetch and make a server move
                }

            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to start a new game. Server Error: {errorContent}");
                MessageBox.Show($"Failed to start a new game. Server Error: {errorContent}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeGameTimer()
        {
            // Reset total game time
            totalGameTimeInSeconds = 0;

            // Set up the timer
            gameTimer = new Timer();
            gameTimer.Interval = 1000; // 1 second
            gameTimer.Tick += (sender, e) => { totalGameTimeInSeconds++; };
            gameTimer.Start();

            Console.WriteLine("Game timer started.");
        }



        private void UpdateBoardFromServer(GameState gameState)
        {
            board = Newtonsoft.Json.JsonConvert.DeserializeObject<int[,]>(gameState.Board);
            Invalidate(); // Redraw board

            if ((!gameState.IsNewGame) && (IsView==false))
            {
                // Only call OnClientTurnChange if the game is not new
                OnClientTurnChange();
            }
            else
            {
                Console.WriteLine("This is a new game or view mode. No turn change needed.");
            }
        }

        // Initialize animation for moving pieces
        private void InitializeAnimation()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 50; // Timer for controlling animation speed
            animationTimer.Tick += AnimatePieceMovement;
        }


        // Paint the chessboard, pieces, and pen drawing
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            DrawChessBoard(g);
            DrawPieces(g);
            if(IsView==false)
                g.DrawImage(penBitmap, 0, 0); // Draw the pen drawing bitmap
        }


        // Draw the chessboard with alternating colors
        private void DrawChessBoard(Graphics g)
        {
            for (int row = 0; row < BoardRows; row++)
            {
                for (int col = 0; col < BoardCols; col++)
                {
                    Brush brush;

                    // Apply blinking effect for the blinking square
                    if (blinkingSquare.X == col && blinkingSquare.Y == row && isBlinking)
                    {
                        brush = Brushes.Red; // Blinking red square
                    }
                    else
                    {
                        // Default square color
                        brush = (row + col) % 2 == 0 ? Brushes.White : Brushes.Black;
                    }

                    g.FillRectangle(brush, boardSquares[row, col]);
                }
            }
        }


        // Draw the chess pieces
        private void DrawPieces(Graphics g)
        {
            // Loop through the board and draw every piece at its respective position
            for (int row = 0; row < BoardRows; row++)
            {
                for (int col = 0; col < BoardCols; col++)
                {
                    int pieceIndex = board[row, col];
                    if (pieceIndex >= 0 && pieceIndex < pieceImages.Length && pieceImages[pieceIndex] != null)
                    {
                        g.DrawImage(pieceImages[pieceIndex], boardSquares[row, col]);
                    }
                }
            }

        }


        // Mouse down handler for selecting/moving pieces or starting drawing
        private async void OnMouseDownAsync(object sender, MouseEventArgs e)
        {
            

            if (penMode)
            {
                // Start a new drawing path
                lastPoint = e.Location;
            }
            else
            {
                Point clickedSquare = GetSquareFromMousePosition(e.Location);
                Console.WriteLine($"Clicked square: {clickedSquare.X}, {clickedSquare.Y }");

                if (clickedSquare.X == -1 || clickedSquare.Y == -1)
                {
                    Console.WriteLine("Click was outside the board.");
                    return; // Invalid square; ignore the click
                }

                if (selectedPiece.X == -1 && selectedPiece.Y == -1)
                {
                    selectedPiece = clickedSquare;
                    Console.WriteLine($"Piece selected at {selectedPiece.X}, {selectedPiece.Y}");
                }
                else
                {
                    Console.WriteLine($"Attempting to move from {selectedPiece} to {clickedSquare}");
                    await SendMoveToServer(selectedPiece, clickedSquare);
                    selectedPiece = new Point(-1, -1); // Deselect after move

                }

            }
        }
        // Sends a move to the server and updates the board
        private async Task SendMoveToServer(Point from, Point to)
        {
            var moveRequest = new
            {
                GameId = _currentGameId,
                FromX = from.X,
                FromY = from.Y,
                ToX = to.X,
                ToY = to.Y
            };

            try
            {
                var response = await client.PostAsJsonAsync("api/TblGames/move", moveRequest);
                if (response.IsSuccessStatusCode)
                {
                    var updatedGameState = await response.Content.ReadFromJsonAsync<GameState>();
                    AddGameState(updatedGameState);
                    UpdateBoardFromServer(updatedGameState);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Move request failed: {errorContent}");
                    MessageBox.Show($"Move request failed: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SendMoveToServer: {ex.Message}");
                MessageBox.Show($"Exception in SendMoveToServer: {ex.Message}");
            }
        }

        private void AddGameState(GameState newGameState)
        {
            try
            {
                // Add the new GameState to the DbContext
                db.GameState.Add(newGameState);
                Console.WriteLine($"Entity State: {db.Entry(newGameState).State}");
                // Save changes to the database
                db.SaveChanges();
                Console.WriteLine("GameState added to the database successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddGameState: {ex.Message}");
            }
        }
        private async Task<List<Moves>> GetAllValidMovesForServerAsync()
        {
            var response = await client.GetAsync($"api/TblGames/validMoves/{_currentGameId}");
            if (response.IsSuccessStatusCode)
            {
                var validMoves = await response.Content.ReadFromJsonAsync<List<Moves>>();
                return validMoves ?? new List<Moves>();
            }
            else
            {
                MessageBox.Show("Failed to retrieve valid moves from the server.");
                return new List<Moves>();
            }
        }


        private async Task PerformServerMoveAsync()
        {
            var validMoves = await GetAllValidMovesForServerAsync();

            if (validMoves.Count > 0)
            {
                Console.WriteLine($"Valid moves available for server: {validMoves.Count}");
                Random random = new Random();
                var move = validMoves[random.Next(validMoves.Count)];

                Console.WriteLine($"Server selected move: ({move.FromX}, {move.FromY}) -> ({move.ToX}, {move.ToY})");

                // Validate that the piece being moved belongs to the server (green: 0-4)
                int piece = board[move.FromY, move.FromX];
                if (piece < 0 || piece > 4)
                {
                    Console.WriteLine($"Invalid piece moved by server: {piece}");
                    return;
                }

                // Execute the move and request the updated game state from the server
                try
                {
                    var moveRequest = new
                    {
                        GameId = _currentGameId,
                        FromX = move.FromX,
                        FromY = move.FromY,
                        ToX = move.ToX,
                        ToY = move.ToY
                    };

                    var response = await client.PostAsJsonAsync("api/TblGames/move", moveRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var updatedGameState = await response.Content.ReadFromJsonAsync<GameState>();
                        UpdateBoardFromServer(updatedGameState); // Update the board state and turn
                        
                        Console.WriteLine("Server move completed successfully.");
                        AddGameState(updatedGameState);

                        if (updatedGameState.IsChessMat == true) {
                            Console.WriteLine("Chess Mate is True");
                            string message = updatedGameState.ClientTurn ? "user" : "server";
                            MessageBox.Show($"Game over! {message} wins!");
                            countdownTimer.Stop();
                            gameTimer.Stop();

                            UpdateGameDuration(updatedGameState.GameId, totalGameTimeInSeconds);
                            HandleGameOver(clientTurn ? "Player" : "Server");
                        }
                        else if (updatedGameState.IsChess == true)
                        {
                            Console.WriteLine("Chess is True");
                            IsChessTrue(updatedGameState);
                        }

                        if (IsKingCaptured(1) || IsKingCaptured(8)) // 1 = Green King, 8 = Blue King
                        {
                            string message = updatedGameState.ClientTurn ? "user" : "server";
                            MessageBox.Show($"Game over! {message} wins!");
                            Console.WriteLine($"game duration1: {totalGameTimeInSeconds}");
                            countdownTimer.Stop();
                            gameTimer.Stop();
                            Console.WriteLine($"game duration: {totalGameTimeInSeconds}");
                            UpdateGameDuration(updatedGameState.GameId, totalGameTimeInSeconds);

                            HandleGameOver(clientTurn ? "Player" : "Server");
                        }

                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Failed to execute server move: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while performing server move: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No valid moves available for the server.");
            }
        }
        private bool IsKingCaptured(int kingPiece)
        {
            foreach (var piece in board)
            {
                if (piece == kingPiece)
                {
                    return false; // King is still on the board
                }
            }
            return true; // King is captured
        }

        private void StartBlinkingEffect(int kingX, int kingY)
        {
            // Set the blinking square position
            blinkingSquare = new Point(kingX, kingY);

            // Initialize the timer if not already created
            if (blinkTimer == null)
            {
                blinkTimer = new Timer { Interval = 500 }; // 500ms for blinking
                blinkTimer.Tick += (sender, args) =>
                {
                    isBlinking = !isBlinking; // Toggle blinking state
                    Invalidate(boardSquares[blinkingSquare.Y, blinkingSquare.X]);
                };
            }

            blinkTimer.Start();

            // Stop blinking after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                blinkTimer.Stop();
                blinkTimer.Dispose();
                blinkTimer = null;
                blinkingSquare = new Point(-1, -1); // Reset blinking state
                Invalidate(); // Redraw the board to clear blinking
            });
        }


        private Control GetSquareUIElement(int x, int y)
        {
            string squareName = $"Square_{x}_{y}";
            return Controls.Find(squareName, true).FirstOrDefault();
        }


        // Mouse move handler to draw when pen mode is enabled
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (penMode && e.Button == MouseButtons.Left)
            {
                // Draw line to the current mouse position on the bitmap
                penGraphics.DrawLine(Pens.Red, lastPoint, e.Location);
                lastPoint = e.Location;
                Invalidate(); // Redraw the board to update the bitmap
            }
        }


        // Convert mouse coordinates to chessboard squares
        private Point GetSquareFromMousePosition(Point mousePos)
        {
            // Check if the mouse click is within the bounds of the chessboard
            Rectangle boardBounds = boardSquares[0, 0];
            boardBounds.Width = BoardCols * squareSize;
            boardBounds.Height = BoardRows * squareSize;

            // Adjust position dynamically if the chessboard moves
            if (!boardBounds.Contains(mousePos))
            {
                return new Point(-1, -1); // Click is outside the chessboard
            }

            // Calculate the clicked square
            int col = (mousePos.X - boardBounds.X) / squareSize;
            int row = (mousePos.Y - boardBounds.Y) / squareSize;

            return new Point(col, row);
        }


        private void buttonClean_Click(object sender, EventArgs e)
        {
            // Clear the penBitmap by filling it with a transparent background or any other color
            penGraphics.Clear(Color.Transparent); // You can also use Color.White or any other background color

            // Refresh the form to reflect the changes (clear the drawing)
            Invalidate();
        }


        // Timer event for animating piece movement
        private void AnimatePieceMovement(object sender, EventArgs e)
        {
            currentFrame++;
            if (currentFrame <= animationFrames)
            {
                Invalidate(); // Redraw the board
            }
            else
            {
                animationTimer.Stop();
                selectedPiece = new Point(-1, -1); // Reset selected piece
            }
        }

        private async void UpdateGameDuration(int gameId, int durationInSeconds)
        {

            try
            {
                var response = await client.PutAsJsonAsync($"api/TblGames/update-duration/{gameId}", durationInSeconds);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Game duration updated successfully.");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to update game duration: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateGameDuration: {ex.Message}");
            }
        }


    }
}
