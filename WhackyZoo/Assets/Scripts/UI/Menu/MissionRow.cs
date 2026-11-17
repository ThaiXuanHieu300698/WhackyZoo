using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Một dòng nhiệm vụ (Missions25D .panel.row): ô icon 52, tên 15 (1–2 dòng), thanh tiến độ 10 + "32/50",
    /// dòng thưởng "+100 · +20 điểm", bên phải nút Nhận (vàng, nhịp 0,8 s) / Chơi (xanh) / Xong (viền gạch xanh).
    /// Dòng tự đặt chiều cao theo số dòng tên; phần nhìn thấy (visual) trượt vào khi đổi tab.
    /// </summary>
    public class MissionRow : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [SerializeField] LayoutElement layout;
        [Tooltip("Phần nhìn thấy (con của dòng): trượt từ phải 30 px + mờ vào khi đổi tab.")]
        [SerializeField] RectTransform visual;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image background;
        [SerializeField] Sprite normalBackground;
        [Tooltip("Nền khi đủ điều kiện nhận: #FFFBE0 → #FFEBA8, bóng #C99A2E.")]
        [SerializeField] Sprite readyBackground;
        [SerializeField] Image icon;
        [SerializeField] Text title;
        [SerializeField] RectTransform titleRect;
        [SerializeField] RectTransform barRow;
        [SerializeField] RectTransform barFill;
        [SerializeField] Image barFillImage;
        [SerializeField] Sprite fillBlue;
        [SerializeField] Sprite fillGreen;
        [SerializeField] Text progress;
        [SerializeField] RectTransform rewardRow;
        [SerializeField] Text reward;
        [SerializeField] Button claimButton;
        [SerializeField] Button playButton;
        [SerializeField] GameObject doneMark;
        [SerializeField] GameObject soonMark;

        [Header("Kích thước (px thiết kế)")]
        [SerializeField] float titleLineHeight = 17.25f;
        [SerializeField] float barRowHeight = 16f;
        [SerializeField] float rewardRowHeight = 18f;
        [SerializeField] float innerGap = 4f;
        [Tooltip("Đệm trên + dưới + viền 2 bên.")]
        [SerializeField] float verticalChrome = 22f;
        [SerializeField] float minHeight = 80f;

        public MissionData Data { get; private set; }
        public Button ClaimButton => claimButton;
        public Button PlayButton => playButton;

        float enterT = -1f, enterDelay, pulseT;
        float doneAlpha = 1f;

        public void Bind(MissionData m, Action<MissionData> onClaim, Action onPlay)
        {
            Data = m;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaim(m));
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => onPlay());
            icon.sprite = m.icon;
            title.text = Loc.T(m.title);
            Refresh();
        }

        public void Refresh()
        {
            var m = Data;
            if (m == null) return;
            int cur = Progression.Progress(m);
            bool full = Progression.IsComplete(m), done = Progression.IsClaimed(m);
            bool canClaim = full && !done;

            barFill.anchorMax = new Vector2(m.goal > 0 ? Mathf.Clamp01(cur / (float)m.goal) : 1f, 1f);
            barFillImage.sprite = full ? fillGreen : fillBlue;
            progress.text = UiUtil.Thousands(cur) + "/" + UiUtil.Thousands(m.goal);
            FitBar();
            reward.text = "+" + UiUtil.Thousands(m.reward) + (m.points > 0 ? "<color=#5E4E3A>" + Loc.F(" · +{0} điểm", m.points) + "</color>" : "");

            background.sprite = canClaim ? readyBackground : normalBackground;
            claimButton.gameObject.SetActive(canClaim);
            playButton.gameObject.SetActive(!full && !m.comingSoon);
            soonMark.SetActive(!full && m.comingSoon);
            doneMark.SetActive(done);
            doneAlpha = done ? 0.7f : 1f;
            if (enterT < 0f) group.alpha = doneAlpha;
            if (!canClaim) claimButton.transform.localScale = Vector3.one;
            Relayout();
        }

        /// <summary>Chiều cao theo số dòng tên; khối chữ (tên, thanh, thưởng) căn giữa theo chiều dọc.</summary>
        void Relayout()
        {
            var gen = new TextGenerator();
            float w = titleRect.rect.width > 1f ? titleRect.rect.width : 176f * PxToUnits;
            gen.Populate(title.text, title.GetGenerationSettings(new Vector2(w, 0f)));
            int lines = Mathf.Clamp(gen.lineCount, 1, 3);
            float titleH = lines * titleLineHeight;
            float block = titleH + innerGap + barRowHeight + innerGap + rewardRowHeight;
            layout.preferredHeight = Mathf.Max(minHeight, block + verticalChrome) * PxToUnits;

            float top = block / 2f;
            Place(titleRect, top - titleH / 2f, titleH);
            Place(barRow, top - titleH - innerGap - barRowHeight / 2f, barRowHeight);
            Place(rewardRow, -top + rewardRowHeight / 2f, rewardRowHeight);
        }

        /// <summary>
        /// Thanh tiến độ dài cố định cho mọi dòng: chừa chỗ cho số dài nhất có thể ("8.888/8.888", chữ số rộng nhất),
        /// số căn phải ở mép phải khối chữ (cách nút 6 pt), thanh chạy từ mép trái tới cách vùng số 6 pt.
        /// </summary>
        void FitBar()
        {
            var bar = barFill.parent != null ? barFill.parent.parent as RectTransform : null;
            if (bar == null) return;
            float rowW = barRow.rect.width > 1f ? barRow.rect.width / PxToUnits : 176f;
            const float rightMargin = 6f, gap = 6f;
            var gen = new TextGenerator();
            float textW = gen.GetPreferredWidth("8.888/8.888", progress.GetGenerationSettings(Vector2.zero)) / progress.pixelsPerUnit / PxToUnits;
            float left = -rowW / 2f, right = rowW / 2f - rightMargin - textW - gap;
            float w = Mathf.Max(24f, right - left);
            bar.sizeDelta = new Vector2(w * PxToUnits, bar.sizeDelta.y);
            bar.anchoredPosition = new Vector2((left + w / 2f) * PxToUnits, bar.anchoredPosition.y);
            var pr = progress.rectTransform;
            pr.sizeDelta = new Vector2(Mathf.Max(textW, 1f) * PxToUnits + 2f, pr.sizeDelta.y);
            pr.anchoredPosition = new Vector2((rowW / 2f - rightMargin - textW / 2f) * PxToUnits, pr.anchoredPosition.y);
        }

        static void Place(RectTransform rt, float centerY, float height)
        {
            var p = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(p.x, centerY * PxToUnits);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height * PxToUnits);
        }

        /// <summary>.row: trượt từ phải 30 px + mờ vào 0,4 s cubic-bezier(.2,.9,.3,1.2), trễ theo thứ tự dòng.</summary>
        public void PlayEnter(float delay)
        {
            enterT = 0f;
            enterDelay = delay;
            ApplyEnter(0f);
        }

        static readonly Bezier EnterEase = new Bezier(0.2f, 0.9f, 0.3f, 1.2f);

        void ApplyEnter(float t)
        {
            float p = Mathf.Clamp01((t - enterDelay) / 0.4f);
            float e = EnterEase.Evaluate(p);
            visual.anchoredPosition = new Vector2((1f - e) * 30f * PxToUnits, 0f);
            group.alpha = Mathf.Clamp01(p) * doneAlpha; // opacity tuyến tính theo tiến độ để không vượt 1
            if (p >= 1f) enterT = -1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (enterT >= 0f)
            {
                enterT += dt;
                ApplyEnter(enterT);
            }
            // .ready: scale 1 → 1,07, 0,8 s ease-in-out, qua lại
            if (claimButton.gameObject.activeSelf)
            {
                pulseT += dt;
                float c = pulseT % 1.6f / 0.8f;
                float u = Bezier.EaseInOut.Evaluate(c < 1f ? c : 2f - c);
                claimButton.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.07f, u);
            }
        }
    }
}
