using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WhackyZoo
{
    /// <summary>
    /// Chuyển giữa các màn trong scene Home (Trang chủ, Chọn map, Cài đặt) bằng hiệu ứng mờ dần,
    /// giữ ngăn xếp để nút quay lại / phím Back của Android về đúng màn trước.
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        [Tooltip("Màn đầu tiên khi mở scene.")]
        [SerializeField] CanvasGroup rootScreen;
        [SerializeField] CanvasGroup[] screens;
        [SerializeField] float fadeSeconds = 0.18f;

        readonly Stack<CanvasGroup> stack = new Stack<CanvasGroup>();
        CanvasGroup showing, hiding;
        float t = -1f;

        void Awake()
        {
            foreach (var s in screens) SetVisible(s, s == rootScreen);
            stack.Push(rootScreen);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Flush();
        }

        void OnApplicationQuit() => SaveSystem.Flush();

        public void Show(CanvasGroup screen)
        {
            if (screen == null || stack.Peek() == screen) return;
            var from = stack.Peek();
            stack.Push(screen);
            Transition(from, screen);
        }

        public void Back()
        {
            if (stack.Count <= 1) return;
            var from = stack.Pop();
            Transition(from, stack.Peek());
        }

        void Transition(CanvasGroup from, CanvasGroup to)
        {
            if (hiding != null) SetVisible(hiding, false); // kết thúc chuyển cảnh dở dang
            hiding = from;
            showing = to;
            showing.gameObject.SetActive(true);
            showing.alpha = 0f;
            showing.interactable = hiding.interactable = false;
            t = 0f;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Back(); // nút Back của Android

            if (t < 0f) return;
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeSeconds);
            showing.alpha = p;
            hiding.alpha = 1f - p;
            if (p < 1f) return;
            SetVisible(hiding, false);
            SetVisible(showing, true);
            hiding = showing = null;
            t = -1f;
        }

        static void SetVisible(CanvasGroup g, bool visible)
        {
            g.gameObject.SetActive(visible);
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
        }
    }
}
