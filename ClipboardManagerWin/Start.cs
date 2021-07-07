using System;
using System.Threading;

namespace ClipboardManagerWindows
{
    class Start
    {
        private static int EXIT_CODE_SUCCESS = 0;
        private static int EXIT_CODE_ANOTHER_INSTANCE_RUNNING = 1;
        private static int EXIT_CODE_UNKNOWN_ERROR = 2;
        private static int EXIT_CODE_INVALID_ARGS = 3;

        private static void Main(string[] args)
        {
            try
            {
                int clipBoardManagerPort = -1;
                int unityServerPort = -1;
                if (args.Length >= 2)
                {
                    clipBoardManagerPort = int.Parse(args[0]);
                    unityServerPort = int.Parse(args[1]);
                }

                if (clipBoardManagerPort == -1 || unityServerPort == -1)
                {
                    Console.WriteLine("Invalid arguments received.");
                    Environment.Exit(EXIT_CODE_INVALID_ARGS);
                    return;
                }

                ClipboardApp clipboardApp = new ClipboardApp(unityServerPort);

                // Clipboard event listener
                Thread listenerThread = new Thread(() =>
                {
                    Console.WriteLine("Initializing clipboard change listener.");
                    clipboardApp.StartListening();
                });
                listenerThread.SetApartmentState(ApartmentState.STA);
                listenerThread.Start();

                // Using socket server as a session lock for this process as well :D
                Console.WriteLine("Initializing socket server.");

                Server server = new Server(clipBoardManagerPort, clipboardApp);
                int retCode = server.InitServer();
                if (retCode > 0)
                {
                    switch (retCode)
                    {
                        case 1:
                            Console.WriteLine($"Another instance of process already running port {clipBoardManagerPort}");
                            Environment.Exit(EXIT_CODE_ANOTHER_INSTANCE_RUNNING);
                            break;
                        case 2:
                            Console.WriteLine($"Failed to start server on port {clipBoardManagerPort}");
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
            catch(Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(EXIT_CODE_UNKNOWN_ERROR);
            }
        }
    }
}
