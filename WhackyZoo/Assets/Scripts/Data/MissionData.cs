using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Tab của nhiệm vụ: ngày (làm mới 00:00), tuần (làm mới 00:00 thứ Hai), thành tựu (không làm mới).</summary>
    public enum MissionPeriod
    {
        Daily,
        Weekly,
        Achievement,
    }

    /// <summary>Chỉ số mà nhiệm vụ theo dõi, cập nhật sau mỗi ván.</summary>
    public enum MissionStat
    {
        [Tooltip("Combo cao nhất đạt được trong một ván (lấy lớn nhất trong kỳ).")]
        BestCombo,
        [Tooltip("Tổng số thú thường + thưởng đã đập trúng.")]
        CrittersHit,
        [Tooltip("Tổng số con thưởng vàng của mọi map đã đập trúng.")]
        BonusHit,
        [Tooltip("Số ván đã chơi xong.")]
        RoundsPlayed,
        [Tooltip("Số lần xem quảng cáo nhân đôi xu (bước 6).")]
        AdsWatched,
        [Tooltip("Số map khác nhau đã chơi trong kỳ.")]
        MapsPlayed,
        [Tooltip("Điểm cao nhất trong một ván.")]
        BestScore,
    }

    /// <summary>Một nhiệm vụ / thành tựu (Docs/GameSpec.md › Tiến trình, Docs/DesignSource/Missions25D.dc.html).</summary>
    [CreateAssetMenu(fileName = "Mission", menuName = "WhackyZoo/Mission")]
    public class MissionData : ScriptableObject
    {
        [Tooltip("Khoá lưu tiến độ trong save.")]
        public string id;
        public MissionPeriod period;
        public MissionStat stat;
        public string title;
        public Sprite icon;
        [Tooltip("Mức cần đạt.")]
        public int goal = 1;
        [Tooltip("Xu thưởng khi nhận.")]
        public int reward;
        [Tooltip("Điểm nhiệm vụ cộng vào thanh rương (thành tựu = 0).")]
        public int points;
        [Tooltip("Chưa làm được (vd. cần quảng cáo — bước 6): hiện nhãn \"Sắp có\" thay cho nút Chơi.")]
        public bool comingSoon;
    }
}
