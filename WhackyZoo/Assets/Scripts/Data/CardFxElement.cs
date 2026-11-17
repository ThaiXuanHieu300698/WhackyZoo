using System;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Hàm thời gian của hiệu ứng thẻ (như animation-timing-function của CSS, áp cho từng đoạn giữa hai khung).</summary>
    public enum FxEase { Linear, Ease, EaseIn, EaseOut, EaseInOut }

    /// <summary>Vùng cắt của một hiệu ứng thẻ (ngoài việc luôn bị cắt theo khung thẻ bo góc).</summary>
    public enum FxClip
    {
        [Tooltip("Chỉ cắt theo khung thẻ.")] None,
        [Tooltip("Cắt theo mặt nạ MapData.cardSkyMask (vd. vùng nước phía trên đồi cát).")] MapMask,
        [Tooltip("Chỉ hiện ở dải trời phía trên MapData.cardSkyBottom (tính từ mép trên ảnh nền).")] SkyBand,
    }

    /// <summary>
    /// Một hiệu ứng nền trên thẻ ở trang Chọn map (Docs/Animations.md mục 1): mây, gió, hạt cát, tuyết, dơi, bong bóng…
    /// Đơn vị px thiết kế, gốc toạ độ ở góc trên trái ảnh nền thẻ (344 × 122), trục y đi XUỐNG như CSS.
    /// Mỗi thuộc tính có dãy khung riêng (như @keyframes của CSS); dãy rỗng = không chạy thuộc tính đó.
    /// </summary>
    [Serializable]
    public class CardFxElement
    {
        public string name;
        [Tooltip("Để trống = nhóm rỗng (vd. cả đàn dơi), các phần tử con trỏ tới nó qua parent.")]
        public Sprite sprite;
        [Tooltip("Các khung luân phiên với sprite (vd. dơi cánh lên / cánh xuống).")]
        public Sprite[] frames;
        [Tooltip("Giây mỗi khung khi có frames.")]
        public float frameInterval = 0.22f;
        public Color color = Color.white;
        [Tooltip("Chỉ số phần tử cha trong mảng (−1 = gắn thẳng vào thẻ). Vị trí con tính từ góc trên trái của cha.")]
        public int parent = -1;

        [Header("Vị trí (px, từ góc trên trái)")]
        public float left;
        public float top;
        public float width;
        public float height;
        [Tooltip("Tâm xoay (0–1), mặc định giữa như transform-origin của CSS.")]
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public FxClip clip;
        [Tooltip("Vẽ dần theo chiều ngang (Image Filled Horizontal) bằng fillKeys.")]
        public bool fillHorizontal;

        [Header("Thời gian")]
        public float period = 1f;
        [Tooltip("Trễ (giây); số âm = bắt đầu giữa chừng như animation-delay âm của CSS.")]
        public float delay;
        public FxEase ease = FxEase.Linear;
        [Tooltip("Đi rồi về (animation-direction: alternate): một chiều = period.")]
        public bool alternate;

        [Header("Khung hình (t = 0–1 trong chu kỳ)")]
        [Tooltip("(t, x, y): dịch chuyển px, y dương = xuống.")]
        public Vector3[] moveKeys;
        [Tooltip("(t, alpha): nhân với color.a.")]
        public Vector2[] alphaKeys;
        [Tooltip("(t, fillAmount) khi fillHorizontal.")]
        public Vector2[] fillKeys;
        [Tooltip("(t, độ): dương = theo chiều kim đồng hồ như CSS.")]
        public Vector2[] rotateKeys;

        [Header("Trước khi bắt đầu (trong thời gian trễ)")]
        [Tooltip("Alpha khi chưa chạy (CSS: opacity gốc của phần tử).")]
        public float restAlpha = 1f;
        [Tooltip("Góc khi chưa chạy (CSS: transform gốc của phần tử).")]
        public float restRotation;
    }
}
