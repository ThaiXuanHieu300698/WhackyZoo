using System;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Kiểu một chip bo tròn (nút 9-slice + màu phủ + màu chữ).</summary>
    [Serializable]
    public struct ChipStyle
    {
        public Sprite sprite;
        public Color tint;
        public Color textColor;

        public ChipStyle(Sprite sprite, Color tint, Color textColor)
        {
            this.sprite = sprite;
            this.tint = tint;
            this.textColor = textColor;
        }
    }

    /// <summary>Một map: nền, hố, thú có sẵn, thưởng, vật cản và màu chip giao diện.</summary>
    [CreateAssetMenu(fileName = "Map", menuName = "WhackyZoo/Map")]
    public class MapData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite background;
        [Tooltip("Nhạc nền khi chơi map này (lặp).")]
        public AudioClip music;
        [Tooltip("Lớp sau của hố (vành + lòng hố).")]
        public Sprite holeBack;
        [Tooltip("Lớp trước của hố (mép che thân thú).")]
        public Sprite holeFront;
        [Tooltip("Mặt nạ cắt thú theo đường cong mép trước của hố (script dựng scene tự tạo từ holeFront).")]
        public Sprite holeMask;
        public CritterData[] normalCritters;
        public CritterData bonus;
        public CritterData obstacle;

        [Header("Hiệu ứng nền màn chơi (Animations.md mục 3)")]
        [Tooltip("Rong biển, bong bóng, tia sáng… vẽ trên nền, sau lưới hố. Ảnh nền nên là bản không vẽ sẵn các vật này.")]
        public AmbientElement[] ambient;
        [Tooltip("Hiệu ứng nền theo khung hình (đàn dơi, dải gió…), cùng kiểu dữ liệu với hiệu ứng thẻ; toạ độ px trong khung 390 × 844, vẽ sau các phần tử ambient.")]
        public CardFxElement[] ambientFx;
        [Tooltip("Mặt nạ cho ambientFx có clip = MapMask (vd. vùng trời Sa mạc để dải gió nằm sau mặt trời và đồi cát).")]
        public Sprite ambientFxMask;

        [Header("Thẻ ở trang Chọn map")]
        [Tooltip("Ảnh nền thẻ (images/sprites/map_cards/card_bg_<map>, 344 × 122 pt).")]
        public Sprite banner;
        [Tooltip("Hiệu ứng nền trên thẻ, vẽ giữa ảnh nền và 4 thú (Docs/Animations.md mục 1). Map mới chỉ cần khai báo ở đây.")]
        public CardFxElement[] cardEffects;
        [Tooltip("Mặt nạ cho hiệu ứng có clip = MapMask (vd. vùng nước phía trên đồi cát của thẻ Dưới biển).")]
        public Sprite cardSkyMask;
        [Tooltip("Hiệu ứng có clip = SkyBand chỉ hiện phía trên đường này (px từ mép trên ảnh nền thẻ).")]
        public float cardSkyBottom = 60f;
        [Tooltip("Gắn nhãn \"MỚI\" ở trang Chọn map (map mới nhất).")]
        public bool isNew;

        [Header("Giao diện")]
        [Tooltip("Nhãn nhóm thú thường ở dải chú thích, vd. \"Hải sản\".")]
        public string normalLabel = "Thú thường";
        [Tooltip("Chip tên map cạnh tiêu đề CÁCH CHƠI.")]
        public ChipStyle mapChip;
        public ChipStyle normalChip;
        public ChipStyle bonusChip;
        public ChipStyle obstacleChip;
        [Tooltip("Nền màn Cách chơi: màu ở mép trên (đậm).")]
        public Color howToTop = new Color32(0x2F, 0x8A, 0x3A, 0xFF);
        [Tooltip("Nền màn Cách chơi: màu ở mép dưới (nhạt).")]
        public Color howToBottom = new Color32(0x6C, 0xC2, 0x4A, 0xFF);
    }
}
