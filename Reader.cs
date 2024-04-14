using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatApp
{
    public class Reader
    {
        private readonly TcpClient _client;
        private Action? _onClose;
        private bool _readingFile = false;
        private string? _fileName;
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
                    if (line.StartsWith("transfer"))
                    {
                        ReadFile(line.Split(" ")[1], stream);
                    }
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

        private void ReadFile(string pathName, NetworkStream dataStream)
        {
            FileStream fs = new("new" + pathName, FileMode.OpenOrCreate, FileAccess.Write);
            byte[] buffer = new byte[1024];
            int count;
            while ((count = dataStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string bufStr = Encoding.ASCII.GetString(buffer);
                if (bufStr.StartsWith("EOF"))
                {
                    // var buf = bufStr.Substring(3);
                    // fs.Write(Encoding.ASCII.GetBytes(buf), 0, count);
                    // fs.Flush();
                    break;
                }
                fs.Write(buffer, 0, count);
                fs.Flush();
            }
            Console.WriteLine("Completed file storage.");
            // fs.Flush();
            fs.Close();
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