using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Màn Điểm danh tháng (Docs/DesignSource/DailyReward25D.dc.html, images/screens/09b_qua_hang_ngay.png):
    /// thẻ quà ngày 30 + thanh tiến độ 4 mốc (7 · 14 · 21 · 30), lịch 6 × 5 ô (đã nhận / hôm nay / bỏ lỡ / tương lai),
    /// nút NHẬN + xem quảng cáo x2; nhận xong hiện "Đã nhận …!" + giờ tới ô kế tiếp và nút Trang chủ.
    /// Rương bí ẩn (ngày 7, 21, 29) mở popup liệt kê quà. Quảng cáo (x2, điểm danh bù) thuộc bước 6: nút hiện nhưng tắt.
    /// </summary>
    public class DailyRewardScreen : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [Serializable]
        public struct DayCell
        {
            public RectTransform root;
            public CanvasGroup group;
            public Image background;
            public Image glow;
            public Image icon;
            public Text label;
            public GameObject tick;
            public GameObject todayTag;
            public GameObject missed;
            public Button makeUpButton;
        }

        [SerializeField] ScreenRouter router;
        [SerializeField] DailyRewardMonth month;
        [SerializeField] Button backButton;

        [Header("Thẻ quà ngày 30")]
        [SerializeField] RectTransform prizeIcon;
        [SerializeField] RectTransform progressFill;
        [Tooltip("Mốc tròn trên thanh tiến độ, cùng thứ tự với milestoneDays.")]
        [SerializeField] Image[] milestones;
        [SerializeField] int[] milestoneDays = { 7, 14, 21, 30 };
        [SerializeField] Sprite milestoneDone;
        [SerializeField] Sprite milestoneGrand;
        [SerializeField] Sprite milestoneNormal;
        [SerializeField] Text countText;
        [SerializeField] RectTransform prizeShine;
        [SerializeField] float prizeShineTravel = 420f;

        [Header("Lịch")]
        [SerializeField] DayCell[] days;
        [SerializeField] Sprite cellNormal;
        [SerializeField] Sprite cellPurple;
        [SerializeField] Sprite cellGold;
        [SerializeField] Sprite cellToday;
        [SerializeField] Sprite cellClaimed;
        [SerializeField] float futureAlpha = 0.9f;
        [SerializeField] Text makeUpText;

        [Header("Nút")]
        [SerializeField] GameObject claimGroup;
        [SerializeField] Button claimButton;
        [SerializeField] Button adButton;
        [SerializeField] GameObject claimedGroup;
        [SerializeField] Text gotText;
        [SerializeField] Text nextText;
        [SerializeField] Button homeButton;

        [Header("Popup rương bí ẩn")]
        [SerializeField] CanvasGroup chestPopup;
        [SerializeField] RectTransform chestPanel;
        [SerializeField] Image[] chestRowIcons;
        [SerializeField] Text[] chestRowTexts;
        [SerializeField] Button chestOkButton;
        [SerializeField] Sprite coinIcon;

        [Header("Chuyển động")]
        [SerializeField] RectTransform rays;

        float t, popT = -1f, popupT = -1f;
        float fillShown, fillFrom, fillTo, fillT = -1f;
        float[] targetAlpha;
        int shownSecond = -1;
        string lastGot;
        Vector2 prizeIconBase;

        static readonly Bezier PopEase = new Bezier(0.2f, 0.9f, 0.3f, 1.3f);
        static readonly Bezier FillEase = new Bezier(0.2f, 0.9f, 0.3f, 1.2f);
        const float CellDelay = 0.015f, CellPop = 0.4f;

        void Awake()
        {
            backButton.onClick.AddListener(() => router.Back());
            homeButton.onClick.AddListener(() => router.Back());
            claimButton.onClick.AddListener(Claim);
            if (adButton != null) UiUtil.SetAvailable(adButton, false); // quảng cáo — bước 6
            foreach (var d in days)
                if (d.makeUpButton != null) UiUtil.SetAvailable(d.makeUpButton, false); // điểm danh bù cần quảng cáo — bước 6
            if (chestOkButton != null) chestOkButton.onClick.AddListener(CloseChest);
            if (prizeIcon != null) prizeIconBase = prizeIcon.anchoredPosition; // nhún quanh vị trí gốc
        }

        void OnEnable()
        {
            if (month == null) return;
            lastGot = null;
            if (targetAlpha == null || targetAlpha.Length != days.Length) targetAlpha = new float[days.Length];
            if (chestPopup != null) { chestPopup.gameObject.SetActive(false); popupT = -1f; }
            fillT = -1f;
            fillShown = Progress();
            SetFill(fillShown);
            Refresh();
            popT = 0f; // .cell: pop lần lượt khi mở màn
            ApplyPop(0f);
        }

        float Progress() => month.Count > 0 ? CheckIn.ClaimedCount(month) / (float)month.Count : 0f;

        void Claim()
        {
            var r = CheckIn.ClaimToday(month);
            if (r == null) return;
            AudioManager.Play(Sfx.Reward);
            lastGot = CheckIn.DayName(r.data) + (r.multiplier > 1 ? " (x" + r.multiplier + ")" : "");
            Refresh();
            // .fill: width chạy 0,4 s cubic-bezier(.2,.9,.3,1.2)
            fillFrom = fillShown;
            fillTo = Progress();
            fillT = 0f;
            if (r.data.mystery) OpenChest(r);
        }

        void Refresh()
        {
            CheckIn.Refresh();
            int count = CheckIn.ClaimedCount(month);
            countText.text = Loc.F("Đã điểm danh {0}/{1} ngày", count, month.Count);
            for (int i = 0; i < milestones.Length && i < milestoneDays.Length; i++)
            {
                int d = milestoneDays[i];
                milestones[i].sprite = CheckIn.IsClaimed(d) ? milestoneDone : d == month.Count ? milestoneGrand : milestoneNormal;
            }

            for (int i = 0; i < days.Length; i++)
            {
                int d = i + 1;
                var c = days[i];
                var data = month.Day(d);
                var st = CheckIn.StateOf(month, d);
                bool grand = d == month.Count, purple = data != null && !data.mystery && data.gift != null && data.gift.kind == DailyGiftKind.Pet;
                Sprite bg = grand ? cellGold : purple ? cellPurple : cellNormal;
                if (st == CheckIn.State.Today) bg = cellToday;
                if (st == CheckIn.State.Claimed && !grand && !purple) bg = cellClaimed;
                c.background.sprite = bg;
                if (c.glow != null) c.glow.gameObject.SetActive(st == CheckIn.State.Today);
                c.tick.SetActive(st == CheckIn.State.Claimed);
                c.todayTag.SetActive(st == CheckIn.State.Today);
                c.missed.SetActive(st == CheckIn.State.Missed);
                if (data != null && c.label != null) c.label.text = CheckIn.DayLabel(data);
                targetAlpha[i] = st == CheckIn.State.Future ? futureAlpha : 1f;
                if (popT < 0f) c.group.alpha = targetAlpha[i];
            }
            makeUpText.text = Loc.F("Lỡ ngày? Xem quảng cáo để điểm danh bù · còn {0}/{1}", CheckIn.MakeUpsLeft(month), month.makeUpsPerMonth);

            bool canClaim = CheckIn.CanClaimToday(month);
            claimGroup.SetActive(canClaim);
            claimedGroup.SetActive(!canClaim);
            if (canClaim)
            {
                // Ngày tặng thú / dùng thử dụng cụ không có x2 (người dùng chốt): ẩn nút quảng cáo
                var today = month.Day(CheckIn.Today);
                if (adButton != null) adButton.gameObject.SetActive(today == null || today.allowDouble);
            }
            else
            {
                var today = month.Day(CheckIn.Today);
                if (today == null) gotText.text = Loc.T("Đã hết ô điểm danh tháng này!");
                else gotText.text = Loc.F("Đã nhận {0}!", lastGot ?? CheckIn.DayName(today));
            }
            shownSecond = -1;
        }

        void SetFill(float v)
        {
            fillShown = v;
            progressFill.anchorMax = new Vector2(Mathf.Max(0f, v), 1f);
            progressFill.gameObject.SetActive(v > 0.001f);
        }

        /// <summary>.cell pop: scale 0,6 → 1 (cubic-bezier(.2,.9,.3,1.3)) và hiện dần trong 0,4 s, trễ 0,015 s mỗi ô.</summary>
        void ApplyPop(float time)
        {
            bool done = true;
            for (int i = 0; i < days.Length; i++)
            {
                float p = Mathf.Clamp01((time - i * CellDelay) / CellPop);
                if (p < 1f) done = false;
                days[i].root.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, PopEase.Evaluate(p));
                days[i].group.alpha = targetAlpha[i] * p;
            }
            if (done) popT = -1f;
        }

        // ───────── Rương bí ẩn ─────────

        void OpenChest(CheckIn.Result r)
        {
            if (chestPopup == null) return;
            for (int i = 0; i < chestRowTexts.Length; i++)
            {
                bool on = i < r.items.Count;
                chestRowTexts[i].transform.parent.gameObject.SetActive(on);
                if (!on) continue;
                var got = r.items[i];
                chestRowTexts[i].text = CheckIn.GotText(got);
                chestRowIcons[i].sprite = IconOf(got.gift);
            }
            chestPopup.gameObject.SetActive(true);
            popupT = 0f;
            ApplyPopup(0f);
        }

        Sprite IconOf(DailyGift g)
        {
            switch (g.kind)
            {
                case DailyGiftKind.Booster: return g.booster != null ? g.booster.icon : null;
                case DailyGiftKind.Pet: return g.pet != null ? g.pet.icon : null;
                case DailyGiftKind.ToolTrial: return g.tool != null ? g.tool.icon : null;
                default: return coinIcon;
            }
        }

        void CloseChest()
        {
            if (chestPopup != null) chestPopup.gameObject.SetActive(false);
            popupT = -1f;
        }

        /// <summary>Popup: nền tối mờ vào, khung scale 0,7 → 1 (pop) trong 0,35 s.</summary>
        void ApplyPopup(float time)
        {
            float p = Mathf.Clamp01(time / 0.35f);
            chestPopup.alpha = Mathf.Clamp01(time / 0.2f);
            chestPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, PopEase.Evaluate(p));
            if (p >= 1f) popupT = -1f;
        }

        void Update()
        {
            if (month == null) return;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (popT >= 0f) { popT += dt; ApplyPop(popT); }
            if (popupT >= 0f) { popupT += dt; ApplyPopup(popupT); }
            if (fillT >= 0f)
            {
                fillT += dt;
                float p = Mathf.Clamp01(fillT / 0.4f);
                SetFill(Mathf.LerpUnclamped(fillFrom, fillTo, FillEase.Evaluate(p)));
                if (p >= 1f) fillT = -1f;
            }

            if (claimedGroup.activeSelf)
            {
                var left = CheckIn.UntilMidnight;
                int sec = (int)left.TotalSeconds;
                if (sec != shownSecond)
                {
                    shownSecond = sec;
                    nextText.text = Loc.F("Điểm danh tiếp sau {0}", Progression.Clock(left));
                    if (sec <= 0) { Refresh(); SetFill(Progress()); } // sang ngày (tháng) mới khi đang mở màn
                }
            }

            // .rays: quay 360° mỗi 30 s
            if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -360f * (t % 30f / 30f));
            // .bounce (ô quà ngày 30): 1,6 s — 30% lên 5 px xoay −6°, 60% về 0 xoay 5°, 100% về 0
            if (prizeIcon != null)
            {
                float q = t % 1.6f / 1.6f, y, r;
                if (q < 0.3f) { float u = Bezier.EaseInOut.Evaluate(q / 0.3f); y = -5f * u; r = -6f * u; }
                else if (q < 0.6f) { float u = Bezier.EaseInOut.Evaluate((q - 0.3f) / 0.3f); y = Mathf.Lerp(-5f, 0f, u); r = Mathf.Lerp(-6f, 5f, u); }
                else { float u = Bezier.EaseInOut.Evaluate((q - 0.6f) / 0.4f); y = 0f; r = Mathf.Lerp(5f, 0f, u); }
                prizeIcon.anchoredPosition = prizeIconBase + new Vector2(0f, -y * PxToUnits);
                prizeIcon.localRotation = Quaternion.Euler(0f, 0f, -r);
            }
            // .shine::before: vệt sáng quét qua thẻ, 2,8 s (0–55% đứng ở trái, 85% sang phải)
            if (prizeShine != null)
            {
                float q = t % 2.8f / 2.8f;
                float u = q < 0.55f ? 0f : q < 0.85f ? Bezier.EaseInOut.Evaluate((q - 0.55f) / 0.3f) : 1f;
                prizeShine.anchoredPosition = new Vector2((-60f + u * prizeShineTravel) * PxToUnits, prizeShine.anchoredPosition.y);
            }
            // .glow (ô hôm nay): quầng vàng 0 → .95, 1,1 s qua lại
            float g = Bezier.EaseInOut.Evaluate(Mathf.PingPong(t / 1.1f, 1f));
            foreach (var c in days)
                if (c.glow != null && c.glow.gameObject.activeSelf) c.glow.color = new Color(1f, 1f, 1f, g);
            // .breathe (NHẬN): scale 1 → 1,035 → 1, 1,4 s
            if (claimGroup.activeSelf)
            {
                float b = t % 1.4f / 1.4f;
                float u = Bezier.EaseInOut.Evaluate(b < 0.5f ? b * 2f : (1f - b) * 2f);
                claimButton.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.035f, u);
            }
        }
    }
}
