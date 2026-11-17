using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Dựng và chạy hiệu ứng nền theo khung hình (CardFxElement): thẻ ở trang Chọn map (MapData.cardEffects — nằm trong khung
    /// ảnh nền thẻ, trên ảnh nền và dưới 4 thú) và nền màn chơi (MapData.ambientFx — đàn dơi, dải gió…).
    /// Lớp chứa chưa gán sẵn thì tự tạo (phủ kín RectTransform của component này).
    /// Đơn vị px thiết kế; 1 px = 3 đơn vị canvas. Chạy theo thời gian thật (không phụ thuộc Time.timeScale).
    /// </summary>
    public class CardFxView : MonoBehaviour
    {
        const float PxToUnits = 3f;

        [Tooltip("Lớp phủ kín ảnh nền thẻ, chứa hiệu ứng clip = None.")]
        [SerializeField] RectTransform plainLayer;
        [Tooltip("Image + Mask phủ kín ảnh nền: hiệu ứng clip = MapMask (sprite lấy từ MapData.cardSkyMask).")]
        [SerializeField] Image mapMask;
        [Tooltip("RectTransform + RectMask2D neo mép trên: hiệu ứng clip = SkyBand.")]
        [SerializeField] RectMask2D skyBand;
        [Tooltip("Phần dải trời nhô lên trên mép ảnh (px) để mép mờ của RectMask2D không chạm vùng nhìn thấy.")]
        [SerializeField] float skyBandOverhang = 20f;
        [Tooltip("Độ mờ mép dưới của dải trời (px).")]
        [SerializeField] float skyBandSoftness = 10f;

        class Runner
        {
            public CardFxElement data;
            public RectTransform rt;
            public Image image;
            public Vector2 basePos;
            public Sprite[] sequence;
        }

        readonly List<Runner> runners = new List<Runner>();
        readonly List<GameObject> spawned = new List<GameObject>();
        float time;

        public void Build(MapData map) =>
            Build(map != null ? map.cardEffects : null, map != null ? map.cardSkyMask : null, map != null ? map.cardSkyBottom : 0f);

        public void Build(CardFxElement[] fx, Sprite maskSprite, float skyBottom)
        {
            EnsureLayers();
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            runners.Clear();
            time = 0f;

            bool useMask = false, useSky = false;
            if (fx != null)
            {
                foreach (var e in fx)
                {
                    if (e == null) continue;
                    if (e.parent < 0 && e.clip == FxClip.MapMask) useMask = true;
                    if (e.parent < 0 && e.clip == FxClip.SkyBand) useSky = true;
                }
            }
            useMask &= maskSprite != null;
            if (mapMask != null)
            {
                mapMask.gameObject.SetActive(useMask);
                if (useMask) mapMask.sprite = maskSprite;
            }
            if (skyBand != null)
            {
                skyBand.gameObject.SetActive(useSky);
                if (useSky)
                {
                    var band = skyBand.rectTransform;
                    band.anchorMin = new Vector2(0f, 1f);
                    band.anchorMax = new Vector2(1f, 1f);
                    band.pivot = new Vector2(0.5f, 1f);
                    band.anchoredPosition = new Vector2(0f, skyBandOverhang * PxToUnits);
                    band.sizeDelta = new Vector2(0f, (skyBandOverhang + skyBottom) * PxToUnits);
                    int soft = Mathf.RoundToInt(skyBandSoftness * PxToUnits);
                    skyBand.softness = new Vector2Int(0, soft);
                }
            }
            if (fx == null) return;

            var created = new RectTransform[fx.Length];
            for (int i = 0; i < fx.Length; i++)
            {
                var e = fx[i];
                if (e == null) continue;

                RectTransform container;
                float yShift = 0f; // dải trời bắt đầu cao hơn mép ảnh skyBandOverhang px
                if (e.parent >= 0 && e.parent < i && created[e.parent] != null) container = created[e.parent];
                else if (e.clip == FxClip.MapMask && useMask) container = mapMask.rectTransform;
                else if (e.clip == FxClip.SkyBand && useSky) { container = skyBand.rectTransform; yShift = skyBandOverhang; }
                else container = plainLayer;

                var go = new GameObject(string.IsNullOrEmpty(e.name) ? $"Fx{i}" : e.name, typeof(RectTransform));
                go.layer = gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(container, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = e.pivot;
                rt.sizeDelta = new Vector2(e.width, e.height) * PxToUnits;
                var basePos = new Vector2(e.left + e.width * e.pivot.x, -(e.top + yShift + e.height * (1f - e.pivot.y))) * PxToUnits;
                rt.anchoredPosition = basePos;
                if (e.parent < 0 || e.parent >= i) spawned.Add(go);
                created[i] = rt;

                Image img = null;
                if (e.sprite != null)
                {
                    img = go.AddComponent<Image>();
                    img.sprite = e.sprite;
                    img.color = e.color;
                    img.raycastTarget = false;
                    if (e.fillHorizontal)
                    {
                        img.type = Image.Type.Filled;
                        img.fillMethod = Image.FillMethod.Horizontal;
                        img.fillOrigin = (int)Image.OriginHorizontal.Left;
                    }
                }

                Sprite[] seq = null;
                if (img != null && e.frames != null && e.frames.Length > 0)
                {
                    seq = new Sprite[e.frames.Length + 1];
                    seq[0] = e.sprite;
                    e.frames.CopyTo(seq, 1);
                }
                var r = new Runner { data = e, rt = rt, image = img, basePos = basePos, sequence = seq };
                runners.Add(r);
                Apply(r);
            }
        }

        void EnsureLayers()
        {
            if (plainLayer == null) plainLayer = NewLayer("Plain");
            if (mapMask == null)
            {
                var rt = NewLayer("MapMask");
                mapMask = rt.gameObject.AddComponent<Image>();
                mapMask.raycastTarget = false;
                rt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            }
            if (skyBand == null) skyBand = NewLayer("SkyBand").gameObject.AddComponent<RectMask2D>();
        }

        RectTransform NewLayer(string layerName)
        {
            var go = new GameObject(layerName, typeof(RectTransform));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        void Update()
        {
            if (runners.Count == 0) return;
            time += Time.unscaledDeltaTime;
            foreach (var r in runners) Apply(r);
        }

        void Apply(Runner r)
        {
            var e = r.data;
            float local = time - e.delay;
            Vector2 move = Vector2.zero;
            float alpha = e.restAlpha, rot = e.restRotation, fill = 1f;
            if (local >= 0f && e.period > 0f)
            {
                float u;
                if (e.alternate)
                {
                    float c = local % (2f * e.period) / e.period; // 0–2: đi rồi về
                    u = c < 1f ? c : 2f - c;
                }
                else u = local % e.period / e.period;

                var ease = ToBezier(e.ease);
                if (e.moveKeys != null && e.moveKeys.Length > 0) move = SampleMove(e.moveKeys, u, ease);
                alpha = e.alphaKeys != null && e.alphaKeys.Length > 0 ? Sample(e.alphaKeys, u, ease) : 1f;
                rot = e.rotateKeys != null && e.rotateKeys.Length > 0 ? Sample(e.rotateKeys, u, ease) : 0f;
                if (e.fillKeys != null && e.fillKeys.Length > 0) fill = Sample(e.fillKeys, u, ease);
            }
            else if (e.fillKeys != null && e.fillKeys.Length > 0) fill = e.fillKeys[0].y;

            r.rt.anchoredPosition = r.basePos + new Vector2(move.x, -move.y) * PxToUnits;
            r.rt.localRotation = Quaternion.Euler(0f, 0f, -rot);
            if (r.image != null)
            {
                var c = e.color;
                c.a *= alpha;
                r.image.color = c;
                if (e.fillHorizontal) r.image.fillAmount = fill;
                if (r.sequence != null)
                {
                    int f = Mathf.FloorToInt(time / Mathf.Max(0.01f, e.frameInterval)) % r.sequence.Length;
                    if (r.image.sprite != r.sequence[f]) r.image.sprite = r.sequence[f];
                }
            }
        }

        static Bezier ToBezier(FxEase e)
        {
            switch (e)
            {
                case FxEase.Ease: return Bezier.Ease;
                case FxEase.EaseIn: return Bezier.EaseIn;
                case FxEase.EaseOut: return Bezier.EaseOut;
                case FxEase.EaseInOut: return Bezier.EaseInOut;
                default: return Bezier.Linear;
            }
        }

        // Như CSS: hàm thời gian áp cho từng đoạn giữa hai khung liền nhau của thuộc tính đó
        static float Sample(Vector2[] k, float p, Bezier ease)
        {
            if (p <= k[0].x) return k[0].y;
            for (int i = 1; i < k.Length; i++)
            {
                if (p > k[i].x) continue;
                float span = k[i].x - k[i - 1].x;
                float t = span > 0f ? (p - k[i - 1].x) / span : 1f;
                return Mathf.LerpUnclamped(k[i - 1].y, k[i].y, ease.Evaluate(t));
            }
            return k[k.Length - 1].y;
        }

        static Vector2 SampleMove(Vector3[] k, float p, Bezier ease)
        {
            if (p <= k[0].x) return new Vector2(k[0].y, k[0].z);
            for (int i = 1; i < k.Length; i++)
            {
                if (p > k[i].x) continue;
                float span = k[i].x - k[i - 1].x;
                float t = ease.Evaluate(span > 0f ? (p - k[i - 1].x) / span : 1f);
                return new Vector2(Mathf.LerpUnclamped(k[i - 1].y, k[i].y, t), Mathf.LerpUnclamped(k[i - 1].z, k[i].z, t));
            }
            var last = k[k.Length - 1];
            return new Vector2(last.y, last.z);
        }
    }
}
