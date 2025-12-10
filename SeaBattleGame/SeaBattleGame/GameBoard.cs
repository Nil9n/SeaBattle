using System;
using System.Collections.Generic;
using System.Linq;

namespace SeaBattleGame
{
    public class GameBoard
    {
        // Двумерный массив состояний клеток (сетка 10x10)
        public CellState[,] Grid { get; private set; }
        public int Size { get; } = 10;

        // Список всех кораблей на этом поле
        public List<Ship> Ships { get; private set; }

        public GameBoard()
        {
            Grid = new CellState[Size, Size];
            Ships = new List<Ship>();

            // Инициализация поля пустой водой
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    Grid[i, j] = CellState.Empty;
        }

        // Попытка поставить корабль. Возвращает true, если получилось
        public bool PlaceShip(Ship ship, int x, int y, bool isHorizontal)
        {
            if (!IsValidPlacement(ship, x, y, isHorizontal))
                return false; // Нельзя поставить (выход за границы или наложение)

            // Заполняем сетку клетками корабля
            for (int i = 0; i < ship.Size; i++)
            {
                int posX = x + (isHorizontal ? i : 0);
                int posY = y + (isHorizontal ? 0 : i);
                Grid[posX, posY] = CellState.Ship;
            }

            // Запоминаем координаты внутри объекта корабля и добавляем в список
            ship.SetPosition(x, y, isHorizontal);
            Ships.Add(ship);
            return true;
        }

        // Проверка правил расстановки (границы поля + правило "вокруг корабля должно быть пусто")
        public bool IsValidPlacement(Ship ship, int x, int y, bool isHorizontal)
        {
            // 1. Проверка выхода за границы массива
            if (x < 0 || x >= Size || y < 0 || y >= Size) return false;

            if (isHorizontal)
            {
                if (x + ship.Size > Size) return false;
            }
            else
            {
                if (y + ship.Size > Size) return false;
            }

            // 2. Проверка соседей (нельзя ставить вплотную к другим)
            for (int i = 0; i < ship.Size; i++)
            {
                int shipX = x + (isHorizontal ? i : 0);
                int shipY = y + (isHorizontal ? 0 : i);

                // Если клетка уже занята
                if (Grid[shipX, shipY] != CellState.Empty)
                    return false;

                // Проверяем все 8 клеток вокруг текущей точки корабля
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int checkX = shipX + dx;
                        int checkY = shipY + dy;

                        // Если соседняя клетка внутри поля и там уже есть корабль -> ошибка
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

        // Обработка выстрела по координатам (x, y)
        public CellState MakeMove(int x, int y)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size)
                return CellState.Miss;

            // Если попали в корабль
            if (Grid[x, y] == CellState.Ship)
            {
                Grid[x, y] = CellState.Hit; // Ставим метку "Ранен"

                // Ищем, в какой именно корабль попали
                var ship = Ships.FirstOrDefault(s => s.IsAt(x, y));
                if (ship != null)
                {
                    ship.Hit(); // Увеличиваем счетчик попаданий
                    if (ship.IsSunk()) // Если утонул
                    {
                        MarkSunkShip(ship); // Помечаем как убитый + рисуем ореол
                        return CellState.Sunk;
                    }
                }
                return CellState.Hit;
            }
            // Если выстрел в воду
            else if (Grid[x, y] == CellState.Empty)
            {
                Grid[x, y] = CellState.Miss;
                return CellState.Miss;
            }

            return Grid[x, y]; // Если стрельнули туда, где уже стреляли
        }

        // Метод пометки утопленного корабля и обводки вокруг него
        private void MarkSunkShip(Ship ship)
        {
            // 1. Красим сам корабль в темно-красный (Sunk)
            for (int i = 0; i < ship.Size; i++)
            {
                int x = ship.StartX + (ship.IsHorizontal ? i : 0);
                int y = ship.StartY + (ship.IsHorizontal ? 0 : i);
                Grid[x, y] = CellState.Sunk;
            }

            // 2. Ставим "Промахи" (Miss) вокруг корабля
            int startX = Math.Max(0, ship.StartX - 1);
            int startY = Math.Max(0, ship.StartY - 1);
            int endX = Math.Min(Size - 1, ship.StartX + (ship.IsHorizontal ? ship.Size : 1));
            int endY = Math.Min(Size - 1, ship.StartY + (ship.IsHorizontal ? 1 : ship.Size));

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    // Не перезаписываем сам корабль и попадания, красим только пустую воду
                    if (Grid[x, y] != CellState.Sunk && Grid[x, y] != CellState.Hit)
                    {
                        Grid[x, y] = CellState.Miss;
                    }
                }
            }
        }

        // Проверка победы (все ли корабли потоплены)
        public bool AllShipsSunk()
        {
            if (Ships.Count == 0) return false;
            return Ships.All(s => s.IsSunk());
        }

        // Алгоритм авторасстановки (рандом)
        public void AutoPlaceShips()
        {
            Ships.Clear();
            // Очищаем сетку
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    Grid[i, j] = CellState.Empty;

            int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
            Random rand = new Random();

            foreach (int size in shipSizes)
            {
                bool placed = false;
                int attempts = 0;

                // Пытаемся поставить корабль 100 раз в случайные места
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
}