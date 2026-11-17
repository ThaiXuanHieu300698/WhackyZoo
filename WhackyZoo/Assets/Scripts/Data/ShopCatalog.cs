using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Hàng bán ở Cửa hàng theo thứ tự hiển thị; vật phẩm hỗ trợ lấy từ BoosterCatalog.</summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "WhackyZoo/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        public ToolData[] tools;
        [Tooltip("Thú theo map: mỗi map liệt kê thú có sẵn trước rồi tới thú mua thêm.")]
        public PetData[] pets;
        public BoosterCatalog boosters;

        public ToolData DefaultTool
        {
            get
            {
                if (tools == null) return null;
                foreach (var t in tools) if (t != null && t.isDefault) return t;
                return tools.Length > 0 ? tools[0] : null;
            }
        }

        public ToolData FindTool(string id)
        {
            if (tools != null)
                foreach (var t in tools) if (t != null && t.id == id) return t;
            return null;
        }
    }
}
