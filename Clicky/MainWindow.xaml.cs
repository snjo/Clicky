using Hotkeys;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Clicky
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        bool starting = true;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        int defaultDuration = 10;

        DispatcherTimer waitForStartTimer = new();
        DispatcherTimer clickDurationTimer = new();
        DispatcherTimer clickIntervalTimer = new();

        TaskbarItemInfo taskinfo = new System.Windows.Shell.TaskbarItemInfo();
        public MainWindow()
        {
            InitializeComponent();
            settings.Reload();
            ApplySettingsToControls();

            waitForStartTimer.Tick += new EventHandler(WaitForStart_Tick);
            clickIntervalTimer.Tick += new EventHandler(OnTimer);
            clickDurationTimer.Tick += new EventHandler(disableClickingEvent);

            this.TaskbarItemInfo = taskinfo;
            taskinfo.ProgressState = TaskbarItemProgressState.Normal;
            starting = false;
        }

        public IntPtr GetHandle()
        {
            return new WindowInteropHelper(this).Handle;
        }

        public void LoadHotkeysFromSetting(IntPtr handle)
        {
            HotkeyList = HotkeyTools.LoadHotkeys(HotkeyList, HotkeyNames, handle);
            if (settings.RegisterHotkeys) // optional
            {
                HotkeyTools.UpdateHotkeys(HotkeyList, HotkeyNames, handle);
            }
        }

        private void WaitForStart_Tick(object? sender, EventArgs e)
        {
            StartClicking();
        }

        Point MouseStartPos = new();
        private void OnTimer(object? sender, EventArgs e)
        {
            Point MousePos = GetMousePosition();

            bool StopOnMove = CheckBoxChecked(CheckboxStopOnMouseMove);
            if (StopOnMove)
            {
                if (Math.Abs(MouseStartPos.X - MousePos.X) > 3 || Math.Abs(MouseStartPos.Y - MousePos.Y) > 3)
                {
                    StopClicking();
                    Debug.WriteLine("Mouse moved, Stop");
                }
            }

            bool StopOnCtrl = CheckBoxChecked(CheckboxStopOnCtrl);
            if (StopOnCtrl)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    StopClicking();
                    Debug.WriteLine("Ctrl held, Stop");
                }
            }

            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)(MousePos.X), (uint)(MousePos.Y), 0, 0);
        }

        private void disableClickingEvent(object? sender, EventArgs e)
        {
            StopClicking();
        }

        private void StopClicking()
        {
            clickIntervalTimer.Stop();
            waitForStartTimer.Stop();
            clickDurationTimer.Stop();
            taskinfo.ProgressState = TaskbarItemProgressState.Normal;
            ButtonStartClicking.Content = "Start clicking";
        }

        private void StartClicking()
        {
            MouseStartPos = GetMousePosition();
            int duration = GetNumericValue(TextBoxDuration);
            int clicksPerSecond = GetNumericValue(TextBoxClickPerSecond);
            clickDurationTimer.Interval = TimeSpan.FromSeconds(duration);
            clickIntervalTimer.Interval = TimeSpan.FromMilliseconds(1000 / clicksPerSecond);
            clickIntervalTimer.Start();

            bool StopOnTimer = CheckBoxChecked(CheckboxStopOnCountdown);
            if (StopOnTimer)
            {
                clickDurationTimer.Start();
            }

            waitForStartTimer.Stop();
            taskinfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            ButtonStartClicking.Content = "Stop clicking";
        }

        int GetNumericValue(TextBox textbox)
        {
            string text = textbox.Text;
            if (textbox.Text.Length <= 0) { return 0; };
            if (int.TryParse(text, out int result) == false) { return 0; };
            return result;
        }

        private void ButtonStartClicking_Click(object sender, RoutedEventArgs e)
        {
            waitForStartTimer.Interval = TimeSpan.FromSeconds(GetNumericValue(TextBoxStartDelay));
            if (clickIntervalTimer.IsEnabled || waitForStartTimer.IsEnabled)
            {
                StopClicking();
            }
            else
            {
                waitForStartTimer.Start();
                ButtonStartClicking.Content = "Starting...";
            }
        }

        private void Numeric_PreviewInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (IsNumber(e.Text) == false)
                {
                    Debug.WriteLine("Preview text is not number");
                    e.Handled = true;
                }
                else
                {
                    Debug.WriteLine("Preview text IS number");
                }
            }
        }

        private static bool IsNumber(string text)
        {
            return int.TryParse(text, out var result);
        }

        private void Numeric_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (IsNumber(textbox.Text) == false)
                {
                    Debug.WriteLine("text is not number");
                    StringBuilder sb = new();
                    foreach (char c in textbox.Text)
                    {
                        if (Char.IsNumber(c)) sb.Append(c);
                    }
                    if (sb.ToString().Length > 0)
                    {
                        textbox.Text = sb.ToString();
                    }
                }
                else
                {
                    Debug.WriteLine("text IS number");
                }
            }
        }

        private void Numeric_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (textbox.Text.Length == 0 || IsNumber(textbox.Text) == false)
                {
                    textbox.Text = textbox.Tag.ToString();
                }
            }
        }

        int clickCount = 0;
        private void ButtonTestClicks_Click(object sender, RoutedEventArgs e)
        {
            clickCount++;
            LabelClickCount.Content = clickCount.ToString();
        }

        private bool CheckBoxChecked(CheckBox cb, string message = "...")
        {
            if (cb == null)
            {
                //null during initialization
                return false;
            }
            if (cb.IsChecked != null)
            {
                return (bool)(cb.IsChecked);
            }
            return false;
        }

        private void checkSafety(CheckBox cb)
        {
            if (starting) return; // don't mess with this during program startup
            bool StopOnTimer = CheckBoxChecked(CheckboxStopOnCountdown, "timer");
            bool StopOnCtrl = CheckBoxChecked(CheckboxStopOnCtrl, "ctrl");
            bool StopOnMove = CheckBoxChecked(CheckboxStopOnMouseMove, "move");

            if (!StopOnTimer && !StopOnCtrl && !StopOnMove)
            {
                cb.IsChecked = true;
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
                checkSafety(cb);
            //UpdateCheckboxSettings();
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            Options options = new(this);
            options.ShowDialog();
        }

        #region Hotkeys -----------------------------------------------------------------------------

        Properties.Settings settings = Properties.Settings.Default;
        private HwndSource source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            Debug.WriteLine($"OnSourceInitialize handle: {handle}");
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);
            LoadHotkeysFromSetting(handle);
        }

        // For each hotkey below, add entries in Settings, hk???Key, hk???Ctrl, hk???Alt, hk???Shift, hk???Win
        public List<string> HotkeyNames = new List<string>
        {
            "StartClicking",
            "StopClicking",
            "ToggleClicking"
        };
        public Dictionary<string, Hotkey> HotkeyList = new Dictionary<string, Hotkey>();

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                Key key = (Key)(((int)lParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)lParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = wParam.ToInt32();                                        // The id of the hotkey that was pressed.
                handled = true;
                HandleHotkey(id);
            }
            return IntPtr.Zero;
        }


        private void HandleHotkey(int id)
        {
            if (HotkeyList["StartClicking"] != null) // else so using the same hotkey for stop and start works
            {
                if (id == HotkeyList["StartClicking"].ghk.id)
                {
                    StartClicking();
                }
            }
            if (HotkeyList["StopClicking"] != null)
            {
                if (id == HotkeyList["StopClicking"].ghk.id)
                {
                    StopClicking();
                }
            }

            if (HotkeyList["ToggleClicking"] != null)
            {
                if (id == HotkeyList["ToggleClicking"].ghk.id)
                {
                    if (clickIntervalTimer.IsEnabled)
                    {
                        StopClicking();
                    }
                    else
                    {
                        StartClicking();
                    }
                }
            }
        }

        #endregion

        private void AlwaysOnTopChanged(object sender, EventArgs e)
        {
            if (starting == false)
            {
                if (CheckboxAlwaysOnTop.IsChecked != null) settings.AlwaysOnTop = (bool)CheckboxAlwaysOnTop.IsChecked;
                //SaveSetting();
            }
            Topmost = settings.AlwaysOnTop;
        }

        public void ApplySettingsToControls()
        {
            Debug.WriteLine($"Settings: {settings.StopOnMouseMove} {settings.StopOnCtrl} {settings.StopOnCountdown} {settings.ToggleClickingKey}");
            CheckboxStopOnMouseMove.IsChecked = settings.StopOnMouseMove;
            CheckboxStopOnCtrl.IsChecked = settings.StopOnCtrl;
            CheckboxStopOnCountdown.IsChecked = settings.StopOnCountdown;
            Debug.WriteLine($"Checks  : {CheckboxStopOnMouseMove.IsChecked} {CheckboxStopOnCtrl.IsChecked} {CheckboxStopOnCountdown.IsChecked} {settings.ToggleClickingKey}");
            Debug.WriteLine($"Settings: {settings.StopOnMouseMove} {settings.StopOnCtrl} {settings.StopOnCountdown} {settings.ToggleClickingKey}");
            //TextBoxDuration.Text = settings.Duration.ToString();
            //TextBoxClickPerSecond.Text = settings.ClicksPerSecond.ToString();
            //TextBoxStartDelay.Text = settings.StartDelay.ToString();
            Duration = settings.Duration;
            ClicksPerSecond = settings.ClicksPerSecond;
            StartDelay = settings.StartDelay;
            CheckboxAlwaysOnTop.IsChecked = settings.AlwaysOnTop;
        }

        //private void UpdateCheckboxSettings()
        //{
        //    Debug.WriteLine("Updating checkboxes");
        //    if (starting)
        //    {
        //        //Aborting checkbox update, program is starting up
        //        return;
        //    }

        //    if (CheckboxStopOnCtrl.IsChecked != null) settings.StopOnCtrl = (bool)CheckboxStopOnCtrl.IsChecked;
        //    if (CheckboxStopOnMouseMove.IsChecked != null) settings.StopOnMouseMove = (bool)CheckboxStopOnMouseMove.IsChecked;
        //    if (CheckboxStopOnCountdown.IsChecked != null) settings.StopOnCountdown = (bool)CheckboxStopOnCountdown.IsChecked;
        //    if (CheckboxAlwaysOnTop.IsChecked != null) settings.AlwaysOnTop = (bool)CheckboxAlwaysOnTop.IsChecked;
        //    //SaveSetting();
        //}

        private int ClicksPerSecond
        {
            get
            {
                return GetNumericValue(TextBoxClickPerSecond);
            }
            set
            {
                TextBoxClickPerSecond.Text = value.ToString();
            }
        }

        private int Duration
        {
            get
            {
                return GetNumericValue(TextBoxDuration);
            }
            set
            {
                TextBoxDuration.Text = value.ToString();
            }
        }

        private int StartDelay
        {
            get
            {
                return GetNumericValue(TextBoxStartDelay);
            }
            set
            {
                TextBoxStartDelay.Text = value.ToString();
            }
        }

        private void SaveSetting()
        {
            if (starting)
            {
                //Skipping Save settings, program is starting
                return;
            }
            settings.ClicksPerSecond = ClicksPerSecond;
            settings.Duration = Duration;
            settings.StartDelay = StartDelay;
            if (CheckboxStopOnCtrl.IsChecked != null) settings.StopOnCtrl = (bool)CheckboxStopOnCtrl.IsChecked;
            if (CheckboxStopOnMouseMove.IsChecked != null) settings.StopOnMouseMove = (bool)CheckboxStopOnMouseMove.IsChecked;
            if (CheckboxStopOnCountdown.IsChecked != null) settings.StopOnCountdown = (bool)CheckboxStopOnCountdown.IsChecked;
            if (CheckboxAlwaysOnTop.IsChecked != null) settings.AlwaysOnTop = (bool)CheckboxAlwaysOnTop.IsChecked;
            Debug.WriteLine("Saving settings");
            settings.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSetting();
            HotkeyTools.ReleaseHotkeys(HotkeyList);
        }


    }


}