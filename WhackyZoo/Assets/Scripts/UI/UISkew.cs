using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Nghiêng ngang (CSS skewX) một Graphic quanh mép dưới — UI của Unity không có sẵn phép skew.</summary>
    [RequireComponent(typeof(Graphic))]
    public class UISkew : BaseMeshEffect
    {
        [SerializeField] float angle;
        [Tooltip("Gốc nghiêng theo chiều cao (0 = đáy như rong biển, 0,5 = giữa như transform-origin mặc định của CSS).")]
        [SerializeField, Range(0f, 1f)] float originY;

        public float OriginY { get => originY; set => originY = value; }

        /// <summary>Góc skewX theo CSS (độ).</summary>
        public float Angle
        {
            get => angle;
            set
            {
                if (Mathf.Approximately(angle, value)) return;
                angle = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Mathf.Approximately(angle, 0f)) return;
            var rect = graphic.rectTransform.rect;
            float bottom = rect.yMin + rect.height * originY;
            float k = Mathf.Tan(angle * Mathf.Deg2Rad);
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                // CSS skewX(θ) gốc ở đáy: điểm cao h phía trên đáy dịch ngang −tan(θ)·h
                v.position.x -= k * (v.position.y - bottom);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
