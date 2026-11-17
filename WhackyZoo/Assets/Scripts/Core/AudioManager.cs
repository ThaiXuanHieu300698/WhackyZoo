using UnityEngine;

namespace WhackyZoo
{
    /// <summary>
    /// Phát nhạc nền và tiếng hiệu ứng cho cả game. Tự tạo trước scene đầu tiên, sống qua các scene, nạp
    /// Resources/SoundBank. Âm lượng lấy trực tiếp từ Cài đặt (nhạc / hiệu ứng), đổi thanh trượt là nghe thay đổi ngay.
    /// Gọi: <c>AudioManager.Play(Sfx.Hit)</c>, <c>AudioManager.PlayMusic(clip)</c>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SfxVoices = 8;

        static AudioManager instance;

        SoundBank bank;
        AudioSource musicA, musicB, current;
        AudioSource[] voices;
        int nextVoice;
        float fadeT = -1f;
        readonly float[] lastPlayed = new float[64];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
            // "Tai nghe" chung cho mọi scene (camera do bộ dựng scene tạo không kèm AudioListener)
            go.AddComponent<AudioListener>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => instance.DropExtraListeners();
        }

        /// <summary>Scene nào có sẵn AudioListener khác thì tắt bớt, chỉ giữ cái của AudioManager (Unity chỉ cho 1 cái).</summary>
        void DropExtraListeners()
        {
            var mine = GetComponent<AudioListener>();
            foreach (var l in FindObjectsByType<AudioListener>())
                if (l != mine) l.enabled = false;
        }

        void Awake()
        {
            bank = Resources.Load<SoundBank>("SoundBank");
            musicA = NewSource(true);
            musicB = NewSource(true);
            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++) voices[i] = NewSource(false);
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10f;
        }

        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        static float MusicVolume => instance != null && instance.bank != null ? SaveSystem.Data.settings.musicVolume * instance.bank.musicGain : 0f;
        static float SfxVolume => SaveSystem.Data.settings.sfxVolume;

        public static SoundBank Bank => instance != null ? instance.bank : null;

        /// <summary>Phát một tiếng hiệu ứng (bỏ qua nếu chưa có clip hoặc vừa phát trong minInterval).</summary>
        public static void Play(Sfx id, float pitchScale = 1f, float volumeScale = 1f)
        {
            if (instance == null || instance.bank == null) return;
            var e = instance.bank.Find(id);
            if (e == null || e.clips == null || e.clips.Length == 0) return;
            int k = (int)id;
            float now = Time.unscaledTime;
            if (k < instance.lastPlayed.Length)
            {
                if (now - instance.lastPlayed[k] < e.minInterval) return;
                instance.lastPlayed[k] = now;
            }
            var clip = e.clips[Random.Range(0, e.clips.Length)];
            if (clip == null) return;
            var v = instance.voices[instance.nextVoice];
            instance.nextVoice = (instance.nextVoice + 1) % instance.voices.Length;
            v.clip = clip;
            v.pitch = Random.Range(e.pitchMin, e.pitchMax) * pitchScale;
            v.volume = Mathf.Clamp01(e.volume * volumeScale * SfxVolume);
            v.Play();
        }

        /// <summary>Đổi nhạc nền: bản cũ mờ dần, bản mới rõ dần (bank.musicFade). Cùng bản đang phát thì giữ nguyên.</summary>
        public static void PlayMusic(AudioClip clip)
        {
            if (instance == null) return;
            var m = instance;
            m.duckTarget = 1f; // đổi màn: nhạc về mức bình thường
            if (m.current != null && m.current.clip == clip && m.current.isPlaying) return;
            var next = m.current == m.musicA ? m.musicB : m.musicA;
            next.clip = clip;
            next.volume = 0f;
            if (clip != null) next.Play();
            else next.Stop();
            m.previous = m.current;
            m.current = next;
            m.fadeT = 0f;
        }

        /// <summary>Giảm nhỏ nhạc nền (1 = bình thường), chuyển mượt — vd. khi bảng kết quả hiện để nhạc ăn mừng nghe rõ.</summary>
        public static void DuckMusic(float level)
        {
            if (instance != null) instance.duckTarget = Mathf.Clamp01(level);
        }

        public static void PlayHomeMusic()
        {
            if (instance != null && instance.bank != null) PlayMusic(instance.bank.homeMusic);
        }

        AudioSource previous;

        float duck = 1f, duckTarget = 1f;

        void Update()
        {
            duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime / 0.4f);
            float target = MusicVolume * duck;
            if (fadeT >= 0f)
            {
                float dur = bank != null ? Mathf.Max(0.01f, bank.musicFade) : 0.8f;
                fadeT += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(fadeT / dur);
                if (current != null) current.volume = target * p;
                if (previous != null) previous.volume = target * (1f - p);
                if (p >= 1f)
                {
                    if (previous != null) previous.Stop();
                    previous = null;
                    fadeT = -1f;
                }
            }
            else if (current != null) current.volume = target; // kéo thanh Nhạc nền trong Cài đặt
        }
    }
}
