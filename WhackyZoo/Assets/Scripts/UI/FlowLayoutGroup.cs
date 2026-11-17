using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Xếp con từ trái sang phải, hết chỗ thì xuống dòng (như CSS display:flex; flex-wrap:wrap; align-items:center).
    /// Rộng mỗi con = preferred width (không quá bề rộng nhóm); cao mỗi con = chiều cao hiện có của nó.
    /// </summary>
    [ExecuteAlways]
    public class FlowLayoutGroup : LayoutGroup
    {
        [SerializeField] float spacingX = 12f;
        [SerializeField] float spacingY = 6f;

        public float SpacingX { get => spacingX; set { spacingX = value; SetDirty(); } }
        public float SpacingY { get => spacingY; set { spacingY = value; SetDirty(); } }

        readonly List<int> rowStart = new List<int>();
        readonly List<float> rowHeight = new List<float>();

        float ChildWidth(RectTransform c, float max) => Mathf.Min(LayoutUtility.GetPreferredWidth(c), max);
        static float ChildHeight(RectTransform c) => c.rect.height;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float sum = padding.horizontal, min = padding.horizontal;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                float w = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                sum += w + (i > 0 ? spacingX : 0f);
                min = Mathf.Max(min, padding.horizontal + LayoutUtility.GetMinWidth(rectChildren[i]));
            }
            SetLayoutInputForAxis(min, sum, -1, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float h = BuildRows(rectTransform.rect.width);
            SetLayoutInputForAxis(h, h, -1, -1, 1);
        }

        float BuildRows(float width)
        {
            rowStart.Clear();
            rowHeight.Clear();
            float inner = Mathf.Max(0f, width - padding.horizontal);
            float x = 0f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var c = rectChildren[i];
                float w = ChildWidth(c, inner);
                if (rowStart.Count == 0 || x > 0f && x + spacingX + w > inner + 0.01f)
                {
                    rowStart.Add(i);
                    rowHeight.Add(0f);
                    x = w;
                }
                else x += spacingX + w;
                rowHeight[rowHeight.Count - 1] = Mathf.Max(rowHeight[rowHeight.Count - 1], ChildHeight(c));
            }
            float total = padding.vertical;
            for (int r = 0; r < rowHeight.Count; r++) total += rowHeight[r] + (r > 0 ? spacingY : 0f);
            return total;
        }

        public override void SetLayoutHorizontal() => Place();
        public override void SetLayoutVertical() => Place();

        void Place()
        {
            float inner = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal);
            BuildRows(rectTransform.rect.width);
            float y = padding.top;
            for (int r = 0; r < rowStart.Count; r++)
            {
                int end = r + 1 < rowStart.Count ? rowStart[r + 1] : rectChildren.Count;
                float x = padding.left;
                for (int i = rowStart[r]; i < end; i++)
                {
                    var c = rectChildren[i];
                    float w = ChildWidth(c, inner), h = ChildHeight(c);
                    SetChildAlongAxis(c, 0, x, w);
                    SetChildAlongAxis(c, 1, y + (rowHeight[r] - h) / 2f, h);
                    x += w + spacingX;
                }
                y += rowHeight[r] + spacingY;
            }
        }
    }
}
