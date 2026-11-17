using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Tiến trình người chơi (Docs/GameSpec.md › Tiến trình): nhiệm vụ ngày/tuần/thành tựu, điểm nhiệm vụ + rương,
    /// kinh nghiệm / cấp độ. Dữ liệu ở SaveSystem.Data; số liệu ở ProgressionConfig. Điểm danh tháng: CheckIn.
    /// Giờ theo đồng hồ máy: nhiệm vụ ngày + điểm + rương làm mới lúc 00:00, nhiệm vụ tuần lúc 00:00 thứ Hai.
    /// </summary>
    public static class Progression
    {
        public enum ChestState { Locked, Ready, Opened }

        /// <summary>Kết quả cộng kinh nghiệm sau một ván.</summary>
        public struct XpResult
        {
            public int xpGained, levelsGained, coinsGained;
        }

        static DateTime Now => DateTime.Now;
        static string DayKey(DateTime t) => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        static DateTime WeekStart(DateTime t) => t.Date.AddDays(-(((int)t.DayOfWeek + 6) % 7)); // thứ Hai

        /// <summary>Thời gian tới 00:00 hôm sau (nhiệm vụ ngày).</summary>
        public static TimeSpan UntilMidnight => Now.Date.AddDays(1) - Now;

        public static string Clock(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        // ───────── Làm mới theo kỳ ─────────

        /// <summary>Sang ngày / tuần mới thì xoá tiến độ kỳ cũ. Gọi trước khi đọc hoặc ghi nhiệm vụ.</summary>
        public static void Refresh(ProgressionConfig cfg)
        {
            if (cfg == null) return;
            var d = SaveSystem.Data;
            bool changed = false;
            string today = DayKey(Now), week = DayKey(WeekStart(Now));
            if (d.missionDay != today)
            {
                d.missionDay = today;
                d.missionPoints = 0;
                d.chestsOpened.Clear();
                ResetPeriod(cfg, MissionPeriod.Daily);
                changed = true;
            }
            if (d.missionWeek != week)
            {
                d.missionWeek = week;
                d.weekMaps.Clear();
                ResetPeriod(cfg, MissionPeriod.Weekly);
                changed = true;
            }
            if (changed) SaveSystem.Save();
        }

        static void ResetPeriod(ProgressionConfig cfg, MissionPeriod period)
        {
            foreach (var m in cfg.missions)
                if (m != null && m.period == period)
                {
                    var s = State(m);
                    s.value = 0;
                    s.claimed = false;
                }
        }

        public static MissionState State(MissionData m)
        {
            var list = SaveSystem.Data.missions;
            foreach (var s in list)
                if (s.id == m.id) return s;
            var n = new MissionState { id = m.id };
            // Thành tựu đập thú tính cả số thú đã đập trước khi có nhiệm vụ
            if (m.period == MissionPeriod.Achievement && m.stat == MissionStat.CrittersHit) n.value = SaveSystem.Data.totalCritterHits;
            list.Add(n);
            return n;
        }

        // ───────── Nhiệm vụ ─────────

        public static int Progress(MissionData m) => Mathf.Min(State(m).value, m.goal);
        public static bool IsComplete(MissionData m) => State(m).value >= m.goal;
        public static bool IsClaimed(MissionData m) => State(m).claimed;
        public static bool CanClaim(MissionData m) => IsComplete(m) && !IsClaimed(m);

        /// <summary>Nhận thưởng nhiệm vụ: cộng xu và điểm nhiệm vụ (tối đa maxMissionPoints).</summary>
        public static bool Claim(ProgressionConfig cfg, MissionData m)
        {
            Refresh(cfg);
            if (!CanClaim(m)) return false;
            var d = SaveSystem.Data;
            State(m).claimed = true;
            d.coins += Mathf.Max(0, m.reward);
            if (m.period != MissionPeriod.Achievement)
                d.missionPoints = Mathf.Min(cfg.maxMissionPoints, d.missionPoints + Mathf.Max(0, m.points));
            SaveSystem.Save();
            return true;
        }

        public static ChestState Chest(ProgressionConfig cfg, int index)
        {
            var d = SaveSystem.Data;
            int pts = cfg.chestPoints[index];
            if (d.chestsOpened.Contains(pts)) return ChestState.Opened;
            return d.missionPoints >= pts ? ChestState.Ready : ChestState.Locked;
        }

        public static bool OpenChest(ProgressionConfig cfg, int index)
        {
            Refresh(cfg);
            if (Chest(cfg, index) != ChestState.Ready) return false;
            var d = SaveSystem.Data;
            d.chestsOpened.Add(cfg.chestPoints[index]);
            d.coins += index < cfg.chestRewards.Length ? Mathf.Max(0, cfg.chestRewards[index]) : 0;
            SaveSystem.Save();
            return true;
        }

        /// <summary>Số nhiệm vụ + rương đang chờ nhận (chấm đỏ ở nút Nhiệm vụ trang chủ).</summary>
        public static int PendingCount(ProgressionConfig cfg)
        {
            if (cfg == null) return 0;
            Refresh(cfg);
            int n = 0;
            foreach (var m in cfg.missions) if (m != null && CanClaim(m)) n++;
            for (int i = 0; i < cfg.chestPoints.Length; i++) if (Chest(cfg, i) == ChestState.Ready) n++;
            return n;
        }

        /// <summary>
        /// Ghi một ván vào nhiệm vụ và kinh nghiệm. Gọi sau SaveSystem.RecordRound (đã tính xu ván).
        /// Lên cấp cộng xu thưởng ngay.
        /// </summary>
        public static XpResult RecordRound(ProgressionConfig cfg, string mapId, int score, int critterHits, int bonusHits, int maxCombo)
        {
            var result = new XpResult();
            if (cfg == null) return result;
            Refresh(cfg);
            var d = SaveSystem.Data;
            if (!string.IsNullOrEmpty(mapId) && !d.weekMaps.Contains(mapId)) d.weekMaps.Add(mapId);

            foreach (var m in cfg.missions)
            {
                if (m == null) continue;
                var s = State(m);
                if (s.claimed) continue;
                switch (m.stat)
                {
                    case MissionStat.BestCombo: s.value = Mathf.Max(s.value, maxCombo); break;
                    case MissionStat.CrittersHit: s.value += critterHits; break;
                    case MissionStat.BonusHit: s.value += bonusHits; break;
                    case MissionStat.RoundsPlayed: s.value += 1; break;
                    case MissionStat.MapsPlayed: s.value = d.weekMaps.Count; break;
                    case MissionStat.BestScore: s.value = Mathf.Max(s.value, score); break;
                    case MissionStat.AdsWatched: break; // quảng cáo — bước 6
                }
            }

            // Kinh nghiệm = điểm ÷ xpDivisor; lên cấp n cần xpPerLevel × n, thưởng levelUpCoins × cấp mới
            result.xpGained = cfg.xpDivisor > 0 ? Mathf.Max(0, score) / cfg.xpDivisor : 0;
            d.xp += result.xpGained;
            while (d.xp >= cfg.XpToNext(d.level))
            {
                d.xp -= cfg.XpToNext(d.level);
                d.level++;
                result.levelsGained++;
                int bonus = cfg.levelUpCoins * d.level;
                result.coinsGained += bonus;
                d.coins += bonus;
            }
            SaveSystem.Save();
            return result;
        }

        public static float LevelProgress(ProgressionConfig cfg)
        {
            var d = SaveSystem.Data;
            return cfg == null ? 0f : Mathf.Clamp01(d.xp / (float)cfg.XpToNext(d.level));
        }

        public static IEnumerable<MissionData> Missions(ProgressionConfig cfg, MissionPeriod period)
        {
            if (cfg == null || cfg.missions == null) yield break;
            foreach (var m in cfg.missions) if (m != null && m.period == period) yield return m;
        }
    }
}
