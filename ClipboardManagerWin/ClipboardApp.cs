/*
    Source(s)
        1 - https://github.com/sudhakar3697/node-clipboard-event/blob/master/platform/clipboard-event-handler-win32.cs
        2 - https://docs.microsoft.com/en-us/windows/win32/dataxchg/clipboard
*/


using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace ClipboardManagerWindows
{
    internal static class NativeMethods
    {
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    }

    internal class NotificationForm : Form
    {
        private ClipboardApp clipboardApp;

        public NotificationForm(ClipboardApp clipboardApp)
        {
            this.clipboardApp = clipboardApp;

            //Turn the child window into a message-only window
            NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);

            //Place window in the system-maintained clipboard format listener list
            NativeMethods.AddClipboardFormatListener(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                lock (clipboardApp.GetLock())
                {
                    // handle clipboard update message               
                    switch (m.Msg)
                    {
                        case NativeMethods.WM_CLIPBOARDUPDATE:
                            {
                                // handle different data formats
                                // TODO - there possibly exist better ways of doing this, figure that out later if possible
                                if (Clipboard.ContainsText())
                                {
                                    Console.WriteLine("New text added to clipboard");

                                    var text = Clipboard.GetText();
                                    Console.WriteLine(text);

                                    clipboardApp.SendTextUpdate(text);
                                }
                                else if (Clipboard.ContainsFileDropList())
                                {
                                    // TODO - Handle files later :D, at least we know we can
                                    Console.WriteLine("Files were copied.");

                                    var fileDropList = Clipboard.GetFileDropList();
                                    foreach (var file in fileDropList)
                                    {
                                        Console.WriteLine(file);
                                    }
                                }
                                else if (Clipboard.ContainsImage())
                                {
                                    // TODO - handle images later too
                                    Console.WriteLine("Image was copied.");
                                }
                                else
                                {
                                    Console.WriteLine("Data format not supported as of now.");
                                }

                                break;
                            }
                        default:
                            {
                                // Update message not handled.
                                break;
                            }
                    }
                }

                //Called for any unhandled messages
                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void UpdateClipboard(string dataFormat, object data)
        {
            Clipboard.SetData(dataFormat, data);
        }
    }

    public sealed class ClipboardApp
    {
        private readonly object clipboardLock = new object();

        private NotificationForm notificationForm;
        private Client serverConnecion;
        private JObject lastSuccessfullUpdate = null;

        public ClipboardApp(int clipboardServerPort)
        {
            this.serverConnecion = new Client(clipboardServerPort);
        }

        private void SendUpdate(JObject update)
        {
            if (JToken.DeepEquals(lastSuccessfullUpdate, update))
            {
                Console.WriteLine("Matches with last update, discarding.");
                return;
            }

            JObject commandMessage = new JObject
            {
                {"messageType" , "syncMessage"},
                {"updateMessage", update }
            };

            JObject response = serverConnecion.SendMessage(commandMessage);
            Console.WriteLine(response);

            int status = response?.GetValue("status")?.Value<int>() ?? -1;
            if (status == 0)
            {
                lastSuccessfullUpdate = update;
            }
        }

        public object GetLock()
        {
            return clipboardLock;
        }

        public void StartListening()
        {
            notificationForm = new NotificationForm(this);
            Application.Run(notificationForm);
        }

        public void SendTextUpdate(string text)
        {
            JObject update = new JObject
            {
                { "type", 1 },
                { "text", text }
            };
            SendUpdate(update);
        }

        // Clipboard ops have to run in a single apartment thread on Windows
        // But we'll make it work like a synchronous call
        public void UpdateClipboard(JObject update)
        {
            lock (clipboardLock)
            {
                Console.WriteLine("Updating clipboard");

                int type = update.GetValue("type")?.Value<int>() ?? -1;

                object data = null;
                string format = null;

                switch(type)
                {
                    case 1:

                        // text
                        string text = update.GetValue("text")?.Value<string>() ?? "";
                        if (text.Length > 0)
                        {
                            format = DataFormats.Text;
                            data = text;
                        }

                        break;
                    case 2:

                        // files not supported yet

                        break;
                    default:

                        // do nothing

                        break;
                }

                if (notificationForm != null && format != null && data != null)
                {
                    Thread updateThread = new Thread(() =>
                    {
                        notificationForm?.UpdateClipboard(format, data);
                    });
                    updateThread.SetApartmentState(ApartmentState.STA);
                    updateThread.Start();
                    updateThread.Join();

                    lastSuccessfullUpdate = update;
                }
            }
        }
    }
}
