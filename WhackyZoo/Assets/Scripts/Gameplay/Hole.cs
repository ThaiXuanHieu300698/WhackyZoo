using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Một hố trên lưới: thú ló lên / thở / chớp mắt / rụt xuống / bị đập bẹp, kèm dụng cụ vung, BỐP, sao choáng,
    /// chữ điểm bay, lấp lánh của con thưởng. Thông số theo Docs/Animations.md mục 2
    /// (keyframe CSS trong Docs/DesignSource/Game*.dc.html). Gốc của hố là một Image trong suốt làm vùng chạm.
    /// </summary>
    public class Hole : MonoBehaviour, IPointerDownHandler
    {
        enum State { Empty, Rising, Up, Struck, Stunned, Sinking }

        [Header("Hình")]
        [SerializeField] Image holeBack;
        [SerializeField] Image holeFront;
        [Tooltip("Image + Mask: thú chỉ hiện phía trên mép trước của hố.")]
        [SerializeField] Image popMask;
        [SerializeField] RectTransform critter;
        [SerializeField] Image critterImage;
        [Tooltip("Lớp tia lấp lánh (con của Critter), chỉ bật cho con có CritterData.spark.")]
        [SerializeField] Image sparkImage;

        [Header("Hiệu ứng")]
        [SerializeField] RectTransform dizzyStars;
        [SerializeField] Image hitBurst;
        [SerializeField] Image hammer;
        [SerializeField] Text floatText;

        [Header("Hoạt ảnh")]
        [Tooltip("Vị trí y đáy thú khi ló hẳn (đơn vị canvas @3x, so với đáy hố).")]
        [SerializeField] float upY = 54f;
        [SerializeField] Color positiveColor = Color.white;
        [SerializeField] Color negativeColor = new Color(1f, 0.3f, 0.3f);
        [Tooltip("Màu chữ \"Đã chặn!\" khi Khiên Bom đỡ cú đập bom.")]
        [SerializeField] Color shieldColor = new Color(0.56f, 0.83f, 1f);

        // ── Ló lên: 0,42 s, cubic-bezier(.2,.9,.3,1.25), pivot đáy giữa ──
        static readonly Bezier RiseEase = new Bezier(0.2f, 0.9f, 0.3f, 1.25f);
        static readonly Key[] Rise =
        {
            new Key(0f, 1.05f, 0.9f, 1.1f),
            new Key(0.6f, -0.12f, 0.95f, 1.06f),
            new Key(0.82f, 0.03f, 1.06f, 0.94f),
            new Key(1f),
        };

        // ── Thở khi đang ló: bắt đầu sau 0,45 s kể từ lúc ló, 1,3 s/chu kỳ, y −3 px, scale (1,01; 1,03) ──
        const float BobDelay = 0.45f;
        const float BobPeriod = 1.3f;
        const float BobLiftPx = 3f;

        // ── Chớp mắt: 3,2 s/chu kỳ, tuyến tính: 92% scaleY 1 → 95% 0,1 → 100% 1 ──
        const float BlinkPeriod = 3.2f;
        const float BlinkStart = 0.92f, BlinkMid = 0.95f, BlinkMin = 0.1f;

        // ── Bị đập: ease-in, 18% y +8% (1,3; 0,58) → 45% y +10% (1,15; 0,75) → 100% y +115% (1; 0,8) ──
        static readonly Key[] Squash =
        {
            new Key(0f),
            new Key(0.18f, 0.08f, 1.3f, 0.58f),
            new Key(0.45f, 0.10f, 1.15f, 0.75f),
            new Key(1f, 1.15f, 1f, 0.8f),
        };

        // ── Rụt xuống: 0,24 s ease-in tới y +110% scale (1,05; 0,95) ──
        static readonly Key SinkEnd = new Key(1f, 1.10f, 1.05f, 0.95f);

        // ── Lấp lánh: scale 0,8 ↔ 1,2, alpha 0,8 ↔ 1, 0,25 s, ease-in-out, qua lại ──
        const float SparkHalf = 0.25f;

        // ── BỐP: 0,55 s ease-out: scale 0,2 xoay −20° → 45% scale 1,1 xoay 4° → 100% scale 1 alpha 0 ──
        const float BurstDuration = 0.55f;
        static readonly Key[] Burst =
        {
            new Key(0f, 0f, 0.2f, 0.2f, -20f),
            new Key(0.45f, 0f, 1.1f, 1.1f, 4f),
            new Key(1f, 0f, 1f, 1f, 0f, 0f),
        };

        // ── Dụng cụ vung (giữ nhịp cũ đã chốt khi chơi thử): búa giơ cao, tăng tốc giáng xuống, chạm đầu thú ở ImpactAt,
        //    nảy nhẹ rồi mờ dần. Mọi hiệu ứng trúng (nổ, bẹp, sao, số điểm, rung) chỉ bắt đầu lúc búa chạm. ──
        const float HammerDuration = 0.26f;
        const float ImpactAt = 0.45f;
        const float ImpactTime = HammerDuration * ImpactAt; // ≈ 0,12 giây sau khi chạm màn hình
        /// <summary>Thời gian từ lúc chạm tới lúc búa chạm đầu thú (hiệu ứng sấm sét bắt đầu cùng lúc).</summary>
        public const float ImpactDelay = ImpactTime;
        static readonly Bezier HammerEase = new Bezier(0.5f, 0f, 0.8f, 0.5f);
        static readonly Key[] Swing =
        {
            new Key(0f, rotation: 35f),
            new Key(ImpactAt, rotation: -32f),
            new Key(0.7f, rotation: -24f),
            new Key(1f, rotation: -26f, alpha: 0f),
        };

        // ── Chữ điểm bay: 0,9 s ease-out: 0% y +10 scale 0,6 alpha 0 → 25% y 0 scale 1,15 alpha 1 → 100% y −46 alpha 0 ──
        const float FloatDuration = 0.9f;
        const float FloatRisePt = 46f;
        static readonly Key[] FloatUp =
        {
            new Key(0f, 10f / FloatRisePt, 0.6f, 0.6f, alpha: 0f),
            new Key(0.25f, 0f, 1.15f, 1.15f),
            new Key(1f, -1f, 1f, 1f, alpha: 0f),
        };

        // ── Sao choáng: quay 360° mỗi 0,9 s ──
        const float StarsSpinSeconds = 0.9f;

        const float PxToUnits = 3f;

        GameController owner;
        GameConfig config;
        State state = State.Empty;
        float t;        // thời gian trong trạng thái hiện tại
        float alive;    // thời gian kể từ lúc ló (cho thở, chớp mắt, lấp lánh)
        Key from;       // tư thế lúc bắt đầu rụt / bị đập
        CritterData current;
        int pendingPoints;
        bool pendingShielded;
        int blinkFrame = -1;

        float burstT = -1f, hammerT = -1f, floatT = -1f;
        Vector2 hammerPos, burstPos, floatPos;

        Vector2 CritterSize => critter.rect.size;

        public bool IsEmpty => state == State.Empty;
        public bool CanRetreat => state == State.Rising || state == State.Up;
        /// <summary>
        /// Đập trúng khi thú đang ló hẳn; lúc đang trồi lên / rụt xuống thì chỉ khi còn nhô khỏi hố ít nhất
        /// GameConfig.minVisibleToHit chiều cao — tránh chạm vào hố trông như trống mà vẫn ăn điểm.
        /// </summary>
        public bool CanBeHit
        {
            get
            {
                if (state == State.Up) return true;
                if (state != State.Rising && state != State.Sinking) return false;
                float visible = 1f - CurrentPose().offset.y; // offset.y = phần chiều cao đã chui xuống so với lúc ló hẳn
                return visible >= config.minVisibleToHit;
            }
        }

        public void Init(GameController controller, GameConfig cfg, MapData map)
        {
            owner = controller;
            config = cfg;
            if (map != null)
            {
                if (map.holeBack != null) holeBack.sprite = map.holeBack;
                if (map.holeFront != null) holeFront.sprite = map.holeFront;
                if (map.holeMask != null && popMask != null) popMask.sprite = map.holeMask;
            }
            hammerPos = hammer.rectTransform.anchoredPosition;
            burstPos = hitBurst.rectTransform.anchoredPosition;
            floatPos = floatText.rectTransform.anchoredPosition;
            Clear();
        }

        public void Pop(CritterData data)
        {
            AudioManager.Play(Sfx.Pop);
            current = data;
            blinkFrame = -1;
            critterImage.sprite = data.AnimSprite;
            SetupSpark(data);
            critter.gameObject.SetActive(true);
            state = State.Rising;
            t = alive = 0f;
            ApplyCritter(Rise[0]);
        }

        public void Retreat()
        {
            if (!CanRetreat) return;
            from = CurrentPose();
            state = State.Sinking;
            t = 0f;
        }

        public void Clear()
        {
            state = State.Empty;
            current = null;
            critter.gameObject.SetActive(false);
            ApplyCritter(new Key(0f));
            dizzyStars.gameObject.SetActive(false);
            hitBurst.gameObject.SetActive(false);
            hammer.gameObject.SetActive(false);
            floatText.gameObject.SetActive(false);
            burstT = hammerT = floatT = -1f;
        }

        /// <summary>Thú đang ló là vật cản (bom…): tầm đập lan không đập trúng vật cản.</summary>
        public bool HoldsObstacle => current != null && current.kind == CritterKind.Obstacle;

        /// <summary>Điểm đặt hiệu ứng trên hố (tâm BỐP), toạ độ thế giới.</summary>
        public Vector3 FxPoint => hitBurst.rectTransform.position;

        /// <summary>Hình dụng cụ vung ở hố này (dụng cụ đang trang bị).</summary>
        public void SetHammerSprite(Sprite sprite)
        {
            if (sprite == null) return;
            hammer.sprite = sprite;
            hammer.preserveAspect = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner == null || !owner.CanWhack || !CanBeHit) return;
            owner.Whack(this);
        }

        /// <summary>
        /// Đập thú ở hố này. Ghi điểm ngay (để không mất cú đập sát giờ), còn hiệu ứng chờ tới lúc chạm.
        /// <paramref name="swing"/> = false: bị đập lan (sấm sét) nên không vung dụng cụ, chỉ khựng lại rồi bẹp cùng nhịp.
        /// </summary>
        public void Strike(bool swing)
        {
            if (!CanBeHit) return;
            from = CurrentPose();
            pendingPoints = owner.HandleHit(current, out pendingShielded);
            state = State.Struck;
            t = 0f;
            if (!swing) return;
            AudioManager.Play(Sfx.Swing);
            hammerT = 0f;
            hammer.gameObject.SetActive(true);
            UpdateFx(0f);
        }

        void Impact()
        {
            // Đập nhầm bom (không có Khiên): bom nổ tung — hiệu ứng nổ, bom biến mất ngay, không có sao choáng
            bool boom = current != null && current.kind == CritterKind.Obstacle && !pendingShielded;
            owner.OnImpact(current, pendingShielded);
            PlayHitFx(pendingPoints, pendingShielded);
            if (boom)
            {
                if (blast == null) blast = BombBlast.Attach(hitBurst.rectTransform);
                blast.Play();
                Finish();
                return;
            }
            state = State.Stunned;
            t = 0f;
            dizzyStars.gameObject.SetActive(true);
        }

        BombBlast blast;

        void SetupSpark(CritterData data)
        {
            if (sparkImage == null) return;
            bool on = data.spark != null;
            sparkImage.gameObject.SetActive(on);
            if (!on) return;
            sparkImage.sprite = data.spark;
            var rt = sparkImage.rectTransform;
            rt.anchorMin = rt.anchorMax = data.sparkCenter;
            rt.pivot = data.sparkPivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.Scale(data.sparkSize, CritterSize);
        }

        /// <summary>Số điểm bay lên; Khiên đỡ bom thì không trừ điểm nên hiện "Đã chặn!" thay cho số âm.</summary>
        void PlayHitFx(int points, bool shielded)
        {
            // Bỏ hiệu ứng BỐP theo yêu cầu: chỉ còn sao choáng và số điểm bay. hitBurst vẫn giữ làm điểm neo hiệu ứng (FxPoint).
            floatT = 0f;
            floatText.gameObject.SetActive(true);
            floatText.text = shielded ? Loc.T("Đã chặn!") : UiUtil.Signed(points);
            floatText.color = shielded ? shieldColor : points >= 0 ? positiveColor : negativeColor;
            UpdateFx(0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateCritter(dt);
            UpdateFx(dt);
        }

        Key CurrentPose()
        {
            var size = CritterSize;
            var pos = critter.anchoredPosition;
            return new Key(0f, (upY - pos.y) / size.y, critter.localScale.x, critter.localScale.y);
        }

        void ApplyCritter(Key k) => Keyframes.Apply(critter, k, new Vector2(0f, upY), CritterSize);

        void UpdateCritter(float dt)
        {
            if (state == State.Empty) return;
            t += dt;
            alive += dt;

            switch (state)
            {
                case State.Rising:
                {
                    float p = t / config.riseDuration;
                    ApplyCritter(Keyframes.Sample(Rise, Mathf.Min(p, 1f), RiseEase));
                    if (p >= 1f) { state = State.Up; t = 0f; }
                    break;
                }
                case State.Up:
                {
                    var k = new Key(0f);
                    float local = alive - BobDelay;
                    if (local > 0f)
                    {
                        float phase = local % BobPeriod / BobPeriod;
                        float half = phase < 0.5f ? phase * 2f : (1f - phase) * 2f; // keyframe 0% / 50% / 100%
                        float e = Bezier.EaseInOut.Evaluate(half);
                        k.offset.y = -BobLiftPx * PxToUnits / CritterSize.y * e;
                        k.scale = new Vector2(Mathf.Lerp(1f, 1.01f, e), Mathf.Lerp(1f, 1.03f, e));
                    }
                    ApplyCritter(k);
                    break;
                }
                case State.Struck:
                {
                    // Thú khựng lại chờ búa (không thở nữa)
                    ApplyCritter(from);
                    if (t >= ImpactTime) Impact();
                    break;
                }
                case State.Stunned:
                {
                    float p = t / config.stunDuration;
                    var k = Keyframes.Sample(Squash, Mathf.Min(p, 1f), Bezier.EaseIn);
                    k.offset += from.offset; // bẹp từ đúng chỗ đang đứng
                    ApplyCritter(k);
                    dizzyStars.localRotation = Quaternion.Euler(0f, 0f, -360f * (t / StarsSpinSeconds % 1f));
                    if (p >= 1f) Finish();
                    break;
                }
                case State.Sinking:
                {
                    float p = t / config.retreatDuration;
                    ApplyCritter(Key.Lerp(from, SinkEnd, Bezier.EaseIn.Evaluate(Mathf.Min(p, 1f))));
                    if (p >= 1f) Finish();
                    break;
                }
            }

            if (state != State.Empty)
            {
                UpdateBlink();
                UpdateSpark();
            }
        }

        void UpdateBlink()
        {
            if (current == null || current.blinkHalf == null || current.blinkClosed == null) return;
            float p = alive % BlinkPeriod / BlinkPeriod;
            float s = 1f;
            if (p > BlinkStart && p <= BlinkMid) s = Mathf.Lerp(1f, BlinkMin, (p - BlinkStart) / (BlinkMid - BlinkStart));
            else if (p > BlinkMid) s = Mathf.Lerp(BlinkMin, 1f, (p - BlinkMid) / (1f - BlinkMid));
            // 3 khung đã sinh sẵn: mở (1), khép nửa (0,55), nhắm (0,1) — chọn khung gần nhất
            int frame = s > 0.775f ? 0 : s > 0.325f ? 1 : 2;
            if (frame == blinkFrame) return;
            blinkFrame = frame;
            critterImage.sprite = frame == 0 ? current.AnimSprite : frame == 1 ? current.blinkHalf : current.blinkClosed;
        }

        void UpdateSpark()
        {
            if (sparkImage == null || !sparkImage.gameObject.activeSelf) return;
            float phase = alive / SparkHalf % 2f;
            float u = Bezier.EaseInOut.Evaluate(phase < 1f ? phase : 2f - phase);
            sparkImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.2f, u);
            var c = sparkImage.color;
            c.a = Mathf.Lerp(0.8f, 1f, u);
            sparkImage.color = c;
        }

        void Finish()
        {
            state = State.Empty;
            current = null;
            critter.gameObject.SetActive(false);
            dizzyStars.gameObject.SetActive(false);
        }

        void UpdateFx(float dt)
        {
            if (burstT >= 0f)
            {
                burstT += dt;
                float p = burstT / BurstDuration;
                if (p >= 1f) { burstT = -1f; hitBurst.gameObject.SetActive(false); }
                else Show(hitBurst, Keyframes.Sample(Burst, p, Bezier.EaseOut), burstPos);
            }

            if (hammerT >= 0f)
            {
                hammerT += dt;
                float p = hammerT / HammerDuration;
                if (p >= 1f) { hammerT = -1f; hammer.gameObject.SetActive(false); }
                else Show(hammer, Keyframes.Sample(Swing, p, HammerEase), hammerPos);
            }

            if (floatT >= 0f)
            {
                floatT += dt;
                float p = floatT / FloatDuration;
                if (p >= 1f) { floatT = -1f; floatText.gameObject.SetActive(false); }
                else
                {
                    var k = Keyframes.Sample(FloatUp, p, Bezier.EaseOut);
                    // offset tính theo quãng bay 46 pt (đơn vị canvas = pt × 3)
                    Keyframes.Apply(floatText.rectTransform, k, floatPos, Vector2.one * FloatRisePt * PxToUnits);
                    SetAlpha(floatText, k.alpha);
                }
            }
        }

        static void Show(Graphic g, Key k, Vector2 basePos)
        {
            Keyframes.Apply(g.rectTransform, k, basePos, g.rectTransform.rect.size);
            SetAlpha(g, k.alpha);
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = Mathf.Clamp01(a);
            g.color = c;
        }
    }
}
