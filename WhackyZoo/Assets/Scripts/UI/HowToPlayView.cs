using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Màn Cách chơi: hiện mỗi khi vào map trên nền màu riêng của map, liệt kê thú/thưởng/vật cản,
    /// tự đóng sau GameConfig.howToSeconds hoặc khi bấm Bỏ qua (mờ dần để lộ màn chơi). Đồng hồ ván chưa chạy.
    /// </summary>
    public class HowToPlayView : MonoBehaviour
    {
        [Serializable]
        public struct CritterSlot
        {
            public GameObject root;
            public Image icon;
            public Text name;
            public Text points;
        }

        [Serializable]
        public struct BoosterSlot
        {
            public GameObject root;
            public Image icon;
            public Text name;
            public Text hint;
        }

        [Header("Nền")]
        [Tooltip("Lớp nền đặc phủ kín màn chơi (màu mép dưới).")]
        [SerializeField] Image background;
        [Tooltip("Dải chuyển màu đậm dần lên mép trên.")]
        [SerializeField] Image backgroundShade;
        [SerializeField] CanvasGroup group;
        [SerializeField] float closeSeconds = 0.3f;

        [Header("Tiêu đề")]
        [SerializeField] Image mapChip;
        [SerializeField] Text mapChipText;

        [Header("Thú thường (tối đa số ô có sẵn)")]
        [SerializeField] CritterSlot[] normalSlots;

        [Header("Thưởng và vật cản")]
        [SerializeField] Image bonusIcon;
        [SerializeField] Text bonusName;
        [SerializeField] Text bonusPoints;
        [SerializeField] Text bonusHint;
        [SerializeField] Image obstacleIcon;
        [SerializeField] Text obstacleName;
        [SerializeField] Text obstaclePoints;
        [SerializeField] Text obstacleHint;

        [Header("Vật phẩm hỗ trợ (khối VẬT PHẨM HỖ TRỢ, lưới 2 cột)")]
        [SerializeField] BoosterCatalog boosters;
        [SerializeField] BoosterSlot[] boosterSlots;

        [Header("Dòng thưởng / vật cản tự giãn khi chip điểm xuống dòng")]
        [Tooltip("Khối chữ (tên + chip + gợi ý) của thưởng và vật cản, tự cao theo nội dung.")]
        [SerializeField] RectTransform[] specialInfos;
        [Tooltip("Gốc khối thưởng / vật cản (icon + chữ): dịch xuống nửa phần giãn để vẫn ở giữa đoạn.")]
        [SerializeField] RectTransform[] specialBlocks;
        [Tooltip("Mọi thứ phía dưới đoạn này (đường kẻ dưới, chú thích, tiến trình): dịch xuống đúng phần giãn.")]
        [SerializeField] RectTransform[] belowSpecial;
        [Tooltip("Thẻ và bóng của thẻ: cao thêm phần giãn, giữ nguyên mép trên.")]
        [SerializeField] RectTransform[] growWithCard;
        [Tooltip("Gốc toàn bộ nội dung: dịch lên nửa phần giãn để cả khối vẫn căn giữa màn hình.")]
        [SerializeField] RectTransform content;
        [Tooltip("Khoảng giữa 2 đường kẻ khi chưa giãn (px thiết kế).")]
        [SerializeField] float specialSectionHeight = 71.6f;
        [Tooltip("Lề trên + dưới tối thiểu của khối chữ trong đoạn (px thiết kế, mỗi bên).")]
        [SerializeField] float specialPadding = 12f;

        [Header("Tiến trình")]
        [SerializeField] Text progressText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Button skipButton;

        const string ProgressFormat = "Chuẩn bị vào màn… {0}%";
        const float FadeInSeconds = 0.35f;
        const float PxToUnits = 3f;

        Vector2[] blockBase, belowBase, growPosBase, growSizeBase;
        Vector2 contentBase;
        bool basesStored;

        float duration;
        float elapsed = -1f;
        float closing = -1f;
        int shownPercent = -1;
        Action onDone;

        void Awake()
        {
            skipButton.onClick.AddListener(Finish);
        }

        public void Show(MapData map, float seconds, Action done)
        {
            Fill(map);
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            closing = -1f;
            shownPercent = -1;
            onDone = done;
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = true;
            }
            FitSpecialSection();
            SetProgress(0f);
        }

        public void Hide()
        {
            elapsed = -1f;
            closing = -1f;
            onDone = null;
            gameObject.SetActive(false);
        }

        public void Fill(MapData map)
        {
            if (map == null) return;

            if (background != null) background.color = map.howToBottom;
            if (backgroundShade != null) backgroundShade.color = map.howToTop;

            mapChipText.text = Loc.T(map.displayName);
            UiUtil.ApplyChip(mapChip, mapChipText, map.mapChip);

            for (int i = 0; i < normalSlots.Length; i++)
            {
                var slot = normalSlots[i];
                var critter = map.normalCritters != null && i < map.normalCritters.Length ? map.normalCritters[i] : null;
                slot.root.SetActive(critter != null);
                if (critter == null) continue;
                slot.icon.sprite = critter.sprite;
                slot.name.text = Loc.T(critter.ShortName);
                slot.points.text = UiUtil.Signed(critter.points);
            }

            for (int i = 0; boosterSlots != null && i < boosterSlots.Length; i++)
            {
                var slot = boosterSlots[i];
                var b = boosters != null && boosters.boosters != null && i < boosters.boosters.Length ? boosters.boosters[i] : null;
                slot.root.SetActive(b != null);
                if (b == null) continue;
                slot.icon.sprite = b.icon;
                slot.name.text = Loc.T(b.guideName);
                slot.hint.text = Loc.T(b.guideHint);
            }

            FillSpecial(map.bonus, bonusIcon, bonusName, bonusPoints, bonusHint);
            FillSpecial(map.obstacle, obstacleIcon, obstacleName, obstaclePoints, obstacleHint);
        }

        static void FillSpecial(CritterData critter, Image icon, Text name, Text points, Text hint)
        {
            var root = icon.transform.parent.gameObject;
            root.SetActive(critter != null);
            if (critter == null) return;
            icon.sprite = critter.sprite;
            name.text = Loc.T(critter.displayName);
            points.text = UiUtil.Signed(critter.points);
            hint.text = Loc.T(critter.hint);
        }

        /// <summary>
        /// Như bản thiết kế (flex-wrap), tên dài làm chip điểm xuống dòng → khối chữ cao hơn đoạn giữa 2 đường kẻ.
        /// Khi đó giãn đoạn này: phần dưới dịch xuống, thẻ cao thêm, cả khối dịch lên nửa phần thêm.
        /// </summary>
        void FitSpecialSection()
        {
            if (specialInfos == null || specialInfos.Length == 0) return;
            if (!basesStored)
            {
                blockBase = Positions(specialBlocks);
                belowBase = Positions(belowSpecial);
                growPosBase = Positions(growWithCard);
                growSizeBase = new Vector2[growWithCard.Length];
                for (int i = 0; i < growWithCard.Length; i++) growSizeBase[i] = growWithCard[i].sizeDelta;
                if (content != null) contentBase = content.anchoredPosition;
                basesStored = true;
            }

            float needed = 0f;
            foreach (var info in specialInfos)
            {
                if (info == null || !info.gameObject.activeInHierarchy) continue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(info);
                needed = Mathf.Max(needed, info.rect.height / PxToUnits);
            }
            float extra = Mathf.Max(0f, needed + 2f * specialPadding - specialSectionHeight) * PxToUnits;

            for (int i = 0; i < specialBlocks.Length; i++) specialBlocks[i].anchoredPosition = blockBase[i] + new Vector2(0f, -extra / 2f);
            for (int i = 0; i < belowSpecial.Length; i++) belowSpecial[i].anchoredPosition = belowBase[i] + new Vector2(0f, -extra);
            for (int i = 0; i < growWithCard.Length; i++)
            {
                var rt = growWithCard[i];
                rt.sizeDelta = growSizeBase[i] + new Vector2(0f, extra);
                rt.anchoredPosition = growPosBase[i] + new Vector2(0f, -extra * (1f - rt.pivot.y)); // giữ mép trên
            }
            if (content != null) content.anchoredPosition = contentBase + new Vector2(0f, extra / 2f);
        }

        static Vector2[] Positions(RectTransform[] rts)
        {
            var p = new Vector2[rts.Length];
            for (int i = 0; i < rts.Length; i++) p[i] = rts[i].anchoredPosition;
            return p;
        }

        void Update()
        {
            if (closing >= 0f)
            {
                closing += Time.unscaledDeltaTime;
                float c = Mathf.Clamp01(closing / closeSeconds);
                if (group != null) group.alpha = 1f - Bezier.EaseIn.Evaluate(c);
                if (c >= 1f)
                {
                    var done = onDone;
                    Hide();
                    done?.Invoke();
                }
                return;
            }

            if (elapsed < 0f) return;
            elapsed += Time.unscaledDeltaTime;

            // .guideIn: opacity 0 → 1 trong 0,35 s, ease-out
            if (group != null && elapsed <= FadeInSeconds + Time.unscaledDeltaTime)
                group.alpha = Bezier.EaseOut.Evaluate(Mathf.Clamp01(elapsed / FadeInSeconds));

            SetProgress(elapsed / duration);
            if (elapsed >= duration) Finish();
        }

        void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            progressFill.anchorMax = new Vector2(t, 1f);
            int percent = Mathf.FloorToInt(t * 100f);
            if (percent != shownPercent)
            {
                shownPercent = percent;
                progressText.text = Loc.F(ProgressFormat, percent);
            }
        }

        /// <summary>Đóng màn: mờ dần lộ màn chơi rồi mới bắt đầu ván (đồng hồ chạy sau khi mờ xong).</summary>
        void Finish()
        {
            if (elapsed < 0f || closing >= 0f) return;
            SetProgress(1f);
            closing = 0f;
            if (group != null) group.blocksRaycasts = false;
        }
    }
}
