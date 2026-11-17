using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Hàm thời gian cubic-bezier giống CSS (animation-timing-function).</summary>
    public readonly struct Bezier
    {
        readonly float x1, y1, x2, y2;

        public Bezier(float x1, float y1, float x2, float y2)
        {
            this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2;
        }

        public static readonly Bezier Linear = new Bezier(0f, 0f, 1f, 1f);
        public static readonly Bezier Ease = new Bezier(0.25f, 0.1f, 0.25f, 1f);
        public static readonly Bezier EaseIn = new Bezier(0.42f, 0f, 1f, 1f);
        public static readonly Bezier EaseOut = new Bezier(0f, 0f, 0.58f, 1f);
        public static readonly Bezier EaseInOut = new Bezier(0.42f, 0f, 0.58f, 1f);

        static float Curve(float a, float b, float t)
        {
            float u = 1f - t;
            return 3f * u * u * t * a + 3f * u * t * t * b + t * t * t;
        }

        static float Slope(float a, float b, float t)
        {
            float u = 1f - t;
            return 3f * u * u * a + 6f * u * t * (b - a) + 3f * t * t * (1f - b);
        }

        public float Evaluate(float x)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            // Newton tìm t sao cho X(t) = x, rơi về chia đôi nếu đạo hàm quá nhỏ
            float t = x;
            for (int i = 0; i < 6; i++)
            {
                float dx = Curve(x1, x2, t) - x;
                if (Mathf.Abs(dx) < 1e-4f) return Curve(y1, y2, t);
                float d = Slope(x1, x2, t);
                if (Mathf.Abs(d) < 1e-5f) break;
                t -= dx / d;
            }
            float lo = 0f, hi = 1f;
            t = x;
            for (int i = 0; i < 20; i++)
            {
                float cx = Curve(x1, x2, t);
                if (Mathf.Abs(cx - x) < 1e-4f) break;
                if (cx < x) lo = t; else hi = t;
                t = (lo + hi) * 0.5f;
            }
            return Curve(y1, y2, t);
        }
    }

    /// <summary>
    /// Một khung hoạt ảnh. offset tính theo phần chiều cao/rộng của đối tượng (như translate % trong CSS,
    /// trục y dương = đi XUỐNG như CSS); rotation theo độ, dương = theo chiều kim đồng hồ như CSS.
    /// </summary>
    public struct Key
    {
        public float time;
        public Vector2 offset;
        public Vector2 scale;
        public float rotation;
        public float alpha;

        public Key(float time, float offsetY = 0f, float scaleX = 1f, float scaleY = 1f, float rotation = 0f, float alpha = 1f, float offsetX = 0f)
        {
            this.time = time;
            offset = new Vector2(offsetX, offsetY);
            scale = new Vector2(scaleX, scaleY);
            this.rotation = rotation;
            this.alpha = alpha;
        }

        public static Key Lerp(Key a, Key b, float t) => new Key
        {
            time = Mathf.LerpUnclamped(a.time, b.time, t),
            offset = Vector2.LerpUnclamped(a.offset, b.offset, t),
            scale = Vector2.LerpUnclamped(a.scale, b.scale, t),
            rotation = Mathf.LerpUnclamped(a.rotation, b.rotation, t),
            alpha = Mathf.LerpUnclamped(a.alpha, b.alpha, t),
        };
    }

    public static class Keyframes
    {
        /// <summary>Lấy mẫu tại tiến độ p (0–1). Như CSS, hàm thời gian áp cho từng đoạn giữa hai khung.</summary>
        public static Key Sample(Key[] keys, float p, Bezier ease)
        {
            if (p <= keys[0].time) return keys[0];
            for (int i = 1; i < keys.Length; i++)
            {
                if (p <= keys[i].time)
                {
                    var a = keys[i - 1];
                    var b = keys[i];
                    float span = b.time - a.time;
                    float u = span > 0f ? (p - a.time) / span : 1f;
                    return Key.Lerp(a, b, ease.Evaluate(u));
                }
            }
            return keys[keys.Length - 1];
        }

        /// <summary>Áp khung lên RectTransform: base là vị trí gốc, size dùng để đổi offset % ra đơn vị canvas.</summary>
        public static void Apply(RectTransform rt, Key k, Vector2 basePos, Vector2 size)
        {
            rt.anchoredPosition = basePos + new Vector2(k.offset.x * size.x, -k.offset.y * size.y);
            rt.localScale = new Vector3(k.scale.x, k.scale.y, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, -k.rotation);
        }
    }
}
