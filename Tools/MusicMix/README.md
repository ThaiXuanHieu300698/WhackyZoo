# Công cụ chỉnh nhạc nền (ngoài Unity)

1. `Decode.cs` — giải mã mp3 → `.f32` bằng Media Foundation của Windows: `decode.exe a.mp3 a.f32 ...`
2. `Analyze.cs` — đo im lặng, âm lượng, nhịp, giọng.
3. `Mix.cs` — tìm điểm nối vòng lặp (tròn 4 ô nhịp với bài có nhịp), chồng lớp tự tổng hợp, cân âm lượng, ghi `music_<map>.wav`.
   Cần `Assets/Scripts/Editor/SoundSynth.cs` (đổi các hàm `static` thành `internal static`) và lớp `Pcm` trong `Analyze.cs`.

Biên dịch (.NET Framework 4, x64), ví dụ:
`dotnet <sdk>/Roslyn/bincore/csc.dll -nostdlib -r:mscorlib.dll -r:System.dll -r:System.Core.dll -platform:x64 -optimize -main:Mix -out:mix.exe Mix.cs SynthCopy.cs Analyze.cs`
(đổi `Main` trong Analyze.cs thành tên khác khi biên dịch cùng Mix). Chạy: `mix.exe <thư mục .f32> <thư mục ra> [map]`.

Công cụ dò phụ (dùng chung lớp `Pcm` trong Analyze.cs):
- `Timeline.cs` — âm lượng giữa/bên (stereo) theo thời gian: tìm tiếng thì thầm lệch tai.
- `Odd.cs`, `Uniq.cs` — tìm ô nhịp khác thường so với các ô cùng phần (giọng nói, tiếng hú chen vào).
- `Band.cs` — âm lượng một dải tần theo thời gian (kiểm tra hiệu ứng có nổi trên nền không).
- Biến môi trường `WZ_STEM=all|base|mel|tab` khi chạy `mix.exe … desert`: xuất 10 giây đầu Sa mạc tách lớp để nghe.
