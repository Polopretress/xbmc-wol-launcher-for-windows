using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace XBMCLauncher
{

    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        public static CustomApplicationContext applicationContext;
        public static Launcher l;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            l = new Launcher();
            if (l.StartListener())
            {
                applicationContext = new CustomApplicationContext();
                Application.Run(applicationContext);
            }
        }

        public static void CleanAndExit()
        {
            if (l != null) l.StopListener();
            Log.EndLogging();
            if (applicationContext != null) applicationContext.ExitThread();
        }

    }
     
    public class CustomApplicationContext : ApplicationContext
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenu contextMenu;
        private System.ComponentModel.IContainer components;
        private Icon lstnOn;
        private Icon lstnOff;

        public CustomApplicationContext()
        {
            InitializeContext();
        }

        private void InitializeContext()
        {
            components = new System.ComponentModel.Container();
            contextMenu = new System.Windows.Forms.ContextMenu();

            // Initialize menuItem0
            System.Windows.Forms.MenuItem menuItem0 = new System.Windows.Forms.MenuItem();
            menuItem0.Index = 0;
            menuItem0.Text = "Enable";
            menuItem0.Click += new System.EventHandler(this.lstnToggle);
            menuItem0.Checked = true;

            // Initialize menuItem1
            System.Windows.Forms.MenuItem menuItem1 = new System.Windows.Forms.MenuItem();
            menuItem1.Index = 1;
            menuItem1.Text = "-";

            // Initialize menuItem2
            System.Windows.Forms.MenuItem menuItem2 = new System.Windows.Forms.MenuItem();
            menuItem2.Index = 2;
            menuItem2.Text = "Start XBMC";
            menuItem2.Click += new System.EventHandler(this.StartXBMC);

            // Initialize menuItem3
            System.Windows.Forms.MenuItem menuItem3 = new System.Windows.Forms.MenuItem();
            menuItem3.Index = 3;
            menuItem3.Text = "TopMost";
            menuItem3.Click += new System.EventHandler(this.TopMost);
            
            // Initialize menuItem4
            System.Windows.Forms.MenuItem menuItem4 = new System.Windows.Forms.MenuItem();
            menuItem4.Index = 4;
            menuItem4.Text = "-";

            // Initialize menuItem5
            System.Windows.Forms.MenuItem menuItem5 = new System.Windows.Forms.MenuItem();
            menuItem5.Index = 5;
            menuItem5.Text = "Reload configuration";
            menuItem5.Click += new System.EventHandler(this.lstnReload);

            // Initialize menuItem6
            System.Windows.Forms.MenuItem menuItem6 = new System.Windows.Forms.MenuItem();
            menuItem6.Index = 6;
            menuItem6.Text = "Change \"xbmc.exe\" path";
            menuItem6.Click += new System.EventHandler(this.ChgXBMCPath);

            // Initialize menuItem7
            System.Windows.Forms.MenuItem menuItem7 = new System.Windows.Forms.MenuItem();
            menuItem7.Index = 7;
            menuItem7.Text = "-";

            // Initialize menuItem8
            System.Windows.Forms.MenuItem menuItem8 = new System.Windows.Forms.MenuItem();
            menuItem8.Index = 8;
            menuItem8.DefaultItem = true;
            menuItem8.Text = "Close XBMC Android Remote Launcher";
            menuItem8.Click += new System.EventHandler(this.exitItem_Click);

            // Initialize contextMenu
            contextMenu.MenuItems.AddRange(
            new System.Windows.Forms.MenuItem[] { menuItem0, menuItem1, menuItem2, menuItem3, menuItem4, menuItem5, menuItem6, menuItem7, menuItem8 });

            Stream s = this.GetType().Assembly.GetManifestResourceStream("XBMClauncher.xbmc.png");
            Bitmap PngIcon = new Bitmap(s);
            s.Close();
            lstnOn = Icon.FromHandle(PngIcon.GetHicon());
            s = this.GetType().Assembly.GetManifestResourceStream("XBMClauncher.xbmc-off.png");
            PngIcon = new Bitmap(s);
            s.Close();
            lstnOff = Icon.FromHandle(PngIcon.GetHicon());


            notifyIcon = new NotifyIcon(components)
            {
                ContextMenu = contextMenu,
                Icon = lstnOn,
                Text = "XBMC Android Launcher",
                Visible = true
            };

            notifyIcon.DoubleClick += this.StartXBMC;
            contextMenu.Popup += this.SetContextMenuProperties;

        }

        private void SetContextMenuProperties(object sender, EventArgs e)
        {
            contextMenu.MenuItems[3].Checked = Program.l.c.TopMost;
        }

        public void StartXBMC(object sender, EventArgs e)
        {
            Program.l.StartXBMC();
        }

        private void TopMost(object sender, EventArgs e)
        {
            if (contextMenu.MenuItems[3].Checked)
            {
                contextMenu.MenuItems[3].Checked = false;
                Program.l.c.TopMost = false;
            }
            else
            {
                contextMenu.MenuItems[3].Checked = true;
                Program.l.c.TopMost = true;
            }
            //Program.l.c.TopMost = contextMenu.MenuItems[3].Checked;
            Program.l.c.Save();
        }

        public void lstnIsOn()
        {
            contextMenu.MenuItems[0].Checked = true;
            notifyIcon.Icon = lstnOn;
        }
        public void lstnIsOff()
        {
            contextMenu.MenuItems[0].Checked = false;
            notifyIcon.Icon = lstnOff;
        }

        private void lstnToggle(object sender, EventArgs e)
        {
            if (contextMenu.MenuItems[0].Checked)
            {
                Program.l.StopListener();
                lstnIsOff();
            }
            else
            {
                Program.l.StartListener();
                lstnIsOn();
            }            
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            Program.CleanAndExit();
        }
        private void lstnReload(object sender, EventArgs e)
        {
            Program.l.Reload();
            lstnIsOn();
        }
        private void ChgXBMCPath(object sender, EventArgs e)
        {
            Config.ChangeXBMCPath(Program.l.c);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) { components.Dispose(); }
        }

        protected override void ExitThreadCore()
        {
            //if (mainForm != null) { mainForm.Close(); }
            notifyIcon.Visible = false; // should remove lingering tray icon!
            base.ExitThreadCore();
        }



    }

    public class Launcher
    {
        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            SynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);

        public class UdpState
        {
            public IPEndPoint e;
            public UdpClient u;
        }

        private UdpState s;
        public Config c;
        private EventWaitHandle waitHandle;
        volatile bool _quiting;

        public Launcher()
        {
            // let's check if there is XBMClauncher already running - if there is kill it and run new one (this one)
            // user won't get annoying UDP listener is already used message and don't need to kill it manually
            System.Diagnostics.Process[] prcs = System.Diagnostics.Process.GetProcessesByName("XBMCLauncher");
            int cpid = System.Diagnostics.Process.GetCurrentProcess().Id;
            foreach (var proc in prcs)
            {
                if (proc.Id != cpid)
                {
                    proc.Kill();
                }
            }
            // Load Config
            c = Config.Load();
            s = new UdpState();

            waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        }

        public void Reload()
        {
            StopListener();
            c = Config.Load();
            StartListener();
        }

        public void StopListener()
        {
            bool ThrdReleased;
            try
            {
                waitHandle.Reset();
                _quiting = true;
                s.u.Close();

                ThrdReleased = waitHandle.WaitOne(2000, false);
                if (ThrdReleased)
                {
                    Log.LogLine("Listener Exited Successfully.");
                }
                else
                {
                    Log.LogLine("Unable to properly close the listener: Wait timeout!!!");
                }
            }
            catch
            {
                Log.LogLine("Unable to properly close the listener.");
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
            bool MatchMac = false;

            try
            {
                Byte[] receiveBytes = u.EndReceive(ar, ref e);

                if (receiveBytes.Length > 0)
                {
                    Log.LogLine("Remote sent WOL Packet on listening port [packet lenth {0}]", receiveBytes.Length);

                    StringBuilder hex = new StringBuilder(receiveBytes.Length * 2);
                    //foreach (byte b in receiveBytes) hex.AppendFormat("{0:x2}:", b);
                    for (int b = 6; b < 12; b++) hex.AppendFormat("{0:x2}", receiveBytes[b]);

                    foreach (System.Net.NetworkInformation.NetworkInterface Nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        //Console.Write(Nic.OperationalStatus + " - " + Nic.GetPhysicalAddress().ToString() + "\n");
                        if (hex.ToString().ToUpper() == Nic.GetPhysicalAddress().ToString().ToUpper())
                        {
                            Log.LogLine("[{0}] Magic packet match host MAC Address", hex.ToString().ToUpper());
                            MatchMac = true;
                        }
                    }

                    // Start XBMC Only if MAC Address Match
                    if (MatchMac)
                    {
                        StartXBMC();
                    }
                    else
                    {
                        Log.LogLine("[{0}] Magic packet doesn't match host MAC Address: Ignoring WOL request", hex.ToString().ToUpper());
                    }

                    // BeginReceive Loop
                    s.e = e;
                    s.u = u;
                    u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
                    Log.LogLine("Now listening on UDP port {0}", c.wake_on_lan_port);
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (_quiting)
                {
                    Log.LogLine("No longer listening on UDP port {0}", c.wake_on_lan_port);
                    waitHandle.Set();
                    return;
                }
                Log.LogLine("[ObjectDisposedException] " + ex);
                StartListener();
            }
            catch (SocketException ex)
            {
                Log.LogLine("[SocketException] " + ex);
                StartListener();
            }    
            catch (Exception)
            {
                // BeginReceive Loop
                s.e = e;
                s.u = u;
                u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
            }
        }

        public void StartXBMC()
        {
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo(c.path);

                Log.LogLine("Starting XBMC...");
                p.Start();

                while (p.MainWindowHandle == IntPtr.Zero)
                {
                    Thread.Sleep(200);
                    p.Refresh();
                }

                Log.LogLine("Main Window Handle {0}", p.MainWindowHandle.ToInt32());
                switch (c.bring_window_to_front_mode)
                {
                    case 1:
                        if (c.TopMost)
                        {
                            Log.LogLine("SetWindowPos TopMost {0}", p.MainWindowHandle.ToInt32());
                            SetWindowPos(p.MainWindowHandle, HWND_TOPMOST, 50, 50, 500, 500, SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize);
                        }
                        else
                        {
                            Log.LogLine("SetWindowPos {0}", p.MainWindowHandle.ToInt32());
                            SetWindowPos(p.MainWindowHandle, HWND_TOP, 50, 50, 500, 500, SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize);
                        }
                        break;
                    case 2:
                        Log.LogLine("BringWindowToTop {0}", p.MainWindowHandle.ToInt32());
                        BringWindowToTop(p.MainWindowHandle);
                        break;
                    case 3:
                        Log.LogLine("SetForegroundWindow {0}", p.MainWindowHandle.ToInt32());
                        SetForegroundWindow(p.MainWindowHandle);
                        break;
                    case 4:
                        Log.LogLine("SetFocus {0}", p.MainWindowHandle.ToInt32());
                        SetFocus(p.MainWindowHandle);
                        break;
                }
            }
            catch (InvalidOperationException)
            {
                Log.LogLine("XBMC is already running!");
            }
        }

        public bool StartListener()
        {
            if (c != null)
            {
                Log.LogLine("Starting Listener...");
                if (PortAvailable(c.wake_on_lan_port))
                {
                    IPEndPoint e = new IPEndPoint(IPAddress.Any, c.wake_on_lan_port);
                    UdpClient u = new UdpClient(e);

                    s.e = e;
                    s.u = u;

                    u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
                    Log.LogLine("Now listening on UDP port {0}", c.wake_on_lan_port);
                    return true;
                }
                else
                {
                    Log.LogAndMessegaBox("Another process is already listening on UDP port ({0}).", c.wake_on_lan_port);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        

        private static bool PortAvailable(int p)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] udps = ipGlobalProperties.GetActiveUdpListeners();

            foreach (var endpoint in udps)
            {
                if (endpoint.Port == p)
                    return false;
            }

            return true;
        }
    }

    public class Log
    {
        private static TextWriter logWriter = null;
        private static BlockingCollection<string> logQueue = new BlockingCollection<string>();
        private static Task LogWriterTask = Task.Factory.StartNew(() => LogLineAsync());

        public static void LogLine(String txt, params object[] par)
        {
            logQueue.Add(string.Format(DateTime.Now.ToLongTimeString() + ": " + txt, par));
        }
        public static void EndLogging()
        {
            logQueue.CompleteAdding();
        }

        private static void LogLineAsync()
        {
             foreach (string logline in logQueue.GetConsumingEnumerable())
             {
                 try
                 {
                     if (logWriter == null)
                     {
                         if (File.Exists("XBMCLauncher_log.txt"))
                        {
                            File.Delete("XBMCLauncher_log_old.txt");
                            File.Move("XBMCLauncher_log.txt", "XBMCLauncher_log_old.txt");
                        }
                        logWriter = new StreamWriter("XBMCLauncher_log.txt", false);
                        logWriter.WriteLine(DateTime.Now.ToString());
                     }
                     else
                     {
                        logWriter = new StreamWriter("XBMCLauncher_log.txt", true);
                     }
                     Console.WriteLine(logline);
                     logWriter.WriteLine(DateTime.Now.ToLongTimeString() + ": " + logline);
                     logWriter.Flush();
                     logWriter.Close();
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
             Console.WriteLine("Logger Endded");
        }

        public static void LogAndMessegaBox(String txt, params object[] par)
        {
            LogLine(txt, par);
            MessageBox.Show(String.Format(txt, par), "XBMC Android Remote Launcher");
        }
        public static void LogLine(Object o)
        {
            LogLine(o.ToString());
        }
    }

    
    public class Config
    {
        public int wake_on_lan_port = 9;
        public String path = @"path_to_xbmc.exe";
        public int bring_window_to_front_mode = 1;
        public bool TopMost = true;

        public bool Save(String profile = "XBMCLauncher_cfg")
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            try
            {
                //var stream = File.OpenWrite(String.Format("{0}.xml", profile)); >> Doesn't empty conf file when updating conf 
                FileInfo file2write = new FileInfo(String.Format("{0}.xml", profile));
                FileStream stream = file2write.Open (FileMode.Create, FileAccess.Write, FileShare.None) ;
                serializer.Serialize(stream, this);
                stream.Flush();
                stream.Close();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Log.LogAndMessegaBox("Couldn't create XBMCLauncher_cfg.xml file\nRun application as administrator (right click in application -> Run As Administrator).");
            }
            catch (Exception e)
            {

                Log.LogLine(e);

            }

            return false;
        }

        public static Config ChangeXBMCPath(Config cfg)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XBMC Application|xbmc.exe";
            ofd.CheckFileExists = true;
            ofd.InitialDirectory = cfg.path;
            ofd.Title = "Select XBMC's path"; 
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                cfg.path = ofd.FileName;
                cfg.Save();  
            }
            return cfg;
        }

        [STAThread]
        public static Config Load(String profile = "XBMCLauncher_cfg")
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                var stream = File.OpenRead(String.Format("{0}.xml", profile));
                Object o = serializer.Deserialize(stream);
                stream.Close();


                Config cfg = o as Config;
                if (cfg != null)
                {
                    Log.LogLine("Configuration loaded");
                    if (!File.Exists(cfg.path))
                    {
                        String msg = "File specified in XBMCLauncher_cfg.xml doesnt exist - please check Your settings and restart XBMCLauncher";
                        Log.LogLine(msg);
                        if (MessageBox.Show(msg + "\n\nWould You like to set path to XBMC.exe now?", "File not found", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            OpenFileDialog ofd = new OpenFileDialog();
                            ofd.Filter = "XBMC Application|xbmc.exe";
                            ofd.CheckFileExists = true;
                            ofd.InitialDirectory = cfg.path;
                            ofd.Title = "Select XBMC's path";
                            if (ofd.ShowDialog() == DialogResult.OK)
                            {
                                cfg.path = ofd.FileName;
                                cfg.Save();
                            }
                            else
                            {
                                Program.CleanAndExit();
                            }
                        }
                        else
                        {
                            Program.CleanAndExit();
                        }
                    }
                }
                return cfg;
            }
            catch (FileNotFoundException)
            {
                Config cfg = new Config();

                if (cfg.Save())
                {
                    String msg = "XBMCLauncher couldn't find XBMCLauncher_cfg.xml - new one is created.";
                    Log.LogLine(msg);
                    if (MessageBox.Show(msg + "\n\nWould You like to set path to XBMC.exe now?", "File not found", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Filter = "XBMC Application|xbmc.exe";
                        ofd.CheckFileExists = true;
                        ofd.InitialDirectory = cfg.path;
                        ofd.Title = "Select XBMC's path";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            cfg.path = ofd.FileName;
                            cfg.Save();
                            return cfg;
                        }
                        else
                        {
                            Log.LogAndMessegaBox("Update XBMCLauncher_cfg.xml and restart XBMCLauncher");
                            Program.CleanAndExit();
                        }
                    }
                    else
                    {
                        Log.LogAndMessegaBox("Update XBMCLauncher_cfg.xml and restart XBMCLauncher");
                        Program.CleanAndExit();
                    }
                }
                else
                    Program.CleanAndExit();
            }
            catch (Exception e)
            {
                Log.LogAndMessegaBox("Cannot Load Configuration:\n{0}", e.Message);
                Program.CleanAndExit();
            }

            return null;
        }
    }
}
