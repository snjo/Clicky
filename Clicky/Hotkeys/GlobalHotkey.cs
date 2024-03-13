using System;
using System.Diagnostics;
//using System.Runtime.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
//using System.Windows.Forms;

namespace Hotkeys
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class GlobalHotkey
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public int modifier;
        public int key;
        private IntPtr hWnd;
        public int id;
        public bool registered;
        private bool validKey;
        public string displayName = "Unnamed";

        public int stringToKey(string keystring)
        {
            if (keystring.Length > 0)
            {
                if (keystring.Length == 1)
                {
                    char ch = keystring[0];
                    validKey = true;
                    return (int)ch;
                }
                else
                {
                    validKey = Enum.TryParse(keystring, out Key key);
                    int convertedKey = KeyInterop.VirtualKeyFromKey(key);
                    Debug.WriteLine($"stringToKey: {keystring} {key} {(int)key} {convertedKey}");
                    //return (Key)key;
                    return convertedKey;
                }
            }
            return 0;
        }

        public GlobalHotkey(int modifier, string keystring, IntPtr handle, string name = "Unnamed")
        {
            this.modifier = modifier;
            //Key key = stringToKey(keystring);  // assigns validKey
            //this.key = (int)key;
            this.key = stringToKey(keystring);
            //var handle = new WindowInteropHelper(form).Handle;
            Debug.WriteLine($"ghk Handle:{handle}");
            this.hWnd = handle;
            //this.hWnd = Process.GetCurrentProcess().MainWindowHandle;
            displayName = name;
            id = this.GetHashCode();
        }

        public GlobalHotkey()
        {
            validKey = false;
        }

        public override int GetHashCode()
        {
            return modifier ^ key ^ hWnd.ToInt32();
        }

        public bool Register()
        {
            if (validKey == false)
            {
                registered = false;
                Debug.WriteLine("Validkey false: " + key + " / " + modifier);
                return registered;
            }
            if (id != 0)
            {
                registered = RegisterHotKey(hWnd, id, modifier, key);
                Debug.WriteLine($"Registered hotkey: {registered.ToString()} key:{key} mod:{modifier} id:{id} hWnd:{hWnd}");
                return registered;
            }
            else
            {
                registered = false;
                Debug.WriteLine("Unknown register hotkey error: " + key + " / " + modifier);
                return registered;
            }
        }

        public bool Unregister()
        {
            //Debug.WriteLine("Releasing hotkey: " + key + " / " + modifier);
            registered = false;
            return UnregisterHotKey(hWnd, id);
        }
    }
}
