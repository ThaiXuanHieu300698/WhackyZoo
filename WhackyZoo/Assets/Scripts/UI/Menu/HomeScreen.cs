using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Trang chủ: CHƠI NGAY mở Chọn map, bánh răng mở Cài đặt, Cửa hàng / Nhiệm vụ / Điểm danh mở màn tương ứng.
    /// Ô Lv (cấp + thanh kinh nghiệm), chấm đỏ số nhiệm vụ chờ nhận, dòng "Ngày N/30 · quà" của thẻ Điểm danh,
    /// thời gian dùng thử còn lại của dụng cụ (quà điểm danh ngày 30).
    /// Xếp hạng thuộc bước 6 nên đang mờ.
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] CanvasGroup mapSelectScreen;
        [SerializeField] CanvasGroup settingsScreen;
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;
        [Tooltip("Nút của tính năng chưa làm: hiện mờ, không bấm được.")]
        [SerializeField] Selectable[] comingSoon;
        [Tooltip("Số xu đang có (ô xu cạnh nút cài đặt).")]
        [SerializeField] Text coinsText;
        [Tooltip("Tặng vật phẩm dùng thử cho người chơi mới ngay khi mở game.")]
        [SerializeField] BoosterCatalog boosters;

        [Header("Cửa hàng và dụng cụ đang dùng")]
        [SerializeField] CanvasGroup shopScreen;
        [SerializeField] ShopScreen shopView;
        [SerializeField] ShopCatalog shopCatalog;
        [SerializeField] Button shopButton;
        [Tooltip("Nút Đổi ở thẻ dụng cụ: mở Cửa hàng ở tab Dụng cụ.")]
        [SerializeField] Button swapToolButton;
        [SerializeField] Text toolName;
        [SerializeField] Image toolIcon;

        [Header("Tiến trình")]
        [SerializeField] ProgressionConfig progression;
        [SerializeField] Text levelText;
        [SerializeField] RectTransform xpFill;
        [SerializeField] CanvasGroup missionsScreen;
        [SerializeField] Button missionsButton;
        [Tooltip("Chấm đỏ số nhiệm vụ + rương chờ nhận.")]
        [SerializeField] GameObject missionsBadge;
        [SerializeField] Text missionsBadgeText;
        [SerializeField] CanvasGroup dailyScreen;
        [SerializeField] Button dailyButton;
        [SerializeField] Text dailyButtonText;
        [SerializeField] Text dailySubtitle;
        [Tooltip("Lịch điểm danh tháng (dòng \"Ngày N/30 · quà\").")]
        [SerializeField] DailyRewardMonth month;
        [Tooltip("Dòng dưới tên dụng cụ: \"Đang dùng\" / \"Dùng thử · còn …\".")]
        [SerializeField] Text toolStatus;

        ToolData shownTool;
        bool shownEnglish;
        int shownCoins = -1;
        float refreshT;

        void Awake()
        {
            AudioManager.PlayHomeMusic();
            playButton.onClick.AddListener(() => router.Show(mapSelectScreen));
            settingsButton.onClick.AddListener(() => router.Show(settingsScreen));
            foreach (var c in comingSoon) UiUtil.SetAvailable(c, false);
            SaveSystem.EnsureStarterItems(boosters);
            if (shopButton != null) shopButton.onClick.AddListener(() => router.Show(shopScreen));
            if (swapToolButton != null) swapToolButton.onClick.AddListener(() =>
            {
                if (shopView != null) shopView.OpenTools();
                router.Show(shopScreen);
            });
            if (missionsButton != null) missionsButton.onClick.AddListener(() => router.Show(missionsScreen));
            if (dailyButton != null) dailyButton.onClick.AddListener(() => router.Show(dailyScreen));
        }

        void OnEnable() => RefreshProgress();

        /// <summary>Cấp, kinh nghiệm, chấm đỏ nhiệm vụ, thẻ quà: cập nhật khi mở trang và mỗi giây (qua 00:00, về từ màn khác).</summary>
        void RefreshProgress()
        {
            if (progression == null) return;
            var d = SaveSystem.Data;
            if (levelText != null) levelText.text = "Lv " + d.level;
            if (xpFill != null) xpFill.anchorMax = new Vector2(Progression.LevelProgress(progression), 1f);

            int pending = Progression.PendingCount(progression);
            if (missionsBadge != null) missionsBadge.SetActive(pending > 0);
            if (missionsBadgeText != null) missionsBadgeText.text = pending.ToString();

            if (month != null)
            {
                int day = CheckIn.Today, total = month.Count;
                bool canClaim = CheckIn.CanClaimToday(month);
                var r = month.Day(day);
                if (dailySubtitle != null)
                    dailySubtitle.text = r == null ? Loc.F("Đã điểm danh {0}/{1} ngày", CheckIn.ClaimedCount(month), total)
                        : !canClaim ? Loc.F("Đã nhận hôm nay · Ngày {0}/{1}", day, total)
                        : Loc.F("Ngày {0}/{1} · {2}", day, total, r.gift != null && r.gift.kind == DailyGiftKind.Coins && !r.mystery ? "+" + CheckIn.DayName(r) : CheckIn.DayName(r));
                if (dailyButtonText != null) dailyButtonText.text = Loc.T(canClaim ? "Nhận" : "Xem");
            }
            var tool = SaveSystem.EquippedTool(shopCatalog);
            if (toolStatus != null && tool != null)
            {
                var left = SaveSystem.TrialLeft(tool);
                toolStatus.text = left > System.TimeSpan.Zero ? Loc.F("Dùng thử · còn {0}", SaveSystem.TrialText(left)) : Loc.T("Đang dùng");
            }
        }

        void Update()
        {
            // Xu đổi khi về từ màn chơi hoặc mua ở Cửa hàng: chỉ cập nhật chữ khi số thay đổi
            int coins = SaveSystem.Data.coins;
            if (coinsText != null && coins != shownCoins)
            {
                shownCoins = coins;
                coinsText.text = UiUtil.Thousands(coins);
            }
            // Dụng cụ đang dùng đổi khi trang bị ở Cửa hàng
            var tool = SaveSystem.EquippedTool(shopCatalog);
            if (tool != null && (tool != shownTool || Loc.English != shownEnglish))
            {
                shownTool = tool;
                shownEnglish = Loc.English;
                if (toolName != null) toolName.text = Loc.T(tool.displayName);
                if (toolIcon != null) toolIcon.sprite = tool.icon;
            }
            refreshT += Time.unscaledDeltaTime;
            if (refreshT >= 1f)
            {
                refreshT = 0f;
                RefreshProgress();
            }
        }
    }
}
