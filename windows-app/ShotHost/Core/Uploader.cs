using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShotHost.Core
{
    public sealed class UploadException : Exception
    {
        public UploadException(string message) : base(message) { }
    }

    /// Загрузка через scp.exe и ssh.exe из OpenSSH, который входит в Windows 10 и 11.
    public sealed class Uploader
    {
        readonly Config config;

        public Uploader(Config config)
        {
            this.config = config;
        }

        IEnumerable<string> SshOptions => new[]
        {
            "-i", Environment.ExpandEnvironmentVariables(config.KeyPath),
            "-o", "BatchMode=yes",
            "-o", "StrictHostKeyChecking=accept-new",
            "-o", "ConnectTimeout=10",
        };

        public async Task Upload(string localPath, string remoteName)
        {
            var args = new[] { "-P", config.Port.ToString() }.Concat(SshOptions)
                .Concat(new[] { localPath, config.RemoteTarget(remoteName) });
            var (status, output) = await Run(Tool("scp.exe"), args);
            if (status != 0)
                throw new UploadException(output.Length == 0 ? $"scp завершился с кодом {status}" : output);
        }

        /// Проверяет, что сервер доступен по ключу и папка на нём существует и доступна для записи.
        public async Task TestConnection()
        {
            var dir = config.UploadDir.Replace("'", "'\\''");
            var args = new[] { "-p", config.Port.ToString() }.Concat(SshOptions)
                .Concat(new[] { $"{config.User}@{config.Host}", $"test -d '{dir}' && test -w '{dir}'" });
            var (status, output) = await Run(Tool("ssh.exe"), args);
            if (status == 0) return;
            if (status == 1)
                throw new UploadException($"Соединение есть, но папка {config.UploadDir} не найдена или недоступна для записи");
            throw new UploadException(output.Length == 0 ? $"ssh завершился с кодом {status}" : output);
        }

        /// OpenSSH лежит в System32\OpenSSH; если его там нет, ищем в PATH (например, Git for Windows).
        static string Tool(string name)
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            foreach (var dir in new[] { "System32", "Sysnative" })
            {
                var path = Path.Combine(windows, dir, "OpenSSH", name);
                if (File.Exists(path)) return path;
            }
            var found = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
                .Where(d => d.Length > 0)
                .Select(d => Path.Combine(d.Trim('"'), name))
                .FirstOrDefault(File.Exists);
            return found ?? throw new UploadException(
                $"Не найден {name}. Включите OpenSSH: Параметры → Система → Дополнительные компоненты → Клиент OpenSSH.");
        }

        static Task<(int, string)> Run(string executable, IEnumerable<string> arguments)
        {
            var result = new TaskCompletionSource<(int, string)>();
            var output = new StringBuilder();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executable, string.Join(" ", arguments.Select(QuoteArgument)))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true,
            };
            DataReceivedEventHandler collect = (_, e) =>
            {
                if (e.Data == null) return;
                lock (output) output.AppendLine(e.Data);
            };
            process.OutputDataReceived += collect;
            process.ErrorDataReceived += collect;
            process.Exited += (_, __) =>
            {
                // Дочитываем вывод: Exited может прийти раньше последних строк.
                process.WaitForExit();
                string text;
                lock (output) text = output.ToString().Trim();
                result.TrySetResult((process.ExitCode, text));
                process.Dispose();
            };
            try
            {
                process.Start();
                process.StandardInput.Close();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                result.TrySetException(new UploadException($"Не удалось запустить {Path.GetFileName(executable)}: {e.Message}"));
            }
            return result.Task;
        }

        /// Экранирование одного аргумента по правилам CommandLineToArgvW.
        public static string QuoteArgument(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0) return arg;
            var sb = new StringBuilder("\"");
            var backslashes = 0;
            foreach (var c in arg)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                }
                else
                {
                    sb.Append('\\', backslashes);
                }
                backslashes = 0;
                sb.Append(c);
            }
            sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }
    }
}
