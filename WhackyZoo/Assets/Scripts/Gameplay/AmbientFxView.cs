using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Dựng và chạy các vật trang trí chuyển động trên nền màn chơi theo MapData.ambient
    /// (rong biển nghiêng, bong bóng sủi, tia sáng xoay… — Docs/Animations.md mục 3).
    /// Đặt làm con của ảnh nền; tự co theo ảnh nền (AspectRatioFitter Envelope) để toạ độ thiết kế luôn khớp tranh.
    /// Chạy theo thời gian thật nên nền vẫn sống khi tạm dừng, giống bản thiết kế.
    /// </summary>
    public class AmbientFxView : MonoBehaviour
    {
        const float PxToUnits = 3f;
        const float DesignW = 390f, DesignH = 844f;

        class Live
        {
            public AmbientElement data;
            public RectTransform rt;
            public Image image;
            public UISkew skew;
            public Vector2 basePos;
        }

        [Tooltip("Dùng khi không gắn với map (vd. mây Trang chủ): tự dựng lúc Start nếu có phần tử.")]
        [SerializeField] AmbientElement[] elements;

        readonly List<Live> items = new List<Live>();
        RectTransform rt, parentRt;
        float time;

        void Start()
        {
            if (rt == null && elements != null && elements.Length > 0) Build(elements);
        }

        public void Build(MapData map)
        {
            Build(map != null ? map.ambient : null);
            // Hiệu ứng theo khung hình (đàn dơi, dải gió…): lớp riêng nằm dưới cùng, sau các phần tử ambient (vd. đom đóm)
            if (map != null && map.ambientFx != null && map.ambientFx.Length > 0)
            {
                var layer = NewRect("PathFx", rt);
                layer.anchorMin = Vector2.zero;
                layer.anchorMax = Vector2.one;
                layer.offsetMin = layer.offsetMax = Vector2.zero;
                layer.SetAsFirstSibling();
                layer.gameObject.AddComponent<CardFxView>().Build(map.ambientFx, map.ambientFxMask, 0f);
            }
        }

        public void Build(AmbientElement[] list)
        {
            rt = (RectTransform)transform;
            parentRt = rt.parent as RectTransform;
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
            items.Clear();
            time = 0f;
            if (list == null) return;

            var clipLayers = new Dictionary<float, RectTransform>();
            foreach (var e in list)
            {
                if (e == null || e.sprite == null) continue;
                Transform parent = rt;
                if (e.clipBelowY > 0f)
                {
                    if (!clipLayers.TryGetValue(e.clipBelowY, out var clip))
                    {
                        clip = NewRect($"Clip_{e.clipBelowY}", rt);
                        clip.anchorMin = clip.anchorMax = new Vector2(0f, 1f);
                        clip.pivot = new Vector2(0f, 1f);
                        clip.anchoredPosition = Vector2.zero;
                        clip.sizeDelta = new Vector2(DesignW, e.clipBelowY) * PxToUnits;
                        clip.gameObject.AddComponent<RectMask2D>();
                        clipLayers[e.clipBelowY] = clip;
                    }
                    parent = clip;
                }

                var r = NewRect(string.IsNullOrEmpty(e.name) ? e.sprite.name : e.name, parent);
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f); // gốc trên-trái như CSS
                r.pivot = e.pivot;
                r.sizeDelta = e.size * PxToUnits;
                var basePos = new Vector2(e.position.x, -e.position.y) * PxToUnits;
                // vật trong lớp cắt: lớp đó cũng neo trên-trái nên toạ độ giữ nguyên
                r.anchoredPosition = basePos;
                var img = r.gameObject.AddComponent<Image>();
                img.sprite = e.sprite;
                img.color = e.color;
                img.raycastTarget = false;
                var live = new Live { data = e, rt = r, image = img, basePos = basePos };
                if (e.motion == AmbientMotion.Skew) live.skew = r.gameObject.AddComponent<UISkew>();
                items.Add(live);
                Animate(live, 0f);
            }
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            return r;
        }

        void Update()
        {
            if (rt == null) return;
            // Khung 390×844 thiết kế co theo ảnh nền (ảnh nền giữ tỉ lệ nên một hệ số cho cả hai trục)
            if (parentRt != null)
            {
                float s = parentRt.rect.height / (DesignH * PxToUnits);
                rt.localScale = new Vector3(s, s, 1f);
            }
            time += Time.unscaledDeltaTime;
            foreach (var live in items) Animate(live, time);
        }

        static void Animate(Live live, float time)
        {
            var e = live.data;
            float local = time - e.delay;
            switch (e.motion)
            {
                case AmbientMotion.Rise:
                {
                    if (local < 0f) { SetAlpha(live, 0f); return; }
                    float p = local % e.period / e.period;
                    Vector2 off = p < 0.5f ? Vector2.Lerp(Vector2.zero, e.midOffset, p / 0.5f)
                                           : Vector2.Lerp(e.midOffset, e.endOffset, (p - 0.5f) / 0.5f);
                    live.rt.anchoredPosition = live.basePos + new Vector2(off.x, -off.y) * PxToUnits;
                    float a = p < e.fadeInAt ? p / e.fadeInAt : 1f - (p - e.fadeInAt) / (1f - e.fadeInAt);
                    SetAlpha(live, a);
                    break;
                }
                case AmbientMotion.Rotate:
                    live.rt.localRotation = Quaternion.Euler(0f, 0f, -PingPong(e, local)); // CSS: dương = chiều kim đồng hồ
                    break;
                case AmbientMotion.Skew:
                    live.skew.Angle = PingPong(e, local);
                    break;
                case AmbientMotion.Drift:
                    live.rt.anchoredPosition = live.basePos + new Vector2(PingPong(e, local) * PxToUnits, 0f);
                    break;
                case AmbientMotion.Wander:
                {
                    // CSS alternate: đi tới endOffset rồi quay về; vị trí ease-in-out cả quãng, alpha 2 đoạn (0–50%, 50–100%)
                    float u = 0f;
                    if (local > 0f)
                    {
                        float phase = local % (2f * e.period) / e.period;
                        u = phase < 1f ? phase : 2f - phase;
                    }
                    var off = e.endOffset * Bezier.EaseInOut.Evaluate(u);
                    live.rt.anchoredPosition = live.basePos + new Vector2(off.x, -off.y) * PxToUnits;
                    float a = u < 0.5f ? Mathf.Lerp(e.alphaKeys.x, e.alphaKeys.y, Bezier.EaseInOut.Evaluate(u / 0.5f))
                                       : Mathf.Lerp(e.alphaKeys.y, e.alphaKeys.z, Bezier.EaseInOut.Evaluate((u - 0.5f) / 0.5f));
                    SetAlpha(live, a);
                    break;
                }
            }
        }

        /// <summary>Góc qua lại angleFrom ↔ angleTo, ease-in-out mỗi chiều (CSS alternate). Trước khi bắt đầu giữ ở angleFrom.</summary>
        static float PingPong(AmbientElement e, float local)
        {
            if (local <= 0f) return e.angleFrom;
            float phase = local % (2f * e.period) / e.period;
            float u = phase < 1f ? phase : 2f - phase;
            return Mathf.Lerp(e.angleFrom, e.angleTo, e.linear ? u : Bezier.EaseInOut.Evaluate(u));
        }

        static void SetAlpha(Live live, float a)
        {
            var c = live.data.color;
            c.a *= Mathf.Clamp01(a);
            live.image.color = c;
        }
    }
}
