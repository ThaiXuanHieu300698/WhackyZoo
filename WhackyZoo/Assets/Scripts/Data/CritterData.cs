using UnityEngine;

namespace WhackyZoo
{
    public enum CritterKind
    {
        Normal,
        Bonus,
        Obstacle
    }

    /// <summary>Một loại thú / thưởng / vật cản có thể ló lên khỏi hố.</summary>
    [CreateAssetMenu(fileName = "Critter", menuName = "WhackyZoo/Critter")]
    public class CritterData : ScriptableObject
    {
        public string id;
        [Tooltip("Tên đầy đủ, vd. \"Cá hồi vàng\".")]
        public string displayName;
        [Tooltip("Tên ngắn cho chỗ hẹp (chip chú thích, lưới cách chơi), vd. \"Cá hồi\". Để trống thì dùng tên đầy đủ.")]
        public string shortName;
        [Tooltip("Dòng gợi ý ở màn cách chơi, vd. \"Hiếm, ló rất nhanh\".")]
        public string hint;
        public Sprite sprite;
        public CritterKind kind = CritterKind.Normal;
        [Tooltip("Điểm khi đập trúng; vật cản dùng số âm.")]
        public int points = 10;

        [Header("Hoạt ảnh trong hố (script dựng scene tự sinh từ Docs/DesignSource)")]
        [Tooltip("Ảnh dùng khi đang ló; trống thì dùng sprite. Với con có lấp lánh: sprite đã gỡ tia lấp lánh ra lớp riêng.")]
        public Sprite animSprite;
        [Tooltip("Khung chớp mắt: mắt khép một nửa (scaleY 0,55).")]
        public Sprite blinkHalf;
        [Tooltip("Khung chớp mắt: mắt nhắm (scaleY 0,1).")]
        public Sprite blinkClosed;
        [Tooltip("Lớp tia lấp lánh nhấp nháy riêng (thưởng +50, ngòi bom).")]
        public Sprite spark;
        [Tooltip("Tâm tia lấp lánh trong sprite (0–1, gốc dưới-trái).")]
        public Vector2 sparkCenter;
        [Tooltip("Pivot của lớp lấp lánh (0–1 trong ảnh cắt) để phóng to quanh đúng tâm.")]
        public Vector2 sparkPivot;
        [Tooltip("Kích thước ảnh cắt lấp lánh so với sprite (0–1).")]
        public Vector2 sparkSize;

        public string ShortName => string.IsNullOrEmpty(shortName) ? displayName : shortName;
        public Sprite AnimSprite => animSprite != null ? animSprite : sprite;
    }
}
