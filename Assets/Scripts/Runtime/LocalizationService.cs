using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SkyCourier
{
    public enum GameLanguage
    {
        SimplifiedChinese,
        English
    }

    public static class LocalizationService
    {
        private const string ResourcePath = "Localization/localization";
        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>();
        private static bool loaded;

        public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.SimplifiedChinese;
        public static bool IsEnglish => CurrentLanguage == GameLanguage.English;

        public static void Initialize(GameLanguage language)
        {
            EnsureLoaded();
            CurrentLanguage = Enum.IsDefined(typeof(GameLanguage), language)
                ? language : GameLanguage.SimplifiedChinese;
        }

        public static void SetLanguage(GameLanguage language)
        {
            Initialize(language);
        }

        public static string Text(string key, string chineseFallback, params object[] arguments)
        {
            EnsureLoaded();
            Dictionary<string, string> table = IsEnglish ? English : Chinese;
            string value = table.TryGetValue(key, out string localized) && !string.IsNullOrWhiteSpace(localized)
                ? localized : chineseFallback ?? key;
            if (arguments == null || arguments.Length == 0)
                return value;
            try
            {
                return string.Format(CultureInfo.InvariantCulture, value, arguments);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        public static bool ValidateKeys(IEnumerable<string> keys, out string error)
        {
            EnsureLoaded();
            string[] missing = (keys ?? Array.Empty<string>())
                .Where(key => string.IsNullOrWhiteSpace(key) || !Chinese.ContainsKey(key) ||
                    !English.ContainsKey(key) || string.IsNullOrWhiteSpace(Chinese[key]) ||
                    string.IsNullOrWhiteSpace(English[key]))
                .Distinct().OrderBy(key => key).ToArray();
            error = missing.Length == 0 ? null : $"缺失双语文本键：{string.Join(", ", missing)}";
            return missing.Length == 0;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Localization resource not found: Resources/{ResourcePath}.txt");

            Chinese.Clear();
            English.Clear();
            string[] lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawLine in lines)
            {
                if (rawLine.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] columns = rawLine.Split('\t');
                if (columns.Length < 3)
                    continue;
                string key = columns[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;
                Chinese[key] = Unescape(columns[1]);
                English[key] = Unescape(columns[2]);
            }
            loaded = true;
        }

        private static string Unescape(string value)
        {
            return (value ?? string.Empty).Replace("\\n", "\n").Replace("\\t", "\t");
        }
    }
}
