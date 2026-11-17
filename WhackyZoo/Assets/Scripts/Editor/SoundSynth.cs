using System;
using System.Collections.Generic;
using System.IO;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Tổng hợp tiếng hiệu ứng bằng code (không dùng mẫu âm thanh của ai nên không vướng bản quyền).
    /// Phong cách hiện đại, mềm, hợp trẻ em: nhạc cụ mô phỏng (kalimba, marimba, đàn gảy Karplus-Strong, chuông FM,
    /// kèn mềm), có vang (reverb kiểu Freeverb) và lọc bớt tiếng chói. Không phụ thuộc Unity để chạy thử ngoài Editor.
    /// Mono 16-bit, 32.000 Hz. Nhạc nền không tổng hợp ở đây mà là nhạc Pixabay đã chỉnh (Docs/MusicCredits.md).
    /// </summary>
    internal static class SoundSynth
    {
        internal const int Rate = 32000;
        /// <summary>Tăng khi đổi cách tổng hợp: bộ dựng sẽ sinh lại toàn bộ file tạm.</summary>
        internal const int Version = 20;
        const double TwoPi = Math.PI * 2;

        // ═════════════ Khối dựng ═════════════

        static float[] Buf(double seconds) => new float[Math.Max(1, (int)(seconds * Rate))];

        static void Add(float[] dst, float[] src, double at, double gain)
        {
            int o = (int)(at * Rate);
            for (int i = 0; i < src.Length; i++)
            {
                int k = o + i;
                if (k < 0) continue;
                if (k >= dst.Length) break;
                dst[k] += (float)(src[i] * gain);
            }
        }

        static double Midi(double n) => 440.0 * Math.Pow(2, (n - 69) / 12.0);

        static double Env(double t, double attack, double decay) =>
            (t < attack ? t / attack : 1) * (decay > 0 ? Math.Exp(-Math.Max(0, t - attack) / decay) : 1);

        static void LowPass(float[] b, double cutoff)
        {
            double a = 1 - Math.Exp(-TwoPi * cutoff / Rate), y = 0;
            for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = (float)y; }
        }

        static void HighPass(float[] b, double cutoff)
        {
            double a = 1 - Math.Exp(-TwoPi * cutoff / Rate), y = 0;
            for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = (float)(b[i] - y); }
        }

        static void FadeOut(float[] b, double seconds)
        {
            int n = Math.Min(b.Length, (int)(seconds * Rate));
            for (int i = 0; i < n; i++) b[b.Length - 1 - i] *= (float)(i / (double)n);
        }

        static void Normalize(float[] b, double peak)
        {
            double m = 1e-6;
            foreach (var v in b) m = Math.Max(m, Math.Abs(v));
            float g = (float)(peak / m);
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        static float[] Noise(double dur, int seed)
        {
            var r = new Random(seed);
            var b = Buf(dur);
            for (int i = 0; i < b.Length; i++) b[i] = (float)(r.NextDouble() * 2 - 1);
            return b;
        }

        // ═════════════ Nhạc cụ ═════════════

        /// <summary>Sóng sin trượt tần số f0 → f1 (mũ), bao attack + phân rã mũ.</summary>
        static float[] Sine(double f0, double f1, double dur, double attack, double decay)
        {
            var b = Buf(dur);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)Rate, u = i / (double)b.Length;
                ph += f0 * Math.Pow(f1 / f0, u) / Rate;
                b[i] = (float)(Math.Sin(TwoPi * ph) * Env(t, attack, decay));
            }
            return b;
        }

        /// <summary>Kalimba: âm gốc ngân + bội âm không hài hoà tắt nhanh + tiếng gõ nhẹ.</summary>
        static float[] Kalimba(double f, double dur, double decay = 0.6)
        {
            var b = Buf(dur);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)Rate;
                double v = Math.Sin(TwoPi * f * t) * Env(t, 0.003, decay)
                         + 0.22 * Math.Sin(TwoPi * f * 2.01 * t) * Env(t, 0.002, decay * 0.25)
                         + 0.10 * Math.Sin(TwoPi * f * 5.4 * t) * Env(t, 0.001, 0.03);
                b[i] = (float)v;
            }
            FadeOut(b, 0.03);
            return b;
        }

        /// <summary>Marimba: âm gốc + bội 4 (tắt nhanh) — tiếng gỗ ấm.</summary>
        static float[] Marimba(double f, double dur, double decay = 0.35)
        {
            var b = Buf(dur);
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)Rate;
                b[i] = (float)(Math.Sin(TwoPi * f * t) * Env(t, 0.002, decay)
                    + 0.25 * Math.Sin(TwoPi * f * 3.93 * t) * Env(t, 0.001, decay * 0.18)
                    + 0.06 * Math.Sin(TwoPi * f * 9.2 * t) * Env(t, 0.001, 0.015));
            }
            FadeOut(b, 0.02);
            return b;
        }

        /// <summary>Chuông FM (celesta, hộp nhạc, chuông gió): tỉ lệ bộ điều chế + chỉ số giảm dần.</summary>
        static float[] Bell(double f, double dur, double ratio = 3.5, double index = 2.2, double decay = 0.8)
        {
            var b = Buf(dur);
            // Giữ dải biên FM dưới ~9 kHz: nốt cao giảm chỉ số điều chế, tránh hoạ âm vượt Nyquist dội thành tiếng rè
            index = Math.Min(index, Math.Max(0.2, 9000 / (f * ratio) - 1));
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)Rate;
                double idx = index * Math.Exp(-t / (decay * 0.35));
                b[i] = (float)(Math.Sin(TwoPi * f * t + idx * Math.Sin(TwoPi * f * ratio * t)) * Env(t, 0.002, decay));
            }
            FadeOut(b, 0.03);
            return b;
        }

        /// <summary>
        /// Dây gảy Karplus-Strong (guitar, ukulele, harp, sitar). <paramref name="bright"/> 0–1: độ sáng tiếng gảy;
        /// <paramref name="sustain"/>: hệ số giữ năng lượng mỗi chu kỳ (gần 1 = ngân lâu).
        /// </summary>
        static float[] Pluck(double f, double dur, double bright = 0.5, double sustain = 0.996, int seed = 1)
        {
            var b = Buf(dur);
            double period = Rate / f - 0.5; // phép trung bình 2 mẫu thêm trễ nửa mẫu
            int n0 = (int)Math.Ceiling(period) + 2;
            var r = new Random(seed);
            double lp = 0;
            for (int i = 0; i < Math.Min(n0, b.Length); i++)
            {
                lp += (0.15 + 0.85 * bright) * ((r.NextDouble() * 2 - 1) - lp);
                b[i] = (float)lp;
            }
            for (int i = n0; i < b.Length; i++)
            {
                double p = i - period;
                int i0 = (int)p;
                double fr = p - i0;
                double a = b[i0] * (1 - fr) + b[i0 + 1] * fr;
                double c = b[i0 - 1] * (1 - fr) + b[i0] * fr;
                b[i] = (float)(sustain * 0.5 * (a + c));
            }
            HighPass(b, 40);
            FadeOut(b, 0.03);
            return b;
        }

        // ── Vang (Freeverb rút gọn: 4 comb + 2 allpass) ──
        static float[] Reverb(float[] x, double mix, double room = 0.82, double damp = 0.35)
        {
            double s = Rate / 44100.0;
            int[] combs = { (int)(1116 * s), (int)(1277 * s), (int)(1422 * s), (int)(1557 * s) };
            int[] alls = { (int)(556 * s), (int)(341 * s) };
            var wet = new float[x.Length];
            foreach (int L in combs)
            {
                var buf = new double[L];
                double store = 0;
                int idx = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    double o = buf[idx];
                    store = o * (1 - damp) + store * damp;
                    buf[idx] = x[i] * 0.25 + store * room;
                    wet[i] += (float)o;
                    if (++idx >= L) idx = 0;
                }
            }
            foreach (int L in alls)
            {
                var buf = new double[L];
                int idx = 0;
                for (int i = 0; i < wet.Length; i++)
                {
                    double bo = buf[idx], inp = wet[i];
                    wet[i] = (float)(-inp + bo);
                    buf[idx] = inp + bo * 0.5;
                    if (++idx >= L) idx = 0;
                }
            }
            var y = new float[x.Length];
            for (int i = 0; i < x.Length; i++) y[i] = (float)(x[i] * (1 - mix * 0.5) + wet[i] * mix);
            return y;
        }

        /// <summary>Hoàn thiện tiếng hiệu ứng: vang nhẹ, lọc chói, chuẩn hoá.</summary>
        static float[] Finish(float[] b, double reverb, double peak, double tail = 0.4, double cutoff = 9000)
        {
            var padded = new float[b.Length + (int)(tail * Rate)];
            Array.Copy(b, padded, b.Length);
            var y = reverb > 0 ? Reverb(padded, reverb, 0.7, 0.4) : padded;
            LowPass(y, cutoff);
            // cắt phần đuôi gần như im lặng
            int end = y.Length;
            while (end > b.Length && Math.Abs(y[end - 1]) < 1e-4) end--;
            var o = new float[end];
            Array.Copy(y, o, end);
            FadeOut(o, Math.Min(0.05, end / (double)Rate * 0.2));
            Normalize(o, peak);
            return o;
        }

        // ═════════════ Tiếng hiệu ứng ═════════════

        internal static Dictionary<string, float[]> Effects()
        {
            var d = new Dictionary<string, float[]>();

            // Búa chạm: "bụp" tròn — sin trượt xuống + gõ gỗ marimba + hơi nhiễu đã lọc
            var hit = Buf(0.3);
            Add(hit, Sine(360, 120, 0.22, 0.002, 0.07), 0, 0.9);
            Add(hit, Marimba(Midi(74), 0.2, 0.08), 0, 0.35);
            var thud = Noise(0.05, 3); LowPass(thud, 1500);
            for (int i = 0; i < thud.Length; i++) thud[i] *= (float)Math.Exp(-i / (double)Rate / 0.012);
            Add(hit, thud, 0, 0.5);
            d["sfx_hit"] = Finish(hit, 0.08, 0.85, 0.15);

            // Con thưởng: kalimba lấp lánh đi lên + chuông nhỏ
            var bonus = Buf(0.9);
            double[] up = { 79, 83, 86, 91 };
            for (int i = 0; i < up.Length; i++) Add(bonus, Kalimba(Midi(up[i]), 0.7, 0.35), i * 0.06, 0.5);
            Add(bonus, Bell(Midi(98), 0.6, 3.0, 1.2, 0.3), 0.2, 0.25);
            d["sfx_bonus"] = Finish(bonus, 0.25, 0.8);

            // Vật cản: "bụp phù" — trầm mềm + làn khói (không phải tiếng nổ)
            var poof = Buf(0.6);
            Add(poof, Sine(140, 55, 0.35, 0.003, 0.12), 0, 0.9);
            var puff = Noise(0.5, 7); LowPass(puff, 1200); LowPass(puff, 1200);
            for (int i = 0; i < puff.Length; i++) { double t = i / (double)Rate; puff[i] *= (float)(Math.Min(1, t / 0.02) * Math.Exp(-t / 0.12)); }
            Add(poof, puff, 0, 1.6);
            Add(poof, Sine(420, 200, 0.25, 0.01, 0.1), 0.05, 0.2); // "uôm" đi xuống, hơi buồn cười
            d["sfx_obstacle"] = Finish(poof, 0.15, 0.85);

            // Khiên đỡ: chuông thuỷ tinh trong
            var shield = Buf(0.8);
            Add(shield, Bell(Midi(88), 0.7, 1.4, 1.5, 0.35), 0, 0.6);
            Add(shield, Bell(Midi(95), 0.6, 1.4, 1.0, 0.25), 0.03, 0.35);
            d["sfx_shield"] = Finish(shield, 0.3, 0.75);

            // Thú ló lên: giọt nước "bóp" nhỏ
            d["sfx_pop"] = Finish(Sine(380, 900, 0.07, 0.004, 0.025), 0.05, 0.55, 0.1);

            // Vung búa: "vút" êm (nhiễu dải giữa)
            var swing = Noise(0.16, 11); HighPass(swing, 700); LowPass(swing, 2600);
            for (int i = 0; i < swing.Length; i++) { double t = i / (double)Rate; swing[i] *= (float)Math.Sin(Math.PI * Math.Min(1, t / 0.16)); }
            d["sfx_swing"] = Finish(swing, 0, 0.45, 0.02);

            // Combo: hai nốt kalimba đi lên
            var combo = Buf(0.6);
            Add(combo, Kalimba(Midi(79), 0.4, 0.25), 0, 0.6);
            Add(combo, Kalimba(Midi(86), 0.5, 0.3), 0.08, 0.6);
            d["sfx_combo"] = Finish(combo, 0.2, 0.8);

            // Đếm ngược: mõ gỗ nhỏ
            d["sfx_tick"] = Finish(Marimba(Midi(91), 0.12, 0.03), 0.05, 0.6, 0.1);

            d["sfx_timeup"] = Fanfare();

            // Xu: hai nốt chuông sáng
            var coin = Buf(0.6);
            Add(coin, Bell(Midi(88), 0.25, 2.0, 1.0, 0.1), 0, 0.5);
            Add(coin, Bell(Midi(95), 0.5, 2.0, 1.0, 0.25), 0.07, 0.55);
            d["sfx_coin"] = Finish(coin, 0.2, 0.7);

            // Bấm nút: "bóp" nhỏ mềm
            d["sfx_click"] = Finish(Sine(700, 520, 0.05, 0.002, 0.015), 0, 0.45, 0.02);

            // Nhận thưởng: harp rải đi lên + lấp lánh
            var reward = Buf(1.2);
            double[] arp = { 72, 76, 79, 84, 88 };
            for (int i = 0; i < arp.Length; i++) Add(reward, Pluck(Midi(arp[i]), 1.0, 0.6, 0.997, i + 3), i * 0.06, 0.5);
            Add(reward, Bell(Midi(100), 0.6, 3.0, 1.0, 0.3), 0.3, 0.2);
            d["sfx_reward"] = Finish(reward, 0.3, 0.8);

            // Mua hàng: xu + chuông "ting"
            var buy = Buf(0.9);
            Add(buy, Bell(Midi(88), 0.25, 2.0, 1.0, 0.08), 0, 0.45);
            Add(buy, Bell(Midi(95), 0.3, 2.0, 1.0, 0.12), 0.06, 0.45);
            Add(buy, Bell(Midi(100), 0.7, 1.4, 1.2, 0.4), 0.14, 0.5);
            d["sfx_purchase"] = Finish(buy, 0.25, 0.8);

            // Dùng vật phẩm: "vù" phép thuật + chuông nhỏ
            var boost = Buf(0.9);
            var sw = Noise(0.45, 21);
            for (int i = 0; i < sw.Length; i++) { double t = i / (double)Rate; sw[i] *= (float)Math.Sin(Math.PI * t / 0.45); }
            HighPass(sw, 1200); LowPass(sw, 5000);
            Add(boost, sw, 0, 0.5);
            Add(boost, Sine(300, 900, 0.35, 0.03, 0.4), 0, 0.3);
            for (int i = 0; i < 3; i++) Add(boost, Bell(Midi(91 + i * 4), 0.4, 3.5, 1.0, 0.2), 0.25 + i * 0.05, 0.3);
            d["sfx_booster"] = Finish(boost, 0.25, 0.75);

            // Đóng băng: chuông pha lê + gió lạnh
            var freeze = Buf(1.4);
            double[] ice = { 96, 100, 103, 107 };
            for (int i = 0; i < ice.Length; i++) Add(freeze, Bell(Midi(ice[i]), 1.0, 1.41, 1.8, 0.5), i * 0.05, 0.3);
            var air = Noise(1.0, 31); HighPass(air, 3000); LowPass(air, 7000);
            for (int i = 0; i < air.Length; i++) { double t = i / (double)Rate; air[i] *= (float)(Math.Sin(Math.PI * Math.Min(1, t)) * 0.4); }
            Add(freeze, air, 0, 0.5);
            d["sfx_freeze"] = Finish(freeze, 0.35, 0.7);

            d["sfx_lightning"] = Lightning();

            // Lên cấp: harp + chuông, kết bằng hợp âm sáng
            var lvl = Buf(1.8);
            double[] fan = { 72, 76, 79, 84 };
            for (int i = 0; i < fan.Length; i++) Add(lvl, Pluck(Midi(fan[i]), 0.6, 0.6, 0.997, 40 + i), i * 0.1, 0.45);
            foreach (var n in new double[] { 84, 88, 91, 96 }) Add(lvl, Bell(Midi(n), 1.3, 3.5, 1.2, 0.6), 0.42, 0.22);
            d["sfx_levelup"] = Finish(lvl, 0.3, 0.8);

            return d;
        }

        /// <summary>
        /// Kèn mềm (tiếng trong): chỉ vài hoạ âm sạch 1–5 bằng sóng sin (không răng cưa nên không rè), hoạ âm cao mở ra
        /// lúc thổi rồi dịu lại, rung nhẹ khi ngân.
        /// </summary>
        static float[] Brass(double f, double dur)
        {
            var b = Buf(dur + 0.15);
            double[] amp = { 1, 0.45, 0.22, 0.1, 0.05 };
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double t = i / (double)Rate;
                double vib = 1 + (t > 0.3 ? 0.004 * Math.Sin(TwoPi * 5.2 * t) : 0);
                ph += f * vib / Rate; ph -= Math.Floor(ph);
                double bright = 0.55 + 0.45 * Math.Min(1, t / 0.03) * Math.Exp(-Math.Max(0, t - 0.03) / 0.3);
                double v = 0;
                for (int h = 0; h < amp.Length; h++)
                {
                    if (f * (h + 1) > 7000) break;
                    v += Math.Sin(TwoPi * ph * (h + 1)) * amp[h] * (h == 0 ? 1 : bright);
                }
                double env = Math.Min(1, t / 0.025) * (t > dur ? Math.Max(0, 1 - (t - dur) / 0.15) : 1);
                b[i] = (float)(v * env);
            }
            Normalize(b, 1);
            return b;
        }

        /// <summary>
        /// Kết thúc ván — nhạc ăn mừng "được thưởng": trống con dồn, 3 nốt kèn dồn lên (Đô–Mi–Sol) rồi bung hợp âm Đô trưởng
        /// ngân, harp lướt lên lấp lánh, trống cái + chũm chọe nhẹ ở nhịp bung, kết bằng 2 nốt "ting ting" vui (~2 giây).
        /// </summary>
        static float[] Fanfare()
        {
            var b = Buf(2.3);
            var r = new Random(77);
            // trống con dồn 0 → 0,3 s, to dần
            for (double t = 0; t < 0.3; t += 0.028)
            {
                var sn = Noise(0.05, r.Next()); HighPass(sn, 700); LowPass(sn, 3200); LowPass(sn, 3800);
                for (int i = 0; i < sn.Length; i++) sn[i] *= (float)Math.Exp(-i / (double)Rate / 0.015);
                Add(b, sn, t, 0.06 + 0.2 * t / 0.3);
            }
            // kèn dồn: Đô5 Mi5 Sol5 (ngắn, nảy)
            double[] pick = { 72, 76, 79 };
            for (int i = 0; i < pick.Length; i++) Add(b, Brass(Midi(pick[i]), 0.08), 0.06 + i * 0.08, 0.32);
            // bung hợp âm: Đô5 Mi5 Sol5 Đô6 ngân 1,1 s
            const double hit = 0.3;
            foreach (var n in new double[] { 72, 76, 79, 84 }) Add(b, Brass(Midi(n), 1.1), hit, 0.22);
            Add(b, Brass(Midi(60), 1.1), hit, 0.2); // nốt trầm làm nền
            // trống cái + chũm chọe nhẹ ở nhịp bung
            Add(b, Sine(130, 50, 0.35, 0.002, 0.1), hit, 0.7);
            var crash = Noise(1.3, 501); HighPass(crash, 2500); LowPass(crash, 6000); LowPass(crash, 7000);
            for (int i = 0; i < crash.Length; i++) crash[i] *= (float)(Math.Min(1, i / (double)Rate / 0.004) * Math.Exp(-i / (double)Rate / 0.35));
            Add(b, crash, hit, 0.07);
            // harp lướt lên lấp lánh
            double[] gliss = { 72, 74, 76, 79, 81, 84, 86, 88, 91, 93, 96 };
            for (int i = 0; i < gliss.Length; i++) Add(b, Pluck(Midi(gliss[i]), 0.7, 0.45, 0.997, 600 + i), hit + 0.02 + i * 0.025, 0.12);
            // kết: 2 nốt "ting ting" vui
            Add(b, Bell(Midi(96), 0.5, 2.0, 1.0, 0.25), 1.35, 0.25);
            Add(b, Bell(Midi(100), 0.7, 2.0, 1.0, 0.35), 1.47, 0.28);
            return Finish(b, 0.22, 0.85, 0.4, 7500);
        }

        /// <summary>Tiếng xẹt "lách tách" của Búa Thor (không có tiếng sấm — người dùng chốt).</summary>
        static float[] Lightning() => ZapCrackle();

        /// <summary>
        /// Sét Búa Thor: tia điện "xì xẹt xẹt" — chuỗi tiếng nổ lách tách ngẫu nhiên dày dần rồi thưa, kèm tiếng rè điện
        /// 100 Hz răng cưa đã lọc (kiểu "lach_tach").
        /// </summary>
        static float[] ZapCrackle()
        {
            const double dur = 1.1;
            var b = Buf(dur);
            var r = new Random(57);
            // lách tách: xung nhiễu ngắn 2–10 ms, mật độ cao đầu tiếng rồi giảm
            for (double t = 0; t < 0.75;)
            {
                double len = 0.002 + r.NextDouble() * 0.008;
                var crack = Noise(len, r.Next());
                HighPass(crack, 2500);
                for (int i = 0; i < crack.Length; i++) crack[i] *= (float)Math.Exp(-i / (double)crack.Length * 3);
                Add(b, crack, t, (0.5 + r.NextDouble() * 0.5) * Math.Exp(-t / 0.35));
                t += 0.004 + r.NextDouble() * (0.01 + t * 0.06);
            }
            // rè điện: răng cưa 100 Hz + 3 bội, rung biên độ nhanh, lọc dải
            var buzz = Buf(0.6);
            double ph = 0;
            for (int i = 0; i < buzz.Length; i++)
            {
                double t = i / (double)Rate;
                ph += (100 + 6 * Math.Sin(TwoPi * 13 * t)) / Rate; ph -= Math.Floor(ph);
                double flick = 0.6 + 0.4 * Math.Sin(TwoPi * 37 * t) * Math.Sin(TwoPi * 23 * t);
                buzz[i] = (float)((2 * ph - 1) * flick * Math.Min(1, t / 0.01) * Math.Exp(-t / 0.18));
            }
            HighPass(buzz, 400); LowPass(buzz, 4000);
            Add(b, buzz, 0.01, 0.35);
            return Finish(b, 0.15, 0.8, 0.3, 11000);
        }

        // ═════════════ Ghi WAV ═════════════

        internal static void WriteWav(string path, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var v in samples) w.Write((short)Math.Max(-32768, Math.Min(32767, (int)Math.Round(v * 32767))));
            }
        }
    }
}
