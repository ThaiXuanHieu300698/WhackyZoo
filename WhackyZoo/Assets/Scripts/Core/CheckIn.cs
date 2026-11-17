using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Điểm danh tháng (Docs/GameSpec.md › Điểm danh tháng). Ô ngày = ngày trong tháng theo đồng hồ máy (mốc 00:00):
    /// mỗi ngày nhận quà của ô hôm nay; ô đã qua mà chưa nhận là bỏ lỡ, điểm danh bù bằng quảng cáo (tối đa
    /// DailyRewardMonth.makeUpsPerMonth lần/tháng). Sang tháng mới: xoá các ô đã nhận, lượt bù về đủ.
    /// Tháng ít hơn 30 ngày: ngày cuối tháng, các ô còn lại sau hôm nay tính là bỏ lỡ (bù được). Ngày 31: không có ô mới.
    /// </summary>
    public static class CheckIn
    {
        public enum State { Claimed, Today, Missed, Future }

        /// <summary>Một món đã nhận (để hiện "Đã nhận …" / danh sách quà trong rương).</summary>
        public struct Got
        {
            public DailyGift gift;
            public int amount;
        }

        public class Result
        {
            public int day;
            public DailyDay data;
            public int multiplier = 1;
            public readonly List<Got> items = new List<Got>();
        }

        static DateTime Now => DateTime.Now;
        static string MonthKey(DateTime t) => t.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        /// <summary>Ngày trong tháng hôm nay (1–31).</summary>
        public static int Today => Now.Day;

        /// <summary>Thời gian tới 00:00 hôm sau (ô kế tiếp mở).</summary>
        public static TimeSpan UntilMidnight => Now.Date.AddDays(1) - Now;

        /// <summary>Sang tháng mới thì xoá tiến độ tháng cũ. Gọi trước khi đọc hoặc ghi.</summary>
        public static void Refresh()
        {
            var d = SaveSystem.Data;
            string month = MonthKey(Now);
            if (d.checkInMonth == month) return;
            d.checkInMonth = month;
            d.checkInDays.Clear();
            d.checkInMakeUpsUsed = 0;
            SaveSystem.Save();
        }

        public static bool IsClaimed(int day) => SaveSystem.Data.checkInDays.Contains(day);

        public static int ClaimedCount(DailyRewardMonth cfg)
        {
            Refresh();
            int n = 0;
            foreach (int d in SaveSystem.Data.checkInDays) if (d >= 1 && d <= cfg.Count) n++;
            return n;
        }

        public static int MakeUpsLeft(DailyRewardMonth cfg)
        {
            Refresh();
            return Mathf.Max(0, cfg.makeUpsPerMonth - SaveSystem.Data.checkInMakeUpsUsed);
        }

        public static State StateOf(DailyRewardMonth cfg, int day)
        {
            Refresh();
            if (IsClaimed(day)) return State.Claimed;
            int today = Today;
            if (day == today) return State.Today;
            if (day < today) return State.Missed;
            // ngày cuối của tháng ngắn: các ô sau hôm nay không còn ngày nào để nhận → coi như bỏ lỡ
            if (today == DateTime.DaysInMonth(Now.Year, Now.Month)) return State.Missed;
            return State.Future;
        }

        /// <summary>Ô hôm nay còn chờ nhận.</summary>
        public static bool CanClaimToday(DailyRewardMonth cfg)
        {
            Refresh();
            return cfg != null && Today <= cfg.Count && !IsClaimed(Today);
        }

        /// <summary>Nhận quà hôm nay (x <paramref name="multiplier"/> khi xem quảng cáo, nếu ngày đó cho phép).</summary>
        public static Result ClaimToday(DailyRewardMonth cfg, int multiplier = 1)
        {
            if (!CanClaimToday(cfg)) return null;
            return Grant(cfg, Today, multiplier);
        }

        /// <summary>Điểm danh bù một ô đã lỡ (sau khi xem quảng cáo — bước 6). Không còn lượt bù thì null.</summary>
        public static Result MakeUp(DailyRewardMonth cfg, int day)
        {
            if (cfg == null || StateOf(cfg, day) != State.Missed || MakeUpsLeft(cfg) <= 0) return null;
            SaveSystem.Data.checkInMakeUpsUsed++;
            return Grant(cfg, day, 1);
        }

        static Result Grant(DailyRewardMonth cfg, int day, int multiplier)
        {
            var data = cfg.Day(day);
            if (data == null) return null;
            int k = data.allowDouble ? Mathf.Max(1, multiplier) : 1;
            var r = new Result { day = day, data = data, multiplier = k };
            if (data.mystery && data.chest != null)
                foreach (var g in data.chest) Give(g, k, r);
            else if (data.gift != null)
                Give(data.gift, k, r);
            var d = SaveSystem.Data;
            if (!d.checkInDays.Contains(day)) d.checkInDays.Add(day);
            SaveSystem.Save();
            return r;
        }

        static void Give(DailyGift g, int k, Result r)
        {
            if (g == null) return;
            var d = SaveSystem.Data;
            switch (g.kind)
            {
                case DailyGiftKind.Coins:
                    d.coins += Mathf.Max(0, g.amount) * k;
                    r.items.Add(new Got { gift = g, amount = g.amount * k });
                    break;
                case DailyGiftKind.Booster:
                    string id = g.booster != null ? g.booster.id : g.id;
                    if (!string.IsNullOrEmpty(id)) d.AddItem(id, Mathf.Max(0, g.amount) * k);
                    r.items.Add(new Got { gift = g, amount = g.amount * k });
                    break;
                case DailyGiftKind.Pet:
                    SaveSystem.GrantPet(g.pet); // đã có thì thôi (người dùng chốt: không đổi ra xu)
                    r.items.Add(new Got { gift = g, amount = 1 });
                    break;
                case DailyGiftKind.ToolTrial:
                    SaveSystem.GrantToolTrial(g.tool, g.amount); // đã sở hữu vĩnh viễn thì thôi
                    r.items.Add(new Got { gift = g, amount = g.amount });
                    break;
            }
        }

        static bool IsCoins(DailyDay d) => d != null && !d.mystery && d.gift != null && d.gift.kind == DailyGiftKind.Coins;

        /// <summary>Nhãn ngắn dưới ô (xu viết theo ngôn ngữ đang chọn).</summary>
        public static string DayLabel(DailyDay d) => d == null ? "" : IsCoins(d) ? Loc.F("{0} xu", UiUtil.Thousands(d.gift.amount)) : Loc.T(d.label);

        /// <summary>Tên đầy đủ quà của ô ("Đã nhận …!", dòng Trang chủ).</summary>
        public static string DayName(DailyDay d) => d == null ? "" : IsCoins(d) ? Loc.F("{0} xu", UiUtil.Thousands(d.gift.amount)) : Loc.T(d.fullName);

        /// <summary>Tên một món đã nhận, vd. "250 xu", "+5 Giây x2", "Capybara", "Gậy Bóng Chày (7 ngày)".</summary>
        public static string GotText(Got got)
        {
            var g = got.gift;
            switch (g.kind)
            {
                case DailyGiftKind.Coins: return Loc.F("{0} xu", UiUtil.Thousands(got.amount));
                case DailyGiftKind.Booster: return Loc.T(g.booster != null ? g.booster.displayName : g.id) + " x" + got.amount;
                case DailyGiftKind.Pet: return Loc.T(g.pet != null ? g.pet.displayName : g.id);
                case DailyGiftKind.ToolTrial: return Loc.F("{0} ({1} ngày)", Loc.T(g.tool != null ? g.tool.displayName : g.id), got.amount);
            }
            return "";
        }
    }
}
