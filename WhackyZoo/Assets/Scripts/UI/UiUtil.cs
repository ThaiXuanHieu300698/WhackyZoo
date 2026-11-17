using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    public static class UiUtil
    {
        /// <summary>"+10" / "-20".</summary>
        public static string Signed(int v) => v >= 0 ? "+" + v : "-" + Mathf.Abs(v);

        /// <summary>12450 → "12.450" (dấu chấm ngăn nghìn như thiết kế).</summary>
        /// <summary>1.000 (tiếng Việt) / 1,000 (English).</summary>
        public static string Thousands(int v) => Loc.English ? v.ToString("#,0") : v.ToString("#,0").Replace(',', '.');

        public static void ApplyChip(Image background, Text label, ChipStyle style)
        {
            if (background != null)
            {
                if (style.sprite != null) background.sprite = style.sprite;
                background.color = style.tint.a > 0f ? style.tint : Color.white;
            }
            if (label != null && style.textColor.a > 0f) label.color = style.textColor;
        }

        /// <summary>Điểm thấp nhất trong nhóm thú thường (chú thích ghi "Thú thường +10").</summary>
        public static int MinNormalPoints(MapData map)
        {
            int pts = int.MaxValue;
            if (map.normalCritters != null)
                foreach (var c in map.normalCritters)
                    if (c != null) pts = Mathf.Min(pts, c.points);
            return pts == int.MaxValue ? 0 : pts;
        }

        /// <summary>Bật/tắt một nút; nút chưa dùng được (tính năng của bước sau) thì mờ đi.</summary>
        public static void SetAvailable(Selectable control, bool available)
        {
            if (control == null) return;
            control.interactable = available;
            var group = control.GetComponent<CanvasGroup>();
            if (group == null) group = control.gameObject.AddComponent<CanvasGroup>();
            group.alpha = available ? 1f : 0.45f;
        }
    }
}
