using System.Net;
using System.Net.Sockets;

namespace ChatApp
{
    public class App
    {
        private Reader? _reader;
        private Writer? _writer;

        private readonly TcpListener _listener;

        public App(int readPort)
        {
            _listener = new(IPAddress.Parse("127.0.0.1"), readPort);
            _listener.Start();
            BeginAcceptingClients();
        }

        public void InitializeWriter(int writePort)
        {
            _writer = new Writer(writePort);
        }

        private void BeginAcceptingClients()
        {
            Console.WriteLine($"Accepting connections on port {((IPEndPoint)_listener.LocalEndpoint).Port}");
            _listener.BeginAcceptTcpClient(AcceptClient, _listener);
        }

        private void AcceptClient(IAsyncResult result)
        {
            // Accepts the Connection request, creates a socket and a new TCP Client bound to this socket.
            // Socket can be found in client.Client.Handle.
            TcpClient readClient = _listener.EndAcceptTcpClient(result);
            _reader = new Reader(readClient);
            _reader.OnClose(BeginAcceptingClients);
            ThreadPool.QueueUserWorkItem(_reader.Read, readClient);
        }

        private void WriteFromConsole(object? obj)
        {
            if (_writer == null)
            {
                Console.WriteLine("ERROR: Please Initialize the messaging destination port before calling this function!");
                return;
            }
            while (true)
            {
                string message = Console.ReadLine() ?? "";
                _writer.Write(message);
            }
        }
        public void BeginWriting()
        {
            ThreadPool.QueueUserWorkItem(WriteFromConsole, null);
        }

        /// <summary>
        /// Signal from the parent process to close the app.
        /// </summary>
        public void CloseApp()
        {
            _reader?.CloseConnection();
            _writer?.Close();
        }
    }
}