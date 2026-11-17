using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Màn Nhiệm vụ (Docs/DesignSource/Missions25D.dc.html): 3 tab Hằng ngày / Hằng tuần / Thành tựu, khung Điểm nhiệm vụ
    /// với 3 rương theo mốc, đồng hồ "Làm mới" tới 00:00, danh sách nhiệm vụ cuộn. Nút Chơi mở trang Chọn map.
    /// </summary>
    public class MissionsScreen : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [Serializable]
        public struct ChestSlot
        {
            public Button button;
            public RectTransform icon;
            public Image iconImage;
            public Image glow;
            public Image labelBackground;
            public Text label;
        }

        [SerializeField] ScreenRouter router;
        [SerializeField] CanvasGroup mapSelectScreen;
        [SerializeField] ProgressionConfig config;
        [SerializeField] Button backButton;

        [Header("Xu")]
        [SerializeField] Text coinsText;
        [SerializeField] RectTransform coinChip;

        [Header("Tab")]
        [SerializeField] Image[] tabBackgrounds;
        [SerializeField] Text[] tabLabels;
        [SerializeField] Button[] tabButtons;
        [SerializeField] Color tabTextSelected = new Color32(0x3A, 0x24, 0x12, 0xFF);
        [SerializeField] Color tabTextNormal = new Color32(0x5E, 0x4E, 0x3A, 0xFF);

        [Header("Điểm nhiệm vụ")]
        [SerializeField] Text pointsText;
        [SerializeField] Text resetText;
        [SerializeField] RectTransform pointsFill;
        [SerializeField] ChestSlot[] chests;
        [SerializeField] Sprite chestLabelLocked;
        [SerializeField] Sprite chestLabelReady;
        [SerializeField] Sprite chestLabelOpened;
        [SerializeField] Color chestOpenedTint = new Color(0.75f, 0.72f, 0.68f, 0.6f);

        [Header("Bong bóng phần thưởng rương")]
        [Tooltip("Chạm rương chưa đủ điểm / đã mở: hiện số xu trong rương phía trên rương. Mở rương: hiện \"+N xu\".")]
        [SerializeField] RectTransform tipRoot;
        [SerializeField] CanvasGroup tipGroup;
        [SerializeField] RectTransform tipArrow;
        [SerializeField] Text tipTitle;
        [SerializeField] Text tipAmount;
        [Tooltip("Mép trái / phải tối đa của tâm bong bóng so với tâm màn (px thiết kế).")]
        [SerializeField] float tipMaxOffset = 119f;
        [SerializeField] float tipSeconds = 2.5f;

        [Header("Danh sách")]
        [SerializeField] MissionRow[] rows;
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform listContent;

        MissionPeriod tab = MissionPeriod.Daily;
        int shownCoins = -1, shownSecond = -1;
        float bumpT = -1f, chestT, tipT = -1f;
        int tipIndex = -1;
        readonly List<MissionData> list = new List<MissionData>();

        void Awake()
        {
            backButton.onClick.AddListener(() => router.Back());
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var p = (MissionPeriod)i;
                tabButtons[i].onClick.AddListener(() => { AudioManager.Play(Sfx.Click); SelectTab(p); });
            }
            for (int i = 0; i < chests.Length; i++)
            {
                int index = i;
                chests[i].button.onClick.AddListener(() => OpenChest(index));
            }
        }

        void OnEnable()
        {
            if (config == null) return;
            Progression.Refresh(config);
            SelectTab(tab);
        }

        void SelectTab(MissionPeriod p)
        {
            tab = p;
            HideTip(); // đổi tab thì tắt bong bóng rương
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                bool on = i == (int)p;
                tabBackgrounds[i].enabled = on;
                tabLabels[i].color = on ? tabTextSelected : tabTextNormal;
            }
            list.Clear();
            list.AddRange(Progression.Missions(config, p));
            for (int i = 0; i < rows.Length; i++)
            {
                bool show = i < list.Count;
                rows[i].gameObject.SetActive(show);
                if (!show) continue;
                rows[i].Bind(list[i], Claim, () => router.Show(mapSelectScreen));
                rows[i].PlayEnter(i * 0.06f);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            RefreshPoints();
        }

        void Claim(MissionData m)
        {
            if (!Progression.Claim(config, m)) return;
            AudioManager.Play(Sfx.Reward);
            bumpT = 0f;
            foreach (var r in rows) if (r.gameObject.activeSelf) r.Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            RefreshPoints();
        }

        /// <summary>Rương đủ điểm thì mở và báo "+N xu"; rương chưa đủ / đã mở thì chỉ báo trong rương có bao nhiêu xu.</summary>
        void OpenChest(int index)
        {
            int pts = config.chestPoints[index];
            int coins = index < config.chestRewards.Length ? config.chestRewards[index] : 0;
            var state = Progression.Chest(config, index);
            if (state == Progression.ChestState.Ready && Progression.OpenChest(config, index))
            {
                AudioManager.Play(Sfx.Reward);
                bumpT = 0f;
                RefreshPoints();
                ShowTip(index, Loc.T("Đã mở rương!"), Loc.F("+{0} xu", UiUtil.Thousands(coins)));
                return;
            }
            if (tipT >= 0f && tipIndex == index) { tipT = tipSeconds; return; } // chạm lại để ẩn
            string title = state == Progression.ChestState.Opened ? Loc.T("Đã nhận hôm nay") : Loc.F("Cần {0} điểm", pts);
            ShowTip(index, title, Loc.F("{0} xu", UiUtil.Thousands(coins)));
        }

        void HideTip()
        {
            tipT = -1f;
            if (tipRoot != null) tipRoot.gameObject.SetActive(false);
        }

        void ShowTip(int index, string title, string amount)
        {
            if (tipRoot == null) return;
            tipIndex = index;
            tipTitle.text = title;
            tipAmount.text = amount;
            float chestX = ((RectTransform)chests[index].button.transform).anchoredPosition.x;
            float limit = tipMaxOffset * PxToUnits;
            float x = Mathf.Clamp(chestX, -limit, limit);
            tipRoot.anchoredPosition = new Vector2(x, tipRoot.anchoredPosition.y);
            tipArrow.anchoredPosition = new Vector2(chestX - x, tipArrow.anchoredPosition.y);
            tipRoot.gameObject.SetActive(true);
            tipRoot.SetAsLastSibling();
            tipT = 0f;
            ApplyTip();
        }

        /// <summary>Hiện: scale 0,8 → 1 + mờ vào 0,15 s; giữ tipSeconds; mờ ra 0,15 s.</summary>
        void ApplyTip()
        {
            const float fade = 0.15f;
            float a, s = 1f;
            if (tipT < fade) { float e = Bezier.EaseOut.Evaluate(tipT / fade); a = e; s = Mathf.Lerp(0.8f, 1f, e); }
            else if (tipT < tipSeconds) a = 1f;
            else a = 1f - Mathf.Clamp01((tipT - tipSeconds) / fade);
            tipGroup.alpha = a;
            tipRoot.localScale = Vector3.one * s;
            if (tipT >= tipSeconds + fade)
            {
                tipT = -1f;
                tipRoot.gameObject.SetActive(false);
            }
        }

        void RefreshPoints()
        {
            int pts = SaveSystem.Data.missionPoints;
            pointsText.text = pts + "/" + config.maxMissionPoints;
            pointsFill.anchorMax = new Vector2(Mathf.Clamp01(pts / (float)Mathf.Max(1, config.maxMissionPoints)), 1f);
            for (int i = 0; i < chests.Length && i < config.chestPoints.Length; i++)
            {
                var c = chests[i];
                var state = Progression.Chest(config, i);
                c.label.text = state == Progression.ChestState.Opened ? Loc.T("Đã mở") : config.chestPoints[i].ToString();
                c.labelBackground.sprite = state == Progression.ChestState.Ready ? chestLabelReady
                    : state == Progression.ChestState.Opened ? chestLabelOpened : chestLabelLocked;
                c.iconImage.color = state == Progression.ChestState.Opened ? chestOpenedTint : Color.white;
                if (c.glow != null) c.glow.gameObject.SetActive(state == Progression.ChestState.Ready);
                c.button.interactable = true; // rương nào cũng chạm được: mở, hoặc xem phần thưởng
                if (state != Progression.ChestState.Ready)
                {
                    c.icon.localRotation = Quaternion.identity;
                    c.icon.localScale = Vector3.one;
                }
            }
        }

        void Update()
        {
            if (config == null) return;
            float dt = Time.unscaledDeltaTime;
            int coins = SaveSystem.Data.coins;
            if (coins != shownCoins)
            {
                shownCoins = coins;
                coinsText.text = UiUtil.Thousands(coins);
            }

            var left = Progression.UntilMidnight;
            int sec = (int)left.TotalSeconds;
            if (sec != shownSecond)
            {
                shownSecond = sec;
                resetText.text = Loc.F("Làm mới\n{0}", Progression.Clock(left));
                if (sec <= 0) { Progression.Refresh(config); SelectTab(tab); } // qua 00:00 khi đang mở màn
            }

            if (tipT >= 0f) { tipT += dt; ApplyTip(); }

            // Ô xu nảy khi nhận: scale 1 → 1,18 (40%) → 1, 0,35 s ease-out
            if (bumpT >= 0f)
            {
                bumpT += dt;
                float p = Mathf.Clamp01(bumpT / 0.35f), e = Bezier.EaseOut.Evaluate(p);
                coinChip.localScale = Vector3.one * (e < 0.4f ? Mathf.Lerp(1f, 1.18f, e / 0.4f) : Mathf.Lerp(1.18f, 1f, (e - 0.4f) / 0.6f));
                if (p >= 1f) bumpT = -1f;
            }

            // .chestReady: 1,2 s — 70% đứng yên, 76% −10° x1,08, 84% 9° x1,08, 92% −5°, ease-in-out; gốc 50% 90%
            chestT += dt;
            float q = chestT % 1.2f / 1.2f;
            float rot = 0f, scale = 1f;
            if (q > 0.7f)
            {
                if (q < 0.76f) { float u = Bezier.EaseInOut.Evaluate((q - 0.7f) / 0.06f); rot = -10f * u; scale = 1f + 0.08f * u; }
                else if (q < 0.84f) { float u = Bezier.EaseInOut.Evaluate((q - 0.76f) / 0.08f); rot = Mathf.Lerp(-10f, 9f, u); scale = 1.08f; }
                else if (q < 0.92f) { float u = Bezier.EaseInOut.Evaluate((q - 0.84f) / 0.08f); rot = Mathf.Lerp(9f, -5f, u); scale = Mathf.Lerp(1.08f, 1f, u); }
                else { float u = Bezier.EaseInOut.Evaluate((q - 0.92f) / 0.08f); rot = Mathf.Lerp(-5f, 0f, u); }
            }
            for (int i = 0; i < chests.Length && i < config.chestPoints.Length; i++)
            {
                if (Progression.Chest(config, i) != Progression.ChestState.Ready) continue;
                chests[i].icon.localRotation = Quaternion.Euler(0f, 0f, -rot); // CSS xoay dương = cùng chiều kim đồng hồ
                chests[i].icon.localScale = Vector3.one * scale;
            }
        }
    }
}
