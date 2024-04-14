using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatApp
{
    public class Writer
    {
        private readonly TcpClient _client;
        private readonly int _port;
        public Writer(int port)
        {
            _port = port;
            _client = new TcpClient("127.0.0.1", _port);
            Console.WriteLine("Connected to the destination. You can now type messages below to be sent to the client.");
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
            // dataStream.Close();
            fs.Close();
            dataStream.Write(Encoding.ASCII.GetBytes("EOF"));
            dataStream.Flush();
            // dataStream.Dispose();
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