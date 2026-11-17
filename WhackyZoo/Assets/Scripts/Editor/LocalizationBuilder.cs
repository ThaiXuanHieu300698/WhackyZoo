using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Gắn LocalizedText vào mọi Text trong scene vừa dựng mà chữ có trong bảng dịch (Resources/Localization/en.txt),
    /// kể cả dạng "tên (số)" như chip map ở Cửa hàng. Chữ do code đặt lúc chạy được dịch trong code (Loc.T / Loc.F).
    /// </summary>
    internal static class LocalizationBuilder
    {
        internal static int Tag(Transform root)
        {
            AssetDatabase.ImportAsset("Assets/Resources/Localization/en.txt");
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t.GetComponent<LocalizedText>() != null || string.IsNullOrEmpty(t.text)) continue;
                string key = t.text, tail = "";
                if (Loc.EnglishOf(key) == null)
                {
                    var m = Regex.Match(key, @"^(.+?)( \(\d+\))$"); // "Trên cạn (8)"
                    if (!m.Success || Loc.EnglishOf(m.Groups[1].Value) == null) continue;
                    key = m.Groups[1].Value;
                    tail = m.Groups[2].Value;
                }
                t.gameObject.AddComponent<LocalizedText>().SetKey(key, tail);
                n++;
            }
            return n;
        }
    }
}
