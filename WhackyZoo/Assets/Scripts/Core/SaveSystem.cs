using System;
using System.IO;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Đọc/ghi tiến trình ở Application.persistentDataPath/save.json.
    /// Ghi qua file tạm rồi thay thế để mất điện giữa chừng không làm hỏng file lưu.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";

        static SaveData data;
        static bool dirty;

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath))
                    data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                // File hỏng: giữ lại một bản để điều tra, bắt đầu dữ liệu mới
                Debug.LogWarning($"[WhackyZoo] Không đọc được file lưu, tạo mới. {e.Message}");
                try { File.Copy(FilePath, FilePath + ".corrupt", true); } catch { /* bỏ qua */ }
            }
            if (data == null) data = new SaveData();
            if (data.settings == null) data.settings = new SettingsData();
            if (data.maps == null) data.maps = new System.Collections.Generic.List<MapProgress>();
            if (data.items == null) data.items = new System.Collections.Generic.List<ItemCount>();
            if (data.owned == null) data.owned = new System.Collections.Generic.List<string>();
            if (data.petsOn == null) data.petsOn = new System.Collections.Generic.List<string>();
            if (data.missions == null) data.missions = new System.Collections.Generic.List<MissionState>();
            if (data.chestsOpened == null) data.chestsOpened = new System.Collections.Generic.List<int>();
            if (data.weekMaps == null) data.weekMaps = new System.Collections.Generic.List<string>();
            if (data.level < 1) data.level = 1;
            data.version = SaveData.CurrentVersion;
            dirty = false;
        }

        /// <summary>Đánh dấu cần ghi (vd. kéo thanh âm lượng); ghi thật khi gọi Flush hoặc khi app tạm dừng.</summary>
        public static void MarkDirty() => dirty = true;

        public static void Flush()
        {
            if (dirty) Save();
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WhackyZoo] Không ghi được file lưu: {e.Message}");
            }
        }

        /// <summary>Tặng vật phẩm dùng thử (BoosterData.starterCount) một lần cho người chơi mới.</summary>
        public static void EnsureStarterItems(BoosterCatalog catalog)
        {
            var d = Data;
            if (d.starterItemsGranted || catalog == null || catalog.boosters == null) return;
            foreach (var b in catalog.boosters)
                if (b != null && b.starterCount > 0) d.AddItem(b.id, b.starterCount);
            d.starterItemsGranted = true;
            Save();
        }

        /// <summary>Dùng 1 vật phẩm; false nếu đã hết.</summary>
        public static bool UseItem(string id)
        {
            if (Data.ItemCount(id) <= 0) return false;
            Data.AddItem(id, -1);
            MarkDirty();
            return true;
        }

        /// <summary>Trừ xu nếu đủ; false nếu không đủ xu.</summary>
        public static bool SpendCoins(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            Save();
            return true;
        }

        /// <summary>Mua món (dụng cụ / thú) bằng xu; false nếu đã có hoặc không đủ xu.</summary>
        public static bool BuyWithCoins(ShopItemData item)
        {
            var d = Data;
            if (item == null || d.Owns(item.id) || d.coins < item.price) return false;
            d.coins -= item.price;
            d.owned.Add(item.id);
            Save();
            return true;
        }

        /// <summary>Mua <paramref name="count"/> vật phẩm với tổng giá <paramref name="cost"/>.</summary>
        public static bool BuyBooster(BoosterData b, int count, int cost)
        {
            var d = Data;
            if (b == null || count <= 0 || d.coins < cost) return false;
            d.coins -= cost;
            d.AddItem(b.id, count);
            Save();
            return true;
        }

        public static void EquipTool(ToolData tool)
        {
            if (tool == null) return;
            Data.equippedTool = tool.id;
            Save();
        }

        /// <summary>Dụng cụ đang trang bị (mặc định nếu chưa chọn, id không còn trong danh mục, hoặc hết hạn dùng thử).</summary>
        public static ToolData EquippedTool(ShopCatalog catalog)
        {
            if (catalog == null) return null;
            var t = catalog.FindTool(Data.equippedTool);
            return t != null && CanUseTool(t) ? t : catalog.DefaultTool;
        }

        /// <summary>Dùng được: dụng cụ mặc định, đã mua, hoặc đang trong hạn dùng thử (quà điểm danh).</summary>
        public static bool CanUseTool(ToolData tool) => tool != null && (tool.isDefault || Data.Owns(tool.id) || TrialLeft(tool) > System.TimeSpan.Zero);

        /// <summary>Thời gian dùng thử còn lại (0 nếu không dùng thử hoặc đã sở hữu vĩnh viễn).</summary>
        public static System.TimeSpan TrialLeft(ToolData tool)
        {
            var d = Data;
            if (tool == null || d.Owns(tool.id) || d.trialToolId != tool.id || d.trialToolUntil <= 0) return System.TimeSpan.Zero;
            var left = new System.DateTime(d.trialToolUntil) - System.DateTime.Now;
            return left > System.TimeSpan.Zero ? left : System.TimeSpan.Zero;
        }

        /// <summary>Cho dùng thử <paramref name="tool"/> trong <paramref name="days"/> ngày tính từ bây giờ và trang bị luôn. Đã sở hữu thì bỏ qua.</summary>
        public static bool GrantToolTrial(ToolData tool, int days)
        {
            var d = Data;
            if (tool == null || days <= 0 || tool.isDefault || d.Owns(tool.id)) return false;
            d.trialToolId = tool.id;
            d.trialToolUntil = System.DateTime.Now.AddDays(days).Ticks;
            d.equippedTool = tool.id;
            Save();
            return true;
        }

        /// <summary>"6 ngày 23 giờ" / "5 giờ 12 phút" / "12 phút".</summary>
        public static string TrialText(System.TimeSpan left)
        {
            if (left.TotalDays >= 1) return Loc.F("{0} ngày {1} giờ", (int)left.TotalDays, left.Hours);
            if (left.TotalHours >= 1) return Loc.F("{0} giờ {1} phút", (int)left.TotalHours, left.Minutes);
            return Loc.F("{0} phút", System.Math.Max(1, left.Minutes));
        }

        /// <summary>Thêm thú vào bộ sưu tập và bật ở map của nó; đã có thì không làm gì.</summary>
        public static bool GrantPet(PetData pet)
        {
            var d = Data;
            if (pet == null || pet.isBase || d.Owns(pet.id)) return false;
            d.owned.Add(pet.id);
            if (!d.petsOn.Contains(pet.id)) d.petsOn.Add(pet.id);
            Save();
            return true;
        }

        public static bool IsPetOn(PetData pet) => pet != null && Data.petsOn.Contains(pet.id);

        public static void SetPetOn(PetData pet, bool on)
        {
            if (pet == null || pet.isBase || !Data.Owns(pet.id)) return;
            var list = Data.petsOn;
            if (on && !list.Contains(pet.id)) list.Add(pet.id);
            if (!on) list.Remove(pet.id);
            Save();
        }

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Data.coins += amount;
            Save();
        }

        /// <summary>Ghi kết quả một ván; trả về true nếu là kỷ lục mới của map.</summary>
        public static bool RecordRound(string mapId, int score, int critterHits, int coins)
        {
            var d = Data;
            d.coins += Mathf.Max(0, coins);
            var map = d.GetMap(mapId, create: true);
            map.plays++;
            bool best = score > map.bestScore;
            if (best) map.bestScore = score;
            d.totalPlays++;
            d.totalCritterHits += critterHits;
            d.lastMapId = mapId;
            Save();
            return best;
        }

        /// <summary>Thời gian ván theo cài đặt; giá trị lạ (file cũ, sửa tay) thì dùng mặc định trong GameConfig.</summary>
        public static int RoundSeconds(GameConfig config)
        {
            int s = Data.settings.roundSeconds;
            if (config.roundSecondsOptions != null)
                foreach (int o in config.roundSecondsOptions)
                    if (o == s) return s;
            return Mathf.RoundToInt(config.roundSeconds);
        }
    }
}
