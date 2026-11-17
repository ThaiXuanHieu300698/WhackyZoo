using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static WhackyZoo.EditorTools.GameSceneBuilder;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Cửa hàng: dữ liệu dụng cụ / thú (Docs/GameSpec.md › Cửa hàng, mô tả + chỉ số lấy từ Shop25D.dc.html)
    /// và màn Cửa hàng trong scene Home theo Docs/DesignSource/Shop25D.dc.html + images/screens/06_cua_hang.png.
    /// Toạ độ px thiết kế (390 × 844, gốc trên-trái), 1 px = 3 đơn vị UI.
    /// </summary>
    internal static class ShopBuilder
    {
        internal const string CatalogPath = DataRoot + "/ShopCatalog.asset";

        // ───────────────────────── Dữ liệu ─────────────────────────

        struct ToolDef
        {
            public string id, file, name, desc, cash, free;
            public int price, coinBonus;
            public HitArea area;
            public bool premium, isDefault, lightning;
        }

        // Dụng cụ tạm thời chỉ khác nhau ở hình và tầm đập (người dùng chốt): Búa Thor 6 ô kèm sấm sét, còn lại 1 ô.
        // Mô tả = một dòng về tầm đập; bỏ 3 chỉ số.
        const string SingleDesc = "Tầm đập 1 ô: trúng đúng ô bạn chạm.";
        static readonly ToolDef[] Tools =
        {
            new ToolDef { id = "go", file = "tool_wood_hammer", name = "Búa Gỗ", price = 0, isDefault = true, desc = SingleDesc },
            new ToolDef { id = "dep", file = "tool_slipper", name = "Dép Lào", price = 500, desc = SingleDesc },
            new ToolDef { id = "vot", file = "tool_bug_racket", name = "Vợt Muỗi", price = 1200, desc = SingleDesc },
            new ToolDef { id = "hoi", file = "tool_squeaky_hammer", name = "Búa Hơi", price = 2000, desc = SingleDesc },
            new ToolDef { id = "chao", file = "tool_iron_pan", name = "Chảo Gang", price = 3500, desc = SingleDesc },
            new ToolDef { id = "thor", file = "tool_thor_hammer", name = "Búa Thor", price = 9900, cash = "49.000đ", premium = true,
                area = HitArea.Triangle, lightning = true, desc = "Tầm đập 6 ô tam giác, sấm sét lan khắp bàn." },
            new ToolDef { id = "bbat", file = "tool_baseball_bat", name = "Gậy Bóng Chày", price = 8000, cash = "39.000đ", premium = true, coinBonus = 20,
                free = "Dùng miễn phí 7 ngày khi điểm danh ngày 30", desc = "Tầm đập 1 ô, mỗi ván được thêm 20% xu." },
        };

        struct PetDef
        {
            public string id, map, file, name, desc, cash, free;
            public int price, points;
            public bool premium;
        }

        // Thú có sẵn của từng map, cùng thứ tự với MapData.normalCritters (điểm lấy từ CritterData)
        static readonly Dictionary<string, (string name, string card, string desc)[]> BasePets = new Dictionary<string, (string, string, string)[]>
        {
            ["land"] = new[] { ("Chó", (string)null, "Bạn thân của mọi nhà, ló lên đều đặn."), ("Mèo", (string)null, "Lười biếng thò đầu ra rồi ngáp một cái."),
                               ("Chuột", (string)null, "Nhỏ xíu mà lẹ làng, chui ra chui vào liên tục."), ("Heo", (string)null, "Ủn ỉn tròn trịa, chạy chẳng nổi.") },
            ["sea"] = new[] { ("Cua", (string)null, "Bò ngang ra khỏi hố, giơ càng chào."), ("Bạch Tuộc", (string)null, "Tám xúc tu ngọ nguậy, phun mực khi rụt xuống."),
                              ("Cá", (string)null, "Quẫy đuôi ló lên rồi lượn mất."), ("Sao Biển", (string)null, "Nằm im cười tươi giữa đáy biển.") },
            ["desert"] = new[] { ("Cáo", (string)null, "Tai to thính, nghe động là lủi ngay."), ("Rắn", (string)null, "Uốn lượn chữ S trồi lên khỏi cát."),
                                 ("Thằn Lằn", (string)null, "Lè lưỡi liếm mép rồi vụt chạy."), ("Chồn Đất", (string)null, "Đứng thẳng ngó nghiêng canh gác.") },
            ["arctic"] = new[] { ("Gấu Bắc Cực", (string)null, "To xác lông trắng, thò đầu ra chào."), ("Cánh Cụt", (string)null, "Lạch bạch trượt bụng ra khỏi hố băng."),
                                 ("Hải Cẩu", (string)null, "Mắt tròn long lanh, vỗ vây chào."), ("Cá Voi Sát Thủ", "Cá Voi", "Vây lưng nhô lên trước, rồi cả thân vọt ra.") },
            ["grave"] = new[] { ("Dơi", (string)null, "Treo ngược ngủ ngày, đêm đến vỗ cánh ló ra."), ("Mèo Đen", (string)null, "Mắt vàng sáng rực trong bóng tối."),
                                ("Quạ", (string)null, "Kêu “quạ quạ” rồi vụt bay lên."), ("Nhện", (string)null, "Đu tơ thả xuống rồi leo lên thoăn thoắt.") },
        };

        static readonly PetDef[] ShopPets =
        {
            new PetDef { id = "rabbit", map = "land", file = "animal_shop_rabbit", name = "Thỏ", price = 800, points = 12, desc = "Tai dài vểnh cao, ló lên là nhún nhảy liên hồi." },
            new PetDef { id = "duck", map = "land", file = "animal_shop_duck", name = "Vịt", price = 1000, points = 12, desc = "Kêu “quạc quạc” mỗi lần ló lên khỏi hố." },
            new PetDef { id = "panda", map = "land", file = "animal_shop_panda", name = "Gấu Trúc", price = 2500, points = 20, desc = "Tròn trịa đen trắng, vừa ló lên đã nhai trúc." },
            new PetDef { id = "capy", map = "land", file = "animal_shop_capybara", name = "Capybara", price = 6000, points = 25,
                free = "Miễn phí khi điểm danh ngày 14", desc = "Chill nhất hội, thong thả ló lên chẳng sợ ai." },
            new PetDef { id = "uni", map = "land", file = "animal_shop_unicorn", name = "Kỳ Lân", price = 9900, cash = "49.000đ", premium = true, points = 40, desc = "Sừng vàng lấp lánh, bờm óng ánh sắc cầu vồng." },
            new PetDef { id = "dolphin", map = "sea", file = "animal_shop_dolphin", name = "Cá Heo", price = 1500, points = 15, desc = "Nhảy vọt khỏi mặt nước rồi lặn mất tăm." },
            new PetDef { id = "turtle", map = "sea", file = "animal_shop_turtle", name = "Rùa Biển", price = 3000, points = 22, desc = "Thò đầu khỏi mai, chớp mắt nhìn quanh." },
            new PetDef { id = "dino", map = "desert", file = "animal_shop_dino", name = "Khủng Long", price = 5000, points = 25, desc = "Khủng long con lạch bạch chui lên từ cát." },
            new PetDef { id = "camel", map = "desert", file = "animal_shop_camel", name = "Lạc Đà", price = 4000, points = 24, desc = "Nhai cỏ khô thong thả, chẳng vội trốn đi đâu." },
            new PetDef { id = "owl", map = "arctic", file = "animal_shop_snowy_owl", name = "Cú Tuyết", price = 3500, points = 22, desc = "Xoay đầu nhìn quanh rồi vụt bay mất." },
            new PetDef { id = "ghost", map = "grave", file = "animal_shop_ghost", name = "Ma Nhỏ", price = 4500, points = 26, desc = "Lúc ẩn lúc hiện, cười khúc khích trong đêm." },
            new PetDef { id = "howl", map = "grave", file = "animal_shop_horned_owl", name = "Cú Mèo", price = 3800, points = 24, desc = "Mắt tròn xoe canh đêm, nghe động là rụt cổ." },
        };

        /// <summary>
        /// Dữ liệu ló lên của thú mua: thú thường, chỉ khác hình và điểm. Ảnh "trong hố" xuất từ SVG ở Game&lt;Map&gt;25D.dc.html
        /// (key = id thú) đặt tại Art/Characters/InHole/pet_&lt;id&gt;.png; chớp mắt sinh từ nhóm g.eyes cùng file.
        /// Chưa có ảnh trong hố thì tạm dùng ảnh thẻ Cửa hàng, không chớp mắt.
        /// </summary>
        static CritterData PetCritter(PetDef d, Sprite shopIcon)
        {
            var c = Upsert<CritterData>($"{DataRoot}/Shop/Pets/Critters/Critter_{Capitalize(d.id)}.asset");
            c.id = d.id;
            c.displayName = d.name;
            c.shortName = null;
            c.hint = null;
            c.kind = CritterKind.Normal;
            c.points = d.points;
            string inHole = $"{Art}/Characters/InHole/pet_{d.id}.png";
            if (System.IO.File.Exists(inHole))
            {
                c.sprite = LoadSprite(inHole);
                CritterAnimBuilder.Apply(c, d.map, d.id, inHole);
            }
            else
            {
                c.sprite = shopIcon;
                c.animSprite = null; c.blinkHalf = null; c.blinkClosed = null; c.spark = null;
            }
            EditorUtility.SetDirty(c);
            return c;
        }

        internal static readonly (string id, string name)[] PetMaps =
            { ("land", "Trên cạn"), ("sea", "Dưới biển"), ("desert", "Sa mạc"), ("arctic", "Bắc cực"), ("grave", "Nghĩa trang") };

        internal static ShopCatalog CreateShop(MapCatalog maps, BoosterCatalog boosters)
        {
            EnsureFolder($"{DataRoot}/Shop/Tools");
            EnsureFolder($"{DataRoot}/Shop/Pets");
            EnsureFolder($"{DataRoot}/Shop/Pets/Critters");
            var tools = new List<ToolData>();
            foreach (var d in Tools)
            {
                var t = Upsert<ToolData>($"{DataRoot}/Shop/Tools/Tool_{Capitalize(d.id)}.asset");
                t.id = d.id; t.displayName = d.name; t.cardName = null; t.description = d.desc;
                t.icon = LoadSprite($"{Art}/Tools/{d.file}.png");
                t.price = d.price; t.cashPrice = d.cash; t.premium = d.premium; t.isDefault = d.isDefault;
                t.hitArea = d.area; t.lightning = d.lightning; t.coinBonusPercent = d.coinBonus; t.freeHint = d.free;
                EditorUtility.SetDirty(t);
                tools.Add(t);
            }

            var pets = new List<PetData>();
            foreach (var (mapId, _) in PetMaps)
            {
                var map = maps != null ? maps.Find(mapId) : null;
                if (map != null && BasePets.TryGetValue(mapId, out var bases))
                    for (int i = 0; i < bases.Length && i < map.normalCritters.Length; i++)
                    {
                        var critter = map.normalCritters[i];
                        var p = Upsert<PetData>($"{DataRoot}/Shop/Pets/Pet_{Capitalize(mapId)}_{critter.name}.asset");
                        p.id = "base_" + critter.name; p.mapId = mapId; p.isBase = true;
                        p.displayName = bases[i].name; p.cardName = bases[i].card; p.description = bases[i].desc;
                        p.icon = critter.sprite; p.points = critter.points; p.critter = critter;
                        p.price = 0; p.cashPrice = null; p.premium = false; p.freeHint = null;
                        EditorUtility.SetDirty(p);
                        pets.Add(p);
                    }
                foreach (var d in ShopPets)
                {
                    if (d.map != mapId) continue;
                    var p = Upsert<PetData>($"{DataRoot}/Shop/Pets/Pet_{Capitalize(d.id)}.asset");
                    p.id = d.id; p.mapId = d.map; p.isBase = false;
                    p.displayName = d.name; p.cardName = null; p.description = d.desc;
                    p.icon = LoadSprite($"{Art}/Characters/{d.file}.png");
                    p.points = d.points;
                    p.critter = PetCritter(d, p.icon);
                    p.price = d.price; p.cashPrice = d.cash; p.premium = d.premium; p.freeHint = d.free;
                    EditorUtility.SetDirty(p);
                    pets.Add(p);
                }
            }

            var catalog = Upsert<ShopCatalog>(CatalogPath);
            catalog.tools = tools.ToArray();
            catalog.pets = pets.ToArray();
            catalog.boosters = boosters;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ───────────────────────── Màn Cửa hàng ─────────────────────────

        static Vector2 P(float cx, float cy) => CenterPos(cx, cy);
        static readonly Color Muted = Hex("5E4E3A");
        static readonly UiArt.Stop[] PanelWhite = { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "F3EEE4") };
        static UiArt.Stop[] Grad(params string[] hex)
        {
            var s = new UiArt.Stop[hex.Length];
            for (int i = 0; i < hex.Length; i++) s[i] = new UiArt.Stop(hex.Length == 1 ? 0f : i / (float)(hex.Length - 1), hex[i]);
            return s;
        }

        /// <summary>Nền màn: radial-gradient(ellipse at 50% 0%, #FFF5D6 0%, #FFE2A0 55%, #F7C873 100%).</summary>
        internal static Sprite Background()
        {
            const int w = 390, h = 844;
            string path = $"{Art}/Generated/gen_shop_radial.png";
            if (!System.IO.File.Exists(path))
            {
                Color a = Hex("FFF5D6"), b = Hex("FFE2A0"), c = Hex("F7C873");
                // ellipse "farthest-corner" tâm (195, 0): bán kính theo tỉ lệ khung → khoảng cách chuẩn hoá tới góc dưới = 1
                float rx = 195f * Mathf.Sqrt(2f), ry = 844f * Mathf.Sqrt(2f);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fx = (x + 0.5f - 195f) / rx, fy = (h - 1 - y + 0.5f) / ry;
                        float t = Mathf.Sqrt(fx * fx + fy * fy);
                        px[y * w + x] = t < 0.55f ? Color.Lerp(a, b, t / 0.55f) : Color.Lerp(b, c, Mathf.Clamp01((t - 0.55f) / 0.45f));
                    }
                EnsureFolder($"{Art}/Generated");
                AmbientArtBuilder.Write(path, w, h, px);
            }
            return LoadSprite(path);
        }

        /// <summary>Nút .gbtn cao 52 (+ cạnh 5), co giãn ngang theo hàng nút; trả về (Button, ảnh, CanvasGroup).</summary>
        static Button GameBtn(string name, Transform parent, string key, string hi, string mid, string lo, string edge)
        {
            var img = Img(name, parent, UiArt.GameButton($"shop_btn_{key}", 160, 52, 18, hi, mid, lo, edge, 5f, 3f, sliceX: true),
                Center, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(160, 57));
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            img.gameObject.AddComponent<PressEffect>();
            img.gameObject.AddComponent<CanvasGroup>();
            return b;
        }

        /// <summary>Chữ trên mặt nút (tâm mặt cao hơn tâm ảnh 2,5 vì cạnh 5 phía dưới).</summary>
        static Text BtnText(Button b, string text, float size, Color color, float dy = 0f)
        {
            var t = Txt("Label", b.transform, text, fontHeavy, size, color, TextAnchor.MiddleCenter, Center, new Vector2(0, 2.5f + dy), new Vector2(160, size + 6));
            t.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            t.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(0, (size + 6) * PT);
            return t;
        }

        static Text Chip(string name, Transform parent, Sprite sprite, string text, float font, Color color, float h, float pad)
        {
            var t = PillLabel(name, parent, sprite, text, font, color, h, pad);
            return t;
        }

        internal static ShopScreen Build(CanvasGroup screen, RectTransform c, ScreenRouter router, ShopCatalog catalog, CanvasGroup dailyScreen)
        {
            screen.transform.Find("Background").GetComponent<Image>().sprite = Background();

            // ── Hàng tiêu đề (cao 54, tâm y 83): nút quay lại 48 + viền = 54; tiêu đề 30; ô xu (đệm 4/12/4/6, xu 24, chữ 16) ──
            var back = Btn("BackButton", c, Spr("UI/button_white"), Center, P(43, 83) - new Vector2(0, 2.5f), new Vector2(54, 59), null, 0, Dark, null);
            Img("Icon", back.transform, Spr("UI/icon_back"), Center, Center, new Vector2(0, 2.5f), new Vector2(26, 26));
            var title = Txt("Title", c, "CỬA HÀNG", fontHeavy, 30, Color.white, TextAnchor.MiddleLeft, Center, P(82 + 120, 83), new Vector2(240, 40));
            title.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));

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
            var coinIcon = Img("Icon", coinChip.transform, Spr("UI/icon_coin"), Center, Center, Vector2.zero, new Vector2(24, 24));
            coinIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 24 * PT;
            var coinsText = Txt("Coins", coinChip.transform, "0", fontHeavy, 16, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(60, 24));

            // ── Tab (y 122–176): khay #E9DFCB → #F6F0E4 viền 3 bo 16, đệm 4; nút chọn vàng cao 40, viền 2, bo 12, cạnh 3 ──
            Img("TabTray", c, UiArt.Box("shop_tab_tray", 358, 54, 16, 3, Dark, Grad("E9DFCB", "F6F0E4")), Center, Center, P(195, 149), new Vector2(358, 54));
            var tabOn = UiArt.GameButton("shop_tab_on", 112, 40, 12, "FFE68A", "FFC93C", "F0A400", "B87800", 3f, 2f);
            string[] tabNames = { "Dụng cụ", "Thú", "Vật phẩm" };
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
                tabTexts[i] = Txt("Label", hit.transform, tabNames[i], fontHeavy, 16, Dark, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(112, 24));
            }

            // ── Thân trang (từ y 188): khung chi tiết rồi lưới, cách 12 ──
            var body = Rect("Body", c, Center, new Vector2(0.5f, 1f), P(195, 188), new Vector2(358, 560));
            var bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 12 * PT;
            bodyLayout.childAlignment = TextAnchor.UpperCenter;
            bodyLayout.childControlWidth = bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            var view = screen.gameObject.AddComponent<ShopScreen>();
            BuildDetail(body, view);
            var tagSprites = TagSprites();
            var toolCards = ToolScroll(body, view, catalog.tools.Length, out var toolGrid);
            var itemCards = Grid(body, "ItemGrid", 3, 112.67f, 10, catalog.boosters.boosters.Length, false, out var itemGrid);
            var petPanel = BuildPetPanel(body, view, catalog);

            // Thẻ Huyền thoại: nền tím, bóng #8A62C4, vệt sáng
            for (int i = 0; i < toolCards.Length; i++) StyleCard(toolCards[i], catalog.tools[i].premium, false);
            for (int i = 0; i < itemCards.Length; i++) StyleCard(itemCards[i], false, false);

            // ── Ô Tích xu miễn phí (đáy cách 20): .panel bo 20, đệm 10/10/10/14, icon 38, chữ 16 / 13, nút +100 xu cao 44 ──
            const float freeH = 74.4f, freeTop = 844 - 20 - freeH;
            var free = Img("FreeCoins", c, UiArt.Box("shop_free_panel", 358, freeH, 20, 3, Dark, PanelWhite, 5, Hex("B9A98C")),
                Center, new Vector2(0.5f, 1f), P(195, freeTop), new Vector2(358, freeH + 5));
            float freeCy = freeTop + freeH / 2f;
            Img("Icon", c, Spr("UI/icon_watch_ad"), Center, Center, P(16 + 3 + 14 + 19, freeCy), new Vector2(38, 38));
            var freeTitle = Txt("Title", c, "Tích xu miễn phí", fontHeavy, 16, Dark, TextAnchor.MiddleLeft, Center, P(81 + 95, freeCy - 15), new Vector2(190, 20));
            var freeSub = Txt("Sub", c, "Xem quảng cáo ngắn · còn 4 lượt hôm nay", fontBold, 13, Muted, TextAnchor.UpperLeft, Center, P(81 + 95, freeCy + 9), new Vector2(190, 30));
            freeSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            freeSub.lineSpacing = 0.72f; // 13 px × 1,15
            var freeBtnImg = Img("FreeCoinsButton", c, UiArt.GameButton("shop_btn_free", 82, 44, 14, "B8F5B0", "5BD06A", "2FA444", "1F7A33", 4f),
                Center, Center, P(361 - 41, freeCy + 2), new Vector2(82, 48));
            freeBtnImg.raycastTarget = true;
            var freeBtn = freeBtnImg.gameObject.AddComponent<Button>();
            freeBtnImg.gameObject.AddComponent<PressEffect>();
            Txt("Label", freeBtnImg.transform, "+100 xu", fontHeavy, 15, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 2), new Vector2(82, 20));

            Set(view, "router", router);
            Set(view, "catalog", catalog);
            Set(view, "dailyScreen", dailyScreen);
            Set(view, "backButton", back);
            Set(view, "coinsText", coinsText);
            Set(view, "coinChip", coinChip.rectTransform);
            SetArray(view, "tabBackgrounds", tabBgs);
            SetArray(view, "tabLabels", tabTexts);
            SetArray(view, "tabButtons", tabBtns);
            Set(view, "toolGrid", toolGrid);
            SetArray(view, "toolCards", toolCards);
            Set(view, "itemGrid", itemGrid);
            SetArray(view, "itemCards", itemCards);
            Set(view, "petPanel", petPanel);
            Set(view, "tagInUse", tagSprites[0]);
            Set(view, "tagBase", tagSprites[1]);
            Set(view, "tagOwned", tagSprites[2]);
            Set(view, "tagPrice", tagSprites[3]);
            Set(view, "freeCoinsButton", freeBtn);
            Set(view, "body", body);
            Set(view, "freePanel", free.rectTransform);
            return view;
        }

        // Nhãn trên thẻ: Đang dùng (xanh lá) / Có sẵn (xanh dương) / Đã có (xám kem) / giá (vàng); viền 2, cao 22
        static Sprite[] TagSprites()
        {
            Sprite T(string key, string a, string b) => UiArt.Box($"shop_tag_{key}", 22, 22, 11, 2, Dark, Grad(a, b), sliceX: true);
            return new[] { T("inuse", "B8F5B0", "5BD06A"), T("base", "E2F4FF", "A8DCFF"), T("owned", "F3EEE4", "DDD3C0"), T("price", "FFE68A", "FFC93C") };
        }

        // ── Khung chi tiết: nền #FFFBEF → #F7E9C6 viền 3 bo 22, bóng 0 5px #B9A98C + 0 14px 20px rgba(0,0,0,.2); đệm 12/14/14, cách 8 ──
        static void BuildDetail(RectTransform body, ShopScreen view)
        {
            var detail = Rect("Detail", body, Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(358, 230));
            var v = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(Mathf.RoundToInt(17 * PT), Mathf.RoundToInt(17 * PT), Mathf.RoundToInt(15 * PT), Mathf.RoundToInt(17 * PT));
            v.spacing = 8 * PT;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var soft = Img("SoftShadow", detail, UiArt.SoftShadow("shop_detail_soft", 358, 230, 22, 20, 0.2f), Center, Center, Vector2.zero, Vector2.zero);
            var bg = Img("Background", detail, UiArt.Box("shop_detail", 358, 120, 22, 3, Dark, Grad("FFFBEF", "F7E9C6"), 5, Hex("B9A98C"), sliceX: true, sliceY: true),
                Center, Center, Vector2.zero, Vector2.zero);
            bg.type = Image.Type.Sliced;
            Ignore(bg.rectTransform, 0, -5, 0, 0);
            Ignore(soft.rectTransform, -20, -20 - 14, 20, 20 - 14); // gọi sau để bóng nằm dưới nền

            // Tên 24 + nhãn HUYỀN THOẠI (tím #C9A2FF → #8A4FD8, viền 2, bo 8, chữ 12 trắng, bóng 0 2px #4E2490)
            var nameRow = Rect("NameRow", detail, Center, Center, Vector2.zero, new Vector2(324, 24));
            nameRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 24 * PT;
            var nh = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            nh.spacing = 8 * PT;
            nh.childAlignment = TextAnchor.MiddleLeft;
            nh.childControlWidth = true;
            nh.childControlHeight = false;
            nh.childForceExpandWidth = nh.childForceExpandHeight = false;
            var name = Txt("Name", nameRow, "Búa Thor", fontHeavy, 24, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(200, 30));
            var badgeText = PillLabel("Premium", nameRow, UiArt.Box("shop_premium", 30, 22, 8, 2, Dark, Grad("C9A2FF", "8A4FD8"), 2, Hex("4E2490"), sliceX: true),
                "HUYỀN THOẠI", 12, Color.white, 22, 6);
            Object.DestroyImmediate(badgeText.transform.parent.GetComponent<ContentSizeFitter>());
            ((RectTransform)badgeText.transform).anchoredPosition = new Vector2(0, 1f * PT);

            // Mô tả 14 Bold #5E4E3A, cao dòng 1,3
            var desc = Txt("Desc", detail, "Mô tả", fontBold, 14, Muted, TextAnchor.UpperLeft, Center, Vector2.zero, new Vector2(324, 18.2f));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.lineSpacing = 1.3f / 1.6f; // Baloo 2: cao dòng mặc định ≈ 1,6 em
            var descLayout = desc.gameObject.AddComponent<LayoutElement>();
            descLayout.preferredHeight = 18.2f * PT;

            // Nhãn vàng "Miễn phí khi điểm danh ngày 14" (Shop25D: viền 2, bo tròn, đệm 1/10/1/4, icon quà 18, cách 6, chữ 12) — bấm mở Điểm danh.
            // Đang dùng thử dụng cụ (quà ngày 30): cùng chỗ, nền xanh, "Đang dùng thử · còn …", không bấm được.
            var freeRow = Rect("FreeRow", detail, Center, Center, Vector2.zero, new Vector2(324, 23));
            freeRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 23 * PT;
            var frh = freeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            frh.childAlignment = TextAnchor.MiddleLeft;
            frh.childControlWidth = true;
            frh.childControlHeight = false;
            frh.childForceExpandWidth = frh.childForceExpandHeight = false;
            var freeSprite = UiArt.Box("shop_free_pill", 23, 23, 11.5f, 2, Dark, Grad("FFF3B0", "FFC93C"), sliceX: true);
            var trialSprite = UiArt.Box("shop_trial_pill", 23, 23, 11.5f, 2, Dark, Grad("B8F5B0", "5BD06A"), sliceX: true);
            var freeText = PillLabel("FreePill", freeRow, freeSprite, "Miễn phí khi điểm danh ngày 14", 12, Dark, 23, 6);
            var freePill = (RectTransform)freeText.transform.parent;
            var fpl = freePill.GetComponent<HorizontalLayoutGroup>();
            fpl.padding = new RectOffset(Mathf.RoundToInt(6 * PT), Mathf.RoundToInt(12 * PT), 0, 0);
            fpl.spacing = 6 * PT;
            var freeIcon = Img("Icon", freePill, Spr("UI/icon_daily_gift"), Center, Center, Vector2.zero, new Vector2(18, 18));
            freeIcon.preserveAspect = true;
            freeIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 18 * PT;
            freeIcon.transform.SetAsFirstSibling();
            var freeImg = freePill.GetComponent<Image>();
            freeImg.raycastTarget = true;
            var freeBtn2 = freePill.gameObject.AddComponent<Button>();
            freeBtn2.transition = Selectable.Transition.None;
            freePill.gameObject.AddComponent<PressEffect>();
            Set(view, "freeRow", freeRow.gameObject);
            Set(view, "freePill", freeBtn2);
            Set(view, "freeImage", freeImg);
            Set(view, "freeIcon", freeIcon.gameObject);
            Set(view, "freeText", freeText);
            Set(view, "freeSprite", freeSprite);
            Set(view, "trialSprite", trialSprite);

            // Thú: một dòng điểm thưởng (bỏ 3 chỉ số — mọi thú ló/rụt như nhau, chỉ khác điểm): 14 Heavy nâu đậm
            var points = Txt("Points", detail, "Điểm thưởng: +10 điểm mỗi lần đập trúng", fontHeavy, 14, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(324, 20));
            points.gameObject.AddComponent<LayoutElement>().preferredHeight = 20 * PT;

            // Dụng cụ: lưới 3×3 mô phỏng tầm đập (ô 22, cách 4,5 → cao 75 như khối chỉ số để khung không đổi cỡ) + chú thích bên phải
            var range = Rect("HitRange", detail, Center, Center, Vector2.zero, new Vector2(324, 75));
            range.gameObject.AddComponent<LayoutElement>().preferredHeight = 75 * PT;
            const float cell = 22, cellGap = 4.5f;
            var cellNormal = UiArt.Box("shop_range_cell", cell, cell, 6, 2, Hex("B9A98C"), Grad("FFFFFF", "EDE5D6"));
            var cellHit = UiArt.Box("shop_range_hit", cell, cell, 6, 2, Dark, Grad("FFB8A0", "FF6B4A", "E0482A"));
            var cellBolt = UiArt.Box("shop_range_bolt", cell, cell, 6, 2, Dark, Grad("FFF6B0", "FFD84A", "F0A400"));
            var cells = new Image[9];
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    cells[r * 3 + c] = Img($"Cell{r}{c}", range, cellNormal, Center, Center,
                        new Vector2(-162 + cell / 2f + c * (cell + cellGap), 37.5f - cell / 2f - r * (cell + cellGap)), new Vector2(cell, cell));
            var tapIcon = Img("TapIcon", cells[4].transform, null, Center, Center, Vector2.zero, new Vector2(20, 20));
            tapIcon.preserveAspect = true;
            float legendX = -162 + 3 * cell + 2 * cellGap + 18;
            var swatch = Img("LegendHit", range, cellHit, Center, Center, new Vector2(legendX + 8, 13), new Vector2(16, 16));
            Txt("LegendHitText", range, "Ô bị đập", fontBold, 13, Muted, TextAnchor.MiddleLeft, Center, new Vector2(legendX + 24 + 70, 13), new Vector2(140, 20));
            var legendIcon = Img("LegendTap", range, null, Center, Center, new Vector2(legendX + 8, -13), new Vector2(18, 18));
            legendIcon.preserveAspect = true;
            Txt("LegendTapText", range, "Ô bạn chạm", fontBold, 13, Muted, TextAnchor.MiddleLeft, Center, new Vector2(legendX + 24 + 70, -13), new Vector2(140, 20));
            var rangeView = range.gameObject.AddComponent<HitRangeView>();
            Set(rangeView, "legendIcon", legendIcon);
            SetArray(rangeView, "cells", cells);
            Set(rangeView, "cellNormal", cellNormal);
            Set(rangeView, "cellHit", cellHit);
            Set(rangeView, "cellLightning", cellBolt);
            Set(rangeView, "tapIcon", tapIcon);
            Set(rangeView, "legendSwatch", swatch);

            // Vật phẩm: "Đang có:" + chip xN (xanh, viền 2, cao 26); bỏ câu "· dùng được trong mọi map" theo yêu cầu
            var owned = Rect("OwnedRow", detail, Center, Center, Vector2.zero, new Vector2(324, 26));
            owned.gameObject.AddComponent<LayoutElement>().preferredHeight = 26 * PT;
            var oh = owned.gameObject.AddComponent<HorizontalLayoutGroup>();
            oh.spacing = 8 * PT;
            oh.childAlignment = TextAnchor.MiddleLeft;
            oh.childControlWidth = true;
            oh.childControlHeight = false;
            oh.childForceExpandWidth = oh.childForceExpandHeight = false;
            Txt("Label", owned, "Đang có:", fontHeavy, 14, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(70, 22));
            var qty = PillLabel("Qty", owned, UiArt.Box("shop_qty_chip", 26, 26, 13, 2, Dark, Grad("A8DCFF", "3FA9F5"), sliceX: true), "x2", 14, Color.white, 26, 10);
            Object.DestroyImmediate(qty.transform.parent.GetComponent<ContentSizeFitter>());
            TextShadow(qty, 1, 0.35f);

            // Hàng nút: cách trên 4 (margin-top), nút cao 52 + cạnh 5, cách 8, chia đều
            var buttons = Rect("Buttons", detail, Center, Center, Vector2.zero, new Vector2(324, 56));
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 56 * PT;
            var bh = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.padding = new RectOffset(0, 0, Mathf.RoundToInt(4 * PT), 0);
            bh.spacing = 8 * PT;
            bh.childAlignment = TextAnchor.UpperLeft;
            bh.childControlWidth = true;
            bh.childControlHeight = false;
            bh.childForceExpandWidth = true;
            bh.childForceExpandHeight = false;

            Color brownMuted = Hex("6B5A45");
            var equipped = GameBtn("Equipped", buttons, "grey", "F3EEE4", "E3DACA", "CFC3AE", "A8977A"); BtnText(equipped, "Đang dùng", 18, Dark);
            var equip = GameBtn("Equip", buttons, "green", "B8F5B0", "5BD06A", "2FA444", "1F7A33"); BtnText(equip, "Trang bị", 18, Dark);
            var petOn = GameBtn("PetOn", buttons, "green", "B8F5B0", "5BD06A", "2FA444", "1F7A33"); var petOnText = BtnText(petOn, "Đang dùng", 18, Dark);
            var petOff = GameBtn("PetOff", buttons, "yellow", "FFE68A", "FFC93C", "F0A400", "B87800"); var petOffText = BtnText(petOff, "Dùng", 18, Dark);
            var baseBtn = GameBtn("Base", buttons, "blue", "E2F4FF", "BDE3FF", "8CC8F0", "4F8FBF"); BtnText(baseBtn, "Có sẵn", 18, Dark);
            var buy = GameBtn("Buy", buttons, "yellow", "FFE68A", "FFC93C", "F0A400", "B87800");
            var buyRow = Rect("Content", buy.transform, Center, Center, new Vector2(0, 2.5f), new Vector2(160, 30));
            buyRow.anchorMin = new Vector2(0f, 0.5f); buyRow.anchorMax = new Vector2(1f, 0.5f); buyRow.sizeDelta = new Vector2(0, 30 * PT);
            var brh = buyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            brh.spacing = 8 * PT; brh.childAlignment = TextAnchor.MiddleCenter; brh.childControlWidth = true; brh.childControlHeight = false;
            brh.childForceExpandWidth = brh.childForceExpandHeight = false;
            var buyIcon = Img("Coin", buyRow, Spr("UI/icon_coin"), Center, Center, Vector2.zero, new Vector2(22, 22));
            buyIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 22 * PT;
            var buyText = Txt("Label", buyRow, "Mua 9.900", fontHeavy, 18, Dark, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(100, 24));
            var shortBtn = GameBtn("Short", buttons, "grey", "F3EEE4", "E3DACA", "CFC3AE", "A8977A"); var shortText = BtnText(shortBtn, "Thiếu 0 xu", 15, brownMuted);
            var cash = GameBtn("Cash", buttons, "red", "FFB8A0", "FF6B4A", "E0482A", "A8321C"); var cashText = BtnText(cash, "49.000đ", 18, Color.white);
            var cashShadow = cashText.gameObject.AddComponent<Shadow>(); cashShadow.effectColor = Hex("A8321C"); cashShadow.effectDistance = new Vector2(0, -2 * PT);
            var buy1 = GameBtn("Buy1", buttons, "yellow", "FFE68A", "FFC93C", "F0A400", "B87800");
            BtnText(buy1, "Mua x1", 17, Dark, 6.5f); var buy1Price = BtnText(buy1, "150 xu", 12, Dark, -8.5f);
            var buy5 = GameBtn("Buy5", buttons, "green", "B8F5B0", "5BD06A", "2FA444", "1F7A33");
            BtnText(buy5, "Mua x5", 17, Dark, 6.5f); var buy5Price = BtnText(buy5, "600 xu", 12, Dark, -8.5f);
            // Nhãn −20%: top −12, right −6, đỏ, viền 2, bo 8, chữ 11
            var off = PillLabel("Discount", buy5.transform, UiArt.Box("shop_discount", 24, 20, 8, 2, Dark, Grad("FF8A80", "E53935"), sliceX: true), "−20%", 11, Color.white, 20, 5);
            var offRt = (RectTransform)off.transform.parent;
            offRt.anchorMin = offRt.anchorMax = new Vector2(1f, 1f);
            offRt.pivot = new Vector2(1f, 1f);
            offRt.anchoredPosition = new Vector2(6f - 3f, 12f - 3f) * PT;
            offRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            Set(view, "detail", detail);
            Set(view, "nameText", name);
            Set(view, "premiumBadge", badgeText.transform.parent.gameObject);
            Set(view, "descText", desc);
            Set(view, "descLayout", descLayout);
            Set(view, "pointsText", points);
            Set(view, "hitRange", rangeView);
            Set(view, "ownedRow", owned.gameObject);
            Set(view, "ownedQty", qty);
            Set(view, "equippedButton", equipped);
            Set(view, "equipButton", equip);
            Set(view, "petOnButton", petOn);
            Set(view, "petOnText", petOnText);
            Set(view, "petOffButton", petOff);
            Set(view, "petOffText", petOffText);
            Set(view, "baseButton", baseBtn);
            Set(view, "buyButton", buy);
            Set(view, "buyText", buyText);
            Set(view, "shortButton", shortBtn);
            Set(view, "shortText", shortText);
            Set(view, "cashButton", cash);
            Set(view, "cashText", cashText);
            Set(view, "buy1Button", buy1);
            Set(view, "buy1Price", buy1Price);
            Set(view, "buy5Button", buy5);
            Set(view, "buy5Price", buy5Price);
        }

        /// <summary>Con không theo layout, căng theo cha với lề (pt): trái, dưới, phải, trên.</summary>
        static void Ignore(RectTransform rt, float l, float b, float r, float t)
        {
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b) * PT;
            rt.offsetMax = new Vector2(r, t) * PT;
            rt.SetAsFirstSibling();
        }

        // ── Lưới thẻ (.grid: padding-top 6, cách 10; thẻ cao 126,4 = viền 3 + đệm 8 + icon 62 + 2 + tên 15,4 + 2 + nhãn 22 + đệm 9 + viền 3) ──
        const float CardH = 126.4f;

        // Khoảng cách giữa hai hàng thẻ (thiết kế 8–10) nới lên 16: thẻ đang chọn nhấc 6 + viền cam cách 2 dày 3 = nhô 11 pt,
        // còn thẻ hàng trên có bóng cứng 4 pt bên dưới → 16 − 4 = 12 ≥ 11 để viền không đè lên thẻ phía trên.
        const float SelectedRowGap = 16f;

        static ShopCard[] Grid(RectTransform body, string name, int cols, float cellW, float gap, int count, bool pet, out GameObject root)
        {
            var g = Rect(name, body, Center, Center, Vector2.zero, new Vector2(358, 100));
            int rows = Mathf.CeilToInt(count / (float)cols);
            g.gameObject.AddComponent<LayoutElement>().preferredHeight = (6 + rows * CardH + (rows - 1) * gap) * PT;
            var grid = g.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, Mathf.RoundToInt(6 * PT), 0);
            grid.cellSize = new Vector2(cellW, CardH) * PT;
            grid.spacing = new Vector2(gap, SelectedRowGap) * PT;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            var cards = new ShopCard[count];
            for (int i = 0; i < count; i++) cards[i] = Card(g, $"Card{i}", cellW, pet);
            root = g.gameObject;
            return cards;
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!cache.TryGetValue(key, out var s) || s == null) cache[key] = s = make();
            return s;
        }

        /// <summary>Thẻ (.card): viền 3 bo 18, nền trắng → #F1EADC (Huyền thoại: #F3E8FF → #D8BEFA), bóng 0 4px #B9A98C + 0 8px 10px .16.</summary>
        static ShopCard Card(RectTransform parent, string name, float w, bool pet)
        {
            var root = Rect(name, parent, Center, Center, Vector2.zero, new Vector2(w, CardH));
            string wk = w.ToString("0");
            var soft = Img("SoftShadow", root, Cached("soft" + wk, () => UiArt.SoftShadow($"shop_card_soft_{wk}", w, CardH, 18, 10, 0.16f)),
                Center, Center, new Vector2(0, -8), new Vector2(w + 20, CardH + 20));
            var hard = Img("HardShadow", root, Cached("hard", () => UiArt.Box("shop_card_shape", 60, 60, 18, 0, Dark, Grad("B9A98C", "B9A98C"), sliceX: true, sliceY: true)),
                Center, Center, new Vector2(0, -4), new Vector2(w, CardH));
            hard.type = Image.Type.Sliced;
            var visual = Rect("Visual", root, Center, Center, Vector2.zero, new Vector2(w, CardH));
            Stretch(visual, 0);
            // viền cam 3 px cách thẻ 2 px (outline-offset) khi chọn
            var outline = Img("Outline", visual, Cached("ring", () => UiArt.Ring("shop_card_outline", 70, 70, 23, 3, "FF6B4A")), Center, Center, Vector2.zero, Vector2.zero);
            outline.type = Image.Type.Sliced;
            Stretch(outline.rectTransform, -5);
            outline.gameObject.SetActive(false);
            var bodyImg = Img("Body", visual, Cached("white", () => UiArt.Box("shop_card_white", 60, 60, 18, 3, Dark, Grad("FFFFFF", "F1EADC"), sliceX: true, sliceY: true)),
                Center, Center, Vector2.zero, Vector2.zero);
            bodyImg.type = Image.Type.Sliced;
            Stretch(bodyImg.rectTransform, 0);
            bodyImg.raycastTarget = true;
            bodyImg.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            // Vệt sáng (.shine::before): rộng 30, cao 140%, trắng .75 ở giữa, nghiêng −20°, bắt đầu ở left −50
            var shine = Img("Shine", bodyImg.transform, Cached("shine", ShineSprite), Center, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30, CardH * 1.4f));
            shine.rectTransform.anchorMin = shine.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            shine.rectTransform.anchoredPosition = new Vector2(-50, 0) * PT;
            shine.rectTransform.localRotation = Quaternion.Euler(0, 0, 20f);
            shine.gameObject.SetActive(false);

            // icon 62 (bóng drop-shadow 0 4px 3px .25), tên 14 (cao dòng 1,1), nhãn cao 22 — căn từ mép trên: 11 / 75 / 92,4
            float top = CardH / 2f;
            var icon = Rect("Icon", visual, Center, Center, new Vector2(0, top - 11 - 31), new Vector2(62, 62));
            var iconShadow = Img("Shadow", icon, null, Center, Center, new Vector2(0, -4), new Vector2(62, 62));
            iconShadow.color = new Color(0, 0, 0, 0.25f);
            iconShadow.preserveAspect = true;
            var iconImg = Img("Image", icon, null, Center, Center, Vector2.zero, new Vector2(62, 62));
            iconImg.preserveAspect = true;
            var label = Txt("Name", visual, "Tên", fontHeavy, 14, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, top - 75 - 7.7f), new Vector2(w, 18));
            label.resizeTextForBestFit = true; // tên dài (vd. "Gấu Bắc Cực") thu về tối thiểu 12 như thiết kế
            label.resizeTextMinSize = Mathf.RoundToInt(12 * PT);
            label.resizeTextMaxSize = Mathf.RoundToInt(14 * PT);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tagText = PillLabel("Tag", visual, null, "0 xu", pet ? 11 : 12, Dark, 22, pet ? 5 : 8);
            var tagRt = (RectTransform)tagText.transform.parent;
            tagRt.anchoredPosition = new Vector2(0, top - 92.4f - 11) * PT;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bodyImg;
            var card = root.gameObject.AddComponent<ShopCard>();
            Set(card, "button", button);
            Set(card, "visual", visual);
            Set(card, "hardShadow", hard.rectTransform);
            Set(card, "softShadow", soft.rectTransform);
            Set(card, "outline", outline.gameObject);
            Set(card, "icon", icon);
            Set(card, "iconImage", iconImg);
            Set(card, "iconShadow", iconShadow);
            Set(card, "nameText", label);
            Set(card, "tagImage", tagRt.GetComponent<Image>());
            Set(card, "tagText", tagText);
            Set(card, "shine", shine.rectTransform);
            var so = new SerializedObject(card);
            so.FindProperty("shineTravel").floatValue = w * 1.2f + 50f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        /// <summary>Thẻ Huyền thoại: nền tím, bóng cứng #8A62C4, bật vệt sáng.</summary>
        static void StyleCard(ShopCard card, bool premium, bool pet)
        {
            if (!premium) return;
            var t = card.transform;
            var body = t.Find("Visual/Body").GetComponent<Image>();
            body.sprite = Cached("purple", () => UiArt.Box("shop_card_purple", 60, 60, 18, 3, Dark, Grad("F3E8FF", "D8BEFA"), sliceX: true, sliceY: true));
            t.Find("HardShadow").GetComponent<Image>().sprite = Cached("hardPurple", () => UiArt.Box("shop_card_shape_purple", 60, 60, 18, 0, Dark, Grad("8A62C4", "8A62C4"), sliceX: true, sliceY: true));
            t.Find("Visual/Body/Shine").gameObject.SetActive(true);
        }

        static Sprite ShineSprite()
        {
            string path = $"{Art}/Generated/UI/shop_shine.png";
            const int w = 30, h = 8;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    px[y * w + x] = new Color(1f, 1f, 1f, 0.75f * (1f - Mathf.Abs(u - 0.5f) * 2f));
                }
            EnsureFolder($"{Art}/Generated/UI");
            AmbientArtBuilder.Write(path, w, h, px);
            return LoadSprite(path);
        }

        // ── Tab Dụng cụ: lưới 3 cột trong khung cuộn dọc (Shop25D: .petScroll max-height 300), chỉ mờ mép dưới ──
        static ShopCard[] ToolScroll(RectTransform body, ShopScreen view, int count, out GameObject root)
        {
            var holder = Rect("ToolScrollHolder", body, Center, Center, Vector2.zero, new Vector2(358, 300));
            var holderLayout = holder.gameObject.AddComponent<LayoutElement>();
            holderLayout.preferredHeight = 300 * PT;
            var scrollRt = Rect("ToolScroll", holder, Center, Center, Vector2.zero, Vector2.zero);
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            const float TopRoom = 8f;
            scrollRt.offsetMin = new Vector2(-10, 0) * PT; scrollRt.offsetMax = new Vector2(10, TopRoom) * PT;
            const float fadePt = 26f;
            scrollRt.gameObject.AddComponent<Image>();
            scrollRt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            var fadeRt = Rect("BottomFade", scrollRt, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fadeRt, 0);
            var mask = fadeRt.gameObject.AddComponent<RectMask2D>();
            int fadeUnits = Mathf.RoundToInt(fadePt * PT);
            mask.softness = new Vector2Int(0, fadeUnits);
            mask.padding = new Vector4(0, 0, 0, -fadeUnits);
            var content = Rect("Content", fadeRt, Top, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(378, 300));
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(Mathf.RoundToInt(10 * PT), Mathf.RoundToInt(10 * PT), Mathf.RoundToInt((6 + TopRoom) * PT), Mathf.RoundToInt(14 * PT));
            const float toolW = 112.67f;
            grid.cellSize = new Vector2(toolW, CardH) * PT;
            grid.spacing = new Vector2(10, SelectedRowGap) * PT;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = scrollRt;
            var cards = new ShopCard[count];
            for (int i = 0; i < count; i++) cards[i] = Card(content, $"Card{i}", toolW, false);
            Set(view, "toolScroll", scroll);
            Set(view, "toolScrollLayout", holderLayout);
            root = holder.gameObject;
            return cards;
        }

        // ── Tab Thú: hàng chip lọc map (cuộn ngang, tràn 16 hai bên, mép mờ) rồi lưới 4 cột cuộn dọc (cao tối đa 262) ──
        static GameObject BuildPetPanel(RectTransform body, ShopScreen view, ShopCatalog catalog)
        {
            var panel = Rect("PetPanel", body, Center, Center, Vector2.zero, new Vector2(358, 313));
            var pv = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            pv.spacing = 12 * PT;
            pv.childControlWidth = pv.childControlHeight = true;
            pv.childForceExpandWidth = true;
            pv.childForceExpandHeight = false;

            // Chip: cao 34, viền 2, bo tròn, đệm 12, chữ 12; chọn = dải màu map + bóng 0 3px rgba(58,36,18,.45); thường = trắng 70%
            var chipsHolder = Rect("ChipsHolder", panel, Center, Center, Vector2.zero, new Vector2(358, 39));
            chipsHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 39 * PT;
            var chipScrollRt = Rect("ChipScroll", chipsHolder, Center, Center, Vector2.zero, Vector2.zero);
            chipScrollRt.anchorMin = Vector2.zero; chipScrollRt.anchorMax = Vector2.one;
            chipScrollRt.offsetMin = new Vector2(-16, 0) * PT; chipScrollRt.offsetMax = new Vector2(16, 0) * PT;
            var chipMask = chipScrollRt.gameObject.AddComponent<RectMask2D>();
            chipMask.softness = new Vector2Int(Mathf.RoundToInt(16 * PT), 0);
            chipScrollRt.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
            var chipScroll = chipScrollRt.gameObject.AddComponent<ScrollRect>();
            chipScroll.horizontal = true; chipScroll.vertical = false;
            chipScroll.movementType = ScrollRect.MovementType.Elastic;
            var chipContent = Rect("Content", chipScrollRt, Center, new Vector2(0f, 1f), Vector2.zero, new Vector2(500, 39));
            chipContent.anchorMin = new Vector2(0f, 0f); chipContent.anchorMax = new Vector2(0f, 1f);
            chipContent.sizeDelta = new Vector2(500 * PT, 0);
            var ch = chipContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.padding = new RectOffset(Mathf.RoundToInt(16 * PT), Mathf.RoundToInt(16 * PT), 0, Mathf.RoundToInt(2 * PT));
            ch.spacing = 6 * PT;
            ch.childAlignment = TextAnchor.UpperLeft;
            ch.childControlWidth = true; ch.childControlHeight = false;
            ch.childForceExpandWidth = ch.childForceExpandHeight = false;
            chipContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            chipScroll.content = chipContent;
            chipScroll.viewport = chipScrollRt;

            var normal = UiArt.Box("shop_chip_normal", 34, 34, 17, 2, Dark, Grad("FFF8EA", "FFF8EA"), 3, new Color(0, 0, 0, 0), sliceX: true);
            string[][] colors = { new[] { "A6F09A", "4CC25E" }, new[] { "A8DCFF", "3FA9F5" }, new[] { "FFD3A0", "FF9F43" }, new[] { "E2F4FF", "7FC4EE" }, new[] { "D9CCF5", "A68CDC" } };
            var chips = new List<ShopScreen.PetChip>();
            var selected = new List<Sprite>();
            for (int i = 0; i < PetMaps.Length; i++)
            {
                var (id, label) = PetMaps[i];
                int n = 0;
                foreach (var p in catalog.pets) if (p.mapId == id) n++;
                var sel = UiArt.Box($"shop_chip_{id}", 34, 34, 17, 2, Dark, Grad(colors[i][0], colors[i][1]), 3, new Color(58 / 255f, 36 / 255f, 18 / 255f, 0.45f), sliceX: true);
                selected.Add(sel);
                var text = PillLabel($"Chip_{id}", chipContent, normal, $"{label} ({n})", 12, Dark, 37, 12);
                var img = text.transform.parent.GetComponent<Image>();
                ((RectTransform)text.transform).anchoredPosition = new Vector2(0, 1.5f * PT); // chữ giữa mặt chip (bỏ dải bóng 3 phía dưới)
                img.raycastTarget = true;
                var btn = img.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                Object.DestroyImmediate(img.GetComponent<ContentSizeFitter>());
                chips.Add(new ShopScreen.PetChip { mapId = id, background = img, label = text, button = btn });
            }

            // Danh sách thú: tràn 10 hai bên, đệm dưới 14, chỉ mờ mép dưới 26
            var scrollHolder = Rect("PetScrollHolder", panel, Center, Center, Vector2.zero, new Vector2(358, 262));
            var holderLayout = scrollHolder.gameObject.AddComponent<LayoutElement>();
            holderLayout.preferredHeight = 262 * PT;
            var scrollRt = Rect("PetScroll", scrollHolder, Center, Center, Vector2.zero, Vector2.zero);
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            const float TopRoom = 8f;
            scrollRt.offsetMin = new Vector2(-10, 0) * PT; scrollRt.offsetMax = new Vector2(10, TopRoom) * PT; // nới lên 8 (vào khoảng cách với hàng chip) để viền cam của thẻ đang chọn (nhấc 6 + viền 5) không bị cắt
            // Chỉ mờ mép dưới (người dùng chốt). RectMask2D mờ đều hai mép nên: khung nhìn cắt cứng bằng Mask stencil,
            // lớp con RectMask2D mờ 26 nhưng nới lên trên 26 (padding âm) để dải mờ trên rơi vào phần đã bị cắt.
            const float fadePt = 26f;
            scrollRt.gameObject.AddComponent<Image>(); // trắng đặc để Mask ghi stencil; không vẽ ra
            scrollRt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            var fadeRt = Rect("BottomFade", scrollRt, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fadeRt, 0);
            var mask = fadeRt.gameObject.AddComponent<RectMask2D>();
            int fadeUnits = Mathf.RoundToInt(fadePt * PT);
            mask.softness = new Vector2Int(0, fadeUnits);
            mask.padding = new Vector4(0, 0, 0, -fadeUnits); // trái, dưới, phải, trên
            var content = Rect("Content", fadeRt, Top, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(378, 300));
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(Mathf.RoundToInt(10 * PT), Mathf.RoundToInt(10 * PT), Mathf.RoundToInt((6 + TopRoom) * PT), Mathf.RoundToInt(14 * PT));
            const float petW = (358 - 3 * 8) / 4f;
            grid.cellSize = new Vector2(petW, CardH) * PT;
            grid.spacing = new Vector2(8, SelectedRowGap) * PT;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = scrollRt;

            var cards = new ShopCard[catalog.pets.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = Card(content, $"Pet{i}", petW, true);
                StyleCard(cards[i], catalog.pets[i].premium, true);
            }

            var so = new SerializedObject(view);
            var cp = so.FindProperty("petChips");
            cp.arraySize = chips.Count;
            for (int i = 0; i < chips.Count; i++)
            {
                var e = cp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("mapId").stringValue = chips[i].mapId;
                e.FindPropertyRelative("background").objectReferenceValue = chips[i].background;
                e.FindPropertyRelative("label").objectReferenceValue = chips[i].label;
                e.FindPropertyRelative("button").objectReferenceValue = chips[i].button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(view, "petChipNormal", normal);
            SetArray(view, "petChipSelected", selected);
            SetArray(view, "petCards", cards);
            Set(view, "petScroll", scroll);
            Set(view, "petScrollLayout", holderLayout);
            return panel.gameObject;
        }
    }
}
