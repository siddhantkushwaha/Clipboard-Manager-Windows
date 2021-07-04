/*
    Source(s)
        1 - https://github.com/sudhakar3697/node-clipboard-event/blob/master/platform/clipboard-event-handler-win32.cs
        2 - https://docs.microsoft.com/en-us/windows/win32/dataxchg/clipboard
*/


using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

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

    public sealed class ClipboardApp
    {
        private class NotificationForm : Form
        {
            public NotificationForm()
            {
                //Turn the child window into a message-only window
                NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);

                //Place window in the system-maintained clipboard format listener list
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                try
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

                                    // TODO - send message to sync server to handle event
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

                    //Called for any unhandled messages
                    base.WndProc(ref m);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void Run()
        {
            Application.Run(new NotificationForm());
        }
    }
}
