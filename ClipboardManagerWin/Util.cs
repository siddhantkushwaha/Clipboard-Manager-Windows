using System;
using System.Net.Sockets;
using System.Text;

namespace ClipboardUtilityWindows
{
    class Util
    {
        public static string readFromSocket(Socket socket)
        {
            try
            {
                string messageReceived = "";

                int maxBufferSize = 1024;
                int bufferSize = maxBufferSize;
                byte[] buffer = new byte[maxBufferSize];

                while (bufferSize >= maxBufferSize)
                {
                    bufferSize = socket.Receive(buffer);
                    if (bufferSize > 0)
                    {
                        messageReceived += Encoding.ASCII.GetString(buffer, 0, bufferSize);
                    }
                }

                return messageReceived;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            
            return null;
        }
    }
}
