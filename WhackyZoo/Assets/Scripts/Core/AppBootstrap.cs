using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Thiết lập chung khi mở game, chạy trước scene đầu tiên.</summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // Android mặc định khoá 30 fps; hoạt ảnh thú cần 60 fps mới mượt.
            Application.targetFrameRate = 60;
        }
    }
}
