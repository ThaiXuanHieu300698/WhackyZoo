using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static WhackyZoo.EditorTools.GameSceneBuilder;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Dựng scene Home (Assets/Scenes/Home.unity) gồm 3 màn: Trang chủ, Chọn map, Cài đặt.
    /// Bố cục theo images/screens/01_trang_chu.png, 02_chon_map.png, 07_cai_dat_huong_dan.png (khung 390 × 844 pt).
    /// Tự chạy một lần khi scene chưa tồn tại; chạy lại bằng menu WhackyZoo › Dựng lại scene Home.
    /// </summary>
    [InitializeOnLoad]
    public static class HomeSceneBuilder
    {
        const string GenDir = Art + "/Generated";

        static readonly Color Cream = Hex("F6F0E4");
        static readonly Color Orange = Hex("FF9F1C");
        static readonly Color TrackDark = Hex("3B3B3B"); // phần chưa kéo của thanh trượt (Guide25D .slider)
        static readonly Color MapSelectTop = Hex("FFF6DC");
        static readonly Color MapSelectBottom = Hex("F9C77D");

        static Sprite roundRect, circle, fade, mapSelectBg, mapSelectTopFade, mapSelectBottomFade, cardShape, cardMask, cardFrame, cardSoftShadow;

        static HomeSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(HomeScenePath) || SessionState.GetBool("WhackyZoo.HomeAutoBuilt", false)) return;
            SessionState.SetBool("WhackyZoo.HomeAutoBuilt", true);
            Build(additive: true);
        }

        [MenuItem("WhackyZoo/Dựng lại scene Home")]
        static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(additive: false);
        }

        static void Build(bool additive)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                LoadFonts();
                var config = CreateConfig();
                CreateMaps();
                var catalog = AssetDatabase.LoadAssetAtPath<MapCatalog>($"{DataRoot}/MapCatalog.asset");
                SoundBankBuilder.Create();
                GenerateSprites();
                BuildScene(config, catalog);
                EditorSceneManager.SaveScene(scene, HomeScenePath);
                UpdateBuildScenes();
                AssetDatabase.SaveAssets();
                Debug.Log($"[WhackyZoo] Đã dựng {HomeScenePath}. Mở scene Home và bấm Play để chơi từ Trang chủ.");
            }
            finally
            {
                if (additive && previous.IsValid())
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        // ───────────────────────── Ảnh phụ tự sinh ─────────────────────────

        static void GenerateSprites()
        {
            EnsureFolder(GenDir);
            roundRect = Gen("gen_round_rect", 128, 128, (x, y) => RoundRectAlpha(x, y, 128, 128, 48f));
            circle = Gen("gen_circle", 128, 128, (x, y) => RoundRectAlpha(x, y, 128, 128, 64f));
            fade = FadeSprite(); // trên đặc, dưới trong suốt
            mapSelectBg = MapSelectBackground();
            (mapSelectTopFade, mapSelectBottomFade) = MapSelectFades();
            // Thẻ map (MapSelect25D .card): bo 26 pt (78 px), viền 3 pt (9 px) — cùng một hình cho thân, mặt nạ và viền
            cardShape = Gen("gen_card26", 192, 192, (x, y) => RoundRectAlpha(x, y, 192, 192, 78f));
            cardFrame = Gen("gen_card26_frame", 192, 192, (x, y) =>
                Mathf.Clamp01(RoundRectAlpha(x, y, 192, 192, 78f) - (x < 9 || y < 9 || x >= 183 || y >= 183 ? 0f : RoundRectAlpha(x - 9, y - 9, 174, 174, 69f))));
            // Mặt nạ thân thẻ thụt vào 4 px (nằm dưới viền 9 px): stencil cắt theo mọi điểm ảnh có alpha > 0 nên mặt nạ
            // cùng cỡ viền làm ảnh nền lộ ra ở mép răng cưa ngoài góc bo.
            cardMask = Gen("gen_card26_mask", 192, 192, (x, y) =>
                x < 4 || y < 4 || x >= 188 || y >= 188 ? 0f : RoundRectAlpha(x - 4, y - 4, 184, 184, 74f));
            cardSoftShadow = CardSoftShadow();
        }

        // Nền trang Chọn map: radial-gradient(ellipse at 50% 0%, #FFF5D6 0%, #FFE2A0 55%, #F7C873 100%), cỡ farthest-corner
        static Color MapSelectColorAt(float xPt, float yPt)
        {
            float rx = 195f * Mathf.Sqrt(2f), ry = DesignH * Mathf.Sqrt(2f);
            float t = Mathf.Sqrt(Mathf.Pow((xPt - 195f) / rx, 2f) + Mathf.Pow(yPt / ry, 2f));
            return t < 0.55f ? Color.Lerp(Hex("FFF5D6"), Hex("FFE2A0"), t / 0.55f) : Color.Lerp(Hex("FFE2A0"), Hex("F7C873"), Mathf.Clamp01((t - 0.55f) / 0.45f));
        }

        static Sprite MapSelectBackground()
        {
            const int w = 390, h = 844; // màu chuyển rất mượt nên ảnh 1x là đủ, giãn ra toàn màn
            string path = $"{GenDir}/gen_mapselect_radial.png";
            if (!File.Exists(path))
            {
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++) px[y * w + x] = MapSelectColorAt(x + 0.5f, h - 1 - y + 0.5f);
                AmbientArtBuilder.Write(path, w, h, px);
            }
            return LoadSprite(path);
        }

        /// <summary>
        /// .fade-y của danh sách (mờ 14 px trên, 28 px dưới): Unity UI không có mask gradient nên phủ một dải
        /// mang đúng màu nền tại chỗ đó, alpha giảm dần về phía nội dung.
        /// </summary>
        static (Sprite top, Sprite bottom) MapSelectFades()
        {
            Sprite Band(string name, float y0, float y1, bool opaqueAtTop)
            {
                const int w = 390;
                int h = Mathf.RoundToInt((y1 - y0) * 3f);
                string path = $"{GenDir}/{name}.png";
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    float fromTop = (h - 1 - y + 0.5f) / h; // 0 = mép trên dải
                    float yPt = y0 + fromTop * (y1 - y0);
                    float a = opaqueAtTop ? 1f - fromTop : fromTop;
                    for (int x = 0; x < w; x++)
                    {
                        Color c = MapSelectColorAt(x + 0.5f, yPt);
                        c.a = a;
                        px[y * w + x] = c;
                    }
                }
                AmbientArtBuilder.Write(path, w, h, px);
                return LoadSprite(path);
            }
            return (Band("gen_mapselect_fade_top", 126f, 140f, true), Band("gen_mapselect_fade_bottom", 792f, 820f, false));
        }

        // Bóng mờ của thẻ: 0 16px 22px rgba(0,0,0,.22) quanh hình bo 26 — ảnh 9-slice có lề SoftBlur mỗi bên
        const float SoftBlur = 22f;

        static Sprite CardSoftShadow()
        {
            string path = $"{GenDir}/gen_card26_softshadow.png";
            if (!File.Exists(path))
            {
                int pad = Mathf.RoundToInt(SoftBlur * 3f), core = 192, n = core + 2 * pad;
                float sigma = SoftBlur * 3f / 2f;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // khoảng cách có dấu tới hình bo 78 px (âm = bên trong)
                        float qx = Mathf.Abs(x + 0.5f - n / 2f) - (core / 2f - 78f), qy = Mathf.Abs(y + 0.5f - n / 2f) - (core / 2f - 78f);
                        float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - 78f;
                        float a = 0.22f * (1f - Smooth(d / sigma)); // xấp xỉ bóng Gauss bằng đường cong mềm
                        px[y * n + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                    }
                AmbientArtBuilder.Write(path, n, n, px);
            }
            return LoadSprite(path);
        }

        static float Smooth(float z) => Mathf.Clamp01(0.5f + 0.5f * (float)System.Math.Tanh(z * 0.8f));

        /// <summary>Độ phủ của hình chữ nhật bo góc (khử răng cưa 1 px).</summary>
        static float RoundRectAlpha(int x, int y, int w, int h, float r)
        {
            float px = x + 0.5f, py = y + 0.5f;
            float cx = Mathf.Clamp(px, r, w - r), cy = Mathf.Clamp(py, r, h - r);
            float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        /// <summary>Sinh PNG trắng với alpha theo hàm (alpha &lt; 0 = dải màu nền Chọn map). Chỉ sinh khi chưa có file.</summary>
        static Sprite Gen(string name, int w, int h, Func<int, int, float> alpha)
        {
            string path = $"{GenDir}/{name}.png";
            if (!File.Exists(path))
            {
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float a = alpha(x, y);
                        px[y * w + x] = a < 0f
                            ? (Color32)Color.Lerp(MapSelectBottom, MapSelectTop, y / (float)(h - 1))
                            : new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return LoadSprite(path);
        }

        // ───────────────────────── Scene ─────────────────────────

        static void BuildScene(GameConfig config, MapCatalog catalog)
        {
            var root = CreateCanvas();
            var router = root.gameObject.AddComponent<ScreenRouter>();
            var bgHome = LoadSprite($"{Art}/Backgrounds/bg_home.png");

            var (home, homeContent) = Screen("HomeScreen", root, bgHome, envelope: true);
            var (mapSel, mapContent) = Screen("MapSelectScreen", root, mapSelectBg, envelope: false);
            var (settings, settingsContent) = Screen("SettingsScreen", root, bgHome, envelope: true);
            var (shop, shopContent) = Screen("ShopScreen", root, null, envelope: false);
            var (missions, missionsContent) = Screen("MissionsScreen", root, null, envelope: false);
            var (daily, dailyContent) = Screen("DailyRewardScreen", root, null, envelope: false);
            Set(router, "rootScreen", home);
            SetArray(router, "screens", new[] { home, mapSel, settings, shop, missions, daily });

            var boosters = BoosterBuilder.CreateBoosters();
            var shopCatalog = ShopBuilder.CreateShop(catalog, boosters);
            var shopView = ShopBuilder.Build(shop, shopContent, router, shopCatalog, daily);
            var progression = ProgressionBuilder.CreateProgression(boosters);
            var month = ProgressionBuilder.CreateDailyMonth(boosters, shopCatalog);
            ProgressionBuilder.BuildMissions(missions, missionsContent, router, mapSel, progression);
            ProgressionBuilder.BuildDaily(daily, dailyContent, router, month);
            BuildHome(home, homeContent, router, mapSel, settings, catalog, shop, shopView, shopCatalog, boosters, progression, missions, daily, month);
            BuildMapSelect(mapSel, mapContent, router, catalog);
            BuildSettings(settings, settingsContent, router, config);

            mapSel.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            shop.gameObject.SetActive(false);
            missions.gameObject.SetActive(false);
            daily.gameObject.SetActive(false);
            LocalizationBuilder.Tag(root); // chữ cố định đổi theo ngôn ngữ
        }

        /// <summary>Một màn: CanvasGroup phủ toàn màn, nền, và khung nội dung 390×844 neo trên để dùng toạ độ ảnh thiết kế.</summary>
        static (CanvasGroup, RectTransform) Screen(string name, Transform root, Sprite bg, bool envelope)
        {
            var rt = Rect(name, root, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(rt, 0);
            var group = rt.gameObject.AddComponent<CanvasGroup>();

            var bgImg = Img("Background", rt, bg, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            if (envelope)
            {
                var fit = bgImg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 1170f / 2532f;
            }
            else Stretch(bgImg.rectTransform, 0);

            var content = Rect("Content", rt, Top, Center, TopPos(195, 422), new Vector2(DesignW, DesignH));
            return (group, content);
        }

        static Vector2 P(float cx, float cy) => CenterPos(cx, cy);

        static Button IconButton(string name, Transform parent, string icon, Vector2 posPt, float sizePt = 52)
        {
            var b = Btn(name, parent, Spr("UI/button_white"), Center, posPt, new Vector2(sizePt, sizePt), null, 0, Dark, null);
            Img("Icon", b.transform, Spr("UI/" + icon), Center, Center, new Vector2(0, 2), new Vector2(28, 28));
            return b;
        }

        static Text ScreenTitle(Transform parent, string text)
        {
            var t = Txt("Title", parent, text, fontHeavy, 32, Color.white, TextAnchor.MiddleLeft, Center, P(218, 82), new Vector2(260, 48));
            AddOutline(t, Dark, 8);
            return t;
        }

        static Image Panel(string name, Transform parent, Vector2 posPt, Vector2 sizePt)
        {
            var p = Img(name, parent, Spr("UI/panel_cream"), Center, Center, posPt, sizePt);
            Sliced(p, 1.2f);
            return p;
        }

        // ───────────────────────── Trang chủ ─────────────────────────
        // Bố cục theo Main25D.dc.html: lề 56/20/24, các khối cách nhau 18, bề rộng nội dung 350 (x 20–370).
        // Toạ độ y (tâm, pt) tính theo flex của thiết kế và đã so với images/screens/01_trang_chu.png.
        static readonly Color HomeMuted = Hex("6B5A45");

        static void BuildHome(CanvasGroup screen, RectTransform c, ScreenRouter router, CanvasGroup mapSel, CanvasGroup settings, MapCatalog catalog,
            CanvasGroup shopScreen, ShopScreen shopView, ShopCatalog shopCatalog, BoosterCatalog boosters,
            ProgressionConfig progression, CanvasGroup missionsScreen, CanvasGroup dailyScreen, DailyRewardMonth month)
        {
            // Nền sạch + 2 đám mây trôi
            var bg = screen.transform.Find("Background").GetComponent<Image>();
            bg.sprite = AmbientArtBuilder.HomeCleanBackground();
            var ambientRt = Rect("Ambient", bg.transform, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            var ambient = ambientRt.gameObject.AddComponent<AmbientFxView>();
            var clouds = AmbientArtBuilder.HomeClouds();
            var field = typeof(AmbientFxView).GetField("elements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(ambient, clouds); // mảng lớp thường: gán thẳng rồi đánh dấu thay đổi để lưu vào scene
            EditorUtility.SetDirty(ambient);

            // Hàng đầu (cao 56, tâm y 84): ô Lv, ô xu; nút cài đặt 46 + viền 3 = 52, bo 14
            var settingsBtn = IconButton("SettingsButton", c, "icon_settings_gear", P(344, 84), 52);

            // Ô Lv (.panel bo tròn, đệm 3/14/3/3): avatar tròn 44 (#FFF0F4, viền 3), cách 10, "Lv N" 15 + thanh KN 64 × 9 cách 4.
            // Rộng 141 (x 20–161), cao 56 (avatar 44 + đệm 3 + viền 3 mỗi bên, y 56–112) + bóng 4. Avatar tạm là Chó (chọn avatar thuộc bước 6).
            Img("LevelChip", c, UiArt.Box("home_level_chip", 141, 56, 28, 3, Dark, CardWhite, 4, Hex("B9A98C")), Center, new Vector2(0.5f, 1f), P(90.5f, 84 - 28), new Vector2(141, 60));
            Img("Avatar", c, UiArt.Box("home_avatar", 44, 44, 22, 3, Dark, new[] { new UiArt.Stop(0f, "FFF0F4") }), Center, Center, P(20 + 3 + 3 + 22, 84), new Vector2(44, 44));
            var landForAvatar = catalog.Find("land");
            if (landForAvatar != null && landForAvatar.normalCritters.Length > 0)
                Img("AvatarAnimal", c, landForAvatar.normalCritters[0].AnimSprite, Center, Center, P(48, 85), new Vector2(36, 36)).preserveAspect = true;
            var levelText = Txt("Level", c, "Lv 1", fontHeavy, 15, Dark, TextAnchor.MiddleLeft, Center, P(80 + 40, 77.5f), new Vector2(80, 18));
            var xpTrack = Img("XpBar", c, UiArt.Box("home_xp_track", 20, 9, 4.5f, 0, Dark, new[] { new UiArt.Stop(0f, "CFC5B2"), new UiArt.Stop(1f, "E9E1D0") }, sliceX: true),
                Center, Center, P(112, 93.5f), new Vector2(64, 9));
            xpTrack.type = Image.Type.Sliced;
            var xpFill = Img("Fill", xpTrack.transform, UiArt.Box("home_xp_fill", 20, 9, 4.5f, 0, Dark,
                new[] { new UiArt.Stop(0f, "A6F09A"), new UiArt.Stop(0.6f, "4CC25E"), new UiArt.Stop(1f, "2E9E44") }, sliceX: true), Center, Center, Vector2.zero, Vector2.zero);
            xpFill.type = Image.Type.Sliced;
            xpFill.rectTransform.anchorMin = Vector2.zero;
            xpFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            xpFill.rectTransform.offsetMin = xpFill.rectTransform.offsetMax = Vector2.zero;

            // Ô xu (.panel bo tròn): đệm 5/12/5/7 + viền 3, icon xu 28, cách 6, số ExtraBold 17; cao 44, cách nút cài đặt 10
            // → mép phải x 308. Rộng co theo số xu.
            var coinChip = Img("CoinChip", c, UiArt.Box("home_coin_chip", 60, 44, 22, 3, Dark, CardWhite, 4, Hex("B9A98C"), sliceX: true),
                Center, new Vector2(1f, 1f), P(308, 84 - 22), new Vector2(110, 48));
            coinChip.type = Image.Type.Sliced;
            var coinRow = coinChip.gameObject.AddComponent<HorizontalLayoutGroup>();
            coinRow.padding = new RectOffset(Mathf.RoundToInt(10 * PT), Mathf.RoundToInt(15 * PT), 0, Mathf.RoundToInt(4 * PT));
            coinRow.spacing = 6 * PT;
            coinRow.childAlignment = TextAnchor.MiddleLeft;
            coinRow.childControlWidth = true;
            coinRow.childControlHeight = false;
            coinRow.childForceExpandWidth = coinRow.childForceExpandHeight = false;
            coinChip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var coinShadow = Img("SoftShadow", coinChip.transform, UiArt.SoftShadow("home_coin_chip_soft", 110, 44, 22, 12, 0.18f), Center, Center, Vector2.zero, Vector2.zero);
            coinShadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            coinShadow.rectTransform.anchorMin = Vector2.zero;
            coinShadow.rectTransform.anchorMax = Vector2.one;
            coinShadow.rectTransform.offsetMin = new Vector2(-12, -12 - 8 + 4) * PT; // bóng 0 8px 12px, ảnh nút đã gồm cạnh 4
            coinShadow.rectTransform.offsetMax = new Vector2(12, 12 - 8) * PT;
            coinShadow.transform.SetAsFirstSibling();
            var coinIcon = Img("Icon", coinChip.transform, Spr("UI/icon_coin"), Center, Center, Vector2.zero, new Vector2(28, 28));
            coinIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 28 * PT;
            var coinsText = Txt("Coins", coinChip.transform, "0", fontHeavy, 17, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(60, 26));

            // Logo: 3 thú 72 / 84 / 72 cách nhau 6, từ y 148 (căn trên); chữ bắt đầu ở y 212 (margin −20)
            var land = catalog.Find("land");
            var animalRts = new List<RectTransform>();
            var animalImgs = new List<Image>();
            var animalData = new List<CritterData>();
            (float cx, float cy, float size)[] slots = { (114, 148 + 36, 72), (195, 148 + 42, 84), (276, 148 + 36, 72) };
            for (int i = 0; i < slots.Length && land != null && i < land.normalCritters.Length; i++)
            {
                var d = land.normalCritters[i];
                var img = Img($"LogoAnimal{i}", c, d.AnimSprite, Center, Center, P(slots[i].cx, slots[i].cy), Vector2.one * slots[i].size);
                img.preserveAspect = true;
                animalRts.Add(img.rectTransform);
                animalImgs.Add(img);
                animalData.Add(d);
            }
            // Khối chữ (.logo): "ĐẬP THÚ" 68 (cao dòng 0,95) rồi "VUI NHỘN" 34 cách 4 → tâm khối y 263; xoay −3°
            var logoRt = Rect("LogoText", c, Center, Center, P(195, 263.3f), new Vector2(DesignW, 103));
            logoRt.localRotation = Quaternion.Euler(0, 0, 3f);
            var logoGroup = logoRt.gameObject.AddComponent<CanvasGroup>();
            var title = Txt("Title", logoRt, "ĐẬP THÚ", fontHeavy, 68, Color.white, TextAnchor.MiddleCenter, Center, new Vector2(0, 19), new Vector2(DesignW, 66));
            title.gameObject.AddComponent<LetterSpacing>().Spacing = 1 * PT;
            title.gameObject.AddComponent<TextStroke>().Configure(Dark, 3 * PT, new Vector2(0, -5 * PT));
            var subtitle = Txt("Subtitle", logoRt, "VUI NHỘN", fontHeavy, 34, Hex("FF7A4A"), TextAnchor.MiddleCenter, Center, new Vector2(0, -34.3f), new Vector2(DesignW, 36));
            subtitle.gameObject.AddComponent<LetterSpacing>().Spacing = 4 * PT;
            subtitle.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));

            // Thẻ "Đang dùng" (tâm y 389,6; cao 98): ô icon 66 + viền = 72, chữ 13 / 20 / 13, nút Đổi cao 44 + viền
            const float toolY = 389.6f;
            Panel("ToolCard", c, P(195, toolY), new Vector2(350, 98));
            var iconBox = Img("ToolIconBox", c, Spr("UI/button_white"), Center, Center, P(71, toolY), new Vector2(72, 72));
            Sliced(iconBox, 2f);
            iconBox.color = Hex("FFE6A0");
            var hammer = Img("ToolIcon", c, Spr("Tools/tool_wood_hammer"), Center, new Vector2(0.5f, 0.2f), P(71, toolY + 0.3f * 56), new Vector2(56, 56));
            // 2 dòng (Main25D, line-height 1,15): tên dụng cụ ExtraBold 20 #3A2412 (dòng 23) rồi "Đang dùng" Bold 13 #6B5A45 (dòng 15),
            // khối cao 38 căn giữa theo ô icon → tâm dòng trên −7,5, dòng dưới +11,5 so với tâm thẻ
            var toolName = Txt("ToolName", c, "Búa Gỗ", fontHeavy, 20, Dark, TextAnchor.MiddleLeft, Center, P(119 + 90, toolY - 7.5f), new Vector2(180, 26));
            var toolStatus = Txt("ToolStatus", c, "Đang dùng", fontBold, 13, HomeMuted, TextAnchor.MiddleLeft, Center, P(119 + 90, toolY + 11.5f), new Vector2(180, 18));
            var swapTool = Btn("SwapToolButton", c, Spr("UI/button_yellow"), Center, P(323, toolY), new Vector2(64, 50), "Đổi", 16, Dark, null);

            // CHƠI NGAY (tâm y 514,6; cao 98 + viền): tam giác trắng viền nâu 34 + cách 8 + chữ 36 viền 5, thở + vệt sáng
            var play = Btn("PlayButton", c, Spr("UI/button_green"), Center, P(195, 515), new Vector2(350, 106), null, 0, Color.white, null);
            play.gameObject.AddComponent<Mask>().showMaskGraphic = true; // vệt sáng chỉ hiện trong nút
            var shineArea = Rect("Shine", play.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(shineArea, 0);
            var shine = Img("Band", shineArea, ShineSprite(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-60, 0), new Vector2(40, 106 * 1.4f));
            var shineSkew = shine.gameObject.AddComponent<UISkew>();
            shineSkew.OriginY = 0.5f;
            shineSkew.Angle = -20f;
            var content = Rect("Content", play.transform, Center, Center, new Vector2(0, 3), new Vector2(350, 60));
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8 * PT;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var playIcon = Img("Icon", content, PlayIconSprite(), Center, Center, Vector2.zero, new Vector2(34, 34));
            playIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 34 * PT;
            var playLabel = Txt("Label", content, "CHƠI NGAY", fontHeavy, 36, Color.white, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(240, 50));
            playLabel.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -3 * PT));

            // 3 ô tính năng (tâm y 631,6; rộng 108,7, cao 88 + viền, cách 12): icon 32, cách 2, chữ 15
            Button Tile(string name, string icon, string label, float cx)
            {
                var b = Btn(name, c, Spr("UI/button_white"), Center, P(cx, 631.6f), new Vector2(108.7f, 94), null, 0, Dark, null);
                Img("Icon", b.transform, Spr("UI/" + icon), Center, Center, new Vector2(0, 9.6f + 2), new Vector2(32, 32));
                Txt("Label", b.transform, label, fontHeavy, 15, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, -17 + 2), new Vector2(108, 22));
                return b;
            }
            var shop = Tile("ShopButton", "icon_shop_cart", "Cửa hàng", 74.3f);
            var leaderboard = Tile("LeaderboardButton", "icon_leaderboard", "Xếp hạng", 195);
            var missions = Tile("MissionsButton", "icon_missions", "Nhiệm vụ", 315.7f);
            // Chấm đỏ số nhiệm vụ chờ nhận: tròn tối thiểu 26, viền 2, bóng 0 2px #8E1F1A, chữ 14 trắng; top −10 right −8
            var badge = PillLabel("Badge", missions.transform, UiArt.Box("home_badge", 26, 26, 13, 2, Dark,
                new[] { new UiArt.Stop(0f, "FF8A80"), new UiArt.Stop(1f, "E53935") }, 2, Hex("8E1F1A"), sliceX: true), "3", 14, Color.white, 28, 4);
            var badgeRt = (RectTransform)badge.transform.parent;
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(8f, 10f) * PT;
            badgeRt.gameObject.AddComponent<LayoutElement>().minWidth = 26 * PT;
            badgeRt.gameObject.SetActive(false);

            // Điểm danh (đáy 820, cao 76): icon 40 lắc, chữ 17 / 13 ("Ngày 12/30 · +250 xu"), nút Nhận cao 44 + viền
            const float giftY = 782f;
            Panel("DailyGiftCard", c, P(195, giftY), new Vector2(350, 76));
            var gift = Img("GiftIcon", c, Spr("UI/icon_daily_gift"), Center, new Vector2(0.5f, 0.2f), P(57, giftY + 0.3f * 40), new Vector2(40, 40));
            Txt("GiftTitle", c, "Điểm danh", fontHeavy, 17, Dark, TextAnchor.MiddleLeft, Center, P(89 + 90, giftY - 8.5f), new Vector2(180, 24));
            var giftSub = Txt("GiftSub", c, "Ngày 1/30 · +100 xu", fontBold, 13, HomeMuted, TextAnchor.MiddleLeft, Center, P(89 + 90, giftY + 10.2f), new Vector2(180, 18));
            var claim = Btn("ClaimGiftButton", c, Spr("UI/button_yellow"), Center, P(319, giftY), new Vector2(76, 50), "Nhận", 16, Dark, null);

            var view = screen.gameObject.AddComponent<HomeScreen>();
            Set(view, "router", router);
            Set(view, "mapSelectScreen", mapSel);
            Set(view, "settingsScreen", settings);
            Set(view, "playButton", play);
            Set(view, "settingsButton", settingsBtn);
            Set(view, "coinsText", coinsText);
            Set(view, "boosters", boosters);
            Set(view, "shopScreen", shopScreen);
            Set(view, "shopView", shopView);
            Set(view, "shopCatalog", shopCatalog);
            Set(view, "shopButton", shop);
            Set(view, "swapToolButton", swapTool);
            Set(view, "toolName", toolName);
            Set(view, "toolIcon", hammer);
            Set(view, "toolStatus", toolStatus);
            Set(view, "month", month);
            SetArray(view, "comingSoon", new Selectable[] { leaderboard });
            Set(view, "progression", progression);
            Set(view, "levelText", levelText);
            Set(view, "xpFill", xpFill.rectTransform);
            Set(view, "missionsScreen", missionsScreen);
            Set(view, "missionsButton", missions);
            Set(view, "missionsBadge", badgeRt.gameObject);
            Set(view, "missionsBadgeText", badge);
            Set(view, "dailyScreen", dailyScreen);
            Set(view, "dailyButton", claim);
            Set(view, "dailyButtonText", claim.transform.Find("Label").GetComponent<Text>());
            Set(view, "dailySubtitle", giftSub);

            var anim = screen.gameObject.AddComponent<HomeAnimator>();
            Set(anim, "logoText", logoRt);
            Set(anim, "logoGroup", logoGroup);
            SetArray(anim, "animals", animalRts);
            SetArray(anim, "animalImages", animalImgs);
            SetArray(anim, "animalData", animalData);
            Set(anim, "playButton", (RectTransform)play.transform);
            Set(anim, "shine", shine.rectTransform);
            SetArray(anim, "wiggles", new[] { hammer.rectTransform, gift.rectTransform });
        }

        /// <summary>Dải sáng .shine: linear-gradient(90deg, trong suốt, trắng 65%, trong suốt).</summary>
        static Sprite ShineSprite() => Gen("gen_shine", 64, 4, (x, y) => 0.65f * (1f - Mathf.Abs((x + 0.5f) / 32f - 1f)));

        /// <summary>Icon nút CHƠI NGAY: path "M7 4.5 L19 12 L7 19.5 Z" (viewBox 24), ruột trắng, viền #3A2412 dày 2,2, góc tròn.</summary>
        static Sprite PlayIconSprite()
        {
            string path = $"{GenDir}/gen_play_icon.png";
            if (!File.Exists(path))
            {
                const int n = 102; // 34 pt @3x
                float k = n / 24f;
                Vector2[] tri = { new Vector2(7, 4.5f), new Vector2(19, 12), new Vector2(7, 19.5f) };
                var px = new Color32[n * n];
                for (int py = 0; py < n; py++)
                    for (int x = 0; x < n; x++)
                    {
                        int fill = 0, stroke = 0;
                        for (int j = 0; j < 4; j++)
                            for (int i = 0; i < 4; i++)
                            {
                                var p = new Vector2((x + (i + 0.5f) / 4f) / k, (n - 1 - py + (j + 0.5f) / 4f) / k); // y xuống như SVG
                                bool inside = InTriangle(p, tri);
                                float d = float.MaxValue;
                                for (int e = 0; e < 3; e++) d = Mathf.Min(d, SegDist(p, tri[e], tri[(e + 1) % 3]));
                                if (d <= 1.1f) stroke++;
                                else if (inside) fill++;
                            }
                        float a = (fill + stroke) / 16f;
                        Color col = stroke + fill == 0 ? Color.clear : Color.Lerp(Color.white, Dark, stroke / (float)(stroke + fill));
                        col.a = a;
                        px[py * n + x] = col;
                    }
                AmbientArtBuilder.Write(path, n, n, px);
            }
            return LoadSprite(path);
        }

        static bool InTriangle(Vector2 p, Vector2[] t)
        {
            float S(Vector2 a, Vector2 b, Vector2 c) => (a.x - c.x) * (b.y - c.y) - (b.x - c.x) * (a.y - c.y);
            float d1 = S(p, t[0], t[1]), d2 = S(p, t[1], t[2]), d3 = S(p, t[2], t[0]);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ───────────────────────── Chọn map ─────────────────────────

        static void BuildMapSelect(CanvasGroup screen, RectTransform c, ScreenRouter router, MapCatalog catalog)
        {
            // Hàng tiêu đề (MapSelect25D): lề trên 56, nút quay lại 48, tiêu đề cỡ 30 cách nút 12
            // nút 48 + viền 3 × 2 = 54 (tâm 47, 83); tiêu đề 30, viền 5 px (thấy 2,5) + bóng 0 4px, bắt đầu ở x 20 + 54 + 12 = 86
            var back = IconButton("BackButton", c, "icon_back", P(47, 83), 54);
            var title = Txt("Title", c, "CHỌN MAP", fontHeavy, 30, Color.white, TextAnchor.MiddleLeft, Center, P(86 + 130, 83), new Vector2(260, 44));
            title.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));

            // Danh sách cuộn: từ dưới tiêu đề tới đáy màn, không thanh cuộn, mép mờ dần
            var scrollRt = Rect("Scroll", screen.transform, Center, Center, Vector2.zero, Vector2.zero);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0, 24 * PT);   // lề dưới trang 24
            scrollRt.offsetMax = new Vector2(0, -126 * PT); // dưới hàng tiêu đề (56 + 54 + 16)
            scrollRt.gameObject.AddComponent<RectMask2D>();
            scrollRt.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0); // nhận kéo ở khoảng trống
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var content = Rect("Content", scrollRt, Top, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(DesignW, 0));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = new Vector2(0, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, Mathf.RoundToInt(4 * PT), Mathf.RoundToInt(18 * PT));
            layout.spacing = 16 * PT;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = scrollRt;
            scroll.content = content;

            var template = BuildMapCard(content, catalog.maps[0]);
            var soon = BuildComingSoonCard(content);

            // Danh sách cuộn chỉ mờ mép dưới (người dùng chốt), bỏ dải mờ 14 px ở mép trên của .fade-y
            var bottomFade = Img("BottomFade", scrollRt, mapSelectBottomFade, Bottom, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(DesignW, 28));
            bottomFade.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomFade.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomFade.rectTransform.sizeDelta = new Vector2(0, 28 * PT);

            var view = screen.gameObject.AddComponent<MapSelectScreen>();
            Set(view, "router", router);
            Set(view, "catalog", catalog);
            Set(view, "backButton", back);
            Set(view, "content", content);
            Set(view, "cardTemplate", template);
            Set(view, "comingSoonCard", soon);
            Set(view, "scroll", scroll);
        }

        // Thẻ map theo MapSelect25D .card: rộng 350 (lề trang 20), viền 3, bo 26, ảnh nền 344 × 122,
        // phần dưới padding 12/16/14 + nút 48 + viền 6 = 54 → cao 3 + 122 + 80 + 3 = 208 (đo ảnh 02: 207,5);
        // bóng cứng 0 6px #B9A98C + bóng mờ 0 16px 22px rgba(0,0,0,.22)
        const float CardW = 350, CardH = 208, Border = 3, BannerW = 344, BannerH = 122, Pad = 16;
        const float LowerH = CardH - 2 * Border - BannerH;
        // 4 thú 64 × 64, cách nhau 2, căn giữa, cách đáy ảnh nền 8
        static readonly float[] AnimalX = { -99f, -33f, 33f, 99f };
        const float AnimalSize = 64, AnimalBottom = 8;

        /// <summary>
        /// Khung thẻ: gốc (do layout đặt chỗ) → Visual (chạy hiệu ứng trượt vào / nhấn lún, có CanvasGroup)
        /// → bóng đổ, thân (Mask cắt theo góc bo 26, nền trắng → #F5EFE3), sau cùng là viền vẽ đè.
        /// </summary>
        static (RectTransform root, RectTransform visual, CanvasGroup group, RectTransform body, RectTransform shadow, RectTransform soft) CardShell(
            string name, Transform parent, float height, Color top, Color bottom)
        {
            var root = Rect(name, parent, Center, Center, Vector2.zero, new Vector2(CardW, height));
            var visual = Rect("Visual", root, Center, Center, Vector2.zero, new Vector2(CardW, height));
            var group = visual.gameObject.AddComponent<CanvasGroup>();

            var soft = Img("SoftShadow", visual, cardSoftShadow, Center, Center, new Vector2(0, -16), new Vector2(CardW + 2 * SoftBlur, height + 2 * SoftBlur));
            Sliced(soft, 1f);
            var shadow = Img("Shadow", visual, cardShape, Center, Center, new Vector2(0, -6), new Vector2(CardW, height));
            Sliced(shadow, 1f);
            shadow.color = Hex("B9A98C");

            var body = Img("Body", visual, cardMask, Center, Center, Vector2.zero, new Vector2(CardW, height));
            Sliced(body, 1f);
            body.color = bottom;
            body.raycastTarget = true; // nhận chạm cho cả thẻ
            body.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var sheen = Img("Sheen", body.rectTransform, fade, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(sheen.rectTransform, 0);
            sheen.color = top;
            return (root, visual, group, body.rectTransform, shadow.rectTransform, soft.rectTransform);
        }

        static void CardFrame(RectTransform visual, float height)
        {
            var frame = Img("Frame", visual, cardFrame, Center, Center, Vector2.zero, new Vector2(CardW, height));
            Sliced(frame, 1f);
            frame.color = Dark;
        }

        static MapCard BuildMapCard(Transform parent, MapData preview)
        {
            var (root, visual, group, body, shadowRt, softRt) = CardShell("CardTemplate", parent, CardH, Color.white, Hex("F5EFE3"));

            // Ảnh nền thẻ (map_cards/card_bg_*), cắt theo khung ảnh như overflow:hidden
            var clip = Rect("BannerClip", body, Top, new Vector2(0.5f, 1f), new Vector2(0, -Border), new Vector2(BannerW, BannerH));
            clip.gameObject.AddComponent<RectMask2D>();
            var banner = Img("Banner", clip, preview.banner != null ? preview.banner : preview.background, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(banner.rectTransform, 0);

            // Hiệu ứng nền của map (MapData.cardEffects): trên ảnh nền, dưới viền và 4 thú; bị cắt theo khung ảnh + góc bo của thân thẻ.
            // Ba lớp chứa: không cắt thêm / theo mặt nạ của map (vùng nước) / dải trời phía trên đầu thú.
            var fxRoot = Rect("Effects", clip, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(fxRoot, 0);
            var fxView = fxRoot.gameObject.AddComponent<CardFxView>();
            var plain = Rect("Plain", fxRoot, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(plain, 0);
            var maskImg = Img("MapMask", fxRoot, preview.cardSkyMask, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(maskImg.rectTransform, 0);
            maskImg.raycastTarget = false;
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var sky = Rect("SkyBand", fxRoot, Center, Center, Vector2.zero, Vector2.zero);
            var skyMask = sky.gameObject.AddComponent<RectMask2D>();
            Set(fxView, "plainLayer", plain);
            Set(fxView, "mapMask", maskImg);
            Set(fxView, "skyBand", skyMask);

            CardFrame(visual, CardH);

            // 4 thú lơ lửng
            var animals = new Image[4];
            var animalRts = new RectTransform[4];
            float animalY = CardH / 2f - (Border + BannerH - AnimalBottom - AnimalSize / 2f);
            for (int i = 0; i < 4; i++)
            {
                animals[i] = Img($"Animal{i}", visual, preview.normalCritters[i].sprite, Center, Center, new Vector2(AnimalX[i], animalY), new Vector2(AnimalSize, AnimalSize));
                animals[i].preserveAspect = true;
                animalRts[i] = animals[i].rectTransform;
            }

            // Phần dưới: tên map 24 + nhãn MỚI, nút "Chọn" (cao 48, chữ 18)
            float lowerY = CardH / 2f - (Border + BannerH + LowerH / 2f);
            var nameRow = Rect("NameRow", visual, Center, new Vector2(0f, 0.5f), new Vector2(-CardW / 2f + Border + Pad, lowerY), new Vector2(220, 32));
            var row = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8 * PT;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var nameText = Txt("Name", nameRow, preview.displayName, fontHeavy, 24, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(160, 32));
            // Nhãn MỚI phẳng (MapSelect25D): nền #E53935, bo 8, cao 22, đệm ngang 8, chữ trắng ExtraBold 13; không viền/bóng
            var badge = Img("NewBadge", nameRow, roundRect, Center, Center, Vector2.zero, new Vector2(40, 22));
            Sliced(badge, 2f); // góc 48 px của gen_round_rect ÷ 2 = 24 đơn vị = 8 pt
            badge.color = Hex("E53935");
            var badgeLayout = badge.gameObject.AddComponent<HorizontalLayoutGroup>();
            int badgePad = Mathf.RoundToInt(8 * PT);
            badgeLayout.padding = new RectOffset(badgePad, badgePad, 0, 0);
            badgeLayout.childAlignment = TextAnchor.MiddleCenter;
            badgeLayout.childControlWidth = true;
            badgeLayout.childControlHeight = false;
            badgeLayout.childForceExpandWidth = badgeLayout.childForceExpandHeight = false;
            Txt("Text", badge.transform, "MỚI", fontHeavy, 13, Color.white, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(24, 22));

            // .gbtn "Chọn": cao 48 + viền 6 = 54, đệm 20, chữ 18 → rộng ~88 (đo ảnh 02); sprite nút có thêm cạnh 3D 5 pt phía dưới
            const float ChooseW = 88, ChooseH = 54, ChooseEdge = 5;
            var choose = Img("Choose", visual, Spr("UI/button_green"), Center, Center, new Vector2(CardW / 2f - Border - Pad - ChooseW / 2f, lowerY - ChooseEdge / 2f), new Vector2(ChooseW, ChooseH + ChooseEdge));
            Sliced(choose, 1.4f);
            Txt("Label", choose.transform, "Chọn", fontHeavy, 18, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, ChooseEdge / 2f), new Vector2(ChooseW, ChooseH));

            // Cả thẻ là một nút (như <a class="card">); hiệu ứng nhấn do MapCardAnimator lo nên tắt đổi màu
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = body.GetComponent<Image>();

            var anim = root.gameObject.AddComponent<MapCardAnimator>();
            Set(anim, "visual", visual);
            Set(anim, "visualGroup", group);
            SetArray(anim, "animals", animalRts);
            Set(anim, "shadow", shadowRt);
            Set(anim, "softShadow", softRt);

            var view = root.gameObject.AddComponent<MapCard>();
            Set(view, "banner", banner);
            SetArray(view, "animals", animals);
            Set(view, "nameText", nameText);
            Set(view, "newBadge", badge.gameObject);
            Set(view, "button", button);
            Set(view, "effects", fxView);
            return view;
        }

        /// <summary>Thẻ "Map mới · Sắp ra mắt": ổ khoá 40 + 2 dòng chữ, nền #F3EEE4 → #E3DACA, padding 14/16.</summary>
        static MapCardAnimator BuildComingSoonCard(Transform parent)
        {
            const float H = 74;
            var (root, visual, group, _, shadowRt, softRt) = CardShell("ComingSoonCard", parent, H, Hex("F3EEE4"), Hex("E3DACA"));
            CardFrame(visual, H);
            float left = -CardW / 2f + Border + Pad;
            Img("Lock", visual, Spr("UI/icon_lock"), Center, Center, new Vector2(left + 20, 0), new Vector2(40, 40));
            float textX = left + 40 + 14 + 100;
            Txt("Name", visual, "Map mới", fontHeavy, 20, Dark, TextAnchor.MiddleLeft, Center, new Vector2(textX, 10), new Vector2(200, 26));
            Txt("Sub", visual, "Sắp ra mắt", fontBold, 14, Hex("5E4E3A"), TextAnchor.MiddleLeft, Center, new Vector2(textX, -12), new Vector2(200, 20));

            var anim = root.gameObject.AddComponent<MapCardAnimator>();
            Set(anim, "visual", visual);
            Set(anim, "visualGroup", group);
            Set(anim, "shadow", shadowRt);
            Set(anim, "softShadow", softRt);
            return anim;
        }

        // ───────────────────────── Cài đặt (Guide25D.dc.html) ─────────────────────────
        // Lề 56/16/24; 3 thẻ cách nhau 12 (đo ảnh 07: 120–241,5 / 254–429,5 / 442–559,5): thẻ trắng → #F3EEE4, viền 3, bo 22,
        // nút <button> tính viền vào chiều cao (border-box): chọn giờ 56, ngôn ngữ 52, quay lại 48; dòng tiêu đề thẻ cao 27,5.
        // bóng 0 4px #B9A98C + 0 8px 12px; đệm 10/14/14; tiêu đề thẻ = icon 24 + cách 8 + chữ 18.
        static readonly UiArt.Stop[] CardWhite = { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "F3EEE4") };

        static void BuildSettings(CanvasGroup screen, RectTransform c, ScreenRouter router, GameConfig config)
        {
            // Nền sạch + mây trôi ở vị trí riêng của màn này
            var bg = screen.transform.Find("Background").GetComponent<Image>();
            bg.sprite = AmbientArtBuilder.HomeCleanBackground();
            var ambientRt = Rect("Ambient", bg.transform, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            var ambient = ambientRt.gameObject.AddComponent<AmbientFxView>();
            typeof(AmbientFxView).GetField("elements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ambient, AmbientArtBuilder.SettingsClouds());
            EditorUtility.SetDirty(ambient);

            // Đầu trang: nút 48 (tâm 40, 80); "CÀI ĐẶT" 32 trắng viền 5 + bóng 4, bắt đầu ở x 16 + 48 + 12 = 76
            var back = IconButton("BackButton", c, "icon_back", P(40, 80), 48);
            var title = Txt("Title", c, "CÀI ĐẶT", fontHeavy, 32, Color.white, TextAnchor.MiddleLeft, Center, P(76 + 130, 80), new Vector2(260, 44));
            title.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));

            // Các thẻ trượt lên + mờ dần vào mỗi lần mở màn (.enter 0,5 s)
            var cards = Rect("Cards", c, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            cards.gameObject.AddComponent<CanvasGroup>();
            cards.gameObject.AddComponent<UIEnter>();

            void Card(string name, float top, float h)
            {
                Img(name + "Shadow", cards, UiArt.SoftShadow($"settings_{name}_soft", 358, h, 22, 12, 0.18f), Center, Center, P(195, top + h / 2f + 8), new Vector2(358 + 24, h + 24));
                Img(name, cards, UiArt.Box($"settings_{name}", 358, h, 22, 3, Dark, CardWhite, 4, Hex("B9A98C")),
                    Center, new Vector2(0.5f, 1f), P(195, top), new Vector2(358, h + 4));
            }
            void Header(Sprite icon, string text, float y)
            {
                Img("HeaderIcon", cards, icon, Center, Center, P(33 + 12, y), new Vector2(24, 24));
                Txt("Header", cards, text, fontHeavy, 18, Dark, TextAnchor.MiddleLeft, Center, P(65 + 120, y), new Vector2(240, 28));
            }

            // Thẻ 1 — Thời gian mỗi ván (y 120, cao 121,5): 3 nút cách 10, cao 56 (gồm viền); chữ 22 / 12
            Card("RoundCard", 120, 121.5f);
            Header(Spr("UI/icon_timer"), "Thời gian mỗi ván", 146.75f);
            string[] names = { "Nhanh", "Thường", "Thử thách" };
            int count = Mathf.Min(3, config.roundSecondsOptions.Length);
            const float optW = (324f - 20f) / 3f;
            var roundButtons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                float cx = 33 + optW / 2f + i * (optW + 10);
                var b = GBtn($"Round{config.roundSecondsOptions[i]}", cards, Spr("UI/button_white"), Center, P(cx, 197f), optW, 56, null, 0, Dark, null, 0);
                // chữ trong mặt nút (tâm mặt cao hơn tâm sprite 2,5 pt vì cạnh 3D phía dưới)
                Txt("Seconds", b.transform, config.roundSecondsOptions[i] + "s", fontHeavy, 22, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 2.5f + 7), new Vector2(optW, 26));
                Txt("Name", b.transform, i < names.Length ? names[i] : "", fontBold, 12, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 2.5f - 12), new Vector2(optW, 16));
                roundButtons[i] = b;
            }

            // Thẻ 2 — Âm thanh (y 254, cao 175,5): lưới 92 | thanh | 40 cách 10, hàng cao 24 cách 8; nét đứt rồi công tắc rung
            Card("SoundCard", 254f, 175.5f);
            Header(UiArt.SpeakerIcon(), "Âm thanh", 280.75f);
            const float sliderW = 324f - 92f - 40f - 20f;
            // Giá trị mặc định lấy từ SaveData (Nhạc nền 70%, Hiệu ứng 85%); SettingsScreen nạp lại giá trị đã lưu khi mở
            var defaults = new SettingsData();
            Text Row(string key, string label, float y, float value, out Slider slider)
            {
                Txt(key + "Label", cards, label, fontHeavy, 15, Dark, TextAnchor.MiddleLeft, Center, P(33 + 46, y), new Vector2(92, 22));
                slider = BuildSlider(key + "Slider", cards, P(33 + 92 + 10 + sliderW / 2f, y), sliderW);
                slider.value = value;
                return Txt(key + "Value", cards, Mathf.RoundToInt(value * 100f) + "%", fontHeavy, 14, GuideMuted, TextAnchor.MiddleRight, Center, P(357 - 20, y), new Vector2(40, 22));
            }
            var musicValue = Row("Music", "Nhạc nền", 312.5f, defaults.musicVolume, out var music);
            var sfxValue = Row("Sfx", "Hiệu ứng", 344.5f, defaults.sfxVolume, out var sfx);
            DashedLine(cards, P(195, 367.5f).y, 324);
            Txt("VibrateLabel", cards, "Rung khi đập trúng", fontHeavy, 15, Dark, TextAnchor.MiddleLeft, Center, P(33 + 110, 395.5f), new Vector2(220, 22));
            var vibrate = BuildToggle("VibrateSwitch", cards, P(357 - 30, 395.5f));

            // Thẻ 3 — Ngôn ngữ (y 442, cao 117,5): 2 nút cách 10, cao 52 (gồm viền); chip mã (viền 2, bo 8, nền trắng) + chữ 17
            Card("LanguageCard", 442f, 117.5f);
            Header(UiArt.GlobeIcon(), "Ngôn ngữ", 468.75f);
            var codeChip = UiArt.Box("chip_lang_code", 30, 24, 8, 2, Dark, new[] { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "FFFFFF") }, sliceX: true);
            Button Lang(string name, string code, string label, int col)
            {
                const float w = (324f - 10f) / 2f;
                var b = GBtn(name, cards, Spr("UI/button_white"), Center, P(33 + w / 2f + col * (w + 10), 516.5f), w, 52, label, 17, Dark, null, 0);
                var content = b.transform.Find("Content");
                var chipText = PillLabel("Code", content, codeChip, code, 13, Dark, 24, 6);
                Object.DestroyImmediate(chipText.transform.parent.GetComponent<ContentSizeFitter>());
                chipText.transform.parent.SetAsFirstSibling();
                return b;
            }
            var vi = Lang("LanguageVi", "VI", "Tiếng Việt", 0);
            var en = Lang("LanguageEn", "EN", "English", 1);

            var view = screen.gameObject.AddComponent<SettingsScreen>();
            Set(view, "router", router);
            Set(view, "config", config);
            Set(view, "backButton", back);
            SetArray(view, "roundButtons", roundButtons);
            Set(view, "selectedSprite", Spr("UI/button_yellow"));
            Set(view, "normalSprite", Spr("UI/button_white"));
            Set(view, "musicSlider", music);
            Set(view, "musicValue", musicValue);
            Set(view, "sfxSlider", sfx);
            Set(view, "sfxValue", sfxValue);
            Set(view, "vibrateSwitch", vibrate);
            Set(view, "viButton", vi);
            Set(view, "enButton", en);
        }

        /// <summary>
        /// Thanh trượt (Guide25D .slider): cả hàng cao 24 để dễ chạm; rãnh cao 6 bo tròn hai đầu, phần đã kéo #FF9F1C, phần còn lại #3B3B3B;
        /// núm tròn 16 #FF9F1C không viền. Không đổi màu khi rê / nhấn / kéo (Transition = None).
        /// </summary>
        static Slider BuildSlider(string name, Transform parent, Vector2 posPt, float W)
        {
            const float H = 24, Knob = 16, Track = 6;
            const float PillSlice = 48f / (Track / 2f * PT); // góc 48 px của gen_round_rect → bán kính 3 pt = nửa chiều cao rãnh
            var rt = Rect(name, parent, Center, Center, posPt, new Vector2(W, H));
            var track = Img("Track", rt, roundRect, Center, Center, Vector2.zero, new Vector2(W, Track));
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0, Track * PT);
            Sliced(track, PillSlice);
            track.color = TrackDark;

            var fillArea = Rect("FillArea", rt, Center, Center, Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(0, Track * PT);
            var fill = Img("Fill", fillArea, roundRect, Center, Center, Vector2.zero, Vector2.zero);
            Sliced(fill, PillSlice);
            fill.color = Orange;
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect("HandleArea", rt, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(handleArea, 0);
            handleArea.offsetMin = new Vector2(Knob / 2f * PT, 0);
            handleArea.offsetMax = new Vector2(-Knob / 2f * PT, 0);
            var handle = Img("Handle", handleArea, circle, Center, Center, Vector2.zero, new Vector2(Knob, Knob));
            handle.color = Orange;
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(Knob, Knob - H) * PT; // Slider kéo neo dọc theo vùng trượt cao H

            var hitArea = rt.gameObject.AddComponent<Image>();
            hitArea.color = new Color(1, 1, 1, 0); // bấm vào rãnh cũng kéo được

            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.7f;
            return slider;
        }

        /// <summary>
        /// Công tắc rung (switchStyle trong Guide25D): 60 × 34, viền 3, bo tròn; tắt #CFC5B2 → #E9E1D0, bật #A6F09A → #4CC25E
        /// (lớp bật mờ dần chồng lên); núm 24 trắng → #E3DACA viền 2, cách mép 2 → dịch ±13.
        /// </summary>
        static ToggleSwitch BuildToggle(string name, Transform parent, Vector2 posPt)
        {
            const float W = 60, H = 34, Knob = 24;
            var track = Img(name, parent, UiArt.Box("switch_off", W, H, H / 2f, 3, Dark, new[] { new UiArt.Stop(0f, "CFC5B2"), new UiArt.Stop(1f, "E9E1D0") }),
                Center, Center, posPt, new Vector2(W, H));
            track.raycastTarget = true;
            var onFill = Img("On", track.transform, UiArt.Box("switch_on", W, H, H / 2f, 3, Dark, new[] { new UiArt.Stop(0f, "A6F09A"), new UiArt.Stop(1f, "4CC25E") }),
                Center, Center, Vector2.zero, new Vector2(W, H));
            var knob = Img("Knob", track.transform, UiArt.Box("switch_knob", Knob, Knob, Knob / 2f, 2, Dark, new[] { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "E3DACA") }),
                Center, Center, new Vector2((W - 6 - 4 - Knob) / 2f, 0), new Vector2(Knob, Knob));

            var toggle = track.gameObject.AddComponent<ToggleSwitch>();
            Set(toggle, "track", track);
            Set(toggle, "onFill", onFill);
            Set(toggle, "knob", knob.rectTransform);
            SetFloat(toggle, "knobTravel", (W - 6 - 4 - Knob) / 2f * PT);
            return toggle;
        }
    }
}
