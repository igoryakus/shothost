using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShotHost.Core;

namespace ShotHost
{
    /// Значок в трее, слежение за папкой и загрузка — то же, что AppState в приложении для macOS.
    sealed class TrayApp : ApplicationContext
    {
        readonly string settingsPath = Settings.DefaultPath;
        readonly Settings settings;
        readonly NotifyIcon tray;
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly ToolStripMenuItem statusItem = new ToolStripMenuItem { Enabled = false };
        readonly ToolStripMenuItem pauseItem = new ToolStripMenuItem("Пауза");
        readonly ToolStripMenuItem recentItem = new ToolStripMenuItem("Последние загрузки");
        // Окна и таймеры создаются в потоке интерфейса; через этот элемент туда попадают события из других потоков.
        readonly Control invoker = new Control();
        readonly HashSet<string> inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        FileSystemWatcher watcher;
        string watcherError;
        int activeUploads;
        string lastBalloonLink;
        SettingsForm settingsForm;

        public static string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version is System.Version v ? $"{v.Major}.{v.Minor}.{v.Build}" : "";

        public static Icon LoadIcon(Size size)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.ico"))
                return new Icon(stream, size);
        }

        public TrayApp(bool startedAtLogin)
        {
            _ = invoker.Handle;
            settings = Settings.Load(settingsPath);
            if (string.IsNullOrEmpty(settings.Config.WatchDir))
                settings.Config.WatchDir = DefaultScreenshotsDir();

            pauseItem.Click += (_, __) =>
            {
                settings.IsPaused = !settings.IsPaused;
                Save();
                RestartWatcher();
            };
            var uploadItem = new ToolStripMenuItem("Загрузить файл…", null, (_, __) => ChooseFiles());
            var settingsItem = new ToolStripMenuItem("Настройки…", null, (_, __) => ShowSettings());
            settingsItem.Font = new Font(settingsItem.Font, FontStyle.Bold);
            menu.Items.AddRange(new ToolStripItem[]
            {
                statusItem,
                new ToolStripSeparator(),
                pauseItem,
                uploadItem,
                recentItem,
                new ToolStripSeparator(),
                settingsItem,
                new ToolStripMenuItem("Выйти из ShotHost", null, (_, __) => ExitThread()),
            });
            menu.Opening += (_, __) => RefreshMenu();

            tray = new NotifyIcon
            {
                Icon = LoadIcon(SystemInformation.SmallIconSize),
                ContextMenuStrip = menu,
                Visible = settings.ShowTrayIcon,
            };
            tray.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) ShowSettings();
            };
            tray.BalloonTipClicked += (_, __) =>
            {
                if (lastBalloonLink != null) OpenInBrowser(lastBalloonLink);
            };

            Autostart.RefreshPath();
            RestartWatcher();
            // Без значка в трее запуск вручную иначе выглядел бы так, будто ничего не произошло.
            if (!settings.Config.IsComplete || (!settings.ShowTrayIcon && !startedAtLogin))
                invoker.BeginInvoke(new Action(ShowSettings));
        }

        static string DefaultScreenshotsDir() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

        public Config Config => settings.Config;
        public bool ShowLinkWindow => settings.ShowLinkWindow;
        public bool ShowTrayIcon => settings.ShowTrayIcon;

        public void Update(Config config, bool showLinkWindow, bool showTrayIcon)
        {
            settings.Config = config;
            settings.ShowLinkWindow = showLinkWindow;
            settings.ShowTrayIcon = showTrayIcon;
            tray.Visible = showTrayIcon;
            Save();
            RestartWatcher();
        }

        void Save()
        {
            try
            {
                settings.Save(settingsPath);
            }
            catch (Exception e)
            {
                ShowError($"Не удалось сохранить настройки: {e.Message}");
            }
        }

        public void ShowSettingsFromAnyThread() => invoker.BeginInvoke(new Action(ShowSettings));

        public void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                // Каждый раз новое окно, чтобы форма показывала сохранённые настройки, а не старый черновик.
                settingsForm = new SettingsForm(this);
                settingsForm.Show();
            }
            settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
            settingsForm.BringToFront();
        }

        void RefreshMenu()
        {
            if (activeUploads > 0) statusItem.Text = $"Загрузка… ({activeUploads})";
            else if (watcherError != null) statusItem.Text = watcherError;
            else if (!Config.IsComplete) statusItem.Text = "Нужно заполнить настройки";
            else if (settings.IsPaused) statusItem.Text = "Слежение на паузе";
            else statusItem.Text = $"Слежу за папкой {Path.GetFileName(Config.WatchDir.TrimEnd('\\', '/'))}";

            pauseItem.Checked = settings.IsPaused;
            pauseItem.Enabled = Config.IsComplete;

            recentItem.DropDownItems.Clear();
            if (settings.Recent.Count == 0)
            {
                recentItem.DropDownItems.Add(new ToolStripMenuItem("Пока пусто") { Enabled = false });
            }
            else
            {
                foreach (var item in settings.Recent)
                {
                    var url = item.Url;
                    recentItem.DropDownItems.Add(new ToolStripMenuItem(item.Name, null, (_, __) => CopyToClipboard(url))
                    {
                        ToolTipText = "Скопировать ссылку",
                    });
                }
                recentItem.DropDownItems.Add(new ToolStripSeparator());
                recentItem.DropDownItems.Add(new ToolStripMenuItem("Очистить", null, (_, __) =>
                {
                    settings.Recent.Clear();
                    Save();
                }));
            }
        }

        void RefreshTooltip()
        {
            var text = activeUploads > 0 ? $"ShotHost: загрузка… ({activeUploads})"
                : settings.IsPaused ? "ShotHost: пауза"
                : watcher != null ? "ShotHost: слежу за папкой"
                : "ShotHost";
            // NotifyIcon.Text в .NET Framework не длиннее 63 символов.
            tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        void RestartWatcher()
        {
            watcher?.Dispose();
            watcher = null;
            watcherError = null;
            RefreshTooltip();
            if (settings.IsPaused || !Config.IsComplete) return;
            var dir = Environment.ExpandEnvironmentVariables(Config.WatchDir);
            if (!Directory.Exists(dir))
            {
                watcherError = $"Не найдена папка {dir}";
                RefreshTooltip();
                return;
            }
            // Как и на Mac, файлы, лежавшие в папке до запуска, не трогаем: события приходят только о новых.
            var newWatcher = new FileSystemWatcher(dir)
            {
                NotifyFilter = NotifyFilters.FileName,
                IncludeSubdirectories = false,
                SynchronizingObject = invoker,
                InternalBufferSize = 64 * 1024,
            };
            newWatcher.Created += (_, e) => OnNewFile(e.FullPath);
            newWatcher.Renamed += (_, e) => OnNewFile(e.FullPath);
            newWatcher.Error += (_, e) => RestartWatcher();
            try
            {
                newWatcher.EnableRaisingEvents = true;
                watcher = newWatcher;
            }
            catch (Exception e)
            {
                newWatcher.Dispose();
                watcherError = $"Не удалось следить за папкой {dir}: {e.Message}";
            }
            RefreshTooltip();
        }

        async void OnNewFile(string path)
        {
            var name = Path.GetFileName(path);
            if (Naming.IsProcessed(name) || Naming.Extension(name).Length == 0 || !inProgress.Add(path)) return;
            try
            {
                await WaitUntilWritten(path);
                if (!File.Exists(path)) return;
                var attributes = File.GetAttributes(path);
                if ((attributes & (FileAttributes.Hidden | FileAttributes.Directory)) != 0) return;
                var upload = Naming.UploadNameFor(name);
                if (upload == null) return;
                // Новый скриншот переименовывается рядом, как делал скрипт, и загружается.
                var renamed = Path.Combine(Path.GetDirectoryName(path), upload.FileName);
                try
                {
                    File.Move(path, renamed);
                }
                catch (Exception e)
                {
                    ShowError($"Не удалось переименовать {name}: {e.Message}");
                    return;
                }
                await Upload(renamed, upload);
            }
            finally
            {
                inProgress.Remove(path);
            }
        }

        /// Ждём, пока размер файла перестанет меняться и программа, которая его пишет, закроет его.
        static async Task WaitUntilWritten(string path)
        {
            long? lastSize = null;
            for (var i = 0; i < 40; i++)
            {
                await Task.Delay(250);
                try
                {
                    var size = new FileInfo(path).Length;
                    if (size > 0 && size == lastSize)
                    {
                        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                        return;
                    }
                    lastSize = size;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        void ChooseFiles()
        {
            if (!Config.IsComplete)
            {
                ShowSettings();
                return;
            }
            using (var dialog = new OpenFileDialog
            {
                Title = "Загрузить в ShotHost",
                Multiselect = true,
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp;*.heic|Все файлы|*.*",
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                // Выбранные вручную файлы загружаются под новым именем, оригинал не переименовывается.
                foreach (var file in dialog.FileNames)
                {
                    var upload = Naming.UploadNameFor(Path.GetFileName(file));
                    if (upload != null) _ = Upload(file, upload);
                }
            }
        }

        async Task Upload(string file, UploadName name)
        {
            var config = Config.Copy();
            var link = config.Link(name.Stem);
            // Ссылка нужна сразу, пока файл ещё едет на сервер.
            CopyToClipboard(link);
            activeUploads++;
            RefreshTooltip();
            try
            {
                await new Uploader(config).Upload(file, name.FileName);
                settings.Recent.Insert(0, new RecentUpload { Name = name.FileName, Url = link, Date = DateTime.Now });
                settings.Recent = settings.Recent.Take(10).ToList();
                Save();
                // Без значка в трее уведомление не показать, поэтому тогда всегда окно.
                if (settings.ShowLinkWindow || !settings.ShowTrayIcon)
                    LinkForm.ShowLink(link);
                else
                    ShowBalloon("Скриншот загружен", $"Ссылка скопирована в буфер обмена\n{link}", ToolTipIcon.Info, link);
            }
            catch (Exception e)
            {
                ShowError($"Не удалось загрузить {name.FileName}: {e.Message}");
            }
            finally
            {
                activeUploads--;
                RefreshTooltip();
            }
        }

        public static void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetDataObject(text, true, 10, 100);
            }
            catch (Exception) { }
        }

        public static void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception) { }
        }

        void ShowError(string message)
        {
            if (tray.Visible)
                ShowBalloon("Ошибка ShotHost", message, ToolTipIcon.Error, null);
            else
                MessageBox.Show(message, "Ошибка ShotHost", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void ShowBalloon(string title, string text, ToolTipIcon icon, string link)
        {
            lastBalloonLink = link;
            tray.ShowBalloonTip(10000, title, text, icon);
        }

        protected override void ExitThreadCore()
        {
            watcher?.Dispose();
            tray.Visible = false;
            tray.Dispose();
            settingsForm?.Close();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                watcher?.Dispose();
                tray.Dispose();
                menu.Dispose();
                invoker.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
