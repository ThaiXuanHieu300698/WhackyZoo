using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Tác dụng của vật phẩm hỗ trợ (Docs/GameSpec.md › Vật phẩm hỗ trợ).</summary>
    public enum BoosterEffect
    {
        [Tooltip("Cộng amount giây vào đồng hồ, tức thì.")] AddTime,
        [Tooltip("Mọi điểm dương × amount trong duration giây.")] DoubleScore,
        [Tooltip("Đỡ 1 lần đập nhầm vật cản: không trừ điểm, giữ combo. Bật tới khi dùng (trong ván).")] BombShield,
        [Tooltip("Dừng đồng hồ duration giây, thú vẫn ló lên.")] FreezeTime,
        [Tooltip("Combo bắt đầu từ amount và rơi về amount thay vì mức thường, cả ván.")] ComboCharm,
    }

    /// <summary>
    /// Một vật phẩm hỗ trợ: tác dụng, giá, hình và chữ hiển thị ở thanh vật phẩm, màn Cách chơi và Cửa hàng.
    /// Thêm vật phẩm mới = thêm asset + thêm vào BoosterCatalog (tác dụng mới thì mới cần code).
    /// </summary>
    [CreateAssetMenu(fileName = "Booster", menuName = "WhackyZoo/Booster")]
    public class BoosterData : ScriptableObject
    {
        [Tooltip("Khoá lưu số lượng trong save (time, x2, shield, freeze, fire).")]
        public string id;
        public BoosterEffect effect;
        [Tooltip("Số giây cộng thêm / hệ số nhân điểm / mức combo tối thiểu, tuỳ tác dụng.")]
        public float amount;
        [Tooltip("Thời lượng hiệu lực (giây). 0 = tức thì hoặc tới khi dùng / cả ván.")]
        public float duration;

        [Header("Chữ hiển thị")]
        [Tooltip("Tên đầy đủ (Cửa hàng), vd. \"+5 Giây\".")]
        public string displayName;
        [Tooltip("Nhãn ngắn dưới icon ở thanh vật phẩm, vd. \"+5s\".")]
        public string barLabel;
        [Tooltip("Tên ở khối VẬT PHẨM HỖ TRỢ của màn Cách chơi, vd. \"+5 giây\".")]
        public string guideName;
        [Tooltip("Mô tả ngắn ở màn Cách chơi, vd. \"Cộng 5 giây\".")]
        public string guideHint;
        [Tooltip("Tên trên thẻ ở lưới Cửa hàng, vd. \"Nhân Đôi\".")]
        public string shopCardName;
        [Tooltip("Mô tả ở khung chi tiết Cửa hàng.")]
        [TextArea] public string shopDescription;

        [Header("Hình")]
        public Sprite icon;
        [Tooltip("Nút .gbtn của thanh vật phẩm (màu riêng từng vật phẩm).")]
        public Sprite buttonSprite;

        [Header("Giá (xu)")]
        public int price;
        [Tooltip("Giá gói packSize cái (giảm 20% = giá của 4).")]
        public int packPrice;
        public int packSize = 5;

        [Tooltip("Số lượng tặng sẵn cho người chơi mới để dùng thử.")]
        public int starterCount = 1;
    }
}
