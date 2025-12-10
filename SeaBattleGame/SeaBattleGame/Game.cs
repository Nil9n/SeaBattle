namespace SeaBattleGame
{
    // Главный контроллер игры
    public class Game
    {
        public GameBoard PlayerBoard { get; set; } // Наше поле
        public GameBoard EnemyBoard { get; private set; } // Поле врага (как мы его видим)
        public GameState CurrentState { get; set; }
        public string Winner { get; set; }

        public Game()
        {
            PlayerBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            CurrentState = GameState.Setup;
            Winner = null;
        }

        // Старт игры: Хост ходит первым
        public void StartGame(bool isHost)
        {
            CurrentState = isHost ? GameState.PlayerTurn : GameState.EnemyTurn;
            Winner = null;
        }

        // Обработка выстрела ВРАГА по НАМ
        public CellState ProcessEnemyMove(int x, int y)
        {
            // Делаем ход на нашей доске
            var result = PlayerBoard.MakeMove(x, y);
            return result;
        }

        // Обработка выстрела НАС по ВРАГУ (в этом классе мы просто меняем ход)
        // Реальная логика попадания приходит по сети в методе OnNetworkMessageReceived
        public CellState ProcessPlayerMove(int x, int y)
        {
            // Здесь логика проще, т.к. мы ждем ответа от сервера
            if (CurrentState != GameState.PlayerTurn) return CellState.Miss;
            return CellState.Empty; 
        }

        public void RestartGame()
        {
            PlayerBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            CurrentState = GameState.Setup;
            Winner = null;
        }
    }
}