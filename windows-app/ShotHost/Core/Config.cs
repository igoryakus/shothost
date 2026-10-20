using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ShotHost.Core
{
    /// Настройки соединения, те же поля, что в приложении для macOS.
    [DataContract]
    public sealed class Config
    {
        /// Папка, в которую делаются скриншоты.
        [DataMember] public string WatchDir { get; set; } = "";
        /// IP или домен сервера.
        [DataMember] public string Host { get; set; } = "";
        [DataMember] public int Port { get; set; } = 22;
        [DataMember] public string User { get; set; } = "";
        /// Папка проекта ShotHost на сервере; файлы загружаются в её подпапку files/.
        [DataMember] public string RemoteDir { get; set; } = "/var/www/html/shothost/";
        /// Приватный SSH-ключ (без .pub).
        [DataMember] public string KeyPath { get; set; } = "";
        /// Сайт, на котором открывается ShotHost: https://домен или IP, без пути.
        /// Путь в ссылке берётся из названия папки проекта, поэтому ссылка и папка не могут разойтись.
        [DataMember] public string BaseUrl { get; set; } = "";

        // DataContractJsonSerializer не вызывает конструктор: значения для полей, которых нет в файле.
        [OnDeserializing]
        void SetDefaults(StreamingContext context)
        {
            WatchDir = Host = User = KeyPath = BaseUrl = "";
            Port = 22;
            RemoteDir = "/var/www/html/shothost/";
        }

        public Config Copy() => (Config)MemberwiseClone();

        public bool IsComplete =>
            new[] { WatchDir, Host, User, RemoteDir, KeyPath, BaseUrl }.All(v => !string.IsNullOrWhiteSpace(v));

        /// Название папки проекта, оно же путь на сайте: /var/www/html/shothost/ → shothost.
        public string ProjectName => (RemoteDir ?? "").TrimEnd('/').Split('/').Last();

        public string Link(string stem) => $"{Site(BaseUrl).TrimEnd('/')}/{ProjectName}/{stem}";

        /// Папка, куда загружаются файлы: files/ внутри папки проекта.
        public string UploadDir => (RemoteDir.EndsWith("/") ? RemoteDir : RemoteDir + "/") + "files/";

        public string RemoteTarget(string fileName) => $"{User}@{Host}:{UploadDir}{fileName}";

        /// Если в поле сайта вписали адрес с путём (https://example.com/shothost/), путь отбрасывается.
        public static string Site(string address)
        {
            var trimmed = (address ?? "").Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return $"{uri.Scheme}://{uri.Authority}";
            return trimmed;
        }
    }

    [DataContract]
    public sealed class RecentUpload
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public string Url { get; set; }
        [DataMember] public DateTime Date { get; set; }
    }

    /// Всё, что приложение помнит между запусками: %APPDATA%\ShotHost\settings.json.
    [DataContract]
    public sealed class Settings
    {
        [DataMember] public Config Config { get; set; } = new Config();
        [DataMember] public bool IsPaused { get; set; }
        /// После загрузки показывать окно со ссылкой, а не уведомление.
        [DataMember] public bool ShowLinkWindow { get; set; } = true;
        [DataMember] public bool ShowTrayIcon { get; set; } = true;
        [DataMember] public List<RecentUpload> Recent { get; set; } = new List<RecentUpload>();

        // Как и в Config: значения для полей, которых нет в файле.
        [OnDeserializing]
        void SetDefaults(StreamingContext context)
        {
            Config = new Config();
            ShowLinkWindow = true;
            ShowTrayIcon = true;
            Recent = new List<RecentUpload>();
        }

        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShotHost", "settings.json");

        static DataContractJsonSerializer Serializer => new DataContractJsonSerializer(typeof(Settings),
            new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat("o") });

        public static Settings Load(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var settings = (Settings)Serializer.ReadObject(stream);
                    settings.Config = settings.Config ?? new Config();
                    settings.Recent = settings.Recent ?? new List<RecentUpload>();
                    return settings;
                }
            }
            catch (Exception)
            {
                return new Settings();
            }
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp";
            using (var stream = File.Create(temp))
                Serializer.WriteObject(stream, this);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
