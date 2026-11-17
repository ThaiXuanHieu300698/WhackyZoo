using System;
using UnityEngine;

namespace WhackyZoo
{
    public enum AmbientMotion
    {
        None,
        /// <summary>Bay lên (bong bóng): dịch theo midOffset ở 50%, endOffset ở 100%, tuyến tính; hiện dần rồi mờ.</summary>
        Rise,
        /// <summary>Xoay qua lại angleFrom ↔ angleTo (tia sáng), ease-in-out.</summary>
        Rotate,
        /// <summary>Nghiêng skewX qua lại angleFrom ↔ angleTo quanh đáy (rong biển), ease-in-out.</summary>
        Skew,
        /// <summary>Trôi ngang qua lại angleFrom ↔ angleTo (px thiết kế), ease-in-out hoặc tuyến tính (mây).</summary>
        Drift,
        /// <summary>Bay qua lại từ gốc tới endOffset, ease-in-out mỗi chiều; alpha theo alphaKeys (đom đóm).</summary>
        Wander,
    }

    /// <summary>
    /// Một vật trang trí chuyển động trên nền màn chơi (Docs/Animations.md mục 3).
    /// Toạ độ theo px thiết kế của khung 390 × 844 (gốc trên-trái, y hướng xuống như CSS).
    /// </summary>
    [Serializable]
    public class AmbientElement
    {
        public string name;
        public Sprite sprite;
        public Color color = Color.white;
        [Tooltip("Vị trí điểm pivot (px thiết kế, gốc trên-trái).")]
        public Vector2 position;
        public Vector2 size;
        [Tooltip("Pivot theo Unity (0–1, gốc dưới-trái).")]
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        [Tooltip("Chỉ hiện phía trên đường y này (px thiết kế), vd. tia sáng nằm sau sàn cát. 0 = không cắt.")]
        public float clipBelowY;

        public AmbientMotion motion;
        [Tooltip("Rise: thời gian một lượt. Rotate/Skew: thời gian đi một chiều (CSS alternate).")]
        public float period = 1f;
        public float delay;
        [Tooltip("Rotate/Skew: góc theo CSS (độ). Drift: độ dịch ngang (px thiết kế).")]
        public float angleFrom, angleTo;
        [Tooltip("Rise: độ dịch ở 50% và 100% (px thiết kế, y dương = xuống).")]
        public Vector2 midOffset, endOffset;
        [Tooltip("Rise: alpha 0 → 1 tới mốc này rồi mờ dần về 0 ở cuối.")]
        [Range(0f, 1f)] public float fadeInAt = 0.1f;
        [Tooltip("Drift: chạy tuyến tính thay vì ease-in-out (CSS linear).")]
        public bool linear;
        [Tooltip("Wander: alpha ở đầu / giữa / cuối quãng bay.")]
        public Vector3 alphaKeys = new Vector3(1f, 1f, 1f);
    }
}
