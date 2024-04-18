using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace ChatApp
{
    public class Writer
    {
        private TcpClient _client;
        private readonly int _port;
        private string _appName;
        private const string _localAddress = "127.0.0.1";
        public Writer(int port, string appName)
        {
            _port = port;
            _appName = appName;
            _client = new TcpClient(_localAddress, _port);
            SetWriterNameToDirectory();
            Console.WriteLine("Connected to the destination. You can now type messages below to be sent to the client.");
        }

        private void SetWriterNameToDirectory()
        {
            string jsonResult = File.ReadAllText("nameDirectory.json");
            Dictionary<string, string>? nameDirectory = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResult);
            if (_client.Client?.LocalEndPoint == null)
            {
                return;
            }
            IPEndPoint localIp = (IPEndPoint)_client.Client.LocalEndPoint;
            string localEndPoint = $"{localIp.Address.MapToIPv4().ToString()}:{localIp.Port}";
            if (nameDirectory == null)
            {
                return;
            }
            if (nameDirectory.ContainsKey(localEndPoint))
            {
                nameDirectory[localEndPoint] = _appName;
            }
            else
            {
                nameDirectory.Add(localEndPoint, _appName);
            }
            File.WriteAllText("nameDirectory.json", JsonConvert.SerializeObject(nameDirectory));

        }

        public void Write(string message)
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                StreamWriter writer = new(stream, Encoding.ASCII);
                writer.WriteLine(message);
                writer.Flush();
                if (message.StartsWith("transfer"))
                {
                    Console.WriteLine($"Received command {message}");
                    string pathName = message.Split(" ")[1];
                    WriteFileToNetworkStream(pathName);
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException se)
                {
                    if (se.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionRefused or SocketError.ConnectionReset)
                    {
                        Console.WriteLine("ERROR: Unable to send the message. It looks like the destination has closed the connection.");
                        Console.WriteLine("Closing the connection safely on this end.");
                        Close();
                        return;
                    }
                }
                throw ex;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void WriteFileToNetworkStream(string pathName)
        {
            NetworkStream dataStream = _client.GetStream();
            FileStream fs = new(pathName, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[1024];
            long total = 0;
            int count;
            while ((count = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                dataStream.Write(buffer, 0, count);
                dataStream.Flush();
                total += count;
            }
            dataStream.Close();
            fs.Close();
            _client = new TcpClient("127.0.0.1", _port);
            SetWriterNameToDirectory();
        }

        public void Close()
        {
            _client.Client.Close();
            _client.Client.Dispose();
            _client.Close();
            _client.Dispose();
        }
    }
}