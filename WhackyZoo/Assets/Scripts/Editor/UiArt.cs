using System.IO;
using UnityEditor;
using UnityEngine;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Sinh ảnh giao diện phẳng theo CSS của bản thiết kế: khối bo góc có viền, nền chuyển màu dọc, bóng cứng phía dưới,
    /// và bóng mờ. Ảnh sinh đúng kích thước dùng (@3x) nên không méo dải màu; chip co ngang dùng 9-slice trái/phải.
    /// </summary>
    internal static class UiArt
    {
        const string Dir = GameSceneBuilder.Art + "/Generated/UI";
        const float K = 3f; // px trên 1 pt

        internal struct Stop
        {
            public float at;
            public Color color;
            public Stop(float at, string hex) { this.at = at; color = GameSceneBuilder.Hex(hex); }
        }

        static Color Grad(Stop[] stops, float t)
        {
            if (t <= stops[0].at) return stops[0].color;
            for (int i = 1; i < stops.Length; i++)
                if (t <= stops[i].at) return Color.Lerp(stops[i - 1].color, stops[i].color, (t - stops[i - 1].at) / (stops[i].at - stops[i - 1].at));
            return stops[stops.Length - 1].color;
        }

        /// <summary>Khoảng cách có dấu tới hình chữ nhật bo góc tâm (0,0), nửa cạnh (hx, hy), bán kính r (âm = bên trong).</summary>
        static float Sd(float x, float y, float hx, float hy, float r)
        {
            r = Mathf.Min(r, Mathf.Min(hx, hy));
            float qx = Mathf.Abs(x) - (hx - r), qy = Mathf.Abs(y) - (hy - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>
        /// Khối bo góc cỡ w×h pt: viền <paramref name="border"/> pt màu borderColor, ruột chuyển màu dọc theo stops,
        /// bóng cứng cao <paramref name="drop"/> pt màu dropColor phía dưới (ảnh cao h + drop).
        /// sliceX = true: đặt 9-slice trái/phải bằng bán kính để co giãn bề ngang (chip, thanh tiến trình).
        /// sliceY = true: thêm 9-slice trên/dưới (dưới gồm cả bóng cứng) để co giãn chiều cao (thẻ tự cao theo nội dung).
        /// </summary>
        internal static Sprite Box(string name, float w, float h, float radius, float border, Color borderColor, Stop[] stops,
            float drop = 0f, Color dropColor = default, bool sliceX = false, bool sliceY = false)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            int pw = Mathf.RoundToInt(w * K), ph = Mathf.RoundToInt((h + drop) * K);
            var px = new Color32[pw * ph];
            float hx = w / 2f, hy = h / 2f;
            for (int py = 0; py < ph; py++)
                for (int x = 0; x < pw; x++)
                {
                    float fx = (x + 0.5f) / K - hx, fyTop = (ph - 1 - py + 0.5f) / K; // y tính từ mép trên, xuống dưới
                    float fy = fyTop - hy;
                    Color o = Color.clear;
                    if (drop > 0f)
                    {
                        float ds = Sd(fx, fy - drop, hx, hy, radius);
                        float a = Mathf.Clamp01(0.5f - ds * K) * dropColor.a;
                        o = new Color(dropColor.r, dropColor.g, dropColor.b, a);
                    }
                    float d = Sd(fx, fy, hx, hy, radius);
                    float cover = Mathf.Clamp01(0.5f - d * K);
                    if (cover > 0f)
                    {
                        float inner = border > 0f ? Mathf.Clamp01(0.5f - (d + border) * K) : 1f;
                        Color fill = Grad(stops, Mathf.Clamp01(fyTop / h));
                        Color c = Color.Lerp(borderColor, fill, inner);
                        o = Over(o, c, cover);
                    }
                    px[py * pw + x] = o;
                }
            AmbientArtBuilder.Write(path, pw, ph, px);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var sprite = GameSceneBuilder.LoadSprite(path);
            if (sliceX || sliceY)
            {
                float b = sliceX ? Mathf.Min(radius, w / 2f) * K + 1f : 0f;
                float top = sliceY ? Mathf.Min(radius, h / 2f) * K + 1f : 0f, bottom = sliceY ? top + drop * K : 0f;
                importer.spriteBorder = new Vector4(b, bottom, b, top);
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }

        /// <summary>Bóng mờ (box-shadow 0 dy blur) của khối w×h bo r: ảnh rộng thêm blur mỗi bên, đặt lệch dy khi dùng.</summary>
        internal static Sprite SoftShadow(string name, float w, float h, float radius, float blur, float alpha)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            int pw = Mathf.RoundToInt((w + 2 * blur) * K), ph = Mathf.RoundToInt((h + 2 * blur) * K);
            var px = new Color32[pw * ph];
            float sigma = blur / 2f;
            for (int py = 0; py < ph; py++)
                for (int x = 0; x < pw; x++)
                {
                    float fx = (x + 0.5f) / K - (w / 2f + blur), fy = (py + 0.5f) / K - (h / 2f + blur);
                    float d = Sd(fx, fy, w / 2f, h / 2f, radius);
                    float a = alpha * (1f - Mathf.Clamp01(0.5f + 0.5f * (float)System.Math.Tanh(d / sigma * 0.8f)));
                    px[py * pw + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                }
            AmbientArtBuilder.Write(path, pw, ph, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        // ───────────── Icon vẽ từ path SVG (viewBox 24) của bản thiết kế ─────────────

        /// <summary>Một nét vẽ của icon: đa giác (ruột) và/hoặc đường gấp khúc (nét), toạ độ viewBox 24, y xuống.</summary>
        internal class IconShape
        {
            public Vector2[] points;
            public bool closed;
            public Color fill = Color.clear;
            public Color stroke = Color.clear;
            public float strokeWidth;
        }

        /// <summary>Rasterize icon vuông sizePt (khử răng cưa 4×4), các hình vẽ theo thứ tự, ruột trước nét sau.</summary>
        internal static Sprite Icon(string name, float sizePt, params IconShape[] shapes)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            int n = Mathf.RoundToInt(sizePt * K);
            float k = n / 24f;
            var px = new Color32[n * n];
            for (int py = 0; py < n; py++)
                for (int x = 0; x < n; x++)
                {
                    Color o = Color.clear;
                    foreach (var sh in shapes)
                    {
                        int fill = 0, stroke = 0;
                        for (int j = 0; j < 4; j++)
                            for (int i = 0; i < 4; i++)
                            {
                                var p = new Vector2((x + (i + 0.5f) / 4f) / k, (n - 1 - py + (j + 0.5f) / 4f) / k);
                                if (sh.closed && sh.fill.a > 0f && Inside(p, sh.points)) fill++;
                                if (sh.stroke.a > 0f && PolyDist(p, sh.points, sh.closed) <= sh.strokeWidth / 2f) stroke++;
                            }
                        if (fill > 0) o = Over(o, sh.fill, fill / 16f);
                        if (stroke > 0) o = Over(o, sh.stroke, stroke / 16f);
                    }
                    px[py * n + x] = o;
                }
            AmbientArtBuilder.Write(path, n, n, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        static float PolyDist(Vector2 p, Vector2[] pts, bool closed)
        {
            float best = float.MaxValue;
            int count = closed ? pts.Length : pts.Length - 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Length], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        static Vector2[] Circle(float cx, float cy, float r, int seg = 48)
        {
            var pts = new Vector2[seg];
            for (int i = 0; i < seg; i++) pts[i] = new Vector2(cx + r * Mathf.Cos(i * Mathf.PI * 2f / seg), cy + r * Mathf.Sin(i * Mathf.PI * 2f / seg));
            return pts;
        }

        /// <summary>Cung tròn từ góc a0 tới a1 (độ, 0 = phải, chiều kim đồng hồ vì y xuống).</summary>
        static Vector2[] Arc(float cx, float cy, float r, float a0, float a1, int seg = 24)
        {
            var pts = new Vector2[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad;
                pts[i] = new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a));
            }
            return pts;
        }

        static Vector2[] Cubic(Vector2 p0, Vector2 c0, Vector2 c1, Vector2 p1, int seg = 24)
        {
            var pts = new Vector2[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg, u = 1f - t;
                pts[i] = u * u * u * p0 + 3f * u * u * t * c0 + 3f * u * t * t * c1 + t * t * t * p1;
            }
            return pts;
        }

        /// <summary>Loa (Guide25D, mục Âm thanh): path "M4 9.5h3.5L12 5.5v13l-4.5-4H4z" vàng + 2 sóng a4 / a7.5.</summary>
        internal static Sprite SpeakerIcon()
        {
            var dark = GameSceneBuilder.Hex("3A2412");
            var body = new IconShape
            {
                points = new[] { new Vector2(4, 9.5f), new Vector2(7.5f, 9.5f), new Vector2(12, 5.5f), new Vector2(12, 18.5f), new Vector2(7.5f, 14.5f), new Vector2(4, 14.5f) },
                closed = true, fill = GameSceneBuilder.Hex("FFC93C"), stroke = dark, strokeWidth = 1.8f,
            };
            // "M15.5 9a4 4 0 0 1 0 6" và "M18 6.5a7.5 7.5 0 0 1 0 11": cung lồi sang phải
            float c1 = 15.5f - Mathf.Sqrt(16f - 9f), c2 = 18f - Mathf.Sqrt(56.25f - 30.25f);
            float h1 = Mathf.Asin(3f / 4f) * Mathf.Rad2Deg, h2 = Mathf.Asin(5.5f / 7.5f) * Mathf.Rad2Deg;
            var w1 = new IconShape { points = Arc(c1, 12, 4, -h1, h1), stroke = dark, strokeWidth = 1.8f };
            var w2 = new IconShape { points = Arc(c2, 12, 7.5f, -h2, h2), stroke = dark, strokeWidth = 1.8f };
            return Icon("icon_speaker", 24, body, w1, w2);
        }

        /// <summary>Quả địa cầu (mục Ngôn ngữ): tròn r 9,5 xanh #8FD3FF + xích đạo + 2 kinh tuyến cong.</summary>
        internal static Sprite GlobeIcon()
        {
            var dark = GameSceneBuilder.Hex("3A2412");
            var ball = new IconShape { points = Circle(12, 12, 9.5f), closed = true, fill = GameSceneBuilder.Hex("8FD3FF"), stroke = dark, strokeWidth = 1.8f };
            var eq = new IconShape { points = new[] { new Vector2(2.5f, 12), new Vector2(21.5f, 12) }, stroke = dark, strokeWidth = 1.6f };
            var m1 = new IconShape { points = Cubic(new Vector2(12, 2.5f), new Vector2(15, 5.5f), new Vector2(15, 18.5f), new Vector2(12, 21.5f)), stroke = dark, strokeWidth = 1.6f };
            var m2 = new IconShape { points = Cubic(new Vector2(12, 2.5f), new Vector2(9, 5.5f), new Vector2(9, 18.5f), new Vector2(12, 21.5f)), stroke = dark, strokeWidth = 1.6f };
            return Icon("icon_globe", 24, ball, eq, m1, m2);
        }

        /// <summary>
        /// Nút .gbtn vẽ đúng cỡ (w × h pt, gồm viền 3) + cạnh 3D <paramref name="drop"/> pt màu edge phía dưới:
        /// nền linear-gradient(hi 0%, base 55%, lo 100%), viền #3A2412, bóng trong 0 −3px 0 rgba(0,0,0,.08),
        /// vệt sáng ::after (cách trái/phải 8%, top 3, cao 40%, bo 12, trắng .75 → 0).
        /// </summary>
        internal static Sprite GameButton(string name, float w, float h, float radius, string hi, string mid, string lo, string edge, float drop = 5f,
            float border = 3f, bool sliceX = false)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            Color dark = GameSceneBuilder.Hex("3A2412"), edgeC = GameSceneBuilder.Hex(edge);
            var stops = new[] { new Stop(0f, hi), new Stop(0.55f, mid), new Stop(1f, lo) };
            int pw = Mathf.RoundToInt(w * K), ph = Mathf.RoundToInt((h + drop) * K);
            var px = new Color32[pw * ph];
            float hx = w / 2f, hy = h / 2f;
            float innerW = w - 2 * border, innerH = h - 2 * border;
            float shX = innerW / 2f - 0.08f * innerW, shH = 0.4f * innerH, shTop = border + 3f;
            for (int py = 0; py < ph; py++)
                for (int x = 0; x < pw; x++)
                {
                    float fx = (x + 0.5f) / K - hx, fyTop = (ph - 1 - py + 0.5f) / K, fy = fyTop - hy;
                    Color o = Color.clear;
                    float ds = Sd(fx, fy - drop, hx, hy, radius);
                    float da = Mathf.Clamp01(0.5f - ds * K);
                    if (da > 0f) o = new Color(edgeC.r, edgeC.g, edgeC.b, da);
                    float d = Sd(fx, fy, hx, hy, radius);
                    float cover = Mathf.Clamp01(0.5f - d * K);
                    if (cover > 0f)
                    {
                        float inner = Mathf.Clamp01(0.5f - (d + border) * K);
                        Color fill = Grad(stops, Mathf.Clamp01((fyTop - border) / innerH));
                        // inset 0 −3px 0 rgba(0,0,0,.08): dải 3 pt sát mép dưới phía trong viền
                        float bottomBand = Mathf.Clamp01((fyTop - (h - border - 3f)) * K + 0.5f);
                        fill = Color.Lerp(fill, Color.black, 0.08f * bottomBand);
                        // vệt sáng phía trên
                        float sy = fyTop - shTop;
                        if (sy >= 0f && sy <= shH)
                        {
                            float sd = Sd(fx, sy - shH / 2f, shX, shH / 2f, 12f);
                            float sc = Mathf.Clamp01(0.5f - sd * K);
                            fill = Color.Lerp(fill, Color.white, 0.75f * (1f - sy / shH) * sc);
                        }
                        Color c = Color.Lerp(dark, fill, inner);
                        o = Over(o, c, cover);
                    }
                    px[py * pw + x] = o;
                }
            AmbientArtBuilder.Write(path, pw, ph, px);
            var sprite = GameSceneBuilder.LoadSprite(path);
            if (sliceX)
            {
                // co giãn ngang: giữ nguyên góc bo + phần đầu/cuối vệt sáng
                float b = (Mathf.Max(radius, 0.08f * (w - 2 * border) + border + 12f) + 1f) * K;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.spriteBorder = new Vector4(b, 0, b, 0);
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sprite;
        }

        /// <summary>Vành bo góc (outline) dày <paramref name="thickness"/> pt quanh khối w × h bo r; trong suốt bên trong. 9-slice 4 phía.</summary>
        internal static Sprite Ring(string name, float w, float h, float radius, float thickness, string color)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            Color c = GameSceneBuilder.Hex(color);
            int pw = Mathf.RoundToInt(w * K), ph = Mathf.RoundToInt(h * K);
            var px = new Color32[pw * ph];
            float hx = w / 2f, hy = h / 2f;
            for (int py = 0; py < ph; py++)
                for (int x = 0; x < pw; x++)
                {
                    float d = Sd((x + 0.5f) / K - hx, (py + 0.5f) / K - hy, hx, hy, radius);
                    float a = Mathf.Clamp01(0.5f - d * K) * Mathf.Clamp01(0.5f + (d + thickness) * K);
                    px[py * pw + x] = new Color(c.r, c.g, c.b, a);
                }
            AmbientArtBuilder.Write(path, pw, ph, px);
            GameSceneBuilder.LoadSprite(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            float b = (radius + 1f) * K;
            importer.spriteBorder = new Vector4(b, b, b, b);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Viền sáng quanh nút đang bật: box-shadow 0 0 0 3px #FFE14A, 0 0 14px 4px rgba(255,225,74,.85) quanh khối w × h bo r.
        /// Ảnh rộng thêm <paramref name="pad"/> pt mỗi bên; đặt sau nút, tâm trùng tâm mặt nút.
        /// </summary>
        internal static Sprite ActiveGlow(string name, float w, float h, float radius, float pad = 24f)
        {
            string path = $"{Dir}/{name}.png";
            GameSceneBuilder.EnsureFolder(Dir);
            int pw = Mathf.RoundToInt((w + 2 * pad) * K), ph = Mathf.RoundToInt((h + 2 * pad) * K);
            var px = new Color32[pw * ph];
            float hx = w / 2f, hy = h / 2f, cx = pw / K / 2f, cy = ph / K / 2f;
            Color ring = GameSceneBuilder.Hex("FFE14A"), glow = new Color(1f, 225f / 255f, 74f / 255f);
            const float spread = 4f, sigma = 7f; // blur 14 → sigma 7
            for (int py = 0; py < ph; py++)
                for (int x = 0; x < pw; x++)
                {
                    float fx = (x + 0.5f) / K - cx, fy = (py + 0.5f) / K - cy;
                    float d = Sd(fx, fy, hx, hy, radius);
                    // bóng mờ: hình nở 4 pt, nhoè Gauss (xấp xỉ hàm phân phối bằng logistic)
                    float g = 0.85f / (1f + Mathf.Exp((d - spread) / (sigma * 0.6f)));
                    if (d < 0f) g = 0f; // trong mặt nút: nút che, để trong suốt
                    Color o = new Color(glow.r, glow.g, glow.b, g);
                    float r = Mathf.Clamp01(0.5f - (d - 3f) * K) * Mathf.Clamp01(0.5f + d * K); // vành 0–3 pt
                    o = Over(o, ring, r);
                    px[py * pw + x] = o;
                }
            AmbientArtBuilder.Write(path, pw, ph, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        static Color Over(Color under, Color c, float a)
        {
            float oa = a + under.a * (1f - a);
            if (oa <= 0f) return Color.clear;
            Color o = (c * a + under * under.a * (1f - a)) / oa;
            o.a = oa;
            return o;
        }
    }
}
