using System.IO;
using System.Linq;
using System.Text;
using ShotHost.Core;
using Xunit;

namespace ShotHost.Tests
{
    // Те же проверки, что в macos-app/Tests/ShotHostCoreTests/NamingTests.swift, чтобы обе версии называли файлы одинаково.
    public class NamingTests
    {
        [Fact]
        public void TransliteratesCyrillic()
        {
            Assert.Equal("Snimok_ekrana", Naming.CleanName("Снимок экрана"));
            Assert.Equal("SHHuka_Ezh", Naming.CleanName("Щука Ёж"));
        }

        [Fact]
        public void TransliteratesDecomposedCyrillic()
        {
            Assert.Equal("Moj", Naming.CleanName("Мой".Normalize(NormalizationForm.FormD)));
        }

        [Fact]
        public void ReplacesOtherCharacters()
        {
            Assert.Equal("a_b_1_", Naming.CleanName("a b(1)"));
            Assert.Equal("10.10.2026_v_12.00", Naming.CleanName("10.10.2026 в 12.00"));
            Assert.Equal("a__b", Naming.CleanName("a😀 b"));
        }

        [Fact]
        public void Salt()
        {
            var salt = Naming.MakeSalt();
            Assert.Equal(20, salt.Length);
            Assert.All(salt, c => Assert.Contains(c.ToString(), "0123456789abcdef"));
        }

        [Fact]
        public void UploadNameIsRecognizedAsProcessed()
        {
            var name = Naming.UploadNameFor("Снимок экрана.png", "0123456789abcdef0123");
            Assert.Equal("Snimok_ekrana_0123456789abcdef0123", name.Stem);
            Assert.Equal("Snimok_ekrana_0123456789abcdef0123.png", name.FileName);
            Assert.True(Naming.IsProcessed(name.FileName));
            Assert.False(Naming.IsProcessed("Снимок экрана.png"));
        }

        [Fact]
        public void WindowsScreenshotName()
        {
            var name = Naming.UploadNameFor("Снимок экрана 2026-10-20 082233.png", "0123456789abcdef0123");
            Assert.Equal("Snimok_ekrana_2026-10-20_082233_0123456789abcdef0123.png", name.FileName);
        }

        [Fact]
        public void SkipsFilesWithoutExtension()
        {
            Assert.Null(Naming.UploadNameFor("README"));
            Assert.Null(Naming.UploadNameFor(".gitignore"));
            Assert.Null(Naming.UploadNameFor("file."));
        }

        [Fact]
        public void Link()
        {
            var config = new Config { BaseUrl = "https://example.com/shothost" };
            Assert.Equal("https://example.com/shothost/a_1", config.Link("a_1"));
            config.BaseUrl = "https://example.com/shothost/";
            Assert.Equal("https://example.com/shothost/a_1", config.Link("a_1"));
        }
    }

    public class ConfigTests
    {
        [Fact]
        public void UploadsIntoFilesInsideProjectDir()
        {
            var config = new Config { User = "u", Host = "h", RemoteDir = "/var/www/shothost" };
            Assert.Equal("/var/www/shothost/files/", config.UploadDir);
            Assert.Equal("u@h:/var/www/shothost/files/a.png", config.RemoteTarget("a.png"));
        }

        [Fact]
        public void LinkUsesSiteAndProjectFolderName()
        {
            var config = new Config { RemoteDir = "/var/www/yakus/shothost/", BaseUrl = "https://yakus.ru/" };
            Assert.Equal("https://yakus.ru/shothost/s_1", config.Link("s_1"));
        }

        [Fact]
        public void AddressWithPathBecomesSite()
        {
            Assert.Equal("https://yakus.ru", Config.Site("https://yakus.ru/shothost/"));
            Assert.Equal("http://203.0.113.10:8080", Config.Site("http://203.0.113.10:8080/shothost"));
            Assert.Equal("https://yakus.ru", Config.Site("https://yakus.ru"));
        }

        [Fact]
        public void SettingsRoundTrip()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
            var settings = new Settings { IsPaused = true, ShowTrayIcon = false };
            settings.Config.Host = "yakus.ru";
            settings.Recent.Add(new RecentUpload { Name = "a.png", Url = "https://yakus.ru/shothost/a" });
            settings.Save(path);
            settings.Save(path);
            var loaded = Settings.Load(path);
            Assert.Equal("yakus.ru", loaded.Config.Host);
            Assert.Equal(22, loaded.Config.Port);
            Assert.True(loaded.IsPaused);
            Assert.False(loaded.ShowTrayIcon);
            Assert.True(loaded.ShowLinkWindow);
            Assert.Equal("a.png", loaded.Recent.Single().Name);
        }

        [Fact]
        public void MissingSettingsGiveDefaults()
        {
            var loaded = Settings.Load(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Assert.True(loaded.ShowLinkWindow);
            Assert.Equal("/var/www/html/shothost/", loaded.Config.RemoteDir);
        }
    }

    public class QuoteTests
    {
        [Theory]
        [InlineData("-P", "-P")]
        [InlineData(@"C:\Users\Игорь\Pictures\a b.png", "\"C:\\Users\\Игорь\\Pictures\\a b.png\"")]
        [InlineData("", "\"\"")]
        [InlineData("test -d '/x/' && test -w '/x/'", "\"test -d '/x/' && test -w '/x/'\"")]
        [InlineData("a\"b", "\"a\\\"b\"")]
        [InlineData(@"C:\dir with space\", "\"C:\\dir with space\\\\\"")]
        public void QuotesLikeCommandLineToArgv(string arg, string expected)
        {
            Assert.Equal(expected, Uploader.QuoteArgument(arg));
        }
    }
}
