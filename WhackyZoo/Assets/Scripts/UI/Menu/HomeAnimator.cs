using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Chuyển động của Trang chủ theo Docs/DesignSource/Main25D.dc.html và Docs/Animations.md mục 4:
    /// logo bật ra, 3 thú trên logo lơ lửng + chớp mắt, nút CHƠI NGAY thở + vệt sáng quét, icon búa / quà lắc.
    /// (Mây trôi do AmbientFxView trên ảnh nền lo.) Chạy theo thời gian thật; logo bật lại mỗi lần quay về Trang chủ.
    /// </summary>
    public class HomeAnimator : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [Header("Logo (.logo): 0,7 s cubic-bezier(.2,.9,.3,1.3), scale 0,3 → 1, alpha 0 → 1, giữ xoay −3°")]
        [SerializeField] RectTransform logoText;
        [SerializeField] CanvasGroup logoGroup;
        [SerializeField] float logoDuration = 0.7f;
        [SerializeField] float logoFromScale = 0.3f;

        [Header("Thú trên logo (.bob 1,6 s, −6 px, trễ 0 / 0,3 / 0,6 s; chớp mắt 3,4 s)")]
        [SerializeField] RectTransform[] animals;
        [SerializeField] Image[] animalImages;
        [SerializeField] CritterData[] animalData;
        [SerializeField] float bobPeriod = 1.6f;
        [SerializeField] float bobHeightPx = 6f;
        [SerializeField] float[] bobDelays = { 0f, 0.3f, 0.6f };
        [SerializeField] float blinkPeriod = 3.4f;

        [Header("Nút CHƠI NGAY (.breathe 1,4 s scale 1 ↔ 1,035; .shine quét mỗi 2,8 s)")]
        [SerializeField] RectTransform playButton;
        [SerializeField] float breathePeriod = 1.4f;
        [SerializeField] float breatheScale = 1.035f;
        [SerializeField] RectTransform shine;
        [Tooltip("Vị trí x (đơn vị canvas, mép trái dải so với mép trái nút) ở đầu và cuối lượt quét.")]
        [SerializeField] float shineFromX = -60f * PxToUnits;
        [SerializeField] float shineToX = 385f * PxToUnits; // 110% bề rộng nút 350
        [SerializeField] float shinePeriod = 2.8f;

        [Header("Lắc (.wiggle 2,4 s: 84% −12°, 88% +10°, 92% −6°, 96% +4°)")]
        [SerializeField] RectTransform[] wiggles;
        [SerializeField] float wigglePeriod = 2.4f;

        static readonly Bezier LogoEase = new Bezier(0.2f, 0.9f, 0.3f, 1.3f);
        static readonly Key[] Wiggle =
        {
            new Key(0f), new Key(0.8f),
            new Key(0.84f, rotation: -12f), new Key(0.88f, rotation: 10f),
            new Key(0.92f, rotation: -6f), new Key(0.96f, rotation: 4f), new Key(1f),
        };

        Vector2[] animalBase;
        Quaternion logoRotation;
        float time, logoT;
        int[] blinkFrame;

        void Awake()
        {
            animalBase = new Vector2[animals.Length];
            for (int i = 0; i < animals.Length; i++) animalBase[i] = animals[i].anchoredPosition;
            blinkFrame = new int[animals.Length];
            if (logoText != null) logoRotation = logoText.localRotation;
        }

        void OnEnable()
        {
            logoT = 0f;
            ApplyLogo(0f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            if (logoT >= 0f)
            {
                logoT += dt;
                float p = Mathf.Clamp01(logoT / logoDuration);
                ApplyLogo(LogoEase.Evaluate(p));
                if (p >= 1f) logoT = -1f;
            }

            for (int i = 0; i < animals.Length; i++)
            {
                float delay = i < bobDelays.Length ? bobDelays[i] : 0f;
                float local = time - delay;
                float y = 0f;
                if (local > 0f)
                {
                    float phase = local % bobPeriod / bobPeriod;
                    float half = phase < 0.5f ? phase * 2f : (1f - phase) * 2f;
                    y = bobHeightPx * PxToUnits * Bezier.EaseInOut.Evaluate(half);
                }
                animals[i].anchoredPosition = animalBase[i] + new Vector2(0f, y);
                Blink(i);
            }

            if (playButton != null)
            {
                float phase = time % breathePeriod / breathePeriod;
                float half = phase < 0.5f ? phase * 2f : (1f - phase) * 2f;
                float s = Mathf.Lerp(1f, breatheScale, Bezier.EaseInOut.Evaluate(half));
                playButton.localScale = new Vector3(s, s, 1f);
            }

            if (shine != null)
            {
                // @keyframes sweep{0%,55%{left:-60px}85%,100%{left:110%}}, ease-in-out mỗi đoạn
                float p = time % shinePeriod / shinePeriod;
                float u = p <= 0.55f ? 0f : p >= 0.85f ? 1f : Bezier.EaseInOut.Evaluate((p - 0.55f) / 0.3f);
                shine.anchoredPosition = new Vector2(Mathf.Lerp(shineFromX, shineToX, u), shine.anchoredPosition.y);
            }

            float w = time % wigglePeriod / wigglePeriod;
            var k = Keyframes.Sample(Wiggle, w, Bezier.EaseInOut);
            foreach (var r in wiggles) r.localRotation = Quaternion.Euler(0f, 0f, -k.rotation); // CSS: dương = chiều kim đồng hồ
        }

        void ApplyLogo(float e)
        {
            if (logoText != null)
            {
                float s = Mathf.LerpUnclamped(logoFromScale, 1f, e);
                logoText.localScale = new Vector3(s, s, 1f);
                logoText.localRotation = logoRotation;
            }
            if (logoGroup != null) logoGroup.alpha = Mathf.Clamp01(e);
        }

        /// <summary>Chớp mắt như thú trong hố: 92% → 95% mắt khép tới 0,1 → 100% mở; dùng khung sinh sẵn trong CritterData.</summary>
        void Blink(int i)
        {
            if (animalData == null || i >= animalData.Length || animalData[i] == null || i >= animalImages.Length) return;
            var d = animalData[i];
            if (d.blinkHalf == null || d.blinkClosed == null) return;
            float p = time % blinkPeriod / blinkPeriod;
            float s = 1f;
            if (p > 0.92f && p <= 0.95f) s = Mathf.Lerp(1f, 0.1f, (p - 0.92f) / 0.03f);
            else if (p > 0.95f) s = Mathf.Lerp(0.1f, 1f, (p - 0.95f) / 0.05f);
            int frame = s > 0.775f ? 0 : s > 0.325f ? 1 : 2;
            if (frame == blinkFrame[i]) return;
            blinkFrame[i] = frame;
            animalImages[i].sprite = frame == 0 ? d.AnimSprite : frame == 1 ? d.blinkHalf : d.blinkClosed;
        }
    }
}
