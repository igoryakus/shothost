using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ShotHost.Core
{
    /// Правила именования файлов, те же, что в приложении для macOS и в shothost.sh:
    /// транслитерация кириллицы, замена прочих символов на "_" и соль из 20 hex-символов.
    public static class Naming
    {
        static readonly Dictionary<string, string> Translit = new Dictionary<string, string>
        {
            ["а"] = "a", ["б"] = "b", ["в"] = "v", ["г"] = "g", ["д"] = "d", ["е"] = "e", ["ё"] = "e", ["ж"] = "zh", ["з"] = "z", ["и"] = "i",
            ["й"] = "j", ["к"] = "k", ["л"] = "l", ["м"] = "m", ["н"] = "n", ["о"] = "o", ["п"] = "p", ["р"] = "r", ["с"] = "s", ["т"] = "t",
            ["у"] = "u", ["ф"] = "f", ["х"] = "h", ["ц"] = "c", ["ч"] = "ch", ["ш"] = "sh", ["щ"] = "shh", ["ъ"] = "", ["ы"] = "y", ["ь"] = "",
            ["э"] = "e", ["ю"] = "yu", ["я"] = "ya",
            ["А"] = "A", ["Б"] = "B", ["В"] = "V", ["Г"] = "G", ["Д"] = "D", ["Е"] = "E", ["Ё"] = "E", ["Ж"] = "ZH", ["З"] = "Z", ["И"] = "I",
            ["Й"] = "J", ["К"] = "K", ["Л"] = "L", ["М"] = "M", ["Н"] = "N", ["О"] = "O", ["П"] = "P", ["Р"] = "R", ["С"] = "S", ["Т"] = "T",
            ["У"] = "U", ["Ф"] = "F", ["Х"] = "H", ["Ц"] = "C", ["Ч"] = "CH", ["Ш"] = "SH", ["Щ"] = "SHH", ["Ъ"] = "", ["Ы"] = "Y", ["Ь"] = "",
            ["Э"] = "E", ["Ю"] = "YU", ["Я"] = "YA",
        };

        static readonly Regex ProcessedPattern = new Regex("_[a-f0-9]{20}\\.[a-zA-Z0-9]+$");

        /// Имя файла без расширения, пригодное для ссылки.
        /// Символы перебираются целиком (эмодзи, буква с ударением), как в Swift,
        /// поэтому каждый превращается в один "_", а «й» в NFD-форме транслитерируется.
        public static string CleanName(string name)
        {
            var result = new StringBuilder();
            var elements = StringInfo.GetTextElementEnumerator(name.Normalize(NormalizationForm.FormC));
            while (elements.MoveNext())
            {
                var ch = (string)elements.Current;
                if (Translit.TryGetValue(ch, out var latin))
                    result.Append(latin);
                else if (ch.Length == 1 && IsAllowed(ch[0]))
                    result.Append(ch);
                else
                    result.Append('_');
            }
            return result.ToString();
        }

        static bool IsAllowed(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';

        public static string MakeSalt()
        {
            var bytes = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// Файл уже переименован ShotHost (есть соль перед расширением).
        public static bool IsProcessed(string fileName) => ProcessedPattern.IsMatch(fileName);

        /// Расширение без точки; у ".gitignore" и "file." его нет, как у pathExtension в macOS.
        public static string Extension(string fileName)
        {
            var dot = fileName.LastIndexOf('.');
            return dot <= 0 || dot == fileName.Length - 1 ? "" : fileName.Substring(dot + 1);
        }

        /// Новое имя для загрузки: Stem идёт в ссылку, FileName — на сервер.
        /// Возвращает null для файлов без расширения: их не загружаем.
        public static UploadName UploadNameFor(string fileName, string salt = null)
        {
            var ext = Extension(fileName);
            if (ext.Length == 0) return null;
            var baseName = fileName.Substring(0, fileName.Length - ext.Length - 1);
            var stem = $"{CleanName(baseName)}_{salt ?? MakeSalt()}";
            return new UploadName(stem, $"{stem}.{CleanName(ext)}");
        }
    }

    public sealed class UploadName
    {
        public string Stem { get; }
        public string FileName { get; }

        public UploadName(string stem, string fileName)
        {
            Stem = stem;
            FileName = fileName;
        }
    }
}
