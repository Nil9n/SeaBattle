using System;
using System.Drawing;
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

        private int currentShipSize = 4;
        private int[] shipsToPlace = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private int currentShipIndex = 0;
        private bool isHorizontal = true;

        private DataGridView playerGrid;
        private DataGridView enemyGrid;
        private Button btnConnect;
        private Button btnWait;
        private Button btnAutoPlace;
        private Button btnStartGame;
        private Button btnRotate;
        private Button btnClearShips;
        private TextBox txtIP;
        private TextBox txtPort;
        private Label lblStatus;
        private Label lblPlayer;
        private Label lblEnemy;
        private Label lblShipInfo;

        public MainForm()
        {
            this.ClientSize = new Size(900, 650);
            this.Text = "Морской Бой";
            this.StartPosition = FormStartPosition.CenterScreen;

            game = new Game();
            networkManager = new NetworkManager();
            networkManager.OnMessageReceived += OnNetworkMessageReceived;
            networkManager.OnConnected += OnConnected;

            SetupUI();
            UpdateUI();
            UpdateShipInfo();
        }

        private void SetupUI()
        {
            lblPlayer = new Label { Text = "Ваше поле:", Location = new Point(20, 20), AutoSize = true };
            lblEnemy = new Label { Text = "Поле противника:", Location = new Point(470, 20), AutoSize = true };
            lblStatus = new Label { Text = "Расставьте корабли", Location = new Point(20, 550), AutoSize = true, Font = new Font("Arial", 12) };
            lblShipInfo = new Label { Text = "Разместите корабль (4 палубы)", Location = new Point(20, 580), AutoSize = true, ForeColor = Color.Blue };

            playerGrid = CreateGrid();
            playerGrid.Location = new Point(20, 50);
            playerGrid.CellClick += PlayerGrid_CellClick;
            playerGrid.MouseMove += PlayerGrid_MouseMove;
            playerGrid.MouseLeave += PlayerGrid_MouseLeave;

            enemyGrid = CreateGrid();
            enemyGrid.Location = new Point(470, 50);
            enemyGrid.CellClick += EnemyGrid_CellClick;

            txtIP = new TextBox { Text = "127.0.0.1", Location = new Point(20, 500), Width = 100 };
            txtPort = new TextBox { Text = "12345", Location = new Point(130, 500), Width = 60 };

            btnConnect = new Button { Text = "Подключиться", Location = new Point(200, 500), Width = 100 };
            btnConnect.Click += BtnConnect_Click;

            btnWait = new Button { Text = "Ожидать подключения", Location = new Point(310, 500), Width = 150 };
            btnWait.Click += BtnWait_Click;

            btnAutoPlace = new Button { Text = "Авторасстановка", Location = new Point(470, 500), Width = 120 };
            btnAutoPlace.Click += BtnAutoPlace_Click;

            btnRotate = new Button { Text = "Повернуть ↔", Location = new Point(600, 500), Width = 100 };
            btnRotate.Click += BtnRotate_Click;

            btnClearShips = new Button { Text = "Очистить поле", Location = new Point(710, 500), Width = 100 };
            btnClearShips.Click += BtnClearShips_Click;

            btnStartGame = new Button { Text = "Начать игру", Location = new Point(470, 530), Width = 100 };
            btnStartGame.Click += BtnStartGame_Click;
            btnStartGame.Enabled = false;

            Controls.AddRange(new Control[] { lblPlayer, lblEnemy, lblStatus, lblShipInfo, playerGrid, enemyGrid,
                                            txtIP, txtPort, btnConnect, btnWait, btnAutoPlace, btnRotate, btnClearShips, btnStartGame });
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
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.DefaultCellStyle.SelectionBackColor = Color.Transparent;

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
                    playerGrid.Enabled = true;
                    break;
                case GameState.PlayerTurn:
                    lblStatus.Text = "Ваш ход! Кликните по полю противника!";
                    enemyGrid.Enabled = true;
                    playerGrid.Enabled = false;
                    break;
                case GameState.EnemyTurn:
                    lblStatus.Text = "Ход противника...";
                    enemyGrid.Enabled = false;
                    playerGrid.Enabled = false;
                    break;
                case GameState.GameOver:
                    lblStatus.Text = $"Игра окончена! Победитель: {(game.Winner == "Player" ? "Вы" : "Противник")}";
                    enemyGrid.Enabled = false;
                    playerGrid.Enabled = false;
                    ShowRestartDialog();
                    break;
            }

            btnStartGame.Enabled = networkManager.IsConnected && game.CurrentState == GameState.Setup && currentShipIndex >= shipsToPlace.Length;
        }

        private void DrawBoard(GameBoard board, DataGridView grid, bool isOwnBoard)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var cell = grid.Rows[i].Cells[j];
                    cell.Style.BackColor = GetColorForCell(board.Grid[i, j], isOwnBoard);
                    cell.Style.SelectionBackColor = cell.Style.BackColor;
                }
            }
        }

        private Color GetColorForCell(CellState state, bool isOwnBoard)
        {
            switch (state)
            {
                case CellState.Empty: return Color.LightBlue;
                case CellState.Ship: return isOwnBoard ? Color.DarkGray : Color.LightBlue;
                case CellState.Hit: return Color.Red;
                case CellState.Miss: return Color.White;
                case CellState.Sunk: return Color.DarkRed;
                default: return Color.LightBlue;
            }
        }

        private void UpdateShipInfo()
        {
            if (currentShipIndex < shipsToPlace.Length)
            {
                currentShipSize = shipsToPlace[currentShipIndex];
                lblShipInfo.Text = $"Разместите корабль ({currentShipSize} палубы) {(isHorizontal ? "горизонтально" : "вертикально")}";
                lblShipInfo.ForeColor = Color.Blue;
            }
            else
            {
                lblShipInfo.Text = "Все корабли расставлены! Можете начать игру.";
                lblShipInfo.ForeColor = Color.Green;
            }
        }

        private void PlayerGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (game.CurrentState != GameState.Setup) return;
            if (currentShipIndex >= shipsToPlace.Length) return;

            var hitTest = playerGrid.HitTest(e.X, e.Y);
            int x = hitTest.RowIndex;
            int y = hitTest.ColumnIndex;

            // Если мышь ушла за пределы сетки, перерисовываем чистое поле
            if (x < 0 || y < 0 || x >= 10 || y >= 10)
            {
                UpdateUI();
                return;
            }

            // 1. Сначала очищаем цвета (сбрасываем к состоянию доски)
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var cell = playerGrid.Rows[i].Cells[j];
                    Color baseColor = GetColorForCell(game.PlayerBoard.Grid[i, j], true);

                    cell.Style.BackColor = baseColor;
                    // ВАЖНО: Сбрасываем цвет выделения, чтобы он совпадал с цветом ячейки
                    cell.Style.SelectionBackColor = baseColor;
                }
            }

            // 2. Рассчитываем валидность установки
            Ship tempShip = new Ship(currentShipSize);
            bool canPlace = game.PlayerBoard.IsValidPlacement(tempShip, x, y, isHorizontal);

            Color previewColor = canPlace ? Color.FromArgb(180, 255, 180) : Color.FromArgb(255, 180, 180);

            // 3. Рисуем предпросмотр
            for (int i = 0; i < currentShipSize; i++)
            {
                int posX = x + (isHorizontal ? i : 0);
                int posY = y + (isHorizontal ? 0 : i);

                if (posX >= 0 && posX < 10 && posY >= 0 && posY < 10)
                {
                    var cell = playerGrid.Rows[posX].Cells[posY];

                    cell.Style.BackColor = previewColor;
                    // ВАЖНО: Принудительно красим выделение в цвет предпросмотра,
                    // чтобы ячейка ПОД курсором тоже стала зеленой/красной
                    cell.Style.SelectionBackColor = previewColor;
                }
            }
        }

        private void PlayerGrid_MouseLeave(object sender, EventArgs e)
        {
            if (game.CurrentState == GameState.Setup)
            {
                UpdateUI();
            }
        }

        private void PlayerGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (game.CurrentState != GameState.Setup) return;
            if (currentShipIndex >= shipsToPlace.Length) return;

            int x = e.RowIndex;
            int y = e.ColumnIndex;

            if (x < 0 || y < 0) return;

            Ship ship = new Ship(currentShipSize);
            if (game.PlayerBoard.PlaceShip(ship, x, y, isHorizontal))
            {
                currentShipIndex++;
                UpdateUI();
                UpdateShipInfo();
            }
            else
            {
                MessageBox.Show("Невозможно разместить корабль здесь!", "Ошибка");
            }
        }

        private void BtnRotate_Click(object sender, EventArgs e)
        {
            isHorizontal = !isHorizontal;
            UpdateShipInfo();
        }

        private void BtnClearShips_Click(object sender, EventArgs e)
        {
            game.RestartGame();
            currentShipIndex = 0;
            UpdateUI();
            UpdateShipInfo();
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await networkManager.ConnectToServer(txtIP.Text, int.Parse(txtPort.Text));
                isHost = false;
                lblStatus.Text = "✓ Подключено к хосту!";
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
                lblStatus.Text = "✓ Ожидание подключения...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnAutoPlace_Click(object sender, EventArgs e)
        {
            game.PlayerBoard.AutoPlaceShips();
            currentShipIndex = shipsToPlace.Length;
            UpdateUI();
            UpdateShipInfo();
            lblStatus.Text = "Корабли расставлены!";
        }

        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            if (currentShipIndex < shipsToPlace.Length)
            {
                MessageBox.Show("Расставьте все корабли прежде чем начать игру!", "Ошибка");
                return;
            }

            game.StartGame(isHost);
            await networkManager.SendMessage("START_GAME");

            lblStatus.Text = isHost ? "Игра началась! Ваш ход!" : "Игра началась! Ожидайте хода...";
            UpdateUI();
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
                lblStatus.Text = "✓ Подключение установлено! Расставьте корабли и начните игру.";
                btnStartGame.Enabled = currentShipIndex >= shipsToPlace.Length;
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
                        lblStatus.Text = "Игра началась! Ожидайте хода противника...";
                        UpdateUI();
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
                            lblStatus.Text = "Ваш ход! Стреляйте!";
                            game.CurrentState = GameState.PlayerTurn;
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
                            currentShipIndex = 0;
                            _ = networkManager.SendMessage("RESTART_ACK");
                            UpdateUI();
                            UpdateShipInfo();
                        }
                        break;

                    case "RESTART_ACK":
                        game.RestartGame();
                        currentShipIndex = 0;
                        UpdateUI();
                        UpdateShipInfo();
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