using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClipboardManagerWindows
{
    class Client
    {
        private IPHostEntry ipHost;
        private IPAddress ipAddress;
        private IPEndPoint endPoint;

        public Client(int port)
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

        private string SendMessageSerialized(string message)
        {
            try
            {
                Socket serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // timeout read and write in 5 seconds
                serverSocket.ReceiveTimeout = 5000;
                serverSocket.SendTimeout = 5000;

                serverSocket.Connect(endPoint);
                
                Console.WriteLine($"Sending message [{message}].");

                byte[] messageSent = Encoding.ASCII.GetBytes(message);
                int byteSent = serverSocket.Send(messageSent);

                string messageReceived = Util.readFromSocket(serverSocket);

                Console.WriteLine($"Message received [{messageReceived}].");

                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();

                return messageReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public JObject SendMessage(JObject message)
        {
            try
            {
                string serializedMessage = JsonConvert.SerializeObject(message);

                string response = SendMessageSerialized(serializedMessage);
                if (response == null)
                    return null;

                JObject responseUnserialized = (JObject)JsonConvert.DeserializeObject(response);
                return responseUnserialized;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}
