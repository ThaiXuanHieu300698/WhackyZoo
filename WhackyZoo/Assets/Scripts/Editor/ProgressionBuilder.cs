using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static WhackyZoo.EditorTools.GameSceneBuilder;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Bước 5: dữ liệu nhiệm vụ / cấp độ / quà hằng ngày (Docs/GameSpec.md › Tiến trình + điểm người dùng đã chốt)
    /// và hai màn Nhiệm vụ (Missions25D.dc.html), Điểm danh tháng (DailyReward25D.dc.html).
    /// Icon xuất từ SVG của bản thiết kế, ở Art/Progress.
    /// </summary>
    internal static class ProgressionBuilder
    {
        internal const string ConfigPath = DataRoot + "/ProgressionConfig.asset";
        const string ArtDir = Art + "/Progress";

        struct MissionDef
        {
            public string id, icon, title;
            public MissionPeriod period;
            public MissionStat stat;
            public int goal, reward, points;
            public bool soon;
        }

        // GameSpec › Nhiệm vụ; 3 nhiệm vụ cũ (sao, 3 map, 12 màn) đã thay theo người dùng chốt ngày 15/11/2026.
        static readonly MissionDef[] Missions =
        {
            new MissionDef { id = "combo10", period = MissionPeriod.Daily, stat = MissionStat.BestCombo, icon = "fire", title = "Đạt combo x10 trong một ván", goal = 10, reward = 150, points = 20 },
            new MissionDef { id = "hit50", period = MissionPeriod.Daily, stat = MissionStat.CrittersHit, icon = "hammer", title = "Đập trúng 50 con thú", goal = 50, reward = 100, points = 20 },
            new MissionDef { id = "gold3", period = MissionPeriod.Daily, stat = MissionStat.BonusHit, icon = "chick", title = "Đập 3 con thưởng vàng", goal = 3, reward = 200, points = 30 },
            new MissionDef { id = "play3", period = MissionPeriod.Daily, stat = MissionStat.RoundsPlayed, icon = "play", title = "Chơi 3 ván bất kỳ", goal = 3, reward = 80, points = 20 },
            new MissionDef { id = "ad1", period = MissionPeriod.Daily, stat = MissionStat.AdsWatched, icon = "ad", title = "Xem 1 quảng cáo nhân đôi xu", goal = 1, reward = 50, points = 10, soon = true },
            new MissionDef { id = "w_play20", period = MissionPeriod.Weekly, stat = MissionStat.RoundsPlayed, icon = "star", title = "Chơi 20 ván", goal = 20, reward = 500, points = 30 },
            new MissionDef { id = "w_hit", period = MissionPeriod.Weekly, stat = MissionStat.CrittersHit, icon = "hammer", title = "Đập trúng 500 con thú", goal = 500, reward = 800, points = 30 },
            new MissionDef { id = "w_map", period = MissionPeriod.Weekly, stat = MissionStat.MapsPlayed, icon = "map", title = "Chơi đủ cả 5 map", goal = 5, reward = 600, points = 20 },
            new MissionDef { id = "w_combo", period = MissionPeriod.Weekly, stat = MissionStat.BestCombo, icon = "fire", title = "Đạt combo x25", goal = 25, reward = 1000, points = 40 },
            new MissionDef { id = "a_1000", period = MissionPeriod.Achievement, stat = MissionStat.CrittersHit, icon = "trophy", title = "Thợ đập tập sự: đập 1.000 thú", goal = 1000, reward = 1000 },
            new MissionDef { id = "a_5000", period = MissionPeriod.Achievement, stat = MissionStat.CrittersHit, icon = "trophy", title = "Thợ đập lành nghề: đập 5.000 thú", goal = 5000, reward = 3000 },
            new MissionDef { id = "a_score1000", period = MissionPeriod.Achievement, stat = MissionStat.BestScore, icon = "star", title = "Cao thủ: đạt 1.000 điểm trong một ván", goal = 1000, reward = 2000 },
            new MissionDef { id = "a_gold", period = MissionPeriod.Achievement, stat = MissionStat.BonusHit, icon = "chick", title = "Săn vàng: đập 100 con thưởng vàng", goal = 100, reward = 1500 },
        };

        internal static ProgressionConfig CreateProgression(BoosterCatalog boosters)
        {
            EnsureFolder($"{DataRoot}/Missions");
            var list = new List<MissionData>();
            foreach (var d in Missions)
            {
                var m = Upsert<MissionData>($"{DataRoot}/Missions/Mission_{d.id}.asset");
                m.id = d.id; m.period = d.period; m.stat = d.stat; m.title = d.title;
                m.icon = LoadSprite($"{ArtDir}/mission_{d.icon}.png");
                m.goal = d.goal; m.reward = d.reward; m.points = d.points; m.comingSoon = d.soon;
                EditorUtility.SetDirty(m);
                list.Add(m);
            }

            var cfg = Upsert<ProgressionConfig>(ConfigPath);
            cfg.missions = list.ToArray();
            cfg.maxMissionPoints = 100;
            cfg.chestPoints = new[] { 30, 60, 100 };
            cfg.chestRewards = new[] { 200, 500, 1200 };
            cfg.xpDivisor = 10;
            cfg.xpPerLevel = 100;
            cfg.levelUpCoins = 50;

            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        // ───────────────────────── Chung ─────────────────────────

        static Vector2 P(float cx, float cy) => CenterPos(cx, cy);
        static readonly Color Muted = Hex("5E4E3A");
        static UiArt.Stop[] Grad(params string[] hex)
        {
            var s = new UiArt.Stop[hex.Length];
            for (int i = 0; i < hex.Length; i++) s[i] = new UiArt.Stop(hex.Length == 1 ? 0f : i / (float)(hex.Length - 1), hex[i]);
            return s;
        }
        static readonly UiArt.Stop[] PanelWhite = { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "F3EEE4") };

        /// <summary>radial-gradient(ellipse at 50% cy, …) farthest-corner, PNG 390 × 844.</summary>
        static Sprite Radial(string name, float cyPt, (float t, string hex)[] stops)
        {
            const int w = 390, h = 844;
            string path = $"{Art}/Generated/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                float rx = 195f * Mathf.Sqrt(2f), ry = Mathf.Max(cyPt, h - cyPt) * Mathf.Sqrt(2f);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fx = (x + 0.5f - 195f) / rx, fy = (h - 1 - y + 0.5f - cyPt) / ry;
                        float t = Mathf.Sqrt(fx * fx + fy * fy);
                        Color c = Hex(stops[stops.Length - 1].hex);
                        for (int i = 1; i < stops.Length; i++)
                            if (t <= stops[i].t)
                            {
                                c = Color.Lerp(Hex(stops[i - 1].hex), Hex(stops[i].hex), (t - stops[i - 1].t) / (stops[i].t - stops[i - 1].t));
                                break;
                            }
                        px[y * w + x] = c;
                    }
                EnsureFolder($"{Art}/Generated");
                AmbientArtBuilder.Write(path, w, h, px);
            }
            return LoadSprite(path);
        }

        /// <summary>Viền gạch (CSS dashed) quanh hình bo góc, ruột tô <paramref name="fill"/>; ảnh @3x.</summary>
        static Sprite DashedBox(string name, float w, float h, float r, float thickness, float dash, float gap, Color stroke, Color fill)
        {
            string path = $"{Art}/Generated/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                const float K = 3f;
                int pw = Mathf.RoundToInt(w * K), ph = Mathf.RoundToInt(h * K);
                // Đường viền giữa nét, lấy mẫu dày để tra độ dài cung
                float inset = thickness / 2f, rr = Mathf.Max(0f, r - inset);
                float x0 = inset, y0 = inset, x1 = w - inset, y1 = h - inset;
                var pts = new List<Vector2>();
                void Arc(float cx, float cy, float a0)
                {
                    for (int i = 0; i <= 24; i++) { float a = (a0 + 90f * i / 24f) * Mathf.Deg2Rad; pts.Add(new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr)); }
                }
                Arc(x1 - rr, y0 + rr, -90f); Arc(x1 - rr, y1 - rr, 0f); Arc(x0 + rr, y1 - rr, 90f); Arc(x0 + rr, y0 + rr, 180f);
                pts.Add(pts[0]);
                var dense = new List<Vector2>();
                var len = new List<float>();
                float acc = 0f;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float seg = Vector2.Distance(pts[i], pts[i + 1]);
                    int n = Mathf.Max(1, Mathf.CeilToInt(seg / 0.25f));
                    for (int j = 0; j < n; j++) { dense.Add(Vector2.Lerp(pts[i], pts[i + 1], j / (float)n)); len.Add(acc + seg * j / n); }
                    acc += seg;
                }
                float period = dash + gap;
                int count = Mathf.Max(1, Mathf.RoundToInt(acc / period)); // chia đều cho khép vòng
                period = acc / count;
                float dashLen = period * dash / (dash + gap);

                // Đóng dấu nét: mỗi điểm mẫu thuộc đoạn gạch đánh dấu các điểm ảnh con (2×2) cách nó ≤ nửa nét
                const int SSN = 2;
                int sw = pw * SSN, shh = ph * SSN;
                var hit = new bool[sw * shh];
                float half = thickness / 2f;
                int rad = Mathf.CeilToInt(half * K * SSN) + 1;
                for (int i = 0; i < dense.Count; i++)
                {
                    if (len[i] % period >= dashLen) continue;
                    float sx = dense[i].x * K * SSN, sy = dense[i].y * K * SSN; // y xuống
                    int cx0 = Mathf.FloorToInt(sx), cy0 = Mathf.FloorToInt(sy);
                    for (int yy = cy0 - rad; yy <= cy0 + rad; yy++)
                    {
                        if (yy < 0 || yy >= shh) continue;
                        for (int xx = cx0 - rad; xx <= cx0 + rad; xx++)
                        {
                            if (xx < 0 || xx >= sw) continue;
                            float dx = (xx + 0.5f - sx) / (K * SSN), dy = (yy + 0.5f - sy) / (K * SSN);
                            if (dx * dx + dy * dy <= half * half) hit[yy * sw + xx] = true;
                        }
                    }
                }
                var px = new Color32[pw * ph];
                for (int py = 0; py < ph; py++)
                    for (int qx = 0; qx < pw; qx++)
                    {
                        float a = 0f, f = 0f;
                        int rowDown = ph - 1 - py; // ảnh lưu từ dưới lên, toạ độ vẽ từ trên xuống
                        for (int sj = 0; sj < SSN; sj++)
                            for (int si = 0; si < SSN; si++)
                            {
                                int xx = qx * SSN + si, yy = rowDown * SSN + sj;
                                if (hit[yy * sw + xx]) a += 1f / (SSN * SSN);
                                else if (InRound(new Vector2((xx + 0.5f) / (K * SSN), (yy + 0.5f) / (K * SSN)), w, h, r)) f += 1f / (SSN * SSN);
                            }
                        Color c = fill; c.a *= f;
                        Color s = stroke; s.a *= a;
                        float outA = s.a + c.a * (1f - s.a);
                        Color o = outA > 0f ? (s * s.a + c * c.a * (1f - s.a)) / outA : Color.clear;
                        o.a = outA;
                        px[py * pw + qx] = o;
                    }
                EnsureFolder($"{Art}/Generated");
                AmbientArtBuilder.Write(path, pw, ph, px);
            }
            return LoadSprite(path);
        }

        static bool InRound(Vector2 p, float w, float h, float r)
        {
            float cx = Mathf.Clamp(p.x, r, w - r), cy = Mathf.Clamp(p.y, r, h - r);
            return p.x >= 0 && p.y >= 0 && p.x <= w && p.y <= h && (p - new Vector2(cx, cy)).sqrMagnitude <= r * r;
        }

        /// <summary>Nút .gbtn: ảnh gồm cạnh dưới <paramref name="drop"/>; chữ ở tâm mặt nút.</summary>
        static (Button, Image) GameBtn(string name, Transform parent, string key, float w, float h, float radius, string hi, string mid, string lo, string edge,
            Vector2 centerPt, float drop = 5f, bool slice = false)
        {
            var img = Img(name, parent, UiArt.GameButton(key, w, h, radius, hi, mid, lo, edge, drop, 3f, slice), Center, Center,
                centerPt - new Vector2(0, drop / 2f), new Vector2(w, h + drop));
            if (slice) img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            img.gameObject.AddComponent<PressEffect>();
            img.gameObject.AddComponent<CanvasGroup>();
            return (b, img);
        }

        static Text Label(Transform btn, string text, float size, Color color, float drop, float dx = 0f)
            => Txt("Label", btn, text, fontHeavy, size, color, TextAnchor.MiddleCenter, Center, new Vector2(dx, drop / 2f), new Vector2(300, size + 8));

        /// <summary>Hàng tiêu đề như Cửa hàng: nút quay lại 48 + viền, tiêu đề 30 viền 5.</summary>
        static Button Header(RectTransform c, string title)
        {
            var back = Btn("BackButton", c, Spr("UI/button_white"), Center, P(43, 83) - new Vector2(0, 2.5f), new Vector2(54, 59), null, 0, Dark, null);
            Img("Icon", back.transform, Spr("UI/icon_back"), Center, Center, new Vector2(0, 2.5f), new Vector2(26, 26));
            var t = Txt("Title", c, title, fontHeavy, 30, Color.white, TextAnchor.MiddleLeft, Center, P(82 + 130, 83), new Vector2(260, 40));
            t.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));
            return back;
        }

        // ───────────────────────── Màn Nhiệm vụ ─────────────────────────

        internal static MissionsScreen BuildMissions(CanvasGroup screen, RectTransform c, ScreenRouter router, CanvasGroup mapSelect, ProgressionConfig cfg)
        {
            screen.transform.Find("Background").GetComponent<Image>().sprite =
                Radial("gen_missions_radial", 0f, new[] { (0f, "E9F7FF"), (0.55f, "A8DCFF"), (1f, "6FC0F2") });
            var view = screen.gameObject.AddComponent<MissionsScreen>();
            var back = Header(c, "NHIỆM VỤ");

            // Ô xu (như Cửa hàng)
            var coinChip = Img("CoinChip", c, UiArt.Box("shop_coin_chip", 60, 38, 19, 3, Dark, PanelWhite, 4, Hex("B9A98C"), sliceX: true),
                Center, new Vector2(1f, 0.5f), P(374, 83 + 2), new Vector2(110, 42));
            coinChip.type = Image.Type.Sliced;
            var coinRow = coinChip.gameObject.AddComponent<HorizontalLayoutGroup>();
            coinRow.padding = new RectOffset(Mathf.RoundToInt(9 * PT), Mathf.RoundToInt(15 * PT), 0, Mathf.RoundToInt(4 * PT));
            coinRow.spacing = 6 * PT;
            coinRow.childAlignment = TextAnchor.MiddleLeft;
            coinRow.childControlWidth = true;
            coinRow.childControlHeight = false;
            coinRow.childForceExpandWidth = coinRow.childForceExpandHeight = false;
            coinChip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Img("Icon", coinChip.transform, Spr("UI/icon_coin"), Center, Center, Vector2.zero, new Vector2(24, 24)).gameObject.AddComponent<LayoutElement>().preferredWidth = 24 * PT;
            var coinsText = Txt("Coins", coinChip.transform, "0", fontHeavy, 16, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(60, 24));

            // Tab (y 122–176) như Cửa hàng
            Img("TabTray", c, UiArt.Box("shop_tab_tray", 358, 54, 16, 3, Dark, Grad("E9DFCB", "F6F0E4")), Center, Center, P(195, 149), new Vector2(358, 54));
            var tabOn = UiArt.GameButton("shop_tab_on", 112, 40, 12, "FFE68A", "FFC93C", "F0A400", "B87800", 3f, 2f);
            string[] tabNames = { "Hằng ngày", "Hằng tuần", "Thành tựu" };
            var tabBgs = new Image[3];
            var tabTexts = new Text[3];
            var tabBtns = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                float cx = 16 + 3 + 4 + 56 + i * (112 + 4);
                var hit = Img($"Tab{i}", c, null, Center, Center, P(cx, 149), new Vector2(112, 40));
                hit.color = new Color(1, 1, 1, 0);
                hit.raycastTarget = true;
                tabBtns[i] = hit.gameObject.AddComponent<Button>();
                tabBtns[i].transition = Selectable.Transition.None;
                tabBgs[i] = Img("Selected", hit.transform, tabOn, Center, Center, new Vector2(0, -1.5f), new Vector2(112, 43));
                tabTexts[i] = Txt("Label", hit.transform, tabNames[i], fontHeavy, 15, Dark, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(112, 24));
            }

            // ── Khung Điểm nhiệm vụ (y 188–312,4): nền kem bo 20 viền 3, bóng 5; đệm 10/14/12 ──
            const float panelTop = 188f, panelH = 124.4f;
            Img("PointsPanel", c, UiArt.Box("mission_points_panel", 358, panelH, 20, 3, Dark, Grad("FFFBEF", "F7E9C6"), 5, Hex("B9A98C")),
                Center, new Vector2(0.5f, 1f), P(195, panelTop), new Vector2(358, panelH + 5));
            const float headY = panelTop + 3 + 10 + 13.2f;
            Img("Star", c, LoadSprite($"{ArtDir}/mission_star.png"), Center, Center, P(33 + 13, headY), new Vector2(26, 26));
            Txt("PointsTitle", c, "Điểm nhiệm vụ", fontHeavy, 16, Dark, TextAnchor.MiddleLeft, Center, P(67 + 60, headY), new Vector2(120, 24));
            var points = Txt("Points", c, "0/100", fontHeavy, 16, Hex("C46A00"), TextAnchor.MiddleLeft, Center, P(183 + 40, headY), new Vector2(80, 24));
            var reset = Txt("Reset", c, "Làm mới\n00:00:00", fontHeavy, 12, Muted, TextAnchor.MiddleRight, Center, P(357 - 45, headY), new Vector2(90, 28));
            reset.lineSpacing = 1.1f / 1.6f;

            const float areaTop = panelTop + 3 + 10 + 26.4f + 4f, areaLeft = 33f, areaW = 324f;
            var track = Img("PointsBar", c, UiArt.Box("mission_points_track", 40, 14, 7, 2, Dark, Grad("CFC5B2", "E9E1D0"), sliceX: true),
                Center, Center, P(areaLeft + areaW / 2f, areaTop + 47), new Vector2(areaW, 14)); // cách khung trái/phải đều 14 (người dùng chốt)
            track.type = Image.Type.Sliced;
            var fillArea = Rect("FillArea", track.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fillArea, 2);
            var fill = Img("Fill", fillArea, UiArt.Box("mission_points_fill", 20, 10, 5, 0, Dark, Grad("FFE68A", "FFC93C", "F0A400"), sliceX: true), Center, Center, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            Sprite LabelBg(string key, params string[] g) => UiArt.Box($"mission_chest_label_{key}", 22, 22, 11, 2, Dark, Grad(g), sliceX: true);
            var labelLocked = LabelBg("locked", "FFFFFF");
            var labelReady = LabelBg("ready", "FFE68A", "FFC93C");
            var labelOpened = LabelBg("opened", "E3DACA");
            var chestSprite = LoadSprite($"{ArtDir}/mission_chest.png");
            var chests = new MissionsScreen.ChestSlot[cfg.chestPoints.Length];
            for (int i = 0; i < chests.Length; i++)
            {
                float v = cfg.chestPoints[i];
                float cx = areaLeft + v / 100f * areaW - (v >= 100 ? 44 : 22) + 22;
                var hit = Img($"Chest{i}", c, null, Center, Center, P(cx, areaTop + 34), new Vector2(54, 68));
                hit.color = new Color(1, 1, 1, 0);
                hit.raycastTarget = true;
                var btn = hit.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                // Gốc xoay 50% 90% của icon 44
                var icon = Img("Icon", hit.transform, chestSprite, Center, new Vector2(0.5f, 0.1f), new Vector2(0, 34 - 22 - 0.4f * 44), new Vector2(44, 44));
                var label = PillLabel("Label", hit.transform, labelLocked, v.ToString(), 12, Dark, 22, 6);
                ((RectTransform)label.transform.parent).anchoredPosition = new Vector2(0, (34 - 61) * PT);
                chests[i] = new MissionsScreen.ChestSlot { button = btn, icon = icon.rectTransform, iconImage = icon, labelBackground = label.transform.parent.GetComponent<Image>(), label = label };
            }

            // ── Danh sách (từ y 324,4 tới đáy cách 20): khung nhìn rộng 374 (lề âm 8), chỉ mờ mép dưới 28 ──
            const float listTop = panelTop + panelH + 12f, listBottom = 844f - 20f;
            var viewport = Rect("MissionList", c, Center, Center, P(195, (listTop + listBottom) / 2f), new Vector2(374, listBottom - listTop));
            viewport.gameObject.AddComponent<Image>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            var fadeRt = Rect("BottomFade", viewport, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fadeRt, 0);
            var fadeMask = fadeRt.gameObject.AddComponent<RectMask2D>();
            int fadeUnits = Mathf.RoundToInt(28 * PT);
            fadeMask.softness = new Vector2Int(0, fadeUnits);
            fadeMask.padding = new Vector4(0, 0, 0, -fadeUnits);
            var content = Rect("Content", fadeRt, Top, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(374, 300));
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(Mathf.RoundToInt(8 * PT), Mathf.RoundToInt(8 * PT), Mathf.RoundToInt(2 * PT), Mathf.RoundToInt(12 * PT));
            vl.spacing = 10 * PT;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            int maxRows = 0;
            foreach (MissionPeriod p in System.Enum.GetValues(typeof(MissionPeriod)))
            {
                int n = 0;
                foreach (var m in cfg.missions) if (m.period == p) n++;
                maxRows = Mathf.Max(maxRows, n);
            }
            var rows = new MissionRow[maxRows];
            for (int i = 0; i < maxRows; i++) rows[i] = BuildRow(content, i);

            Set(view, "router", router);
            Set(view, "mapSelectScreen", mapSelect);
            Set(view, "config", cfg);
            Set(view, "backButton", back);
            Set(view, "coinsText", coinsText);
            Set(view, "coinChip", coinChip.rectTransform);
            SetArray(view, "tabBackgrounds", tabBgs);
            SetArray(view, "tabLabels", tabTexts);
            SetArray(view, "tabButtons", tabBtns);
            Set(view, "pointsText", points);
            Set(view, "resetText", reset);
            Set(view, "pointsFill", fill.rectTransform);
            var so = new SerializedObject(view);
            var cp = so.FindProperty("chests");
            cp.arraySize = chests.Length;
            for (int i = 0; i < chests.Length; i++)
            {
                var e = cp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("button").objectReferenceValue = chests[i].button;
                e.FindPropertyRelative("icon").objectReferenceValue = chests[i].icon;
                e.FindPropertyRelative("iconImage").objectReferenceValue = chests[i].iconImage;
                e.FindPropertyRelative("labelBackground").objectReferenceValue = chests[i].labelBackground;
                e.FindPropertyRelative("label").objectReferenceValue = chests[i].label;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(view, "chestLabelLocked", labelLocked);
            Set(view, "chestLabelReady", labelReady);
            Set(view, "chestLabelOpened", labelOpened);
            SetArray(view, "rows", rows);
            Set(view, "scroll", scroll);
            Set(view, "listContent", content);

            // ── Bong bóng phần thưởng rương: khung kem 124 × 48 (viền 3, bo 14, bóng 3), mũi nhọn chỉ xuống đỉnh rương;
            //    dòng trên 12 "Cần 30 điểm", dòng dưới xu 18 + "200 xu" 16. Vẽ trên cùng (sau danh sách). ──
            const float tipW = 124, tipH = 48;
            float tipCy = areaTop - 2 - 7 - tipH / 2f;
            var tip = Rect("ChestTip", c, Center, Center, P(195, tipCy), new Vector2(tipW, tipH));
            var tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
            tipGroup.blocksRaycasts = false;
            var tipFill = Grad("FFFFFF", "FFF3D0");
            var arrow = Img("Arrow", tip, UiArt.Box("mission_tip_arrow", 14, 14, 2, 3, Dark, Grad("FFF3D0")), Center, Center, new Vector2(0, -tipH / 2f), new Vector2(14, 14));
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            Img("Background", tip, UiArt.Box("mission_tip", tipW, tipH, 14, 3, Dark, tipFill, 3, Hex("B9A98C")), Center, Center, new Vector2(0, -1.5f), new Vector2(tipW, tipH + 3));
            var tipTitle = Txt("Title", tip, "Cần 30 điểm", fontBold, 12, Muted, TextAnchor.MiddleCenter, Center, new Vector2(0, 10), new Vector2(tipW - 8, 16));
            var amountRow = Rect("AmountRow", tip, Center, Center, new Vector2(0, -8), new Vector2(tipW - 8, 22));
            var arl = amountRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            arl.spacing = 4 * PT;
            arl.childAlignment = TextAnchor.MiddleCenter;
            arl.childControlWidth = true;
            arl.childControlHeight = false;
            arl.childForceExpandWidth = arl.childForceExpandHeight = false;
            Img("Coin", amountRow, Spr("UI/icon_coin"), Center, Center, Vector2.zero, new Vector2(18, 18)).gameObject.AddComponent<LayoutElement>().preferredWidth = 18 * PT;
            var tipAmount = Txt("Amount", amountRow, "200 xu", fontHeavy, 16, Hex("8A5A00"), TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(80, 22));
            tip.gameObject.SetActive(false);
            Set(view, "tipRoot", tip);
            Set(view, "tipGroup", tipGroup);
            Set(view, "tipArrow", arrow.rectTransform);
            Set(view, "tipTitle", tipTitle);
            Set(view, "tipAmount", tipAmount);
            SetFloat(view, "tipMaxOffset", 195 - 16 - tipW / 2f);
            return view;
        }

        /// <summary>
        /// Dòng nhiệm vụ rộng 358: .panel bo 18, đệm 8/10, cách 10; ô icon 52 + viền = 58; khối chữ rộng 176;
        /// nút 72 × 44 (Nhận) / 78 × 50 (Chơi, Xong) ở mép phải.
        /// </summary>
        static MissionRow BuildRow(RectTransform content, int index)
        {
            var row = Rect($"Row{index}", content, Top, Center, Vector2.zero, new Vector2(358, 81));
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 81 * PT;
            var visual = Rect("Visual", row, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(visual, 0);
            var group = visual.gameObject.AddComponent<CanvasGroup>();

            var normal = UiArt.Box("mission_row", 358, 81, 18, 3, Dark, PanelWhite, 4, Hex("B9A98C"), sliceX: true, sliceY: true);
            var ready = UiArt.Box("mission_row_ready", 358, 81, 18, 3, Dark, Grad("FFFBE0", "FFEBA8"), 4, Hex("C99A2E"), sliceX: true, sliceY: true);
            var bg = Img("Background", visual, normal, Center, Center, Vector2.zero, Vector2.zero);
            bg.type = Image.Type.Sliced;
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = new Vector2(0, -4 * PT); // cạnh bóng 4 dưới đáy
            bg.rectTransform.offsetMax = Vector2.zero;

            var iconBox = Img("IconBox", visual, UiArt.Box("mission_icon_box", 58, 58, 14, 3, Dark, Grad("FFFFFF", "E9F4FB")), Center, Center, new Vector2(-137, 0), new Vector2(58, 58));
            var icon = Img("Icon", iconBox.transform, null, Center, Center, Vector2.zero, new Vector2(36, 36));
            icon.preserveAspect = true;

            var title = Txt("Title", visual, "Nhiệm vụ", fontHeavy, 15, Dark, TextAnchor.UpperLeft, Center, new Vector2(-10, 20), new Vector2(176, 17.25f));
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.lineSpacing = 1.15f / 1.6f;

            var barRow = Rect("BarRow", visual, Center, Center, new Vector2(-10, 0), new Vector2(176, 16));
            var bar = Img("Bar", barRow, UiArt.Box("mission_bar_track", 30, 14, 7, 2, Dark, Grad("CFC5B2", "E9E1D0"), sliceX: true), Center, Center, new Vector2(-32, 0), new Vector2(112, 14)); // thanh ngắn lại (thiết kế 130) để số tiến độ cách nút Chơi
            bar.type = Image.Type.Sliced;
            var barArea = Rect("FillArea", bar.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(barArea, 2);
            var fill = Img("Fill", barArea, null, Center, Center, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var fillBlue = UiArt.Box("mission_fill_blue", 12, 10, 4, 0, Dark, Grad("A8DCFF", "3FA9F5", "1F7FC4"), sliceX: true);
            var fillGreen = UiArt.Box("mission_fill_green", 12, 10, 4, 0, Dark, Grad("A6F09A", "4CC25E", "2E9E44"), sliceX: true);
            fill.sprite = fillBlue;
            var progress = Txt("Progress", barRow, "0/0", fontHeavy, 12, Muted, TextAnchor.MiddleRight, Center, new Vector2(55, 0), new Vector2(54, 16)); // mép phải cách mép khối chữ 6 → cách nút Chơi 16

            var rewardRow = Rect("RewardRow", visual, Center, Center, new Vector2(-10, -20), new Vector2(176, 18));
            Img("Coin", rewardRow, Spr("UI/icon_coin"), Center, Center, new Vector2(-80, 0), new Vector2(16, 16));
            var reward = Txt("Reward", rewardRow, "+0", fontHeavy, 13, Hex("8A5A00"), TextAnchor.MiddleLeft, Center, new Vector2(10, 0), new Vector2(156, 18));
            reward.supportRichText = true;

            var (claim, _) = GameBtn("Claim", visual, "mission_btn_claim", 72, 44, 14, "FFE68A", "FFC93C", "F0A400", "B87800", new Vector2(130, 0));
            Label(claim.transform, "Nhận", 16, Dark, 5);
            var (play, _) = GameBtn("Play", visual, "mission_btn_play", 78, 50, 14, "B8F5B0", "5BD06A", "2FA444", "1F7A33", new Vector2(127, 0));
            Label(play.transform, "Chơi", 16, Dark, 5);

            var done = Img("Done", visual, DashedBox("gen_mission_done", 78, 50, 14, 3, 9, 6, Hex("2E9E44"), new Color(0, 0, 0, 0)), Center, Center, new Vector2(127, 0), new Vector2(78, 50));
            Img("Check", done.transform, LoadSprite($"{ArtDir}/mission_check.png"), Center, Center, new Vector2(-17, 0), new Vector2(18, 18));
            Txt("Label", done.transform, "Xong", fontHeavy, 14, Hex("1F6B2E"), TextAnchor.MiddleCenter, Center, new Vector2(9, 0), new Vector2(40, 20));
            var soon = Img("Soon", visual, UiArt.Box("mission_soon", 78, 50, 14, 3, Hex("A8977A"), Grad("F3EEE4", "E3DACA")), Center, Center, new Vector2(127, 0), new Vector2(78, 50));
            Txt("Label", soon.transform, "Sắp có", fontHeavy, 14, Hex("6B5A45"), TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(70, 20));
            foreach (var go in new[] { claim.gameObject, play.gameObject, done.gameObject, soon.gameObject }) go.SetActive(false);

            var view = row.gameObject.AddComponent<MissionRow>();
            Set(view, "layout", layout);
            Set(view, "visual", visual);
            Set(view, "group", group);
            Set(view, "background", bg);
            Set(view, "normalBackground", normal);
            Set(view, "readyBackground", ready);
            Set(view, "icon", icon);
            Set(view, "title", title);
            Set(view, "titleRect", title.rectTransform);
            Set(view, "barRow", barRow);
            Set(view, "barFill", fill.rectTransform);
            Set(view, "barFillImage", fill);
            Set(view, "fillBlue", fillBlue);
            Set(view, "fillGreen", fillGreen);
            Set(view, "progress", progress);
            Set(view, "rewardRow", rewardRow);
            Set(view, "reward", reward);
            Set(view, "claimButton", claim);
            Set(view, "playButton", play);
            Set(view, "doneMark", done.gameObject);
            Set(view, "soonMark", soon.gameObject);
            return view;
        }

        // ───────────────────────── Điểm danh tháng ─────────────────────────

        internal const string MonthPath = DataRoot + "/DailyRewardMonth.asset";

        /// <summary>Bảng quà 30 ngày (Docs/GameSpec.md › Điểm danh tháng; nhãn ô theo DailyReward25D.dc.html).</summary>
        internal static DailyRewardMonth CreateDailyMonth(BoosterCatalog boosters, ShopCatalog shop)
        {
            var coin = Spr("UI/icon_coin");
            var chestIcon = Spr("UI/icon_chest");
            BoosterData B(string id) => boosters != null ? boosters.Find(id) : null;
            PetData pet = null;
            ToolData tool = null;
            if (shop != null)
            {
                foreach (var p in shop.pets) if (p != null && p.id == "capy") pet = p;
                tool = shop.FindTool("bbat");
            }
            var capyFace = System.IO.File.Exists($"{Art}/Characters/InHole/pet_capy.png") ? LoadSprite($"{Art}/Characters/InHole/pet_capy.png") : pet != null ? pet.icon : null;

            DailyGift Coins(int n) => new DailyGift { kind = DailyGiftKind.Coins, amount = n };
            DailyGift Item(string id, int n) => new DailyGift { kind = DailyGiftKind.Booster, id = id, booster = B(id), amount = n };
            DailyGift[] AllItems(int n) => new[] { Item("time", n), Item("x2", n), Item("shield", n), Item("freeze", n), Item("fire", n) };
            DailyDay C(int n) => new DailyDay { gift = Coins(n), label = UiUtil.Thousands(n) + " xu", fullName = UiUtil.Thousands(n) + " xu", icon = coin };
            DailyDay I(string id, int n, string name) => new DailyDay { gift = Item(id, n), label = "×" + n, fullName = $"{name} x{n}", icon = B(id) != null ? B(id).icon : null };
            DailyDay Chest(bool milestone, params DailyGift[] content) => new DailyDay { label = "Bí ẩn", fullName = "Rương bí ẩn", icon = chestIcon, milestone = milestone, mystery = true, chest = content };

            var cfg = Upsert<DailyRewardMonth>(MonthPath);
            cfg.makeUpsPerMonth = 3;
            cfg.days = new[]
            {
                C(100), I("time", 2, "+5 Giây"), C(150), I("x2", 1, "Nhân Đôi"), C(200), I("shield", 2, "Khiên"),
                Chest(true, Coins(1000)), C(150), I("freeze", 2, "Đóng Băng"), C(200), I("fire", 1, "Bùa Combo"), C(250),
                I("time", 3, "+5 Giây"),
                new DailyDay { gift = new DailyGift { kind = DailyGiftKind.Pet, id = "capy", pet = pet, amount = 1 }, label = "Capybara", fullName = "Capybara",
                    icon = capyFace, milestone = true, allowDouble = false },
                C(200), I("x2", 2, "Nhân Đôi"), C(250), I("shield", 3, "Khiên"),
                C(300), I("freeze", 3, "Đóng Băng"), Chest(true, AllItems(2)), C(250), I("fire", 2, "Bùa Combo"), C(300),
                I("time", 5, "+5 Giây"), C(400), I("x2", 3, "Nhân Đôi"), C(500), Chest(false, AllItems(2)),
                new DailyDay { gift = new DailyGift { kind = DailyGiftKind.ToolTrial, id = "bbat", tool = tool, amount = 7 }, label = "7 ngày", fullName = "Gậy Bóng Chày (7 ngày)",
                    icon = tool != null ? tool.icon : null, milestone = true, allowDouble = false },
            };
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        /// <summary>Khối bo góc tô radial-gradient(circle at 40% 35%, …) + viền + bóng cứng (ô icon tím của thẻ quà ngày 30).</summary>
        static Sprite RadialBox(string name, float w, float h, float r, float border, (float t, string hex)[] stops, float drop, Color dropColor)
        {
            string path = $"{Art}/Generated/UI/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                const float K = 3f;
                int pw = Mathf.RoundToInt(w * K), ph = Mathf.RoundToInt((h + drop) * K);
                var px = new Color32[pw * ph];
                float cx = 0.4f * w, cy = 0.35f * h;
                // farthest-corner: bán kính = khoảng cách từ tâm tới góc xa nhất
                float R = Mathf.Sqrt(Mathf.Pow(Mathf.Max(cx, w - cx), 2) + Mathf.Pow(Mathf.Max(cy, h - cy), 2));
                for (int py = 0; py < ph; py++)
                    for (int x = 0; x < pw; x++)
                    {
                        float fx = (x + 0.5f) / K, fy = (ph - 1 - py + 0.5f) / K; // fy từ mép trên
                        Color o = Color.clear;
                        float dDrop = SdRound(fx - w / 2f, fy - drop - h / 2f, w / 2f, h / 2f, r);
                        if (drop > 0f && dDrop < 0.5f) { var dc = dropColor; dc.a = Mathf.Clamp01(0.5f - dDrop * K); o = dc; }
                        float d = SdRound(fx - w / 2f, fy - h / 2f, w / 2f, h / 2f, r);
                        float cover = Mathf.Clamp01(0.5f - d * K);
                        if (cover > 0f)
                        {
                            float tt = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy)) / R;
                            Color c = Hex(stops[stops.Length - 1].hex);
                            for (int i = 1; i < stops.Length; i++)
                                if (tt <= stops[i].t) { c = Color.Lerp(Hex(stops[i - 1].hex), Hex(stops[i].hex), (tt - stops[i - 1].t) / (stops[i].t - stops[i - 1].t)); break; }
                            float b = Mathf.Clamp01(0.5f - (d + border) * K); // 1 = ruột, 0 = vành
                            c = Color.Lerp(Dark, c, b);
                            c.a = cover;
                            o = Color.Lerp(o, c, cover);
                            o.a = Mathf.Max(o.a, cover);
                        }
                        px[py * pw + x] = o;
                    }
                EnsureFolder($"{Art}/Generated/UI");
                AmbientArtBuilder.Write(path, pw, ph, px);
            }
            return LoadSprite(path);
        }

        static float SdRound(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - (hx - r), qy = Mathf.Abs(y) - (hy - r);
            return Mathf.Min(Mathf.Max(qx, qy), 0f) + new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude - r;
        }

        /// <summary>Quầng vàng .glow: box-shadow 0 0 14px 4px rgba(255,225,74,.95) quanh ô w × h bo r (ruột trong suốt).</summary>
        static Sprite Halo(string name, float w, float h, float r, float pad)
        {
            string path = $"{Art}/Generated/UI/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                const float K = 3f;
                int pw = Mathf.RoundToInt((w + 2 * pad) * K), ph = Mathf.RoundToInt((h + 2 * pad) * K);
                var px = new Color32[pw * ph];
                const float spread = 4f, sigma = 7f;
                for (int py = 0; py < ph; py++)
                    for (int x = 0; x < pw; x++)
                    {
                        float fx = (x + 0.5f) / K - (w / 2f + pad), fy = (py + 0.5f) / K - (h / 2f + pad);
                        float d = SdRound(fx, fy, w / 2f, h / 2f, r);
                        float a = d < 0f ? 0f : 0.95f / (1f + Mathf.Exp((d - spread) / (sigma * 0.6f)));
                        px[py * pw + x] = new Color(1f, 225f / 255f, 74f / 255f, a);
                    }
                EnsureFolder($"{Art}/Generated/UI");
                AmbientArtBuilder.Write(path, pw, ph, px);
            }
            return LoadSprite(path);
        }

        static Sprite Cell(string key, string a, string b, string edge) =>
            UiArt.Box($"dm_cell_{key}", 40, 40, 14, 2.5f, Dark, Grad(a, b), 3, Hex(edge), sliceX: true, sliceY: true);

        static Sprite Dot(string key, string a, string b) => UiArt.Box($"dm_dot_{key}", 26, 26, 13, 2.5f, Dark, Grad(a, b));

        /// <summary>
        /// Màn Điểm danh tháng theo DailyReward25D.dc.html (đo lại trên 09b_qua_hang_ngay.png): khung 390 × 844, đệm 52/16/20, cách 10.
        /// Hàng tiêu đề y 52–106; thẻ quà ngày 30 y 116–259; lịch y 269–680; nút NHẬN mặt y 706–766, quảng cáo 776–824.
        /// </summary>
        internal static DailyRewardScreen BuildDaily(CanvasGroup screen, RectTransform c, ScreenRouter router, DailyRewardMonth month)
        {
            screen.transform.Find("Background").GetComponent<Image>().sprite =
                Radial("gen_daily_radial", 211f, new[] { (0f, "FFD7A8"), (0.5f, "FF9E7A"), (1f, "E0605A") });
            var view = screen.gameObject.AddComponent<DailyRewardScreen>();

            // Tia nắng 760 × 760 (tâm 195, 180), trắng 20%, quay 30 s — nằm dưới nội dung
            var rays = Img("Rays", screen.transform, LoadSprite($"{ArtDir}/daily_rays.png"), Center, Center, P(195, 180), new Vector2(760, 760));
            rays.color = new Color(1, 1, 1, 0.2f);
            rays.transform.SetSiblingIndex(1);

            var back = Header(c, "ĐIỂM DANH");

            // ── Thẻ quà ngày 30: vàng #FFF3B0 → #FFC93C 70% → #F0A400, viền 3, bo 22, bóng 0 5px #B87800 + 0 12px 18px .22; đệm 10/12/12 ──
            const float cardTop = 116f, cardH = 143f;
            var cardSoft = Img("PrizeSoftShadow", c, UiArt.SoftShadow("dm_prize_soft", 358, cardH, 22, 18, 0.22f), Center, Center, P(195, cardTop + cardH / 2f + 12), new Vector2(358 + 36, cardH + 36));
            var card = Img("PrizeCard", c, UiArt.Box("dm_prize", 358, cardH, 22, 3, Dark,
                    new[] { new UiArt.Stop(0f, "FFF3B0"), new UiArt.Stop(0.7f, "FFC93C"), new UiArt.Stop(1f, "F0A400") }, 5, Hex("B87800")),
                Center, new Vector2(0.5f, 1f), P(195, cardTop), new Vector2(358, cardH + 5));
            // .shine: vệt sáng rộng 40 nghiêng −20° quét qua thẻ (chỉ trong mặt thẻ)
            var shineMask = Img("ShineMask", card.transform, UiArt.Box("dm_prize_mask", 358, cardH, 22, 0, Dark, Grad("FFFFFF")), Center, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(358, cardH));
            shineMask.rectTransform.anchorMin = shineMask.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shineMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var shine = Img("Shine", shineMask.transform, ShineBand(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-60, 0), new Vector2(40, cardH * 1.4f));
            shine.rectTransform.localRotation = Quaternion.Euler(0, 0, 20f);

            // Ô icon 82 (76 + viền): radial #F3E8FF → #B07CF0 70% → #7A44C8, bo 18, bóng 0 4px #5A2AA0; gậy 62; nảy 1,6 s
            float iconCy = cardTop + 3 + 10 + 115.2f / 2f;
            var iconBox = Img("PrizeIcon", c, RadialBox("dm_prize_icon", 82, 82, 18, 3, new[] { (0f, "F3E8FF"), (0.7f, "B07CF0"), (1f, "7A44C8") }, 4, Hex("5A2AA0")),
                Center, Center, P(16 + 3 + 12 + 41, iconCy + 2), new Vector2(82, 86));
            var bat = Img("Bat", iconBox.transform, month.Day(month.Count)?.icon, Center, Center, new Vector2(0, 2), new Vector2(62, 62));
            bat.preserveAspect = true;

            // Cột chữ (x 123): "Quà ngày 30" 12 #6B4A00, tên 20, mô tả 11,5 #5E3E00, thanh tiến độ, "Đã điểm danh N/30 ngày" 12
            const float tx = 123f, colW = 236f;
            Txt("PrizeKicker", c, "Quà ngày 30", fontHeavy, 12, Hex("6B4A00"), TextAnchor.MiddleLeft, Center, P(tx + colW / 2f, 138.6f), new Vector2(colW, 18));
            Txt("PrizeName", c, "Gậy Bóng Chày", fontHeavy, 20, Dark, TextAnchor.MiddleLeft, Center, P(tx + colW / 2f, 162.2f), new Vector2(colW, 26));
            Txt("PrizeDesc", c, "Dùng miễn phí 7 ngày · +20% xu mỗi ván", fontBold, 11.5f, Hex("5E3E00"), TextAnchor.MiddleLeft, Center, P(tx + colW / 2f, 182.8f), new Vector2(colW, 16));

            // Thanh: x 125–353, cao 12 (viền 2, bo 6, nền #CFC5B2 → #E9E1D0); phần đầy vàng bo 5; 4 mốc tròn 26 ở 23,33% · 46,67% · 70% · cuối −6
            const float barL = 125f, barR = 353f, barY = 208.4f, barW = barR - barL;
            var track = Img("ProgressTrack", c, UiArt.Box("dm_track", 24, 12, 6, 2, Dark, Grad("CFC5B2", "E9E1D0"), sliceX: true), Center, Center, P((barL + barR) / 2f, barY), new Vector2(barW, 12));
            track.type = Image.Type.Sliced;
            var fillArea = Rect("FillArea", track.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fillArea, 2);
            var fill = Img("Fill", fillArea, UiArt.Box("dm_fill", 16, 8, 4, 0, Dark, new[] { new UiArt.Stop(0f, "FFE68A"), new UiArt.Stop(0.55f, "FFC93C"), new UiArt.Stop(1f, "F0A400") }, sliceX: true),
                Center, Center, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            int[] mDays = { 7, 14, 21, 30 };
            float[] mX = { barL + 0.2333f * barW, barL + 0.4667f * barW, barL + 0.7f * barW, barR - 6f };
            var dotDone = Dot("done", "A6F09A", "4CC25E");
            var dotGrand = Dot("grand", "E3CCFF", "B07CF0");
            var dotNormal = Dot("normal", "FFFFFF", "F1EADC");
            var dots = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                dots[i] = Img($"Milestone{mDays[i]}", c, dotNormal, Center, Center, P(mX[i], barY), new Vector2(26, 26));
                var ic = Img("Icon", dots[i].transform, month.Day(mDays[i])?.icon, Center, Center, Vector2.zero, new Vector2(18, 18));
                ic.preserveAspect = true;
            }
            var count = Txt("Count", c, "Đã điểm danh 0/30 ngày", fontHeavy, 12, Dark, TextAnchor.MiddleLeft, Center, P(tx + colW / 2f, 235f), new Vector2(colW, 18));

            // ── Lịch: nền #FFFBEF → #F7E9C6, viền 3, bo 24, bóng 0 6px #B9A98C + 0 18px 26px .25; đệm 14/10/10; lưới 6 cột cách 12 × 6 ──
            const float calTop = 269f, calH = 411f;
            Img("CalendarSoftShadow", c, UiArt.SoftShadow("dm_cal_soft", 358, calH, 24, 26, 0.25f), Center, Center, P(195, calTop + calH / 2f + 18), new Vector2(358 + 52, calH + 52));
            Img("Calendar", c, UiArt.Box("dm_calendar", 358, calH, 24, 3, Dark, Grad("FFFBEF", "F7E9C6"), 6, Hex("B9A98C")),
                Center, new Vector2(0.5f, 1f), P(195, calTop), new Vector2(358, calH + 6));

            var normal = Cell("normal", "FFFFFF", "F1EADC", "B9A98C");
            var purple = Cell("purple", "F3E8FF", "D8BEFA", "8A62C4");
            var gold = Cell("gold", "FFF3B0", "FFC93C", "B87800");
            var today = Cell("today", "FFF6C8", "FFD84A", "B87800");
            var claimed = Cell("claimed", "F1FBEA", "CFEFC4", "8FBF7E");
            const float cellW = (332f - 5 * 6f) / 6f, cellH = 62f;
            var halo = Halo("dm_cell_glow", cellW, cellH, 14, 22);
            var tickBg = UiArt.Box("dm_tick", 20, 20, 10, 2, Dark, Grad("2E9E44"), 2, Hex("1F6B2E"));
            var tickMark = UiArt.Icon("dm_tick_mark", 12, new UiArt.IconShape { points = new[] { new Vector2(5, 12.5f), new Vector2(9.5f, 17), new Vector2(19, 7) }, stroke = Color.white, strokeWidth = 3.5f });
            var tagSprite = UiArt.Box("dm_today_tag", 17, 17, 8.5f, 2, Dark, Grad("FF8A80", "E53935"), sliceX: true);
            var missSprite = UiArt.Box("dm_miss", 30, 30, 11, 0, Dark, Grad("50463C"), sliceX: true, sliceY: true); // phủ rgba(80,70,60,.45): màu ảnh đặt alpha .45
            var buSprite = UiArt.Box("dm_makeup", 19, 19, 9.5f, 2, Dark, Grad("8FD3FF", "3FA9F5"), sliceX: true);

            var cells = new DailyRewardScreen.DayCell[month.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                int col = i % 6, row = i / 6;
                float cx = 29 + cellW / 2f + col * (cellW + 6), cy = calTop + 3 + 14 + cellH / 2f + row * (cellH + 12);
                cells[i] = DayCellUi(c, i + 1, month.Day(i + 1), P(cx, cy), cellW, cellH, normal, halo, tickBg, tickMark, tagSprite, missSprite, buSprite);
            }
            var makeUp = Txt("MakeUpNote", c, "Lỡ ngày? Xem quảng cáo để điểm danh bù · còn 3/3", fontBold, 11.5f, Muted, TextAnchor.MiddleCenter, Center, P(195, 659.5f), new Vector2(340, 16));

            // ── Nút: NHẬN (mặt cao 60, bo 22, chữ 26 trắng viền 4) + xem quảng cáo x2 (cao 48, bo 18, chữ 16) ──
            var claimGroup = Rect("ClaimGroup", c, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(claimGroup, 0);
            var (claim, _) = GameBtn("Claim", claimGroup, "dm_btn_claim", 358, 60, 22, "FFE68A", "FFC93C", "F0A400", "B87800", P(195, 736), 5, true);
            var claimLabel = Label(claim.transform, "NHẬN", 26, Color.white, 5);
            claimLabel.gameObject.AddComponent<TextStroke>().Configure(Dark, 2f * PT, new Vector2(0, -3 * PT));
            var (ad, _) = GameBtn("Ad", claimGroup, "dm_btn_ad", 358, 48, 18, "D8F1FF", "8FD3FF", "4FB0EE", "2A7FBF", P(195, 800), 5, true);
            Img("Icon", ad.transform, LoadSprite($"{ArtDir}/daily_ad.png"), Center, Center, new Vector2(-98, 2.5f), new Vector2(24, 24));
            Label(ad.transform, "Xem quảng cáo nhận x2", 16, Dark, 5, 16);

            // Đã nhận: khung gạch trắng (bo 20, nền trắng 18%) "Đã nhận …!" 19 + "Điểm danh tiếp sau HH:MM:SS" 14 + nút Về trang chủ (cao 52)
            var claimedGroup = Rect("ClaimedGroup", c, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(claimedGroup, 0);
            Img("Status", claimedGroup, DashedBox("gen_dm_status", 358, 72, 20, 3, 9, 6, Color.white, new Color(1, 1, 1, 0.18f)), Center, Center, P(195, 726), new Vector2(358, 72));
            var got = Txt("Got", claimedGroup, "Đã nhận 100 xu!", fontHeavy, 19, Color.white, TextAnchor.MiddleCenter, Center, P(195, 712), new Vector2(340, 28));
            TextShadow(got, 2, 0.35f);
            var next = Txt("Next", claimedGroup, "Điểm danh tiếp sau 00:00:00", fontHeavy, 14, Color.white, TextAnchor.MiddleCenter, Center, P(195, 741), new Vector2(340, 22));
            TextShadow(next, 2, 0.35f);
            var (home, _) = GameBtn("Home", claimedGroup, "dm_btn_home", 358, 52, 20, "FFE68A", "FFC93C", "F0A400", "B87800", P(195, 798), 5, true);
            Label(home.transform, "Về trang chủ", 19, Dark, 5);
            claimedGroup.gameObject.SetActive(false);

            var popup = BuildChestPopup(screen, c, view);

            Set(view, "router", router);
            Set(view, "month", month);
            Set(view, "backButton", back);
            Set(view, "prizeIcon", iconBox.rectTransform);
            Set(view, "progressFill", fill.rectTransform);
            SetArray(view, "milestones", dots);
            Set(view, "milestoneDone", dotDone);
            Set(view, "milestoneGrand", dotGrand);
            Set(view, "milestoneNormal", dotNormal);
            Set(view, "countText", count);
            Set(view, "prizeShine", shine.rectTransform);
            var so = new SerializedObject(view);
            so.FindProperty("prizeShineTravel").floatValue = 358f * 1.1f + 60f;
            var dp = so.FindProperty("days");
            dp.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                var e = dp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("root").objectReferenceValue = cells[i].root;
                e.FindPropertyRelative("group").objectReferenceValue = cells[i].group;
                e.FindPropertyRelative("background").objectReferenceValue = cells[i].background;
                e.FindPropertyRelative("glow").objectReferenceValue = cells[i].glow;
                e.FindPropertyRelative("icon").objectReferenceValue = cells[i].icon;
                e.FindPropertyRelative("label").objectReferenceValue = cells[i].label;
                e.FindPropertyRelative("tick").objectReferenceValue = cells[i].tick;
                e.FindPropertyRelative("todayTag").objectReferenceValue = cells[i].todayTag;
                e.FindPropertyRelative("missed").objectReferenceValue = cells[i].missed;
                e.FindPropertyRelative("makeUpButton").objectReferenceValue = cells[i].makeUpButton;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(view, "cellNormal", normal);
            Set(view, "cellPurple", purple);
            Set(view, "cellGold", gold);
            Set(view, "cellToday", today);
            Set(view, "cellClaimed", claimed);
            Set(view, "makeUpText", makeUp);
            Set(view, "claimGroup", claimGroup.gameObject);
            Set(view, "claimButton", claim);
            Set(view, "adButton", ad);
            Set(view, "claimedGroup", claimedGroup.gameObject);
            Set(view, "gotText", got);
            Set(view, "nextText", next);
            Set(view, "homeButton", home);
            Set(view, "rays", rays.rectTransform);
            Set(view, "coinIcon", Spr("UI/icon_coin"));
            return view;
        }

        static Sprite ShineBand()
        {
            string path = $"{Art}/Generated/UI/dm_shine.png";
            const int w = 40, h = 8;
            if (!System.IO.File.Exists(path))
            {
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        px[y * w + x] = new Color(1f, 1f, 1f, 0.75f * (1f - Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f));
                EnsureFolder($"{Art}/Generated/UI");
                AmbientArtBuilder.Write(path, w, h, px);
            }
            return LoadSprite(path);
        }

        /// <summary>
        /// Một ô lịch (.cell 50,3 × 62): viền 2,5 bo 14, bóng 0 3px; số ngày 11 ở trên, icon 28 (ô mốc 32) giữa, nhãn 10,5 (mốc 9,5) dưới.
        /// Dấu tích tròn 20 góc trên phải (−7), nhãn HÔM NAY đè mép trên (−13), lớp bỏ lỡ xám + nút "Bù".
        /// </summary>
        static DailyRewardScreen.DayCell DayCellUi(RectTransform c, int day, DailyDay data, Vector2 center, float w, float h, Sprite bg,
            Sprite halo, Sprite tickBg, Sprite tickMark, Sprite tagSprite, Sprite missSprite, Sprite buSprite)
        {
            bool big = data != null && data.milestone;
            var root = Rect($"Day{day}", c, Center, Center, center, new Vector2(w, h));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var glow = Img("Glow", root, halo, Center, Center, Vector2.zero, new Vector2(w + 44, h + 44));
            glow.gameObject.SetActive(false);
            var bgImg = Img("Background", root, bg, Center, Center, Vector2.zero, Vector2.zero);
            bgImg.type = Image.Type.Sliced;
            bgImg.rectTransform.anchorMin = Vector2.zero;
            bgImg.rectTransform.anchorMax = Vector2.one;
            bgImg.rectTransform.offsetMin = new Vector2(0, -3 * PT);
            bgImg.rectTransform.offsetMax = Vector2.zero;

            float half = h / 2f;
            Txt("Day", root, day.ToString(), fontHeavy, 11, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, half - 12f), new Vector2(w, 14));
            float iconSize = big ? 32f : 28f;
            var icon = Img("Icon", root, data?.icon, Center, Center, new Vector2(0, big ? -2.5f : -0.5f), new Vector2(iconSize, iconSize));
            icon.preserveAspect = true;
            var label = Txt("Label", root, data?.label ?? "", fontHeavy, big ? 9.5f : 10.5f, Dark, TextAnchor.MiddleCenter, Center,
                new Vector2(0, big ? -(half - 54.25f) * -1f - 0f : 0f), new Vector2(w + 6, 14));
            label.rectTransform.anchoredPosition = new Vector2(0, (half - (big ? 54.25f : 50.25f))) * PT;

            // Đã nhận: huy hiệu tích 20 (top −7, right −7)
            var tick = Img("Tick", root, tickBg, Center, Center, new Vector2(w / 2f + 7 - 10, half + 7 - 10 - 1), new Vector2(20, 22));
            Img("Mark", tick.transform, tickMark, Center, Center, new Vector2(0, 1), new Vector2(12, 12));
            tick.gameObject.SetActive(false);

            // Bỏ lỡ: lớp xám rgba(80,70,60,.45) trong viền (bo 11) + nút "Bù" (cao 19, chữ 10 trắng, icon quảng cáo 11) cách đáy 3
            var miss = Img("Missed", root, missSprite, Center, Center, Vector2.zero, Vector2.zero);
            miss.type = Image.Type.Sliced;
            Stretch(miss.rectTransform, 2.5f);
            miss.raycastTarget = true;
            miss.color = new Color(1, 1, 1, 0.45f);
            var bu = PillLabel("MakeUp", miss.transform, buSprite, "Bù", 10, Color.white, 19, 4);
            var buImg = (RectTransform)bu.transform.parent;
            buImg.anchoredPosition = new Vector2(0, -half + 2.5f + 3f + 9.5f) * PT;
            var buIcon = Img("Icon", buImg, Spr("UI/icon_watch_ad"), Center, Center, Vector2.zero, new Vector2(11, 11));
            buIcon.preserveAspect = true;
            buIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 11 * PT;
            buIcon.transform.SetAsFirstSibling();
            buImg.GetComponent<HorizontalLayoutGroup>().spacing = 2 * PT;
            buImg.GetComponent<Image>().raycastTarget = true;
            var buBtn = buImg.gameObject.AddComponent<Button>();
            buBtn.transition = Selectable.Transition.None;
            buImg.gameObject.AddComponent<CanvasGroup>();
            miss.gameObject.SetActive(false);

            // Hôm nay: nhãn đỏ HÔM NAY (chữ 9, cao 17) đè mép trên (top −13)
            var tag = PillLabel("TodayTag", root, tagSprite, "HÔM NAY", 9, Color.white, 17, 3);
            ((RectTransform)tag.transform.parent).anchoredPosition = new Vector2(0, half + 13 - 8.5f) * PT;
            var todayTag = tag.transform.parent.gameObject;
            todayTag.SetActive(false);

            return new DailyRewardScreen.DayCell
            {
                root = root, group = group, background = bgImg, glow = glow, icon = icon, label = label,
                tick = tick.gameObject, todayTag = todayTag, missed = miss.gameObject, makeUpButton = buBtn,
            };
        }

        /// <summary>Popup mở rương bí ẩn: nền tối 50%, khung kem 300 (viền 3, bo 24, bóng 6), rương 72, tiêu đề 22, tối đa 5 dòng quà, nút "Tuyệt!".</summary>
        static CanvasGroup BuildChestPopup(CanvasGroup screen, RectTransform c, DailyRewardScreen view)
        {
            var root = Rect("ChestPopup", screen.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(root, 0);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var dim = Img("Dim", root, null, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0);
            dim.color = new Color(0, 0, 0, 0.5f);
            dim.raycastTarget = true;

            const float pw = 300f, ph = 420f;
            var frame = Rect("Frame", root, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH)); // cùng hệ toạ độ với khung nội dung
            var panel = Rect("Panel", frame, Center, Center, P(195, 430), new Vector2(pw, ph));
            Img("Background", panel, UiArt.Box("dm_chest_panel", pw, ph, 24, 3, Dark, Grad("FFFBEF", "F7E9C6"), 6, Hex("B9A98C")),
                Center, new Vector2(0.5f, 1f), new Vector2(0, ph / 2f), new Vector2(pw, ph + 6));
            float top = ph / 2f;
            Img("Chest", panel, Spr("UI/icon_chest"), Center, Center, new Vector2(0, top - 18 - 36), new Vector2(72, 72)).preserveAspect = true;
            Txt("Title", panel, "Rương bí ẩn", fontHeavy, 22, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, top - 18 - 72 - 6 - 15), new Vector2(pw - 20, 30));
            Txt("Sub", panel, "Bạn nhận được", fontBold, 14, Muted, TextAnchor.MiddleCenter, Center, new Vector2(0, top - 18 - 72 - 6 - 30 - 2 - 10), new Vector2(pw - 20, 20));

            var icons = new Image[5];
            var texts = new Text[5];
            float rowTop = top - 18 - 72 - 6 - 30 - 2 - 20 - 10;
            for (int i = 0; i < 5; i++)
            {
                float cy = rowTop - 18 - i * 36;
                var row = Rect($"Row{i}", panel, Center, Center, new Vector2(0, cy), new Vector2(pw - 40, 34));
                icons[i] = Img("Icon", row, null, Center, Center, new Vector2(-(pw - 40) / 2f + 17, 0), new Vector2(30, 30));
                icons[i].preserveAspect = true;
                texts[i] = Txt("Text", row, "+5 Giây x2", fontHeavy, 16, Dark, TextAnchor.MiddleLeft, Center, new Vector2(-(pw - 40) / 2f + 40 + 100, 0), new Vector2(200, 22));
            }
            var (ok, _) = GameBtn("Ok", panel, "dm_btn_ok", pw - 40, 52, 18, "FFE68A", "FFC93C", "F0A400", "B87800", new Vector2(0, -top + 18 + 26 + 5), 5, true);
            Label(ok.transform, "Tuyệt!", 19, Dark, 5);
            root.gameObject.SetActive(false);

            Set(view, "chestPopup", group);
            Set(view, "chestPanel", panel);
            SetArray(view, "chestRowIcons", icons);
            SetArray(view, "chestRowTexts", texts);
            Set(view, "chestOkButton", ok);
            return group;
        }
    }
}
