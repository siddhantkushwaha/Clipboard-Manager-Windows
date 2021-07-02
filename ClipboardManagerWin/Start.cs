using System;
using System.Threading;

namespace ClipboardUtilityWindows
{
    class Start
    {
        private static void Main(string[] args)
        {
            // Clipboard event listener
            Thread listenerThread = new Thread(() => {

                Console.WriteLine("Initializing clipboard change listener.");
                ClipboardApp.Run();

            });
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.Start();

            // Socket server
            Thread sockeServerThread = new Thread(() => {

                Console.WriteLine("Initializing socket server.");
                Server server = new Server(1625);
                server.InitServer();

            });
            sockeServerThread.Start();

            listenerThread.Join();
            sockeServerThread.Join();
        }
    }
}
