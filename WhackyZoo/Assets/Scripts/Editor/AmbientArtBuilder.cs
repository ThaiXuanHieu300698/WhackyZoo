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
    /// Hiệu ứng nền màn chơi (Docs/Animations.md mục 3) lấy từ Docs/DesignSource/Game*.dc.html.
    /// Ảnh nền gốc (images/sprites/backgrounds) đã vẽ sẵn rong / tia / bóng dạng tĩnh, nên map có hiệu ứng
    /// được sinh lại một nền "sạch" từ đúng các dải màu CSS, rồi các vật chuyển động đặt lên trên.
    /// </summary>
    internal static class AmbientArtBuilder
    {
        const string Gen = GameSceneBuilder.Art + "/Generated";

        internal struct Result
        {
            public Sprite background; // null = giữ ảnh nền gốc
            public AmbientElement[] elements;
            public CardFxElement[] pathFx;   // hiệu ứng theo khung hình (MapData.ambientFx)
            public Sprite pathFxMask;        // mặt nạ cho pathFx có clip = MapMask
        }

        internal static Result Build(string mapId)
        {
            switch (mapId)
            {
                case "sea": return Sea();
                case "land": return Land();
                case "arctic": return Arctic();
                case "grave": return Grave();
                case "desert": return Desert();
                default: return new Result { elements = Array.Empty<AmbientElement>() };
            }
        }

        // ───────────── Trên cạn / Bắc cực / Nghĩa trang (GameLand25D, GameArctic25D, GameGrave25D) ─────────────
        // Ảnh nền gốc vẽ sẵn mây / tuyết / đom đóm ở đúng vị trí ban đầu của hiệu ứng → xoá bản vẽ tĩnh rồi đặt bản động lên.

        /// <summary>.cloud Trên cạn (GameLand25D): (20, 190, 104 × 34) 5 s và (150, 222, 70 × 24) 6,5 s lệch pha −2 s; đung đưa −12 ↔ +16 px, ease-in-out, qua lại.</summary>
        static Result Land()
        {
            var img = LoadBg("land");
            // Trời chỉ chuyển màu dọc: chép màu cột x 240 (không mây, không mặt trời) đè vùng mây; dừng trên ngọn cây (y 238)
            CopyColumn(img, 0, 180, 136, 238, 240);
            CopyColumn(img, 136, 212, 232, 256, 240);
            var bg = SaveBg(img, "land");
            const float pad = 14f;
            (float l, float t, float w, float h, float period, float delay)[] clouds = { (20, 190, 104, 34, 5f, 0f), (150, 222, 70, 24, 6.5f, -2f) };
            var list = new List<AmbientElement>();
            foreach (var c in clouds)
                list.Add(new AmbientElement
                {
                    name = "Cloud", sprite = Cloud($"{Gen}/land_cloud_{c.w}x{c.h}.png", c.w, c.h, pad),
                    position = new Vector2(c.l + c.w / 2f, c.t + c.h / 2f), size = new Vector2(c.w + 2 * pad, c.h + 2 * pad),
                    motion = AmbientMotion.Drift, period = c.period, delay = c.delay, angleFrom = -12f, angleTo = 16f, linear = false,
                });
            return new Result { background = bg, elements = list.ToArray() };
        }

        /// <summary>.flake Bắc cực: 8 bông tròn trắng 6–10 px có quầng 4 px; rơi (−20, 560) tuyến tính 9–12 s; alpha 0 → 1 (10%) → 0.</summary>
        static Result Arctic()
        {
            (float l, float t, float s, float delay, float dur)[] flakes =
            {
                (30, 120, 8, 0f, 9f), (90, 60, 6, 2.2f, 11f), (160, 150, 10, 1.1f, 10f), (230, 90, 7, 3.4f, 12f),
                (300, 140, 9, 0.6f, 9f), (350, 70, 6, 4.1f, 11f), (120, 220, 7, 5.0f, 10f), (270, 210, 8, 2.8f, 12f),
            };
            var img = LoadBg("arctic");
            // Lấp theo hàng (trời + dải cực quang đổi màu chậm theo chiều ngang); 2 bông sát mép dải cực quang dùng vùng hẹp hơn
            foreach (var f in flakes) RowFill(img, f.l + f.s / 2f, f.t + f.s / 2f, f.s / 2f + (f.l == 120 || f.l == 270 ? 6f : 14f));
            var bg = SaveBg(img, "arctic");
            var flake = GlowDot($"{Gen}/snow_flake_v2.png", 0.5f, Color.white, 1f, 0.5f, 0.5f, 0.9f);
            var list = new List<AmbientElement>();
            foreach (var f in flakes)
                list.Add(new AmbientElement
                {
                    name = "Flake", sprite = flake, position = new Vector2(f.l + f.s / 2f, f.t + f.s / 2f), size = Vector2.one * (f.s + 8f),
                    motion = AmbientMotion.Rise, period = f.dur, delay = f.delay,
                    midOffset = new Vector2(-10, 280), endOffset = new Vector2(-20, 560), fadeInAt = 0.1f,
                });
            return new Result { background = bg, elements = list.ToArray() };
        }

        /// <summary>.fly Nghĩa trang: 6 đom đóm 6 px #FFF6A0 quầng 0 0 8px 3px vàng .7; bay (+18, −26) ease-in-out 5–7 s qua lại; alpha .2 → 1 → .3.</summary>
        static Result Grave()
        {
            (float l, float t, float delay, float dur)[] flies = { (40, 250, 0f, 5f), (110, 200, 1.6f, 6f), (190, 262, 0.8f, 5f), (250, 220, 2.4f, 7f), (330, 270, 1.2f, 6f), (300, 180, 3.1f, 5f) };
            var img = LoadBg("grave");
            // Gỡ quầng sáng theo mẫu đo ở con nằm giữa vùng trời trơn (113, 203): nền dưới (bia mộ, hàng rào, trăng) giữ nguyên
            var glow = new Color(1f, 240f / 255f, 140f / 255f);
            var profile = GlowProfile(img, 113, 203, 22, glow);
            foreach (var f in flies) Unglow(img, f.l + 3, f.t + 3, profile, 0.9f, 3.5f, glow);
            var bg = SaveBg(img, "grave");
            var fly = GlowDot($"{Gen}/grave_firefly_v2.png", 3f / 18f, new Color(1f, 246f / 255f, 160f / 255f), 1f, 6f / 18f, 14f / 18f, 0.7f, glow);
            var list = new List<AmbientElement>();
            foreach (var f in flies)
                list.Add(new AmbientElement
                {
                    name = "Firefly", sprite = fly, position = new Vector2(f.l + 3, f.t + 3), size = Vector2.one * 36f,
                    motion = AmbientMotion.Wander, period = f.dur, delay = f.delay, endOffset = new Vector2(18, -26), alphaKeys = new Vector3(0.2f, 1f, 0.3f),
                });
            return new Result { background = bg, elements = list.ToArray(), pathFx = BatFlock() };
        }

        /// <summary>
        /// Đàn 5 dơi bay chéo qua mặt trăng (GameGrave25D .flock): cả đàn bay từ (−140, 232) theo hướng tới (470, 160) với cùng tốc độ
        /// (610 px trong 60% của 11 s), tuyến tính, rồi nghỉ. Điểm cuối kéo dài tới x 530 (≈ 66% chu kỳ, y 153): ở x 470 như thiết kế,
        /// 2 con cuối (left −112, −126) vẫn nằm trong khung 390 nên bị thấy đứng lại; 530 − 126 > 390 nên cả đàn ra hẳn khỏi màn rồi mới nghỉ.
        /// mỗi con nhấp nhô 6 px 0,9 s qua lại, lệch pha 0–0,55 s; vỗ cánh = đổi khung card_fx_bat_wings_up / _down mỗi 0,22 s.
        /// </summary>
        static CardFxElement[] BatFlock()
        {
            var list = new List<CardFxElement>
            {
                new CardFxElement
                {
                    name = "Flock", period = 11f, ease = FxEase.Linear,
                    moveKeys = new[] { new Vector3(0f, -140f, 232f), new Vector3(0.66f, 530f, 153f), new Vector3(1f, 530f, 153f) },
                },
            };
            Sprite up = CardFx("bat_wings_up"), down = CardFx("bat_wings_down");
            (float l, float t, float w, float h, float delay)[] bats =
                { (0, 18, 56, 34, 0f), (-60, 0, 44, 26, 0.30f), (-54, 42, 40, 24, 0.55f), (-112, 22, 36, 22, 0.15f), (-126, 52, 30, 18, 0.40f) };
            foreach (var b in bats)
                list.Add(new CardFxElement
                {
                    name = "Bat", sprite = up, frames = new[] { down }, frameInterval = 0.22f, parent = 0,
                    left = b.l, top = b.t, width = b.w, height = b.h,
                    period = 0.9f, delay = b.delay, ease = FxEase.EaseInOut, alternate = true,
                    moveKeys = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, -6f) },
                });
            return list.ToArray();
        }

        /// <summary>
        /// Sa mạc (GameDesert25D .band): 4 dải gió mờ card_fx_wind_band trôi ngang trời x −260 → +420 px, ease-in-out, lặp vô hạn;
        /// alpha 0 → 1 (20%) → 1 (80%) → 0. Chỉ hiện trong vùng trời (mặt nạ sinh từ ảnh nền) nên nằm sau mặt trời và đồi cát.
        /// </summary>
        static Result Desert()
        {
            (float top, float w, float h, float period, float delay)[] bands =
                { (172, 220, 12, 7.0f, 0f), (204, 160, 10, 8.5f, 2.0f), (232, 240, 13, 7.6f, 3.8f), (188, 130, 9, 9.0f, 5.2f) };
            var band = CardFx("wind_band");
            var list = new CardFxElement[bands.Length];
            for (int i = 0; i < bands.Length; i++)
            {
                var d = bands[i];
                list[i] = new CardFxElement
                {
                    name = $"WindBand{i}", sprite = band, clip = FxClip.MapMask,
                    left = 0, top = d.top, width = d.w, height = d.h,
                    period = d.period, delay = d.delay, ease = FxEase.EaseInOut,
                    moveKeys = new[] { new Vector3(0f, -260f, 0f), new Vector3(1f, 420f, 0f) },
                    alphaKeys = new[] { new Vector2(0f, 0f), new Vector2(0.2f, 1f), new Vector2(0.8f, 1f), new Vector2(1f, 0f) },
                    restAlpha = 0f,
                };
            }
            return new Result { elements = Array.Empty<AmbientElement>(), pathFx = list, pathFxMask = DesertSkyMask() };
        }

        static Sprite CardFx(string name) => GameSceneBuilder.LoadSprite($"{GameSceneBuilder.Art}/MapCards/card_fx_{name}.png");

        /// <summary>
        /// Mặt nạ vùng trời của ảnh nền Sa mạc: trời là dải màu dọc có R = 255, mặt trời (+ quầng) và đồi cát khác màu.
        /// Mỗi hàng so với màu trời đo ở cột x 195 (ngoài mặt trời); dưới y 235 cột này chạm đồi nên ngoại suy theo độ dốc màu.
        /// Trên y 140 luôn là trời, dưới y 290 luôn là đất (dải gió nằm ở y 172–245).
        /// </summary>
        static Sprite DesertSkyMask()
        {
            string path = $"{Gen}/bg_map_desert_skymask.png";
            var img = LoadBg("desert");
            int refX = 195 * 3, yA = 195 * 3, yLim = 235 * 3, y0 = 140 * 3, y1 = 290 * 3, yLoose = 240 * 3;
            Color a = img[refX, yA], c = img[refX, yLim];
            float sg = (c.g - a.g) / (yLim - yA), sb = (c.b - a.b) / (yLim - yA);
            var px = new Color32[img.w * img.h];
            for (int y = 0; y < img.h; y++)
            {
                float rg, rb;
                if (y <= yLim) { var r = img[refX, y]; rg = r.g; rb = r.b; }
                else { rg = c.g + sg * (y - yLim); rb = c.b + sb * (y - yLim); }
                float tol = (y > yLoose ? 16f : 6f) / 255f;
                for (int x = 0; x < img.w; x++)
                {
                    bool sky;
                    if (y < y0) sky = true;
                    else if (y > y1) sky = false;
                    else { var p = img[x, y]; sky = p.r >= 254f / 255f && Mathf.Abs(p.g - rg) + Mathf.Abs(p.b - rb) <= tol; }
                    px[(img.h - 1 - y) * img.w + x] = new Color32(255, 255, 255, sky ? (byte)255 : (byte)0); // Bg: hàng 0 ở trên
                }
            }
            GameSceneBuilder.EnsureFolder(Gen);
            Write(path, img.w, img.h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        // ───── Ảnh nền dạng float RGB, hàng 0 ở TRÊN, đơn vị toạ độ pt × 3 ─────

        class Bg
        {
            public int w, h;
            public Color[] px;
            public Color this[int x, int y] { get => px[y * w + x]; set => px[y * w + x] = value; }
        }

        static Bg LoadBg(string id)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes($"{GameSceneBuilder.Art}/Backgrounds/bg_map_{id}.png"));
            var bg = new Bg { w = tex.width, h = tex.height, px = new Color[tex.width * tex.height] };
            var src = tex.GetPixels();
            for (int y = 0; y < bg.h; y++) Array.Copy(src, (bg.h - 1 - y) * bg.w, bg.px, y * bg.w, bg.w);
            Object.DestroyImmediate(tex);
            return bg;
        }

        static Sprite SaveBg(Bg bg, string id)
        {
            GameSceneBuilder.EnsureFolder(Gen);
            var px = new Color32[bg.w * bg.h];
            for (int y = 0; y < bg.h; y++)
                for (int x = 0; x < bg.w; x++) { var c = bg[x, y]; c.a = 1f; px[(bg.h - 1 - y) * bg.w + x] = c; }
            string path = $"{Gen}/bg_map_{id}_clean.png";
            Write(path, bg.w, bg.h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        const float Px = 3f;

        static void CopyColumn(Bg bg, float x0, float y0, float x1, float y1, float refX)
        {
            int rx = Mathf.RoundToInt(refX * Px);
            for (int y = Mathf.RoundToInt(y0 * Px); y < Mathf.RoundToInt(y1 * Px); y++)
            {
                var c = bg[rx, y];
                for (int x = Mathf.Max(0, Mathf.RoundToInt(x0 * Px)); x < Mathf.Min(bg.w, Mathf.RoundToInt(x1 * Px)); x++) bg[x, y] = c;
            }
        }

        /// <summary>Lấp hình tròn theo từng hàng: nội suy giữa điểm ảnh ngay ngoài vùng ở bên trái và bên phải.</summary>
        static void RowFill(Bg bg, float cx, float cy, float r)
        {
            for (int y = Mathf.FloorToInt((cy - r) * Px); y <= Mathf.CeilToInt((cy + r) * Px); y++)
            {
                if (y < 0 || y >= bg.h) continue;
                float dy = y / Px - cy;
                if (Mathf.Abs(dy) > r) continue;
                float half = Mathf.Sqrt(r * r - dy * dy);
                int x0 = Mathf.Max(1, Mathf.FloorToInt((cx - half) * Px)), x1 = Mathf.Min(bg.w - 2, Mathf.CeilToInt((cx + half) * Px));
                Color L = bg[x0 - 1, y], R = bg[x1 + 1, y];
                for (int x = x0; x <= x1; x++) bg[x, y] = Color.Lerp(L, R, (x - x0 + 1f) / (x1 - x0 + 2f));
            }
        }

        /// <summary>
        /// Hồ sơ độ phủ quầng sáng a(d) quanh một chấm sáng nằm trên nền trơn: nền B ước lượng bằng lấp hàng bán kính R,
        /// a = (I − B)·(C − B) / |C − B|² (C = màu quầng). Chỉ số mảng = khoảng cách × 3.
        /// </summary>
        static float[] GlowProfile(Bg bg, float cx, float cy, float R, Color glow)
        {
            var copy = new Bg { w = bg.w, h = bg.h, px = (Color[])bg.px.Clone() };
            RowFill(copy, cx, cy, R);
            int n = Mathf.CeilToInt(R * Px) + 1;
            var sum = new float[n];
            var cnt = new int[n];
            for (int y = Mathf.FloorToInt((cy - R) * Px); y <= Mathf.CeilToInt((cy + R) * Px); y++)
                for (int x = Mathf.FloorToInt((cx - R) * Px); x <= Mathf.CeilToInt((cx + R) * Px); x++)
                {
                    float d = Mathf.Sqrt((x / Px - cx) * (x / Px - cx) + (y / Px - cy) * (y / Px - cy));
                    if (d > R) continue;
                    Color I = bg[x, y], B = copy[x, y];
                    float num = (I.r - B.r) * (glow.r - B.r) + (I.g - B.g) * (glow.g - B.g) + (I.b - B.b) * (glow.b - B.b);
                    float den = (glow.r - B.r) * (glow.r - B.r) + (glow.g - B.g) * (glow.g - B.g) + (glow.b - B.b) * (glow.b - B.b);
                    int bin = Mathf.Min(n - 1, Mathf.RoundToInt(d * Px));
                    sum[bin] += Mathf.Max(0f, num / Mathf.Max(den, 1e-4f));
                    cnt[bin]++;
                }
            for (int i = 0; i < n; i++) sum[i] = cnt[i] > 0 ? sum[i] / cnt[i] : 0f;
            return sum;
        }

        /// <summary>Gỡ quầng sáng: B = (I − a·C) / (1 − a); lõi chấm (bán kính core) thì lấp hàng.</summary>
        static void Unglow(Bg bg, float cx, float cy, float[] profile, float scale, float core, Color glow)
        {
            float R = (profile.Length - 1) / Px;
            for (int y = Mathf.FloorToInt((cy - R) * Px); y <= Mathf.CeilToInt((cy + R) * Px); y++)
                for (int x = Mathf.FloorToInt((cx - R) * Px); x <= Mathf.CeilToInt((cx + R) * Px); x++)
                {
                    if (x < 0 || y < 0 || x >= bg.w || y >= bg.h) continue;
                    float d = Mathf.Sqrt((x / Px - cx) * (x / Px - cx) + (y / Px - cy) * (y / Px - cy));
                    if (d > R || d <= core) continue;
                    float a = Mathf.Min(0.85f, profile[Mathf.Min(profile.Length - 1, Mathf.RoundToInt(d * Px))] * scale);
                    if (a <= 0.003f) continue;
                    var c = (bg[x, y] - glow * a) / (1f - a);
                    bg[x, y] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            RowFill(bg, cx, cy, core);
        }

        /// <summary>
        /// Chấm sáng tròn có quầng (box-shadow mờ): lõi bán kính coreFrac (theo nửa cạnh ảnh) màu core,
        /// quầng mở rộng tới spreadFrac rồi nhoè thêm blurFrac, độ đậm glowAlpha, màu glow (mặc định = màu lõi).
        /// </summary>
        static Sprite GlowDot(string path, float coreFrac, Color core, float coreAlpha, float spreadFrac, float blurFrac, float glowAlpha, Color? glow = null)
        {
            if (File.Exists(path)) return GameSceneBuilder.LoadSprite(path);
            GameSceneBuilder.EnsureFolder(Gen);
            const int n = 64;
            var g = glow ?? core;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f); // 0 ở tâm, 1 ở mép
                    float halo = glowAlpha * Mathf.Clamp01(1f - Mathf.Max(0f, d - spreadFrac) / Mathf.Max(blurFrac, 1e-3f));
                    halo *= halo; // mép mềm như bóng nhoè
                    halo *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - d) / 0.3f)); // về 0 trước mép ảnh, không lộ khung vuông
                    float dot = coreAlpha * Mathf.Clamp01((coreFrac - d) * n / 2f + 0.5f);
                    float a = dot + halo * (1f - dot);
                    Color c = a > 0f ? Color.Lerp(g, core, dot / a) : Color.clear;
                    c.a = a;
                    px[y * n + x] = c;
                }
            Write(path, n, n, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        // ───────────── Dưới biển (GameSea25D.dc.html) ─────────────

        static Result Sea()
        {
            GameSceneBuilder.EnsureFolder(Gen);
            // Nền: nước linear-gradient(#3FB4F0 0%, #1C86CC 30%, #0F5E99 48%) trên khung 844;
            // sàn cát từ y 300: linear-gradient(#EACD8C 0%, #F0D696 45%, #E8CA86 100%)
            var bg = VerticalBackground($"{Gen}/bg_map_sea_clean.png", 1170, 2532, y =>
            {
                float pt = y / 3f;
                if (pt < 300f) return Stops(pt / 844f, (0f, "3FB4F0"), (0.30f, "1C86CC"), (0.48f, "0F5E99"));
                return Stops((pt - 300f) / 544f, (0f, "EACD8C"), (0.45f, "F0D696"), (1f, "E8CA86"));
            });

            var fade = GameSceneBuilder.FadeSprite();
            var bubble = BubbleSprite();
            // .weed: hai đường cong bậc hai, nét 16 #123E22 rồi nét 10 gradient #6FE08E → #1E7A40
            var weedL = Weed($"{Gen}/sea_weed_left.png", "M30 160 Q14 120 30 90 Q46 60 28 30 Q20 14 30 2", 46, 130);
            var weedR = Weed($"{Gen}/sea_weed_right.png", "M30 160 Q46 120 30 90 Q14 60 32 30 Q40 14 30 2", 40, 114);

            var list = new List<AmbientElement>();
            // .ray: top −60, cao 560, trắng 22% → trong suốt, xoay 14° ↔ 24°, 6 s, pivot trên; nằm sau sàn cát
            (float left, float w, float delay)[] rays = { (40, 50, 0), (170, 36, 2), (290, 44, 3.5f) };
            foreach (var r in rays)
                list.Add(new AmbientElement
                {
                    name = "Ray", sprite = fade, color = new Color(1f, 1f, 1f, 0.22f),
                    position = new Vector2(r.left + r.w / 2f, -60f), size = new Vector2(r.w, 560f), pivot = new Vector2(0.5f, 1f),
                    clipBelowY = 300f, motion = AmbientMotion.Rotate, period = 6f, delay = r.delay, angleFrom = 14f, angleTo = 24f,
                });
            // .weed: skewX −7° ↔ 7°, 3 s, gốc ở đáy; trái (left 2, top 196, 46×130), phải (right 8, top 210, 40×114, trễ 1,2 s)
            list.Add(new AmbientElement
            {
                name = "WeedLeft", sprite = weedL, position = new Vector2(2 + 23, 196 + 130), size = new Vector2(46, 130),
                pivot = new Vector2(0.5f, 0f), motion = AmbientMotion.Skew, period = 3f, angleFrom = -7f, angleTo = 7f,
            });
            list.Add(new AmbientElement
            {
                name = "WeedRight", sprite = weedR, position = new Vector2(390 - 8 - 20, 210 + 114), size = new Vector2(40, 114),
                pivot = new Vector2(0.5f, 0f), motion = AmbientMotion.Skew, period = 3f, delay = 1.2f, angleFrom = -7f, angleTo = 7f,
            });
            // .bub: (left, top, cỡ, trễ); bay lên 520 trong 5 s, lệch +6 giữa đường rồi −4; alpha 0 → 1 (10%) → 0
            (float l, float t, float s, float d)[] bubs = { (26, 820, 14, 0), (120, 840, 9, 1.4f), (250, 830, 16, 2.6f), (340, 850, 11, 3.6f) };
            foreach (var b in bubs)
                list.Add(new AmbientElement
                {
                    name = "Bubble", sprite = bubble, color = new Color32(220, 245, 255, 255),
                    position = new Vector2(b.l + b.s / 2f, b.t + b.s / 2f), size = new Vector2(b.s, b.s),
                    motion = AmbientMotion.Rise, period = 5f, delay = b.d, midOffset = new Vector2(6, -260), endOffset = new Vector2(-4, -520), fadeInAt = 0.1f,
                });
            return new Result { background = bg, elements = list.ToArray() };
        }

        // ───────────── Trang chủ (Main25D.dc.html) ─────────────

        /// <summary>
        /// Nền Trang chủ không có mây: bg_home vẽ sẵn 2 đám mây tĩnh; trời chỉ chuyển màu theo chiều dọc
        /// nên chép màu cột giữa (x = 195 pt) đè lên vùng mây là sạch.
        /// </summary>
        internal static Sprite HomeCleanBackground()
        {
            GameSceneBuilder.EnsureFolder(Gen);
            string srcPath = $"{GameSceneBuilder.Art}/Backgrounds/bg_home.png";
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(srcPath));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            float k = w / 390f;
            // vùng mây đo trên bg_home (pt, gốc trên-trái), nới rộng để gồm cả bóng đổ mờ
            (float x0, float y0, float x1, float y1)[] clouds = { (0, 118, 128, 192), (282, 204, 390, 270) };
            int mid = Mathf.RoundToInt(195 * k);
            foreach (var c in clouds)
                for (int y = Mathf.RoundToInt(c.y0 * k); y < Mathf.RoundToInt(c.y1 * k); y++)
                {
                    int row = h - 1 - y; // hàng 0 của texture là đáy
                    var sky = px[row * w + mid];
                    for (int x = Mathf.RoundToInt(c.x0 * k); x < Mathf.Min(w, Mathf.RoundToInt(c.x1 * k)); x++) px[row * w + x] = sky;
                }
            string path = $"{Gen}/bg_home_clean.png";
            Write(path, w, h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        /// <summary>.cloud Trang chủ: (left −30, top 128, 140 × 44) và (right −24, top 214, 120 × 40); trôi −16 ↔ +20 px, 24 s / 30 s.</summary>
        internal static AmbientElement[] HomeClouds() => Clouds((-30, 128, 140, 44, 24), (390 + 24 - 120, 214, 120, 40, 30));

        /// <summary>.cloud màn Cài đặt (Guide25D): (left −30, top 118, 140 × 44) và (right −24, top 70, 110 × 36).</summary>
        internal static AmbientElement[] SettingsClouds() => Clouds((-30, 118, 140, 44, 24), (390 + 24 - 110, 70, 110, 36, 30));

        /// <summary>Mây trôi ngang −16 ↔ +20 px, ease-in-out, qua lại; (left, top, rộng, cao, chu kỳ) theo px thiết kế.</summary>
        static AmbientElement[] Clouds(params (float left, float top, float w, float h, float period)[] list)
        {
            const float pad = 14f; // chừa chỗ cho bóng đổ mờ quanh mây
            var result = new AmbientElement[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                var c = list[i];
                var sprite = Cloud($"{Gen}/home_cloud_{c.w}x{c.h}.png", c.w, c.h, pad);
                result[i] = new AmbientElement
                {
                    name = "Cloud" + i, sprite = sprite, position = new Vector2(c.left + c.w / 2f, c.top + c.h / 2f), size = new Vector2(c.w + 2 * pad, c.h + 2 * pad),
                    motion = AmbientMotion.Drift, period = c.period, angleFrom = -16f, angleTo = 20f,
                };
            }
            return result;
        }

        /// <summary>
        /// Mây bo tròn (border-radius 40 ≥ nửa chiều cao → viên thuốc) trắng, vệt trong dưới đáy
        /// (inset 0 −6px 0 rgba(160,200,230,.45)) và bóng đổ (0 6px 12px rgba(80,140,190,.2)).
        /// </summary>
        static Sprite Cloud(string path, float wPt, float hPt, float padPt)
        {
            const float k = 3f;
            int w = Mathf.RoundToInt((wPt + 2 * padPt) * k), h = Mathf.RoundToInt((hPt + 2 * padPt) * k);
            float r = hPt / 2f;
            float Sd(float x, float y, float oy) // khoảng cách có dấu tới viên thuốc (pt), toạ độ trong hộp mây, y xuống
            {
                float cx = Mathf.Clamp(x, r, wPt - r), cy = hPt / 2f + oy;
                return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r;
            }
            var px = new Color32[w * h];
            Color shade = Color.Lerp(Color.white, new Color32(160, 200, 230, 255), 0.45f);
            Color shadow = new Color32(80, 140, 190, 255);
            for (int py = 0; py < h; py++)
                for (int pxi = 0; pxi < w; pxi++)
                {
                    float x = (pxi + 0.5f) / k - padPt, y = (h - 1 - py + 0.5f) / k - padPt;
                    float d = Sd(x, y, 0f);
                    float body = Mathf.Clamp01(0.5f - d * k);
                    // bóng đổ: hình dời xuống 6, nhoè ~12
                    float sh = 0.2f * Mathf.Clamp01(0.5f - Sd(x, y, 6f) / 12f) * (1f - body);
                    // vệt trong: phần thân không bị hình dời lên 6 che
                    float band = Mathf.Clamp01(0.5f + Sd(x, y, -6f) * k);
                    Color c = Color.Lerp(Color.white, shade, band);
                    float a = body + sh;
                    Color o = a > 0f ? Color.Lerp(shadow, c, body / a) : Color.clear;
                    o.a = a;
                    px[py * w + pxi] = o;
                }
            Write(path, w, h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        // ───────────── Ảnh dùng chung ─────────────

        /// <summary>Bong bóng (.bub): vòng viền + điểm sáng ở 35%/30%; trắng để tô màu khi dùng. Dùng cả cho thẻ ở trang Chọn map.</summary>
        internal static Sprite BubbleSprite()
        {
            string path = $"{Gen}/gen_bubble.png";
            if (!File.Exists(path))
            {
                GameSceneBuilder.EnsureFolder(Gen);
                const int n = 64;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float cx = x + 0.5f - n / 2f, cy = y + 0.5f - n / 2f, r = Mathf.Sqrt(cx * cx + cy * cy);
                        float outer = Mathf.Clamp01(n / 2f - r + 0.5f), inner = Mathf.Clamp01(n / 2f - 9f - r + 0.5f);
                        float ring = (outer - inner) * 0.9f;
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(0.35f * n, 0.7f * n));
                        float glow = outer * 0.8f * Mathf.Clamp01(1f - d / (n / 2f));
                        px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Max(ring, glow) * 255f));
                    }
                Write(path, n, n, px);
            }
            return GameSceneBuilder.LoadSprite(path);
        }

        static Sprite VerticalBackground(string path, int w, int h, Func<int, Color> rowColorFromTop)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                Color32 c = rowColorFromTop(h - 1 - y); // hàng 0 của texture là đáy
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            Write(path, w, h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        static Color Stops(float t, params (float at, string hex)[] stops)
        {
            Color C(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.magenta;
            if (t <= stops[0].at) return C(stops[0].hex);
            for (int i = 1; i < stops.Length; i++)
                if (t <= stops[i].at)
                    return Color.Lerp(C(stops[i - 1].hex), C(stops[i].hex), (t - stops[i - 1].at) / (stops[i].at - stops[i - 1].at));
            return C(stops[stops.Length - 1].hex);
        }

        /// <summary>Vẽ cây rong từ path SVG (viewBox 60×160, khung wPt×hPt, preserveAspectRatio "meet").</summary>
        static Sprite Weed(string path, string d, float wPt, float hPt)
        {
            const float pxPerPt = 3f;
            int w = Mathf.RoundToInt(wPt * pxPerPt), h = Mathf.RoundToInt(hPt * pxPerPt);
            float s = Mathf.Min(wPt / 60f, hPt / 160f) * pxPerPt;            // px trên một đơn vị viewBox
            float ox = (w - 60f * s) / 2f, oy = (h - 160f * s) / 2f;
            var pts = Flatten(d);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            Color dark = new Color32(0x12, 0x3E, 0x22, 255), top = new Color32(0x6F, 0xE0, 0x8E, 255), bottom = new Color32(0x1E, 0x7A, 0x40, 255);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // toạ độ viewBox (y xuống) của tâm điểm ảnh; hàng texture 0 là đáy
                    float vx = (x + 0.5f - ox) / s, vy = (h - 1 - y + 0.5f - oy) / s;
                    float dist = Dist(pts, new Vector2(vx, vy));
                    float outer = Mathf.Clamp01((8f - dist) * s + 0.5f);  // nét 16
                    float inner = Mathf.Clamp01((5f - dist) * s + 0.5f);  // nét 10
                    if (outer <= 0f) continue;
                    Color g = Color.Lerp(top, bottom, Mathf.InverseLerp(minY, maxY, vy));
                    Color c = Color.Lerp(dark, g, inner);
                    c.a = outer;
                    px[y * w + x] = c;
                }
            Write(path, w, h, px);
            return GameSceneBuilder.LoadSprite(path);
        }

        /// <summary>Đổi path "M x y Q cx cy x y …" thành đường gấp khúc dày.</summary>
        static List<Vector2> Flatten(string d)
        {
            var n = Regex.Matches(d, "-?[\\d.]+");
            var v = new List<float>();
            foreach (Match m in n) v.Add(float.Parse(m.Value, CultureInfo.InvariantCulture));
            var pts = new List<Vector2> { new Vector2(v[0], v[1]) };
            for (int i = 2; i + 3 < v.Count; i += 4)
            {
                Vector2 p0 = pts[pts.Count - 1], c = new Vector2(v[i], v[i + 1]), p1 = new Vector2(v[i + 2], v[i + 3]);
                for (int k = 1; k <= 24; k++)
                {
                    float t = k / 24f, u = 1f - t;
                    pts.Add(u * u * p0 + 2f * u * t * c + t * t * p1);
                }
            }
            return pts;
        }

        static float Dist(List<Vector2> pts, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 a = pts[i - 1], ab = pts[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        internal static void Write(string path, int w, int h, Color32[] px)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
