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
            ipHost = Dns.GetHostEntry("localhost");

            // We want IPV4 address
            ipAddress = Array.Find(ipHost.AddressList, ip => ip.AddressFamily == AddressFamily.InterNetwork);

            endPoint = new IPEndPoint(ipAddress, port);
        }

        public int InitServer()
        {
            int returnCode = 0;
            try
            {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endPoint);

                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine($"Unity socket server active on port [{endPoint.Port}].");
                    Socket clientSocket = listener.Accept();

                    HandleClientAsync(clientSocket);
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                returnCode = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                returnCode = 2;
            }

            return returnCode;
        }

        private void HandleClientAsync(Socket clientSocket)
        {
            Thread handlerThread = new Thread(() =>
            {
                try
                {
                    Console.WriteLine("Reading from socket.");
                    string messageReceived = Util.readFromSocket(clientSocket);

                    Console.WriteLine($"Message received [{messageReceived}].");

                    string response = HandleMessage(messageReceived);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    int byteSent = clientSocket.Send(responseBytes);

                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            handlerThread.Start();
        }

        /* 
            Do not send null response from here.
        */
        private string HandleMessage(string message)
        {
            JObject response = new JObject();
            response.Add("status", 1);

            try
            {
                if (message != null)
                {
                    JObject messageUnserialized = (JObject)JsonConvert.DeserializeObject(message);

                    // process my message, and modify response as needed

                    response.Property("status").Remove();
                    response.Add("status", 0);
                }           
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var responseSerialized = JsonConvert.SerializeObject(response);
            return responseSerialized;
        }
    }
}
