using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClipboardManagerWindows
{
    class Server
    {
        private IPHostEntry ipHost;
        private IPAddress ipAddress;
        private IPEndPoint endPoint;

        private ClipboardApp clipboardApp;

        public Server(int port, ClipboardApp clipboardApp)
        {
            BuildEndpoint(port);
            this.clipboardApp = clipboardApp;
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

                    // timeout reads and writes in 5 seconds
                    clientSocket.ReceiveTimeout = 5000;
                    clientSocket.SendTimeout = 5000;

                    HandleClientAsync(clientSocket);
                }
            }
            catch (SocketException)
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
                catch (Exception e)
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
            int status = 1;

            try
            {
                if (message != null)
                {
                    JObject messageUnserialized = (JObject)JsonConvert.DeserializeObject(message);

                    // process my message, and modify response as needed
                    string messageType = messageUnserialized.GetValue("messageType")?.Value<string>() ?? "";
                    switch(messageType)
                    {
                        case "updateClipboard":

                            JObject update = messageUnserialized.GetValue("updateMessage")?.Value<JObject>();
                            clipboardApp.UpdateClipboard(update);
                            status = 0;

                            break;
                        default:

                            // type not supported

                            break;
                    }

                    status = 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            JObject response = new JObject
            {
                {"status", status }
            };

            var responseSerialized = JsonConvert.SerializeObject(response);
            return responseSerialized;
        }
    }
}
