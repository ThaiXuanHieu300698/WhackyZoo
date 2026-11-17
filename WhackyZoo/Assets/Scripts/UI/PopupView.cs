using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Lớp nền mờ + khung bật lên; chạy bằng thời gian không phụ thuộc Time.timeScale.
    /// Hiện ra: nền tối mờ dần vào, khung phóng to (thông số .resIn của thiết kế), rồi từng phần nội dung hiện lần lượt.
    /// Đóng (Close): khung thu nhẹ + mờ, nền mờ ra rồi mới tắt. Hide() tắt ngay (dùng khi đặt lại ván).
    /// </summary>
    public abstract class PopupView : MonoBehaviour
    {
        [Serializable]
        public class StaggerItem
        {
            public RectTransform target;
            [Tooltip("Trễ so với lúc mở popup (giây).")]
            public float delay;
            public float duration = 0.35f;
            public float fromScale = 0.85f;
            [Tooltip("Dịch xuống lúc bắt đầu (pt), trượt lên vị trí gốc.")]
            public float fromOffsetY = 10f;
        }

        [SerializeField] RectTransform panel;
        [Tooltip("Ảnh nền tối phủ màn (alpha gốc giữ nguyên, chỉ mờ dần vào/ra).")]
        [SerializeField] Image dim;
        [SerializeField] float dimSeconds = 0.2f;

        [Header("Khung (.resIn): scale fromScale → 1 trong showDuration giây")]
        [SerializeField] float fromScale = 0.7f;
        [SerializeField] float showDuration = 0.45f;
        [SerializeField] bool fadeIn = true;

        [Header("Nội dung hiện lần lượt")]
        [SerializeField] StaggerItem[] stagger;

        [Header("Đóng")]
        [SerializeField] float closeSeconds = 0.16f;
        [SerializeField] float closeScale = 0.92f;

        static readonly Bezier ShowEase = new Bezier(0.2f, 0.9f, 0.3f, 1.3f);
        static readonly Bezier StaggerEase = new Bezier(0.2f, 0.9f, 0.3f, 1.4f);
        const float PxToUnits = 3f;

        float showT = -1f, closeT = -1f;
        CanvasGroup panelGroup;
        float dimAlpha = -1f;
        Vector2[] itemBase;
        CanvasGroup[] itemGroups;
        float[] itemAlpha;
        Action onClosed;

        public bool IsOpen => gameObject.activeSelf && closeT < 0f;

        public virtual void Show()
        {
            gameObject.SetActive(true);
            Cache();
            closeT = -1f;
            onClosed = null;
            // alpha gốc của từng phần (vd. nút "sắp có" đang mờ 0,45) — hiệu ứng nhân thêm lên
            for (int i = 0; i < itemGroups.Length; i++) itemAlpha[i] = itemGroups[i] != null ? itemGroups[i].alpha : 1f;
            showT = 0f;
            Animate(0f);
        }

        /// <summary>Tắt ngay, không hiệu ứng.</summary>
        public void Hide()
        {
            showT = closeT = -1f;
            if (itemGroups != null) RestoreItems();
            gameObject.SetActive(false);
        }

        /// <summary>Đóng có hiệu ứng rồi tắt; <paramref name="done"/> gọi khi đã tắt hẳn.</summary>
        public void Close(Action done = null)
        {
            if (!gameObject.activeSelf) { done?.Invoke(); return; }
            Cache();
            showT = -1f;
            RestoreItems();
            onClosed = done;
            closeT = 0f;
        }

        void Cache()
        {
            if (panel != null && panelGroup == null)
            {
                panelGroup = panel.GetComponent<CanvasGroup>();
                if (panelGroup == null) panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }
            if (dim != null && dimAlpha < 0f) dimAlpha = dim.color.a;
            if (itemBase != null) return;
            int n = stagger != null ? stagger.Length : 0;
            itemBase = new Vector2[n];
            itemGroups = new CanvasGroup[n];
            itemAlpha = new float[n];
            for (int i = 0; i < n; i++)
            {
                var t = stagger[i].target;
                if (t == null) continue;
                itemBase[i] = t.anchoredPosition;
                itemGroups[i] = t.GetComponent<CanvasGroup>();
                if (itemGroups[i] == null) itemGroups[i] = t.gameObject.AddComponent<CanvasGroup>();
            }
        }

        void RestoreItems()
        {
            for (int i = 0; i < itemBase.Length; i++)
            {
                var t = stagger[i].target;
                if (t == null) continue;
                t.anchoredPosition = itemBase[i];
                t.localScale = Vector3.one;
                if (itemGroups[i] != null && itemAlpha != null) itemGroups[i].alpha = itemAlpha[i] > 0f ? itemAlpha[i] : 1f;
            }
        }

        protected virtual void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (closeT >= 0f)
            {
                closeT += dt;
                float p = Mathf.Clamp01(closeT / closeSeconds);
                float e = Bezier.EaseIn.Evaluate(p);
                if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(1f, closeScale, e);
                if (panelGroup != null) panelGroup.alpha = 1f - e;
                SetDim(1f - p);
                if (p >= 1f)
                {
                    closeT = -1f;
                    gameObject.SetActive(false);
                    if (panel != null) panel.localScale = Vector3.one;
                    if (panelGroup != null) panelGroup.alpha = 1f;
                    SetDim(1f);
                    var done = onClosed;
                    onClosed = null;
                    done?.Invoke();
                }
                return;
            }
            if (showT < 0f) return;
            showT += dt;
            Animate(showT);
        }

        void Animate(float t)
        {
            SetDim(dimSeconds > 0f ? Bezier.EaseOut.Evaluate(Mathf.Clamp01(t / dimSeconds)) : 1f);

            float p = showDuration > 0f ? Mathf.Clamp01(t / showDuration) : 1f;
            float e = ShowEase.Evaluate(p);
            if (panel != null) panel.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, e);
            if (panelGroup != null) panelGroup.alpha = fadeIn ? Mathf.Clamp01(p * 2f) : 1f;

            bool running = p < 1f;
            for (int i = 0; i < itemBase.Length; i++)
            {
                var s = stagger[i];
                if (s.target == null) continue;
                float q = Mathf.Clamp01((t - s.delay) / Mathf.Max(0.01f, s.duration));
                float k = StaggerEase.Evaluate(q);
                s.target.localScale = Vector3.one * Mathf.LerpUnclamped(s.fromScale, 1f, k);
                s.target.anchoredPosition = itemBase[i] + new Vector2(0f, -s.fromOffsetY * PxToUnits * (1f - Mathf.Clamp01(k)));
                if (itemGroups[i] != null) itemGroups[i].alpha = itemAlpha[i] * Mathf.Clamp01(q * 2.5f);
                if (q < 1f) running = true;
            }
            OnAnimate(t);
            if (!running && !KeepAnimating) showT = -1f;
        }

        /// <summary>Lớp con thêm hiệu ứng riêng theo thời gian kể từ lúc mở (vd. đếm điểm).</summary>
        protected virtual void OnAnimate(float t) { }

        /// <summary>Lớp con giữ vòng cập nhật chạy tiếp khi hiệu ứng riêng chưa xong.</summary>
        protected virtual bool KeepAnimating => false;

        void SetDim(float k)
        {
            if (dim == null || dimAlpha < 0f) return;
            var c = dim.color;
            c.a = dimAlpha * Mathf.Clamp01(k);
            dim.color = c;
        }

        protected static void SetAvailable(Button button, bool available) => UiUtil.SetAvailable(button, available);

        protected static bool HomeAvailable => Application.CanStreamedLevelBeLoaded(SceneNames.Home);
    }
}
