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

        public void Close()
        {
            _client.Client.Close();
            _client.Client.Dispose();
            _client.Close();
            _client.Dispose();
        }
    }
}