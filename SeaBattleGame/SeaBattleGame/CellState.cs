public enum CellState
{
    Empty,
    Ship,
    Hit,
    Miss,
    Sunk
}

public enum GameState
{
    Setup,
    PlayerTurn,
    EnemyTurn,
    GameOver
}