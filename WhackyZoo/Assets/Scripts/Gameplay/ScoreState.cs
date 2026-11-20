using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Điểm, combo và thống kê của một ván. Không phụ thuộc Unity scene để dễ test.</summary>
    public class ScoreState
    {
        int comboFloor;
        readonly int minScore;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        /// <summary>Số thú thường + thưởng đã đập trúng (không tính vật cản).</summary>
        public int CrittersHit { get; private set; }
        public int BonusHit { get; private set; }
        public int ObstacleHit { get; private set; }

        public ScoreState(int comboStart, int comboFloor, int minScore)
        {
            this.comboFloor = comboFloor;
            this.minScore = minScore;
            Score = Mathf.Max(0, minScore);
            Combo = comboStart;
            MaxCombo = comboStart;
        }

        /// <summary>Bùa Combo: combo không thấp hơn <paramref name="floor"/> và rơi về mức này khi đập nhầm.</summary>
        public void RaiseComboFloor(int floor)
        {
            comboFloor = Mathf.Max(comboFloor, floor);
            if (Combo < comboFloor) Combo = comboFloor;
            if (Combo > MaxCombo) MaxCombo = Combo;
        }

        /// <summary>
        /// Ghi nhận một cú đập trúng; trả về số điểm hiển thị (đã nhân <paramref name="positiveMultiplier"/> nếu dương).
        /// <paramref name="shielded"/>: Khiên Bom đỡ cú đập nhầm vật cản — không trừ điểm, giữ combo (số vẫn hiện để biết đã đỡ).
        /// </summary>
        public int ApplyHit(CritterData critter, int positiveMultiplier = 1, bool shielded = false)
        {
            int points = critter.points > 0 ? critter.points * Mathf.Max(1, positiveMultiplier) : critter.points;
            if (critter.kind == CritterKind.Obstacle)
            {
                ObstacleHit++;
                if (shielded) return 0; // Khiên đỡ: không trừ điểm, không mất combo
                Combo = comboFloor;
            }
            else
            {
                CrittersHit++;
                if (critter.kind == CritterKind.Bonus) BonusHit++;
                Combo++;
                if (Combo > MaxCombo) MaxCombo = Combo;
            }

            Score = Mathf.Max(minScore, Score + points);
            return points;
        }

        public int Coins(int divisor) => divisor > 0 ? Score / divisor : 0;
    }
}
