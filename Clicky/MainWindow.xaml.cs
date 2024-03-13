﻿using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Hotkeys;

namespace Clicky
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        //IntPtr handle;
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
        //DispatcherTimer loadSettingTimer = new();

        TaskbarItemInfo taskinfo = new System.Windows.Shell.TaskbarItemInfo();
        public MainWindow()
        {
            InitializeComponent();
            settings.Reload();
            ApplySettings();

            //loadSettingTimer.Interval = TimeSpan.FromSeconds(3);
            //loadSettingTimer.Start();

            //handle = new WindowInteropHelper(this).Handle;
            //Debug.WriteLine($"Start, handle:{handle}");
            //LoadHotkeysFromSetting();
            waitForStartTimer.Tick += new EventHandler(WaitForStart_Tick);
            clickIntervalTimer.Tick += new EventHandler(OnTimer);
            clickDurationTimer.Tick += new EventHandler(disableClickingEvent);
            //loadSettingTimer.Tick += new EventHandler(delayedSettingLoad);
            //dispatcherTimer.Interval = new TimeSpan(0,5,0);
            //dispatcherTimer.Start();

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
            //uint X = (uint)Cursor.Position.X;
            //uint Y = (uint)Cursor.Position.Y;
            //bool currentCapsLockState = Control.IsKeyLocked(Keys.CapsLock);

            //Debug.WriteLine("Check if we should stop");
            bool StopOnMove = CheckBoxChecked(CheckboxStopOnMouseMove);
            if (StopOnMove)
            {
                //Debug.WriteLine("Stop if moved is true");
                if (Math.Abs(MouseStartPos.X - MousePos.X) > 3 || Math.Abs(MouseStartPos.Y - MousePos.Y) > 3)
                {
                    StopClicking();
                    Debug.WriteLine("Mouse moved, Stop");
                }
            }
            
            bool StopOnCtrl = CheckBoxChecked(CheckboxStopOnCtrl);
            if (StopOnCtrl)
            {
                //Debug.WriteLine("Stop if Ctrl is true");
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
            Debug.WriteLine("Duration time elapsed");
            StopClicking();
        }

        private void StopClicking()
        {
            clickIntervalTimer.Stop();
            waitForStartTimer.Stop();
            clickDurationTimer.Stop();
            Debug.WriteLine("Stopping clicks");
            taskinfo.ProgressState = TaskbarItemProgressState.Normal;
            ButtonStartClicking.Content = "Start clicking";
        }

        private void StartClicking()
        {
            Debug.WriteLine("Starting clicks");
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
            if (int.TryParse(text, out int result) == false ) { return 0; };
            return result;
        }

        //private void CountOneClick(object sender, RoutedEventArgs e)
        //{
        //    taskinfo.ProgressValue += 0.1d;
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    taskinfo.ProgressState = TaskbarItemProgressState.Indeterminate;
        //}

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
                if (IsNumber(textbox.Text) == false)
                {
                    e.Handled = true;
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
                    StringBuilder sb = new();
                    foreach (char c in textbox.Text)
                    {
                        if (Char.IsNumber(c)) sb.Append(c);
                    }
                    if (sb.ToString().Length > 0)
                    {
                        textbox.Text = sb.ToString();
                    }
                    else
                    {
                        textbox.Text = textbox.Tag.ToString();
                    }
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
            //updateCheckboxSettings();
            //SaveSetting();
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
                checkSafety(cb);
            UpdateCheckboxSettings();
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            Options options = new(this);
            options.ShowDialog();
        }

        #region Hotkeys -----------------------------------------------------------------------------

        //[DllImport("user32.dll")]
        //private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        //private const int HOTKEY_ID = 9000;
        //private const uint MOD_CONTROL = 0x0;//0x0002;
        //private const uint VK_CAPITAL = 0x14;

        Properties.Settings settings =  Properties.Settings.Default;
        private HwndSource source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            Debug.WriteLine($"OnSourceInitialize handle: {handle}");
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);
            LoadHotkeysFromSetting(handle);
            //RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL, VK_CAPITAL);
            //Debug.WriteLine($"Test hotkey: key:{VK_CAPITAL} mod:{MOD_CONTROL} id:{HOTKEY_ID} hWnd:{handle}");
            //RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL, VK_CAPITAL); //CTRL + CAPS_LOCK
        }

        // For each hotkey below, add entries in Settings, hk???Key, hk???Ctrl, hk???Alt, hk???Shift, hk???Win
        public List<string> HotkeyNames = new List<string>
        {
            "StartClicking",
            "StopClicking",
            "ToggleClicking"
        };
        public Dictionary<string, Hotkey> HotkeyList = new Dictionary<string, Hotkey>();

        //private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    HotkeyTools.ReleaseHotkeys(HotkeyList);
        //}

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            //if (msg != 132) Debug.WriteLine($"HwndHook msg:{msg}, waiting for {Hotkeys.Constants.WM_HOTKEY_MSG_ID}");
            if (msg == WM_HOTKEY)
            //if (msg == Hotkeys.Constants.WM_HOTKEY_MSG_ID)
            {
                Debug.WriteLine("Hotkey pressed");
                //Key key = (Key)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                Key key = (Key)(((int)lParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                //KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)lParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                //int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.
                int id = wParam.ToInt32();                                        // The id of the hotkey that was pressed.
                                                                                  //MessageBox.Show("Hotkey " + id + " has been pressed!");
                handled = true;
                HandleHotkey(id);
            }
            //switch (msg)
            //{
            //    case WM_HOTKEY:
            //        switch (wParam.ToInt32())
            //        {
            //            case HOTKEY_ID:
            //                int vkey = (((int)lParam >> 16) & 0xFFFF);
            //                if (vkey == VK_CAPITAL)
            //                {
            //                    //handle global hot key here...
            //                }
            //                handled = true;
            //                break;
            //        }
            //        break;
            //}
            return IntPtr.Zero;
        }

        //protected override void WndProc(ref Message m)
        //{
        //    base.WndProc(ref m);
        //    if (m.Msg == Hotkeys.Constants.WM_HOTKEY_MSG_ID)
        //    {
        //        Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
        //        KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
        //        int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.
        //        //MessageBox.Show("Hotkey " + id + " has been pressed!");
        //        HandleHotkey(id);
        //    }
        //}

        private void HandleHotkey(int id)
        {
            if (HotkeyList["StartClicking"] != null) // else so using the same hotkey for stop and start works
            {
                if (id == HotkeyList["StartClicking"].ghk.id)
                {
                    Debug.WriteLine("Start clicking hotkey pressed");
                    StartClicking();
                }
            }
            if (HotkeyList["StopClicking"] != null)
            {
                if (id == HotkeyList["StopClicking"].ghk.id)
                {
                    Debug.WriteLine("Stop clicking hotkey pressed");
                    StopClicking();
                }
            }

            if (HotkeyList["ToggleClicking"] != null)
            {
                if (id == HotkeyList["ToggleClicking"].ghk.id)
                {
                    Debug.WriteLine("Toggle clicking hotkey pressed");
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
                SaveSetting();
            }
            Topmost = settings.AlwaysOnTop;
        }

        public void ApplySettings()
        {
            Debug.WriteLine($"Settings: {settings.StopOnMouseMove} {settings.StopOnCtrl} {settings.StopOnCountdown} {settings.ToggleClickingKey}");
            CheckboxStopOnMouseMove.IsChecked = settings.StopOnMouseMove;
            CheckboxStopOnCtrl.IsChecked = settings.StopOnCtrl;
            CheckboxStopOnCountdown.IsChecked = settings.StopOnCountdown;
            Debug.WriteLine($"Checks  : {CheckboxStopOnMouseMove.IsChecked} {CheckboxStopOnCtrl.IsChecked} {CheckboxStopOnCountdown.IsChecked} {settings.ToggleClickingKey}");
            Debug.WriteLine($"Settings: {settings.StopOnMouseMove} {settings.StopOnCtrl} {settings.StopOnCountdown} {settings.ToggleClickingKey}");
            TextBoxDuration.Text = settings.Duration.ToString();
            TextBoxClickPerSecond.Text = settings.ClicksPerSecond.ToString();
            TextBoxStartDelay.Text = settings.StartDelay.ToString();
            CheckboxAlwaysOnTop.IsChecked = settings.AlwaysOnTop;
        }

        private void UpdateCheckboxSettings()
        {
            Debug.WriteLine("Updating checkboxes");
            if (starting)
            {
                Debug.WriteLine("Aborting checkbox update, program is starting up");
                return;
            }
            
            if (CheckboxStopOnCtrl.IsChecked != null) settings.StopOnCtrl = (bool)CheckboxStopOnCtrl.IsChecked;
            if (CheckboxStopOnMouseMove.IsChecked != null) settings.StopOnMouseMove = (bool)CheckboxStopOnMouseMove.IsChecked;
            if (CheckboxStopOnCountdown.IsChecked != null) settings.StopOnCountdown = (bool)CheckboxStopOnCountdown.IsChecked;
            if (CheckboxAlwaysOnTop.IsChecked != null) settings.AlwaysOnTop = (bool)CheckboxAlwaysOnTop.IsChecked;
            SaveSetting();
        }

        private void SaveSetting()
        {
            if (starting)
            {
                //Debug.WriteLine("Skipping Save settings, program is starting");
                return;
            }
            Debug.WriteLine("Saving settings");
            settings.Save();
        }

        //private void delayedSettingLoad(object? sender, EventArgs e)
        //{
        //    ApplySettings();
        //    loadSettingTimer.Stop();
        //    starting = false;
        //}
    }


}