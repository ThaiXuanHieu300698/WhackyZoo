using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Popup Hết giờ: điểm (đếm tăng dần), số thú đã đập, combo cao nhất, xu nhận được.</summary>
    public class RoundEndPopup : PopupView
    {
        [SerializeField] Text scoreValue;
        [SerializeField] Text hitsValue;
        [SerializeField] Text maxComboValue;
        [SerializeField] Text coinsValue;
        [SerializeField] Button doubleCoinsButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button homeButton;

        [Header("Điểm đếm tăng từ 0")]
        [SerializeField] float countDelay = 0.3f;
        [SerializeField] float countSeconds = 0.7f;

        int finalScore, shownScore = -1;
        float countT = -1f;

        public void Init(GameController controller)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(controller.Restart);
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(controller.GoHome);
        }

        int earnedCoins;

        public void Show(int score, int hits, int maxCombo, int coins)
        {
            finalScore = score;
            shownScore = -1;
            countT = 0f;
            SetScore(0);
            hitsValue.text = hits.ToString();
            maxComboValue.text = "x" + maxCombo;
            coinsValue.text = "+" + coins;
            earnedCoins = coins;
            // Đặt trạng thái nút trước khi mở để hiệu ứng hiện lần lượt giữ đúng độ mờ "sắp có".
            // Quảng cáo nhân đôi xu làm ở bước 6.
            SetAvailable(doubleCoinsButton, false);
            SetAvailable(homeButton, HomeAvailable);
            base.Show();
        }

        protected override void OnAnimate(float t)
        {
            if (countT < 0f) return;
            float q = Mathf.Clamp01((t - countDelay) / countSeconds);
            SetScore(Mathf.RoundToInt(finalScore * Bezier.EaseOut.Evaluate(q)));
            if (q >= 1f)
            {
                countT = -1f;
                if (earnedCoins > 0) AudioManager.Play(Sfx.Coin); // đếm điểm xong: "ting" xu
            }
        }

        protected override bool KeepAnimating => countT >= 0f;

        void SetScore(int v)
        {
            if (v == shownScore) return;
            shownScore = v;
            scoreValue.text = v.ToString();
        }
    }
}
