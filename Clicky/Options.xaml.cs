using Clicky.Properties;
using Hotkeys;
using System.Diagnostics;
using System.Windows;

namespace Clicky;

/// <summary>
/// Interaction logic for Options.xaml
/// </summary>
public partial class Options : Window
{
    MainWindow mainForm;

    public Options(MainWindow parent)
    {
        InitializeComponent();
        mainForm = parent;
        FillSettings();
    }

    private void ButtonOK_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        DialogResult = true;
    }

    private void ButtonApply_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings();
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    List<HotkeyEntry> hotkeyEntries = [];
    private void FillSettings()
    {
        //textBoxFilename.Text = Settings.Default.Filename;
        CheckboxRegisterHotkeys.IsChecked = Settings.Default.RegisterHotkeys;
        //fillHotkeyGrid();
        //List<HotkeyEntry> testList = [];
        hotkeyEntries = LoadCollectionData();
        HotkeyGrid.ItemsSource = hotkeyEntries;
        //HotkeyGrid.AutoGenerateColumns = true;
        //testList.Add(new HotkeyEntry() { Function = "Hello" });

    }

    private List<HotkeyEntry> LoadCollectionData()
    {
        List<HotkeyEntry> listResult = [];
        //listResult.Add(new HotkeyEntry() { Function = "Hello", Key = "NumLock" });

        foreach (KeyValuePair<string, Hotkey> kvp in mainForm.HotkeyList)
        {
            Hotkey hotkey = kvp.Value;
            listResult.Add(new HotkeyEntry()
            {
                Function = kvp.Key,
                Key = hotkey.Key,
                Ctrl = hotkey.Ctrl,
                Alt = hotkey.Alt,
                Shift = hotkey.Shift,
                Win = hotkey.Win
            });
        }

        return listResult;
    }

    private void ApplySettings()
    {
        if (CheckboxRegisterHotkeys.IsChecked != null)
        {
            Settings.Default.RegisterHotkeys = (bool)CheckboxRegisterHotkeys.IsChecked;
        }

        int i = 0;
        //foreach (KeyValuePair<string, Hotkey> kvp in mainForm.HotkeyList)
        foreach (HotkeyEntry entry in hotkeyEntries)
        {
            string keyName = entry.Function;
            //if (HotkeyGrid.Rows[i].Cells[1].Value == null)
            //{
            //    HotkeyGrid.Rows[i].Cells[1].Value = "";
            //}
            Properties.Settings.Default[keyName + "Key"] = entry.Key; //HotkeyGrid.Rows[i].Cells[1].Value.ToString();

            Properties.Settings.Default[keyName + "Ctrl"] = entry.Ctrl; //Convert.ToBoolean(HotkeyGrid.Rows[i].Cells[2].Value);
            Properties.Settings.Default[keyName + "Alt"] = entry.Alt;  //Convert.ToBoolean(HotkeyGrid.Rows[i].Cells[3].Value);
            Properties.Settings.Default[keyName + "Shift"] = entry.Shift; //Convert.ToBoolean(HotkeyGrid.Rows[i].Cells[4].Value);
            Properties.Settings.Default[keyName + "Win"] = entry.Win; //Convert.ToBoolean(HotkeyGrid.Rows[i].Cells[5].Value);

            mainForm.HotkeyList[keyName] = GetHotkeyFromGrid(mainForm.HotkeyList[keyName], entry);

            i++;
        }

        Settings.Default.Save();

        reloadHotkeys();
    }

    private Hotkey GetHotkeyFromGrid(Hotkey hotkey, HotkeyEntry entry)
    {
        string settingKey = string.Empty;

        hotkey.Key = entry.Key;
        hotkey.Ctrl = entry.Ctrl;
        hotkey.Alt = entry.Alt;
        hotkey.Shift = entry.Shift;
        hotkey.Win = entry.Win;

        return hotkey;
    }

    private void reloadHotkeys()
    {
        if (CheckboxRegisterHotkeys.IsChecked != null)
        {
            if ((bool)CheckboxRegisterHotkeys.IsChecked)
            {
                HotkeyTools.UpdateHotkeys(mainForm.HotkeyList, mainForm.HotkeyNames, mainForm.GetHandle());
            }
        }
        else
        {
            HotkeyTools.ReleaseHotkeys(mainForm.HotkeyList);
        }
        Debug.WriteLine("Released and re-registered hotkeys");
    }
}

public class HotkeyEntry()
{
    public string Function { get; set; }
    public string Key { get; set; }
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }
}
