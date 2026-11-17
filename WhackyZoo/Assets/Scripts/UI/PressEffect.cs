using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Nút lún xuống 4 pt khi nhấn rồi bật lên (như .gbtn:active của bản thiết kế).
    /// Vị trí gốc lấy ngay lúc bắt đầu nhấn (không lưu từ Awake) vì nút có thể nằm trong layout group
    /// và được xếp lại sau khi bật/tắt; chỉ trả về vị trí gốc khi chính nó đã dịch nút.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class PressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float depthUnits = 12f; // 4 pt × 3
        [SerializeField] float speed = 30f;

        Selectable selectable;
        RectTransform rt;
        Vector2 basePos;
        bool pressed;
        float amount;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            rt = (RectTransform)transform;
        }

        void OnDisable()
        {
            pressed = false;
            if (amount > 0f && rt != null) rt.anchoredPosition = basePos;
            amount = 0f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!selectable.IsInteractable()) return;
            if (amount <= 0f) basePos = rt.anchoredPosition;
            pressed = true;
            AudioManager.Play(Sfx.Click);
        }

        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnPointerExit(PointerEventData e) => pressed = false;

        void Update()
        {
            float target = pressed ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            amount = Mathf.MoveTowards(amount, target, speed * Time.unscaledDeltaTime);
            rt.anchoredPosition = basePos + Vector2.down * depthUnits * amount;
        }
    }
}
