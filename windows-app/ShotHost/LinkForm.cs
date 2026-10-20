using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShotHost
{
    /// Окно после загрузки, как на Mac: «ОК» или «Показать», само закрывается через 10 секунд.
    sealed class LinkForm : Form
    {
        static LinkForm current;
        readonly Timer closeTimer = new Timer { Interval = 10000 };

        public static void ShowLink(string link)
        {
            current?.Close();
            current = new LinkForm(link);
            current.Show();
            current.Activate();
        }

        LinkForm(string link)
        {
            // Размеры ниже заданы для 100%, при другом масштабе экрана WinForms пересчитывает их.
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Скриншот загружен";
            Font = SystemFonts.MessageBoxFont;
            Icon = TrayApp.LoadIcon(SystemInformation.IconSize);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);

            var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
            layout.Controls.Add(new Label
            {
                Text = "Ссылка скопирована в буфер обмена",
                Font = new Font(Font.FontFamily, Font.Size * 1.15f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
            });
            layout.Controls.Add(new TextBox
            {
                Text = link,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BackColor,
                ForeColor = SystemColors.GrayText,
                Width = 420,
                TabStop = false,
                Margin = new Padding(0, 0, 0, 12),
            });

            var ok = new Button { Text = "ОК", AutoSize = true, DialogResult = DialogResult.Cancel };
            var open = new Button { Text = "Показать", AutoSize = true };
            ok.Click += (_, __) => Close();
            open.Click += (_, __) =>
            {
                TrayApp.OpenInBrowser(link);
                Close();
            };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = Padding.Empty,
            };
            buttons.Controls.Add(open);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons);
            Controls.Add(layout);

            AcceptButton = open;
            CancelButton = ok;
            closeTimer.Tick += (_, __) => Close();
            Shown += (_, __) =>
            {
                open.Focus();
                closeTimer.Start();
            };
            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            closeTimer.Dispose();
            if (current == this) current = null;
            base.OnFormClosed(e);
        }
    }
}
