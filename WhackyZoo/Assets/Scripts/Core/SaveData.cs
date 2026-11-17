using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Cài đặt người chơi (Docs/GameSpec.md › Cài đặt).</summary>
    [Serializable]
    public class SettingsData
    {
        /// <summary>0 = dùng GameConfig.roundSeconds.</summary>
        public int roundSeconds;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.85f;
        public bool vibrate = true;
        public string language = "vi";
    }

    [Serializable]
    public class MapProgress
    {
        public string mapId;
        public int bestScore;
        public int plays;
    }

    /// <summary>Số lượng một món trong kho (vật phẩm hỗ trợ…), theo id.</summary>
    [Serializable]
    public class ItemCount
    {
        public string id;
        public int count;
    }

    /// <summary>Tiến độ một nhiệm vụ (MissionData.id) trong kỳ hiện tại.</summary>
    [Serializable]
    public class MissionState
    {
        public string id;
        public int value;
        public bool claimed;
    }

    /// <summary>Toàn bộ dữ liệu lưu trên máy. Thêm trường mới thì để giá trị mặc định hợp lý: file cũ thiếu trường vẫn đọc được.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public SettingsData settings = new SettingsData();
        public List<MapProgress> maps = new List<MapProgress>();
        public int totalPlays;
        public int totalCritterHits;
        public string lastMapId;

        [Tooltip("Xu đang có.")]
        public int coins;
        [Tooltip("Vật phẩm hỗ trợ đang có, theo BoosterData.id.")]
        public List<ItemCount> items = new List<ItemCount>();
        [Tooltip("Đã tặng vật phẩm dùng thử cho người chơi mới (BoosterData.starterCount).")]
        public bool starterItemsGranted;

        [Tooltip("Dụng cụ và thú đã mua (ToolData.id / PetData.id).")]
        public List<string> owned = new List<string>();
        [Tooltip("Dụng cụ đang trang bị; trống = dụng cụ mặc định.")]
        public string equippedTool;
        [Tooltip("Thú mua thêm đang bật trong map của nó.")]
        public List<string> petsOn = new List<string>();

        [Header("Cấp độ")]
        public int level = 1;
        [Tooltip("Kinh nghiệm trong cấp hiện tại.")]
        public int xp;

        [Header("Nhiệm vụ")]
        public List<MissionState> missions = new List<MissionState>();
        [Tooltip("Ngày (yyyy-MM-dd) của kỳ nhiệm vụ ngày đang tính.")]
        public string missionDay;
        [Tooltip("Thứ Hai (yyyy-MM-dd) của kỳ nhiệm vụ tuần đang tính.")]
        public string missionWeek;
        [Tooltip("Điểm nhiệm vụ trong ngày (thanh rương).")]
        public int missionPoints;
        [Tooltip("Mốc rương đã mở trong ngày (điểm).")]
        public List<int> chestsOpened = new List<int>();
        [Tooltip("Map đã chơi trong tuần (nhiệm vụ \"Chơi đủ cả 5 map\").")]
        public List<string> weekMaps = new List<string>();

        [Header("Điểm danh tháng")]
        [Tooltip("Tháng (yyyy-MM) của lịch điểm danh đang tính; sang tháng mới thì xoá các ngày đã nhận.")]
        public string checkInMonth;
        [Tooltip("Các ô (1–30) đã nhận trong tháng.")]
        public List<int> checkInDays = new List<int>();
        [Tooltip("Số lần điểm danh bù đã dùng trong tháng.")]
        public int checkInMakeUpsUsed;

        [Header("Dùng thử dụng cụ (quà điểm danh)")]
        [Tooltip("Dụng cụ đang dùng thử (ToolData.id).")]
        public string trialToolId;
        [Tooltip("Hết hạn dùng thử (DateTime.Ticks, giờ máy).")]
        public long trialToolUntil;

        public bool Owns(string id) => owned.Contains(id);

        public int ItemCount(string id)
        {
            foreach (var i in items)
                if (i.id == id) return i.count;
            return 0;
        }

        public void AddItem(string id, int delta)
        {
            foreach (var i in items)
                if (i.id == id) { i.count = Math.Max(0, i.count + delta); return; }
            if (delta > 0) items.Add(new ItemCount { id = id, count = delta });
        }

        public MapProgress GetMap(string mapId, bool create = false)
        {
            foreach (var m in maps)
                if (m.mapId == mapId) return m;
            if (!create) return null;
            var p = new MapProgress { mapId = mapId };
            maps.Add(p);
            return p;
        }

        public int BestScore(string mapId) => GetMap(mapId)?.bestScore ?? 0;
    }

}
