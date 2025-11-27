using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class NetworkManager
{
    private TcpClient client;
    private TcpListener listener;
    private NetworkStream stream;
    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public bool IsConnected => client?.Connected == true;

    public async Task StartServer(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        client = await listener.AcceptTcpClientAsync();
        stream = client.GetStream();
        OnConnected?.Invoke();
        _ = Task.Run(StartListening);
    }

    public async Task ConnectToServer(string ip, int port)
    {
        client = new TcpClient();
        await client.ConnectAsync(ip, port);
        stream = client.GetStream();
        OnConnected?.Invoke();
        _ = Task.Run(StartListening);
    }

    private async Task StartListening()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (client?.Connected == true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
        }
    }

    public async Task SendMessage(string message)
    {
        if (stream?.CanWrite == true)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    public void Disconnect()
    {
        stream?.Close();
        client?.Close();
        listener?.Stop();
    }
}