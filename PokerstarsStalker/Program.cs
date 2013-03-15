using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
//using ReadWriteMemory;


namespace ConsoleApplication1
{
    public delegate bool EnumWindowsProc(int hwnd, int lParam);


    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //[DllImport("coredll.dll")]
        //private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = System.Runtime.InteropServices.CharSet.Auto)] //
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam,int lparam);

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);



        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        const int VK_UP = 0x26;   //UP ARROW key 
        const int VK_DOWN = 0x28;   //DOWN ARROW key


        static int viewhandle = 0;
        static int listhandle = 0;

        static string text = "";

        static List<UInt64> ids = new List<UInt64>();

        public static void GetText()
        {
            uint scanCode = MapVirtualKey((uint) VK_DOWN, 0);
            int lParam = (int) (0x00000001 | (scanCode << 16));
            char[] delims = new char[] { '#', ':' };

            string[] idtext;
            UInt64 id;
            
            for (int i = 1; i < 2000; i++)
            {
                StringBuilder sb = new StringBuilder(1024);
                //GetWindowText(new IntPtr(hwnd), sb, sb.Capacity);

                if (!SendMessage(new IntPtr(viewhandle), WM_GETTEXT, sb.Capacity, sb))
                {
                    Console.WriteLine("Error");
                }
                Console.WriteLine(sb.ToString().Substring(0, 30));
                idtext = sb.ToString().Substring(0, 32).Split(delims, StringSplitOptions.RemoveEmptyEntries);

                if (idtext.Length == 3)
                {
                    id = (Convert.ToUInt64(idtext[1]));
                    if (ids.Any(x => x==id))
                    {
                        Console.WriteLine("Already in list");
                    }
                    else
                    {
                        ids.Add(id);
                        text = text + "\r\n" + sb.ToString();
                    }
                }

                System.Threading.Thread.Sleep(400);
                SendMessage(listhandle, WM_KEYDOWN, VK_DOWN, lParam);
                SendMessage(listhandle, WM_KEYUP, VK_DOWN, lParam);
            }
        }

        public static bool Report(int hwnd, int lParam)
        {
            string ViewClassName = "PokerStarsViewClass";
            string ItemListName = "PokerStarsListClass";

            //Console.WriteLine("Window handle is " + hwnd);
            StringBuilder ClassName = new StringBuilder(100);
            //Get the window class name
            int nRet = GetClassName(new IntPtr(hwnd), ClassName, ClassName.Capacity);
            if (nRet != 0)
            {
                if (ClassName.ToString().Contains(ItemListName))
                {
                    Console.WriteLine(ClassName);
                    listhandle = hwnd;
                }
                if (ClassName.ToString().Contains(ViewClassName))
                {
                    Console.WriteLine(ClassName);
                    viewhandle = hwnd;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static int FindWindow(string windowName, bool wait)
        {
            int hWnd = FindWindow(null, windowName).ToInt32();
            while (wait && hWnd == 0)
            {
                System.Threading.Thread.Sleep(500);
                hWnd = FindWindow(null, windowName).ToInt32();
            }

            EnumWindowsProc callBackPtr = new EnumWindowsProc(Program.Report);
            EnumWindows(callBackPtr, IntPtr.Zero);

            return hWnd;
        }

        // THE FOLLOWING METHOD REFERENCES THE SetForegroundWindow API
        public static bool BringWindowToTop(string windowName, bool wait)
        {
            int hWnd = FindWindow(windowName, wait);
            if (hWnd != 0)
            {
                return SetForegroundWindow((IntPtr)hWnd);
            }
            return false;
        }

        static void Main(string[] args)
        {

            string ProcessName = "PokerStars";
            string MainWindowName = "Instant";

            //ProcessMemory Mem = new ProcessMemory(ProcessName);

            Process[] processCollection = Process.GetProcessesByName(ProcessName);

            EnumWindowsProc callBackPtr = new EnumWindowsProc(Program.Report);

            if (processCollection != null && processCollection.Length >= 1 &&
            processCollection[0] != null)
            {
                IntPtr activeWindowHandle = GetForegroundWindow();
                foreach (Process p in processCollection)
                {
                    Console.WriteLine("PokerStars process:" + p.Handle);
                    //FindWindow("PokerStars Lobby", false);
                    Console.WriteLine("Title: " + p.MainWindowTitle);
                    if (p.MainWindowTitle.StartsWith(MainWindowName))
                    {
                        Console.WriteLine("Title: " + p.MainWindowTitle + "  Handle: " + p.MainWindowHandle);
                        EnumChildWindows(p.MainWindowHandle, callBackPtr, IntPtr.Zero);
                    }
                }
            }

            if (viewhandle != 0 && listhandle != 0)
            {
                GetText();
            }

            System.IO.File.WriteAllText(@"D:\my\download\datos_pokerstars.txt", text);
            Console.WriteLine("FIN");
            Console.ReadKey();
        }




        static void otro()
        {
            //Open Up blank Notepad First !
            string lpszParentClass = "Notepad";
            string lpszParentWindow = "Instant Hand History";
            string lpszClass = "Edit";

            System.Threading.Thread.Sleep(5000);

            IntPtr windowHandle = GetForegroundWindow();

            IntPtr ParenthWnd = new IntPtr(0);
            IntPtr hWnd = new IntPtr(0);
            ParenthWnd = FindWindow(lpszParentClass, lpszParentWindow);
            if (ParenthWnd.Equals(IntPtr.Zero))
                Console.WriteLine("Notepad Not Running");
            else
            {
                hWnd = FindWindowEx(ParenthWnd, hWnd, lpszClass, "");
                if (hWnd.Equals(IntPtr.Zero))
                    Console.WriteLine("Notepad doesn't have an edit component ?");
                else
                {
                    Console.WriteLine("Notepad Window: " + ParenthWnd.ToString());
                    Console.WriteLine("Edit Control: " + hWnd.ToString());
                }
            }
            Console.ReadKey();
        }
    }
}

