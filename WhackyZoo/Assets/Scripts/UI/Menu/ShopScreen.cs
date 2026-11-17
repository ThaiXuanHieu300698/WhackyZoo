using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Cửa hàng (Docs/DesignSource/Shop25D.dc.html): 3 tab Dụng cụ / Thú / Vật phẩm, khung chi tiết món đang chọn
    /// (tên, mô tả, lưới tầm đập của dụng cụ / dòng điểm thưởng của thú / số đang có, nút mua / trang bị / bật thú), lưới thẻ, ô Tích xu miễn phí.
    /// Mua bằng xu lưu ngay; nút tiền thật và quảng cáo thuộc bước 6 nên chưa có tác dụng.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        const float PxToUnits = 3f;
        enum Tab { Tool, Pet, Item }

        [Serializable]
        public struct PetChip
        {
            public string mapId;
            public Image background;
            public Text label;
            public Button button;
        }

        [SerializeField] ScreenRouter router;
        [SerializeField] ShopCatalog catalog;
        [SerializeField] Button backButton;
        [Tooltip("Màn Điểm danh (nhãn \"Miễn phí khi điểm danh …\" mở màn này).")]
        [SerializeField] CanvasGroup dailyScreen;

        [Header("Xu")]
        [SerializeField] Text coinsText;
        [SerializeField] RectTransform coinChip;

        [Header("Tab")]
        [SerializeField] Image[] tabBackgrounds;
        [SerializeField] Text[] tabLabels;
        [SerializeField] Button[] tabButtons;
        [SerializeField] Color tabTextSelected = new Color32(0x3A, 0x24, 0x12, 0xFF);
        [SerializeField] Color tabTextNormal = new Color32(0x5E, 0x4E, 0x3A, 0xFF);

        [Header("Khung chi tiết")]
        [SerializeField] RectTransform detail;
        [SerializeField] Text nameText;
        [SerializeField] GameObject premiumBadge;
        [SerializeField] Text descText;
        [Tooltip("Chiều cao mô tả = số dòng × cao dòng (14 px × line-height 1,3).")]
        [SerializeField] LayoutElement descLayout;
        [SerializeField] float descLineHeight = 18.2f;
        [Tooltip("Dòng điểm thưởng của thú (chỉ dùng cho thú).")]
        [SerializeField] Text pointsText;
        [Tooltip("Lưới 3×3 mô phỏng tầm đập (chỉ dùng cho dụng cụ).")]
        [SerializeField] HitRangeView hitRange;
        [SerializeField] GameObject ownedRow;
        [SerializeField] Text ownedQty;
        [Tooltip("Nhãn vàng nhận miễn phí qua Điểm danh / nhãn xanh đang dùng thử.")]
        [SerializeField] GameObject freeRow;
        [SerializeField] Button freePill;
        [SerializeField] Image freeImage;
        [SerializeField] GameObject freeIcon;
        [SerializeField] Text freeText;
        [SerializeField] Sprite freeSprite;
        [SerializeField] Sprite trialSprite;

        [Header("Nút trong khung chi tiết")]
        [SerializeField] Button equippedButton;
        [SerializeField] Button equipButton;
        [SerializeField] Button petOnButton;
        [SerializeField] Text petOnText;
        [SerializeField] Button petOffButton;
        [SerializeField] Text petOffText;
        [SerializeField] Button baseButton;
        [SerializeField] Button buyButton;
        [SerializeField] Text buyText;
        [SerializeField] Button shortButton;
        [SerializeField] Text shortText;
        [SerializeField] Button cashButton;
        [SerializeField] Text cashText;
        [SerializeField] Button buy1Button;
        [SerializeField] Text buy1Price;
        [SerializeField] Button buy5Button;
        [SerializeField] Text buy5Price;

        [Header("Lưới")]
        [SerializeField] GameObject toolGrid;
        [SerializeField] ScrollRect toolScroll;
        [Tooltip("Ô chứa lưới dụng cụ: cao tối đa toolScrollMax, co lại cho vừa chỗ trống trên ô Tích xu.")]
        [SerializeField] LayoutElement toolScrollLayout;
        [SerializeField] float toolScrollMax = 300f;
        [SerializeField] ShopCard[] toolCards;
        [SerializeField] GameObject petPanel;
        [SerializeField] PetChip[] petChips;
        [SerializeField] Sprite petChipNormal;
        [Tooltip("Nền chip map đang chọn, cùng thứ tự với petChips.")]
        [SerializeField] Sprite[] petChipSelected;
        [SerializeField] ShopCard[] petCards;
        [SerializeField] ScrollRect petScroll;
        [Tooltip("Ô chứa danh sách thú: cao tối đa petScrollMax, co lại cho vừa chỗ trống trên ô Tích xu (flex-shrink như thiết kế).")]
        [SerializeField] LayoutElement petScrollLayout;
        [SerializeField] float petScrollMax = 262f;
        [SerializeField] RectTransform body;
        [SerializeField] RectTransform freePanel;
        [SerializeField] float pageGap = 12f;
        [SerializeField] GameObject itemGrid;
        [SerializeField] ShopCard[] itemCards;

        [Header("Nhãn trên thẻ")]
        [SerializeField] Sprite tagInUse;
        [SerializeField] Sprite tagBase;
        [SerializeField] Sprite tagOwned;
        [SerializeField] Sprite tagPrice;

        [Header("Ô Tích xu miễn phí (quảng cáo — bước 6)")]
        [SerializeField] Button freeCoinsButton;


        Tab tab = Tab.Tool;
        string petMap = "land";
        ShopCard pickedTool, pickedPet, pickedItem;
        int shownCoins = -1;
        float bumpT = -1f, trialRefreshT;

        void Awake()
        {
            backButton.onClick.AddListener(() => router.Back());
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var t = (Tab)i;
                tabButtons[i].onClick.AddListener(() => { AudioManager.Play(Sfx.Click); SelectTab(t); });
            }
            for (int i = 0; i < petChips.Length; i++)
            {
                string id = petChips[i].mapId;
                petChips[i].button.onClick.AddListener(() => SelectPetMap(id));
            }

            BindCards();
            equipButton.onClick.AddListener(Equip);
            petOnButton.onClick.AddListener(() => TogglePet(false));
            petOffButton.onClick.AddListener(() => TogglePet(true));
            buyButton.onClick.AddListener(Buy);
            buy1Button.onClick.AddListener(() => BuyBooster(1));
            buy5Button.onClick.AddListener(() => BuyBooster(5));
            foreach (var b in new[] { equippedButton, baseButton, shortButton }) b.interactable = false;
            if (freeCoinsButton != null) UiUtil.SetAvailable(freeCoinsButton, false);
            if (freePill != null) freePill.onClick.AddListener(() => { if (dailyScreen != null) router.Show(dailyScreen); });
        }

        void BindCards()
        {
            for (int i = 0; i < toolCards.Length; i++)
            {
                var d = catalog.tools != null && i < catalog.tools.Length ? catalog.tools[i] : null;
                toolCards[i].gameObject.SetActive(d != null);
                if (d != null) toolCards[i].Bind(d, d.icon, CardName(d), Pick);
            }
            for (int i = 0; i < petCards.Length; i++)
            {
                var d = catalog.pets != null && i < catalog.pets.Length ? catalog.pets[i] : null;
                petCards[i].gameObject.SetActive(d != null);
                if (d != null) petCards[i].Bind(d, d.icon, CardName(d), Pick);
            }
            var boosters = catalog.boosters != null ? catalog.boosters.boosters : null;
            for (int i = 0; i < itemCards.Length; i++)
            {
                var d = boosters != null && i < boosters.Length ? boosters[i] : null;
                itemCards[i].gameObject.SetActive(d != null);
                if (d != null) itemCards[i].Bind(d, d.icon, Loc.T(string.IsNullOrEmpty(d.shopCardName) ? d.displayName : d.shopCardName), Pick);
            }
            pickedTool = FirstActive(toolCards);
            pickedItem = FirstActive(itemCards);
        }

        void RefreshCardNames()
        {
            foreach (var c in toolCards) if (c.Data is ShopItemData d) c.SetLabel(CardName(d));
            foreach (var c in petCards) if (c.Data is ShopItemData d) c.SetLabel(CardName(d));
            foreach (var c in itemCards)
                if (c.Data is BoosterData b) c.SetLabel(Loc.T(string.IsNullOrEmpty(b.shopCardName) ? b.displayName : b.shopCardName));
        }

        static string CardName(ShopItemData d) => Loc.T(string.IsNullOrEmpty(d.cardName) ? d.displayName : d.cardName);

        static ShopCard FirstActive(ShopCard[] cards)
        {
            foreach (var c in cards) if (c != null && c.gameObject.activeSelf) return c;
            return null;
        }

        void OnEnable()
        {
            if (catalog == null) return;
            RefreshCardNames(); // ngôn ngữ có thể vừa đổi ở Cài đặt
            SelectTab(tab, instant: true);
        }

        /// <summary>Mở Cửa hàng ở tab Dụng cụ, chọn sẵn dụng cụ đang dùng (nút Đổi ở Trang chủ).</summary>
        public void OpenTools()
        {
            tab = Tab.Tool;
            var equipped = SaveSystem.EquippedTool(catalog);
            foreach (var c in toolCards) if (c.Data == equipped) pickedTool = c;
        }

        void SelectTab(Tab t, bool instant = false)
        {
            tab = t;
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                bool on = i == (int)t;
                tabBackgrounds[i].enabled = on;
                tabLabels[i].color = on ? tabTextSelected : tabTextNormal;
            }
            toolGrid.SetActive(t == Tab.Tool);
            petPanel.SetActive(t == Tab.Pet);
            itemGrid.SetActive(t == Tab.Item);
            if (t == Tab.Pet) SelectPetMap(petMap, keepPick: pickedPet != null);
            if (t == Tab.Tool && toolScroll != null) toolScroll.verticalNormalizedPosition = 1f;
            RefreshAll(instant);
        }

        void SelectPetMap(string mapId, bool keepPick = false)
        {
            petMap = mapId;
            for (int i = 0; i < petChips.Length; i++)
            {
                bool on = petChips[i].mapId == mapId;
                petChips[i].background.sprite = on ? petChipSelected[i] : petChipNormal;
            }
            ShopCard first = null;
            foreach (var c in petCards)
            {
                var p = c.Data as PetData;
                bool show = p != null && p.mapId == mapId;
                c.gameObject.SetActive(show);
                if (show && first == null) first = c;
            }
            if (!keepPick || pickedPet == null || (pickedPet.Data as PetData)?.mapId != mapId) pickedPet = first;
            if (petScroll != null) petScroll.verticalNormalizedPosition = 1f;
            RefreshAll(false);
        }

        void Pick(ShopCard card)
        {
            if (card.Data is ToolData) pickedTool = card;
            else if (card.Data is PetData) pickedPet = card;
            else pickedItem = card;
            RefreshAll(false);
        }

        ShopCard Current => tab == Tab.Tool ? pickedTool : tab == Tab.Pet ? pickedPet : pickedItem;

        void RefreshAll(bool instant)
        {
            RefreshCards(instant);
            RefreshDetail(instant);
        }

        void RefreshCards(bool instant)
        {
            var save = SaveSystem.Data;
            var equipped = SaveSystem.EquippedTool(catalog);
            foreach (var c in toolCards)
            {
                if (!c.gameObject.activeSelf) continue;
                var t = (ToolData)c.Data;
                bool owned = t.isDefault || save.Owns(t.id);
                if (t == equipped) c.SetTag(Loc.T("Đang dùng"), tagInUse);
                else if (owned) c.SetTag(Loc.T("Đã có"), tagOwned);
                else if (SaveSystem.TrialLeft(t) > TimeSpan.Zero) c.SetTag(Loc.T("Dùng thử"), tagOwned);
                else c.SetTag(Loc.F("{0} xu", UiUtil.Thousands(t.price)), tagPrice);
                c.SetSelected(c == pickedTool, instant);
            }
            foreach (var c in petCards)
            {
                var p = c.Data as PetData;
                if (p == null) continue;
                if (p.isBase) c.SetTag(Loc.T("Có sẵn"), tagBase);
                else if (SaveSystem.IsPetOn(p)) c.SetTag(Loc.T("Đang dùng"), tagInUse);
                else if (save.Owns(p.id)) c.SetTag(Loc.T("Đã có"), tagOwned);
                else c.SetTag(Loc.F("{0} xu", UiUtil.Thousands(p.price)), tagPrice);
                c.SetSelected(c == pickedPet, instant);
            }
            foreach (var c in itemCards)
            {
                if (!c.gameObject.activeSelf) continue;
                c.SetTag(Loc.F("{0} xu", UiUtil.Thousands(((BoosterData)c.Data).price)), tagPrice);
                c.SetSelected(c == pickedItem, instant);
            }
        }

        void RefreshDetail(bool instant)
        {
            var card = Current;
            if (card == null) return;
            var save = SaveSystem.Data;
            int coins = save.coins;

            foreach (var b in new[] { equippedButton, equipButton, petOnButton, petOffButton, baseButton, buyButton, shortButton, cashButton, buy1Button, buy5Button })
                b.gameObject.SetActive(false);
            ownedRow.SetActive(false);
            if (freeRow != null) freeRow.SetActive(false);
            pointsText.gameObject.SetActive(false);
            if (hitRange != null) hitRange.gameObject.SetActive(false);
            premiumBadge.SetActive(false);

            if (card.Data is BoosterData booster)
            {
                nameText.text = Loc.T(booster.displayName);
                descText.text = Loc.T(booster.shopDescription);
                ownedRow.SetActive(true);
                ownedQty.text = "x" + save.ItemCount(booster.id);
                int pack = booster.packPrice > 0 ? booster.packPrice : booster.price * booster.packSize;
                buy1Price.text = Loc.F("{0} xu", UiUtil.Thousands(booster.price));
                buy5Price.text = Loc.F("{0} xu", UiUtil.Thousands(pack));
                buy1Button.gameObject.SetActive(true);
                buy5Button.gameObject.SetActive(true);
                buy1Button.interactable = coins >= booster.price;
                buy5Button.interactable = coins >= pack;
            }
            else if (card.Data is ShopItemData item)
            {
                nameText.text = Loc.T(item.displayName);
                descText.text = Loc.T(item.description);
                premiumBadge.SetActive(item.premium);

                var tool = item as ToolData;
                var pet = item as PetData;
                if (pet != null) ShowPoints(pet);
                else if (tool != null && hitRange != null)
                {
                    hitRange.gameObject.SetActive(true);
                    hitRange.Show(tool);
                }
                bool owned = (tool != null && tool.isDefault) || (pet != null && pet.isBase) || save.Owns(item.id);
                var trial = tool != null ? SaveSystem.TrialLeft(tool) : TimeSpan.Zero;
                if (freeRow != null)
                {
                    if (trial > TimeSpan.Zero)
                    {
                        // Đang dùng thử (quà điểm danh ngày 30): nhãn xanh đếm thời gian còn lại
                        freeRow.SetActive(true);
                        freeImage.sprite = trialSprite;
                        freeIcon.SetActive(false);
                        freeText.text = Loc.F("Đang dùng thử · còn {0}", SaveSystem.TrialText(trial));
                        freePill.interactable = false;
                    }
                    else if (!owned && !string.IsNullOrEmpty(item.freeHint))
                    {
                        freeRow.SetActive(true);
                        freeImage.sprite = freeSprite;
                        freeIcon.SetActive(true);
                        freeText.text = Loc.T(item.freeHint);
                        freePill.interactable = true;
                    }
                }
                if (tool != null && (owned || trial > TimeSpan.Zero))
                {
                    bool equipped = SaveSystem.EquippedTool(catalog) == tool;
                    equippedButton.gameObject.SetActive(equipped);
                    equipButton.gameObject.SetActive(!equipped);
                    if (!owned)
                    {
                        // dùng thử: vẫn mua được để giữ vĩnh viễn
                        bool enough = coins >= item.price;
                        buyButton.gameObject.SetActive(enough);
                        shortButton.gameObject.SetActive(!enough);
                        buyText.text = Loc.F("Mua {0}", UiUtil.Thousands(item.price));
                        shortText.text = Loc.F("Thiếu {0} xu", UiUtil.Thousands(Mathf.Max(0, item.price - coins)));
                    }
                }
                else if (pet != null && pet.isBase) baseButton.gameObject.SetActive(true);
                else if (pet != null && owned)
                {
                    // Nhãn gọn "Dùng" / "Đang dùng" (người dùng chốt); thú vẫn chỉ bật ở map của nó (PetData.mapId)
                    bool on = SaveSystem.IsPetOn(pet);
                    petOnButton.gameObject.SetActive(on);
                    petOffButton.gameObject.SetActive(!on);
                    petOnText.text = Loc.T("Đang dùng");
                    petOffText.text = Loc.T("Dùng");
                }
                else
                {
                    bool enough = coins >= item.price;
                    buyButton.gameObject.SetActive(enough);
                    shortButton.gameObject.SetActive(!enough);
                    buyText.text = Loc.F("Mua {0}", UiUtil.Thousands(item.price));
                    shortText.text = Loc.F("Thiếu {0} xu", UiUtil.Thousands(Mathf.Max(0, item.price - coins)));
                    bool cash = !string.IsNullOrEmpty(item.cashPrice);
                    cashButton.gameObject.SetActive(cash);
                    if (cash) cashText.text = item.cashPrice;
                }
            }

            foreach (var b in new[] { equippedButton, baseButton, shortButton, buy1Button, buy5Button })
                b.GetComponent<CanvasGroup>().alpha = b.interactable ? 1f : 0.85f; // .gbtn[disabled] opacity .85

            // Mô tả: cao đúng số dòng × 18,2 (Unity tính cao dòng theo font nên đặt tay)
            var gen = new TextGenerator();
            float descW = descText.rectTransform.rect.width > 1f ? descText.rectTransform.rect.width : 324f * PxToUnits;
            var settings = descText.GetGenerationSettings(new Vector2(descW, 0f));
            gen.Populate(descText.text, settings);
            descLayout.preferredHeight = Mathf.Max(1, gen.lineCount) * descLineHeight * PxToUnits;

            LayoutRebuilder.ForceRebuildLayoutImmediate(detail);
            if (tab == Tab.Pet) FitScroll(petScrollLayout, petScrollMax);
            else if (tab == Tab.Tool && toolScrollLayout != null) FitScroll(toolScrollLayout, toolScrollMax);

        }

        /// <summary>Danh sách thú (cao tối đa 262) / dụng cụ (300) co lại để không đè ô Tích xu (cách 12).</summary>
        void FitScroll(LayoutElement layout, float max)
        {
            layout.preferredHeight = max * PxToUnits;
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            var holder = (RectTransform)layout.transform;
            var corners = new Vector3[4];
            holder.GetWorldCorners(corners);
            float top = body.InverseTransformPoint(corners[1]).y;
            freePanel.GetWorldCorners(corners);
            float limit = body.InverseTransformPoint(corners[1]).y + pageGap * PxToUnits;
            layout.preferredHeight = Mathf.Clamp(top - limit, 60f * PxToUnits, max * PxToUnits);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
        }

        void ShowPoints(PetData pet)
        {
            pointsText.gameObject.SetActive(true);
            pointsText.text = Loc.F("Điểm thưởng: +{0} điểm mỗi lần đập trúng", pet.points);
        }


        void Equip()
        {
            if (pickedTool == null) return;
            SaveSystem.EquipTool((ToolData)pickedTool.Data);
            RefreshAll(false);
        }

        void TogglePet(bool on)
        {
            if (pickedPet == null) return;
            SaveSystem.SetPetOn((PetData)pickedPet.Data, on);
            RefreshAll(false);
        }

        void Buy()
        {
            var card = Current;
            if (card == null || !(card.Data is ShopItemData item)) return;
            if (!SaveSystem.BuyWithCoins(item)) return;
            AudioManager.Play(Sfx.Purchase);
            if (item is PetData pet) SaveSystem.SetPetOn(pet, true); // thú vừa mua bật luôn ở map của nó
            bumpT = 0f;
            RefreshAll(false);
        }

        void BuyBooster(int count)
        {
            if (pickedItem == null || !(pickedItem.Data is BoosterData b)) return;
            int cost = count == 1 ? b.price : (b.packPrice > 0 ? b.packPrice : b.price * count);
            if (!SaveSystem.BuyBooster(b, count, cost)) return;
            AudioManager.Play(Sfx.Purchase);
            bumpT = 0f;
            RefreshAll(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // nhãn "Đang dùng thử · còn …": cập nhật mỗi giây (và khi hết hạn thì về trạng thái chưa có)
            if (freeRow != null && freeRow.activeSelf && freeImage.sprite == trialSprite)
            {
                trialRefreshT += dt;
                if (trialRefreshT >= 1f) { trialRefreshT = 0f; RefreshAll(false); }
            }
            int coins = SaveSystem.Data.coins;
            if (coins != shownCoins)
            {
                shownCoins = coins;
                coinsText.text = UiUtil.Thousands(coins);
            }

            // Ô xu nảy khi mua: scale 1 → 1,18 (40%) → 1, 0,35 s ease-out
            if (bumpT >= 0f)
            {
                bumpT += dt;
                float p = Mathf.Clamp01(bumpT / 0.35f), e = Bezier.EaseOut.Evaluate(p);
                float s = e < 0.4f ? Mathf.Lerp(1f, 1.18f, e / 0.4f) : Mathf.Lerp(1.18f, 1f, (e - 0.4f) / 0.6f);
                coinChip.localScale = Vector3.one * s;
                if (p >= 1f) bumpT = -1f;
            }
        }
    }
}
