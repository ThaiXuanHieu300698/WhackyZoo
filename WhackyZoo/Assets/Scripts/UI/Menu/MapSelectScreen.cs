using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Trang Chọn map: một thẻ cho mỗi map trong MapCatalog, cuối danh sách là thẻ "Sắp ra mắt".
    /// Mỗi lần mở trang, các thẻ trượt vào lần lượt; bấm thẻ thì thẻ lún xuống rồi mới vào màn chơi.
    /// </summary>
    public class MapSelectScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] MapCatalog catalog;
        [SerializeField] Button backButton;
        [SerializeField] RectTransform content;
        [SerializeField] MapCard cardTemplate;
        [Tooltip("Thẻ \"Map mới · Sắp ra mắt\", luôn nằm cuối danh sách.")]
        [SerializeField] MapCardAnimator comingSoonCard;
        [SerializeField] ScrollRect scroll;
        [Tooltip("Độ trễ trượt vào của từng thẻ theo thứ tự (Animations.md: 0, 0,1, 0,2, 0,3, 0,35, 0,45 s); thẻ dư dùng bước cuối.")]
        [SerializeField] float[] introDelays = { 0f, 0.1f, 0.2f, 0.3f, 0.35f, 0.45f };

        readonly List<MapCardAnimator> animators = new List<MapCardAnimator>();
        bool loading;

        void Awake()
        {
            backButton.onClick.AddListener(router.Back);
            cardTemplate.gameObject.SetActive(false);
            foreach (var map in catalog.maps)
            {
                if (map == null) continue;
                var card = Instantiate(cardTemplate, content);
                card.gameObject.SetActive(true);
                card.Bind(map, Play);
                animators.Add(card.Animator);
            }
            comingSoonCard.transform.SetAsLastSibling();
            animators.Add(comingSoonCard);
        }

        void OnEnable()
        {
            loading = false;
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            for (int i = 0; i < animators.Count; i++) animators[i].PlayIntro(IntroDelay(i));
        }

        float IntroDelay(int index)
        {
            if (introDelays == null || introDelays.Length == 0) return 0f;
            if (index < introDelays.Length) return introDelays[index];
            float step = introDelays.Length > 1 ? introDelays[^1] - introDelays[^2] : 0.1f;
            return introDelays[^1] + step * (index - introDelays.Length + 1);
        }

        void Play(MapData map, MapCard card)
        {
            if (loading) return;
            loading = true;
            card.Animator.HoldPressed();
            StartCoroutine(LoadAfterPress(map, card.Animator.PressDuration));
        }

        IEnumerator LoadAfterPress(MapData map, float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            GameSession.SelectedMap = map;
            SaveSystem.Flush();
            SceneManager.LoadScene(SceneNames.Game);
        }
    }
}
