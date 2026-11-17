using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Rung khung chơi 0,28 giây mỗi cú đập, luân phiên hai hướng (shakeA/shakeB của bản thiết kế).</summary>
    public class ScreenShake : MonoBehaviour
    {
        const float Duration = 0.28f;
        const float PxToUnits = 3f;

        // (thời điểm, dịch x, dịch y) theo px CSS; y dương = xuống
        static readonly Vector3[] PatternA = { new Vector3(0f, 0, 0), new Vector3(0.2f, -4, 2), new Vector3(0.4f, 4, -2), new Vector3(0.6f, -3, 1), new Vector3(0.8f, 2, -1), new Vector3(1f, 0, 0) };

        RectTransform rt;
        Vector2 basePos;
        float t = -1f;
        float flip = 1f;

        void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        public void Play()
        {
            flip = -flip;
            t = 0f;
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.unscaledDeltaTime;
            float p = t / Duration;
            if (p >= 1f)
            {
                t = -1f;
                rt.anchoredPosition = basePos;
                return;
            }
            Vector2 d = Vector2.zero;
            for (int i = 1; i < PatternA.Length; i++)
            {
                if (p > PatternA[i].x) continue;
                var a = PatternA[i - 1];
                var b = PatternA[i];
                float u = (p - a.x) / (b.x - a.x);
                d = Vector2.Lerp(new Vector2(a.y, a.z), new Vector2(b.y, b.z), u);
                break;
            }
            rt.anchoredPosition = basePos + new Vector2(d.x * flip, -d.y) * PxToUnits;
        }
    }
}
