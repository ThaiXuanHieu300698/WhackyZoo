using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Hiệu ứng .enter của bản thiết kế: mỗi lần bật, trượt lên từ dưới <see cref="offsetPt"/> và mờ dần vào.
    /// Mặc định 0,5 s, cubic-bezier(.2,.9,.3,1.2).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIEnter : MonoBehaviour
    {
        [SerializeField] float offsetPt = 24f;
        [SerializeField] float duration = 0.5f;
        [SerializeField] float delay;

        static readonly Bezier Ease = new Bezier(0.2f, 0.9f, 0.3f, 1.2f);
        RectTransform rt;
        CanvasGroup group;
        Vector2 basePos;
        float t = -1f;

        void Awake()
        {
            rt = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            basePos = rt.anchoredPosition;
        }

        void OnEnable()
        {
            t = 0f;
            Apply(0f);
        }

        void OnDisable()
        {
            t = -1f;
            Apply(1f);
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01((t - delay) / duration);
            Apply(Ease.Evaluate(p));
            if (p >= 1f) t = -1f;
        }

        void Apply(float e)
        {
            if (rt == null) return;
            rt.anchoredPosition = basePos + new Vector2(0f, -offsetPt * 3f * (1f - e));
            group.alpha = Mathf.Clamp01(e);
        }
    }
}
