using UnityEditor;
using UnityEngine;

namespace WhackyZoo.EditorTools
{
    /// <summary>Công cụ cho người test (chỉ có trong Editor, không vào bản build).</summary>
    internal static class TestTools
    {
        const int TestCoins = 100000;

        /// <summary>Cộng 100.000 xu vào file lưu. Dùng được cả khi đang Play (Cửa hàng cập nhật số xu ngay).</summary>
        [MenuItem("WhackyZoo/Test/Cộng 100.000 xu")]
        static void AddCoins()
        {
            SaveSystem.AddCoins(TestCoins);
            SaveSystem.Save();
            Debug.Log($"[WhackyZoo] Đã cộng {TestCoins} xu. Hiện có {SaveSystem.Data.coins} xu.");
        }
    }
}
