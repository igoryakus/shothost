using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ShotHost.Core;

namespace ShotHost
{
    /// Окно настроек с теми же полями, что на Mac.
    sealed class SettingsForm : Form
    {
        readonly TrayApp app;
        readonly TableLayoutPanel grid = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Top };
        readonly TextBox watchDir = Field();
        readonly TextBox host = Field("203.0.113.10");
        readonly NumericUpDown port = new NumericUpDown { Minimum = 1, Maximum = 65535, Width = 90, Anchor = AnchorStyles.Left };
        readonly TextBox user = Field("root");
        readonly TextBox keyPath = Field();
        readonly TextBox remoteDir = Field("/var/www/html/shothost/");
        readonly TextBox baseUrl = Field("https://example.com");
        readonly Label serverHint = Hint("");
        readonly CheckBox showLinkWindow = Check("Показывать окно со ссылкой после загрузки");
        readonly CheckBox showTrayIcon = Check("Показывать значок в трее");
        readonly Label trayHint = Hint("Без значка ShotHost продолжает загружать скриншоты. Чтобы открыть настройки, запустите ShotHost.exe ещё раз.");
        readonly CheckBox launchAtLogin = Check("Запускать при входе в Windows");
        readonly Button checkButton = new Button { Text = "Проверить соединение", AutoSize = true };
        readonly Button saveButton = new Button { Text = "Сохранить", AutoSize = true };
        readonly Label checkResult = Hint("");

        public SettingsForm(TrayApp app)
        {
            this.app = app;
            // Размеры ниже заданы для 100%, при другом масштабе экрана WinForms пересчитывает их.
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = $"Настройки ShotHost {TrayApp.AppVersion}".TrimEnd();
            Font = SystemFonts.MessageBoxFont;
            Icon = TrayApp.LoadIcon(SystemInformation.IconSize);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Section("Скриншоты");
            Row("Папка", watchDir, Browse(() => ChooseFolder(watchDir)));
            Span(Hint("Новые файлы в этой папке загружаются автоматически. Win+PrintScreen и «Ножницы» сохраняют сюда: Изображения\\Снимки экрана."));

            Section("Сервер");
            Row("Сервер (IP или домен)", host);
            Row("SSH-порт", port);
            Row("Пользователь", user);
            Row("SSH-ключ", keyPath, Browse(() => ChooseKey()));

            Section("ShotHost на сервере");
            Row("Папка ShotHost", remoteDir);
            Row("Сайт", baseUrl);
            Span(serverHint);

            Section("");
            Span(showLinkWindow);
            Span(Hint("В окне кнопка «Показать» открывает скриншот в браузере. Если выключено, приходит уведомление."));
            Span(showTrayIcon);
            Span(trayHint);
            Span(launchAtLogin);

            var buttons = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            checkButton.Anchor = AnchorStyles.Left;
            saveButton.Anchor = AnchorStyles.Right;
            buttons.Controls.Add(checkButton, 0, 0);
            buttons.Controls.Add(saveButton, 1, 0);
            Span(buttons);
            Span(checkResult);
            Controls.Add(grid);

            var config = app.Config;
            watchDir.Text = config.WatchDir;
            host.Text = config.Host;
            port.Value = Math.Max(1, Math.Min(65535, config.Port));
            user.Text = config.User;
            keyPath.Text = config.KeyPath;
            remoteDir.Text = config.RemoteDir;
            baseUrl.Text = config.BaseUrl;
            showLinkWindow.Checked = app.ShowLinkWindow;
            showTrayIcon.Checked = app.ShowTrayIcon;
            try
            {
                launchAtLogin.Checked = Autostart.IsEnabled;
            }
            catch (Exception) { }

            foreach (var box in new[] { watchDir, host, user, keyPath, remoteDir, baseUrl })
                box.TextChanged += (_, __) => RefreshState();
            port.ValueChanged += (_, __) => RefreshState();
            showTrayIcon.CheckedChanged += (_, __) => RefreshState();
            checkButton.Click += async (_, __) => await Check();
            saveButton.Click += (_, __) => Save();
            AcceptButton = saveButton;
            RefreshState();
            ResumeLayout(false);
            PerformLayout();
        }

        Config Draft => new Config
        {
            WatchDir = watchDir.Text.Trim(),
            Host = host.Text.Trim(),
            Port = (int)port.Value,
            User = user.Text.Trim(),
            KeyPath = keyPath.Text.Trim(),
            RemoteDir = remoteDir.Text.Trim(),
            BaseUrl = baseUrl.Text.Trim(),
        };

        void RefreshState()
        {
            var draft = Draft;
            var hint = $"Файлы загружаются по SSH в {draft.UploadDir}. Ссылка строится из сайта и названия папки, поэтому адрес отдельно вводить не нужно.";
            if (draft.BaseUrl.Length > 0)
                hint += $"\nПример ссылки: {draft.Link("screenshot_0123456789abcdef0123")}";
            serverHint.Text = hint;
            trayHint.Visible = !showTrayIcon.Checked;
            saveButton.Enabled = draft.IsComplete;
            checkButton.Enabled = draft.Host.Length > 0 && draft.User.Length > 0 && draft.KeyPath.Length > 0;
        }

        async System.Threading.Tasks.Task Check()
        {
            checkButton.Enabled = false;
            checkButton.Text = "Проверяю…";
            checkResult.Text = "";
            try
            {
                await new Uploader(Draft).TestConnection();
                checkResult.ForeColor = Color.DarkGreen;
                checkResult.Text = "✔ Соединение работает, папка на сервере доступна для записи.";
            }
            catch (Exception e)
            {
                checkResult.ForeColor = Color.Firebrick;
                checkResult.Text = $"✘ {e.Message}";
            }
            checkButton.Text = "Проверить соединение";
            if (!IsDisposed) RefreshState();
        }

        void Save()
        {
            if (!Directory.Exists(Environment.ExpandEnvironmentVariables(watchDir.Text.Trim())))
            {
                MessageBox.Show(this, $"Папка {watchDir.Text} не найдена.", "ShotHost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                Autostart.SetEnabled(launchAtLogin.Checked);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, $"Автозапуск: {e.Message}", "ShotHost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            app.Update(Draft, showLinkWindow.Checked, showTrayIcon.Checked);
            Close();
        }

        void ChooseFolder(TextBox target)
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = target.Text, ShowNewFolderButton = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.SelectedPath;
            }
        }

        void ChooseKey()
        {
            var sshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            using (var dialog = new OpenFileDialog
            {
                Title = "Выберите приватный ключ (файл без .pub)",
                InitialDirectory = Directory.Exists(sshDir) ? sshDir : "",
                Filter = "Все файлы|*.*",
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) keyPath.Text = dialog.FileName;
            }
        }

        void Section(string title)
        {
            var label = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, grid.RowCount == 0 ? 0 : 14, 0, 4),
            };
            Span(label);
        }

        void Row(string title, Control field, Control extra = null)
        {
            var row = grid.RowCount++;
            grid.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) }, 0, row);
            grid.Controls.Add(field, 1, row);
            if (extra != null) grid.Controls.Add(extra, 2, row);
        }

        void Span(Control control)
        {
            var row = grid.RowCount++;
            grid.Controls.Add(control, 0, row);
            grid.SetColumnSpan(control, 3);
        }

        static TextBox Field(string placeholder = null)
        {
            var box = new TextBox { Width = 320, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            if (placeholder != null) box.HandleCreated += (_, __) => Cue.Set(box, placeholder);
            return box;
        }

        static Label Hint(string text) => new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 2, 0, 6),
        };

        static CheckBox Check(string text) => new CheckBox { Text = text, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };

        static Button Browse(Action choose)
        {
            var button = new Button { Text = "Выбрать…", AutoSize = true, Anchor = AnchorStyles.Left };
            button.Click += (_, __) => choose();
            return button;
        }

        /// Серая подсказка в пустом поле (EM_SETCUEBANNER).
        static class Cue
        {
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

            public static void Set(TextBox box, string text) => SendMessage(box.Handle, 0x1501, (IntPtr)1, text);
        }
    }
}
