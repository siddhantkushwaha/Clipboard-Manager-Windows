using System;
using System.Threading;

namespace ClipboardUtilityWindows
{
    class Start
    {
        private static int EXIT_CODE_SUCCESS = 0;
        private static int EXIT_CODE_ANOTHER_INSTANCE_RUNNING = 1;
        private static int EXIT_CODE_UNKNOWN_ERROR = 2;

        private static void Main(string[] args)
        {
            int port = 1625;
            if (args.Length > 0)
            {
                port = int.Parse(args[0]);
            }

            // Clipboard event listener
            Thread listenerThread = new Thread(() =>
            {

                Console.WriteLine("Initializing clipboard change listener.");
                ClipboardApp.Run();

            });
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.Start();

            // Using socket server as a session lock for this process as well :D
            Console.WriteLine("Initializing socket server.");

            Server server = new Server(port);
            int retCode = server.InitServer();
            if (retCode > 0)
            {
                switch (retCode)
                {
                    case 1:
                        Console.WriteLine($"Another instance of process already running port {port}");
                        Environment.Exit(EXIT_CODE_ANOTHER_INSTANCE_RUNNING);
                        break;
                    case 2:
                        Console.WriteLine($"Failed to start server on port {port}");
                        Environment.Exit(EXIT_CODE_UNKNOWN_ERROR);
                        break;
                    default:
                        Console.WriteLine($"Error code unknown {retCode}");
                        break;
                }
            }

            listenerThread.Join();
            Environment.Exit(EXIT_CODE_SUCCESS);
        }
    }
}
