using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Phần chung của món bán ở Cửa hàng (dụng cụ, thú): tên, mô tả, giá.</summary>
    public abstract class ShopItemData : ScriptableObject
    {
        [Tooltip("Khoá lưu trong save (đã mua / đang dùng).")]
        public string id;
        public string displayName;
        [Tooltip("Tên ngắn trên thẻ ở lưới (để trống = displayName), vd. \"Cá Voi\".")]
        public string cardName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Giá xu; 0 = miễn phí / có sẵn.")]
        public int price;
        [Tooltip("Giá tiền thật hiển thị trên nút đỏ (vd. \"49.000đ\"); để trống nếu không bán bằng tiền thật.")]
        public string cashPrice;
        [Tooltip("Món Huyền thoại: nền tím, nhãn HUYỀN THOẠI, vệt sáng quét qua thẻ.")]
        public bool premium;
        [Tooltip("Nhãn vàng nhận miễn phí qua Điểm danh, vd. \"Miễn phí khi điểm danh ngày 14\" (bấm mở màn Điểm danh); ẩn khi đã sở hữu.")]
        public string freeHint;
    }
}
