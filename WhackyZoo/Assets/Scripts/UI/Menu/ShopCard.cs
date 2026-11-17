using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Một thẻ ở lưới Cửa hàng (Shop25D .card): icon 62, tên 14, nhãn trạng thái/giá.
    /// Khi chọn: nhấc lên 6 px, bóng cứng 4 → 9 px, viền cam #FF6B4A cách 2 px, icon lắc lư (wobble 1,2 s).
    /// Món Huyền thoại có vệt sáng quét qua (sweep 2,6 s). Chuyển động theo thời gian thật.
    /// </summary>
    public class ShopCard : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [SerializeField] Button button;
        [Tooltip("Phần nhấc lên khi chọn (thân, viền, icon, chữ).")]
        [SerializeField] RectTransform visual;
        [SerializeField] RectTransform hardShadow;
        [SerializeField] RectTransform softShadow;
        [SerializeField] GameObject outline;
        [SerializeField] RectTransform icon;
        [SerializeField] Image iconImage;
        [Tooltip("Bóng icon (drop-shadow 0 4px 3px rgba(0,0,0,.25)): cùng hình, tô đen 25%.")]
        [SerializeField] Image iconShadow;
        [SerializeField] Text nameText;
        [SerializeField] Image tagImage;
        [SerializeField] Text tagText;
        [Tooltip("Vệt sáng quét qua (chỉ món Huyền thoại).")]
        [SerializeField] RectTransform shine;
        [SerializeField] float shineTravel = 170f;

        [Header("Chọn: transition .15s")]
        [SerializeField] float liftPx = 6f;
        [SerializeField] float shadowRestPx = 4f;
        [SerializeField] float shadowSelectedPx = 9f;
        [SerializeField] float softRestPx = 8f;
        [SerializeField] float softSelectedPx = 18f;
        [SerializeField] float transition = 0.15f;

        public ScriptableObject Data { get; private set; }

        bool selected;
        float amount, time;
        Vector2 iconBase;
        float shineStartX;
        bool inited;

        static readonly Key[] Wobble =
        {
            new Key(0f), new Key(0.25f, offsetY: -3f / 62f, rotation: -8f), new Key(0.75f, offsetY: -3f / 62f, rotation: 8f), new Key(1f),
        };

        void Awake() => Init();

        // Thẻ có thể được cập nhật khi còn ẩn (lọc thú theo map) nên lưu vị trí gốc ở lần dùng đầu tiên
        void Init()
        {
            if (inited) return;
            inited = true;
            iconBase = icon.anchoredPosition;
            if (shine != null) shineStartX = shine.anchoredPosition.x;
        }

        public void Bind(ScriptableObject data, Sprite sprite, string label, Action<ShopCard> onPick)
        {
            Data = data;
            iconImage.sprite = sprite;
            if (iconShadow != null) iconShadow.sprite = sprite;
            nameText.text = label;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onPick(this));
        }

        public void SetLabel(string label) => nameText.text = label;

        public void SetTag(string text, Sprite background)
        {
            tagText.text = text;
            tagImage.sprite = background;
        }

        public void SetSelected(bool on, bool instant = false)
        {
            Init();
            selected = on;
            if (outline != null) outline.SetActive(on);
            if (instant) { amount = on ? 1f : 0f; Apply(); }
            if (!on)
            {
                icon.anchoredPosition = iconBase;
                icon.localRotation = Quaternion.identity;
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            amount = Mathf.MoveTowards(amount, selected ? 1f : 0f, dt / transition);
            Apply();

            if (selected)
            {
                var k = Keyframes.Sample(Wobble, time % 1.2f / 1.2f, Bezier.EaseInOut);
                Keyframes.Apply(icon, k, iconBase, Vector2.one * 62f * PxToUnits);
            }
            if (shine != null && shine.gameObject.activeInHierarchy)
            {
                // sweep: 0–50% đứng ngoài mép trái, 85–100% đã qua mép phải; ease-in-out
                float p = time % 2.6f / 2.6f;
                float u = p <= 0.5f ? 0f : p >= 0.85f ? 1f : Bezier.EaseInOut.Evaluate((p - 0.5f) / 0.35f);
                var pos = shine.anchoredPosition;
                pos.x = shineStartX + u * shineTravel * PxToUnits;
                shine.anchoredPosition = pos;
            }
        }

        void Apply()
        {
            float e = Bezier.Ease.Evaluate(amount);
            visual.anchoredPosition = new Vector2(0f, liftPx * PxToUnits * e);
            // bóng nằm ngoài visual: theo mép dưới thẻ đã nhấc + độ dày bóng
            if (hardShadow != null) hardShadow.anchoredPosition = new Vector2(0f, (liftPx * e - Mathf.Lerp(shadowRestPx, shadowSelectedPx, e)) * PxToUnits);
            if (softShadow != null) softShadow.anchoredPosition = new Vector2(0f, (liftPx * e - Mathf.Lerp(softRestPx, softSelectedPx, e)) * PxToUnits);
        }
    }
}
