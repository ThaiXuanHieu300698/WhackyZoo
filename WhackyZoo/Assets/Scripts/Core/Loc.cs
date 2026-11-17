using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Đa ngôn ngữ (Tiếng Việt / English — Docs/GameSpec.md › Cài đặt). Khoá dịch chính là câu tiếng Việt gốc, bảng
    /// dịch ở Resources/Localization/en.tsv (mỗi dòng: "tiếng Việt [Tab] English"; \n = xuống dòng). Câu không có trong
    /// bảng thì giữ nguyên tiếng Việt. Câu có chỗ trống dùng {0}, {1}… với <see cref="F"/>.
    /// Đổi ngôn ngữ: <see cref="SetLanguage"/> → phát <see cref="Changed"/> để chữ trên màn đổi ngay.
    /// </summary>
    public static class Loc
    {
        public static event Action Changed;

        static Dictionary<string, string> en;

        public static bool English => SaveSystem.Data.settings.language == "en";

        /// <summary>Dịch câu tiếng Việt sang ngôn ngữ đang chọn.</summary>
        public static string T(string vi)
        {
            if (string.IsNullOrEmpty(vi) || !English) return vi;
            return EnglishOf(vi) ?? vi;
        }

        /// <summary>Dịch rồi điền chỗ trống: <c>Loc.F("Ngày {0}/{1}", 3, 7)</c>.</summary>
        public static string F(string viFormat, params object[] args) => string.Format(T(viFormat), args);

        /// <summary>Bản tiếng Anh của câu (null nếu chưa có trong bảng) — để biết chữ trên màn còn là chữ gốc hay không.</summary>
        public static string EnglishOf(string vi)
        {
            Load();
            return vi != null && en.TryGetValue(vi, out var s) ? s : null;
        }

        public static void SetLanguage(string lang)
        {
            var s = SaveSystem.Data.settings;
            if (s.language == lang) return;
            s.language = lang;
            SaveSystem.Save();
            Changed?.Invoke();
        }

        static void Load()
        {
            if (en != null) return;
            en = new Dictionary<string, string>();
            var asset = Resources.Load<TextAsset>("Localization/en");
            if (asset == null) return;
            foreach (var raw in asset.text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string vi = Unescape(line.Substring(0, tab)), eng = Unescape(line.Substring(tab + 1));
                if (eng.Length > 0) en[vi] = eng;
            }
        }

        static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");
    }
}
