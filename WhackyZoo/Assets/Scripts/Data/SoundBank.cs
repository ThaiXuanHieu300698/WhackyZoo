using System;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Các tiếng hiệu ứng trong game. Thêm tiếng mới: thêm vào cuối enum rồi gán clip trong SoundBank.</summary>
    public enum Sfx
    {
        Hit,        // búa chạm thú thường
        Bonus,      // đập trúng con thưởng vàng
        Obstacle,   // đập nhầm vật cản (bom…)
        Shielded,   // Khiên Bom đỡ cú đập nhầm
        Pop,        // thú ló lên
        Swing,      // vung dụng cụ
        Combo,      // combo đạt mốc (x5, x10…)
        Tick,       // đếm ngược những giây cuối
        TimeUp,     // hết giờ
        Coin,       // cộng xu cuối ván
        Click,      // bấm nút
        Reward,     // nhận nhiệm vụ / rương / quà hằng ngày
        Purchase,   // mua ở Cửa hàng
        Booster,    // dùng vật phẩm hỗ trợ
        Freeze,     // Đóng Băng Giờ
        Lightning,  // sấm sét Búa Thor
        LevelUp,    // lên cấp
    }

    [Serializable]
    public class SoundEntry
    {
        public Sfx id;
        [Tooltip("Một hoặc nhiều biến thể; mỗi lần phát chọn ngẫu nhiên một clip.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Cao độ ngẫu nhiên trong khoảng này để tiếng lặp lại không nhàm.")]
        public float pitchMin = 1f, pitchMax = 1f;
        [Tooltip("Khoảng cách tối thiểu giữa hai lần phát (giây), tránh chồng tiếng khi nhiều sự kiện cùng lúc.")]
        public float minInterval = 0.03f;
    }

    /// <summary>
    /// Kho âm thanh (Assets/Resources/SoundBank.asset, AudioManager tự nạp). Nhạc nền từng map ở MapData.music,
    /// nhạc Trang chủ ở đây. Thay tiếng: kéo clip mới vào ô tương ứng, không cần sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundBank", menuName = "WhackyZoo/Sound Bank")]
    public class SoundBank : ScriptableObject
    {
        public AudioClip homeMusic;
        public SoundEntry[] sounds;
        [Tooltip("Thời gian chuyển nhạc (mờ dần bản cũ, rõ dần bản mới).")]
        public float musicFade = 0.8f;
        [Tooltip("Âm lượng tối đa của nhạc so với thanh Nhạc nền trong Cài đặt.")]
        [Range(0f, 1f)] public float musicGain = 0.6f;
        [Tooltip("Combo phát tiếng ở mỗi bội số này (5 → x5, x10, x15…).")]
        public int comboStep = 5;
        [HideInInspector, Tooltip("Phiên bản bộ tổng hợp đã sinh tiếng tạm (để tự sinh lại khi bộ tổng hợp đổi).")]
        public int generatorVersion;

        public SoundEntry Find(Sfx id)
        {
            if (sounds == null) return null;
            foreach (var s in sounds) if (s != null && s.id == id) return s;
            return null;
        }
    }
}
