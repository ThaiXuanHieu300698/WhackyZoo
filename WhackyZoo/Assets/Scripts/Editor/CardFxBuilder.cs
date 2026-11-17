using System.Collections.Generic;
using UnityEngine;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Dữ liệu hiệu ứng nền của thẻ ở trang Chọn map (Docs/Animations.md mục 1, Docs/DesignSource/MapSelect25D.dc.html),
    /// ghi vào MapData.cardEffects. Sprite lấy ở Art/MapCards/card_fx_*.png (chép từ images/sprites/map_cards).
    /// Toạ độ px thiết kế, gốc ở góc trên trái ảnh nền thẻ 344 × 122.
    /// </summary>
    internal static class CardFxBuilder
    {
        const string Dir = GameSceneBuilder.Art + "/MapCards";

        /// <summary>Hiệu ứng thẻ của map <paramref name="id"/>; skyBottom = mép dưới dải trời cho hiệu ứng SkyBand.</summary>
        internal static CardFxElement[] Build(string id, out float skyBottom)
        {
            skyBottom = 60f;
            switch (id)
            {
                case "land": return Land();
                case "sea": return Sea();
                case "desert": return Desert();
                case "arctic": return Arctic();
                case "grave": return Grave();
                default: return new CardFxElement[0];
            }
        }

        static Sprite Fx(string name) => GameSceneBuilder.LoadSprite($"{Dir}/card_fx_{name}.png");

        // Mây đung đưa: trôi ngang −8 ↔ +12 px, ease-in-out, ping-pong; mây lớn 5 s, mây nhỏ 6,5 s lệch pha −2 s
        static CardFxElement[] Land()
        {
            CardFxElement Cloud(string name, float left, float top, float w, float h, float alpha, float period, float delay) => new CardFxElement
            {
                name = name, sprite = Fx(name), color = new Color(1f, 1f, 1f, alpha),
                left = left, top = top, width = w, height = h,
                period = period, delay = delay, ease = FxEase.EaseInOut, alternate = true,
                moveKeys = new[] { new Vector3(0f, -8f, 0f), new Vector3(1f, 12f, 0f) },
            };
            return new[]
            {
                Cloud("cloud_big", 18, 22, 92, 30, 1f, 5f, 0f),
                Cloud("cloud_small", 140, 12, 58, 20, 0.9f, 6.5f, -2f),
            };
        }

        // Tia nắng + bong bóng sủi, chỉ trong vùng nước (mặt nạ MapData.cardSkyMask, nằm sau đồi cát)
        static CardFxElement[] Sea()
        {
            var list = new List<CardFxElement>();
            var fade = GameSceneBuilder.FadeSprite(); // trên đặc → dưới trong suốt
            float[] rayLeft = { 40, 170 }, rayDelay = { 0f, 1.5f };
            for (int i = 0; i < 2; i++)
                list.Add(new CardFxElement
                {
                    name = $"Ray{i}", sprite = fade, color = new Color(1f, 1f, 1f, 0.28f), clip = FxClip.MapMask,
                    left = rayLeft[i], top = -30, width = 34, height = 220,
                    period = 5f, delay = rayDelay[i], ease = FxEase.EaseInOut, alternate = true,
                    rotateKeys = new[] { new Vector2(0f, 12f), new Vector2(1f, 22f) }, restRotation = 16f,
                });
            var bubble = AmbientArtBuilder.BubbleSprite();
            float[,] bub = { { 34, 30, 14, 0f }, { 300, 40, 18, 1.3f }, { 330, 30, 10, 2.4f } }; // left, bottom, cỡ, trễ
            for (int i = 0; i < 3; i++)
            {
                float size = bub[i, 2];
                list.Add(new CardFxElement
                {
                    name = $"Bubble{i}", sprite = bubble, color = new Color32(220, 245, 255, 255), clip = FxClip.MapMask,
                    left = bub[i, 0], top = 122 - bub[i, 1] - size, width = size, height = size,
                    period = 4f, delay = bub[i, 3], ease = FxEase.Linear,
                    moveKeys = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, -110f) },
                    alphaKeys = new[] { new Vector2(0f, 0f), new Vector2(0.15f, 1f), new Vector2(1f, 0f) },
                    restAlpha = 0f,
                });
            }
            return list.ToArray();
        }

        // Gió (dải gió mờ): 4 dải card_fx_wind_band kéo giãn theo cỡ từng dải, trôi x −180 → +380 px, ease-in-out, lặp vô hạn;
        // mờ dần vào tới 20%, tan dần ở cuối chu kỳ. Nằm sau thú, chỉ cắt theo khung thẻ.
        static CardFxElement[] Desert()
        {
            var band = Fx("wind_band");
            (float top, float w, float h, float period, float delay)[] bands =
            {
                (14, 150, 10, 6.0f, 0f), (34, 110, 9, 7.5f, 1.8f), (52, 170, 11, 6.8f, 3.4f), (24, 90, 8, 8.0f, 4.6f),
            };
            var list = new CardFxElement[bands.Length];
            for (int i = 0; i < bands.Length; i++)
            {
                var d = bands[i];
                list[i] = new CardFxElement
                {
                    name = $"WindBand{i}", sprite = band,
                    left = 0, top = d.top, width = d.w, height = d.h,
                    period = d.period, delay = d.delay, ease = FxEase.EaseInOut,
                    moveKeys = new[] { new Vector3(0f, -180f, 0f), new Vector3(1f, 380f, 0f) },
                    alphaKeys = new[] { new Vector2(0f, 0f), new Vector2(0.2f, 1f), new Vector2(0.8f, 1f), new Vector2(1f, 0f) },
                    restAlpha = 0f,
                };
            }
            return list;
        }

        // Tuyết: 9 bông từ top −8 rơi (−12, +136) px, tuyến tính; alpha 0 → 1 (12%) → 0,2
        static CardFxElement[] Arctic()
        {
            float[,] flakes = { { 20, 6, 4.6f, 0f }, { 62, 4, 5.8f, 1.8f }, { 104, 7, 4.2f, 3.1f }, { 150, 5, 5.2f, 0.7f }, { 196, 6, 4.8f, 2.4f },
                                { 238, 4, 6.0f, 4.0f }, { 276, 7, 4.4f, 1.2f }, { 318, 5, 5.4f, 3.6f }, { 130, 4, 6.2f, 5.0f } };
            var dot = Fx("snow_dot"); // 12 × 12 px: bông 6 px + quầng sáng → khung = 2 × cỡ bông
            var list = new CardFxElement[flakes.GetLength(0)];
            for (int i = 0; i < list.Length; i++)
            {
                float s = flakes[i, 1];
                list[i] = new CardFxElement
                {
                    name = $"Flake{i}", sprite = dot,
                    left = flakes[i, 0] - s / 2f, top = -8 - s / 2f, width = 2 * s, height = 2 * s,
                    period = flakes[i, 2], delay = flakes[i, 3], ease = FxEase.Linear,
                    moveKeys = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, -12f, 136f) },
                    alphaKeys = new[] { new Vector2(0f, 0f), new Vector2(0.12f, 1f), new Vector2(1f, 0.2f) },
                    restAlpha = 0f,
                };
            }
            return list;
        }

        // Đàn 5 dơi: cả đàn bay (−110, 34) → (440, 4) trong 65% của 9 s rồi nghỉ; mỗi con nhấp nhô 4 px 0,9 s ping-pong;
        // vỗ cánh bằng 2 khung đổi mỗi 0,22 s
        static CardFxElement[] Grave()
        {
            var list = new List<CardFxElement>
            {
                new CardFxElement
                {
                    name = "Flock", period = 9f, ease = FxEase.Linear,
                    moveKeys = new[] { new Vector3(0f, -110f, 34f), new Vector3(0.65f, 440f, 4f), new Vector3(1f, 440f, 4f) },
                },
            };
            Sprite up = Fx("bat_wings_up"), down = Fx("bat_wings_down");
            float[,] bats = { { 0, 14, 44, 26, 0f }, { -46, 0, 34, 20, 0.30f }, { -42, 34, 30, 18, 0.55f }, { -84, 18, 28, 17, 0.15f }, { -96, 42, 24, 14, 0.40f } };
            for (int i = 0; i < bats.GetLength(0); i++)
                list.Add(new CardFxElement
                {
                    name = $"Bat{i}", sprite = up, frames = new[] { down }, frameInterval = 0.22f, parent = 0,
                    left = bats[i, 0], top = bats[i, 1], width = bats[i, 2], height = bats[i, 3],
                    period = 0.9f, delay = bats[i, 4], ease = FxEase.EaseInOut, alternate = true,
                    moveKeys = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, -4f) },
                });
            return list.ToArray();
        }
    }
}
