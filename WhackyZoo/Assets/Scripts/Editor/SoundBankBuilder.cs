using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static WhackyZoo.EditorTools.GameSceneBuilder;

namespace WhackyZoo.EditorTools
{
    /// <summary>
    /// Sinh tiếng hiệu ứng (SoundSynth) vào Assets/Audio/Generated, đặt chế độ nạp cho Android, tạo
    /// Assets/Resources/SoundBank.asset và gán nhạc nền (file trong Assets/Audio/Music) cho Trang chủ và từng MapData.
    /// Tiếng đã có thì giữ nguyên (thay bằng âm thanh thật: chép đè file cùng tên).
    /// </summary>
    internal static class SoundBankBuilder
    {
        const string AudioDir = "Assets/Audio/Generated";
        /// <summary>Nhạc nền cố định: file ở đây được dùng nguyên, không tạo lại. Từ 16/11/2026 là nhạc Pixabay đã chỉnh (nguồn: Docs/MusicCredits.md).</summary>
        const string MusicDir = "Assets/Audio/Music";
        internal const string BankPath = "Assets/Resources/SoundBank.asset";

        // Tiếng → (file, âm lượng, cao độ thấp, cao độ cao, khoảng cách tối thiểu)
        static readonly (Sfx id, string file, float vol, float pMin, float pMax, float gap)[] Map =
        {
            (Sfx.Hit, "sfx_hit", 0.9f, 0.92f, 1.08f, 0.02f),
            (Sfx.Bonus, "sfx_bonus", 0.8f, 1f, 1f, 0.05f),
            (Sfx.Obstacle, "sfx_obstacle", 0.9f, 0.95f, 1.05f, 0.05f),
            (Sfx.Shielded, "sfx_shield", 0.8f, 1f, 1f, 0.05f),
            (Sfx.Pop, "sfx_pop", 0.35f, 0.9f, 1.15f, 0.04f),
            (Sfx.Swing, "sfx_swing", 0.45f, 0.9f, 1.1f, 0.02f),
            (Sfx.Combo, "sfx_combo", 0.7f, 1f, 1f, 0.1f),
            (Sfx.Tick, "sfx_tick", 0.6f, 1f, 1f, 0.3f),
            (Sfx.TimeUp, "sfx_timeup", 0.85f, 1f, 1f, 0.5f),
            (Sfx.Coin, "sfx_coin", 0.7f, 1f, 1f, 0.05f),
            (Sfx.Click, "sfx_click", 0.5f, 0.95f, 1.05f, 0.03f),
            (Sfx.Reward, "sfx_reward", 0.8f, 1f, 1f, 0.1f),
            (Sfx.Purchase, "sfx_purchase", 0.8f, 1f, 1f, 0.1f),
            (Sfx.Booster, "sfx_booster", 0.75f, 1f, 1f, 0.1f),
            (Sfx.Freeze, "sfx_freeze", 0.75f, 1f, 1f, 0.1f),
            (Sfx.Lightning, "sfx_lightning", 0.8f, 0.95f, 1.05f, 0.1f),
            (Sfx.LevelUp, "sfx_levelup", 0.85f, 1f, 1f, 0.5f),
        };

        [MenuItem("WhackyZoo/Tạo lại âm thanh tạm")]
        static void RebuildMenu()
        {
            Create(force: true);
            AssetDatabase.SaveAssets();
            Debug.Log("[WhackyZoo] Đã tạo lại âm thanh tạm và SoundBank.");
        }

        /// <summary>Gọi từ các bộ dựng scene: chỉ sinh file còn thiếu.</summary>
        internal static SoundBank Create(bool force = false)
        {
            EnsureFolder("Assets/Audio");
            EnsureFolder(AudioDir);
            EnsureFolder("Assets/Resources");
            var existing = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (existing == null || existing.generatorVersion != SoundSynth.Version) force = true; // bộ tổng hợp mới: sinh lại hết

            var effects = SoundSynth.Effects();
            foreach (var kv in effects) Write(kv.Key, kv.Value, force);

            var bank = Upsert<SoundBank>(BankPath);
            var list = new List<SoundEntry>();
            foreach (var m in Map)
                list.Add(new SoundEntry { id = m.id, clips = new[] { Clip(m.file) }, volume = m.vol, pitchMin = m.pMin, pitchMax = m.pMax, minInterval = m.gap });
            bank.sounds = list.ToArray();
            bank.generatorVersion = SoundSynth.Version;
            EditorUtility.SetDirty(bank);
            AssignMusic(bank);
            return bank;
        }

        /// <summary>
        /// Nhạc nền: file trong Assets/Audio/Music tên music_home / music_land / music_sea / music_desert / music_arctic /
        /// music_grave (đuôi .mp3, .ogg hoặc .wav — ưu tiên .mp3/.ogg tải về hơn .wav cũ). Gán vào SoundBank.homeMusic và MapData.music.
        /// </summary>
        [MenuItem("WhackyZoo/Cập nhật nhạc nền")]
        static void UpdateMusicMenu()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null) { Debug.LogWarning("[WhackyZoo] Chưa có SoundBank — hãy dựng scene trước."); return; }
            AssignMusic(bank);
            AssetDatabase.SaveAssets();
        }

        static void AssignMusic(SoundBank bank)
        {
            var report = new List<string>();
            var home = MusicClip("music_home");
            if (home != null) { bank.homeMusic = home; report.Add("Trang chủ: " + home.name); EditorUtility.SetDirty(bank); }
            var catalog = AssetDatabase.LoadAssetAtPath<MapCatalog>($"{DataRoot}/MapCatalog.asset");
            if (catalog != null)
                foreach (var map in catalog.maps)
                {
                    if (map == null) continue;
                    var clip = MusicClip("music_" + map.id);
                    if (clip == null) continue;
                    map.music = clip;
                    report.Add(map.id + ": " + AssetDatabase.GetAssetPath(clip));
                    EditorUtility.SetDirty(map);
                }
            Debug.Log("[WhackyZoo] Nhạc nền đang dùng:\n" + string.Join("\n", report));
        }

        static readonly string[] MusicExt = { ".mp3", ".ogg", ".wav" };

        static string FindMusic(string name)
        {
            foreach (var ext in MusicExt)
            {
                string p = $"{MusicDir}/{name}{ext}";
                if (File.Exists(p)) return p;
            }
            return null;
        }

        /// <summary>Nhạc nền: file trong Music, đặt chế độ phát luồng, nén Vorbis, giữ stereo. Không có file thì trả về null.</summary>
        static AudioClip MusicClip(string name)
        {
            string p = FindMusic(name);
            if (p == null) return null;
            var importer = AssetImporter.GetAtPath(p) as AudioImporter;
            // nhạc thật là stereo: không ép mono (bản tổng hợp cũ thì mono)
            if (importer != null && (importer.defaultSampleSettings.loadType != AudioClipLoadType.Streaming || importer.forceToMono))
            {
                var st = importer.defaultSampleSettings;
                st.loadType = AudioClipLoadType.Streaming;
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = 0.7f;
                importer.defaultSampleSettings = st;
                importer.forceToMono = false;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
        }

        /// <summary>Tiếng hiệu ứng: ghi WAV (nếu thiếu / bắt sinh lại), giải nén sẵn khi nạp để phát tức thì.</summary>
        static void Write(string name, float[] samples, bool force)
        {
            string path = $"{AudioDir}/{name}.wav";
            if (force || !File.Exists(path))
            {
                SoundSynth.WriteWav(path, samples);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            var s = importer.defaultSampleSettings;
            if (!force && s.loadType == AudioClipLoadType.DecompressOnLoad && importer.forceToMono) return; // đã đặt rồi: khỏi nạp lại
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.quality = 1f;
            importer.defaultSampleSettings = s;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");
    }
}
