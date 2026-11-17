using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Giãn chữ (CSS letter-spacing) cho UI Text một dòng căn giữa. Đặt TRƯỚC TextStroke để viền đi theo chữ đã giãn.
    /// Unity không sinh đỉnh cho dấu cách nên khoảng giãn chỉ tính giữa các chữ nhìn thấy.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LetterSpacing : BaseMeshEffect
    {
        [Tooltip("Khoảng giãn giữa các chữ (đơn vị canvas).")]
        [SerializeField] float spacing;

        static readonly List<UIVertex> stream = new List<UIVertex>();

        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Mathf.Approximately(spacing, 0f)) return;
            stream.Clear();
            vh.GetUIVertexStream(stream);
            int glyphs = stream.Count / 6; // 2 tam giác mỗi chữ
            if (glyphs < 2) return;
            float start = -(glyphs - 1) * spacing / 2f; // giữ chữ căn giữa
            for (int i = 0; i < stream.Count; i++)
            {
                var v = stream[i];
                v.position.x += start + (i / 6) * spacing;
                stream[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(stream);
        }
    }
}
