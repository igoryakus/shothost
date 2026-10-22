using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ShotHost
{
    /// Запуск при входе в систему через HKCU\...\Run, без прав администратора.
    static class Autostart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "ShotHost";

        public const string Argument = "--autostart";

        static string Command => $"\"{Application.ExecutablePath}\" {Argument}";

        public static bool IsEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                    return key?.GetValue(ValueName) != null;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled) key.SetValue(ValueName, Command);
                else key.DeleteValue(ValueName, false);
            }
        }

        /// Если ShotHost.exe перенесли в другую папку, автозапуск переходит на новое место.
        public static void RefreshPath()
        {
            try
            {
                if (IsEnabled) SetEnabled(true);
            }
            catch (Exception) { }
        }
    }
}
