using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Chữ cố định trong scene tự đổi theo ngôn ngữ (Loc). Bộ dựng scene tự gắn vào mọi Text có câu trong bảng dịch.
    /// Chỉ thay chữ khi trên màn vẫn đang là câu gốc (bản Việt hoặc bản Anh) — chữ do code đặt lúc chạy thì để code lo.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Câu tiếng Việt gốc (khoá dịch).")]
        [SerializeField] string key;
        [Tooltip("Phần đuôi giữ nguyên, không dịch (vd. \" (8)\" sau tên map).")]
        [SerializeField] string suffix;

        Text text;

        public void SetKey(string vi, string tail = null) { key = vi; suffix = tail; }

        void Awake()
        {
            text = GetComponent<Text>();
            if (string.IsNullOrEmpty(key)) key = text.text;
            if (suffix == null) suffix = "";
        }

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        void Apply()
        {
            if (text == null) return;
            string tail = suffix ?? "", en = Loc.EnglishOf(key);
            if (text.text == key + tail || (en != null && text.text == en + tail)) text.text = Loc.T(key) + tail;
        }
    }
}
