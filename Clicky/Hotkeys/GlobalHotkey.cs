using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Hotkeys
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class GlobalHotkey
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public int Modifier;
        public int KeyCode;
        private IntPtr WindowHandle;
        public int ID;
        public bool Registered;
        private bool ValidKey;
        public string HotkeyName = "Unnamed";
        private string KeyName = "";

        public int stringToKey(string keystring)
        {
            if (keystring.Length > 0)
            {
                if (keystring.Length == 1)
                {
                    char ch = keystring[0];
                    ValidKey = true;
                    return (int)ch;
                }
                else
                {
                    ValidKey = Enum.TryParse(keystring, out Key key);
                    int convertedKey = KeyInterop.VirtualKeyFromKey(key);
                    return convertedKey;
                }
            }
            return 0;
        }

        public GlobalHotkey(int modifier, string keystring, IntPtr handle, string name = "Unnamed")
        {
            this.Modifier = modifier;
            this.KeyCode = stringToKey(keystring);
            KeyName = keystring;
            this.WindowHandle = handle;
            HotkeyName = name;
            ID = this.GetHashCode();
        }

        public GlobalHotkey()
        {
            ValidKey = false;
        }

        public override int GetHashCode()
        {
            return Modifier ^ KeyCode ^ WindowHandle.ToInt32();
        }

        public bool Register()
        {
            if (ValidKey == false)
            {
                Registered = false;
                Debug.WriteLine("Validkey false: " + KeyCode + " / " + Modifier);
                return Registered;
            }
            if (ID != 0)
            {
                Registered = RegisterHotKey(WindowHandle, ID, Modifier, KeyCode);
                Debug.WriteLine($"Registered hotkey: {Registered.ToString()} displayname:{HotkeyName} keystring:{KeyName} key:{KeyCode} mod:{Modifier} id:{ID} handle:{WindowHandle}");
                return Registered;
            }
            else
            {
                Registered = false;
                Debug.WriteLine("Unknown register hotkey error: " + KeyCode + " / " + Modifier);
                return Registered;
            }
        }

        public bool Unregister()
        {
            Registered = false;
            return UnregisterHotKey(WindowHandle, ID);
        }
    }
}
