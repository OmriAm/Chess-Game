using ClientChessBoard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientChessBoard
{
    public partial class UserAuthenticationForm : Form
    {
        private static HttpClient client = new HttpClient();
        private const string PATH = "http://localhost:55430/";
        private Player pl = null;
        private int? selectedGameId = null;
        public UserAuthenticationForm()
        {
            InitializeComponent();
        }


        private async void GetPlayerById(int playerId , Boolean IsView)
        {
            try
            {
                // Attempt to fetch player data
                pl = await GetPlayerAsync($"api/TblPlayers/{playerId}");

                // Check if player data is null
                if (pl == null)
                {
                    MessageBox.Show("Player not found. Please check the ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Open new form with player data if player exists
                Form1 form1 = new Form1(pl, IsView, selectedGameId);
                form1.Show();
                this.Hide();
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Network error: Unable to reach the server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid ID format. Please enter a valid integer ID.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // General catch for unexpected exceptions
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            async Task<Player> GetPlayerAsync(string path)
            {
                Player player = null;
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    player = await response.Content.ReadAsAsync<Player>();
                }
                return player;
            }

        }


        //Start new game button
        private async void button1_Click(object sender, EventArgs e)
        {
            int playerId = int.Parse(textBoxID.Text);
            GetPlayerById(playerId, false);
        }

        private void UserAuthenticationForm_Load(object sender, EventArgs e)
        {
            // Set up HTTP client
            client.BaseAddress = new Uri(PATH);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            comboBox.Enabled = false;
        }
        
        //Restore Game
        private async void RestoreButton_Click(object sender, EventArgs e)
        {
            comboBox.Enabled = true;
            // Fetch the list of games using the API call
            var games = await GetAllPlayerGamesAsync();

            // Set the ComboBox DataSource
            comboBox.DataSource = games;

            comboBox.DisplayMember = "GameId"; // Display the game ID
            comboBox.ValueMember = "GameId";   // Use the ID as the value
        }
        private async Task<List<Game>> GetAllPlayerGamesAsync()
        {
            // Parse the player ID from the text box
            int playerId = int.Parse(textBoxID.Text);

            try
            {
                // Make the API call
                var response = await client.GetAsync($"api/TblGames/allGames/{playerId}");

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the JSON response into a list of Game objects
                    var games = await response.Content.ReadFromJsonAsync<List<Game>>();

                    foreach (var game in games)
                    {
                        Console.WriteLine($"Game ID: {game.GameId}");
                    }

                    // Return the list of games
                    return games ?? new List<Game>();
                }
                else
                {
                    MessageBox.Show("Failed to retrieve valid games from the server.");
                    return new List<Game>();
                }
            }
            catch (Exception ex)
            {
                // Show any error that occurs
                MessageBox.Show($"An error occurred: {ex.Message}");
                return new List<Game>();
            }
        }
        
        //view Game
        private async void ViewGameButton_Click(object sender, EventArgs e)
        {
            int playerId = int.Parse(textBoxID.Text);
            selectedGameId = (int)comboBox.SelectedValue;
            Console.WriteLine(selectedGameId + "is my game ID");
            GetPlayerById(playerId, true);
            
        }
    }
}