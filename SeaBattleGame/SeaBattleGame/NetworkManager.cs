using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class NetworkManager
{
    private TcpClient client;      // Клиент для подключения
    private TcpListener listener;  // Сервер для ожидания подключения
    private NetworkStream stream;  // Поток данных (канал связи)

    // События, на которые подписывается форма (чтобы узнать, что пришло сообщение)
    public event Action<string> OnMessageReceived;
    public event Action OnConnected;

    public bool IsConnected => client?.Connected == true;

    // Запуск в режиме Сервера (Хоста)
    public async Task StartServer(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(); // Начинаем слушать порт
        // Ждем подключения (асинхронно, не блокируя UI)
        client = await listener.AcceptTcpClientAsync();
        stream = client.GetStream(); // Получаем канал для общения
        OnConnected?.Invoke();       // Сообщаем форме, что соединение есть
        _ = Task.Run(StartListening); // Запускаем бесконечный цикл прослушивания сообщений
    }

    // Запуск в режиме Клиента (Подключение к другу)
    public async Task ConnectToServer(string ip, int port)
    {
        client = new TcpClient();
        await client.ConnectAsync(ip, port);
        stream = client.GetStream();
        OnConnected?.Invoke();
        _ = Task.Run(StartListening);
    }

    // Бесконечный цикл чтения входящих сообщений
    private async Task StartListening()
    {
        byte[] buffer = new byte[1024]; // Буфер для данных
        try
        {
            while (client?.Connected == true)
            {
                // Читаем данные из потока
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // Если 0 байт - соединение закрыто

                // Преобразуем байты в строку (UTF8)
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Вызываем событие (сообщаем форме "Пришло письмо!")
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
        }
    }

    // Отправка сообщения
    public async Task SendMessage(string message)
    {
        if (stream?.CanWrite == true)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    // Закрытие соединения (освобождение ресурсов)
    public void Disconnect()
    {
        stream?.Close();
        client?.Close();
        listener?.Stop();
    }
}