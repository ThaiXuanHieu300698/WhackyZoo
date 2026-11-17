using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Thanh vật phẩm hỗ trợ dưới cùng màn chơi (GameLand25D toolbar): mỗi nút có icon, nhãn ngắn, badge số lượng.
    /// Badge: đỏ "xN" khi còn, xanh "BẬT" khi đang có hiệu lực (kèm viền sáng vàng), xám "x0" khi hết (nút mờ, không bấm được).
    /// </summary>
    public class BoosterBar : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public Button button;
            public Image face;
            public Image icon;
            public Text label;
            public Image badge;
            public Text badgeText;
            [Tooltip("Viền sáng vàng quanh nút khi vật phẩm đang bật.")]
            public GameObject glow;
            public CanvasGroup group;
        }

        [SerializeField] Slot[] slots;
        [SerializeField] Sprite badgeRed;
        [SerializeField] Sprite badgeGreen;
        [SerializeField] Sprite badgeGrey;
        [Tooltip("Độ đậm của nút khi hết vật phẩm.")]
        [SerializeField, Range(0f, 1f)] float emptyAlpha = 0.5f;
        [SerializeField] string activeText = "BẬT";

        BoosterData[] items = Array.Empty<BoosterData>();
        Func<BoosterData, bool> isActive;

        public void Init(BoosterCatalog catalog, Action<BoosterData> onUse, Func<BoosterData, bool> activeCheck)
        {
            items = catalog != null && catalog.boosters != null ? catalog.boosters : Array.Empty<BoosterData>();
            isActive = activeCheck;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                var b = i < items.Length ? items[i] : null;
                s.button.gameObject.SetActive(b != null);
                if (b == null) continue;
                if (b.buttonSprite != null) s.face.sprite = b.buttonSprite;
                s.icon.sprite = b.icon;
                s.label.text = Loc.T(b.barLabel);
                s.button.onClick.RemoveAllListeners();
                s.button.onClick.AddListener(() => onUse(b));
            }
            Refresh();
        }

        public void Refresh()
        {
            for (int i = 0; i < slots.Length && i < items.Length; i++)
            {
                var s = slots[i];
                var b = items[i];
                if (b == null) continue;
                bool on = isActive != null && isActive(b);
                int q = SaveSystem.Data.ItemCount(b.id);
                s.badgeText.text = on ? Loc.T(activeText) : "x" + q;
                s.badge.sprite = on ? badgeGreen : q > 0 ? badgeRed : badgeGrey;
                if (s.glow != null) s.glow.SetActive(on);
                bool usable = q > 0 && !on;
                s.button.interactable = usable;
                if (s.group != null) s.group.alpha = q == 0 && !on ? emptyAlpha : 1f;
            }
        }
    }
}
