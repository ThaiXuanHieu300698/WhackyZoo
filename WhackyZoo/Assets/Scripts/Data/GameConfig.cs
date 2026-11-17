using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Số liệu cân bằng của một ván (Docs/GameSpec.md › Luật một ván).</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "WhackyZoo/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Thời gian")]
        [Tooltip("Thời gian ván mặc định (giây) khi người chơi chưa chọn trong Cài đặt.")]
        public float roundSeconds = 60f;
        [Tooltip("Các lựa chọn ở Cài đặt: Nhanh / Thường / Thử thách.")]
        public int[] roundSecondsOptions = { 30, 60, 90 };
        [Tooltip("Số giây cuối đồng hồ chuyển đỏ.")]
        public float warningSeconds = 10f;
        [Tooltip("Màn cách chơi tự đóng sau số giây này (đồng hồ ván chưa chạy).")]
        public float howToSeconds = 8f;

        [Header("Sinh thú")]
        [Tooltip("Nhịp sinh thú (giây).")]
        public float spawnInterval = 1.15f;
        [Range(0f, 1f), Tooltip("Mỗi nhịp, mỗi con đang ló có xác suất này để rụt.")]
        public float retreatChance = 0.6f;
        [Min(0)] public int spawnPerTickMin = 1;
        [Min(0)] public int spawnPerTickMax = 2;
        [Min(1), Tooltip("Số con tối đa cùng lúc trên lưới.")]
        public int maxActiveCritters = 4;

        [Header("Nước rút cuối ván (tăng dần tới giây cuối)")]
        [Tooltip("Số giây cuối bắt đầu tăng tốc; 0 = tắt.")]
        public float rushSeconds = 10f;
        [Tooltip("Nhịp sinh thú ở giây cuối cùng (giây); giữa chừng nội suy từ spawnInterval.")]
        public float rushSpawnInterval = 0.7f;
        [Min(0)] public int rushSpawnPerTickMin = 2;
        [Min(0)] public int rushSpawnPerTickMax = 3;
        [Min(1), Tooltip("Số con tối đa cùng lúc ở giây cuối cùng.")]
        public int rushMaxActiveCritters = 6;

        [Header("Tỉ lệ ra (phần còn lại là thú thường, chia đều)")]
        [Range(0f, 1f)] public float bonusChance = 0.1f;
        [Range(0f, 1f)] public float obstacleChance = 0.1f;

        [Header("Nhịp hoạt ảnh ảnh hưởng luật chơi (giây)")]
        [Tooltip("Thú bị đập choáng bao lâu rồi biến mất.")]
        public float stunDuration = 0.43f;
        public float riseDuration = 0.42f;
        [Tooltip("Thời gian rụt xuống; thú mới sinh sau khoảng này.")]
        public float retreatDuration = 0.24f;
        [Range(0f, 1f), Tooltip("Lúc đang trồi lên / rụt xuống, chỉ tính đập trúng khi thú còn nhô khỏi hố ít nhất phần này chiều cao.")]
        public float minVisibleToHit = 0.4f;

        [Header("Rung")]
        [Tooltip("Độ dài rung khi đập trúng (mili giây), nếu người chơi bật Rung trong Cài đặt.")]
        public int hitVibrationMs = 25;

        [Header("Điểm, combo, xu")]
        public int comboStart = 1;
        [Tooltip("Combo rơi về mức này khi đập nhầm vật cản.")]
        public int comboFloor = 1;
        public int minScore = 0;
        [Tooltip("Xu nhận = điểm ÷ số này (làm tròn xuống).")]
        public int coinDivisor = 10;
    }
}
