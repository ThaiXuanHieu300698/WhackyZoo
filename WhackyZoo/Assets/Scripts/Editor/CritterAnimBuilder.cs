using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Sinh khung hình phụ cho thú trong hố từ mã SVG của bản thiết kế (Docs/DesignSource/Game*.dc.html):
    ///  • chớp mắt (.eyes, Animations.md mục 2): nhóm mắt bị ép dọc quanh tâm nhóm — sinh 2 khung scaleY 0,55 và 0,1;
    ///  • lấp lánh (.spark): cắt tia lấp lánh ra lớp riêng để nhấp nháy, phần còn lại thành animSprite.
    /// Sprite PNG xuất cùng hệ toạ độ với SVG (viewBox 100 → 300 px @3x) nên toạ độ mắt dùng thẳng được.
    /// </summary>
    internal static class CritterAnimBuilder
    {
        const string OutDir = "Assets/Art/Generated/Critters";
        const int SS = 4; // lấy mẫu 4×4 mỗi điểm ảnh để khử răng cưa
        static readonly float[] BlinkScales = { 0.55f, 0.1f };

        static readonly Dictionary<string, string> DesignFiles = new Dictionary<string, string>
        {
            { "land", "GameLand25D" }, { "sea", "GameSea25D" }, { "desert", "GameDesert25D" },
            { "arctic", "GameArctic25D" }, { "grave", "GameGrave25D" },
        };

        // Tên con trong sprite → khoá trong mã thiết kế ({{c.khoá}}); trùng tên thì không cần ghi
        static readonly Dictionary<string, string> DesignKeys = new Dictionary<string, string>
        {
            { "octopus", "octo" }, { "starfish", "star" }, { "meerkat", "meer" }, { "polar_bear", "bear" },
            { "penguin", "peng" }, { "black_cat", "bcat" }, { "golden_chick", "chick" }, { "golden_vase", "vase" },
            { "golden_salmon", "salmon" }, { "golden_firefly", "firefly" }, { "ice_ball", "iceball" },
        };

        class Shape
        {
            public bool poly;
            public bool open; // đường gấp khúc hở (chỉ có nét), không phải đa giác kín
            public float cx, cy, rx, ry;
            public Vector2[] pts;
            public Color fill;
            public bool hasFill, hasStroke;
            public Color stroke;
            public float sw;
        }

        class Parts
        {
            public readonly List<Shape> eyes = new List<Shape>();
            public readonly List<Shape> spark = new List<Shape>();
        }

        static readonly Dictionary<string, Dictionary<string, Parts>> cache = new Dictionary<string, Dictionary<string, Parts>>();

        /// <summary>Sinh và gán animSprite / blinkHalf / blinkClosed / spark cho một con. Không có dữ liệu thiết kế thì để trống.</summary>
        internal static void Apply(CritterData c, string mapId, string animalFile, string spritePath)
        {
            c.animSprite = c.blinkHalf = c.blinkClosed = c.spark = null;
            var parts = Find(mapId, animalFile);
            if (parts == null || !File.Exists(spritePath)) return;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(spritePath));
            var img = new Img(tex.width, tex.height, tex.GetPixels32());
            Object.DestroyImmediate(tex);
            float k = img.w / 100f; // px trên một đơn vị viewBox
            GameSceneBuilder.EnsureFolder(OutDir);
            string id = c.id;

            if (parts.spark.Count > 0)
            {
                // Vùng tia lấp lánh = hình + nửa nét viền + 1,3 đơn vị để không sót điểm khử răng cưa
                bool InSpark(float x, float y)
                {
                    foreach (var s in parts.spark) if (Dist(s, x, y) <= s.sw / 2f + 1.3f) return true;
                    return false;
                }
                int x0 = img.w, y0 = img.h, x1 = -1, y1 = -1;
                var inside = new bool[img.w * img.h];
                for (int y = 0; y < img.h; y++)
                    for (int x = 0; x < img.w; x++)
                        if (InSpark((x + 0.5f) / k, (y + 0.5f) / k))
                        {
                            inside[y * img.w + x] = true;
                            x0 = Mathf.Min(x0, x); y0 = Mathf.Min(y0, y); x1 = Mathf.Max(x1, x); y1 = Mathf.Max(y1, y);
                        }
                if (x1 >= 0)
                {
                    int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
                    var crop = new Img(cw, ch);
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                            if (inside[y * img.w + x])
                            {
                                crop.Set(x - x0, y - y0, img.Get(x, y));
                                img.Set(x, y, new Color32(0, 0, 0, 0));
                            }
                    c.spark = Save(crop, $"{OutDir}/{id}_spark.png");
                    // transform-origin 50% 50% của fill-box: tâm khung bao hình (không tính nét)
                    var (bx0, by0, bx1, by1) = Bounds(parts.spark);
                    float scx = (bx0 + bx1) / 2f * k, scy = (by0 + by1) / 2f * k;
                    c.sparkCenter = new Vector2(scx / img.w, 1f - scy / img.h);
                    c.sparkPivot = new Vector2((scx - x0) / cw, 1f - (scy - y0) / ch);
                    c.sparkSize = new Vector2(cw / (float)img.w, ch / (float)img.h);
                    c.animSprite = Save(img, $"{OutDir}/{id}_base.png");
                }
            }

            if (parts.eyes.Count > 0)
            {
                var (ex0, ey0, ex1, ey1) = Bounds(parts.eyes);
                float gy = (ey0 + ey1) / 2f; // tâm nhóm mắt (fill-box)
                bool InEyes(float x, float y)
                {
                    foreach (var s in parts.eyes) if (Dist(s, x, y) <= s.sw / 2f + 0.6f) return true;
                    return false;
                }
                // Mắt nhô MỘT PHẦN ra ngoài đầu (vd. thằn lằn): không đoán được phần đầu nằm dưới mắt,
                // xoá đi sẽ để lại vệt loang → bỏ chớp mắt cho con này. Mắt nằm trọn trong mặt hoặc nhô hẳn ra (cua) thì ổn.
                float edge = TransparentEdgeRatio(img, InEyes, k);
                if (edge > 0.05f && edge < 0.95f)
                {
                    Debug.Log($"[WhackyZoo] {id}: mắt nhô một phần ra ngoài sprite ({edge:P0} mép trong suốt) — bỏ qua chớp mắt.");
                    return;
                }
                var erased = img.Clone();
                Inpaint(erased, InEyes, k);
                var frames = new Sprite[BlinkScales.Length];
                for (int i = 0; i < BlinkScales.Length; i++)
                {
                    var frame = erased.Clone();
                    foreach (var s in parts.eyes) DrawSquished(frame, s, gy, BlinkScales[i], k);
                    frames[i] = Save(frame, $"{OutDir}/{id}_blink{i}.png");
                }
                c.blinkHalf = frames[0];
                c.blinkClosed = frames[1];
            }
        }

        // ───────────── Đọc SVG trong mã thiết kế ─────────────

        static Parts Find(string mapId, string animalFile)
        {
            if (!DesignFiles.TryGetValue(mapId, out var file)) return null;
            if (!cache.TryGetValue(file, out var table))
            {
                table = Parse(Path.Combine(Application.dataPath, "..", "..", "Docs", "DesignSource", file + ".dc.html"));
                cache[file] = table;
            }
            string key = DesignKeys.TryGetValue(animalFile, out var k) ? k : animalFile;
            return table.TryGetValue(key, out var p) ? p : null;
        }

        static Dictionary<string, Parts> Parse(string path)
        {
            var table = new Dictionary<string, Parts>();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WhackyZoo] Không thấy {path}: bỏ qua chớp mắt / lấp lánh.");
                return table;
            }
            string html = File.ReadAllText(path);
            foreach (Match block in Regex.Matches(html, "<sc-if value=\"\\{\\{c\\.(\\w+)\\}\\}\"[\\s\\S]*?</sc-if>"))
            {
                var parts = new Parts();
                foreach (Match g in Regex.Matches(block.Value, "class=\"eyes\"[^>]*>([\\s\\S]*?)</g>"))
                    parts.eyes.AddRange(Shapes(g.Groups[1].Value));
                foreach (Match g in Regex.Matches(block.Value, "class=\"spark\"[^>]*>([\\s\\S]*?)</g>"))
                    parts.spark.AddRange(Shapes(g.Groups[1].Value));
                if (parts.eyes.Count > 0 || parts.spark.Count > 0) table[block.Groups[1].Value] = parts;
            }
            return table;
        }

        static IEnumerable<Shape> Shapes(string svg)
        {
            foreach (Match m in Regex.Matches(svg, "<(ellipse|circle|path)\\b([^>]*)/?>"))
            {
                string a = m.Groups[2].Value;
                var s = new Shape();
                if (m.Groups[1].Value == "path")
                {
                    // Hỗ trợ đa giác kín M…L…Z và các đoạn thẳng hở (vd. lông mi "M36 46 L33 43 M38.5 44.5 L37.5 41");
                    // mỗi lệnh M mở một hình riêng. Đường cong (Q/C/A) không hỗ trợ.
                    string d = Attr(a, "d") ?? "";
                    if (Regex.IsMatch(d, "[QqCcSsTtAaHhVv]"))
                    {
                        Debug.LogWarning($"[WhackyZoo] Bỏ qua path có đường cong trong nhóm mắt: {d}");
                        continue;
                    }
                    bool hasFill = TryColor(Attr(a, "fill"), out var pf), hasStroke = TryColor(Attr(a, "stroke"), out var pc);
                    float psw = hasStroke ? F(Attr(a, "stroke-width") ?? "1") : 0f;
                    foreach (var sub in Regex.Split(d, "(?=[Mm])"))
                    {
                        var nums = Regex.Matches(sub, "-?[\\d.]+");
                        var pts = new List<Vector2>();
                        for (int i = 0; i + 1 < nums.Count; i += 2) pts.Add(new Vector2(F(nums[i].Value), F(nums[i + 1].Value)));
                        bool closed = Regex.IsMatch(sub, "[Zz]");
                        if (closed ? pts.Count < 3 : pts.Count < 2) continue;
                        yield return new Shape
                        {
                            poly = true, open = !closed, pts = pts.ToArray(),
                            hasFill = hasFill && closed, fill = pf, hasStroke = hasStroke, stroke = pc, sw = psw,
                        };
                    }
                    continue;
                }
                else
                {
                    s.cx = F(Attr(a, "cx"));
                    s.cy = F(Attr(a, "cy"));
                    if (m.Groups[1].Value == "circle") s.rx = s.ry = F(Attr(a, "r"));
                    else { s.rx = F(Attr(a, "rx")); s.ry = F(Attr(a, "ry")); }
                }
                s.hasFill = TryColor(Attr(a, "fill"), out s.fill);
                s.hasStroke = TryColor(Attr(a, "stroke"), out s.stroke);
                s.sw = s.hasStroke ? F(Attr(a, "stroke-width") ?? "1") : 0f;
                yield return s;
            }
        }

        static string Attr(string attrs, string name)
        {
            var m = Regex.Match(attrs, "\\b" + name + "=\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        static float F(string s) => s == null ? 0f : float.Parse(s, CultureInfo.InvariantCulture);

        static bool TryColor(string v, out Color c)
        {
            c = Color.clear;
            return v != null && v.StartsWith("#") && ColorUtility.TryParseHtmlString(v, out c);
        }

        // ───────────── Hình học ─────────────

        /// <summary>Khoảng cách tới hình (≤ 0 là bên trong), đơn vị viewBox.</summary>
        static float Dist(Shape s, float x, float y)
        {
            if (s.poly && s.open)
            {
                float best = float.MaxValue;
                for (int i = 1; i < s.pts.Length; i++) best = Mathf.Min(best, SegDist(new Vector2(x, y), s.pts[i - 1], s.pts[i]));
                return best;
            }
            if (s.poly)
            {
                bool inside = false;
                float best = float.MaxValue;
                for (int i = 0, j = s.pts.Length - 1; i < s.pts.Length; j = i++)
                {
                    Vector2 a = s.pts[j], b = s.pts[i];
                    if ((b.y > y) != (a.y > y) && x < (a.x - b.x) * (y - b.y) / (a.y - b.y) + b.x) inside = !inside;
                    best = Mathf.Min(best, SegDist(new Vector2(x, y), a, b));
                }
                return inside ? -best : best;
            }
            // xấp xỉ khoảng cách tới elip (đủ cho vài phần mười đơn vị)
            float nx = (x - s.cx) / s.rx, ny = (y - s.cy) / s.ry;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            return (r - 1f) * Mathf.Min(s.rx, s.ry);
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector2.Distance(p, a + ab * t);
        }

        static (float, float, float, float) Bounds(List<Shape> shapes)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var s in shapes)
            {
                if (s.poly)
                    foreach (var p in s.pts) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
                else { x0 = Mathf.Min(x0, s.cx - s.rx); x1 = Mathf.Max(x1, s.cx + s.rx); y0 = Mathf.Min(y0, s.cy - s.ry); y1 = Mathf.Max(y1, s.cy + s.ry); }
            }
            return (x0, y0, x1, y1);
        }

        /// <summary>Tỉ lệ điểm ảnh ngay sát mép trái/phải vùng mắt là nền trong suốt.</summary>
        static float TransparentEdgeRatio(Img img, Func<float, float, bool> region, float k)
        {
            int total = 0, clear = 0;
            for (int y = 0; y < img.h; y++)
                for (int x = 1; x < img.w - 1; x++)
                {
                    if (!region((x + 0.5f) / k, (y + 0.5f) / k)) continue;
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        if (region((x + dx + 0.5f) / k, (y + 0.5f) / k)) continue;
                        total++;
                        if (img.Get(x + dx, y).a < 128) clear++;
                    }
                }
            return total == 0 ? 0f : clear / (float)total;
        }

        /// <summary>Xoá vùng mắt: lấp mỗi hàng bằng cách nội suy giữa điểm ảnh còn lại bên trái và bên phải.</summary>
        static void Inpaint(Img img, Func<float, float, bool> erase, float k)
        {
            var mask = new bool[img.w];
            for (int y = 0; y < img.h; y++)
            {
                for (int x = 0; x < img.w; x++) mask[x] = erase((x + 0.5f) / k, (y + 0.5f) / k);
                int xi = 0;
                while (xi < img.w)
                {
                    if (!mask[xi]) { xi++; continue; }
                    int start = xi;
                    while (xi < img.w && mask[xi]) xi++;
                    int end = xi - 1;
                    bool hasL = start > 0, hasR = end < img.w - 1;
                    Color32 L = hasL ? img.Get(start - 1, y) : default, R = hasR ? img.Get(end + 1, y) : default;
                    if (!hasL) L = R;
                    if (!hasR) R = L;
                    for (int x = start; x <= end; x++)
                    {
                        float t = (x - start + 1f) / (end - start + 2f);
                        img.Set(x, y, LerpPremul(L, R, t));
                    }
                }
            }
        }

        static Color32 LerpPremul(Color32 a, Color32 b, float t)
        {
            float aa = a.a / 255f, ba = b.a / 255f;
            float al = Mathf.Lerp(aa, ba, t);
            if (al <= 0f) return new Color32(0, 0, 0, 0);
            float r = Mathf.Lerp(a.r * aa, b.r * ba, t) / al, g = Mathf.Lerp(a.g * aa, b.g * ba, t) / al, bl = Mathf.Lerp(a.b * aa, b.b * ba, t) / al;
            return new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(bl), 0, 255), (byte)Mathf.RoundToInt(al * 255f));
        }

        /// <summary>Vẽ một hình mắt bị ép dọc (scaleY = s quanh tâm nhóm gy): ruột trước, nét viền sau như SVG.</summary>
        static void DrawSquished(Img img, Shape sh, float gy, float s, float k)
        {
            if (sh.poly && sh.open)
            {
                DrawSquishedLines(img, sh, gy, s, k);
                return;
            }
            if (sh.poly) return;
            float cy = gy + (sh.cy - gy) * s;
            float half = sh.sw / 2f;
            float minY = cy - (sh.ry + half) * s - 1f, maxY = cy + (sh.ry + half) * s + 1f;
            float minX = sh.cx - sh.rx - half - 1f, maxX = sh.cx + sh.rx + half + 1f;
            int px0 = Mathf.Max(0, Mathf.FloorToInt(minX * k)), px1 = Mathf.Min(img.w - 1, Mathf.CeilToInt(maxX * k));
            int py0 = Mathf.Max(0, Mathf.FloorToInt(minY * k)), py1 = Mathf.Min(img.h - 1, Mathf.CeilToInt(maxY * k));
            bool InEllipse(float x, float y, float rx, float ry) =>
                rx > 0f && ry > 0f && ((x - sh.cx) / rx) * ((x - sh.cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry) <= 1f;

            for (int py = py0; py <= py1; py++)
                for (int px = px0; px <= px1; px++)
                {
                    int fill = 0, stroke = 0;
                    for (int j = 0; j < SS; j++)
                        for (int i = 0; i < SS; i++)
                        {
                            float x = (px + (i + 0.5f) / SS) / k, y = (py + (j + 0.5f) / SS) / k;
                            if (sh.hasFill && InEllipse(x, y, sh.rx, sh.ry * s)) fill++;
                            if (sh.hasStroke && InEllipse(x, y, sh.rx + half, (sh.ry + half) * s) && !InEllipse(x, y, sh.rx - half, (sh.ry - half) * s)) stroke++;
                        }
                    if (fill > 0) img.Over(px, py, sh.fill, fill / (float)(SS * SS));
                    if (stroke > 0) img.Over(px, py, sh.stroke, stroke / (float)(SS * SS));
                }
        }

        /// <summary>Nét gấp khúc hở (đầu bo tròn) bị ép dọc quanh gy, ví dụ lông mi khép xuống theo mí mắt.</summary>
        static void DrawSquishedLines(Img img, Shape sh, float gy, float s, float k)
        {
            if (!sh.hasStroke) return;
            var pts = new Vector2[sh.pts.Length];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(sh.pts[i].x, gy + (sh.pts[i].y - gy) * s);
                minX = Mathf.Min(minX, pts[i].x); maxX = Mathf.Max(maxX, pts[i].x);
                minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y);
            }
            float half = sh.sw / 2f;
            int px0 = Mathf.Max(0, Mathf.FloorToInt((minX - half - 1f) * k)), px1 = Mathf.Min(img.w - 1, Mathf.CeilToInt((maxX + half + 1f) * k));
            int py0 = Mathf.Max(0, Mathf.FloorToInt((minY - half - 1f) * k)), py1 = Mathf.Min(img.h - 1, Mathf.CeilToInt((maxY + half + 1f) * k));
            for (int py = py0; py <= py1; py++)
                for (int px = px0; px <= px1; px++)
                {
                    int hit = 0;
                    for (int j = 0; j < SS; j++)
                        for (int i = 0; i < SS; i++)
                        {
                            var p = new Vector2((px + (i + 0.5f) / SS) / k, (py + (j + 0.5f) / SS) / k);
                            for (int n = 1; n < pts.Length; n++)
                                if (SegDist(p, pts[n - 1], pts[n]) <= half) { hit++; break; }
                        }
                    if (hit > 0) img.Over(px, py, sh.stroke, hit / (float)(SS * SS));
                }
        }

        static Sprite Save(Img img, string path)
        {
            var tex = new Texture2D(img.w, img.h, TextureFormat.RGBA32, false);
            var flipped = new Color32[img.px.Length];
            for (int y = 0; y < img.h; y++) Array.Copy(img.px, y * img.w, flipped, (img.h - 1 - y) * img.w, img.w);
            tex.SetPixels32(flipped);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return GameSceneBuilder.LoadSprite(path);
        }

        /// <summary>Ảnh RGBA với hàng 0 ở TRÊN (cùng chiều với toạ độ SVG).</summary>
        class Img
        {
            public readonly int w, h;
            public readonly Color32[] px;

            public Img(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }

            public Img(int w, int h, Color32[] bottomUp) : this(w, h)
            {
                for (int y = 0; y < h; y++) Array.Copy(bottomUp, (h - 1 - y) * w, px, y * w, w);
            }

            public Img Clone()
            {
                var c = new Img(w, h);
                Array.Copy(px, c.px, px.Length);
                return c;
            }

            public Color32 Get(int x, int y) => px[y * w + x];
            public void Set(int x, int y, Color32 c) => px[y * w + x] = c;

            /// <summary>Phủ màu c với độ che a lên điểm ảnh (toán tử "over").</summary>
            public void Over(int x, int y, Color c, float a)
            {
                a *= c.a;
                var d = px[y * w + x];
                float da = d.a / 255f;
                float oa = a + da * (1f - a);
                if (oa <= 0f) return;
                float r = (c.r * a + d.r / 255f * da * (1f - a)) / oa;
                float g = (c.g * a + d.g / 255f * da * (1f - a)) / oa;
                float b = (c.b * a + d.b / 255f * da * (1f - a)) / oa;
                px[y * w + x] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(g * 255f), (byte)Mathf.RoundToInt(b * 255f), (byte)Mathf.RoundToInt(oa * 255f));
            }
        }
    }
}
