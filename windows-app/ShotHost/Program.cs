using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ShotHost
{
    static class Program
    {
        const string MutexName = "Local\\ShotHost.SingleInstance";
        const string OpenSettingsEventName = "Local\\ShotHost.OpenSettings";

        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int processId);

        [STAThread]
        static void Main(string[] args)
        {
            using (var mutex = new Mutex(true, MutexName, out var isFirst))
            {
                // Значок в трее может быть скрыт (в настройках или самой Windows),
                // поэтому повторный запуск открывает настройки уже запущенного ShotHost.
                if (!isFirst)
                {
                    try
                    {
                        // Разрешаем уже запущенному ShotHost вывести окно настроек на передний план.
                        AllowSetForegroundWindow(-1);
                        using (var signal = EventWaitHandle.OpenExisting(OpenSettingsEventName))
                            signal.Set();
                    }
                    catch (WaitHandleCannotBeOpenedException) { }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var openSettings = new EventWaitHandle(false, EventResetMode.AutoReset, OpenSettingsEventName))
                using (var app = new TrayApp(Array.IndexOf(args, Autostart.Argument) >= 0))
                {
                    var registration = ThreadPool.RegisterWaitForSingleObject(openSettings,
                        (_, __) => app.ShowSettingsFromAnyThread(), null, Timeout.Infinite, false);
                    Application.Run(app);
                    registration.Unregister(null);
                }
            }
        }
    }
}
