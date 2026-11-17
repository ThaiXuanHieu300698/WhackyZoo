using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>Cài đặt: thời gian mỗi ván, nhạc nền, hiệu ứng, rung, ngôn ngữ. Lưu khi rời màn hoặc app tạm dừng.</summary>
    public class SettingsScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] GameConfig config;
        [SerializeField] Button backButton;

        [Header("Thời gian mỗi ván (cùng thứ tự với GameConfig.roundSecondsOptions)")]
        [SerializeField] Button[] roundButtons;
        [SerializeField] Sprite selectedSprite;
        [SerializeField] Sprite normalSprite;

        [Header("Âm thanh")]
        [SerializeField] Slider musicSlider;
        [SerializeField] Text musicValue;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Text sfxValue;
        [SerializeField] ToggleSwitch vibrateSwitch;

        [Header("Ngôn ngữ")]
        [SerializeField] Button viButton;
        [SerializeField] Button enButton;

        SettingsData S => SaveSystem.Data.settings;

        void Awake()
        {
            backButton.onClick.AddListener(router.Back);
            for (int i = 0; i < roundButtons.Length; i++)
            {
                int index = i;
                roundButtons[i].onClick.AddListener(() => SelectRound(index));
            }
            musicSlider.onValueChanged.AddListener(v => { S.musicVolume = v; SaveSystem.MarkDirty(); RefreshAudio(); });
            sfxSlider.onValueChanged.AddListener(v => { S.sfxVolume = v; SaveSystem.MarkDirty(); RefreshAudio(); });
            vibrateSwitch.Changed += on =>
            {
                S.vibrate = on;
                SaveSystem.MarkDirty();
                if (on) Haptics.Tap(config.hitVibrationMs);
            };
            // Đổi ngôn ngữ: chữ trên màn đổi ngay (LocalizedText), các màn khác dịch lại khi mở
            viButton.onClick.AddListener(() => { Loc.SetLanguage("vi"); RefreshLanguage(); });
            enButton.onClick.AddListener(() => { Loc.SetLanguage("en"); RefreshLanguage(); });
        }

        void OnEnable()
        {
            musicSlider.SetValueWithoutNotify(S.musicVolume);
            sfxSlider.SetValueWithoutNotify(S.sfxVolume);
            vibrateSwitch.SetValue(S.vibrate, false);
            RefreshRound();
            RefreshAudio();
            RefreshLanguage();
        }

        void OnDisable() => SaveSystem.Flush();

        void SelectRound(int index)
        {
            S.roundSeconds = config.roundSecondsOptions[index];
            SaveSystem.MarkDirty();
            RefreshRound();
        }

        void RefreshRound()
        {
            int current = SaveSystem.RoundSeconds(config);
            for (int i = 0; i < roundButtons.Length; i++)
            {
                bool on = i < config.roundSecondsOptions.Length && config.roundSecondsOptions[i] == current;
                ((Image)roundButtons[i].targetGraphic).sprite = on ? selectedSprite : normalSprite;
            }
        }

        void RefreshAudio()
        {
            musicValue.text = Mathf.RoundToInt(S.musicVolume * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(S.sfxVolume * 100f) + "%";
        }

        void RefreshLanguage()
        {
            ((Image)viButton.targetGraphic).sprite = S.language == "vi" ? selectedSprite : normalSprite;
            ((Image)enButton.targetGraphic).sprite = S.language == "en" ? selectedSprite : normalSprite;
        }
    }
}
