using System;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Toolkit.Uwp.Notifications;

/* Test Cases:
 Single monitor - activate: should create notification, and do nothing
 2 monitors - unplug external monitor: should close any existing 'pause' windows
 2 monitors, mirrored - activate: pause to external monitor (screenshot -> extend monitors -> show screenshot on secondary)
 2 monitors, extended - activate: unpause (close existing 'pause' windows, mirror monitors)
 Case to test that 'pause' image appears fullscreen (this requires setting the extended mode to test case before switching to mirroring):
    both monitors same resolution
    internal monitor low res (e.g. 1280x720), ext monitor high res (e.g. 1920x1080)
    internal monitor high res (e.g. 2560x1440), ext monitor low res (e.g. 1920x1080)
    Need to check a range of display scaling options
    Need to check a range of second screen placement options - left, right, above, below, diagonal.
*/
//TODO - autostart on login (perhaps via installer?) - shortcut to Start Menu\Programs\Startup folder is easiest
//TODO - Intro page

// https://stackoverflow.com/questions/362986/capture-the-screen-into-a-bitmap

// https://stackoverflow.com/questions/69083184/change-between-duplicate-mirror-and-extend-display-modes-in-c

// Need to add <UseWPF>true</UseWPF> to .csproj file to enable hotkey stuff
namespace PauseToScreen
{
    class Program
    {
        const int ENUM_CURRENT_SETTINGS = -1;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            GlobalHotKey.RegisterHotKey("Ctrl + Shift + P", () => HandleHotKey());
            var hotplug = new DisplayHotplugDetect(HidePauseForm);
            var notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Pause/Unpause", null, (s, e) => HandleHotKey());
            notifyIcon.ContextMenuStrip.Items.Add("Help", null, ShowHelp);
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null,(s, e) => Application.Exit());
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Text = "Pause To Screen";
            notifyIcon.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) HandleHotKey(); };
            notifyIcon.Visible = true;

            Application.Run(new ApplicationContext()); 
        }
        public static void HandleMonitorHotplug()
        {
            switch (GetMonitorTopology())
            {
                case CCDWrapper.DisplayConfigTopologyId.External: //Only one monitor
                case CCDWrapper.DisplayConfigTopologyId.Internal:
                    HidePauseForm();
                    break;
            }
        }

        public static void ShowHelp(object s, EventArgs e)
        {
            Form HelpForm = new Form();
            TextBox textBox1 = new System.Windows.Forms.TextBox();
            Button button1 = new System.Windows.Forms.Button();
            Button button2 = new System.Windows.Forms.Button();
            HelpForm.SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(15, 15);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(765, 26);
            textBox1.TabIndex = 0;
            textBox1.Text = "This is some text";
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(113, 378);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(163, 60);
            button1.TabIndex = 1;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(551, 378);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(161, 60);
            button2.TabIndex = 2;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            HelpForm.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            HelpForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            HelpForm.ClientSize = new System.Drawing.Size(800, 576);
            HelpForm.Controls.Add(button2);
            HelpForm.Controls.Add(button1);
            HelpForm.Controls.Add(textBox1);
            HelpForm.Name = "Form1";
        }

        public static void HandleHotKey()
        {
            //Possible states:
            // 1) Single Monitor only: do nothing (perhaps a popup/notification?)
            // 2) Mirrored: Pause action (screenshot, switch to extend, show screenshot on secondary)
            // 3) Extended: Unpause action (close screenshot window, switch to mirror)

            switch (GetMonitorTopology())
            {
                case CCDWrapper.DisplayConfigTopologyId.Clone:
                    Pause();
                    break;
                case CCDWrapper.DisplayConfigTopologyId.Extend:
                    UnPause();
                    break;
                case CCDWrapper.DisplayConfigTopologyId.External:  //Only one monitor
                case CCDWrapper.DisplayConfigTopologyId.Internal:
                    new ToastContentBuilder().AddText("PauseToScreen only works when multiple screens are active")
                        .Show();
                    break; 
                default:  //No display? 
                    break;
            }
        }

        public static void HidePauseForm()
        {
            if (Application.OpenForms.Count > 0)
            {
                foreach (Form form in Application.OpenForms)
                {
                    form.Hide();
                }
            }
        }
        public static void UnPause()
        {
            CloneDisplays();
            HidePauseForm();
        }
        public static void Pause()
        {
            // Take screenshot
            Screen s = Screen.AllScreens[0];
            //This bit (DEVMODE -> EnumDisplaySettings) is needed to get screen resolution in pixels
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            // can probably use null instead of screen.DeviceName since we're supposed to be in mirroring mode
            EnumDisplaySettings(s.DeviceName, ENUM_CURRENT_SETTINGS, ref dm);

            Bitmap bmp = new Bitmap(dm.dmPelsWidth, dm.dmPelsHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(dm.dmPositionX, dm.dmPositionY, 0, 0, bmp.Size);
                // bmp.Save(s.DeviceName.Split('\\').Last() + ".png");
            }

            // Switch to extended mode
            ExtendDisplays();

            Form f = null;
            if (Application.OpenForms.Count > 0)
            {
                f = Application.OpenForms[0];
            }
            else
            {
                f = new Form();
            }
            f.Text = "Screen Pause";
            f.FormBorderStyle = FormBorderStyle.None;
            f.WindowState = FormWindowState.Maximized;

            int x = 0;
            int y = 0;
            int width = 0;
            int height = 0;
            Screen ss = Screen.AllScreens[0];
            while (System.Windows.Forms.SystemInformation.MonitorCount <= 1)  // TODO - don't do this if only one monitor connected
            {
                Thread.Sleep(1000);
            }

            typeof(Screen)
                .GetField("screens", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .SetValue(null,
                    null); // slightly evil way to clear AllScreens cache via https://stackoverflow.com/questions/5020559/screen-allscreen-is-not-giving-the-correct-monitor-count

            foreach (Screen screen in
                Screen.AllScreens) // Screen.AllScreens is incorrect if monitor count changes during runtime https://stackoverflow.com/questions/5020559/screen-allscreen-is-not-giving-the-correct-monitor-count
            {
                // Need to use EnumDIsplayMonitors instead
                Console.WriteLine(screen.DeviceName);
                if (!screen.Primary)
                {
                    ss = screen;
                    x = screen.Bounds.X;
                    y = screen.Bounds.Y;
                    width = screen.Bounds.Width;
                    height = screen.Bounds.Height;
                }
            }

            //f.Location = ss.WorkingArea.Location;
            //width = 3840;
            //height = 2160;
            f.SetBounds(x, y, width, height);
            // f.Height = dm.dmPelsHeight;
            // f.Width = dm.dmPelsWidth;
            f.Height = height;
            f.Width = width;
            f.StartPosition = FormStartPosition.Manual;
            PictureBox p = new PictureBox();
            p.Dock = DockStyle.Fill;

            p.SizeMode = PictureBoxSizeMode.AutoSize;
            //p.Height = height;
            //p.Width = width;
            p.Image = new Bitmap(bmp, width, height); //this seems necessary for going up resolution
            //p.Image = bmp; // and this one for keeping the same (or possibly going down - untested!) resolution
            f.Controls.Add(p);

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            //f.AutoScaleMode = AutoScaleMode.None;
            //f.PerformAutoScale();
            f.ShowDialog();
        }

        // Allows us to read monitor scaling factor
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode,
            int dwFlags);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayDevices(string lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [DllImport("user32.dll")]
        public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
            ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ChangeDisplaySettings(uint lpDevMode, uint dwflags);


        
        public static void EnableSecondaryDisplay()
        {
            var secondaryIndex = 1;
            var secondary = GetDisplayDevice(secondaryIndex);
            var id = secondary.DeviceKey.Split('\\')[7];

            using (var key =
                Registry.CurrentConfig.OpenSubKey(string.Format(@"System\CurrentControlSet\Control\VIDEO\{0}", id),
                    true))
            {
                using (var subkey = key.CreateSubKey("000" + secondaryIndex))
                {
                    subkey.SetValue("Attach.ToDesktop", 1, RegistryValueKind.DWord);
                    subkey.SetValue("Attach.RelativeX", 1024, RegistryValueKind.DWord);
                    subkey.SetValue("DefaultSettings.XResolution", 1024, RegistryValueKind.DWord);
                    subkey.SetValue("DefaultSettings.YResolution", 768, RegistryValueKind.DWord);
                    subkey.SetValue("DefaultSettings.BitsPerPel", 32, RegistryValueKind.DWord);
                }
            }

            ChangeDisplaySettings(0, 0);
        }

        private static DISPLAY_DEVICE GetDisplayDevice(int id)
        {
            var d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);
            if (!EnumDisplayDevices(null, id, ref d, 0))
                throw new NotSupportedException("Could not find a monitor with id " + id);
            return d;
        }

        private static CCDWrapper.DisplayConfigTopologyId GetMonitorTopology() 
        {
            //From https://stackoverflow.com/questions/16082330/communicating-with-windows7-display-api via https://stackoverflow.com/questions/22258906/how-to-detect-duplicated-monitors-as-separate-screens
            int numPathArrayElements;
            int numModeInfoArrayElements;
            CCDWrapper.DisplayConfigTopologyId currentTopologyId = CCDWrapper.DisplayConfigTopologyId.Zero; 
            
            // query active paths from the current computer.
            if (CCDWrapper.GetDisplayConfigBufferSizes(CCDWrapper.QueryDisplayFlags.OnlyActivePaths, out numPathArrayElements,
                out numModeInfoArrayElements) == 0)
            {
                // 0 is success.
                var pathInfoArray = new CCDWrapper.DisplayConfigPathInfo[numPathArrayElements];
                var modeInfoArray = new CCDWrapper.DisplayConfigModeInfo[numModeInfoArrayElements];

                var first = Marshal.SizeOf(new CCDWrapper.DisplayConfigPathInfo());
                var second = Marshal.SizeOf(new CCDWrapper.DisplayConfigModeInfo());
                //var status = CCDWrapper.QueryDisplayConfig(CCDWrapper.QueryDisplayFlags.OnlyActivePaths,
               //     ref numPathArrayElements, pathInfoArray, ref numModeInfoArrayElements, modeInfoArray, IntPtr.Zero);
                var status = CCDWrapper.QueryDisplayConfig( CCDWrapper.QueryDisplayFlags.DatabaseCurrent,
                    ref numPathArrayElements, pathInfoArray, ref numModeInfoArrayElements, modeInfoArray, ref currentTopologyId);
            }

            return currentTopologyId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)] public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            [MarshalAs(UnmanagedType.U4)] public DisplayDeviceStateFlags StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,

            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,

            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,

            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,

            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,

            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        enum DISP_CHANGE : int
        {
            Successful = 0,
            Restart = 1,
            Failed = -1,
            BadMode = -2,
            NotUpdated = -3,
            BadFlags = -4,
            BadParam = -5,
            BadDualView = -6
        }

        [Flags]
        public enum SetDisplayConfigFlags : uint
        {
            SDC_TOPOLOGY_INTERNAL = 0x00000001,
            SDC_TOPOLOGY_CLONE = 0x00000002,
            SDC_TOPOLOGY_EXTEND = 0x00000004,
            SDC_TOPOLOGY_EXTERNAL = 0x00000008,
            SDC_APPLY = 0x00000080
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern long SetDisplayConfig(uint numPathArrayElements,
            IntPtr pathArray, uint numModeArrayElements, IntPtr modeArray, SetDisplayConfigFlags flags);

        static void CloneDisplays()
        {
            SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero,
                SetDisplayConfigFlags.SDC_TOPOLOGY_CLONE | SetDisplayConfigFlags.SDC_APPLY);
        }

        static void ExtendDisplays()
        {
            SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero,
                SetDisplayConfigFlags.SDC_TOPOLOGY_EXTEND | SetDisplayConfigFlags.SDC_APPLY);
        }

        static void ExternalDisplay()
        {
            SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero,
                SetDisplayConfigFlags.SDC_TOPOLOGY_EXTERNAL | SetDisplayConfigFlags.SDC_APPLY);
        }

        static void InternalDisplay()
        {
            SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero,
                SetDisplayConfigFlags.SDC_TOPOLOGY_INTERNAL | SetDisplayConfigFlags.SDC_APPLY);
        }

        [Flags()]
        public enum ChangeDisplaySettingsFlags : uint
        {
            CDS_NONE = 0,
            CDS_UPDATEREGISTRY = 0x00000001,
            CDS_TEST = 0x00000002,
            CDS_FULLSCREEN = 0x00000004,
            CDS_GLOBAL = 0x00000008,
            CDS_SET_PRIMARY = 0x00000010,
            CDS_VIDEOPARAMETERS = 0x00000020,
            CDS_ENABLE_UNSAFE_MODES = 0x00000100,
            CDS_DISABLE_UNSAFE_MODES = 0x00000200,
            CDS_RESET = 0x40000000,
            CDS_RESET_EX = 0x20000000,
            CDS_NORESET = 0x10000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public int x;
            public int y;
        }
    }
}

