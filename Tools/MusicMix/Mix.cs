// Biến 6 bài Pixabay thành nhạc nền game: tìm điểm nối vòng lặp liền mạch, chồng lớp tiếng tự tổng hợp
// (sóng biển + bong bóng; tabla + santoor; quạ kêu), cân âm lượng, ghi WAV 16-bit stereo.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using S = WhackyZoo.EditorTools.SoundSynth;

static class Mix
{
    const double TwoPi = Math.PI * 2;
    static int R = S.Rate; // tốc độ mẫu của các lớp tự tổng hợp

    static void Main(string[] a)
    {
        string pcm = a[0], outDir = a[1];
        var only = a.Length > 2 ? a[2] : null;
        foreach (var m in new[] { "home", "land", "sea", "arctic", "desert", "grave" })
        {
            if (only != null && only != m) continue;
            int rate; float[][] st; var mono = Pcm.LoadMono(Path.Combine(pcm, m + ".f32"), out rate, out st);
            double len = mono.Length / (double)rate;
            // Vùng tìm điểm đầu / cuối vòng lặp (giây)
            double s0 = 0, s1 = 12, e0 = len - 26, e1 = len - 0.3;
            if (m == "sea") { s0 = 22; s1 = 40; e0 = 140; e1 = 168; } // bỏ đoạn mở đầu nhỏ dần và đoạn kết nhỏ dần
            if (m == "arctic") { e1 = len - 2.5; }                      // bỏ đuôi tắt dần
            if (m == "desert") { s0 = 10.9; s1 = 11.6; e0 = 63; e1 = 66; } // cắt 4 s đầu bản trước: nền từ ~10,9 s, lớp tabla/santoor bỏ 2 ô đầu; 26 ô ≈ 118 nhịp/phút
            if (m == "land") { s0 = 1.1; }
            int s, e; double score;
            // bài có nhịp rõ: độ dài vòng phải tròn số ô nhịp (4 phách) theo nhịp đo chính xác
            double approx = m == "home" || m == "land" ? 100 : m == "arctic" ? 105.5 : m == "grave" ? 102.25 : 0;
            double bar = approx > 0 ? 16 * RefineBeat(mono, rate, 60 / approx) : 0; // vòng = bội 4 ô (một câu nhạc)
            if (bar > 0) Console.WriteLine($"{m}: phách {bar / 16:F4}s = {960 / bar:F2} nhịp/phút");
            FindLoop(mono, rate, s0, s1, e0, e1, bar, out s, out e, out score);
            var loop = MakeLoop(st, s, e, rate);
            if (m == "grave")
            {
                // Bản 3 (16/11/2026, theo người dùng): chỉ lấy phần đầu bài 0–19,2 s (ô 0–7, không có tiếng hú của phần chính);
                // ô 0 có tiếng thì thầm (0,3–1,3 s) thay bằng ô 4. Hiệu ứng chèn thêm ở GraveSfx.
                double barLen = 4 * RefineBeat(mono, rate, 60 / 102.25), off = 0.28;
                var list = new List<int> { 4, 1, 2, 3, 4, 5, 6, 7 };
                loop = BarsLoop(st, rate, off, barLen, list);
                Console.WriteLine($"grave: ghép {list.Count} ô: " + string.Join(",", list));
            }
            double L = loop[0].Length / (double)rate;
            Console.WriteLine($"{m}: vòng lặp {s / (double)rate:F2}s → {e / (double)rate:F2}s ({L:F2}s), độ khớp {score:F3}");

            if (m == "sea") { AddLayer(loop, rate, SeaWaves(L, 1), -0.35, 0.3); AddLayer(loop, rate, SeaWaves(L, 2), 0.35, 0.3); AddLayer(loop, rate, Bubbles(L), 0, 1); }
            string stem = Environment.GetEnvironmentVariable("WZ_STEM"); // tách lớp để nghe: base / mel / tab
            if (m == "desert") { Gain(loop, stem == null || stem == "base" || stem == "all" ? 2.2 : 0); if (stem != "base") DesertLayers(loop, rate, L); }
            if (stem != null) { Gain(loop, 0.74); var cut = new float[loop.Length][]; for (int c = 0; c < loop.Length; c++) { cut[c] = new float[rate * 10]; Array.Copy(loop[c], cut[c], rate * 10); } WriteWav(Path.Combine(outDir, "sa_mac_10s_" + stem + ".wav"), cut, rate); continue; }
            if (m == "grave") GraveSfx(loop, rate, L);
            if (m == "land") Soften(loop, rate, 2000, 0.55);

            double target = m == "sea" || m == "desert" ? -18.5 : m == "grave" ? -18 : -17;
            Loudness(loop, target);
            WriteWav(Path.Combine(outDir, "music_" + m + ".wav"), loop, rate);
        }
    }

    // ── Vòng lặp ──

    /// <summary>Đặc trưng mỗi khung 1024 mẫu: năng lượng log 8 dải (lọc một cực nối tiếp) — so khớp hai đoạn 4 giây.</summary>
    static double[][] Features(float[] x, int rate, int hop)
    {
        double[] cut = { 150, 300, 600, 1200, 2400, 4800, 9600 };
        int F = x.Length / hop; var f = new double[F][];
        var lp = new double[cut.Length];
        var a = cut.Select(c => 1 - Math.Exp(-TwoPi * c / rate)).ToArray();
        for (int k = 0; k < F; k++)
        {
            var e = new double[8];
            for (int i = k * hop; i < (k + 1) * hop; i++)
            {
                double v = x[i], prev = 0;
                for (int b = 0; b < cut.Length; b++) { lp[b] += a[b] * (v - lp[b]); double band = lp[b] - prev; e[b] += band * band; prev = lp[b]; }
                double hi = v - prev; e[7] += hi * hi;
            }
            for (int b = 0; b < 8; b++) e[b] = Math.Log(1e-9 + e[b]);
            f[k] = e;
        }
        return f;
    }

    static void FindLoop(float[] x, int rate, double s0, double s1, double e0, double e1, double bar, out int s, out int e, out double best)
    {
        int hop = 1024; var f = Features(x, rate, hop); double fps = rate / (double)hop;
        int W = (int)(4 * fps), F = f.Length;
        best = double.MaxValue; int bs = 0, be = 0;
        for (int i = (int)(s0 * fps); i <= (int)(s1 * fps); i++)
            for (int j = (int)(e0 * fps); j <= (int)(e1 * fps); j++)
            {
                if (j + W >= F || i + W >= F) continue;
                if (bar > 0) { double Lsec = (j - i) / fps, k = Math.Round(Lsec / bar); if (Math.Abs(Lsec - k * bar) > 1.2 / fps) continue; }
                // sau điểm cuối (j) phải giống sau điểm đầu (i): nhạc nhảy về i mà không nhận ra
                double d = 0;
                for (int k = 0; k < W && d < best; k++) { var p = f[i + k]; var q = f[j + k]; for (int b = 0; b < 8; b++) { double t = p[b] - q[b]; d += t * t; } }
                if (d < best) { best = d; bs = i; be = j; }
            }
        best /= W * 8;
        // tinh chỉnh từng mẫu bằng tương quan dạng sóng ±1 khung
        s = bs * hop; e = be * hop;
        int N = rate / 10; double bc = double.MinValue; int bo = 0;
        int span = bar > 0 ? rate / 50 : hop;
        if (bar > 0) { double k = Math.Round((e - s) / (double)rate / bar); e = s + (int)Math.Round(k * bar * rate); }
        for (int o = -span; o <= span; o++)
        {
            double c = 0; int ee = e + o; if (ee < 0 || ee + N >= x.Length) continue;
            for (int k = 0; k < N; k++) c += x[s + k] * x[ee + k];
            if (c > bc) { bc = c; bo = o; }
        }
        e += bo;
    }

    /// <summary>Vòng lặp x[s..e): đầu vòng hoà dần từ đoạn ngay sau e (chính là đoạn nhạc lẽ ra phát tiếp) sang x[s..].</summary>
    static float[][] MakeLoop(float[][] st, int s, int e, int rate)
    {
        int n = e - s, X = Math.Min(rate / 4, st[0].Length - e);
        var o = new float[st.Length][];
        for (int c = 0; c < st.Length; c++)
        {
            o[c] = new float[n];
            Array.Copy(st[c], s, o[c], 0, n);
            for (int i = 0; i < X; i++)
            {
                double u = (i + 0.5) / X, gin = Math.Sin(u * Math.PI / 2), gout = Math.Cos(u * Math.PI / 2);
                o[c][i] = (float)(st[c][s + i] * gin + st[c][e + i] * gout);
            }
        }
        return o;
    }

    /// <summary>Độ dài phách chính xác: tối đa tự tương quan đường bao khởi âm ở các bội 4..64 phách.</summary>
    static double RefineBeat(float[] x, int rate, double approx)
    {
        int hop = 128; int F = x.Length / hop; var env = new double[F]; double prev = 0;
        for (int f = 0; f < F; f++) { double e = 0; for (int i = f * hop; i < (f + 1) * hop; i++) e += x[i] * x[i]; e = Math.Log(1e-7 + e); env[f] = Math.Max(0, e - prev); prev = e; }
        double mean = env.Average(); for (int f = 0; f < F; f++) env[f] -= mean;
        double fps = rate / (double)hop, best = double.MinValue, bb = approx;
        for (double b = approx * 0.985; b <= approx * 1.015; b += approx * 0.00005)
        {
            double sc = 0;
            foreach (int k in new[] { 4, 8, 16, 32, 64 })
            {
                double lag = k * b * fps; int L = (int)lag; double t = lag - L; if (L + 1 >= F) continue;
                for (int f = 0; f + L + 1 < F; f += 2) sc += env[f] * (env[f + L] * (1 - t) + env[f + L + 1] * t);
            }
            if (sc > best) { best = sc; bb = b; }
        }
        return bb;
    }

    /// <summary>Ghép các ô nhịp theo danh sách thành vòng lặp; mỗi chỗ nối hoà 20 ms với phần nhạc lẽ ra phát tiếp của ô trước.</summary>
    static float[][] BarsLoop(float[][] st, int rate, double off, double barLen, List<int> bars)
    {
        int X0 = rate / 50, Xsec = (int)(rate * 0.6); // nối trong cùng phần: 20 ms; đổi phần (hú → nền): hoà 0,6 s cho tiếng hú tắt dần
        Func<int, int> start = b => (int)Math.Round((off + b * barLen) * rate);
        int total = 0; foreach (var b in bars) total += start(b + 1) - start(b);
        var o = new float[st.Length][];
        for (int c = 0; c < st.Length; c++)
        {
            o[c] = new float[total]; int w = 0;
            for (int k = 0; k < bars.Count; k++)
            {
                int b = bars[k], a = start(b), n = start(b + 1) - a;
                int prev = bars[(k + bars.Count - 1) % bars.Count], prevEnd = start(prev + 1);
                int X = prev >= 8 && b < 8 ? Xsec : X0;
                for (int i = 0; i < n; i++)
                {
                    double v = st[c][a + i];
                    if (i < X && prevEnd + i < st[c].Length)
                    {
                        double u = (i + 0.5) / X;
                        v = v * Math.Sin(u * Math.PI / 2) + st[c][prevEnd + i] * Math.Cos(u * Math.PI / 2);
                    }
                    o[c][w + i] = (float)v;
                }
                w += n;
            }
        }
        return o;
    }

    // ── Trộn ──

    /// <summary>Cộng lớp mono (tốc độ R) vào stereo (tốc độ rate), quấn vòng ở cuối để vòng lặp liền; pan -1..1.</summary>
    static void AddLayer(float[][] dst, int rate, float[] src, double pan, double gain)
    {
        int n = dst[0].Length; double gl = gain * Math.Cos((pan + 1) * Math.PI / 4) * Math.Sqrt(2), gr = gain * Math.Sin((pan + 1) * Math.PI / 4) * Math.Sqrt(2);
        long m = (long)src.Length * rate / R;
        for (long i = 0; i < m; i++)
        {
            double p = i * (double)R / rate; int k = (int)p; double t = p - k;
            double v = src[k] * (1 - t) + (k + 1 < src.Length ? src[k + 1] : 0) * t;
            int d = (int)(i % n);
            dst[0][d] += (float)(v * gl);
            if (dst.Length > 1) dst[1][d] += (float)(v * gr);
        }
    }

    /// <summary>Giảm phần trên tần số fc đi (1 - amount) lần: tiếng sáo / kèn tròn, nhẹ hơn.</summary>
    static void Soften(float[][] x, int rate, double fc, double amount)
    {
        double a = 1 - Math.Exp(-TwoPi * fc / rate);
        foreach (var c in x) { double y = 0; for (int i = 0; i < c.Length; i++) { y += a * (c[i] - y); c[i] = (float)(y + (c[i] - y) * (1 - amount)); } }
    }

    /// <summary>Nền bài gốc hoà vào nhẹ ở đầu vòng (trước khi chồng lớp), tránh vào màn đột ngột.</summary>
    static void FadeInBase(float[][] x, int rate, double sec)
    {
        int n = (int)(sec * rate);
        foreach (var c in x) for (int i = 0; i < n && i < c.Length; i++) { double u = i / (double)n; c[i] = (float)(c[i] * (0.35 + 0.65 * u * u * (3 - 2 * u))); }
    }

    static void Gain(float[][] x, double g) { foreach (var c in x) for (int i = 0; i < c.Length; i++) c[i] = (float)(c[i] * g); }

    static void Put(float[] b, float[] src, double at, double gain)
    {
        int o = (int)Math.Round(at * R);
        for (int i = 0; i < src.Length; i++) { int k = o + i; if (k < 0) continue; if (k >= b.Length) break; b[k] += (float)(src[i] * gain); }
    }

    static float[] Buf(double sec) => new float[Math.Max(1, (int)(sec * R))];

    static float[] Noise(double sec, int seed)
    {
        var r = new Random(seed); var b = Buf(sec);
        for (int i = 0; i < b.Length; i++) b[i] = (float)(r.NextDouble() * 2 - 1);
        return b;
    }

    static void LP(float[] b, double c) { double a = 1 - Math.Exp(-TwoPi * c / R), y = 0; for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = (float)y; } }
    static void HP(float[] b, double c) { double a = 1 - Math.Exp(-TwoPi * c / R), y = 0; for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = (float)(b[i] - y); } }
    static void Norm(float[] b, double peak) { double m = 1e-9; foreach (var v in b) m = Math.Max(m, Math.Abs(v)); for (int i = 0; i < b.Length; i++) b[i] = (float)(b[i] * peak / m); }

    static float[] Glide(double f0, double f1, double dur, double att, double dec)
    {
        var b = Buf(dur); double ph = 0;
        for (int i = 0; i < b.Length; i++)
        {
            double t = i / (double)R, u = t / dur; ph += (f0 * Math.Pow(f1 / f0, u)) / R;
            b[i] = (float)(Math.Sin(TwoPi * ph) * Math.Min(1, t / att) * Math.Exp(-t / dec));
        }
        return b;
    }

    // ── Dưới biển: sóng rì rào + bong bóng lục bục ──

    /// <summary>Sóng: tiếng ầm thấp dâng/rút theo chu kỳ khớp độ dài vòng lặp + bọt "rì rào" đi sau đỉnh sóng.</summary>
    static float[] SeaWaves(double L, int seed)
    {
        var low = Noise(L, 400 + seed); LP(low, 380); LP(low, 520); Norm(low, 1);
        var foam = Noise(L, 500 + seed); HP(foam, 700); LP(foam, 3200); LP(foam, 4200); Norm(foam, 1);
        int n1 = Math.Max(1, (int)Math.Round(L / 15.0)), n2 = Math.Max(1, (int)Math.Round(L / 23.0)); // ~15 s một con sóng
        double ph = -Math.PI / 2 - 0.12 - (seed - 1) * 0.23; // đầu vòng là đáy sóng (lặng): con sóng đầu tiên nổi lên sau ~4 s, rồi lặp mỗi ~15 s
        var b = Buf(L);
        for (int i = 0; i < b.Length; i++)
        {
            double t = i / (double)R;
            double sw = Math.Pow(Math.Max(0, Math.Sin(TwoPi * n1 * t / L + ph)), 2) * (0.75 + 0.25 * Math.Sin(TwoPi * n2 * t / L + ph * 2));
            double crest = Math.Pow(Math.Max(0, Math.Sin(TwoPi * n1 * t / L + ph - 0.6)), 3);
            b[i] = (float)(low[i] * sw * 0.7 + foam[i] * crest * 0.14);
        }
        return b;
    }

    /// <summary>Bong bóng: từng chùm 2–5 tiếng "bóp" trượt lên, thỉnh thoảng giọt nước "tách" — vui tai, không cao độ cố định nên không lệch giọng.</summary>
    static float[] Bubbles(double L)
    {
        var b = Buf(L + 1); var r = new Random(77);
        for (double t = 1.2; t < L - 1; t += 2.6 + r.NextDouble() * 3.2)
        {
            int k = 2 + r.Next(4); double at = t;
            for (int j = 0; j < k; j++)
            {
                double f0 = 260 + r.NextDouble() * 260, f1 = f0 * (2.2 + r.NextDouble());
                var p = Glide(f0, f1, 0.07, 0.002, 0.022); LP(p, 4000);
                Put(b, p, at, 0.09 * (0.6 + 0.4 * r.NextDouble()));
                at += 0.07 + r.NextDouble() * 0.11;
            }
            if (r.NextDouble() < 0.35) Put(b, Glide(1500 + r.NextDouble() * 500, 2600, 0.05, 0.001, 0.015), at + 0.4, 0.05);
        }
        var w = S.Reverb(b, 0.25, 0.7, 0.4);
        Array.Resize(ref w, Buf(L).Length);
        return w;
    }

    // ── Sa mạc: tabla + santoor (thang Bhairav: Đô Rê♭ Mi Fa Son La♭ Si), nhịp nhanh vui ──

    static readonly int[] Bhairav = { 0, 1, 4, 5, 7, 8, 11 };

    static float[] Santoor(double f, double dur, int seed, double vel = 1)
    {
        var b = Buf(dur + 1.4);
        for (int k = 0; k < 3; k++) Put(b, S.Pluck(f * (1 + (k - 1) * 0.0018), dur + 1.3, 0.6, 0.9985, seed * 3 + k), 0, 0.4);
        var h = Noise(0.02, seed); HP(h, 3500);
        for (int i = 0; i < h.Length; i++) h[i] *= (float)Math.Exp(-i / (double)R / 0.004);
        Put(b, h, 0, 0.05);
        LP(b, 5000); Norm(b, vel);
        return b;
    }

    /// <summary>Tabla: dayan (mặt nhỏ, ngân có cao độ — chỉnh theo Đô), bayan (mặt lớn, tiếng trầm trượt lên).</summary>
    static float[] Tabla(string bol, int seed)
    {
        var b = Buf(0.9);
        double tonic = 261.63;
        bool na = bol == "Na" || bol == "Dha" || bol == "Ta", tin = bol == "Tin" || bol == "Dhi", ti = bol == "Ti" || bol == "Te";
        bool ge = bol == "Ge" || bol == "Dha" || bol == "Dhi", ka = bol == "Ka";
        if (na || tin)
        {
            double[] p = { 1, 2, 3, 4, 5 }, amp = { 1, 0.55, 0.35, 0.2, 0.1 };
            double d0 = na ? 0.32 : 0.13;
            for (int h = 0; h < p.Length; h++)
            {
                var s = S.Sine(tonic * p[h], tonic * p[h] * 0.997, 0.8, 0.001, d0 / (1 + h * 0.45));
                Put(b, s, 0, amp[h] * 0.5);
            }
            var c = Noise(0.02, seed); HP(c, 2500);
            for (int i = 0; i < c.Length; i++) c[i] *= (float)Math.Exp(-i / (double)R / 0.003);
            Put(b, c, 0, 0.25);
        }
        if (ti)
        {
            var c = Noise(0.05, seed); HP(c, 1200); LP(c, 5000);
            for (int i = 0; i < c.Length; i++) c[i] *= (float)Math.Exp(-i / (double)R / 0.012);
            Put(b, c, 0, 0.55); Put(b, S.Sine(tonic * 2, tonic * 2, 0.06, 0.001, 0.015), 0, 0.2);
        }
        if (ge)
        {
            var g = Glide(82, 118, 0.7, 0.003, 0.24);
            Put(b, g, 0, 0.9); Put(b, Glide(164, 236, 0.4, 0.003, 0.08), 0, 0.15);
        }
        if (ka)
        {
            var c = Noise(0.06, seed); LP(c, 900);
            for (int i = 0; i < c.Length; i++) c[i] *= (float)Math.Exp(-i / (double)R / 0.02);
            Put(b, c, 0, 0.7); Put(b, S.Sine(130, 100, 0.05, 0.001, 0.015), 0, 0.4);
        }
        return b;
    }

    /// <summary>Giai điệu tự soạn: (cao độ tính theo nửa cung so với Đô5, độ dài theo móc kép, ngân rung tremolo?).</summary>
    static readonly (int p, int d, bool tr)[][] Phrases =
    {
        new[] { (0,2,false),(4,2,false),(5,2,false),(7,4,true),(5,1,false),(4,1,false),(5,2,false),(8,2,false) },
        new[] { (7,2,false),(5,1,false),(4,1,false),(1,2,false),(4,2,false),(0,8,true) },
        new[] { (0,2,false),(4,2,false),(5,2,false),(7,2,false),(8,2,false),(11,2,false),(12,4,true) },
        new[] { (11,1,false),(8,1,false),(7,2,false),(5,2,false),(4,2,false),(1,2,false),(0,6,true) },
        new[] { (7,1,false),(7,1,false),(8,2,false),(7,1,false),(7,1,false),(5,2,false),(4,2,false),(5,2,false),(7,4,true) },
        new[] { (8,2,false),(7,2,false),(5,2,false),(4,2,false),(5,1,false),(4,1,false),(1,2,false),(4,4,true) },
        new[] { (12,2,false),(11,2,false),(8,2,false),(7,2,false),(8,2,false),(7,2,false),(5,2,false),(4,2,false) },
        new[] { (5,2,false),(4,2,false),(1,2,false),(-1,2,false),(0,8,true) },
    };

    static void DesertLayers(float[][] loop, int rate, double L)
    {
        // bản cắt đầu (16/11/2026): bỏ 4 ô mở đầu — ô "ảo" vb = ô thật + sh giữ nguyên bố cục cũ từ ô 4 trở đi
        // 16/11: cắt thêm 2 ô (~4 s) — mở đầu bằng câu đi lên Đô6 (câu 3), ô 2 có đoạn dồn tabla
        const int sh = 6, bars = 32 - sh; double beat = L / (bars * 4), six = beat / 4;
        Console.WriteLine($"   sa mạc: {60 / beat:F1} nhịp/phút");
        var mel = Buf(L + 2); var tab = Buf(L + 2); var jin = Buf(L + 2);
        // bố cục: 0-3 mở đầu (santoor ngân, tabla nhẹ); 4-11 A A' B B'; 12-19 lặp quãng tám dưới; 20-27 B B' A A'; 28-31 A + chạy nốt + tihai
        var plan = new List<(int bar, int phrase, int oct)>();
        for (int i = 2; i < 8; i++) plan.Add((4 + i, i, 0));
        for (int i = 0; i < 8; i++) plan.Add((12 + i, i, -12));
        int[] order = { 4, 5, 6, 7, 0, 1, 2, 3 };
        for (int i = 0; i < 8; i++) plan.Add((20 + i, order[i], 0));
        plan.Add((28, 0, 0)); plan.Add((29, 1, 0));
        int seed = 1;
        foreach (var (bar, ph, oct) in plan)
        {
            double t = (bar - sh) * 4 * beat;
            foreach (var (p, d, tr) in Phrases[ph])
            {
                double f = 440 * Math.Pow(2, (72 + p + oct - 69) / 12.0);
                Put(mel, Santoor(f, d * six, seed++), t, 0.24); // nốt dài: gõ một lần, để ngân tự nhiên
                t += d * six;
            }
        }
        // ô 30: chạy nốt xuống rồi lên theo thang
        var run = new List<int>();
        for (int i = 7; i >= 0; i--) run.Add(i);
        for (int i = 1; i <= 7; i++) run.Add(i);
        run.Add(7);
        for (int k = 0; k < 16; k++)
        {
            int deg = run[k]; int semis = Bhairav[deg % 7] + 12 * (deg / 7);
            Put(mel, Santoor(440 * Math.Pow(2, (72 + semis - 69) / 12.0), six, seed++), (30 - sh) * 4 * beat + k * six, 0.2);
        }
        Put(mel, Santoor(440 * Math.Pow(2, (72 - 69) / 12.0), 3 * beat, seed++), (31 - sh) * 4 * beat, 0.2);

        // tabla: keherwa 8 phách (mỗi phách = móc đơn): Dha Ge Na Ti | Na Ka Dhi Na; ô thứ 4 mỗi câu có đoạn dồn
        string[] keherwa = { "Dha", "Ge", "Na", "Ti", "Na", "Ka", "Dhi", "Na" };
        string[] fill = { "Dha", "Ti", "Te", "Ti", "Dha", "Ge", "Na", "Dha", "Ti", "Te", "Dha", "Ge", "Dha", "Ti", "Na", "Ge" };
        for (int bar = 0; bar < bars; bar++)
        {
            double t0 = bar * 4 * beat, g = bar == 0 ? 0.7 : 1; int vb = bar + sh; // ô đầu nhẹ hơn: vào màn êm, phách 1 vẫn rõ (đích của tihai)
            if (vb == 31)
            {   // tihai: Dha-Ti-Dha ×3 kết vào phách đầu của vòng sau
                string[] th = { "Dha", "Ti", "Dha", "-", "Dha", "Ti", "Dha", "-", "Dha", "Ti", "Dha", "-", "-", "-", "Ge", "Na" };
                for (int k = 0; k < 16; k++) if (th[k] != "-") Put(tab, Tabla(th[k], 900 + k), t0 + k * six, 0.3);
                continue;
            }
            if (vb % 4 == 3)
            {
                for (int k = 0; k < 8; k++) Put(tab, Tabla(keherwa[k], 100 + bar * 8 + k), t0 + k * 2 * six, 0.3 * (k == 0 ? 1.1 : 0.9));
                for (int k = 8; k < 16; k++) Put(tab, Tabla(fill[k], 300 + bar * 16 + k), t0 + k * six, 0.26);
                continue;
            }
            for (int k = 0; k < 8; k++)
            {
                double acc = k == 0 ? 1.15 : k == 4 ? 1 : 0.85;
                Put(tab, Tabla(keherwa[k], 100 + bar * 8 + k), t0 + k * 2 * six, 0.3 * acc * (k == 0 ? 1 : g));
                if (k == 3 || k == 7) Put(tab, Tabla("Te", 600 + bar * 8 + k), t0 + (k * 2 + 1) * six, 0.14);
            }
        }
        // chuông lắc chân (ghungroo) phách lẻ, từ ô 12
        var r = new Random(5);
        for (int bar = 12 - sh; bar < 31 - sh; bar++)
            for (int k = 0; k < 4; k++)
            {
                var j = Noise(0.12, r.Next()); HP(j, 5000); LP(j, 11000);
                for (int i = 0; i < j.Length; i++) { double tt = i / (double)R; j[i] *= (float)(Math.Min(1, tt / 0.004) * Math.Exp(-tt / 0.04)); }
                Put(jin, j, bar * 4 * beat + k * beat + beat / 2, 0.12);
            }
        Array.Resize(ref mel, Buf(L).Length + R); var melR = S.Reverb(mel, 0.22, 0.8, 0.4);
        string stem = Environment.GetEnvironmentVariable("WZ_STEM");
        if (stem == null || stem == "mel" || stem == "all") AddLayer(loop, rate, melR, -0.15, 1.0);
        if (stem == null || stem == "tab" || stem == "all") { AddLayer(loop, rate, tab, 0.12, 1.0); AddLayer(loop, rate, jin, 0.4, 1.0); }
    }

    // ── Nghĩa trang: quạ kêu thưa thớt, có vang ──

    static void Crows(float[][] loop, int rate, double L)
    {
        var r = new Random(13);
        double[] at = { 0.22, 0.42, 0.66, 0.86 }; // tránh đoạn hú đầu vòng
        int[] caws = { 2, 3, 2, 3 };
        double[] pan = { -0.6, 0.55, -0.3, 0.7 };
        for (int i = 0; i < at.Length; i++)
        {
            var c = S.Crow(caws[i], 31 + i * 7);
            var b = Buf(c.Length / (double)R + 1.5); Put(b, c, 0, 1);
            var w = S.Reverb(b, 0.35, 0.85, 0.4);
            var full = Buf(L); Put(full, w, at[i] * L + r.NextDouble() * 2, 0.16);
            AddLayer(loop, rate, full, pan[i], 1);
        }
    }

    /// <summary>Sói hú dài ~5,5 s: "ú" trượt lên, giữ có rung, hạ xuống; hơi thở khàn nhẹ.</summary>
    static float[] LongWolf(double baseHz, int seed)
    {
        const double dur = 5.6, up = 1.1, hold = 3.0;
        var b = Buf(dur); var nz = Noise(dur, seed); HP(nz, 500); LP(nz, 1500);
        double ph = 0;
        for (int i = 0; i < b.Length; i++)
        {
            double t = i / (double)R, f;
            if (t < up) { double u = t / up; f = baseHz * (1 + 0.8 * u * u * (3 - 2 * u)); }
            else if (t < up + hold) f = baseHz * (1.8 + 0.05 * Math.Sin(TwoPi * (t - up) / hold * 0.5));
            else f = baseHz * (1.85 - 0.6 * (t - up - hold) / (dur - up - hold));
            f *= 1 + (t > up ? 0.012 * Math.Sin(TwoPi * 5.2 * t) : 0);
            ph += f / R; ph -= Math.Floor(ph);
            double env = Math.Min(1, t / 0.6) * Math.Min(1, Math.Max(0, dur - t) / 1.2);
            b[i] = (float)((Math.Sin(TwoPi * ph) + 0.3 * Math.Sin(2 * TwoPi * ph) + 0.1 * Math.Sin(3 * TwoPi * ph) + nz[i] * 0.1) * env);
        }
        LP(b, 2200); Norm(b, 1);
        return b;
    }

    /// <summary>Tiếng hú ma quái ~4 s: "u-u-u" trượt lên xuống chậm kiểu theremin, rung sâu, hơi gió, vang dài.</summary>
    static float[] GhostHowl(int seed)
    {
        const double dur = 6.0; // người dùng: dài hơn (4 → 6 s)
        var b = Buf(dur); var air = Noise(dur, seed); HP(air, 600); LP(air, 2500);
        double ph = 0, ph2 = 0;
        double[] pts = { 440, 659, 523, 698, 587, 659, 440 }; // La4 → Mi5 → Đô5 → Fa5 → Rê5 → Mi5 → La4 (giọng La thứ của bài)
        for (int i = 0; i < b.Length; i++)
        {
            double t = i / (double)R, u = t / dur * (pts.Length - 1); int k = Math.Min(pts.Length - 2, (int)u); double w = u - k;
            w = w * w * (3 - 2 * w);
            double f = pts[k] * Math.Pow(pts[k + 1] / pts[k], w);
            f *= 1 + 0.025 * Math.Sin(TwoPi * 6 * t);
            ph += f / R; ph -= Math.Floor(ph); ph2 += f * 1.006 / R; ph2 -= Math.Floor(ph2);
            double env = Math.Min(1, t / 0.7) * Math.Min(1, Math.Max(0, dur - t) / 1.0);
            b[i] = (float)((Math.Sin(TwoPi * ph) * 0.6 + Math.Sin(TwoPi * ph2) * 0.4 + 0.15 * Math.Sin(2 * TwoPi * ph) + air[i] * 0.18 * (0.5 + 0.5 * Math.Sin(TwoPi * 0.7 * t))) * env);
        }
        LP(b, 3000); Norm(b, 1);
        return b;
    }

    /// <summary>Nghĩa trang: hú ma quái ~4 s ở đầu vòng, mỗi tiếng có vang, đặt lệch trái/phải.</summary>
    static void GraveSfx(float[][] loop, int rate, double L)
    {
        Action<float[], double, double, double, double> place = (snd, at, gain, pan, rev) =>
        {
            var b = Buf(snd.Length / (double)R + 2.5); Put(b, snd, 0, 1);
            var w = S.Reverb(b, rev, 0.88, 0.35);
            var full = Buf(L); Put(full, w, at, gain);
            AddLayer(loop, rate, full, pan, 1);
        };
        place(S.Wolf(300, 72), 5.0, 0.22, 0.35, 0.4); // người dùng: thay tiếng hú ma bằng sói hú (~3,2 s), vào map ~5 s mới hú, lặp theo vòng
        // người dùng bỏ quạ kêu và sói hú (16/11/2026): chỉ còn tiếng hú ma quái đầu vòng
        Console.WriteLine($"   nghĩa trang: sói hú 5 s (vòng {L:F1} s)");
    }

    // ── Âm lượng & ghi file ──

    static void Loudness(float[][] x, double targetDb)
    {
        double e = 0; long n = 0;
        foreach (var c in x) foreach (var v in c) { e += v * v; n++; }
        double rms = 10 * Math.Log10(e / n + 1e-12), g = Math.Pow(10, (targetDb - rms) / 20);
        double peak = 0;
        foreach (var c in x) for (int i = 0; i < c.Length; i++) { c[i] = (float)(c[i] * g); peak = Math.Max(peak, Math.Abs(c[i])); }
        // nén đỉnh mềm phía trên 0,85 để không vỡ tiếng
        int clipped = 0;
        foreach (var c in x) for (int i = 0; i < c.Length; i++)
            {
                double v = c[i], a = Math.Abs(v);
                if (a > 0.85) { clipped++; c[i] = (float)(Math.Sign(v) * (0.85 + 0.13 * Math.Tanh((a - 0.85) / 0.13))); }
            }
        Console.WriteLine($"   âm lượng {rms:F1} → {targetDb} dB (×{g:F2}), đỉnh {peak:F2}, mẫu bị nén {clipped * 100.0 / n:F3}%");
    }

    static void WriteWav(string path, float[][] x, int rate)
    {
        int ch = x.Length, n = x[0].Length;
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write("RIFF".ToCharArray()); w.Write(36 + n * ch * 2); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(n * ch * 2);
            for (int i = 0; i < n; i++) for (int c = 0; c < ch; c++) w.Write((short)Math.Max(-32767, Math.Min(32767, Math.Round(x[c][i] * 32767))));
        }
        Console.WriteLine($"   → {Path.GetFileName(path)} {new FileInfo(path).Length / 1048576.0:F1} MB");
    }
}
