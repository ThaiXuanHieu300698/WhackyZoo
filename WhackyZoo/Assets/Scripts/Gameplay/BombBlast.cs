using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Hiệu ứng nổ bom kiểu hoạt hình (khi đập nhầm bom, không có Khiên): loé sáng vàng trắng, quả cầu lửa cam phồng ra,
    /// khói đen cuộn lên trộn các cụm lửa cam đỏ (lửa cháy dần thành khói đen),
    /// vòng sóng xung kích, tia lửa bắn ra rơi theo trọng lực. Ảnh sinh lúc chạy,
    /// dùng lại giữa các lần nổ. Đặt trong hố (cùng chỗ với tâm hiệu ứng trúng), chạy theo Time.deltaTime.
    /// Đơn vị px thiết kế (1 px = 3 đơn vị canvas).
    /// </summary>
    public class BombBlast : MonoBehaviour
    {
        const float PxToUnits = 3f;
        const float Duration = 1.0f;
        const int Sparks = 10, Puffs = 9, FirePuffs = 8;

        static Sprite softDisc, ring;

        class Part
        {
            public Image img;
            public Vector2 from, velocity;
            public float delay, life, size0, size1, alpha, spin;
            public bool gravity;
            public Color c0, c1; // màu đầu → màu cuối (lửa cháy thành khói đen)
        }

        readonly List<Part> parts = new List<Part>();
        float t = -1f;

        /// <summary>Tạo (một lần) khung hiệu ứng nổ nằm đúng chỗ <paramref name="anchor"/>, vẽ trên cùng trong hố.</summary>
        public static BombBlast Attach(RectTransform anchor)
        {
            var go = new GameObject("BombBlast", typeof(RectTransform));
            go.layer = anchor.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(anchor.parent, false);
            rt.anchorMin = anchor.anchorMin;
            rt.anchorMax = anchor.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchor.anchoredPosition;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            var b = go.AddComponent<BombBlast>();
            go.SetActive(false);
            return b;
        }

        public void Play()
        {
            EnsureSprites();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            parts.Clear();
            int used = 0;
            Image Take(Sprite s, Color c)
            {
                Image img;
                if (used < transform.childCount) img = transform.GetChild(used).GetComponent<Image>();
                else
                {
                    var go = new GameObject("Part", typeof(RectTransform));
                    go.layer = gameObject.layer;
                    img = go.AddComponent<Image>();
                    img.raycastTarget = false;
                    img.rectTransform.SetParent(transform, false);
                }
                used++;
                img.sprite = s;
                img.color = c;
                img.gameObject.SetActive(true);
                return img;
            }
            void AddPart(Sprite s, Color c, Vector2 from, Vector2 vel, float delay, float life, float s0, float s1, bool gravity = false, float spin = 0f, Color? to = null)
                => parts.Add(new Part { img = Take(s, c), from = from, velocity = vel, delay = delay, life = life, size0 = s0, size1 = s1, alpha = c.a, gravity = gravity, spin = spin, c0 = c, c1 = to ?? c });

            // khói đen: vẽ trước (nằm dưới), cuộn lên và toả rộng
            for (int i = 0; i < Puffs; i++)
            {
                float a = (i / (float)Puffs) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.7f);
                float dark = Random.Range(0.1f, 0.22f);
                AddPart(softDisc, new Color(dark, dark * 0.95f, dark * 0.9f, 0.85f), dir * 10f, dir * Random.Range(40f, 70f) + Vector2.up * Random.Range(40f, 70f),
                    0.05f, Random.Range(0.7f, 0.9f), 28f, Random.Range(62f, 84f));
            }
            // lửa lẫn trong khói: cụm lửa cam đỏ bốc lên cùng khói rồi cháy thành khói đen
            for (int i = 0; i < FirePuffs; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f);
                var fire = Random.value < 0.5f ? new Color(1f, 0.5f, 0.1f, 0.95f) : new Color(1f, 0.28f, 0.08f, 0.95f);
                AddPart(softDisc, fire, dir * 12f, dir * Random.Range(30f, 60f) + Vector2.up * Random.Range(50f, 80f),
                    Random.Range(0.02f, 0.12f), Random.Range(0.5f, 0.7f), 22f, Random.Range(46f, 60f), to: new Color(0.12f, 0.11f, 0.1f, 0.8f));
            }
            // quả cầu lửa cam + lõi vàng trắng
            AddPart(softDisc, new Color(1f, 0.55f, 0.15f, 0.95f), Vector2.zero, Vector2.zero, 0f, 0.38f, 30f, 120f);
            AddPart(softDisc, new Color(1f, 0.93f, 0.55f, 1f), Vector2.zero, Vector2.zero, 0f, 0.22f, 20f, 85f);
            // vòng sóng xung kích
            AddPart(ring, new Color(1f, 0.95f, 0.8f, 0.8f), Vector2.zero, Vector2.zero, 0.02f, 0.32f, 30f, 150f);
            // tia lửa bắn ra, rơi theo trọng lực
            for (int i = 0; i < Sparks; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                AddPart(softDisc, new Color(1f, Random.Range(0.75f, 0.95f), 0.3f, 1f), dir * 10f, dir * Random.Range(160f, 260f) + Vector2.up * 60f,
                    0.02f, Random.Range(0.35f, 0.5f), 12f, 5f, gravity: true);
            }
            for (int i = used; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            t = 0f;
            Apply();
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime;
            Apply();
            if (t >= Duration)
            {
                t = -1f;
                gameObject.SetActive(false);
            }
        }

        void Apply()
        {
            foreach (var p in parts)
            {
                float local = t - p.delay;
                bool on = local >= 0f && local < p.life;
                p.img.enabled = on;
                if (!on) continue;
                float u = local / p.life;
                float e = 1f - (1f - u) * (1f - u); // ease-out
                var pos = p.from + p.velocity * local;
                if (p.gravity) pos += Vector2.down * 420f * local * local;
                var rt = p.img.rectTransform;
                rt.anchoredPosition = pos * PxToUnits;
                rt.sizeDelta = Vector2.one * Mathf.Lerp(p.size0, p.size1, e) * PxToUnits;
                var c = Color.Lerp(p.c0, p.c1, Mathf.Clamp01(u / 0.6f));
                c.a = Mathf.Lerp(p.c0.a, p.c1.a, Mathf.Clamp01(u / 0.6f)) * (u < 0.15f ? 1f : 1f - (u - 0.15f) / 0.85f);
                p.img.color = c;
            }
        }

        // Đốm tròn mờ dần ra mép (lửa, khói, tia lửa) và vòng tròn mềm (sóng xung kích)
        static void EnsureSprites()
        {
            if (softDisc == null) softDisc = Make("BlastDisc", (d) => Mathf.Pow(Mathf.Clamp01(1f - d), 1.4f));
            if (ring == null) ring = Make("BlastRing", (d) => Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f) * 0.9f);
        }

        static Sprite Make(string name, System.Func<float, float> alpha)
        {
            const int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = new Vector2((x + 0.5f) / s * 2f - 1f, (y + 0.5f) / s * 2f - 1f).magnitude;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, d > 1f ? 0f : alpha(d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
