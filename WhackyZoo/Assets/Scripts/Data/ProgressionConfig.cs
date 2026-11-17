using System;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Số liệu tiến trình (Docs/GameSpec.md › Tiến trình + các điểm người dùng đã chốt): nhiệm vụ, rương điểm nhiệm vụ,
    /// kinh nghiệm / cấp độ. Điểm danh tháng ở DailyRewardMonth.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "WhackyZoo/Progression Config")]
    public class ProgressionConfig : ScriptableObject
    {
        [Header("Nhiệm vụ")]
        public MissionData[] missions;
        [Tooltip("Điểm nhiệm vụ tối đa (thang của thanh rương).")]
        public int maxMissionPoints = 100;
        [Tooltip("Mốc mở rương (điểm nhiệm vụ).")]
        public int[] chestPoints = { 30, 60, 100 };
        [Tooltip("Xu trong từng rương, cùng thứ tự với chestPoints.")]
        public int[] chestRewards = { 200, 500, 1200 };

        [Header("Cấp độ")]
        [Tooltip("Kinh nghiệm mỗi ván = điểm ván ÷ số này.")]
        public int xpDivisor = 10;
        [Tooltip("Lên cấp n cần xpPerLevel × n kinh nghiệm.")]
        public int xpPerLevel = 100;
        [Tooltip("Lên cấp được levelUpCoins × cấp mới xu.")]
        public int levelUpCoins = 50;

        public int XpToNext(int level) => Mathf.Max(1, xpPerLevel * Mathf.Max(1, level));
    }
}
