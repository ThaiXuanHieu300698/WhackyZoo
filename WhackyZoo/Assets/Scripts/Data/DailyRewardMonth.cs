using System;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Loại một phần quà điểm danh.</summary>
    public enum DailyGiftKind
    {
        Coins,
        [Tooltip("Vật phẩm hỗ trợ (booster) × amount.")]
        Booster,
        [Tooltip("Thú mua thêm: thêm vào bộ sưu tập và bật ở map của nó. Đã có thì không thêm gì.")]
        Pet,
        [Tooltip("Dùng thử dụng cụ amount ngày; hết hạn phải mua mới giữ được. Đã sở hữu thì không thêm gì.")]
        ToolTrial,
    }

    /// <summary>Một phần quà: loại, món (id + tham chiếu dữ liệu), số lượng (xu / vật phẩm / số ngày dùng thử).</summary>
    [Serializable]
    public class DailyGift
    {
        public DailyGiftKind kind;
        [Tooltip("Id của món (BoosterData / PetData / ToolData.id); để trống với xu.")]
        public string id;
        [Min(0)] public int amount;
        public BoosterData booster;
        public PetData pet;
        public ToolData tool;
    }

    /// <summary>Một ô trong lịch điểm danh tháng.</summary>
    [Serializable]
    public class DailyDay
    {
        [Tooltip("Quà của ngày (bỏ qua khi là rương bí ẩn — xem chest).")]
        public DailyGift gift;
        [Tooltip("Nhãn ngắn dưới ô, vd. \"100 xu\", \"×2\", \"Bí ẩn\", \"7 ngày\".")]
        public string label;
        [Tooltip("Tên đầy đủ trong \"Đã nhận …!\", vd. \"+5 Giây x2\", \"Rương bí ẩn\".")]
        public string fullName;
        public Sprite icon;
        [Tooltip("Ô mốc 7 · 14 · 21 · 30: to hơn, có mốc tròn trên thanh tiến độ.")]
        public bool milestone;
        [Tooltip("Rương bí ẩn: ô chỉ ghi \"Bí ẩn\", nội dung (chest) chỉ hiện khi mở.")]
        public bool mystery;
        public DailyGift[] chest;
        [Tooltip("Cho phép xem quảng cáo nhận x2 (ngày tặng thú / dùng thử dụng cụ: không).")]
        public bool allowDouble = true;
    }

    /// <summary>
    /// Lịch điểm danh tháng (Docs/GameSpec.md › Điểm danh tháng): 30 ô, mỗi ngày nhận một quà; bỏ lỡ thì điểm danh bù bằng
    /// quảng cáo, tối đa <see cref="makeUpsPerMonth"/> lần/tháng; sang tháng mới quay về Ngày 1.
    /// </summary>
    [CreateAssetMenu(fileName = "DailyRewardMonth", menuName = "WhackyZoo/Daily Reward Month")]
    public class DailyRewardMonth : ScriptableObject
    {
        public DailyDay[] days;
        [Min(0)] public int makeUpsPerMonth = 3;

        public int Count => days != null ? days.Length : 0;

        /// <summary>Ô ngày <paramref name="day"/> (1…Count) hoặc null.</summary>
        public DailyDay Day(int day) => days != null && day >= 1 && day <= days.Length ? days[day - 1] : null;
    }
}
