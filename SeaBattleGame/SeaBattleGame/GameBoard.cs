using System;
using System.Collections.Generic;
using System.Linq;

public class GameBoard
{
    public CellState[,] Grid { get; private set; }
    public int Size { get; } = 10;
    public List<Ship> Ships { get; private set; }

    public GameBoard()
    {
        Grid = new CellState[Size, Size];
        Ships = new List<Ship>();

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                Grid[i, j] = CellState.Empty;
    }

    public bool PlaceShip(Ship ship, int x, int y, bool isHorizontal)
    {
        if (!IsValidPlacement(ship, x, y, isHorizontal))
            return false;

        for (int i = 0; i < ship.Size; i++)
        {
            int posX = x + (isHorizontal ? i : 0);
            int posY = y + (isHorizontal ? 0 : i);
            Grid[posX, posY] = CellState.Ship;
        }

        ship.SetPosition(x, y, isHorizontal);
        Ships.Add(ship);
        return true;
    }

    public bool IsValidPlacement(Ship ship, int x, int y, bool isHorizontal)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size) return false;

        if (isHorizontal)
        {
            if (x + ship.Size > Size) return false;
        }
        else
        {
            if (y + ship.Size > Size) return false;
        }

        for (int i = 0; i < ship.Size; i++)
        {
            int shipX = x + (isHorizontal ? i : 0);
            int shipY = y + (isHorizontal ? 0 : i);

            if (Grid[shipX, shipY] != CellState.Empty)
                return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = shipX + dx;
                    int checkY = shipY + dy;

                    if (checkX >= 0 && checkX < Size && checkY >= 0 && checkY < Size)
                    {
                        if (Grid[checkX, checkY] == CellState.Ship)
                            return false;
                    }
                }
            }
        }

        return true;
    }

    public CellState MakeMove(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            return CellState.Miss;

        if (Grid[x, y] == CellState.Ship)
        {
            Grid[x, y] = CellState.Hit;
            var ship = Ships.FirstOrDefault(s => s.IsAt(x, y));
            if (ship != null)
            {
                ship.Hit();
                if (ship.IsSunk())
                {
                    MarkSunkShip(ship);
                    return CellState.Sunk;
                }
            }
            return CellState.Hit;
        }
        else if (Grid[x, y] == CellState.Empty)
        {
            Grid[x, y] = CellState.Miss;
            return CellState.Miss;
        }

        return Grid[x, y];
    }

    private void MarkSunkShip(Ship ship)
    {
        // 1. Помечаем сам корабль как потопленный
        for (int i = 0; i < ship.Size; i++)
        {
            int x = ship.StartX + (ship.IsHorizontal ? i : 0);
            int y = ship.StartY + (ship.IsHorizontal ? 0 : i);
            Grid[x, y] = CellState.Sunk;
        }

        // 2. Помечаем клетки ВОКРУГ корабля как "Промах" (Miss)
        // Проходим по прямоугольнику вокруг корабля
        int startX = Math.Max(0, ship.StartX - 1);
        int startY = Math.Max(0, ship.StartY - 1);
        int endX = Math.Min(Size - 1, ship.StartX + (ship.IsHorizontal ? ship.Size : 1));
        int endY = Math.Min(Size - 1, ship.StartY + (ship.IsHorizontal ? 1 : ship.Size));

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                // Если клетка не является частью корабля (то есть она Empty или Miss), ставим Miss
                if (Grid[x, y] != CellState.Sunk && Grid[x, y] != CellState.Hit)
                {
                    Grid[x, y] = CellState.Miss;
                }
            }
        }
    }

    public bool AllShipsSunk()
    {
        if (Ships.Count == 0) return false;

        foreach (var ship in Ships)
        {
            if (!ship.IsSunk())
                return false;
        }
        return true;
    }

    public void AutoPlaceShips()
    {
        Ships.Clear();

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                if (Grid[i, j] == CellState.Ship)
                    Grid[i, j] = CellState.Empty;

        int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        Random rand = new Random();

        foreach (int size in shipSizes)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < 100)
            {
                int x = rand.Next(Size);
                int y = rand.Next(Size);
                bool horizontal = rand.Next(2) == 0;

                Ship ship = new Ship(size);
                if (PlaceShip(ship, x, y, horizontal))
                {
                    placed = true;
                }
                attempts++;
            }
        }
    }
}