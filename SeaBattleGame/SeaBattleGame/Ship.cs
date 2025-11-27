public class Ship
{
    public int Size { get; private set; }
    public int Hits { get; private set; }
    public bool IsHorizontal { get; private set; }
    public int StartX { get; private set; }
    public int StartY { get; private set; }

    public Ship(int size)
    {
        Size = size;
        Hits = 0;
    }

    public void SetPosition(int x, int y, bool isHorizontal)
    {
        StartX = x;
        StartY = y;
        IsHorizontal = isHorizontal;
    }

    public void Hit()
    {
        Hits++;
    }

    public bool IsSunk()
    {
        return Hits >= Size;
    }

    public bool IsAt(int x, int y)
    {
        if (IsHorizontal)
        {
            return y == StartY && x >= StartX && x < StartX + Size;
        }
        else
        {
            return x == StartX && y >= StartY && y < StartY + Size;
        }
    }
}