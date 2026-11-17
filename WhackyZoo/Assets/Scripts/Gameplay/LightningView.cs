using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Sấm sét của Búa Thor: một tia từ trời giáng xuống ô được chạm, rồi lan từng bước sang các ô trong tầm đập
    /// (mỗi ô nối từ ô gần nhất đã bị sét), kèm loé sáng cả màn và đốm sáng ở đầu tia. Tia gồm nhiều đoạn gấp khúc,
    /// đổi hình mỗi JitterInterval giây cho cảm giác chớp giật. Phủ kín khung chơi, nằm trên lưới hố.
    /// Chạy theo Time.deltaTime nên dừng khi tạm dừng ván. Đơn vị px thiết kế (1 px = 3 đơn vị canvas).
    /// </summary>
    public class LightningView : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [Tooltip("Lớp loé sáng phủ kín màn (Image trắng xanh, alpha 0 khi nghỉ).")]
        [SerializeField] Image flash;

        // Thông số hình (không phải số cân bằng) để hằng số: đổi ở đây không phải dựng lại scene.
        // Tia 3 lớp cho nổi trên nền sáng: lõi trắng ngà, quầng xanh điện, quầng ngoài xanh tím mờ.
        static readonly Color CoreColor = new Color(1f, 1f, 0.86f, 1f);
        static readonly Color GlowColor = new Color(0.25f, 0.72f, 1f, 1f);
        static readonly Color OuterColor = new Color(0.36f, 0.3f, 1f, 0.45f);
        static readonly Color FlareColor = new Color(1f, 0.95f, 0.6f, 1f);
        const float CoreWidth = 6f, GlowWidth = 16f, OuterWidth = 44f; // px
        const float FlareSize = 90f;
        const int Segments = 8;          // số đoạn gấp khúc của một tia
        const float Jaggedness = 0.18f;  // độ lệch ngang tối đa của khúc gấp, theo tỉ lệ chiều dài tia
        // Điểm giáng sét cố định cho mọi cú (px thiết kế, gốc trên-trái khung 390 × 844): giữa màn theo chiều ngang,
        // ngay trên hàng hố đầu tiên (hàng trên cùng ló tới khoảng y 280) — tia không còn lúc từ mép trên, lúc từ giữa màn.
        const float SkyX = 195f, SkyY = 230f;
        const float DesignW = 390f, DesignH = 844f;
        const float BoltSeconds = 0.55f;
        const float HopDelay = 0.07f;    // mỗi bước lan sang ô kế tiếp trễ thêm
        const float JitterInterval = 0.06f;
        const float FlashAlpha = 0.18f;
        const float FlashSeconds = 0.22f;

        class Bolt
        {
            public Vector2 a, b;
            public float start;
            public bool flareAtEnd;
            public Image flare;
            public readonly List<Image> core = new List<Image>(), glow = new List<Image>(), outer = new List<Image>();
            public readonly List<Vector2> points = new List<Vector2>();
            public int jitterTick = -1;
        }

        readonly List<Bolt> bolts = new List<Bolt>();
        readonly List<Image> pool = new List<Image>();
        readonly List<Vector2> reached = new List<Vector2>();
        readonly List<int> reachedDepth = new List<int>();
        readonly List<Vector2> pending = new List<Vector2>();
        float time = -1f, flashStart;
        static Sprite lineSprite, dotSprite;

        RectTransform Rt => (RectTransform)transform;

        /// <summary>Phát sét: từ trời xuống <paramref name="origin"/>, lan tới các <paramref name="targets"/> (toạ độ thế giới), sau <paramref name="delay"/> giây.</summary>
        public void Play(Vector3 origin, IList<Vector3> targets, float delay)
        {
            EnsureSprites();
            Clear();
            AudioManager.Play(Sfx.Lightning);
            time = -delay;
            flashStart = 0f;

            Vector2 o = Rt.InverseTransformPoint(origin);
            var sky = new Vector2(SkyX - DesignW / 2f, DesignH / 2f - SkyY) * PxToUnits; // toạ độ khung chơi (tâm ở giữa màn)
            AddBolt(sky, o, 0f, true);

            // Lan: ô gần nhất trước, mỗi ô nối từ điểm đã bị sét gần nó nhất
            reached.Clear(); reachedDepth.Clear(); pending.Clear();
            reached.Add(o); reachedDepth.Add(0);
            foreach (var t in targets) pending.Add(Rt.InverseTransformPoint(t));
            pending.Sort((x, y) => (x - o).sqrMagnitude.CompareTo((y - o).sqrMagnitude));
            foreach (var p in pending)
            {
                int best = 0;
                for (int i = 1; i < reached.Count; i++)
                    if ((reached[i] - p).sqrMagnitude < (reached[best] - p).sqrMagnitude) best = i;
                int depth = reachedDepth[best] + 1;
                AddBolt(reached[best], p, depth * HopDelay, true);
                reached.Add(p);
                reachedDepth.Add(depth);
            }
            Apply();
        }

        void AddBolt(Vector2 a, Vector2 b, float start, bool flare)
        {
            var bolt = new Bolt { a = a, b = b, start = start, flareAtEnd = flare };
            int n = Mathf.Max(1, Segments);
            // Lấy theo lớp (ngoài → quầng → lõi) để lõi luôn nằm trên mọi quầng của cùng tia
            for (int i = 0; i < n; i++) bolt.outer.Add(Take(lineSprite, OuterColor));
            for (int i = 0; i < n; i++) bolt.glow.Add(Take(lineSprite, GlowColor));
            for (int i = 0; i < n; i++) bolt.core.Add(Take(lineSprite, CoreColor));
            if (flare)
            {
                bolt.flare = Take(dotSprite, FlareColor);
                var rt = bolt.flare.rectTransform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = b;
                rt.sizeDelta = Vector2.one * FlareSize * PxToUnits;
                rt.localRotation = Quaternion.identity;
            }
            bolts.Add(bolt);
        }

        Image Take(Sprite sprite, Color color)
        {
            Image img = null;
            foreach (var p in pool)
                if (!p.gameObject.activeSelf) { img = p; break; }
            if (img == null)
            {
                var go = new GameObject("Bolt", typeof(RectTransform));
                go.layer = gameObject.layer;
                img = go.AddComponent<Image>();
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.SetParent(transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                pool.Add(img);
            }
            img.sprite = sprite;
            img.color = color;
            img.enabled = false; // đang thuộc một tia (activeSelf = true), hiện/ẩn bằng enabled
            img.gameObject.SetActive(true);
            img.transform.SetAsLastSibling(); // vẽ theo đúng thứ tự lấy, kể cả hình dùng lại
            return img;
        }

        void Clear()
        {
            foreach (var p in pool) p.gameObject.SetActive(false);
            bolts.Clear();
            if (flash != null) SetAlpha(flash, 0f);
        }

        void Update()
        {
            if (time < -1e5f || bolts.Count == 0) return;
            time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            bool alive = false;
            foreach (var bolt in bolts)
            {
                float local = time - bolt.start;
                bool on = local >= 0f && local < BoltSeconds;
                foreach (var img in bolt.core) img.enabled = on;
                foreach (var img in bolt.outer) img.enabled = on;
                foreach (var img in bolt.glow) img.enabled = on;
                if (bolt.flare != null) bolt.flare.enabled = on;
                if (local < BoltSeconds) alive = true;
                if (!on) continue;

                int tick = Mathf.FloorToInt(local / JitterInterval);
                if (tick != bolt.jitterTick)
                {
                    bolt.jitterTick = tick;
                    Reshape(bolt);
                }
                // Chớp: sáng ngẫu nhiên theo nhịp đổi hình, mờ dần ở 40% cuối
                float p = local / BoltSeconds;
                float fade = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
                float flicker = 0.65f + 0.35f * Mathf.PerlinNoise(tick * 0.9f, bolt.start * 7f);
                float a = fade * flicker;
                foreach (var img in bolt.core) SetAlpha(img, CoreColor.a * a);
                foreach (var img in bolt.outer) SetAlpha(img, OuterColor.a * a);
                foreach (var img in bolt.glow) SetAlpha(img, GlowColor.a * a);
                if (bolt.flare != null)
                {
                    SetAlpha(bolt.flare, FlareColor.a * fade);
                    bolt.flare.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, Mathf.Min(1f, p * 3f));
                }
            }

            if (flash != null)
            {
                float f = (time - flashStart) / FlashSeconds;
                SetAlpha(flash, f >= 0f && f < 1f ? FlashAlpha * (1f - f) : 0f);
            }
            if (!alive && (flash == null || time - flashStart >= FlashSeconds))
            {
                Clear();
                time = -1e6f;
            }
        }

        void Reshape(Bolt bolt)
        {
            int n = bolt.core.Count;
            var dir = bolt.b - bolt.a;
            float len = dir.magnitude;
            var normal = len > 0f ? new Vector2(-dir.y, dir.x) / len : Vector2.up;
            bolt.points.Clear();
            for (int i = 0; i <= n; i++)
            {
                float u = (float)i / n;
                float off = i == 0 || i == n ? 0f : Random.Range(-1f, 1f) * Jaggedness * len * Mathf.Sin(u * Mathf.PI);
                bolt.points.Add(bolt.a + dir * u + normal * off);
            }
            for (int i = 0; i < n; i++)
            {
                Place(bolt.outer[i], bolt.points[i], bolt.points[i + 1], OuterWidth);
                Place(bolt.glow[i], bolt.points[i], bolt.points[i + 1], GlowWidth);
                Place(bolt.core[i], bolt.points[i], bolt.points[i + 1], CoreWidth);
            }
        }

        static void Place(Image img, Vector2 a, Vector2 b, float widthPx)
        {
            var rt = img.rectTransform;
            var d = b - a;
            float w = widthPx * PxToUnits;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = a;
            rt.sizeDelta = new Vector2(d.magnitude + w * 0.5f, w); // nối liền khớp gấp
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            rt.localScale = Vector3.one;
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = Mathf.Clamp01(a);
            g.color = c;
        }

        // Ảnh sinh lúc chạy: dải mềm theo chiều ngang (cạnh mờ trên/dưới) và đốm tròn mờ dần
        static void EnsureSprites()
        {
            if (lineSprite == null)
            {
                const int h = 32;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LightningLine" };
                for (int y = 0; y < h; y++)
                {
                    float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - v), 1.6f);
                    for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                lineSprite = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            }
            if (dotSprite == null)
            {
                const int s = 64;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LightningFlare" };
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = new Vector2((x + 0.5f) / s * 2f - 1f, (y + 0.5f) / s * 2f - 1f).magnitude;
                        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                tex.Apply();
                dotSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
