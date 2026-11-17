// Phân tích bản nhạc: im lặng đầu/cuối, độ lớn theo giây, nhịp (BPM + pha phách), giọng (chroma).
using System;
using System.IO;
using System.Linq;

static class Pcm
{
    public static float[] LoadMono(string path, out int rate, out float[][] st)
    {
        using (var r = new BinaryReader(File.OpenRead(path)))
        {
            rate = r.ReadInt32(); int ch = r.ReadInt32();
            long n = (r.BaseStream.Length - 8) / 4 / ch;
            var m = new float[n]; st = new float[ch][];
            for (int c = 0; c < ch; c++) st[c] = new float[n];
            var bytes = r.ReadBytes((int)(n * ch * 4));
            for (long i = 0; i < n; i++)
            {
                float s = 0;
                for (int c = 0; c < ch; c++) { float v = BitConverter.ToSingle(bytes, (int)((i * ch + c) * 4)); st[c][i] = v; s += v; }
                m[i] = s / ch;
            }
            return m;
        }
    }
}

static class Analyze
{
    static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    static void Main(string[] a)
    {
        foreach (var p in a) Run(p);
    }

    static void Run(string path)
    {
        int rate; float[][] st;
        var x = Pcm.LoadMono(path, out rate, out st);
        double dur = x.Length / (double)rate;
        float peak = x.Max(v => Math.Abs(v));
        int lead = 0; while (lead < x.Length && Math.Abs(x[lead]) < 0.003f) lead++;
        int tail = x.Length - 1; while (tail > 0 && Math.Abs(x[tail]) < 0.003f) tail--;
        Console.WriteLine($"== {Path.GetFileNameWithoutExtension(path)}: {dur:F2}s peak {peak:F2}, lặng đầu {lead / (double)rate:F3}s, lặng cuối {(x.Length - 1 - tail) / (double)rate:F3}s");
        // RMS mỗi 2 giây (dB)
        var sb = new System.Text.StringBuilder("   RMS/2s:");
        for (int s = 0; s + rate * 2 <= x.Length; s += rate * 2)
        {
            double e = 0; for (int i = s; i < s + rate * 2; i++) e += x[i] * x[i];
            sb.Append(" " + (10 * Math.Log10(e / (rate * 2) + 1e-12)).ToString("F0"));
        }
        Console.WriteLine(sb);
        // Đường bao khởi âm (hop 512) → tự tương quan tìm nhịp
        int hop = 512; int frames = x.Length / hop;
        var env = new double[frames]; double prev = 0;
        for (int f = 0; f < frames; f++)
        {
            double e = 0; for (int i = f * hop; i < (f + 1) * hop; i++) e += x[i] * x[i];
            e = Math.Log(1e-6 + e); env[f] = Math.Max(0, e - prev); prev = e;
        }
        double fps = rate / (double)hop; double best = 0, bestBpm = 0;
        for (double bpm = 70; bpm <= 180; bpm += 0.25)
        {
            double lag = fps * 60 / bpm; double sc = 0;
            for (int f = 0; f + (int)(lag * 2) + 1 < frames; f++)
            {
                double l1 = Interp(env, f + lag), l2 = Interp(env, f + 2 * lag);
                sc += env[f] * (l1 + 0.5 * l2);
            }
            if (sc > best) { best = sc; bestBpm = bpm; }
        }
        // Pha: vị trí phách đầu tiên cho tổng khởi âm trên lưới lớn nhất
        double period = fps * 60 / bestBpm; double bestPh = 0, bestS = 0;
        for (double ph = 0; ph < period; ph += 0.25)
        {
            double s = 0; for (double f = ph; f < frames - 1; f += period) s += Interp(env, f);
            if (s > bestS) { bestS = s; bestPh = ph; }
        }
        Console.WriteLine($"   Nhịp ~{bestBpm:F2} BPM (phách {60 / bestBpm:F4}s), phách đầu {bestPh / fps:F3}s; ứng viên ×2/÷2: {bestBpm * 2:F1}/{bestBpm / 2:F1}");
        // Chroma bằng Goertzel trên tín hiệu hạ mẫu 11025
        int dec = rate / 11025; int r2 = rate / dec; var y = new float[x.Length / dec];
        for (int i = 0; i < y.Length; i++) { double s = 0; for (int k = 0; k < dec; k++) s += x[i * dec + k]; y[i] = (float)(s / dec); }
        var chroma = new double[12]; var noteE = new double[128];
        int win = 8192;
        for (int s0 = 0; s0 + win < y.Length; s0 += win * 2)
            for (int m = 40; m <= 88; m++)
            {
                double f = 440 * Math.Pow(2, (m - 69) / 12.0), w = 2 * Math.Cos(2 * Math.PI * f / r2), q1 = 0, q2 = 0;
                for (int i = 0; i < win; i++) { double q0 = w * q1 - q2 + y[s0 + i]; q2 = q1; q1 = q0; }
                double pw = q1 * q1 + q2 * q2 - w * q1 * q2;
                chroma[m % 12] += Math.Sqrt(pw); noteE[m] += Math.Sqrt(pw);
            }
        double[] maj = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
        double[] min = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };
        var res = new System.Collections.Generic.List<(double c, string n)>();
        for (int k = 0; k < 12; k++)
        {
            res.Add((Corr(chroma, maj, k), Names[k] + " trưởng"));
            res.Add((Corr(chroma, min, k), Names[k] + " thứ"));
        }
        var top = res.OrderByDescending(t => t.c).Take(3).Select(t => $"{t.n} ({t.c:F2})");
        double mx = chroma.Max();
        Console.WriteLine("   Giọng: " + string.Join(", ", top) + " | chroma " + string.Join(" ", Enumerable.Range(0, 12).Select(k => Names[k] + ":" + (chroma[k] / mx).ToString("F2"))));
    }

    static double Corr(double[] c, double[] prof, int k)
    {
        double mc = c.Average(), mp = prof.Average(), sxy = 0, sx = 0, sy = 0;
        for (int i = 0; i < 12; i++) { double dx = c[(i + k) % 12] - mc, dy = prof[i] - mp; sxy += dx * dy; sx += dx * dx; sy += dy * dy; }
        return sxy / Math.Sqrt(sx * sy);
    }

    static double Interp(double[] e, double f)
    {
        int i = (int)f; if (i + 1 >= e.Length) return 0; double t = f - i; return e[i] * (1 - t) + e[i + 1] * t;
    }
}
