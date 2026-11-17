using UnityEngine;
using UnityEngine.EventSystems;

namespace WhackyZoo
{
    /// <summary>
    /// Chuyển động của một thẻ ở trang Chọn map (Docs/Animations.md mục 1, Docs/DesignSource/MapSelect25D.dc.html):
    /// thú lơ lửng, thẻ trượt vào khi mở trang, thẻ lún xuống khi nhấn. Hiệu ứng nền riêng từng map do CardFxView lo.
    /// Mọi chuyển động chạy trên "visual" (con của gốc thẻ) vì gốc thẻ do VerticalLayoutGroup đặt vị trí.
    /// Đơn vị px thiết kế; 1 px = 3 đơn vị canvas.
    /// </summary>
    public class MapCardAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PxToUnits = 3f;

        [SerializeField] RectTransform visual;
        [SerializeField] CanvasGroup visualGroup;

        [Header("Thú lơ lửng: 0 → −5 px → 0, 1,5 s, ease-in-out, con thứ n trễ n × 0,25 s")]
        [SerializeField] RectTransform[] animals;
        [SerializeField] float bobHeightPx = 5f;
        [SerializeField] float bobPeriod = 1.5f;
        [SerializeField] float bobStagger = 0.25f;

        [Header("Thẻ hiện ra: từ dưới 30 px, scale 0,96, alpha 0 → gốc trong 0,5 s")]
        [SerializeField] float introOffsetPx = 30f;
        [SerializeField] float introScale = 0.96f;
        [SerializeField] float introDuration = 0.5f;

        [Header("Nhấn: lún 3 px, bóng cứng 6 → 2 px, bóng mờ 16 → 8 px, 0,15 s")]
        [SerializeField] float pressSinkPx = 3f;
        [SerializeField] float pressDuration = 0.15f;
        [Tooltip("Bóng cứng #B9A98C dưới thẻ (con của visual).")]
        [SerializeField] RectTransform shadow;
        [SerializeField] float shadowRestPx = 6f;
        [SerializeField] float shadowPressedPx = 2f;
        [Tooltip("Bóng mờ dưới thẻ (con của visual).")]
        [SerializeField] RectTransform softShadow;
        [SerializeField] float softRestPx = 16f;
        [SerializeField] float softPressedPx = 8f;

        static readonly Bezier IntroEase = new Bezier(0.2f, 0.9f, 0.3f, 1.2f); // cubic-bezier của .card
        Vector2[] animalBase;
        float time;
        float introT = -1f, introDelay;
        bool pressed, pressLocked;
        float pressAmount;

        public float PressDuration => pressDuration;

        void Awake()
        {
            animalBase = new Vector2[animals.Length];
            for (int i = 0; i < animals.Length; i++) animalBase[i] = animals[i].anchoredPosition;
        }

        void OnDisable()
        {
            pressed = pressLocked = false;
            pressAmount = 0f;
            ApplyVisual(1f, 0f, 1f, 0f);
        }

        /// <summary>Phát hiệu ứng trượt vào sau <paramref name="delay"/> giây (thẻ sau trễ hơn thẻ trước).</summary>
        public void PlayIntro(float delay)
        {
            introDelay = delay;
            introT = 0f;
            ApplyVisual(introScale, -introOffsetPx * PxToUnits, 0f, 0f);
        }

        /// <summary>Giữ thẻ ở trạng thái lún (đang chuyển màn).</summary>
        public void HoldPressed()
        {
            pressed = pressLocked = true;
        }

        public void OnPointerDown(PointerEventData e) => pressed = true;
        public void OnPointerUp(PointerEventData e) { if (!pressLocked) pressed = false; }
        public void OnPointerExit(PointerEventData e) { if (!pressLocked) pressed = false; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            UpdateBob();

            // Thẻ trượt vào
            float scale = 1f, offsetY = 0f, alpha = 1f;
            if (introT >= 0f)
            {
                introT += dt;
                float local = introT - introDelay;
                float p = Mathf.Clamp01(local / introDuration);
                float e = IntroEase.Evaluate(p);
                scale = Mathf.LerpUnclamped(introScale, 1f, e);
                offsetY = -introOffsetPx * PxToUnits * (1f - e);
                alpha = Mathf.Clamp01(e);
                if (p >= 1f) introT = -1f;
            }

            // Nhấn (transition .15s, hàm "ease" mặc định của CSS)
            pressAmount = Mathf.MoveTowards(pressAmount, pressed ? 1f : 0f, dt / pressDuration);
            ApplyVisual(scale, offsetY, alpha, Bezier.Ease.Evaluate(pressAmount));
        }

        void ApplyVisual(float scale, float offsetY, float alpha, float press)
        {
            if (visual != null)
            {
                visual.localScale = new Vector3(scale, scale, 1f);
                visual.anchoredPosition = new Vector2(0f, offsetY - pressSinkPx * PxToUnits * press);
            }
            if (visualGroup != null) visualGroup.alpha = alpha;
            if (shadow != null) shadow.anchoredPosition = new Vector2(0f, -Mathf.Lerp(shadowRestPx, shadowPressedPx, press) * PxToUnits);
            if (softShadow != null) softShadow.anchoredPosition = new Vector2(0f, -Mathf.Lerp(softRestPx, softPressedPx, press) * PxToUnits);
        }

        void UpdateBob()
        {
            for (int i = 0; i < animals.Length; i++)
            {
                float local = time - i * bobStagger;
                float y = 0f;
                if (local > 0f)
                {
                    float phase = local % bobPeriod / bobPeriod;
                    float half = phase < 0.5f ? phase * 2f : (1f - phase) * 2f; // keyframe 0% / 50% / 100%
                    y = bobHeightPx * PxToUnits * Bezier.EaseInOut.Evaluate(half);
                }
                animals[i].anchoredPosition = animalBase[i] + new Vector2(0f, y);
            }
        }
    }
}
