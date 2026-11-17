using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Thú ở tab Thú của Cửa hàng (Docs/GameSpec.md › Cửa hàng › Thú). Mọi thú ló/rụt giống nhau, chỉ khác điểm thưởng.
    /// Thú có sẵn của map (isBase) chỉ để xem; thú mua thêm bật/tắt bằng nút "Dùng ở &lt;map&gt;" và khi bật thì
    /// ló lên ở map đó với cùng tỉ lệ như thú có sẵn.
    /// </summary>
    [CreateAssetMenu(fileName = "Pet", menuName = "WhackyZoo/Pet")]
    public class PetData : ShopItemData
    {
        [Tooltip("Map mà thú xuất hiện (MapData.id).")]
        public string mapId;
        [Tooltip("Thú có sẵn của map (nhãn \"Có sẵn\").")]
        public bool isBase;
        [Tooltip("Điểm mỗi cú đập trúng.")]
        public int points = 10;
        [Tooltip("Dữ liệu ló lên trong màn chơi (thú có sẵn: CritterData của map; thú mua: CritterData riêng).")]
        public CritterData critter;
    }
}
