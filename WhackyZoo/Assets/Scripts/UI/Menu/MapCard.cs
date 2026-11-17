using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Thẻ một map ở trang Chọn map: ảnh nền thẻ → hiệu ứng nền (MapData.cardEffects) → 4 thú, tên kèm nhãn MỚI, nút Chọn.
    /// Bấm vào bất kỳ đâu trên thẻ để chọn (như thẻ &lt;a class="card"&gt; của bản thiết kế).
    /// </summary>
    [RequireComponent(typeof(MapCardAnimator))]
    public class MapCard : MonoBehaviour
    {
        [SerializeField] Image banner;
        [SerializeField] CardFxView effects;
        [SerializeField] Image[] animals;
        [SerializeField] Text nameText;
        [SerializeField] GameObject newBadge;
        [SerializeField] Button button;

        public MapCardAnimator Animator { get; private set; }

        void Awake() => Animator = GetComponent<MapCardAnimator>();

        MapData boundMap;

        // Tên map theo ngôn ngữ đang chọn (đổi ở Cài đặt rồi quay lại trang Chọn map)
        void OnEnable() { if (boundMap != null) nameText.text = Loc.T(boundMap.displayName); }

        public void Bind(MapData map, Action<MapData, MapCard> onSelect)
        {
            if (Animator == null) Animator = GetComponent<MapCardAnimator>();
            name = "Card_" + map.id;
            banner.sprite = map.banner != null ? map.banner : map.background;
            if (effects != null) effects.Build(map);
            for (int i = 0; i < animals.Length; i++)
            {
                var c = map.normalCritters != null && i < map.normalCritters.Length ? map.normalCritters[i] : null;
                animals[i].gameObject.SetActive(c != null);
                if (c != null) animals[i].sprite = c.sprite;
            }
            boundMap = map;
            nameText.text = Loc.T(map.displayName);
            newBadge.SetActive(map.isNew);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect(map, this));
        }
    }
}
