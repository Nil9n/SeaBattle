namespace SeaBattleGame
{
    public class Ship
    {
        // Свойства корабля (Encapsulation: менять их может только сам класс)
        public int Size { get; private set; }   // Длина (кол-во палуб)
        public int Hits { get; private set; }   // Сколько раз попали
        public bool IsHorizontal { get; private set; } // Ориентация
        public int StartX { get; private set; } // Координата носа корабля X
        public int StartY { get; private set; } // Координата носа корабля Y

        // Конструктор: создаем корабль определенной длины
        public Ship(int size)
        {
            Size = size;
            Hits = 0; // Изначально корабль целый
        }

        // Установка позиции корабля на поле
        public void SetPosition(int x, int y, bool isHorizontal)
        {
            StartX = x;
            StartY = y;
            IsHorizontal = isHorizontal;
        }

        // Метод регистрации попадания
        public void Hit()
        {
            Hits++;
        }

        // Проверка: утонул ли корабль (если кол-во попаданий >= длине)
        public bool IsSunk()
        {
            return Hits >= Size;
        }

        // Проверка: находится ли корабль в координате (x, y)
        // Используется, чтобы узнать, в какой именно корабль мы попали
        public bool IsAt(int x, int y)
        {
            if (IsHorizontal)
            {
                // Если горизонтально: Y должен совпадать, X должен быть в диапазоне длины
                return y == StartY && x >= StartX && x < StartX + Size;
            }
            else
            {
                // Если вертикально: X должен совпадать, Y должен быть в диапазоне длины
                return x == StartX && y >= StartY && y < StartY + Size;
            }
        }
    }
}