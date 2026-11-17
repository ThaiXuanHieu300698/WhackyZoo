// Giải mã mp3 bằng Media Foundation của Windows (không cần ffmpeg) → file .f32: int rate, int ch, float[] xen kẽ.
using System;
using System.IO;
using System.Runtime.InteropServices;

static class MF
{
    [DllImport("mfplat.dll")] public static extern int MFStartup(uint ver, uint flags);
    [DllImport("mfplat.dll")] public static extern int MFCreateMediaType(out IMFMediaType t);
    [DllImport("mfreadwrite.dll")] public static extern int MFCreateSourceReaderFromURL([MarshalAs(UnmanagedType.LPWStr)] string url, IntPtr attr, out IMFSourceReader r);
    public static readonly Guid MajorType = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    public static readonly Guid Audio = new Guid("73647561-0000-0010-8000-00AA00389B71");
    public static readonly Guid SubType = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    public static readonly Guid Float = new Guid("00000003-0000-0010-8000-00AA00389B71");
    public static readonly Guid SampleRate = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
    public static readonly Guid Channels = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b1a9b");
}

[ComImport, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMFMediaType
{
    void GetItem(); void GetItemType(); void CompareItem(); void Compare();
    [PreserveSig] int GetUINT32([In] ref Guid key, out int v);
    void GetUINT64(); void GetDouble(); void GetGUID(); void GetStringLength(); void GetString(); void GetAllocatedString();
    void GetBlobSize(); void GetBlob(); void GetAllocatedBlob(); void GetUnknown(); void SetItem(); void DeleteItem(); void DeleteAllItems();
    [PreserveSig] int SetUINT32([In] ref Guid key, int v);
    void SetUINT64(); void SetDouble();
    [PreserveSig] int SetGUID([In] ref Guid key, [In] ref Guid v);
}

[ComImport, Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMFSourceReader
{
    void GetStreamSelection();
    [PreserveSig] int SetStreamSelection(uint idx, bool sel);
    void GetNativeMediaType();
    [PreserveSig] int GetCurrentMediaType(uint idx, out IMFMediaType t);
    [PreserveSig] int SetCurrentMediaType(uint idx, IntPtr reserved, IMFMediaType t);
    void SetCurrentPosition();
    [PreserveSig] int ReadSample(uint idx, uint ctl, out uint actual, out uint flags, out long ts, out IMFSample s);
}

[ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMFSample
{
    void a01(); void a02(); void a03(); void a04(); void a05(); void a06(); void a07(); void a08(); void a09(); void a10();
    void a11(); void a12(); void a13(); void a14(); void a15(); void a16(); void a17(); void a18(); void a19(); void a20();
    void a21(); void a22(); void a23(); void a24(); void a25(); void a26(); void a27(); void a28(); void a29(); void a30();
    void GetSampleFlags(); void SetSampleFlags(); void GetSampleTime(); void SetSampleTime(); void GetSampleDuration(); void SetSampleDuration();
    void GetBufferCount(); void GetBufferByIndex();
    [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer b);
}

[ComImport, Guid("045FA593-8799-42b8-BC8D-8968C6453507"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMFMediaBuffer
{
    [PreserveSig] int Lock(out IntPtr p, out int max, out int cur);
    [PreserveSig] int Unlock();
}

static class Decode
{
    const uint FirstAudio = 0xFFFFFFFD;
    static void Check(int hr, string what) { if (hr < 0) throw new Exception(what + " hr=0x" + hr.ToString("X8")); }

    [STAThread]
    static void Main(string[] a)
    {
        Check(MF.MFStartup(0x20070, 0), "startup");
        for (int i = 0; i + 1 < a.Length; i += 2) Run(a[i], a[i + 1]);
    }

    static void Run(string src, string dst)
    {
        IMFSourceReader r; Check(MF.MFCreateSourceReaderFromURL(Path.GetFullPath(src), IntPtr.Zero, out r), "open");
        IMFMediaType t; Check(MF.MFCreateMediaType(out t), "type");
        Guid k = MF.MajorType, v = MF.Audio; Check(t.SetGUID(ref k, ref v), "major");
        k = MF.SubType; v = MF.Float; Check(t.SetGUID(ref k, ref v), "sub");
        Check(r.SetCurrentMediaType(FirstAudio, IntPtr.Zero, t), "settype");
        IMFMediaType cur; Check(r.GetCurrentMediaType(FirstAudio, out cur), "gettype");
        int rate, ch; k = MF.SampleRate; Check(cur.GetUINT32(ref k, out rate), "rate"); k = MF.Channels; if (cur.GetUINT32(ref k, out ch) < 0) { k = new Guid("322de230-9eeb-43bd-ab7a-ff412251541d"); Check(cur.GetUINT32(ref k, out ch), "align"); ch /= 4; }
        using (var w = new BinaryWriter(File.Create(dst)))
        {
            w.Write(rate); w.Write(ch);
            long n = 0;
            while (true)
            {
                uint act, flags; long ts; IMFSample s;
                Check(r.ReadSample(FirstAudio, 0, out act, out flags, out ts, out s), "read");
                if (s != null)
                {
                    IMFMediaBuffer b; Check(s.ConvertToContiguousBuffer(out b), "buf");
                    IntPtr p; int max, len; Check(b.Lock(out p, out max, out len), "lock");
                    var bytes = new byte[len]; Marshal.Copy(p, bytes, 0, len); b.Unlock();
                    w.Write(bytes); n += len / 4;
                    Marshal.ReleaseComObject(b); Marshal.ReleaseComObject(s);
                }
                if ((flags & 2) != 0) break;
            }
            Console.WriteLine($"{Path.GetFileName(dst)}: {rate} Hz, {ch} kênh, {n / ch / (double)rate:F2} s");
        }
    }
}
