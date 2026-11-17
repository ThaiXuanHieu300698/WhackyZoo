using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Công tắc bật/tắt dạng viên thuốc, núm trượt qua lại.</summary>
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image track;
        [Tooltip("Lớp nền khi bật (vd. dải xanh) đặt chồng lên track; có thì mờ dần vào thay cho đổi màu track.")]
        [SerializeField] Image onFill;
        [SerializeField] RectTransform knob;
        [SerializeField] Color onColor = new Color32(0x4C, 0xC2, 0x5E, 0xFF);
        [SerializeField] Color offColor = new Color32(0xC9, 0xBF, 0xAE, 0xFF);
        [SerializeField] float knobTravel = 39f; // đơn vị canvas, mỗi bên tính từ tâm
        [SerializeField] float speed = 8f;

        public event Action<bool> Changed;
        public bool IsOn { get; private set; }

        float pos; // 0 = tắt, 1 = bật

        public void SetValue(bool on, bool animate)
        {
            IsOn = on;
            if (!animate) pos = on ? 1f : 0f;
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetValue(!IsOn, true);
            Changed?.Invoke(IsOn);
        }

        void Update()
        {
            float target = IsOn ? 1f : 0f;
            if (Mathf.Approximately(pos, target)) return;
            pos = Mathf.MoveTowards(pos, target, speed * Time.unscaledDeltaTime);
            Refresh();
        }

        void Refresh()
        {
            float e = Bezier.EaseInOut.Evaluate(pos);
            knob.anchoredPosition = new Vector2(Mathf.Lerp(-knobTravel, knobTravel, e), knob.anchoredPosition.y);
            if (onFill != null)
            {
                var c = onFill.color;
                c.a = e;
                onFill.color = c;
            }
            else track.color = Color.Lerp(offColor, onColor, e);
        }
    }
}
