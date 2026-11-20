using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Dựng scene chơi dùng chung cho mọi map (Assets/Scenes/Game.unity) và dữ liệu 5 map trong Assets/Data.
    /// Tự chạy một lần khi scene chưa tồn tại; chạy lại bằng menu WhackyZoo › Dựng lại scene chơi.
    /// Bố cục theo khung 390 × 844 pt (images/screens/03_*.png), nhân 3 ra đơn vị canvas.
    /// Số liệu map/thú lấy từ Docs/GameSpec.md; mỗi lần dựng sẽ ghi đè asset thú và map theo bảng bên dưới.
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneBuilder
    {
        internal const string ScenePath = "Assets/Scenes/Game.unity";
        internal const string OldScenePath = "Assets/Scenes/Land.unity";
        internal const string Art = "Assets/Art";
        internal const string DataRoot = "Assets/Data";
        internal const float PT = 3f; // 1 pt = 3 đơn vị canvas (reference 1170 × 2532)
        internal const float DesignW = 390f, DesignH = 844f;

        internal static readonly Color Dark = Hex("3A2412");
        internal static readonly Color Muted = Hex("7A5A3A");
        internal static readonly Color Yellow = Hex("FFE14A");
        internal static readonly Vector4 ButtonBorder = new Vector4(60, 72, 60, 60); // trái, dưới, phải, trên (px @3x)
        internal static readonly Vector4 PanelBorder = new Vector4(60, 78, 60, 60);

        internal static Font fontHeavy, fontBold;

        static GameSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        internal static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(ScenePath) || SessionState.GetBool("WhackyZoo.GameAutoBuilt", false)) return;
            SessionState.SetBool("WhackyZoo.GameAutoBuilt", true);
            Build(additive: true);
        }

        [MenuItem("WhackyZoo/Dựng lại scene chơi")]
        internal static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(additive: false);
        }

        internal static void Build(bool additive)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                LoadFonts();
                var config = CreateConfig();
                var maps = CreateMaps();
                SoundBankBuilder.Create(); // âm thanh tạm + nhạc từng map (chỉ sinh file còn thiếu)
                BuildScene(config, maps[0]);
                EditorSceneManager.SaveScene(scene, ScenePath);
                UpdateBuildScenes();
                AssetDatabase.SaveAssets();
                Debug.Log($"[WhackyZoo] Đã dựng {ScenePath} và {maps.Length} map. Đổi map chơi thử ở GameController › Map.");
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

        // ───────────────────────── Dữ liệu (Docs/GameSpec.md › Map và thú) ─────────────────────────

        internal struct Animal
        {
            public string file, name, shortName;
            public int points;
            public Animal(string file, string name, int points, string shortName = null)
            { this.file = file; this.name = name; this.points = points; this.shortName = shortName; }
        }

        internal struct Special
        {
            public string file, name, shortName, hint;
            public int points;
            public Special(string file, string name, string shortName, int points, string hint)
            { this.file = file; this.name = name; this.shortName = shortName; this.points = points; this.hint = hint; }
        }

        internal struct MapDef
        {
            public string id, name, normalLabel;
            public bool isNew;
            public Color howToTop, howToBottom;
            public Animal[] animals;
            public Special bonus, obstacle;
            public ChipStyle mapChip, normalChip, bonusChip, obstacleChip;
        }

        internal const string BonusHint = "Hiếm, ló rất nhanh";
        internal const string ObstacleHint = "Đừng đập! Mất combo";

        internal static MapDef[] MapDefs()
        {
            // Chip chú thích dưới lưới hố (Game*.dc.html): .panel trắng / vàng thưởng / màu vật cản riêng từng map
            var white = LegendChip("normal", "FFFFFF", "F3EEE4", "B9A98C", "3A2412");
            var yellow = LegendChip("bonus", "FFF3A0", "FFD84A", "C08A00", "3A2412");
            // Vật cản: mọi map đều là bom (tiếng "bụp phù" là tiếng nổ, không hợp với con vật) — người dùng chốt 15/11/2026
            var bomb = new Special("bomb", "Bom", null, -20, ObstacleHint);
            var bombChip = LegendChip("land_bomb", "5A5A68", "2A2A34", "111111", "FFFFFF");
            return new[]
            {
                new MapDef
                {
                    id = "land", name = "Trên Cạn", normalLabel = "Thú thường",
                    animals = new[] { new Animal("dog", "Chó", 10), new Animal("cat", "Mèo", 10), new Animal("mouse", "Chuột", 15), new Animal("pig", "Heo", 10) },
                    bonus = new Special("golden_chick", "Gà vàng", null, 50, BonusHint),
                    obstacle = bomb,
                    mapChip = new ChipStyle(MapChipSprite("land", "A6F09A", "4CC25E", "2E9E44"), Color.white, Color.white),
                    howToTop = Hex("2F8A3A"), howToBottom = Hex("6CC24A"),
                    normalChip = white, bonusChip = yellow,
                    obstacleChip = bombChip,
                },
                new MapDef
                {
                    id = "sea", name = "Dưới Biển", normalLabel = "Hải sản",
                    animals = new[] { new Animal("crab", "Cua", 12), new Animal("octopus", "Bạch tuộc", 12), new Animal("fish", "Cá", 12), new Animal("starfish", "Sao biển", 12) },
                    bonus = new Special("pearl", "Ngọc trai", null, 52, "Hiếm, mở rất nhanh"),
                    obstacle = bomb,
                    mapChip = new ChipStyle(MapChipSprite("sea", "A8DCFF", "3FA9F5", "1F7FC4"), Color.white, Color.white),
                    howToTop = Hex("145DA0"), howToBottom = Hex("3FA4E0"),
                    normalChip = LegendChip("normal_sea", "FFFFFF", "F3EEE4", "8FA6B8", "3A2412"),
                    bonusChip = LegendChip("sea_pearl", "FFE0EA", "FFB3C7", "C25478", "3A2412"),
                    obstacleChip = bombChip,
                },
                new MapDef
                {
                    id = "desert", name = "Sa Mạc", normalLabel = "Thú sa mạc",
                    animals = new[] { new Animal("fox", "Cáo", 14), new Animal("snake", "Rắn", 14), new Animal("lizard", "Thằn lằn", 14), new Animal("meerkat", "Chồn đất", 14) },
                    bonus = new Special("golden_vase", "Bình vàng", null, 50, BonusHint),
                    obstacle = bomb,
                    mapChip = new ChipStyle(MapChipSprite("desert", "FFD3A0", "FF9F43", "E07A1A"), Color.white, Color.white),
                    howToTop = Hex("C25A1C"), howToBottom = Hex("F0A04B"),
                    normalChip = white, bonusChip = yellow,
                    obstacleChip = bombChip,
                },
                new MapDef
                {
                    id = "arctic", name = "Bắc Cực", normalLabel = "Thú",
                    animals = new[] { new Animal("polar_bear", "Gấu bắc cực", 16), new Animal("penguin", "Cánh cụt", 16), new Animal("seal", "Hải cẩu", 16), new Animal("orca", "Cá voi sát thủ", 16, "Cá voi") },
                    bonus = new Special("golden_salmon", "Cá hồi vàng", "Cá hồi", 50, BonusHint),
                    obstacle = bomb,
                    mapChip = new ChipStyle(MapChipSprite("arctic", "E2F4FF", "7FC4EE", "3F8FCF"), Color.white, Color.white),
                    howToTop = Hex("2C6E9E"), howToBottom = Hex("8CC8EC"),
                    normalChip = white, bonusChip = yellow,
                    obstacleChip = bombChip,
                },
                new MapDef
                {
                    id = "grave", name = "Nghĩa Trang", normalLabel = "Thú", isNew = true,
                    animals = new[] { new Animal("bat", "Dơi", 18), new Animal("black_cat", "Mèo đen", 18), new Animal("crow", "Quạ", 18), new Animal("spider", "Nhện", 18) },
                    bonus = new Special("golden_firefly", "Đom đóm vàng", "Đom đóm", 50, BonusHint),
                    obstacle = bomb,
                    mapChip = new ChipStyle(MapChipSprite("grave", "B9A6E0", "7B62B0", "4A3478"), Color.white, Color.white),
                    howToTop = Hex("2A1F4A"), howToBottom = Hex("5B4A8A"),
                    normalChip = white, bonusChip = yellow,
                    obstacleChip = bombChip,
                },
            };
        }

        internal static ChipStyle Chip(string sprite, Color tint, Color text) => new ChipStyle(Spr("UI/" + sprite), tint, text);

        internal static GameConfig CreateConfig()
        {
            EnsureFolder(DataRoot);
            // Giữ chỉnh sửa của người dùng; trường mới nhận giá trị mặc định trong GameConfig.cs.
            var path = $"{DataRoot}/GameConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        internal static MapData[] CreateMaps()
        {
            EnsureFolder($"{DataRoot}/Maps");
            var defs = MapDefs();
            var maps = new MapData[defs.Length];
            for (int i = 0; i < defs.Length; i++) maps[i] = CreateMap(defs[i]);

            var catalog = Upsert<MapCatalog>($"{DataRoot}/MapCatalog.asset");
            catalog.maps = maps;
            EditorUtility.SetDirty(catalog);
            return maps;
        }

        internal static MapData CreateMap(MapDef d)
        {
            string dir = $"{DataRoot}/Critters/{Capitalize(d.id)}";
            EnsureFolder(dir);

            CritterData Critter(string id, string name, string shortName, string hint, string spritePath, CritterKind kind, int points, string designMap = null)
            {
                var c = Upsert<CritterData>($"{dir}/{id}.asset");
                c.id = id;
                c.displayName = name;
                c.shortName = shortName;
                c.hint = hint;
                c.sprite = LoadSprite(spritePath);
                c.kind = kind;
                c.points = points;
                // chớp mắt + lấp lánh sinh từ SVG thiết kế (id = <map>_<tên file sprite>)
                CritterAnimBuilder.Apply(c, designMap ?? d.id, id.Substring(d.id.Length + 1), spritePath);
                EditorUtility.SetDirty(c);
                return c;
            }

            var normals = new CritterData[d.animals.Length];
            for (int i = 0; i < d.animals.Length; i++)
            {
                var a = d.animals[i];
                normals[i] = Critter($"{d.id}_{a.file}", a.name, a.shortName, null,
                    $"{Art}/Characters/animal_{d.id}_{a.file}.png", CritterKind.Normal, a.points);
            }
            var bonus = Critter($"{d.id}_{d.bonus.file}", d.bonus.name, d.bonus.shortName, d.bonus.hint,
                $"{Art}/BonusObstacles/bonus_{d.id}_{d.bonus.file}.png", CritterKind.Bonus, d.bonus.points);
            // bom: cùng hình (và tia lửa ngòi nổ trong thiết kế) của map Trên cạn cho mọi map
            string obstacleMap = d.obstacle.file == "bomb" ? "land" : d.id;
            var obstacle = Critter($"{d.id}_{d.obstacle.file}", d.obstacle.name, d.obstacle.shortName, d.obstacle.hint,
                $"{Art}/BonusObstacles/obstacle_{obstacleMap}_{d.obstacle.file}.png", CritterKind.Obstacle, d.obstacle.points, obstacleMap);

            var m = Upsert<MapData>($"{DataRoot}/Maps/Map_{Capitalize(d.id)}.asset");
            m.id = d.id;
            m.displayName = d.name;
            m.background = LoadSprite($"{Art}/Backgrounds/bg_map_{d.id}.png");
            // Hiệu ứng nền (rong, bong bóng, tia sáng…); map có hiệu ứng dùng nền sạch sinh từ CSS thiết kế
            var ambientFx = AmbientArtBuilder.Build(d.id);
            if (ambientFx.background != null) m.background = ambientFx.background;
            m.ambient = ambientFx.elements;
            m.ambientFx = ambientFx.pathFx;
            m.ambientFxMask = ambientFx.pathFxMask;
            m.holeBack = LoadSprite($"{Art}/Holes/hole_{d.id}_back.png");
            m.holeFront = LoadSprite($"{Art}/Holes/hole_{d.id}_front.png");
            string cardBg = $"{Art}/MapCards/card_bg_{d.id}.png";
            m.banner = File.Exists(cardBg) ? LoadSprite(cardBg) : null;
            // Hiệu ứng nền thẻ (mây, gió, tuyết, dơi, bong bóng…) — Docs/Animations.md mục 1
            m.cardEffects = CardFxBuilder.Build(d.id, out m.cardSkyBottom);
            bool needsMask = System.Array.Exists(m.cardEffects, e => e.clip == FxClip.MapMask);
            m.cardSkyMask = needsMask && m.banner != null ? CreateSkyMask(cardBg, $"{Art}/MapCards/card_mask_{d.id}.png") : null;
            m.holeMask = CreateHoleMask($"{Art}/Holes/hole_{d.id}_front.png", $"{Art}/Holes/hole_{d.id}_mask.png");
            m.normalCritters = normals;
            m.bonus = bonus;
            m.obstacle = obstacle;
            m.normalLabel = d.normalLabel;
            m.isNew = d.isNew;
            m.howToTop = d.howToTop;
            m.howToBottom = d.howToBottom;
            m.mapChip = d.mapChip;
            m.normalChip = d.normalChip;
            m.bonusChip = d.bonusChip;
            m.obstacleChip = d.obstacleChip;
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ───────────────────────── Scene ─────────────────────────

        internal static void BuildScene(GameConfig config, MapData map)
        {
            var root = CreateCanvas();

            // Nền
            var bgRt = Rect("Background", root, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.sprite = map.background;
            bg.raycastTarget = false;
            var fitter = bgRt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1170f / 2532f;
            var ambientRt = Rect("Ambient", bgRt, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            var ambient = ambientRt.gameObject.AddComponent<AmbientFxView>();

            // Lưới hố 3×3 phối cảnh: hàng xa nhỏ hơn
            // Khung chơi (hố + HUD) rung mỗi cú đập; nền đứng yên để không lộ mép
            var stage = Rect("Stage", root, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(stage, 0);
            var shake = stage.gameObject.AddComponent<ScreenShake>();
            var field = Rect("Field", stage, Center, Center, Vector2.zero, Vector2.zero);
            var rows = new[]
            {
                (bottom: 386f, scale: 0.8f, xs: new[] { 103.5f, 195f, 286.5f }),
                (bottom: 500f, scale: 0.9f, xs: new[] { 91.5f, 195f, 298.5f }),
                (bottom: 618f, scale: 1.0f, xs: new[] { 79f, 195f, 311f }),
            };
            var holes = new List<Hole>();
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < 3; c++)
                    holes.Add(BuildHole(field, $"Hole_{r}{c}", map, CenterPos(rows[r].xs[c], rows[r].bottom), rows[r].scale));

            // Sấm sét Búa Thor: phủ kín khung chơi, trên lưới hố, dưới HUD; loé sáng trắng xanh
            var lightningRt = Rect("Lightning", stage, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(lightningRt, 0);
            var flash = Img("Flash", lightningRt, null, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(flash.rectTransform, 0);
            flash.color = new Color(0.85f, 0.94f, 1f, 0f);
            var lightning = lightningRt.gameObject.AddComponent<LightningView>();
            Set(lightning, "flash", flash);

            var boosters = BoosterBuilder.CreateBoosters();
            var shopCatalog = ShopBuilder.CreateShop(AssetDatabase.LoadAssetAtPath<MapCatalog>($"{DataRoot}/MapCatalog.asset"), boosters);
            var hud = BuildHud(stage);
            hud.SetLegend(map); // hiện chữ thật ngay trong chế độ chỉnh sửa
            var boosterBar = BoosterBuilder.BuildBar(stage, boosters);
            var howTo = BuildHowToPlay(root);
            howTo.Fill(map);
            var pause = BuildPausePopup(root);
            var end = BuildEndPopup(root);

            var controller = new GameObject("GameController").AddComponent<GameController>();
            Set(controller, "config", config);
            Set(controller, "map", map);
            Set(controller, "background", bg);
            Set(controller, "ambient", ambient);
            Set(controller, "hud", hud);
            Set(controller, "howToPlay", howTo);
            Set(controller, "shake", shake);
            Set(controller, "pausePopup", pause);
            Set(controller, "endPopup", end);
            SetArray(controller, "holes", holes);
            Set(controller, "boosters", boosters);
            Set(controller, "boosterBar", boosterBar);
            Set(controller, "shop", shopCatalog);
            Set(controller, "lightning", lightning);
            Set(controller, "progression", ProgressionBuilder.CreateProgression(boosters));
            LocalizationBuilder.Tag(root); // chữ cố định đổi theo ngôn ngữ
        }

        /// <summary>Camera, EventSystem (Input System) và Canvas co giãn theo khung 1170 × 2532.</summary>
        internal static Transform CreateCanvas()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170, 2532);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;
            canvasGo.AddComponent<AdaptiveCanvasScaler>();
            return canvasGo.transform;
        }

        internal static Hole BuildHole(Transform parent, string name, MapData map, Vector2 posPt, float scale)
        {
            // Gốc: vùng chạm trong suốt, đáy trùng đáy hình hố
            var root = Rect(name, parent, Center, new Vector2(0.5f, 0f), posPt, new Vector2(110, 113));
            root.localScale = Vector3.one * scale;
            root.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);

            var back = Img("Back", root, map.holeBack, Bottom, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110, 76));

            // Mặt nạ cong theo mép trước của hố: thú chỉ hiện phía trên mép, phần dưới chui sau lớp Front
            var maskImg = Img("PopMask", root, map.holeMask, Bottom, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(MaskW / PT, MaskH / PT));
            var mask = maskImg.rectTransform;
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var critterImg = Img("Critter", mask, map.normalCritters[0].sprite, Bottom, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(90, 90));
            critterImg.preserveAspect = true;
            // Lớp tia lấp lánh (con thưởng +50, ngòi bom) nằm trong Critter để ló/bẹp cùng thú; Hole đặt vị trí theo CritterData
            var spark = Img("Spark", critterImg.rectTransform, null, Center, Center, Vector2.zero, new Vector2(10, 10));
            spark.gameObject.SetActive(false);
            critterImg.gameObject.SetActive(false); // hố trống như lúc bắt đầu ván

            var front = Img("Front", root, map.holeFront, Bottom, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(110, 76));

            var stars = Img("DizzyStars", root, LoadSprite($"{Art}/FX/fx_dizzy_stars.png"), Bottom, Center, new Vector2(0, 100), new Vector2(70, 26));
            var burst = Img("HitBurst", root, LoadSprite($"{Art}/FX/fx_hit_burst.png"), Bottom, Center, new Vector2(0, 66), new Vector2(120, 120));
            var hammer = Img("Hammer", root, LoadSprite($"{Art}/Tools/tool_wood_hammer.png"), Bottom, new Vector2(0.8f, 0.12f), new Vector2(64, 80), new Vector2(84, 84));
            var floatText = Txt("FloatText", root, "+10", fontHeavy, 32, Color.white, TextAnchor.MiddleCenter, Bottom, new Vector2(26, 104), new Vector2(100, 40));
            AddOutline(floatText, Dark, 4);

            foreach (var g in new Graphic[] { stars, burst, hammer, floatText }) g.gameObject.SetActive(false);

            var hole = root.gameObject.AddComponent<Hole>();
            Set(hole, "holeBack", back);
            Set(hole, "holeFront", front);
            Set(hole, "popMask", maskImg);
            SetFloat(hole, "upY", 18 * PT);
            Set(hole, "critter", critterImg.rectTransform);
            Set(hole, "critterImage", critterImg);
            Set(hole, "sparkImage", spark);
            Set(hole, "dizzyStars", stars.rectTransform);
            Set(hole, "hitBurst", burst);
            Set(hole, "hammer", hammer);
            Set(hole, "floatText", floatText);
            return hole;
        }

        // ───────────── HUD màn chơi (GameLand25D.dc.html: padding 54/16/22, hàng cao 56, cách 10) ─────────────
        static readonly UiArt.Stop[] PanelWhite = { new UiArt.Stop(0f, "FFFFFF"), new UiArt.Stop(1f, "F3EEE4") };

        internal static HudView BuildHud(Transform root)
        {
            var hudRt = Rect("HUD", root, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(hudRt, 0);

            // Nút tạm dừng 56 × 56 (border-box), bo 16, icon 22
            var pauseBtn = GBtn("PauseButton", hudRt, Spr("UI/button_white"), Top, TopPos(44, 82), 56, 56, null, 0, Dark, Spr("UI/icon_pause"), 22);

            // Đồng hồ: .panel 210 × 56 (bo 16, viền 3, trắng → #F3EEE4, bóng 0 4px #B9A98C), đệm 4/10/6
            var timer = Img("TimerPanel", hudRt, UiArt.Box("hud_timer_panel", 210, 56, 16, 3, Dark, PanelWhite, 4, Hex("B9A98C")),
                Top, new Vector2(0.5f, 1f), TopPos(187, 54), new Vector2(210, 60));
            Transform T = timer.transform;
            Vector2 InTimer(float x, float yFromTop) => new Vector2(x - 105f, 30f - yFromTop); // toạ độ trong khung 210 × 60
            // dòng giờ (icon 22 + cách 6 + chữ 22) tâm cách mép trên 17; thanh 16 tâm cách mép trên 40
            Img("Icon", T, Spr("UI/icon_timer"), Center, Center, InTimer(13 + 11, 17), new Vector2(22, 22));
            var timeText = Txt("TimeText", T, "1:00", fontHeavy, 22, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(116, 26));
            timeText.rectTransform.pivot = new Vector2(0f, 0.5f); // đập nhịp quanh mép trái
            timeText.rectTransform.anchoredPosition = InTimer(13 + 22 + 6, 17) * PT;
            var bar = Img("TimeBar", T, UiArt.Box("hud_time_track", 184, 16, 8, 2, Dark, new[] { new UiArt.Stop(0f, "CFC5B2"), new UiArt.Stop(1f, "E9E1D0") }),
                Center, Center, InTimer(105, 40), new Vector2(184, 16));
            var barInner = Rect("Inner", bar.transform, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(barInner, 2);
            barInner.gameObject.AddComponent<RectMask2D>();
            var barGreen = UiArt.Box("hud_time_fill_green", 20, 12, 4, 0, Dark, new[] { new UiArt.Stop(0f, "D6FBCF"), new UiArt.Stop(0.17f, "A6F09A"), new UiArt.Stop(0.6f, "4CC25E"), new UiArt.Stop(1f, "2E9E44") }, sliceX: true);
            var barRed = UiArt.Box("hud_time_fill_red", 20, 12, 4, 0, Dark, new[] { new UiArt.Stop(0f, "FFC4BE"), new UiArt.Stop(0.17f, "FF8A80"), new UiArt.Stop(1f, "E53935") }, sliceX: true);
            var fill = Img("Fill", barInner, barGreen, Center, Center, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Sliced;
            Stretch(fill.rectTransform, 0);

            // Ô ĐIỂM: 72 × 56, bo 16, viền 3, #FFE68A → #FFC93C → #F0A400, bóng 0 4px #B87800; chữ 12 / 24
            var scorePanel = Img("ScorePanel", hudRt, UiArt.Box("hud_score_panel", 72, 56, 16, 3, Dark,
                new[] { new UiArt.Stop(0f, "FFE68A"), new UiArt.Stop(0.55f, "FFC93C"), new UiArt.Stop(1f, "F0A400") }, 4, Hex("B87800")),
                Top, new Vector2(0.5f, 1f), TopPos(338, 54), new Vector2(72, 60));
            Txt("Label", scorePanel.transform, "ĐIỂM", fontHeavy, 12, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 30 - 15.4f), new Vector2(70, 16));
            var scoreText = Txt("ScoreText", scorePanel.transform, "0", fontHeavy, 24, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 30 - 34.3f), new Vector2(70, 28));

            // COMBO: 28 #FF7A4A, viền 4 (thấy 2) + bóng 0 4px, xoay −5°, cách HUD 14 (tâm y 146,4)
            var combo = Txt("ComboText", hudRt, "COMBO x2", fontHeavy, 28, Hex("FF7A4A"), TextAnchor.MiddleLeft, Top, TopPos(16, 146.4f), new Vector2(240, 44));
            combo.rectTransform.pivot = new Vector2(0f, 0.5f);
            combo.rectTransform.localRotation = Quaternion.Euler(0, 0, 5f);
            combo.gameObject.AddComponent<TextStroke>().Configure(Dark, 2f * PT, new Vector2(0, -4 * PT));
            combo.gameObject.SetActive(false);

            // Chú thích điểm: 3 chip phẳng (viền 2, bo tròn, đệm 10, chữ 13, bóng 0 3px), cách 8, màu theo map
            var legend = Rect("Legend", hudRt, Bottom, Center, BottomPos(195, 708.5f), new Vector2(380, 28));
            var row = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8 * PT;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var legendNormal = AutoChip("LegendNormal", legend, "Thú thường +10", 13, 28);
            var legendBonus = AutoChip("LegendBonus", legend, "Gà vàng +50", 13, 28);
            var legendObstacle = AutoChip("LegendObstacle", legend, "Bom -20", 13, 28);

            var hud = hudRt.gameObject.AddComponent<HudView>();
            Set(hud, "pauseButton", pauseBtn);
            Set(hud, "timeText", timeText);
            Set(hud, "timeBarFill", fill.rectTransform);
            Set(hud, "timeBarFillImage", fill);
            Set(hud, "barNormalSprite", barGreen);
            Set(hud, "barLowSprite", barRed);
            Set(hud, "scoreText", scoreText);
            Set(hud, "comboText", combo);
            Set(hud, "legendNormal", legendNormal);
            Set(hud, "legendBonus", legendBonus);
            Set(hud, "legendObstacle", legendObstacle);
            return hud;
        }

        /// <summary>
        /// Chip chú thích co theo chữ: ảnh chip (MapData.*Chip) cao 25 + bóng 3; đệm ngang 10 + viền 2.
        /// Chữ căn giữa phần mặt chip (không tính dải bóng 3 pt phía dưới).
        /// </summary>
        internal static Text AutoChip(string name, Transform parent, string text, float fontPt, float heightPt, bool fitSelf = false)
        {
            var img = Img(name, parent, Spr("UI/button_white"), Center, Center, Vector2.zero, new Vector2(100, heightPt));
            img.type = Image.Type.Sliced;
            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            int pad = Mathf.RoundToInt(12 * PT);
            layout.padding = new RectOffset(pad, pad, 0, Mathf.RoundToInt(3 * PT));
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            if (fitSelf)
            {
                var fitter = img.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return Txt("Text", img.transform, text, fontHeavy, fontPt, Dark, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(80, heightPt - 4));
        }

        /// <summary>Chip chú thích theo CSS từng map: nền chuyển màu dọc, viền 2, bóng cứng 0 3px.</summary>
        internal static ChipStyle LegendChip(string name, string top, string bottom, string drop, string text) =>
            new ChipStyle(UiArt.Box($"chip_legend_{name}", 40, 25, 12.5f, 2, Dark, new[] { new UiArt.Stop(0f, top), new UiArt.Stop(1f, bottom) }, 3, Hex(drop), sliceX: true),
                Color.white, Hex(text));

        /// <summary>
        /// Nút .gbtn: mặt cao <paramref name="faceH"/> (đã gồm viền), sprite nút có thêm cạnh 3D 5 pt phía dưới;
        /// icon + chữ căn giữa, cách 8 như thiết kế. posPt là tâm mặt nút.
        /// </summary>
        internal static Button GBtn(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 posPt, float w, float faceH,
            string label, float fontPt, Color textColor, Sprite icon, float iconPt)
        {
            const float Edge = 5f;
            var btn = Btn(name, parent, sprite, anchor, posPt - new Vector2(0, Edge / 2f), new Vector2(w, faceH + Edge), null, 0, textColor, null);
            var content = Rect("Content", btn.transform, Center, Center, new Vector2(0, Edge / 2f), new Vector2(w, faceH));
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8 * PT;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            if (icon != null)
            {
                var ic = Img("Icon", content, icon, Center, Center, Vector2.zero, new Vector2(iconPt, iconPt));
                ic.gameObject.AddComponent<LayoutElement>().preferredWidth = iconPt * PT;
            }
            if (label != null) Txt("Label", content, label, fontHeavy, fontPt, textColor, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(w - 40, faceH));
            return btn;
        }

        // ───────────── Màn Cách chơi (khối intro trong Docs/DesignSource/GameLand25D.dc.html) ─────────────
        // Cả khối căn giữa dọc (padding 36/16/24, gap 12): hàng tiêu đề 40 → thẻ 358 × 284 → chữ tiến trình + thanh 28.
        // Hàng thưởng / vật cản cao 49,6 vì gợi ý vật cản luôn xuống 2 dòng trong cột 103 (như ảnh thiết kế).
        // Phần "VẬT PHẨM HỖ TRỢ" của thiết kế để tới bước 4 (vật phẩm chưa có trong game).
        // Toạ độ dưới đây là tâm (pt, khung 390 × 844, gốc trên-trái); khối nội dung neo giữa màn.
        static readonly UiArt.Stop[] GreenChip = { new UiArt.Stop(0f, "A6F09A"), new UiArt.Stop(0.6f, "4CC25E"), new UiArt.Stop(1f, "2E9E44") };
        internal static readonly Color GuideMuted = Hex("5E4E3A");

        /// <summary>Chip tên map cạnh tiêu đề CÁCH CHƠI: viền 2, bo 10, cao 26 + 4, dải màu riêng từng map.</summary>
        internal static Sprite MapChipSprite(string mapId, string top, string mid, string bottom) =>
            UiArt.Box($"chip_map_{mapId}", 60, 30, 10, 2, Dark, new[] { new UiArt.Stop(0f, top), new UiArt.Stop(0.6f, mid), new UiArt.Stop(1f, bottom) }, sliceX: true);

        internal static HowToPlayView BuildHowToPlay(Transform root)
        {
            var overlay = Img("HowToPlay", root, null, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(overlay.rectTransform, 0);
            overlay.color = Color.white; // nền đặc che kín màn chơi; màu lấy theo map (MapData.howToBottom)
            overlay.raycastTarget = true;
            var howToGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            var shade = Img("Shade", overlay.transform, FadeSprite(), Center, Center, Vector2.zero, Vector2.zero);
            Stretch(shade.rectTransform, 0);

            var panel = Rect("Content", overlay.transform, Center, Center, Vector2.zero, new Vector2(DesignW, DesignH));
            // Thẻ cao thêm 158 cho khối VẬT PHẨM HỖ TRỢ (gap 10 + nhãn 26 + gap 10 + lưới 3 hàng 112 + gap 10 + chú thích 2 dòng 31,2
            // − chú thích cũ 1 dòng) → cả khối (justify-content: center) dịch lên một nửa = 79. Toạ độ y dưới đây là bản chưa dịch.
            const float Up = 79f;
            Vector2 P(float cx, float cy) => CenterPos(cx, cy - Up);

            // Hàng tiêu đề (tâm y 245,8): "CÁCH CHƠI" 40 #FFE14A viền 5 + bóng 4, xoay −3°; cách 10; chip tên map chữ 15
            var titleRow = Rect("TitleRow", panel, Center, Center, P(195, 244f), new Vector2(DesignW, 44));
            var trow = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            trow.spacing = 10 * PT;
            trow.childAlignment = TextAnchor.MiddleCenter;
            trow.childControlWidth = true;
            trow.childControlHeight = false;
            trow.childForceExpandWidth = trow.childForceExpandHeight = false;
            var title = Txt("Title", titleRow, "CÁCH CHƠI", fontHeavy, 40, Yellow, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(200, 44));
            title.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
            title.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));
            var mapChipText = PillLabel("MapChip", titleRow, MapChipSprite("land", "A6F09A", "4CC25E", "2E9E44"), "Trên Cạn", 15, Color.white, 30, 10);
            var mapChip = mapChipText.transform.parent.GetComponent<Image>();
            Object.DestroyImmediate(mapChip.GetComponent<ContentSizeFitter>());

            // Thẻ: 358 × 280, viền 3, bo 24, nền #FFFBEF → #F7E9C6, bóng cứng 6 #B9A98C + bóng mờ 0 18 26 rgba(0,0,0,.35)
            const float cardTop = 276f, cardH = 442f;
            var cardCenter = P(195, cardTop + cardH / 2f);
            var cardSoft = Img("CardSoftShadow", panel, UiArt.SoftShadow("guide_card_soft", 358, cardH, 24, 26, 0.35f), Center, Center, cardCenter + new Vector2(0, -18), new Vector2(358 + 52, cardH + 52));
            var card = Img("Card", panel, UiArt.Box("guide_card", 358, cardH, 24, 3, Dark,
                new[] { new UiArt.Stop(0f, "FFFBEF"), new UiArt.Stop(1f, "F7E9C6") }, 6, Hex("B9A98C"), sliceX: true, sliceY: true),
                Center, new Vector2(0.5f, 1f), P(195, cardTop), new Vector2(358, cardH + 6));
            card.preserveAspect = false;
            Sliced(card, 1f); // thẻ tự cao thêm khi dòng thưởng / vật cản xuống dòng (HowToPlayView)
            Transform C = panel; // các phần tử đặt theo toạ độ trang cho dễ đối chiếu

            // Nhãn "ĐẬP ĐỂ GHI ĐIỂM": viền 2, bo 10, cao 26, chữ 13 trắng bóng 0 1px
            var header = PillLabel("Header", C, UiArt.Box("chip_header_green", 60, 26, 10, 2, Dark, GreenChip, sliceX: true), "ĐẬP ĐỂ GHI ĐIỂM", 13, Color.white, 26, 10);
            var headerRt = (RectTransform)header.transform.parent;
            headerRt.pivot = new Vector2(0f, 0.5f);
            headerRt.anchoredPosition = P(33, 304f) * PT;

            // 4 ô thú: icon 52, cách 2, tên 12, cách 2, chip điểm cao 22 (viền 2, đệm 7) nền #D8FBD2 → #8EE08A
            var ptsGreen = UiArt.Box("chip_points_green", 30, 22, 11, 2, Dark, new[] { new UiArt.Stop(0f, "D8FBD2"), new UiArt.Stop(1f, "8EE08A") }, sliceX: true);
            var slots = new HowToPlayView.CritterSlot[4];
            float[] xs = { 73.5f, 154.5f, 235.5f, 316.5f };
            for (int i = 0; i < xs.Length; i++)
            {
                var slot = Rect($"Critter{i}", C, Center, Center, P(xs[i], 372.6f), new Vector2(81, 91.2f));
                var icon = Img("Icon", slot, null, Center, Center, new Vector2(0, 45.6f - 26), new Vector2(52, 52));
                icon.preserveAspect = true;
                var nameText = Txt("Name", slot, "Chó", fontHeavy, 12, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 45.6f - 52 - 2 - 6.6f), new Vector2(81, 15));
                var pts = PillLabel("Points", slot, ptsGreen, "+10", 13, Dark, 22, 7);
                ((RectTransform)pts.transform.parent).anchoredPosition = new Vector2(0, (45.6f - 91.2f + 11) * PT);
                slots[i] = new HowToPlayView.CritterSlot { root = slot.gameObject, icon = icon, name = nameText, points = pts };
            }

            DashedLine(C, P(195, 429.2f).y, 324);

            // Thưởng / vật cản: 2 cột (157 + 10 + 157), icon 46, cách 8; dòng tên 14 + chip điểm, gợi ý 12 #5E4E3A
            var ptsYellow = UiArt.Box("chip_points_yellow", 30, 22, 11, 2, Dark, new[] { new UiArt.Stop(0f, "FFF3A0"), new UiArt.Stop(1f, "FFC93C") }, sliceX: true);
            var ptsRed = UiArt.Box("chip_points_red", 30, 22, 11, 2, Dark, new[] { new UiArt.Stop(0f, "FF8A80"), new UiArt.Stop(1f, "E53935") }, sliceX: true);
            var (bonusIcon, bonusName, bonusPoints, bonusHint) = SpecialBlock("Bonus", C, 33, 465f - Up, ptsYellow, Dark, false);
            var (obsIcon, obsName, obsPoints, obsHint) = SpecialBlock("Obstacle", C, 200, 465f - Up, ptsRed, Color.white, true);

            var lowerDashes = DashedLine(C, P(195, 500.8f).y, 324);

            // Khối VẬT PHẨM HỖ TRỢ: nhãn xanh (A8DCFF → 3FA9F5 60% → 1F7FC4), rồi lưới 2 cột (157, cách 10), hàng cách 8:
            // icon 32, cách 6, tên 13 ExtraBold / mô tả 11 Bold #5E4E3A (cao dòng 1,1)
            var boosterHeader = PillLabel("BoosterHeader", C, UiArt.Box("chip_header_blue", 60, 26, 10, 2, Dark,
                new[] { new UiArt.Stop(0f, "A8DCFF"), new UiArt.Stop(0.6f, "3FA9F5"), new UiArt.Stop(1f, "1F7FC4") }, sliceX: true), "VẬT PHẨM HỖ TRỢ", 13, Color.white, 26, 10);
            var boosterHeaderRt = (RectTransform)boosterHeader.transform.parent;
            boosterHeaderRt.pivot = new Vector2(0f, 0.5f);
            boosterHeaderRt.anchoredPosition = P(33, 524.8f) * PT;
            var boosterSlots = new HowToPlayView.BoosterSlot[5];
            var boosterRoots = new RectTransform[5];
            for (int i = 0; i < boosterSlots.Length; i++)
            {
                float left = i % 2 == 0 ? 33f : 200f, cy = 563.8f + (i / 2) * 40f;
                var bRoot = Rect($"Booster{i}", C, Center, new Vector2(0f, 0.5f), P(left, cy), new Vector2(157, 32));
                var bIcon = Img("Icon", bRoot, null, Center, Center, new Vector2(-157 / 2f + 16, 0), new Vector2(32, 32));
                bIcon.rectTransform.anchorMin = bIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                bIcon.rectTransform.anchoredPosition = new Vector2(16, 0) * PT;
                bIcon.preserveAspect = true;
                var bName = Txt("Name", bRoot, "+5 giây", fontHeavy, 13, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(119, 15));
                bName.rectTransform.anchorMin = bName.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                bName.rectTransform.pivot = new Vector2(0f, 0.5f);
                bName.rectTransform.anchoredPosition = new Vector2(38, 6.05f) * PT;
                var bHint = Txt("Hint", bRoot, "Cộng 5 giây", fontBold, 11, GuideMuted, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(119, 13));
                bHint.rectTransform.anchorMin = bHint.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                bHint.rectTransform.pivot = new Vector2(0f, 0.5f);
                bHint.rectTransform.anchoredPosition = new Vector2(38, -7.15f) * PT;
                boosterSlots[i] = new HowToPlayView.BoosterSlot { root = bRoot.gameObject, icon = bIcon, name = bName, hint = bHint };
                boosterRoots[i] = bRoot;
            }

            // Chú thích 12 Bold #5E4E3A, cao dòng 1,3, 2 dòng; chữ COMBO đậm
            var footer = Txt("Footer", C, "Chạm vào vật phẩm ở thanh dưới cùng để dùng. Đập trúng liên tiếp để tăng <b>COMBO</b>, đập nhầm sẽ mất combo.",
                fontBold, 12, GuideMuted, TextAnchor.MiddleLeft, Center, P(195, 685.4f), new Vector2(324, 32));
            footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            footer.supportRichText = true;
            footer.lineSpacing = 1.08f;

            // Chữ tiến trình (15, trắng, bóng 0 2px .45) + "Bỏ qua" (15, #FFE14A, gạch chân); thanh 358 × 28 viền 3 bo 14
            var progressText = Txt("ProgressText", panel, "Chuẩn bị vào màn… 0%", fontHeavy, 15, Color.white, TextAnchor.MiddleLeft, Center, P(16 + 110, 743f), new Vector2(220, 26));
            TextShadow(progressText, 2, 0.45f);
            var skip = Img("SkipButton", panel, null, Center, Center, P(374 - 30, 743f), new Vector2(60, 30));
            skip.color = new Color(1, 1, 1, 0);
            skip.raycastTarget = true;
            var skipBtn = skip.gameObject.AddComponent<Button>();
            skip.gameObject.AddComponent<PressEffect>();
            Txt("Label", skip.transform, "Bỏ qua", fontHeavy, 15, Yellow, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(60, 26));
            Img("Underline", skip.transform, null, Center, Center, new Vector2(0, -10), new Vector2(46, 1.5f)).color = Yellow;

            var bar = Img("ProgressBar", panel, UiArt.Box("guide_bar", 358, 28, 14, 3, Dark,
                new[] { new UiArt.Stop(0f, "CFC5B2"), new UiArt.Stop(1f, "E9E1D0") }, 4, new Color(0, 0, 0, 0.3f)),
                Center, new Vector2(0.5f, 1f), P(195, 762f), new Vector2(358, 32));
            var barFillArea = Rect("FillArea", bar.transform, Top, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(352, 22));
            var barFill = Img("Fill", barFillArea, UiArt.Box("guide_bar_fill", 30, 22, 10, 0, Dark,
                new[] { new UiArt.Stop(0f, "FFE68A"), new UiArt.Stop(0.12f, "FFF4C4"), new UiArt.Stop(0.2f, "FFE68A"), new UiArt.Stop(0.55f, "FFC93C"), new UiArt.Stop(1f, "F0A400") }, sliceX: true),
                Center, Center, Vector2.zero, Vector2.zero);
            barFill.type = Image.Type.Sliced;
            Stretch(barFill.rectTransform, 0);
            barFill.rectTransform.anchorMax = new Vector2(0f, 1f);

            var view = overlay.gameObject.AddComponent<HowToPlayView>();
            Set(view, "background", overlay);
            Set(view, "backgroundShade", shade);
            Set(view, "group", howToGroup);
            Set(view, "mapChip", mapChip);
            Set(view, "mapChipText", mapChipText);
            var so = new SerializedObject(view);
            var slotsProp = so.FindProperty("normalSlots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                var e = slotsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("root").objectReferenceValue = slots[i].root;
                e.FindPropertyRelative("icon").objectReferenceValue = slots[i].icon;
                e.FindPropertyRelative("name").objectReferenceValue = slots[i].name;
                e.FindPropertyRelative("points").objectReferenceValue = slots[i].points;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(view, "bonusIcon", bonusIcon);
            Set(view, "bonusName", bonusName);
            Set(view, "bonusPoints", bonusPoints);
            Set(view, "bonusHint", bonusHint);
            Set(view, "obstacleIcon", obsIcon);
            Set(view, "obstacleName", obsName);
            Set(view, "obstaclePoints", obsPoints);
            Set(view, "obstacleHint", obsHint);
            Set(view, "progressText", progressText);
            Set(view, "progressFill", barFill.rectTransform);
            Set(view, "skipButton", skipBtn);
            // Dòng thưởng / vật cản cao 71,6 giữa 2 đường kẻ; khi tên dài làm chip điểm xuống dòng (vd. "Xương rồng −20")
            // thì giãn đoạn này, đẩy phần dưới xuống, thẻ cao thêm và cả khối dịch lên nửa phần thêm để vẫn căn giữa.
            Set(view, "content", panel);
            SetArray(view, "specialInfos", new[] { (RectTransform)bonusName.transform.parent.parent, (RectTransform)obsName.transform.parent.parent });
            SetArray(view, "specialBlocks", new[] { (RectTransform)bonusIcon.transform.parent, (RectTransform)obsIcon.transform.parent });
            var below = new List<RectTransform> { lowerDashes, boosterHeaderRt, footer.rectTransform, progressText.rectTransform, skip.rectTransform, bar.rectTransform };
            below.AddRange(boosterRoots);
            SetArray(view, "belowSpecial", below);
            Set(view, "boosters", AssetDatabase.LoadAssetAtPath<BoosterCatalog>(BoosterBuilder.CatalogPath));
            var bso = new SerializedObject(view);
            var bprop = bso.FindProperty("boosterSlots");
            bprop.arraySize = boosterSlots.Length;
            for (int i = 0; i < boosterSlots.Length; i++)
            {
                var e = bprop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("root").objectReferenceValue = boosterSlots[i].root;
                e.FindPropertyRelative("icon").objectReferenceValue = boosterSlots[i].icon;
                e.FindPropertyRelative("name").objectReferenceValue = boosterSlots[i].name;
                e.FindPropertyRelative("hint").objectReferenceValue = boosterSlots[i].hint;
            }
            bso.ApplyModifiedPropertiesWithoutUndo();
            SetArray(view, "growWithCard", new[] { card.rectTransform, cardSoft.rectTransform });
            overlay.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Chip co theo chữ: ảnh 9-slice trái/phải, đệm ngang <paramref name="padPt"/> + viền 2.</summary>
        internal static Text PillLabel(string name, Transform parent, Sprite sprite, string text, float fontPt, Color textColor, float heightPt, float padPt)
        {
            var img = Img(name, parent, sprite, Center, Center, Vector2.zero, new Vector2(40, heightPt));
            img.type = Image.Type.Sliced;
            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            int pad = Mathf.RoundToInt((padPt + 2) * PT);
            layout.padding = new RectOffset(pad, pad, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            img.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Txt("Text", img.transform, text, fontHeavy, fontPt, textColor, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(30, heightPt - 4));
            if (textColor == Color.white) TextShadow(t, 1, 0.35f);
            return t;
        }

        /// <summary>text-shadow 0 Npx 0 rgba(0,0,0,a).</summary>
        internal static void TextShadow(Text t, float yPt, float alpha)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = new Vector2(0, -yPt * PT);
        }

        /// <summary>
        /// Khối thưởng / vật cản trong cột rộng 157 bắt đầu ở x <paramref name="left"/>: icon 46 (vật cản có gạch chéo đỏ),
        /// cách 8; dòng tên 14 + chip điểm (cách 4), dưới là gợi ý 12 #5E4E3A. Khối cao 46, tâm y <paramref name="cy"/>.
        /// </summary>
        internal static (Image, Text, Text, Text) SpecialBlock(string name, Transform parent, float left, float cy, Sprite chipSprite, Color chipText, bool crossed)
        {
            var block = Rect(name, parent, Center, Center, CenterPos(left + 23, cy), new Vector2(46, 46));
            var icon = Img("Icon", block, null, Center, Center, Vector2.zero, new Vector2(46, 46));
            icon.preserveAspect = true;
            if (crossed)
            {
                var slash = Img("Slash", block, null, Center, Center, Vector2.zero, new Vector2(50, 4.5f));
                slash.color = Hex("E53935");
                slash.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f);
            }
            // Cột chữ bắt đầu sau icon 46 + 8 (cách tâm icon 31), rộng 157 − 54 = 103: dòng tên (22) rồi gợi ý tự xuống dòng.
            // Khối chữ tự cao theo số dòng và căn giữa theo icon, nên gợi ý 2 dòng không đè lên đường kẻ.
            var info = Rect("Info", block, Center, new Vector2(0f, 0.5f), new Vector2(31, 0), new Vector2(103, 36));
            var vl = info.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleLeft;
            vl.childControlWidth = false;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = vl.childForceExpandHeight = false;
            info.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Dòng tên + chip điểm: flex-wrap như bản thiết kế (column-gap 4, row-gap 2) — tên dài (vd. "Đom đóm vàng")
            // thì chip xuống dòng dưới thay vì tràn sang cột bên cạnh.
            var row = Rect("NameRow", info, Center, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(103, 22));
            var flow = row.gameObject.AddComponent<FlowLayoutGroup>();
            flow.SpacingX = 4 * PT;
            flow.SpacingY = 2 * PT;
            var nameText = Txt("Name", row, "Gà vàng", fontHeavy, 14, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(70, 20));
            var ptsText = PillLabel("Points", row, chipSprite, "+50", 13, chipText, 22, 7);
            Object.DestroyImmediate(ptsText.transform.parent.GetComponent<ContentSizeFitter>()); // trong layout cha thì không cần

            var hint = Txt("Hint", info, "Hiếm, ló rất nhanh", fontBold, 12, GuideMuted, TextAnchor.UpperLeft, Center, Vector2.zero, new Vector2(103, 14));
            hint.rectTransform.pivot = new Vector2(0f, 0.5f);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.verticalOverflow = VerticalWrapMode.Truncate;
            hint.lineSpacing = 0.95f;
            return (icon, nameText, ptsText, hint);
        }

        /// <summary>border-top: 2px dashed #D9CBB0 rộng <paramref name="widthPt"/>, tâm y <paramref name="yPt"/> (toạ độ cha).</summary>
        internal static RectTransform DashedLine(Transform parent, float yPt, float widthPt)
        {
            var line = Rect("Dashes", parent, Center, Center, new Vector2(0, yPt), new Vector2(widthPt, 2));
            const float dash = 6f, gap = 6f;
            int count = Mathf.FloorToInt((widthPt + gap) / (dash + gap));
            float start = -((count * (dash + gap)) - gap) / 2f + dash / 2f;
            for (int i = 0; i < count; i++)
                Img("Dash", line, null, Center, Center, new Vector2(start + i * (dash + gap), 0), new Vector2(dash, 2)).color = Hex("D9CBB0");
            return line;
        }

        internal static void Dashes(Transform parent, float yPt, float widthPt)
        {
            var line = Rect("Dashes", parent, Center, Center, new Vector2(0, yPt), new Vector2(widthPt, 2));
            const float dash = 7f, gap = 5f;
            int count = Mathf.FloorToInt((widthPt + gap) / (dash + gap));
            float start = -((count * (dash + gap)) - gap) / 2f + dash / 2f;
            for (int i = 0; i < count; i++)
                Img("Dash", line, null, Center, Center, new Vector2(start + i * (dash + gap), 0), new Vector2(dash, 2)).color = new Color(Dark.r, Dark.g, Dark.b, 0.2f);
        }

        // ───────────── Popup (cùng khung: rộng 342, viền 4, bo 28, #FFFBEF → #F7E9C6, bóng 0 8px #A8977A + 0 20px 30px) ─────────────

        /// <summary>Popup Tạm dừng: tiêu đề 40 trắng trong khung; Tiếp tục (xanh, 64), Chơi lại và Trang chủ (trắng, 62); hiện ngay.</summary>
        internal static PausePopup BuildPausePopup(Transform root)
        {
            // đệm 24/20, cách 14: tiêu đề 64 → 70 → 62 → 62 ⇒ khung cao 356
            const float H = 356f;
            var (popupGo, panel) = PopupShell("PausePopup", root, "pause", H);
            Vector2 Y(float fromTop) => new Vector2(0, H / 2f - fromTop);
            var pauseTitle = PopupTitle(panel, "TẠM DỪNG", Color.white, 40, Y(4 + 24 + 32), 0f);
            const float W = 342 - 8 - 40;
            var resume = GBtn("ResumeButton", panel, Spr("UI/button_green"), Center, Y(4 + 24 + 64 + 14 + 35), W, 70, "Tiếp tục", 22, Dark, Spr("UI/icon_play"), 24);
            var restart = GBtn("RestartButton", panel, Spr("UI/button_white"), Center, Y(4 + 24 + 64 + 14 + 70 + 14 + 31), W, 62, "Chơi lại", 18, Dark, Spr("UI/icon_restart"), 22);
            var home = GBtn("HomeButton", panel, Spr("UI/button_white"), Center, Y(4 + 24 + 64 + 14 + 70 + 14 + 62 + 14 + 31), W, 62, "Trang chủ", 18, Dark, Spr("UI/icon_home"), 22);

            var popup = popupGo.AddComponent<PausePopup>();
            Set(popup, "panel", panel);
            Set(popup, "dim", popupGo.GetComponent<Image>());
            // Thiết kế cho hiện ngay; làm mềm hơn: khung phóng nhẹ 0,9 → 1 trong 0,28 s, tiêu đề + 3 nút hiện lần lượt
            SetFloat(popup, "fromScale", 0.9f);
            SetFloat(popup, "showDuration", 0.28f);
            SetStagger(popup,
                (pauseTitle.rectTransform, 0.04f, 0.3f, 0.8f, 6f),
                ((RectTransform)resume.transform, 0.09f, 0.3f, 0.92f, 10f),
                ((RectTransform)restart.transform, 0.14f, 0.3f, 0.92f, 10f),
                ((RectTransform)home.transform, 0.19f, 0.3f, 0.92f, 10f));
            Set(popup, "resumeButton", resume);
            Set(popup, "restartButton", restart);
            Set(popup, "homeButton", home);
            popupGo.SetActive(false);
            return popup;
        }

        /// <summary>
        /// Popup Hết giờ (.resIn: scale 0,7 → 1, alpha 0 → 1, 0,45 s): tiêu đề 46 vàng xoay −3°, "ĐIỂM VÁN NÀY" + điểm 50,
        /// 3 ô ĐẬP TRÚNG / COMBO / XU, nút xem quảng cáo x2 xu (chờ bước 6), Chơi lại + Trang chủ.
        /// Hàng 3 ngôi sao của thiết kế bỏ qua vì GameSpec không còn hệ sao.
        /// </summary>
        internal static RoundEndPopup BuildEndPopup(Transform root)
        {
            const float H = 383f;
            var (popupGo, panel) = PopupShell("RoundEndPopup", root, "end", H);
            panel.gameObject.AddComponent<CanvasGroup>();
            Vector2 Y(float fromTop) => new Vector2(0, H / 2f - fromTop);
            const float inner = 342 - 8 - 36;

            var endTitle = PopupTitle(panel, "HẾT GIỜ!", Yellow, 46, Y(24 + 23), 3f);

            var scoreLabel = Txt("ScoreLabel", panel, "ĐIỂM VÁN NÀY", fontHeavy, 13, GuideMuted, TextAnchor.MiddleCenter, Center, Y(82 + 6.5f), new Vector2(inner, 16));
            var scoreValue = Txt("ScoreValue", panel, "340", fontHeavy, 50, Dark, TextAnchor.MiddleCenter, Center, Y(82 + 13 + 25), new Vector2(inner, 54));
            var scoreShadow = scoreValue.gameObject.AddComponent<Shadow>();
            scoreShadow.effectColor = Hex("E3D3B0");
            scoreShadow.effectDistance = new Vector2(0, -3 * PT);

            // 3 ô: rộng (298 − 16) / 3 = 94, cao 52 + bóng 4, bo 14, viền 3; nhãn 11 #5E4E3A, số 20
            const float boxW = (inner - 16) / 3f, boxTop = 157f;
            var statWhite = UiArt.Box("end_stat_white", boxW, 52, 14, 3, Dark, PanelWhite, 4, Hex("B9A98C"));
            var statGold = UiArt.Box("end_stat_gold", boxW, 52, 14, 3, Dark, new[] { new UiArt.Stop(0f, "FFF3A0"), new UiArt.Stop(1f, "FFD84A") }, 4, Hex("B9A98C"));
            Text Stat(string key, string label, int col, Sprite sprite)
            {
                float cx = -inner / 2f + boxW / 2f + col * (boxW + 8);
                var box = Img(key + "Box", panel, sprite, Center, new Vector2(0.5f, 1f), new Vector2(cx, H / 2f - boxTop), new Vector2(boxW, 56));
                Txt("Label", box.transform, label, fontHeavy, 11, GuideMuted, TextAnchor.MiddleCenter, Center, new Vector2(0, 28 - 15f), new Vector2(boxW, 14));
                return Txt("Value", box.transform, "0", fontHeavy, 20, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, 28 - 32.1f), new Vector2(boxW, 24));
            }
            var hitsValue = Stat("Hits", "ĐẬP TRÚNG", 0, statWhite);
            var comboValue = Stat("MaxCombo", "COMBO", 1, statWhite);
            var coinsValue = Stat("Coins", "XU", 2, statGold);

            // Nút xem quảng cáo (xanh, mặt 62): icon 26 + "Xem quảng cáo" bên trái, chip "x2 xu" bên phải
            var ad = GBtn("DoubleCoinsButton", panel, Spr("UI/button_blue"), Center, Y(221.1f + 31), inner, 62, null, 0, Dark, null, 0);
            var adContent = ad.transform.Find("Content");
            var adLeft = Rect("Left", adContent, Center, new Vector2(0f, 0.5f), new Vector2(-inner / 2f + 14 + 3, 0), new Vector2(180, 30));
            var adRow = adLeft.gameObject.AddComponent<HorizontalLayoutGroup>();
            adRow.spacing = 8 * PT;
            adRow.childAlignment = TextAnchor.MiddleLeft;
            adRow.childControlWidth = true;
            adRow.childControlHeight = false;
            adRow.childForceExpandWidth = adRow.childForceExpandHeight = false;
            Destroy(adContent.GetComponent<HorizontalLayoutGroup>());
            var adIcon = Img("Icon", adLeft, Spr("UI/icon_watch_ad"), Center, Center, Vector2.zero, new Vector2(26, 26));
            adIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 26 * PT;
            Txt("Label", adLeft, "Xem quảng cáo", fontHeavy, 17, Dark, TextAnchor.MiddleLeft, Center, Vector2.zero, new Vector2(140, 26));
            var x2 = PillLabel("Badge", adContent, UiArt.Box("chip_x2_coins", 40, 32, 16, 2, Dark, new[] { new UiArt.Stop(0f, "FFF3A0"), new UiArt.Stop(1f, "FFC93C") }, sliceX: true), "x2 xu", 18, Dark, 32, 12);
            var x2Rt = (RectTransform)x2.transform.parent;
            x2Rt.pivot = new Vector2(1f, 0.5f);
            x2Rt.anchoredPosition = new Vector2((inner / 2f - 10 - 3) * PT, 0);

            // Chơi lại (vàng) + Trang chủ (trắng): 2 cột cách 10, mặt 62, chữ 18, icon 22
            float halfW = (inner - 10) / 2f;
            var replay = GBtn("ReplayButton", panel, Spr("UI/button_yellow"), Center, Y(299.1f + 31) - new Vector2(halfW / 2f + 5, 0), halfW, 62, "Chơi lại", 18, Dark, Spr("UI/icon_restart"), 22);
            var home = GBtn("HomeButton", panel, Spr("UI/button_white"), Center, Y(299.1f + 31) + new Vector2(halfW / 2f + 5, 0), halfW, 62, "Trang chủ", 18, Dark, Spr("UI/icon_home"), 22);

            var popup = popupGo.AddComponent<RoundEndPopup>();
            Set(popup, "panel", panel);
            Set(popup, "dim", popupGo.GetComponent<Image>());
            // Khung giữ .resIn của thiết kế (0,7 → 1, 0,45 s); nội dung bật lần lượt, điểm đếm tăng từ 0 (RoundEndPopup)
            RectTransform Box(Text value) => (RectTransform)value.transform.parent;
            SetStagger(popup,
                (endTitle.rectTransform, 0.10f, 0.4f, 1.5f, 0f),
                (scoreLabel.rectTransform, 0.22f, 0.3f, 0.9f, 6f),
                (scoreValue.rectTransform, 0.22f, 0.35f, 0.7f, 6f),
                (Box(hitsValue), 0.38f, 0.32f, 0.6f, 8f),
                (Box(comboValue), 0.45f, 0.32f, 0.6f, 8f),
                (Box(coinsValue), 0.52f, 0.32f, 0.6f, 8f),
                ((RectTransform)ad.transform, 0.60f, 0.3f, 0.92f, 10f),
                ((RectTransform)replay.transform, 0.66f, 0.3f, 0.92f, 10f),
                ((RectTransform)home.transform, 0.71f, 0.3f, 0.92f, 10f));
            Set(popup, "scoreValue", scoreValue);
            Set(popup, "hitsValue", hitsValue);
            Set(popup, "maxComboValue", comboValue);
            Set(popup, "coinsValue", coinsValue);
            Set(popup, "doubleCoinsButton", ad);
            Set(popup, "replayButton", replay);
            Set(popup, "homeButton", home);
            popupGo.SetActive(false);
            return popup;
        }

        static void Destroy(Object o) => Object.DestroyImmediate(o);

        /// <summary>Gán danh sách PopupView.stagger: (đối tượng, trễ, thời lượng, scale đầu, dịch xuống đầu pt).</summary>
        static void SetStagger(PopupView popup, params (RectTransform target, float delay, float duration, float fromScale, float fromOffsetY)[] items)
        {
            var so = new SerializedObject(popup);
            var list = so.FindProperty("stagger");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("target").objectReferenceValue = items[i].target;
                e.FindPropertyRelative("delay").floatValue = items[i].delay;
                e.FindPropertyRelative("duration").floatValue = items[i].duration;
                e.FindPropertyRelative("fromScale").floatValue = items[i].fromScale;
                e.FindPropertyRelative("fromOffsetY").floatValue = items[i].fromOffsetY;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Lớp phủ rgba(30,20,10,.6) chặn chạm + khung popup 342 × H có bóng cứng 8 và bóng mờ.</summary>
        internal static (GameObject, RectTransform) PopupShell(string name, Transform root, string key, float h)
        {
            var overlay = Img(name, root, null, Center, Center, Vector2.zero, Vector2.zero);
            Stretch(overlay.rectTransform, 0);
            overlay.color = new Color32(30, 20, 10, 153);
            overlay.raycastTarget = true; // chặn chạm xuống màn chơi
            var panel = Rect("Panel", overlay.transform, Center, Center, Vector2.zero, new Vector2(342, h));
            Img("SoftShadow", panel, UiArt.SoftShadow($"popup_{key}_soft", 342, h, 28, 30, 0.35f), Center, Center, new Vector2(0, -20), new Vector2(342 + 60, h + 60));
            Img("Body", panel, UiArt.Box($"popup_{key}", 342, h, 28, 4, Dark, new[] { new UiArt.Stop(0f, "FFFBEF"), new UiArt.Stop(1f, "F7E9C6") }, 8, Hex("A8977A")),
                Top, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(342, h + 8));
            return (overlay.gameObject, panel);
        }

        /// <summary>Tiêu đề popup kiểu .txt3d: viền 5 (thấy 2,5) + bóng 0 4px; xoay theo CSS (độ, dương = ngược kim đồng hồ ở Unity).</summary>
        internal static Text PopupTitle(RectTransform panel, string text, Color color, float size, Vector2 posPt, float rotation)
        {
            var t = Txt("Title", panel, text, fontHeavy, size, color, TextAnchor.MiddleCenter, Center, posPt, new Vector2(320, size * 1.3f));
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            t.gameObject.AddComponent<TextStroke>().Configure(Dark, 2.5f * PT, new Vector2(0, -4 * PT));
            return t;
        }

        // ───────────────────────── Tiện ích UI ─────────────────────────

        internal static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        internal static readonly Vector2 Top = new Vector2(0.5f, 1f);
        internal static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        /// <summary>Tâm (pt, gốc trên-trái khung 390×844) → vị trí neo giữa-trên / giữa / giữa-dưới.</summary>
        internal static Vector2 TopPos(float cx, float cy) => new Vector2(cx - DesignW / 2f, -cy);
        internal static Vector2 CenterPos(float cx, float cy) => new Vector2(cx - DesignW / 2f, DesignH / 2f - cy);
        internal static Vector2 BottomPos(float cx, float cy) => new Vector2(cx - DesignW / 2f, DesignH - cy);

        internal static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 posPt, Vector2 sizePt)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = posPt * PT;
            rt.sizeDelta = sizePt * PT;
            return rt;
        }

        internal static void Stretch(RectTransform rt, float insetPt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = Vector2.one * insetPt * PT;
            rt.offsetMax = -Vector2.one * insetPt * PT;
        }

        internal static Image Img(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 posPt, Vector2 sizePt)
        {
            var rt = Rect(name, parent, anchor, pivot, posPt, sizePt);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        internal static void Sliced(Image img, float ppuMultiplier)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppuMultiplier;
        }

        internal static Text Txt(string name, Transform parent, string text, Font font, float sizePt, Color color, TextAnchor align, Vector2 anchor, Vector2 posPt, Vector2 boxPt)
        {
            var rt = Rect(name, parent, anchor, Center, posPt, boxPt);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = Mathf.RoundToInt(sizePt * PT);
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        internal static void AddOutline(Text t, Color color, float distancePx)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distancePx, -distancePx);
        }

        internal static Button Btn(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 posPt, Vector2 sizePt,
            string label, float fontPt, Color textColor, Sprite icon)
        {
            var img = Img(name, parent, sprite, anchor, Center, posPt, sizePt);
            Sliced(img, 1f);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = Color.white;
            btn.colors = colors;
            img.gameObject.AddComponent<PressEffect>();

            float textX = 0f;
            if (icon != null && label != null)
            {
                Img("Icon", img.transform, icon, Center, Center, new Vector2(-sizePt.x / 2f + 32, 3), new Vector2(30, 30));
                textX = 12f;
            }
            if (label != null)
            {
                var t = Txt("Label", img.transform, label, fontHeavy, fontPt, textColor, TextAnchor.MiddleCenter, Center, new Vector2(textX, 3), new Vector2(sizePt.x - 40, sizePt.y));
                if (textColor == Color.white) AddOutline(t, Hex("23803A"), 4);
            }
            return btn;
        }

        // ───────────────────────── Mặt nạ hố ─────────────────────────

        // Kích thước mặt nạ (px @3x = đơn vị canvas): rộng hơn hố 330 px để thú bẹp (x1,3) không bị cắt hai bên,
        // cao đủ cho thú nảy vượt đỉnh.
        internal const int MaskW = 420, MaskH = 540;
        // Cắt lấn vào mép trước vài px để không lộ khe giữa thân thú và mép hố.
        internal const int MaskTuck = 9;

        /// <summary>
        /// Tạo PNG mặt nạ từ lớp trước của hố: với mỗi cột, vùng hiện = phía trên điểm cao nhất của mép hố.
        /// Đọc thẳng file PNG nên không cần bật Read/Write cho texture gốc.
        /// </summary>
        /// <summary>
        /// Mặt nạ vùng nước của ảnh nền thẻ: mỗi cột hiện từ mép trên tới điểm đầu tiên không còn là nước (xanh dương),
        /// để bong bóng và tia nắng nằm sau đồi cát như bản thiết kế (ở đó chúng được vẽ trước lớp đồi).
        /// </summary>
        internal static Sprite CreateSkyMask(string cardPath, string maskPath)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(cardPath));
            int w = src.width, h = src.height;
            var px = src.GetPixels32();
            Object.DestroyImmediate(src);
            var mask = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                bool water = true;
                for (int y = h - 1; y >= 0; y--) // GetPixels32: hàng 0 là đáy ảnh, quét từ trên xuống
                {
                    var c = px[y * w + x];
                    if (water && !(c.b > c.r + 30)) water = false;
                    mask[y * w + x] = new Color32(255, 255, 255, water ? (byte)255 : (byte)0);
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(mask);
            tex.Apply();
            File.WriteAllBytes(maskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);
            return LoadSprite(maskPath);
        }

        internal static Sprite CreateHoleMask(string frontPath, string maskPath)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(File.ReadAllBytes(frontPath)))
            {
                Debug.LogError($"[WhackyZoo] Không đọc được {frontPath}");
                return null;
            }
            int sw = src.width, sh = src.height;
            var px = src.GetPixels32();
            Object.DestroyImmediate(src);

            // Chiều cao cắt (tính từ đáy) cho từng cột của ảnh hố; -1 = cột không có mép
            var cut = new int[sw];
            for (int x = 0; x < sw; x++)
            {
                cut[x] = -1;
                for (int y = sh - 1; y >= 0; y--) // GetPixels32: hàng 0 là đáy ảnh
                {
                    if (px[y * sw + x].a > 128) { cut[x] = Mathf.Max(0, y + 1 - MaskTuck); break; }
                }
            }
            // Cột trống ở hai rìa: lấy theo cột có mép gần nhất
            int first = Array.FindIndex(cut, c => c >= 0), last = Array.FindLastIndex(cut, c => c >= 0);
            if (first < 0) { first = 0; last = sw - 1; for (int x = 0; x < sw; x++) cut[x] = 0; }
            for (int x = 0; x < first; x++) cut[x] = cut[first];
            for (int x = last + 1; x < sw; x++) cut[x] = cut[last];

            int offsetX = (MaskW - sw) / 2;
            var mask = new Color32[MaskW * MaskH];
            for (int x = 0; x < MaskW; x++)
            {
                int c = cut[Mathf.Clamp(x - offsetX, 0, sw - 1)];
                for (int y = 0; y < MaskH; y++)
                    mask[y * MaskW + x] = y >= c ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }

            var tex = new Texture2D(MaskW, MaskH, TextureFormat.RGBA32, false);
            tex.SetPixels32(mask);
            tex.Apply();
            File.WriteAllBytes(maskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);
            return LoadSprite(maskPath);
        }

        /// <summary>Dải alpha dọc (trên đặc, dưới trong suốt) dùng chung cho nền Cách chơi và mép mờ danh sách.</summary>
        internal static Sprite FadeSprite()
        {
            const string path = Art + "/Generated/gen_fade.png";
            if (!File.Exists(path))
            {
                EnsureFolder(Art + "/Generated");
                const int h = 128;
                var px = new Color32[4 * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < 4; x++)
                        px[y * 4 + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(y / (h - 1f) * 255f));
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return LoadSprite(path);
        }

        // ───────────────────────── Asset ─────────────────────────

        internal static Sprite Spr(string pathInArt) => LoadSprite($"{Art}/{pathInArt}.png");

        internal static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
            {
                Debug.LogError($"[WhackyZoo] Không tìm thấy ảnh {path}");
                return null;
            }

            Vector4 border = Vector4.zero;
            string file = Path.GetFileNameWithoutExtension(path);
            if (file.StartsWith("button_")) border = ButtonBorder;
            else if (file.StartsWith("panel_")) border = PanelBorder;
            else if (file == "gen_round_rect") border = new Vector4(48, 48, 48, 48); // ảnh sinh trong HomeSceneBuilder
            else if (file == "gen_card26" || file == "gen_card26_frame" || file == "gen_card26_mask") border = new Vector4(80, 80, 80, 80); // bo 26 pt = 78 px
            else if (file == "gen_card26_softshadow") border = new Vector4(146, 146, 146, 146); // lề 66 + bo 80

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            // Ảnh @3x thường bị thu nhỏ 1,5–3 lần (máy độ phân giải thấp, hố hàng xa, nút 9-slice thu góc);
            // không có mipmap thì GPU bỏ qua điểm ảnh khi thu nhỏ → mép răng cưa. Mipmap + trilinear làm mép mịn.
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
            if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; dirty = true; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f)) { importer.spritePixelsPerUnit = 100f; dirty = true; }
            if (importer.maxTextureSize < 4096) { importer.maxTextureSize = 4096; dirty = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; } // loang màu mép, tránh viền tối/sáng quanh hình
            if (importer.textureCompression != TextureImporterCompression.CompressedHQ) { importer.textureCompression = TextureImporterCompression.CompressedHQ; dirty = true; } // Android: ASTC 4x4, mép hình sắc nét
            if (dirty) importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Tải asset có sẵn (giữ GUID để các tham chiếu không gãy) hoặc tạo mới.</summary>
        internal static T Upsert<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        internal const string HomeScenePath = "Assets/Scenes/Home.unity";

        /// <summary>Thứ tự build: Home (mở đầu tiên) rồi Game; bỏ scene Land cũ.</summary>
        internal static void UpdateBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            // bỏ cả scene đã xoá khỏi đĩa (Land.unity, SampleScene.unity…)
            scenes.RemoveAll(s => s.path == ScenePath || s.path == OldScenePath || s.path == HomeScenePath || !File.Exists(s.path));
            int i = 0;
            if (File.Exists(HomeScenePath)) scenes.Insert(i++, new EditorBuildSettingsScene(HomeScenePath, true));
            if (File.Exists(ScenePath)) scenes.Insert(i, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void LoadFonts()
        {
            fontHeavy = AssetDatabase.LoadAssetAtPath<Font>($"{Art}/Fonts/Baloo2-ExtraBold.ttf");
            fontBold = AssetDatabase.LoadAssetAtPath<Font>($"{Art}/Fonts/Baloo2-Bold.ttf");
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[WhackyZoo] {target.GetType().Name} không có trường {field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray<T>(Object target, string field, IList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
