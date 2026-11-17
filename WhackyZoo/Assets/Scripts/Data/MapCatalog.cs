using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Danh sách map theo thứ tự hiển thị ở trang Chọn map. Thêm map mới = thêm asset vào đây.</summary>
    [CreateAssetMenu(fileName = "MapCatalog", menuName = "WhackyZoo/Map Catalog")]
    public class MapCatalog : ScriptableObject
    {
        public MapData[] maps;

        public MapData Find(string id)
        {
            if (maps == null) return null;
            foreach (var m in maps)
                if (m != null && m.id == id) return m;
            return null;
        }
    }
}
