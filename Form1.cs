using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using static DWMBGConfigEditor.Properties.Settings;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace DWMBGConfigEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!CheckValidity(Default.DWMBG_Directory))
                if (!CheckValidity())
                    if (!SearchFolder()) Application.Exit();

            GetColor();
            checkBox1.Checked = Default.LinkBothColorsAutomatically;
            
            comboBox1.SelectedIndex = 0;
        }

        private void button2_Click(object sender, EventArgs e) => SearchFolder();

        private bool SearchFolder()
        {
            Begin:

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (CheckValidity(folderBrowserDialog1.SelectedPath))
                {
                    Default.DWMBG_Directory = folderBrowserDialog1.SelectedPath;
                    Default.Save();
                    GetColor();
                    return true;
                }
                else goto Begin;
            }
            else return false;
        }

        private void GetColor()
        {
            foreach (string line in File.ReadAllLines(Path.Combine(Default.DWMBG_Directory, "data\\config.ini")))
            {
                var first = "activeBlendColor=";
                int firstL = first.Length;
                var second = "activeBlendColorDark=";
                int secondL = second.Length;
                var third = "PrimaryBalance=";
                int thirdL = third.Length;
                var fourth = "Active_SecondaryBalance=";
                int fourthL = fourth.Length;
                var fifth = "Inactive_SecondaryBalance=";
                int fifthL = fifth.Length;
                var sixth = "Active_BlurBalance=";
                int sixthL = sixth.Length;
                var seventh = "Inactive_BlurBalance=";
                int seventhL = seventh.Length;

                if (line.ToLower().StartsWith(first.ToLower()) && long.TryParse(line.Substring(firstL), out _))
                {
                    string color = long.Parse(line.Substring(firstL)).ToString("X8");

                    B.Value = int.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    G.Value = int.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    R.Value = int.Parse(color.Substring(6), System.Globalization.NumberStyles.HexNumber);
                }

                else if (line.ToLower().StartsWith(second.ToLower()) && long.TryParse(line.Substring(secondL), out _))
                {
                    string color = long.Parse(line.Substring(secondL)).ToString("X8");

                    B2.Value = int.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    G2.Value = int.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    R2.Value = int.Parse(color.Substring(6), System.Globalization.NumberStyles.HexNumber);
                }

                else if (line.ToLower().StartsWith(third.ToLower()))
                {
                    A.Value = Convert.ToInt32(GetDouble(line, thirdL) * 100);
                }

                else if (line.ToLower().StartsWith(fourth.ToLower()))
                {
                    SA1.Value = Convert.ToInt32(GetDouble(line, fourthL) * 1000);
                }

                else if (line.ToLower().StartsWith(fifth.ToLower()))
                {
                    SA2.Value = Convert.ToInt32(GetDouble(line, fifthL) * 1000);
                }

                else if (line.ToLower().StartsWith(sixth.ToLower()))
                {
                    BA1.Value = Convert.ToInt32(GetDouble(line, sixthL) * 1000);
                }

                else if (line.ToLower().StartsWith(seventh.ToLower()))
                {
                    BA2.Value = Convert.ToInt32(GetDouble(line, seventhL) * 1000);
                }
            }
        }

        private double GetDouble(string line, int substring)
        {
            string s = line.Substring(substring);
            try { s = line.Substring(substring, Math.Min(line.Length - substring, 6)).Trim().Replace(";", "").ToLower().Replace("c", "").Replace("o", "").Replace("n", "").Replace("t", "").Replace("r", "").Replace("l", ""); }
            catch { s = line.Substring(substring); }

            if (double.TryParse(s, out double y))
                return y;
            return 0.00d;
        }

        private string ToHex(int value) => value.ToString("X2");
        private string ToHex(double value) => Convert.ToInt32(value).ToString("X2");
        private string ToHex(decimal value) => Convert.ToInt32(value).ToString("X2");

        private void SaveColor()
        {
            // Create new color value
            // *************
            string hex = ToHex(255) + ToHex((int)B.Value) + ToHex((int)G.Value) + ToHex((int)R.Value);
            string hex2 = ToHex(255) + ToHex((int)B2.Value) + ToHex((int)G2.Value) + ToHex((int)R2.Value);
            IDictionary<string, string> newValues = new Dictionary<string, string>
            {
                { "activeBlendColor=", long.Parse(hex, System.Globalization.NumberStyles.HexNumber).ToString() },
                { "inactiveBlendColor=", long.Parse(hex, System.Globalization.NumberStyles.HexNumber).ToString() },
                { "activeBlendColorDark=", long.Parse(hex2, System.Globalization.NumberStyles.HexNumber).ToString() },
                { "inactiveBlendColorDark=", long.Parse(hex, System.Globalization.NumberStyles.HexNumber).ToString() },
                { "PrimaryBalance=", numericUpDown1.Value + " 		; Controls normal layer opacity. If you turn this up to 1 the window will be fully opaque, for example. Ranges from 0 to 1" }, // 
                { "Active_SecondaryBalance=", numericUpDown2.Value + "	; Controls the multiply layer intensity for active windows. Ranges from 0 to 1." },
                { "Inactive_SecondaryBalance=", numericUpDown3.Value + "	; Controls the multiply layer intensity for inactive windows. Ranges from 0 to 1." },
                { "Active_BlurBalance=", numericUpDown4.Value + "	; Controls \"overexposure\" effect for active windows. Ranges from -1 to 1" },
                { "Inactive_BlurBalance=", numericUpDown5.Value + "	; Controls \"overexposure\" effect for inactive windows. Ranges from -1 to 1 " },
            };

            // Write to file
            // *************

            string configPath = Path.Combine(Default.DWMBG_Directory, "data\\config.ini");
            var newConfig = File.ReadAllLines(configPath);
            bool modified = false;

            for (int j = 0; j < newValues.Count; j++)
                for (int i = 0; i < newConfig.Length; i++)
                    if (newConfig[i].ToLower().StartsWith(newValues.ElementAt(j).Key.ToLower())) { newConfig[i] = newConfig[i].Substring(0, newValues.ElementAt(j).Key.Length) + newValues.ElementAt(j).Value; modified = true; }

            if (!modified)
            {
                MessageBox.Show("Failed to save config.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            using (StreamWriter sw = new StreamWriter(configPath))
            {
                foreach (var item in newConfig)
                    sw.WriteLine(item);
            }

            // Also change StartIsBack++ value
            // *************
            bool sib = Default.ChangeStartIsBack ? Default.ChangeStartIsBack
                     : Registry.CurrentUser.OpenSubKey("SOFTWARE\\StartIsBack", true) != null ? MessageBox.Show("Do you also want to set the StartIsBack++ taskbar and start menu colors to the light window color?", button0.Text, MessageBoxButtons.YesNo) == DialogResult.Yes : false;
            if (sib)
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\StartIsBack", true))
                {
                    if (key != null)  // Must check for null key
                    {
                        (int, int, int) p = ((int)R.Value, (int)G.Value, (int)B.Value);
                        p.Item1 = Math.Max(1, p.Item1 - 1);
                        p.Item2 = Math.Max(1, p.Item2 - 1);
                        p.Item3 = Math.Max(1, p.Item3 - 1);

                        string colorHex = "0x00" + ToHex(p.Item3) + ToHex(p.Item2) + ToHex(p.Item1);
                        string alphaHex = ToHex(Math.Round((numericUpDown1.Value + (numericUpDown2.Value / 2)) / 1.5m * 255m)).ToLower();
                        var colorUint32 = Convert.ToUInt32(colorHex, 16);
                        var alphaUint32 = Convert.ToUInt32(alphaHex, 16);

                        key.SetValue("StartMenuColor", colorUint32, RegistryValueKind.DWord);
                        key.SetValue("TaskbarColor", colorUint32, RegistryValueKind.DWord);
                        key.SetValue("StartMenuAlpha", alphaUint32, RegistryValueKind.DWord);
                        key.SetValue("TaskbarAlpha", alphaUint32, RegistryValueKind.DWord);
                    }
                }

            // Restart
            // *************
            bool restart = Default.DWMBG_AutoRestart ? Default.DWMBG_AutoRestart : MessageBox.Show("Restart DWMBlurGlass?", button0.Text, MessageBoxButtons.YesNo) == DialogResult.Yes;
            // bool kill = MessageBox.Show("Do you also want to kill all DWM processes?", "Save", MessageBoxButtons.YesNo) == DialogResult.Yes;

            if (restart)
            {
                using (Process p = Process.Start(Path.Combine(Default.DWMBG_Directory, "DWMBlurGlass.exe"), "unloaddll")) p.WaitForExit();

                /* if (kill)
                    foreach (var item in Process.GetProcessesByName("dwm"))
                        item.Kill(); */

                using (Process p = Process.Start(Path.Combine(Default.DWMBG_Directory, "DWMBlurGlass.exe"), "loaddll")) p.WaitForExit();
            }

            if (sib && Default.ChangeStartIsBack) MessageBox.Show("StartIsBack++ color settings have been detected and changed.\n\nYou may need to apply its configuration again manually or restart Explorer for full changes to take effect.", button0.Text);
        }

        private bool CheckValidity(string path = null)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) path = Environment.CurrentDirectory;

            bool valid = false;
            bool exists = File.Exists(Path.Combine(path, "DWMBlurGlass.exe")) && File.Exists(Path.Combine(path, "data\\config.ini"));
            if (exists)
                foreach (string line in File.ReadAllLines(Path.Combine(path, "data\\config.ini")))
                    if (line.ToLower().Contains("primarybalance") || line.ToLower().Contains("active_blurbalance"))
                        valid = true;

            if (valid) { Default.DWMBG_Directory = path; Default.Save(); }

            return valid;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = !checkBox1.Checked;
            if (!groupBox2.Enabled)
            {
                R2.Value = R.Value;
                G2.Value = G.Value;
                B2.Value = B.Value;
            }
        }

        private void NumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (sender == numericUpDown1)
                A.Value = (int)(numericUpDown1.Value * 100);
            else if (sender == numericUpDown2)
                SA1.Value = (int)(numericUpDown2.Value * 1000);
            else if (sender == numericUpDown3)
                SA2.Value = (int)(numericUpDown3.Value * 1000);
            else if (sender == numericUpDown4)
                BA1.Value = (int)(numericUpDown4.Value * 1000);
            else if (sender == numericUpDown5)
                BA2.Value = (int)(numericUpDown5.Value * 1000);
        }

        private void ChangeColorValue(object sender, EventArgs e)
        {
            if (!groupBox2.Enabled)
            {
                R2.Value = R.Value;
                G2.Value = G.Value;
                B2.Value = B.Value;
            }

            decimal alpha = (numericUpDown1.Value + (numericUpDown2.Value / 2)) / 1.5m * 255m;
            panel1.BackColor = Color.FromArgb(Convert.ToInt32(Math.Round(alpha)), (int)R.Value, (int)G.Value, (int)B.Value);
            panel4.BackColor = Color.FromArgb(Convert.ToInt32(Math.Round(alpha)), (int)R2.Value, (int)G2.Value, (int)B2.Value);

            if (sender == A)
                numericUpDown1.Value = A.Value / 100m;
            else if (sender == SA1)
                numericUpDown2.Value = SA1.Value / 1000m;
            else if (sender == SA2)
                numericUpDown3.Value = SA2.Value / 1000m;
            else if (sender == BA1)
                numericUpDown4.Value = BA1.Value / 1000m;
            else if (sender == BA2)
                numericUpDown5.Value = BA2.Value / 1000m;
        }

        private void button0_Click(object sender, EventArgs e) => SaveColor();

        private void button1_Click(object sender, EventArgs e)
        {
            var colorSetEx = GetImmersiveColorFromColorSetEx((uint)GetImmersiveUserColorSetPreference(false, false), GetImmersiveColorTypeFromName(Marshal.StringToHGlobalUni("ImmersiveStartSelectionBackground")), false, 0);
            
            byte redColor = (byte)((0x000000FF & colorSetEx) >> 0);
            byte greenColor = (byte)((0x0000FF00 & colorSetEx) >> 8);
            byte blueColor = (byte)((0x00FF0000 & colorSetEx) >> 16);
            // byte alphaColor = (byte)((0xFF000000 & colorSetEx) >> 24);

            B2.Value = B.Value = blueColor;
            G2.Value = G.Value = greenColor;
            R2.Value = R.Value = redColor;
        }

        [DllImport("uxtheme.dll", EntryPoint = "#95")]
        private static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType,
                                                                    bool bIgnoreHighContrast, uint dwHighContrastCacheMode);
        [DllImport("uxtheme.dll", EntryPoint = "#96")]
        private static extern uint GetImmersiveColorTypeFromName(IntPtr pName);

        [DllImport("uxtheme.dll", EntryPoint = "#98")]
        private static extern int GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        private void TaskScheduler_Run(object sender, EventArgs e)
        {
            using (TaskService ts = new TaskService())
            {
                string name = "DWMBlurGlass Fork";

                if (sender == button3)
                {
                    if (!ts.RootFolder.Tasks.Exists(name))
                    {
                        // Create a new task definition and assign properties
                        TaskDefinition td = ts.NewTask();
                        td.RegistrationInfo.Description = "Runs DWMBlurGlass at logon.";

                        td.Triggers.Add(new LogonTrigger());
                        td.Actions.Add(new ExecAction(Path.Combine(Default.DWMBG_Directory, "DWMBlurGlass.exe"), "loaddll", null));

                        td.Settings.DisallowStartIfOnBatteries = false;
                        td.Settings.AllowDemandStart = true;
                        td.Settings.AllowHardTerminate = true;

                        // Logged on or not with highest privileges
                        td.Principal.LogonType = TaskLogonType.S4U;
                        td.Principal.RunLevel = TaskRunLevel.Highest;

                        // Register the task in the root folder
                        ts.RootFolder.RegisterTaskDefinition(name, td);

                        MessageBox.Show("Successfully installed DMWBlurGlass as a task to run at logon.", button3.Text);
                    }

                    else MessageBox.Show("DWMBlurGlass has already been installed as a task.", button3.Text);
                }

                else if (sender == button4)
                {
                    if (ts.RootFolder.Tasks.Exists(name))
                    {
                        ts.RootFolder.DeleteTask(name);
                        MessageBox.Show("Successfully uninstalled DWMBlurGlass from the Task Scheduler.", button3.Text);
                    }

                    else MessageBox.Show("DWMBlurGlass has already been uninstalled from the Task Scheduler.", button4.Text);
                }
            }
        }

        private void SetPreset_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
            switch (comboBox1.SelectedIndex)
            {
                default:
                    (R.Value, G.Value, B.Value)
                        = (116, 184, 252);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.08m, 0.43m, 0.43m, -0.125m, 0.365m);
                    break;

                case 1:
                    (R.Value, G.Value, B.Value)
                        = (0, 70, 173);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.56m, 0.11m, 0.11m, -0.125m, 0.125m);
                    break;

                case 2:
                    (R.Value, G.Value, B.Value)
                        = (50, 205, 205);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.24m, 0.34m, 0.4m, -0.125m, 0.325m);
                    break;

                case 3:
                    (R.Value, G.Value, B.Value)
                        = (20, 166, 0);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.45m, 0.45m, -0.125m, 0.4m);
                    break;

                case 4:
                    (R.Value, G.Value, B.Value)
                        = (151, 217, 55);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.45m, 0.45m, -0.125m, 0.4m);
                    break;

                case 5:
                    (R.Value, G.Value, B.Value)
                        = (250, 220, 14);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.35m, 0.35m, -0.05m, 0.365m);
                    break;

                case 6:
                    (R.Value, G.Value, B.Value)
                        = (255, 156, 0);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.24m, 0.32m, 0.32m, -0.1m, 0.325m);
                    break;

                case 7:
                    (R.Value, G.Value, B.Value)
                        = (206, 15, 15);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.56m, 0.11m, 0.11m, -0.125m, 0.125m);
                    break;

                case 8:
                    (R.Value, G.Value, B.Value)
                        = (255, 0, 153);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.45m, 0.4m, -0.075m, 0.4m);
                    break;

                case 9:
                    (R.Value, G.Value, B.Value)
                        = (252, 199, 248);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.12m, 0.4m, 0.4m, -0.09m, 0.385m);
                    break;

                case 10:
                    (R.Value, G.Value, B.Value)
                        = (110, 59, 161);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.29m, 0.29m, 0.29m, -0.115m, 0.30m);
                    break;

                case 11:
                    (R.Value, G.Value, B.Value)
                        = (141, 90, 148);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.34m, 0.45m, -0.025m, 0.395m);
                    break;

                case 12:
                    (R.Value, G.Value, B.Value)
                        = (152, 132, 76);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.45m, 0.45m, -0.035m, 0.385m);
                    break;

                case 13:
                    (R.Value, G.Value, B.Value)
                        = (79, 27, 27);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.56m, 0.11m, 0.11m, -0.125m, 0.125m);
                    break;

                case 14:
                    (R.Value, G.Value, B.Value)
                        = (85, 85, 85);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.24m, 0.32m, 0.32m, -0.125m, 0.325m);
                    break;

                case 15:
                    (R.Value, G.Value, B.Value)
                        = (252, 252, 252);
                    (numericUpDown1.Value, numericUpDown2.Value, numericUpDown3.Value, numericUpDown4.Value, numericUpDown5.Value)
                        = (0.05m, 0.35m, 0.35m, -0.025m, 0.415m);
                    break;
            }

            tabControl1.SelectedTab = tabPage1;
        }
    }
}
