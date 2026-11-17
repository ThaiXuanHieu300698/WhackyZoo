using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Lưới 3×3 mô phỏng tầm đập của dụng cụ ở khung chi tiết Cửa hàng: ô bị đập tô đậm, ô được chạm có icon dụng cụ.
    /// Tầm cột dọc (Gậy Như Ý) minh hoạ ở cột giữa. Tầm tam giác (Búa Thor) đổi qua lại giữa nửa trên và nửa dưới. Chạy theo thời gian thật.
    /// </summary>
    public class HitRangeView : MonoBehaviour
    {
        [Tooltip("9 ô, thứ tự hàng × 3 + cột (hàng 0 ở trên).")]
        [SerializeField] Image[] cells;
        [SerializeField] Sprite cellNormal;
        [SerializeField] Sprite cellHit;
        [Tooltip("Ô bị đập của dụng cụ có sấm sét.")]
        [SerializeField] Sprite cellLightning;
        [Tooltip("Icon dụng cụ đặt lên ô được chạm.")]
        [SerializeField] Image tapIcon;
        [Tooltip("Ô mẫu ở chú thích \"Ô bị đập\": cùng màu với ô bị đập.")]
        [SerializeField] Image legendSwatch;
        [Tooltip("Icon dụng cụ ở chú thích \"Ô bạn chạm\".")]
        [SerializeField] Image legendIcon;
        [Tooltip("Ô chạm minh hoạ: tầm 1 ô và cột dọc, tam giác trên, tam giác dưới.")]
        [SerializeField] int singleTap = 4, upperTap = 1, lowerTap = 7;
        [SerializeField] float swapSeconds = 1.4f;
        [Tooltip("Ô vừa tô nảy nhẹ: scale 0,8 → 1 trong thời gian này (ease-out).")]
        [SerializeField] float popSeconds = 0.18f;

        readonly List<int> area = new List<int>();
        ToolData tool;
        bool upper = true;
        float t, popT = -1f;

        public void Show(ToolData data)
        {
            bool changed = data != tool;
            tool = data;
            if (tool != null && tapIcon != null) tapIcon.sprite = tool.icon;
            if (tool != null && legendIcon != null) legendIcon.sprite = tool.icon;
            if (changed)
            {
                upper = true;
                t = 0f;
                Paint(changed);
            }
        }

        void Paint(bool pop)
        {
            if (tool == null) return;
            int tapped = tool.hitArea != HitArea.Triangle ? singleTap : upper ? upperTap : lowerTap;
            ToolData.AreaCells(tool.hitArea, tapped, upper, area);
            var hit = tool.lightning && cellLightning != null ? cellLightning : cellHit;
            if (legendSwatch != null) legendSwatch.sprite = hit;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].sprite = area.Contains(i) ? hit : cellNormal;
                cells[i].rectTransform.localScale = Vector3.one;
            }
            if (tapIcon != null)
            {
                var rt = tapIcon.rectTransform;
                rt.SetParent(cells[tapped].rectTransform, false);
                rt.anchoredPosition = Vector2.zero;
            }
            popT = pop ? 0f : -1f;
            ApplyPop(0f);
        }

        void Update()
        {
            if (tool == null) return;
            float dt = Time.unscaledDeltaTime;
            if (tool.hitArea == HitArea.Triangle)
            {
                t += dt;
                if (t >= swapSeconds)
                {
                    t -= swapSeconds;
                    upper = !upper;
                    Paint(true);
                }
            }
            if (popT >= 0f)
            {
                popT += dt;
                ApplyPop(popT);
            }
        }

        void ApplyPop(float time)
        {
            if (popT < 0f) return;
            float p = Mathf.Clamp01(time / popSeconds);
            float s = Mathf.Lerp(0.8f, 1f, Bezier.EaseOut.Evaluate(p));
            foreach (int i in area) cells[i].rectTransform.localScale = Vector3.one * s;
            if (p >= 1f) popT = -1f;
        }
    }
}
