using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Popup Tạm dừng: Tiếp tục, Chơi lại, Về trang chủ.</summary>
    public class PausePopup : PopupView
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button homeButton;

        public void Init(GameController controller)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(controller.Resume);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(controller.Restart);
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(controller.GoHome);
        }

        public override void Show()
        {
            // đặt trước khi mở để hiệu ứng hiện lần lượt giữ đúng độ mờ của nút chưa dùng được
            SetAvailable(homeButton, HomeAvailable);
            base.Show();
        }
    }
}
