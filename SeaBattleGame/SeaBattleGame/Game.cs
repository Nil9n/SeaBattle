public class Game
{
    public GameBoard PlayerBoard { get; private set; }
    public GameBoard EnemyBoard { get; private set; }
    public GameState CurrentState { get; set; }
    public string Winner { get; set; } 

    public Game()
    {
        PlayerBoard = new GameBoard();
        EnemyBoard = new GameBoard();
        CurrentState = GameState.Setup;
        Winner = null;
    }

    public void StartGame(bool isHost)
    {
        if (isHost)
        {
            CurrentState = GameState.PlayerTurn;
        }
        else
        {
            CurrentState = GameState.EnemyTurn;
        }
        Winner = null;
    }

    public CellState ProcessPlayerMove(int x, int y)
    {
        if (CurrentState != GameState.PlayerTurn)
            return CellState.Miss;

        var result = EnemyBoard.MakeMove(x, y);

        if (result == CellState.Miss)
        {
            CurrentState = GameState.EnemyTurn;
        }

        if (EnemyBoard.AllShipsSunk())
        {
            CurrentState = GameState.GameOver;
            Winner = "Player";
        }

        return result;
    }

    public CellState ProcessEnemyMove(int x, int y)
    {
        var result = PlayerBoard.MakeMove(x, y);

        if (result == CellState.Miss)
        {
            CurrentState = GameState.PlayerTurn;
        }

        if (PlayerBoard.AllShipsSunk())
        {
            CurrentState = GameState.GameOver;
            Winner = "Enemy";
        }

        return result;
    }

    public void RestartGame()
    {
        PlayerBoard = new GameBoard();
        EnemyBoard = new GameBoard();
        CurrentState = GameState.Setup;
        Winner = null;
    }
}