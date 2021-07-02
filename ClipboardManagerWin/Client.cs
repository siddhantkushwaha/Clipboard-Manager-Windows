using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClipboardUtilityWindows
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
            ipHost = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHost.AddressList[0];
            endPoint = new IPEndPoint(ipAddress, port);
        }

        private string SendMessageSerialized(string message)
        {
            try
            {
                Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(endPoint);

                Console.WriteLine($"Sending message [{message}].");

                byte[] messageSent = Encoding.ASCII.GetBytes(message);
                int byteSent = sender.Send(messageSent);

                // ------------ TODO - repetetive code, might wanna encapsulate ------------
                string messageReceived = "";

                int bufferSize = 1;
                byte[] buffer = new byte[1024];

                while (bufferSize > 0)
                {
                    bufferSize = sender.Receive(buffer);
                    if (bufferSize > 0)
                    {
                        messageReceived += Encoding.ASCII.GetString(buffer, 0, bufferSize);
                    }
                }
                // -------------------------------------------------------------------------

                Console.WriteLine($"Message received [{messageReceived}].");

                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

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
