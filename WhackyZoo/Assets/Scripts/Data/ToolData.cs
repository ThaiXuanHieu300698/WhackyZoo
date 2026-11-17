using System.Collections.Generic;
using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Tầm đập của dụng cụ trên lưới 3×3.</summary>
    public enum HitArea
    {
        [Tooltip("Chỉ ô được chạm.")]
        Single,
        [Tooltip("6 ô: nửa tam giác trên (cột ≥ hàng) hoặc dưới (cột ≤ hàng) theo đường chéo trái-trên → phải-dưới, chọn nửa chứa ô được chạm.")]
        Triangle,
        [Tooltip("3 ô cột dọc: ô được chạm cùng ô trên và ô dưới (Gậy Như Ý).")]
        Column,
    }

    /// <summary>
    /// Dụng cụ đập (Docs/GameSpec.md › Cửa hàng › Dụng cụ đập). Hiện chỉ khác nhau ở hình và tầm đập;
    /// mô tả (description) là một dòng nói về tầm đập.
    /// </summary>
    [CreateAssetMenu(fileName = "Tool", menuName = "WhackyZoo/Tool")]
    public class ToolData : ShopItemData
    {
        [Tooltip("Dụng cụ mặc định, có sẵn từ đầu (Búa Gỗ).")]
        public bool isDefault;
        [Tooltip("Các ô bị đập khi chạm một ô.")]
        public HitArea hitArea = HitArea.Single;
        [Tooltip("Sấm sét lan từ ô chạm sang các ô trong tầm đập (Búa Thor).")]
        public bool lightning;
        [Min(0), Tooltip("Thêm % xu mỗi ván (Gậy Bóng Chày: 20).")]
        public int coinBonusPercent;

        public const int GridSize = 3;

        /// <summary>
        /// Các ô (chỉ số hàng × 3 + cột, hàng 0 ở xa nhất) bị đập khi chạm ô <paramref name="tapped"/>.
        /// Ô nằm trên đường chéo thuộc cả hai nửa tam giác: <paramref name="upperOnDiagonal"/> chọn nửa nào.
        /// </summary>
        public static void AreaCells(HitArea area, int tapped, bool upperOnDiagonal, List<int> result)
        {
            result.Clear();
            if (area == HitArea.Single)
            {
                result.Add(tapped);
                return;
            }
            int tr = tapped / GridSize, tc = tapped % GridSize;
            if (area == HitArea.Column)
            {
                result.Add(tapped);
                for (int r = 0; r < GridSize; r++)
                    if (r != tr) result.Add(r * GridSize + tc);
                return;
            }
            bool upper = tc > tr || (tc == tr && upperOnDiagonal);
            result.Add(tapped);
            for (int r = 0; r < GridSize; r++)
                for (int c = 0; c < GridSize; c++)
                {
                    int i = r * GridSize + c;
                    if (i != tapped && (upper ? c >= r : c <= r)) result.Add(i);
                }
        }
    }
}
