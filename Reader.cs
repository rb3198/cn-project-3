using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatApp
{
    public class Reader
    {
        private readonly TcpClient _client;
        private Action? _onClose;
        public Reader(TcpClient client)
        {
            _client = client;
            Console.WriteLine($"Connected to {client.Client.RemoteEndPoint?.ToString() ?? "unknown"}");
        }

        public void OnClose(Action onClose)
        {
            _onClose = onClose;
        }

        public void Read(object? obj)
        {
            try
            {
                var stream = _client.GetStream();
                var reader = new StreamReader(stream, Encoding.ASCII);
                string? line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    Console.WriteLine($"Remote: {line}");
                }
                if (_client.Client.Poll(1, SelectMode.SelectRead) && !stream.DataAvailable)
                {
                    CloseConnection();
                    return;
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException se)
                {
                    if (se.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionReset)
                    {
                        CloseConnection();
                        return;
                    }
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CloseConnection()
        {
            _client.Client.Close();
            _client.Client.Dispose();
            _client.Dispose();
            _client.Close();
            _onClose?.Invoke();
        }
    }
}