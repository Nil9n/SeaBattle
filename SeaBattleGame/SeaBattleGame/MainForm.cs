using System;
using System.Drawing;
using System.Linq; // Нужно для поиска кораблей (Linq)
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeaBattleGame
{
    public partial class MainForm : Form
    {
        // === Флаги для синхронизации старта игры ===
        private bool isEnemyReady = false;   // Готов ли противник?
        private bool isMyShipsPlaced = false; // Готов ли я?

        private Game game;
        private NetworkManager networkManager;
        private bool isHost = false;
        private int cellSize = 35; // Размер клетки в пикселях

        // Настройки расстановки
        private int currentShipSize = 4;
        private int[] shipsToPlace = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private int currentShipIndex = 0;
        private bool isHorizontal = true;

        // Элементы интерфейса
        private DataGridView playerGrid;
        private DataGridView enemyGrid;
        private Button btnConnect, btnWait, btnAutoPlace, btnStartGame, btnRotate, btnClearShips;
        private TextBox txtIP, txtPort;
        private Label lblStatus, lblPlayer, lblEnemy, lblShipInfo;

        public MainForm()
        {
            this.ClientSize = new Size(900, 650);
            this.Text = "Морской Бой - Сетевая игра";
            this.StartPosition = FormStartPosition.CenterScreen;

            game = new Game();
            networkManager = new NetworkManager();

            // Подписываемся на события сети
            networkManager.OnMessageReceived += OnNetworkMessageReceived;
            networkManager.OnConnected += OnConnected;

            SetupUI();     // Создаем кнопки и сетки
            UpdateUI();    // Рисуем начальное состояние
            UpdateShipInfo(); // Пишем "Ставьте 4-палубный"
        }

        // --- Инициализация интерфейса (создание кнопок кодом) ---
        private void SetupUI()
        {
            lblPlayer = new Label { Text = "Ваше поле:", Location = new Point(20, 20), AutoSize = true };
            lblEnemy = new Label { Text = "Поле противника:", Location = new Point(470, 20), AutoSize = true };
            lblStatus = new Label { Text = "Расставьте корабли", Location = new Point(20, 550), AutoSize = true, Font = new Font("Arial", 12) };
            lblShipInfo = new Label { Text = "Разместите корабль (4 палубы)", Location = new Point(20, 580), AutoSize = true, ForeColor = Color.Blue };

            // Создаем таблицы
            playerGrid = CreateGrid();
            playerGrid.Location = new Point(20, 50);
            playerGrid.CellClick += PlayerGrid_CellClick;
            playerGrid.MouseMove += PlayerGrid_MouseMove; // Для предпросмотра
            playerGrid.MouseLeave += PlayerGrid_MouseLeave;

            enemyGrid = CreateGrid();
            enemyGrid.Location = new Point(470, 50);
            enemyGrid.CellClick += EnemyGrid_CellClick; // Для стрельбы

            // Элементы подключения
            txtIP = new TextBox { Text = "127.0.0.1", Location = new Point(20, 500), Width = 100 };
            txtPort = new TextBox { Text = "12345", Location = new Point(130, 500), Width = 60 };

            btnConnect = new Button { Text = "Подключиться", Location = new Point(200, 500), Width = 100 };
            btnConnect.Click += BtnConnect_Click;

            btnWait = new Button { Text = "Ожидать (Сервер)", Location = new Point(310, 500), Width = 150 };
            btnWait.Click += BtnWait_Click;

            // Кнопки управления расстановкой
            btnAutoPlace = new Button { Text = "Авторасстановка", Location = new Point(470, 500), Width = 120 };
            btnAutoPlace.Click += BtnAutoPlace_Click;

            btnRotate = new Button { Text = "Повернуть ↔", Location = new Point(600, 500), Width = 100 };
            btnRotate.Click += BtnRotate_Click;

            btnClearShips = new Button { Text = "Очистить поле", Location = new Point(710, 500), Width = 100 };
            btnClearShips.Click += BtnClearShips_Click;

            btnStartGame = new Button { Text = "Я ГОТОВ", Location = new Point(470, 530), Width = 100 };
            btnStartGame.Click += BtnStartGame_Click;
            btnStartGame.Enabled = false;

            Controls.AddRange(new Control[] { lblPlayer, lblEnemy, lblStatus, lblShipInfo, playerGrid, enemyGrid,
                                            txtIP, txtPort, btnConnect, btnWait, btnAutoPlace, btnRotate, btnClearShips, btnStartGame });
        }

        // Настройка внешнего вида DataGridView
        private DataGridView CreateGrid()
        {
            var grid = new DataGridView();
            grid.Width = cellSize * 10 + 20;
            grid.Height = cellSize * 10 + 20;
            grid.ScrollBars = ScrollBars.None;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.BackgroundColor = Color.LightBlue;

            // Настройка колонок и строк 10x10
            for (int i = 0; i < 10; i++)
                grid.Columns.Add(new DataGridViewTextBoxColumn { Width = cellSize });
            grid.RowCount = 10;
            for (int i = 0; i < 10; i++) grid.Rows[i].Height = cellSize;

            return grid;
        }

        // --- Отрисовка и обновление UI ---
        private void UpdateUI()
        {
            DrawBoard(game.PlayerBoard, playerGrid, true);
            DrawBoard(game.EnemyBoard, enemyGrid, false);

            bool isSetup = game.CurrentState == GameState.Setup;

            // Блокируем кнопки очистки и поворота во время игры, чтобы не сломать логику (Фикс бага №4)
            btnAutoPlace.Enabled = isSetup;
            btnClearShips.Enabled = isSetup;
            btnRotate.Enabled = isSetup;

            switch (game.CurrentState)
            {
                case GameState.Setup:
                    lblStatus.Text = networkManager.IsConnected ? "Расставьте корабли и нажмите 'Я ГОТОВ'" : "Подключитесь к сети...";
                    enemyGrid.Enabled = false;
                    playerGrid.Enabled = true;
                    break;
                case GameState.PlayerTurn:
                    lblStatus.Text = "Ваш ход! Стреляйте по полю противника!";
                    enemyGrid.Enabled = true; // Можно кликать
                    playerGrid.Enabled = false;
                    break;
                case GameState.EnemyTurn:
                    lblStatus.Text = "Ход противника... Ждите.";
                    enemyGrid.Enabled = false; // Нельзя кликать
                    playerGrid.Enabled = false;
                    break;
                case GameState.GameOver:
                    lblStatus.Text = $"Игра окончена! Победитель: {(game.Winner == "Player" ? "ВЫ!" : "ПРОТИВНИК")}";
                    enemyGrid.Enabled = false;
                    playerGrid.Enabled = false;
                    break;
            }

            // Кнопка старта доступна, только если подключены и расставили все корабли
            if (game.CurrentState == GameState.Setup)
                btnStartGame.Enabled = networkManager.IsConnected && currentShipIndex >= shipsToPlace.Length && !isMyShipsPlaced;
        }

        // Раскраска ячеек в зависимости от состояния
        private void DrawBoard(GameBoard board, DataGridView grid, bool isOwnBoard)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var cell = grid.Rows[i].Cells[j];
                    Color color = GetColorForCell(board.Grid[i, j], isOwnBoard);
                    cell.Style.BackColor = color;
                    cell.Style.SelectionBackColor = color; // Чтобы выделение не портило цвет
                }
            }
        }

        private Color GetColorForCell(CellState state, bool isOwnBoard)
        {
            switch (state)
            {
                case CellState.Empty: return Color.LightBlue;
                // Чужие корабли не показываем (рисуем как воду), свои - серым
                case CellState.Ship: return isOwnBoard ? Color.DarkGray : Color.LightBlue;
                case CellState.Hit: return Color.OrangeRed;
                case CellState.Miss: return Color.WhiteSmoke; // "Молоко"
                case CellState.Sunk: return Color.DarkRed;    // Убит
                default: return Color.LightBlue;
            }
        }

        private void UpdateShipInfo()
        {
            if (currentShipIndex < shipsToPlace.Length)
            {
                currentShipSize = shipsToPlace[currentShipIndex];
                lblShipInfo.Text = $"Текущий: {currentShipSize}-палубный ({(isHorizontal ? "Горизонтально" : "Вертикально")})";
            }
            else
            {
                lblShipInfo.Text = "Все корабли расставлены! Нажмите 'Я ГОТОВ'.";
                lblShipInfo.ForeColor = Color.Green;
            }
        }

        // --- Логика событий мыши (Предпросмотр установки) ---
        private void PlayerGrid_MouseMove(object sender, MouseEventArgs e)
        {
            // Работает только в фазе расстановки
            if (game.CurrentState != GameState.Setup) return;
            if (currentShipIndex >= shipsToPlace.Length) return;

            var hitTest = playerGrid.HitTest(e.X, e.Y);
            int x = hitTest.RowIndex;
            int y = hitTest.ColumnIndex;

            if (x < 0 || y < 0 || x >= 10 || y >= 10) { UpdateUI(); return; }

            // 1. Очищаем цвета (сбрасываем выделение) - Фикс проблемы с незакрашенной клеткой
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var cell = playerGrid.Rows[i].Cells[j];
                    Color baseColor = GetColorForCell(game.PlayerBoard.Grid[i, j], true);
                    cell.Style.BackColor = baseColor;
                    cell.Style.SelectionBackColor = baseColor; // ВАЖНО для фикса
                }
            }

            // 2. Проверяем, можно ли тут поставить корабль
            Ship tempShip = new Ship(currentShipSize);
            bool canPlace = game.PlayerBoard.IsValidPlacement(tempShip, x, y, isHorizontal);
            Color previewColor = canPlace ? Color.LightGreen : Color.LightPink;

            // 3. Рисуем "призрак" корабля
            for (int i = 0; i < currentShipSize; i++)
            {
                int posX = x + (isHorizontal ? i : 0);
                int posY = y + (isHorizontal ? 0 : i);

                if (posX >= 0 && posX < 10 && posY >= 0 && posY < 10)
                {
                    playerGrid.Rows[posX].Cells[posY].Style.BackColor = previewColor;
                    playerGrid.Rows[posX].Cells[posY].Style.SelectionBackColor = previewColor; // ВАЖНО
                }
            }
        }

        private void PlayerGrid_MouseLeave(object sender, EventArgs e)
        {
            if (game.CurrentState == GameState.Setup) UpdateUI();
        }

        // Клик по своему полю - установка корабля
        private void PlayerGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (game.CurrentState != GameState.Setup) return;
            if (currentShipIndex >= shipsToPlace.Length) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            Ship ship = new Ship(currentShipSize);
            if (game.PlayerBoard.PlaceShip(ship, e.RowIndex, e.ColumnIndex, isHorizontal))
            {
                currentShipIndex++; // Переходим к следующему кораблю
                UpdateUI();
                UpdateShipInfo();
            }
        }

        // Клик по вражескому полю - СТРЕЛЬБА
        private async void EnemyGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (game.CurrentState != GameState.PlayerTurn) return; // Не твой ход!

            int x = e.RowIndex;
            int y = e.ColumnIndex;

            // Нельзя стрелять туда, где уже стреляли
            if (game.EnemyBoard.Grid[x, y] == CellState.Hit ||
                game.EnemyBoard.Grid[x, y] == CellState.Miss ||
                game.EnemyBoard.Grid[x, y] == CellState.Sunk)
            {
                return;
            }

            // Отправляем координаты выстрела врагу по сети
            await networkManager.SendMessage($"MOVE:{x}:{y}");
        }

        // --- Сетевая логика ---
        private string GetLocalIPAddress()
        {
            // Получаем свой IP для удобства
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            return "127.0.0.1";
        }

        private async void BtnWait_Click(object sender, EventArgs e)
        {
            try
            {
                string myIP = GetLocalIPAddress();
                txtIP.Text = myIP;
                await networkManager.StartServer(int.Parse(txtPort.Text));
                isHost = true;
                MessageBox.Show($"Сервер запущен!\nВаш IP: {myIP}\nСообщите его другу.", "Инфо");
                lblStatus.Text = $"Сервер ({myIP}). Ждем игрока...";
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await networkManager.ConnectToServer(txtIP.Text, int.Parse(txtPort.Text));
                isHost = false;
                lblStatus.Text = "Подключено! Расставляйте корабли.";
            }
            catch (Exception ex) { MessageBox.Show("Ошибка подключения: " + ex.Message); }
        }

        // Событие: Установили соединение
        private void OnConnected()
        {
            this.Invoke(new Action(() => {
                lblStatus.Text = "Соединение установлено. Расставляйте корабли.";
                UpdateUI();
            }));
        }

        // ГЛАВНЫЙ МЕТОД ОБРАБОТКИ СООБЩЕНИЙ ОТ ПРОТИВНИКА
        private void OnNetworkMessageReceived(string message)
        {
            // Invoke нужен, чтобы работать с формой из сетевого потока
            this.Invoke(new Action(async () =>
            {
                string[] parts = message.Split(':');
                switch (parts[0])
                {
                    case "READY": // Противник нажал "Я ГОТОВ"
                        isEnemyReady = true;
                        CheckGameStart(); // Проверяем, можем ли начать
                        break;

                    case "MOVE": // Враг выстрелил в нас (x, y)
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);

                        // Проверяем попадание на нашем поле
                        var result = game.ProcessEnemyMove(x, y);

                        // Если убили корабль, отправляем его данные, чтобы враг нарисовал "ореол"
                        string extraData = "";
                        if (result == CellState.Sunk)
                        {
                            var sunkShip = game.PlayerBoard.Ships.FirstOrDefault(s => s.IsAt(x, y));
                            if (sunkShip != null)
                                extraData = $":{sunkShip.StartX}:{sunkShip.StartY}:{sunkShip.Size}:{(sunkShip.IsHorizontal ? 1 : 0)}";
                        }

                        // Отправляем результат врагу (Попал/Мимо)
                        await networkManager.SendMessage($"RESULT:{result}:{x}:{y}{extraData}");

                        // Проверка проигрыша
                        if (game.PlayerBoard.AllShipsSunk())
                        {
                            game.CurrentState = GameState.GameOver;
                            game.Winner = "Enemy";
                            lblStatus.Text = "Вы проиграли! Все корабли потоплены.";
                            UpdateUI();
                            // Сообщаем врагу, что он выиграл (чтобы у него тоже игра кончилась)
                            await networkManager.SendMessage("GAME_OVER_YOU_WIN");
                            ShowRestartDialog();
                        }
                        else
                        {
                            // Логика передачи хода: Если попал - стреляет снова. Если мимо - ход переходит.
                            if (result == CellState.Miss)
                            {
                                game.CurrentState = GameState.PlayerTurn;
                                lblStatus.Text = "Противник промахнулся! ВАШ ХОД!";
                            }
                            else
                            {
                                game.CurrentState = GameState.EnemyTurn;
                                lblStatus.Text = "Противник попал! Он стреляет снова...";
                            }
                            UpdateUI();
                        }
                        break;

                    case "RESULT": // Пришел результат НАШЕГО выстрела
                        var resultType = (CellState)Enum.Parse(typeof(CellState), parts[1]);
                        int targetX = int.Parse(parts[2]);
                        int targetY = int.Parse(parts[3]);

                        // Обновляем карту врага (ставим крестик или точку)
                        game.EnemyBoard.Grid[targetX, targetY] = resultType;

                        // Если убили корабль, рисуем ореол (Miss вокруг)
                        if (resultType == CellState.Sunk && parts.Length > 4)
                        {
                            int sX = int.Parse(parts[4]);
                            int sY = int.Parse(parts[5]);
                            int sSize = int.Parse(parts[6]);
                            bool sHor = int.Parse(parts[7]) == 1;

                            Ship tempShip = new Ship(sSize);
                            tempShip.SetPosition(sX, sY, sHor);
                            MarkSurroundingAsMiss(game.EnemyBoard, tempShip);
                        }

                        // Логика хода
                        if (resultType == CellState.Miss)
                        {
                            game.CurrentState = GameState.EnemyTurn;
                            lblStatus.Text = "Промах! Ход переходит к противнику.";
                        }
                        else
                        {
                            game.CurrentState = GameState.PlayerTurn;
                            lblStatus.Text = "ЕСТЬ ПОПАДАНИЕ! Стреляйте еще!";
                        }
                        UpdateUI();
                        break;

                    case "GAME_OVER_YOU_WIN": // Враг сдался (мы победили)
                        game.CurrentState = GameState.GameOver;
                        game.Winner = "Player";
                        lblStatus.Text = "ПОБЕДА! Вы уничтожили флот противника!";
                        UpdateUI();
                        ShowRestartDialog();
                        break;

                    case "RESTART": // Предложение реванша
                        if (MessageBox.Show("Противник предлагает реванш. Играем?", "Реванш", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            ResetGameVariables();
                            game.RestartGame();
                            await networkManager.SendMessage("RESTART_ACK");
                            UpdateUI();
                            UpdateShipInfo();
                        }
                        break;

                    case "RESTART_ACK": // Согласие на реванш
                        ResetGameVariables();
                        game.RestartGame();
                        UpdateUI();
                        UpdateShipInfo();
                        break;
                }
            }));
        }

        // --- Вспомогательные методы ---

        // Рисует белые точки вокруг убитого корабля на поле врага
        private void MarkSurroundingAsMiss(GameBoard board, Ship ship)
        {
            int startX = Math.Max(0, ship.StartX - 1);
            int startY = Math.Max(0, ship.StartY - 1);
            int endX = Math.Min(9, ship.StartX + (ship.IsHorizontal ? ship.Size : 1));
            int endY = Math.Min(9, ship.StartY + (ship.IsHorizontal ? 1 : ship.Size));

            for (int x = startX; x <= endX; x++)
                for (int y = startY; y <= endY; y++)
                    if (board.Grid[x, y] == CellState.Empty)
                        board.Grid[x, y] = CellState.Miss;
        }

        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            if (currentShipIndex < shipsToPlace.Length) { MessageBox.Show("Сначала расставьте все корабли!"); return; }

            // Не начинаем сразу, а посылаем сигнал готовности
            isMyShipsPlaced = true;
            btnStartGame.Enabled = false;
            lblStatus.Text = "Ожидаем готовности соперника...";

            await networkManager.SendMessage("READY");
            CheckGameStart();
        }

        private void CheckGameStart()
        {
            // Начинаем, только если ОБА готовы
            if (isMyShipsPlaced && isEnemyReady)
            {
                game.StartGame(isHost);
                lblStatus.Text = isHost ? "ИГРА НАЧАЛАСЬ! Ваш ход." : "ИГРА НАЧАЛАСЬ! Ход противника.";
                UpdateUI();
            }
            else if (isEnemyReady && !isMyShipsPlaced)
            {
                lblStatus.Text = "Противник уже готов! Скорее расставляйте корабли.";
            }
        }

        // Сброс всего
        private void BtnClearShips_Click(object sender, EventArgs e)
        {
            game.RestartGame();
            currentShipIndex = 0;
            isMyShipsPlaced = false;
            UpdateUI();
            UpdateShipInfo();
        }

        private void BtnRotate_Click(object sender, EventArgs e)
        {
            isHorizontal = !isHorizontal;
            UpdateShipInfo();
        }

        private void BtnAutoPlace_Click(object sender, EventArgs e)
        {
            game.PlayerBoard.AutoPlaceShips();
            currentShipIndex = shipsToPlace.Length;
            UpdateUI();
            UpdateShipInfo();
        }

        private void ResetGameVariables()
        {
            isEnemyReady = false;
            isMyShipsPlaced = false;
            currentShipIndex = 0;
            btnStartGame.Enabled = false;
        }

        private void ShowRestartDialog()
        {
            // Таймер нужен, чтобы диалог не вылез раньше отрисовки последнего взрыва
            Timer timer = new Timer { Interval = 1000 };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                if (MessageBox.Show("Сыграть еще раз?", "Конец игры", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    _ = networkManager.SendMessage("RESTART");
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