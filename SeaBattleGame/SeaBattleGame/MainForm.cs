using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeaBattleGame
{
    public partial class MainForm : Form
    {
        private Game game;
        private NetworkManager networkManager;
        private bool isHost = false;
        private int cellSize = 35;

        private DataGridView playerGrid;
        private DataGridView enemyGrid;
        private Button btnConnect;
        private Button btnWait;
        private Button btnAutoPlace;
        private Button btnStartGame;
        private TextBox txtIP;
        private TextBox txtPort;
        private Label lblStatus;
        private Label lblPlayer;
        private Label lblEnemy;

        public MainForm()
        {
            this.ClientSize = new Size(800, 600);
            this.Text = "Морской Бой";
            this.StartPosition = FormStartPosition.CenterScreen;

            game = new Game();
            networkManager = new NetworkManager();
            networkManager.OnMessageReceived += OnNetworkMessageReceived;
            networkManager.OnConnected += OnConnected;

            SetupUI();
            UpdateUI();
        }

        private void SetupUI()
        {
            // Labels
            lblPlayer = new Label { Text = "Ваше поле:", Location = new Point(20, 20), AutoSize = true };
            lblEnemy = new Label { Text = "Поле противника:", Location = new Point(420, 20), AutoSize = true };
            lblStatus = new Label { Text = "Расставьте корабли", Location = new Point(20, 500), AutoSize = true, Font = new Font("Arial", 12) };

            playerGrid = CreateGrid();
            playerGrid.Location = new Point(20, 50);
            playerGrid.CellClick += PlayerGrid_CellClick;

            enemyGrid = CreateGrid();
            enemyGrid.Location = new Point(420, 50);
            enemyGrid.CellClick += EnemyGrid_CellClick;

            txtIP = new TextBox { Text = "127.0.0.1", Location = new Point(20, 450), Width = 100 };
            txtPort = new TextBox { Text = "12345", Location = new Point(130, 450), Width = 60 };

            btnConnect = new Button { Text = "Подключиться", Location = new Point(200, 450), Width = 100 };
            btnConnect.Click += BtnConnect_Click;

            btnWait = new Button { Text = "Ожидать подключения", Location = new Point(310, 450), Width = 150 };
            btnWait.Click += BtnWait_Click;

            btnAutoPlace = new Button { Text = "Авторасстановка", Location = new Point(20, 480), Width = 120 };
            btnAutoPlace.Click += BtnAutoPlace_Click;

            btnStartGame = new Button { Text = "Начать игру", Location = new Point(150, 480), Width = 100 };
            btnStartGame.Click += BtnStartGame_Click;
            btnStartGame.Enabled = false;

            Controls.AddRange(new Control[] { lblPlayer, lblEnemy, lblStatus, playerGrid, enemyGrid,
                                            txtIP, txtPort, btnConnect, btnWait, btnAutoPlace, btnStartGame });
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView();

            grid.Width = cellSize * 10 + 20;
            grid.Height = cellSize * 10 + 20;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;
            grid.ScrollBars = ScrollBars.None;
            grid.BackgroundColor = Color.LightBlue;
            grid.BorderStyle = BorderStyle.FixedSingle;

            grid.Columns.Clear();

            for (int i = 0; i < 10; i++)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    Width = cellSize,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            grid.RowCount = 10;
            for (int i = 0; i < 10; i++)
            {
                grid.Rows[i].Height = cellSize;
            }

            return grid;
        }

        private void UpdateUI()
        {
            DrawBoard(game.PlayerBoard, playerGrid, true);
            DrawBoard(game.EnemyBoard, enemyGrid, false);

            switch (game.CurrentState)
            {
                case GameState.Setup:
                    lblStatus.Text = "Расставьте корабли";
                    enemyGrid.Enabled = false;
                    break;
                case GameState.PlayerTurn:
                    lblStatus.Text = "Ваш ход! Стреляйте!";
                    enemyGrid.Enabled = true;
                    break;
                case GameState.EnemyTurn:
                    lblStatus.Text = "Ход противника...";
                    enemyGrid.Enabled = false;
                    break;
                case GameState.GameOver:
                    lblStatus.Text = $"Игра окончена! Победитель: {(game.Winner == "Player" ? "Вы" : "Противник")}";
                    enemyGrid.Enabled = false;
                    ShowRestartDialog();
                    break;
            }

            btnStartGame.Enabled = networkManager.IsConnected && game.CurrentState == GameState.Setup;
        }

        private void DrawBoard(GameBoard board, DataGridView grid, bool isOwnBoard)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (i < grid.RowCount && j < grid.ColumnCount)
                    {
                        var cell = grid.Rows[i].Cells[j];
                        cell.Style.BackColor = GetColorForCell(board.Grid[i, j], isOwnBoard);
                        cell.Style.SelectionBackColor = cell.Style.BackColor;
                    }
                }
            }
        }

        private Color GetColorForCell(CellState state, bool isOwnBoard)
        {
            switch (state)
            {
                case CellState.Empty: return Color.LightBlue;
                case CellState.Ship: return isOwnBoard ? Color.Gray : Color.LightBlue;
                case CellState.Hit: return Color.Red;
                case CellState.Miss: return Color.White;
                case CellState.Sunk: return Color.DarkRed;
                default: return Color.LightBlue;
            }
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await networkManager.ConnectToServer(txtIP.Text, int.Parse(txtPort.Text));
                isHost = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async void BtnWait_Click(object sender, EventArgs e)
        {
            try
            {
                await networkManager.StartServer(int.Parse(txtPort.Text));
                isHost = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnAutoPlace_Click(object sender, EventArgs e)
        {
            game.PlayerBoard.AutoPlaceShips();
            UpdateUI();
        }

        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            game.StartGame(true); 
            await networkManager.SendMessage("START_GAME");

            lblStatus.Text = "Игра началась! Ваш ход!";
            UpdateUI();
        }

        private void PlayerGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private async void EnemyGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (game.CurrentState != GameState.PlayerTurn) return;

            int x = e.RowIndex;
            int y = e.ColumnIndex;

            if (game.EnemyBoard.Grid[x, y] == CellState.Hit || game.EnemyBoard.Grid[x, y] == CellState.Miss)
            {
                return;
            }

            Console.WriteLine($"Игрок стреляет в [{x},{y}], потоплено кораблей: {game.EnemyBoard.Ships.Count(s => s.IsSunk())}/{game.EnemyBoard.Ships.Count}");

            await networkManager.SendMessage($"MOVE:{x}:{y}");

            if (game.EnemyBoard.AllShipsSunk())
            {
                game.CurrentState = GameState.GameOver;
                game.Winner = "Player";
                lblStatus.Text = "Вы победили! Все корабли противника потоплены!";
                UpdateUI();
                ShowRestartDialog();
            }
        }

        private void OnConnected()
        {
            this.Invoke(new Action(() =>
            {
                lblStatus.Text = "✓ Подключено! Расставьте корабли и начните игру.";
                btnStartGame.Enabled = true;
                UpdateUI();
            }));
        }

        private void OnNetworkMessageReceived(string message)
        {
            this.Invoke(new Action(() =>
            {
                string[] parts = message.Split(':');
                switch (parts[0])
                {
                    case "START_GAME":

                        game.StartGame(false); 
                        UpdateUI();
                        lblStatus.Text = "Игра началась! Ожидайте хода противника...";
                        break;

                    case "MOVE":
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        var result = game.ProcessEnemyMove(x, y);
                        _ = networkManager.SendMessage($"RESULT:{result}:{x}:{y}");

                        if (game.PlayerBoard.AllShipsSunk())
                        {
                            game.CurrentState = GameState.GameOver;
                            game.Winner = "Enemy";
                            lblStatus.Text = "Вы проиграли! Все ваши корабли потоплены!";
                            UpdateUI();
                            ShowRestartDialog();
                        }
                        else
                        {
                            game.CurrentState = GameState.PlayerTurn;
                            lblStatus.Text = "Ваш ход! Стреляйте!";
                            UpdateUI();
                        }
                        break;

                    case "RESULT":
                        var resultType = (CellState)Enum.Parse(typeof(CellState), parts[1]);
                        int targetX = int.Parse(parts[2]);
                        int targetY = int.Parse(parts[3]);

                        game.EnemyBoard.Grid[targetX, targetY] = resultType;

                        if (game.EnemyBoard.AllShipsSunk())
                        {
                            game.CurrentState = GameState.GameOver;
                            game.Winner = "Player";
                            lblStatus.Text = "Вы победили! Все корабли противника потоплены!";
                            UpdateUI();
                            ShowRestartDialog();
                            break;
                        }

                        if (resultType == CellState.Miss)
                        {
                            game.CurrentState = GameState.EnemyTurn;
                            lblStatus.Text = "Промах! Ход противника...";
                        }
                        else
                        {
                            game.CurrentState = GameState.PlayerTurn;
                            lblStatus.Text = "Попадание! Продолжайте стрелять!";
                        }
                        UpdateUI();
                        break;

                    case "RESTART":
                        if (MessageBox.Show("Противник предлагает реванш. Согласны?", "Реванш",
                            MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            game.RestartGame();
                            _ = networkManager.SendMessage("RESTART_ACK");
                            UpdateUI();
                        }
                        break;

                    case "RESTART_ACK":
                        game.RestartGame();
                        UpdateUI();
                        break;
                }
            }));
        }

        private void ShowRestartDialog()
        {
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                if (MessageBox.Show("Хотите сыграть еще раз?", "Игра окончена",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _ = networkManager.SendMessage("RESTART");
                }
            };
            timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            networkManager?.Disconnect();
            base.OnFormClosing(e);
        }
    }
}