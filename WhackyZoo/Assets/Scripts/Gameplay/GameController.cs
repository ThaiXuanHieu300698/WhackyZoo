using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace WhackyZoo
{
    /// <summary>Điều khiển một ván: đồng hồ, nhịp sinh thú, điểm/combo, tạm dừng và hết giờ.</summary>
    public class GameController : MonoBehaviour
    {
        enum Phase { HowTo, Playing, Paused, Ended }

        [SerializeField] GameConfig config;
        [Tooltip("Map dùng khi chưa chọn từ trang Chọn map (GameSession.SelectedMap). Đổi ở đây để chơi thử map khác.")]
        [SerializeField] MapData map;
        [SerializeField] Image background;
        [Tooltip("Vật trang trí chuyển động trên nền (con của Background), dựng theo MapData.ambient.")]
        [SerializeField] AmbientFxView ambient;
        [SerializeField] Hole[] holes;
        [SerializeField] HudView hud;
        [SerializeField] HowToPlayView howToPlay;
        [SerializeField] ScreenShake shake;
        [SerializeField] PausePopup pausePopup;
        [SerializeField] RoundEndPopup endPopup;
        [Header("Vật phẩm hỗ trợ")]
        [SerializeField] BoosterCatalog boosters;
        [SerializeField] BoosterBar boosterBar;
        [Header("Dụng cụ")]
        [Tooltip("Lấy dụng cụ đang trang bị (hình vung búa, tầm đập).")]
        [SerializeField] ShopCatalog shop;
        [Tooltip("Sấm sét lan của dụng cụ có ToolData.lightning (Búa Thor).")]
        [SerializeField] LightningView lightning;
        [Header("Tiến trình")]
        [Tooltip("Nhiệm vụ, kinh nghiệm: cập nhật khi hết ván.")]
        [SerializeField] ProgressionConfig progression;

        Phase phase;
        ScoreState score;
        float roundLength;
        float timeLeft;
        float spawnTimer;
        float pendingSpawn = -1f;
        readonly List<Hole> emptyHoles = new List<Hole>();

        // Vật phẩm đang có hiệu lực trong ván (không cộng dồn: đang bật thì không dùng thêm được)
        float doubleLeft, freezeLeft;
        int scoreMultiplier = 1;
        bool shieldOn, comboCharmOn;

        ToolData tool;
        readonly List<int> areaCells = new List<int>();
        readonly List<Vector3> boltTargets = new List<Vector3>();
        readonly List<CritterData> critterPool = new List<CritterData>();
        int lastImpactFrame = -1;
        int lastTickSecond = int.MaxValue;

        public bool CanWhack => phase == Phase.Playing;
        public GameConfig Config => config;

        public MapData Map => map;

        void Awake()
        {
            if (GameSession.SelectedMap != null) map = GameSession.SelectedMap;
            if (background != null && map.background != null) background.sprite = map.background;
            if (ambient != null) ambient.Build(map);
            AudioManager.PlayMusic(map.music);
            foreach (var hole in holes) hole.Init(this, config, map);
            tool = SaveSystem.EquippedTool(shop);
            BuildCritterPool();
            if (tool != null) foreach (var hole in holes) hole.SetHammerSprite(tool.icon);
            hud.Init(this, map);
            pausePopup.Init(this);
            endPopup.Init(this);
            SaveSystem.EnsureStarterItems(boosters);
            if (boosterBar != null) boosterBar.Init(boosters, UseBooster, IsBoosterActive);
        }

        void Start()
        {
            // Vào map: hiện màn cách chơi trước, đồng hồ chưa chạy.
            ResetRound();
            phase = Phase.HowTo;
            howToPlay.Show(map, config.howToSeconds, BeginPlay);
        }

        void OnDestroy() => Time.timeScale = 1f;

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Pause();
            SaveSystem.Flush();
        }

        void ResetRound()
        {
            Time.timeScale = 1f;
            roundLength = SaveSystem.RoundSeconds(config);
            timeLeft = roundLength;
            lastTickSecond = int.MaxValue;
            AudioManager.DuckMusic(1f); // chơi lại: nhạc nền về mức bình thường
            score = new ScoreState(config.comboStart, config.comboFloor, config.minScore);
            spawnTimer = 0f;
            pendingSpawn = -1f;

            foreach (var hole in holes) hole.Clear();
            howToPlay.Hide();
            pausePopup.Hide();
            endPopup.Hide();
            hud.SetScore(score.Score);
            hud.SetCombo(score.Combo, false);
            hud.SetTime(timeLeft, roundLength, false);
            doubleLeft = freezeLeft = 0f;
            scoreMultiplier = 1;
            shieldOn = comboCharmOn = false;
            if (boosterBar != null) boosterBar.Refresh();
        }

        void BeginPlay()
        {
            phase = Phase.Playing;
            SpawnCritters();
        }

        /// <summary>Chơi lại trong cùng map: không hiện lại màn cách chơi.</summary>
        public void StartRound()
        {
            ResetRound();
            BeginPlay();
        }

        void Update()
        {
            // Nút Back của Android (escapeKey): tạm dừng / tiếp tục
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (phase == Phase.Playing) Pause();
                else if (phase == Phase.Paused) Resume();
            }
            if (phase != Phase.Playing) return;
            float dt = Time.deltaTime;
            UpdateBoosterTimers(dt);

            if (freezeLeft <= 0f) timeLeft -= dt; // Đóng Băng Giờ: đồng hồ đứng, thú vẫn ló
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                hud.SetTime(0f, roundLength, true);
                EndRound();
                return;
            }
            hud.SetTime(timeLeft, roundLength, timeLeft <= config.warningSeconds);
            // Đếm ngược: "tích" mỗi khi qua một giây trong những giây cảnh báo cuối
            int whole = Mathf.CeilToInt(timeLeft);
            if (whole != lastTickSecond)
            {
                if (whole < lastTickSecond && whole <= config.warningSeconds) AudioManager.Play(Sfx.Tick);
                lastTickSecond = whole;
            }

            spawnTimer += dt;
            float interval = Mathf.Lerp(config.spawnInterval, config.rushSpawnInterval, Rush());
            if (spawnTimer >= interval)
            {
                spawnTimer -= interval;
                Tick();
            }

            if (pendingSpawn >= 0f)
            {
                pendingSpawn -= dt;
                if (pendingSpawn < 0f) SpawnCritters();
            }
        }

        /// <summary>Mỗi nhịp: thú đang ló có thể rụt, sau đó sinh 1–2 con mới vào hố trống.</summary>
        void Tick()
        {
            foreach (var hole in holes)
                if (hole.CanRetreat && Random.value < config.retreatChance)
                    hole.Retreat();
            pendingSpawn = config.retreatDuration;
        }

        /// <summary>Mức nước rút 0…1: 0 trước rushSeconds giây cuối, tăng đều tới 1 ở giây cuối cùng.</summary>
        float Rush() => config.rushSeconds > 0f ? Mathf.Clamp01((config.rushSeconds - timeLeft) / config.rushSeconds) : 0f;

        void SpawnCritters()
        {
            float rush = Rush();
            int min = Mathf.RoundToInt(Mathf.Lerp(config.spawnPerTickMin, config.rushSpawnPerTickMin, rush));
            int max = Mathf.RoundToInt(Mathf.Lerp(config.spawnPerTickMax, config.rushSpawnPerTickMax, rush));
            int cap = Mathf.RoundToInt(Mathf.Lerp(config.maxActiveCritters, config.rushMaxActiveCritters, rush));
            int count = Random.Range(min, Mathf.Max(min, max) + 1);
            for (int n = 0; n < count; n++)
            {
                emptyHoles.Clear();
                int active = 0;
                foreach (var hole in holes)
                {
                    if (hole.IsEmpty) emptyHoles.Add(hole);
                    else active++;
                }
                if (active >= cap || emptyHoles.Count == 0) break;

                var critter = PickCritter();
                if (critter == null) break;
                emptyHoles[Random.Range(0, emptyHoles.Count)].Pop(critter);
            }
        }

        CritterData PickCritter()
        {
            float r = Random.value;
            if (r < config.bonusChance && map.bonus != null) return map.bonus;
            if (r < config.bonusChance + config.obstacleChance && map.obstacle != null) return map.obstacle;
            if (critterPool.Count == 0) return null;
            return critterPool[Random.Range(0, critterPool.Count)];
        }

        /// <summary>Thú thường của map cộng thú đã mua đang bật ở map này; mọi con cùng tỉ lệ ló lên.</summary>
        void BuildCritterPool()
        {
            critterPool.Clear();
            if (map.normalCritters != null)
                foreach (var c in map.normalCritters) if (c != null) critterPool.Add(c);
            if (shop == null || shop.pets == null) return;
            foreach (var pet in shop.pets)
                if (pet != null && !pet.isBase && pet.mapId == map.id && pet.critter != null
                    && SaveSystem.Data.Owns(pet.id) && SaveSystem.IsPetOn(pet)) // đã mua VÀ đang bật "Dùng ở <map>"
                    critterPool.Add(pet.critter);
        }

        /// <summary>
        /// Chạm trúng thú ở <paramref name="tapped"/>: hố đó vung dụng cụ như thường. Dụng cụ tầm tam giác (Búa Thor) đập luôn
        /// thú đang ló ở 5 ô còn lại của nửa tam giác chứa ô chạm (ô trên đường chéo thì chọn ngẫu nhiên nửa trên/dưới);
        /// dụng cụ tầm cột dọc (Gậy Như Ý) đập luôn ô trên và ô dưới ô chạm; vật cản ở các ô lan không bị đập. Sấm sét bắt đầu cùng lúc búa chạm.
        /// </summary>
        public void Whack(Hole tapped)
        {
            if (!CanWhack || tapped == null) return;
            tapped.Strike(true);
            if (tool == null || tool.hitArea == HitArea.Single) return;
            int index = System.Array.IndexOf(holes, tapped);
            if (index < 0) return;

            ToolData.AreaCells(tool.hitArea, index, Random.value < 0.5f, areaCells);
            boltTargets.Clear();
            foreach (int i in areaCells)
            {
                if (i == index || i >= holes.Length) continue;
                var hole = holes[i];
                if (hole.CanBeHit && !hole.HoldsObstacle) hole.Strike(false);
                boltTargets.Add(hole.FxPoint);
            }
            if (tool.lightning && lightning != null) lightning.Play(tapped.FxPoint, boltTargets, Hole.ImpactDelay);
        }

        /// <summary>Gọi từ Hole ngay khi chạm trúng: ghi điểm/combo, trả về điểm danh nghĩa để hiện số bay lên.</summary>
        public int HandleHit(CritterData critter, out bool shielded)
        {
            shielded = shieldOn && critter.kind == CritterKind.Obstacle;
            int shown = score.ApplyHit(critter, scoreMultiplier, shielded);
            if (shielded)
            {
                shieldOn = false; // Khiên đỡ 1 lần rồi hết
                if (boosterBar != null) boosterBar.Refresh();
            }
            return shown;
        }

        // ───────── Vật phẩm hỗ trợ ─────────

        public bool IsBoosterActive(BoosterData b)
        {
            switch (b.effect)
            {
                case BoosterEffect.DoubleScore: return doubleLeft > 0f;
                case BoosterEffect.FreezeTime: return freezeLeft > 0f;
                case BoosterEffect.BombShield: return shieldOn;
                case BoosterEffect.ComboCharm: return comboCharmOn;
                default: return false;
            }
        }

        /// <summary>Bấm nút ở thanh vật phẩm: chỉ khi đang chơi, còn vật phẩm và vật phẩm đó chưa bật.</summary>
        public void UseBooster(BoosterData b)
        {
            if (b == null || phase != Phase.Playing || IsBoosterActive(b)) return;
            if (!SaveSystem.UseItem(b.id)) return;
            AudioManager.Play(b.effect == BoosterEffect.FreezeTime ? Sfx.Freeze : Sfx.Booster);
            switch (b.effect)
            {
                case BoosterEffect.AddTime:
                    timeLeft += b.amount;
                    hud.SetTime(timeLeft, roundLength, timeLeft <= config.warningSeconds);
                    break;
                case BoosterEffect.DoubleScore:
                    doubleLeft = b.duration;
                    scoreMultiplier = Mathf.Max(1, Mathf.RoundToInt(b.amount));
                    break;
                case BoosterEffect.FreezeTime:
                    freezeLeft = b.duration;
                    break;
                case BoosterEffect.BombShield:
                    shieldOn = true;
                    break;
                case BoosterEffect.ComboCharm:
                    comboCharmOn = true;
                    score.RaiseComboFloor(Mathf.RoundToInt(b.amount));
                    hud.SetCombo(score.Combo, false);
                    break;
            }
            if (boosterBar != null) boosterBar.Refresh();
        }

        void UpdateBoosterTimers(float dt)
        {
            bool changed = false;
            if (doubleLeft > 0f)
            {
                doubleLeft -= dt;
                if (doubleLeft <= 0f) { doubleLeft = 0f; scoreMultiplier = 1; changed = true; }
            }
            if (freezeLeft > 0f)
            {
                freezeLeft -= dt;
                if (freezeLeft <= 0f) { freezeLeft = 0f; changed = true; }
            }
            if (changed && boosterBar != null) boosterBar.Refresh();
        }

        /// <summary>Gọi từ Hole lúc búa chạm đầu thú: cập nhật HUD, rung màn và rung máy cùng nhịp với hiệu ứng.</summary>
        public void OnImpact(CritterData critter, bool shielded)
        {
            hud.SetScore(score.Score);
            hud.SetCombo(score.Combo, true);
            PlayHitSound(critter, shielded);
            // Nhiều thú bị đập cùng một cú (sấm sét) thì chỉ rung một lần
            if (Time.frameCount == lastImpactFrame) return;
            lastImpactFrame = Time.frameCount;
            if (shake != null) shake.Play();
            Haptics.Tap(config.hitVibrationMs);
        }

        /// <summary>Tiếng lúc búa chạm: thường / thưởng / vật cản (hoặc khiên đỡ); combo đạt mốc thì thêm tiếng combo cao dần.</summary>
        void PlayHitSound(CritterData critter, bool shielded)
        {
            if (critter.kind == CritterKind.Obstacle) AudioManager.Play(shielded ? Sfx.Shielded : Sfx.Obstacle);
            else
            {
                AudioManager.Play(critter.kind == CritterKind.Bonus ? Sfx.Bonus : Sfx.Hit);
                var bank = AudioManager.Bank;
                int step = bank != null ? Mathf.Max(1, bank.comboStep) : 5;
                if (score.Combo >= step && score.Combo % step == 0)
                    AudioManager.Play(Sfx.Combo, Mathf.Min(1.5f, 1f + 0.06f * (score.Combo / step - 1)));
            }
        }

        void EndRound()
        {
            phase = Phase.Ended;
            pendingSpawn = -1f;
            foreach (var hole in holes) hole.Retreat();
            int coins = score.Coins(config.coinDivisor);
            if (tool != null && tool.coinBonusPercent > 0) coins += coins * tool.coinBonusPercent / 100; // Gậy Bóng Chày +20% xu
            // Nhiệm vụ + kinh nghiệm trước (thành tựu đập thú lấy tổng cũ làm mốc khi mới tạo), rồi xu và kỷ lục
            var xp = Progression.RecordRound(progression, map.id, score.Score, score.CrittersHit, score.BonusHit, score.MaxCombo);
            SaveSystem.RecordRound(map.id, score.Score, score.CrittersHit, coins);
            AudioManager.DuckMusic(0.3f); // nhạc nền nhỏ lại cho nhạc ăn mừng nghe rõ
            AudioManager.Play(Sfx.TimeUp);
            endPopup.Show(score.Score, score.CrittersHit, score.MaxCombo, coins);
            if (xp.levelsGained > 0) StartCoroutine(PlayLater(Sfx.LevelUp, 1.6f));
        }

        System.Collections.IEnumerator PlayLater(Sfx id, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            AudioManager.Play(id);
        }

        public void Pause()
        {
            if (phase != Phase.Playing) return;
            phase = Phase.Paused;
            Time.timeScale = 0f;
            pausePopup.Show();
        }

        public void Resume()
        {
            if (phase != Phase.Paused) return;
            pausePopup.Close(); // mờ ra nhẹ trong lúc ván chạy tiếp
            phase = Phase.Playing;
            Time.timeScale = 1f;
        }

        public void Restart() => StartRound();

        public void GoHome()
        {
            // Trang chủ làm ở bước 3; nút chỉ bật khi đã có scene "Home" trong Build Profiles.
            if (!Application.CanStreamedLevelBeLoaded(SceneNames.Home)) return;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Home);
        }
    }

    public static class SceneNames
    {
        public const string Home = "Home";
        public const string Game = "Game";
    }
}
