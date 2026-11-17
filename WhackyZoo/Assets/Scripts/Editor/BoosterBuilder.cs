using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static WhackyZoo.EditorTools.GameSceneBuilder;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Dữ liệu 5 vật phẩm hỗ trợ (Docs/GameSpec.md › Vật phẩm hỗ trợ) và thanh vật phẩm dưới cùng màn chơi
    /// (Docs/DesignSource/GameLand25D.dc.html: toolbar 5 cột, nút .gbtn cao 64, icon 34, nhãn 11, badge số lượng).
    /// </summary>
    internal static class BoosterBuilder
    {
        internal const string CatalogPath = DataRoot + "/BoosterCatalog.asset";

        struct Def
        {
            public string id, file, displayName, barLabel, guideName, guideHint, cardName, desc;
            public BoosterEffect effect;
            public float amount, duration;
            public int price, packPrice, starter;
            public string hi, mid, lo, edge; // màu .gbtn của nút (--hi/--base/--lo/--edge)
        }

        // Thứ tự = thứ tự nút trên thanh, ô ở màn Cách chơi và Cửa hàng. Giá gói x5 = giá của 4 (giảm 20%).
        // Tặng dùng thử cho người chơi mới theo ảnh thiết kế (qty: +5s x2, các món khác x1).
        static readonly Def[] Defs =
        {
            new Def { id = "time", file = "booster_plus5s", effect = BoosterEffect.AddTime, amount = 5, price = 150, packPrice = 600, starter = 2,
                displayName = "+5 Giây", cardName = "+5 Giây", desc = "Cộng thêm 5 giây vào đồng hồ khi đang chơi.", barLabel = "+5s", guideName = "+5 giây", guideHint = "Cộng 5 giây",
                hi = "D8F1FF", mid = "8FD3FF", lo = "4FB0EE", edge = "2A7FBF" },
            new Def { id = "x2", file = "booster_double_score", effect = BoosterEffect.DoubleScore, amount = 2, duration = 10, price = 300, packPrice = 1200, starter = 1,
                displayName = "Nhân Đôi Điểm", cardName = "Nhân Đôi", desc = "Mọi điểm nhận được tăng gấp đôi trong 10 giây.", barLabel = "x2", guideName = "Nhân đôi", guideHint = "Điểm x2 trong 10s",
                hi = "FFE0EA", mid = "FFB3C7", lo = "F07FA0", edge = "C25478" },
            new Def { id = "shield", file = "booster_bomb_shield", effect = BoosterEffect.BombShield, price = 250, packPrice = 1000, starter = 1,
                displayName = "Khiên Bom", cardName = "Khiên Bom", desc = "Đỡ một lần đập nhầm vật cản: không bị trừ điểm, giữ combo.", barLabel = "Khiên", guideName = "Khiên", guideHint = "Đỡ 1 lần đập nhầm",
                hi = "E2F4FF", mid = "B8E2FF", lo = "7CC8FF", edge = "2A7FBF" },
            new Def { id = "freeze", file = "booster_freeze", effect = BoosterEffect.FreezeTime, duration = 5, price = 350, packPrice = 1400, starter = 1,
                displayName = "Đóng Băng Giờ", cardName = "Đóng Băng", desc = "Dừng đồng hồ 5 giây, thú vẫn ló lên bình thường.", barLabel = "Băng", guideName = "Đóng băng", guideHint = "Dừng giờ 5 giây",
                hi = "FFFFFF", mid = "E6F6FF", lo = "BDE7FF", edge = "5FA8D8" },
            new Def { id = "fire", file = "booster_combo_charm", effect = BoosterEffect.ComboCharm, amount = 3, price = 500, packPrice = 2000, starter = 1,
                displayName = "Bùa Combo", cardName = "Bùa Combo", desc = "Combo bắt đầu từ x3 thay vì x1 trong cả ván.", barLabel = "Combo", guideName = "Bùa combo", guideHint = "Combo luôn từ x3",
                hi = "FFD3A0", mid = "FF9F43", lo = "E07A1A", edge = "A8560E" },
        };

        // Thanh: rộng 358 (lề 16), viền 3, bo 20, đệm 10/8/8, 5 cột cách 6 → nút 62,4 × 64; đáy thanh cách đáy màn 22
        const float BarW = 358, BarH = 88, BarTop = 734, BtnW = 62.4f, BtnH = 64, BtnRadius = 14, BtnEdge = 5, Gap = 6;
        const float BtnTop = BarTop + 3 + 10, FirstCx = 16 + 3 + 8 + BtnW / 2f;

        internal static BoosterCatalog CreateBoosters()
        {
            EnsureFolder($"{DataRoot}/Boosters");
            var list = new BoosterData[Defs.Length];
            for (int i = 0; i < Defs.Length; i++)
            {
                var d = Defs[i];
                var b = Upsert<BoosterData>($"{DataRoot}/Boosters/Booster_{Capitalize(d.id)}.asset");
                b.id = d.id;
                b.effect = d.effect;
                b.amount = d.amount;
                b.duration = d.duration;
                b.displayName = d.displayName;
                b.barLabel = d.barLabel;
                b.guideName = d.guideName;
                b.guideHint = d.guideHint;
                b.shopCardName = d.cardName;
                b.shopDescription = d.desc;
                b.icon = LoadSprite($"{Art}/Boosters/{d.file}.png");
                b.buttonSprite = UiArt.GameButton($"booster_btn_{d.id}", BtnW, BtnH, BtnRadius, d.hi, d.mid, d.lo, d.edge, BtnEdge);
                b.price = d.price;
                b.packPrice = d.packPrice;
                b.packSize = 5;
                b.starterCount = d.starter;
                EditorUtility.SetDirty(b);
                list[i] = b;
            }
            var catalog = Upsert<BoosterCatalog>(CatalogPath);
            catalog.boosters = list;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        internal static BoosterBar BuildBar(Transform parent, BoosterCatalog catalog)
        {
            // Nền thanh: kem #FFFBEF → #F7E9C6, viền 3, bo 20, bóng cứng 0 4px #B9A98C + bóng mờ 0 10px 14px rgba(0,0,0,.2)
            float barCy = BarTop + BarH / 2f;
            Img("BoosterBarShadow", parent, UiArt.SoftShadow("booster_bar_soft", BarW, BarH, 20, 14, 0.2f), Bottom, Center,
                BottomPos(195, barCy + 10), new Vector2(BarW + 28, BarH + 28));
            var bar = Img("BoosterBar", parent, UiArt.Box("booster_bar", BarW, BarH, 20, 3, Dark,
                new[] { new UiArt.Stop(0f, "FFFBEF"), new UiArt.Stop(1f, "F7E9C6") }, 4, Hex("B9A98C")),
                Bottom, new Vector2(0.5f, 1f), BottomPos(195, BarTop), new Vector2(BarW, BarH + 4));
            bar.raycastTarget = true; // chạm vào khe giữa các nút không lọt xuống hố

            var glowSprite = UiArt.ActiveGlow("booster_glow", BtnW, BtnH, BtnRadius);
            Sprite Badge(string key, string top, string bottom) =>
                UiArt.Box($"booster_badge_{key}", 22, 20, 10, 2, Dark, new[] { new UiArt.Stop(0f, top), new UiArt.Stop(1f, bottom) }, sliceX: true);
            var badgeRed = Badge("red", "FF8A80", "E53935");
            var badgeGreen = Badge("green", "A6F09A", "2E9E44");
            var badgeGrey = Badge("grey", "9A8F80", "9A8F80");

            var so = new SerializedObject(bar.gameObject.AddComponent<BoosterBar>());
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = Defs.Length;
            float faceCy = BtnTop + BtnH / 2f;
            for (int i = 0; i < Defs.Length; i++)
            {
                float cx = FirstCx + i * (BtnW + Gap);
                var slot = Rect($"Booster{i}", bar.transform, Top, Center, TopPos(cx, faceCy - BarTop), new Vector2(BtnW, BtnH));
                var glow = Img("Glow", slot, glowSprite, Center, Center, Vector2.zero, new Vector2(BtnW + 48, BtnH + 48));
                glow.gameObject.SetActive(false);

                var data = catalog.boosters[i];
                // Ảnh nút gồm cạnh 3D 5 pt phía dưới → tâm ảnh thấp hơn tâm mặt nút 2,5
                var face = Img("Button", slot, data.buttonSprite, Center, Center, new Vector2(0, -BtnEdge / 2f), new Vector2(BtnW, BtnH + BtnEdge));
                face.raycastTarget = true;
                var button = face.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                face.gameObject.AddComponent<PressEffect>();
                var group = face.gameObject.AddComponent<CanvasGroup>();

                // Cột icon 34 + cách 2 + nhãn 11 (cao dòng 1) căn giữa mặt nút: tâm icon cao hơn tâm mặt 6,5; nhãn thấp hơn 18
                float faceOffset = BtnEdge / 2f; // toạ độ con tính từ tâm ảnh
                var icon = Img("Icon", face.transform, data.icon, Center, Center, new Vector2(0, faceOffset + 6.5f), new Vector2(34, 34));
                icon.preserveAspect = true;
                var label = Txt("Label", face.transform, data.barLabel, fontHeavy, 11, Dark, TextAnchor.MiddleCenter, Center, new Vector2(0, faceOffset - 18f), new Vector2(BtnW, 14));

                // Badge: top −10, right −6 so với mép trong viền → góc trên-phải ở (mép phải + 3, mép trên − 7); cao 20, tối thiểu 22
                var badgeText = PillLabel("Badge", face.transform, badgeRed, "x1", 11, Color.white, 20, 4); // đệm ngang 4 + viền 2
                var badgeImg = (RectTransform)badgeText.transform.parent;
                badgeImg.anchorMin = badgeImg.anchorMax = new Vector2(1f, 1f);
                badgeImg.pivot = new Vector2(1f, 1f);
                badgeImg.anchoredPosition = new Vector2(3f, 7f) * PT; // mép trên ảnh nút = mép trên mặt nút
                badgeImg.gameObject.AddComponent<LayoutElement>().minWidth = 22 * PT;
                TextShadow(badgeText, 1, 0.35f);

                var e = slotsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("button").objectReferenceValue = button;
                e.FindPropertyRelative("face").objectReferenceValue = face;
                e.FindPropertyRelative("icon").objectReferenceValue = icon;
                e.FindPropertyRelative("label").objectReferenceValue = label;
                e.FindPropertyRelative("badge").objectReferenceValue = badgeImg.GetComponent<Image>();
                e.FindPropertyRelative("badgeText").objectReferenceValue = badgeText;
                e.FindPropertyRelative("glow").objectReferenceValue = glow.gameObject;
                e.FindPropertyRelative("group").objectReferenceValue = group;
            }
            so.FindProperty("badgeRed").objectReferenceValue = badgeRed;
            so.FindProperty("badgeGreen").objectReferenceValue = badgeGreen;
            so.FindProperty("badgeGrey").objectReferenceValue = badgeGrey;
            so.ApplyModifiedPropertiesWithoutUndo();
            return bar.GetComponent<BoosterBar>();
        }
    }
}
