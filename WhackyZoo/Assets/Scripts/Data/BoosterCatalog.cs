using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Danh sách vật phẩm hỗ trợ theo thứ tự hiển thị ở thanh vật phẩm, màn Cách chơi và Cửa hàng.</summary>
    [CreateAssetMenu(fileName = "BoosterCatalog", menuName = "WhackyZoo/Booster Catalog")]
    public class BoosterCatalog : ScriptableObject
    {
        public BoosterData[] boosters;

        public BoosterData Find(string id)
        {
            if (boosters == null) return null;
            foreach (var b in boosters)
                if (b != null && b.id == id) return b;
            return null;
        }
    }
}
