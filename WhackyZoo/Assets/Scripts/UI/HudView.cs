using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Thanh trên màn chơi: nút tạm dừng, đồng hồ + thanh giờ, điểm, chữ COMBO; dải chú thích điểm.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] Button pauseButton;
        [SerializeField] Text timeText;
        [SerializeField] RectTransform timeBarFill;
        [SerializeField] Image timeBarFillImage;
        [SerializeField] Text scoreText;
        [SerializeField] Text comboText;

        [Header("Chú thích điểm")]
        [SerializeField] Text legendNormal;
        [SerializeField] Text legendBonus;
        [SerializeField] Text legendObstacle;

        [Header("Màu")]
        [SerializeField] Color timeNormalColor = new Color32(0x3A, 0x24, 0x12, 0xFF);
        [SerializeField] Color timeLowColor = new Color32(0xE5, 0x39, 0x35, 0xFF);
        [SerializeField] Color barNormalColor = new Color32(0x4C, 0xC2, 0x5E, 0xFF);
        [SerializeField] Color barLowColor = new Color32(0xE5, 0x39, 0x35, 0xFF);
        [Tooltip("Dải màu thanh giờ (thiết kế: xanh #A6F09A → #2E9E44, ≤ 10 s đỏ #FF8A80 → #E53935). Trống thì dùng màu phẳng ở trên.")]
        [SerializeField] Sprite barNormalSprite;
        [SerializeField] Sprite barLowSprite;

        const float ComboPopDuration = 0.25f;
        float comboPopT = -1f;
        int shownSeconds = -1;
        bool shownLow;

        public void Init(GameController controller, MapData map)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(controller.Pause);
            SetLegend(map);
        }

        public void SetLegend(MapData map)
        {
            if (map == null) return;
            SetChip(legendNormal, map.normalChip, Loc.T(map.normalLabel) + " " + UiUtil.Signed(UiUtil.MinNormalPoints(map)), true);
            SetChip(legendBonus, map.bonusChip, map.bonus != null ? Loc.T(map.bonus.ShortName) + " " + UiUtil.Signed(map.bonus.points) : null, map.bonus != null);
            SetChip(legendObstacle, map.obstacleChip, map.obstacle != null ? Loc.T(map.obstacle.ShortName) + " " + UiUtil.Signed(map.obstacle.points) : null, map.obstacle != null);
        }

        static void SetChip(Text label, ChipStyle style, string text, bool visible)
        {
            if (label == null) return;
            var chip = label.transform.parent;
            chip.gameObject.SetActive(visible);
            if (!visible) return;
            label.text = text;
            UiUtil.ApplyChip(chip.GetComponent<Image>(), label, style);
        }

        public void SetTime(float left, float total, bool low)
        {
            int secs = Mathf.CeilToInt(left);
            if (secs != shownSeconds || low != shownLow)
            {
                shownSeconds = secs;
                shownLow = low;
                timeText.text = (secs / 60) + ":" + (secs % 60).ToString("00");
                timeText.color = low ? timeLowColor : timeNormalColor;
                if (barNormalSprite != null && barLowSprite != null)
                {
                    timeBarFillImage.sprite = low ? barLowSprite : barNormalSprite;
                    timeBarFillImage.color = Color.white;
                }
                else timeBarFillImage.color = low ? barLowColor : barNormalColor;
            }
            float pct = total > 0f ? Mathf.Clamp01(left / total) : 0f;
            timeBarFill.anchorMax = new Vector2(pct, 1f);
        }

        public void SetScore(int score) => scoreText.text = score.ToString();

        public void SetCombo(int combo, bool animate)
        {
            bool show = combo >= 2;
            comboText.gameObject.SetActive(show);
            comboText.text = "COMBO x" + combo;
            if (show && animate) comboPopT = 0f;
        }

        void Update()
        {
            UpdateLowPulse();
            if (comboPopT < 0f) return;
            comboPopT += Time.deltaTime;
            float p = comboPopT / ComboPopDuration;
            if (p >= 1f)
            {
                comboPopT = -1f;
                comboText.rectTransform.localScale = Vector3.one;
                return;
            }
            float s = p < 0.4f ? Mathf.Lerp(1f, 1.25f, p / 0.4f) : Mathf.Lerp(1.25f, 1f, (p - 0.4f) / 0.6f);
            comboText.rectTransform.localScale = Vector3.one * s;
        }

        // Sắp hết giờ: số đồng hồ đập nhịp 1 → 1,12 mỗi 0,6 giây (ease-in-out, qua lại)
        const float PulseHalf = 0.6f;
        float pulseT;

        void UpdateLowPulse()
        {
            if (!shownLow || shownSeconds <= 0)
            {
                pulseT = 0f;
                timeText.rectTransform.localScale = Vector3.one;
                return;
            }
            pulseT += Time.deltaTime;
            float phase = pulseT / PulseHalf % 2f;
            float u = phase < 1f ? phase : 2f - phase;
            timeText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, Bezier.EaseInOut.Evaluate(u));
        }
    }
}
