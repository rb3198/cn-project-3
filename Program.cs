namespace ChatApp
{
    public class Program
    {

        public static void Main(string[] args)
        {
            // Create a cancellation token source
            var exitEvent = new ManualResetEvent(false);
            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevents the process from terminating immediately
                cancellationTokenSource.Cancel(); // Cancel the cancellation token
                exitEvent.Set();
            };
            string appName = ""; int port = 8001;
            string? writePortInput = string.Empty; int writePort = -1;
            if (args.Length > 0)
            {
                appName = args[0] ?? appName;
                bool validPort = int.TryParse(args[1], out port);
                if (!validPort)
                {
                    Console.WriteLine("WARNING: No port number / invalid port number passed. Starting the app on port 8001.");
                }
            }
            App app = new(port);
            Console.WriteLine($"Welcome to the Chat App! You are talking as {appName} on port {port}.");
            Console.WriteLine("Please Enter the Receiver port number");
            bool isValidPort = false;
            while (!isValidPort)
            {
                writePortInput = Console.ReadLine();
                isValidPort = int.TryParse(writePortInput, out writePort);
                if (!isValidPort)
                {
                    Console.WriteLine("Invalid Port entered. It should be numeric.");
                }
            }
            if (writePort == -1)
            {
                Environment.Exit(0);
            }
            app.InitializeWriter(writePort);
            app.BeginWriting();
            exitEvent.WaitOne();
            Console.WriteLine("Safely terminating all the connections");
            app.CloseApp();
            Console.WriteLine("App Exit.");
        }
    }
}