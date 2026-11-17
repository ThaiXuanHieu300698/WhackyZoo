using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Viền chữ dày + bóng đổ cứng như .txt3d của bản thiết kế
    /// (-webkit-text-stroke + paint-order stroke fill + text-shadow 0 Npx 0 #3A2412).
    /// Outline có sẵn của Unity chỉ lệch 4 góc nên viền dày bị gãy; ở đây vẽ thêm các bản sao quanh một vòng tròn.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextStroke : BaseMeshEffect
    {
        [SerializeField] Color color = new Color32(0x3A, 0x24, 0x12, 0xFF);
        [Tooltip("Bề dày viền nhìn thấy (đơn vị canvas) = nửa -webkit-text-stroke-width.")]
        [SerializeField] float radius = 9f;
        [SerializeField, Range(8, 32)] int samples = 16;
        [Tooltip("Bóng đổ cứng (đơn vị canvas, y âm = xuống). (0,0) = không bóng.")]
        [SerializeField] Vector2 shadowOffset = new Vector2(0f, -15f);

        static readonly List<UIVertex> src = new List<UIVertex>();
        static readonly List<UIVertex> dst = new List<UIVertex>();

        public void Configure(Color strokeColor, float strokeRadius, Vector2 shadow)
        {
            color = strokeColor;
            radius = strokeRadius;
            shadowOffset = shadow;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            src.Clear();
            dst.Clear();
            vh.GetUIVertexStream(src);
            if (src.Count == 0) return;

            if (shadowOffset != Vector2.zero) Ring(shadowOffset);
            Ring(Vector2.zero);
            dst.AddRange(src);

            vh.Clear();
            vh.AddUIVertexTriangleStream(dst);
        }

        /// <summary>Một vòng bản sao màu viền quanh tâm <paramref name="center"/> (kèm bản ở tâm để lấp ruột).</summary>
        void Ring(Vector2 center)
        {
            Copy(center);
            for (int i = 0; i < samples; i++)
            {
                float a = i * Mathf.PI * 2f / samples;
                Copy(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }

        void Copy(Vector2 offset)
        {
            Color32 c = color;
            for (int i = 0; i < src.Count; i++)
            {
                var v = src[i];
                v.position += (Vector3)offset;
                c.a = (byte)(color.a * v.color.a);
                v.color = c;
                dst.Add(v);
            }
        }
    }
}
