using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClipboardUtilityWindows
{
    class Server
    {
        private IPHostEntry ipHost;
        private IPAddress ipAddress;
        private IPEndPoint endPoint;

        public Server(int port)
        {
            BuildEndpoint(port);
        }

        private void BuildEndpoint(int port)
        {
            ipHost = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHost.AddressList[0];
            endPoint = new IPEndPoint(ipAddress, port);
        }

        public void InitServer()
        {
            try
            {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endPoint);

                listener.Listen(1);

                while (true)
                {
                    Console.WriteLine($"Unity socket server active on port [{endPoint.Port}].");
                    Socket clientSocket = listener.Accept();

                    // ------------ TODO - repetetive code, might wanna encapsulate ------------
                    string messageReceived = "";

                    int bufferSize = 1;
                    byte[] buffer = new byte[1024];

                    while (bufferSize > 0)
                    {
                        bufferSize = clientSocket.Receive(buffer);
                        if (bufferSize > 0)
                        {
                            messageReceived += Encoding.ASCII.GetString(buffer, 0, bufferSize);
                        }
                    }
                    // -------------------------------------------------------------------------

                    Console.WriteLine($"Message received [{messageReceived}].");

                    // implementing this via thread because this is not an asynchronous SocketServer like NodeJS
                    HandleMessageAsync(clientSocket, messageReceived);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void HandleMessageAsync(Socket clientSocket, string message)
        {
            Thread handlerThread = new Thread(() => {
                HandleMessage(clientSocket, message);
            });
            handlerThread.Start();
        }

        private void HandleMessage(Socket clientSocket, string message)
        {
            try
            {
                JObject messageUnserialized = (JObject)JsonConvert.DeserializeObject(message);

                // process my message, and send response

                JObject response = new JObject();
                response.Add("status", 0);

                var responseSerialized = JsonConvert.SerializeObject(response);
                byte[] responseBytes = Encoding.ASCII.GetBytes(responseSerialized);
                int byteSent = clientSocket.Send(responseBytes);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
