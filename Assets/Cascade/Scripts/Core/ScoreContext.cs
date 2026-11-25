// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Score d'une bille en cours de chute : jetons x mult, puis x valeur de la case.
    /// Chaque gain passe par ici pour déclencher les popups et la musique.
    /// </summary>
    public class ScoreContext
    {
        public BallInstance ball;
        public BallController controller;
        public int throwIndex;        // numéro du lancer dans la manche (1, 2, 3...)

        public double chips;
        public double mult = 1;
        public double xmult = 1;      // multiplicateurs finaux (x), appliqués à l'arrivée

        public int pegHits;
        public int wallHits;
        public int collisions;
        public int dirChanges;
        public float airTime;
        public bool touchedWall;
        public bool resolved;

        public double finalScore;
        public float slotValue;
        public int slotIndex = -1;

        public double Preview => chips * mult;

        public ScoreContext CloneForTwin()
        {
            return new ScoreContext
            {
                ball = ball, throwIndex = throwIndex, chips = chips, mult = mult, xmult = xmult,
                pegHits = pegHits, wallHits = wallHits, collisions = collisions, dirChanges = dirChanges,
                airTime = airTime, touchedWall = touchedWall
            };
        }

        public void AddChips(double v, Vector3 at)
        {
            if (v == 0) return;
            chips = System.Math.Max(0, chips + v);
            GameManager.Instance?.Juice.ScorePopup(at, (v > 0 ? "+" : "") + Fmt.Num(v), Palette.Chips, 0.9f);
        }

        public void AddMult(double v, Vector3 at)
        {
            if (v == 0) return;
            mult = System.Math.Max(1, mult + v);
            GameManager.Instance?.Juice.ScorePopup(at, (v > 0 ? "+" : "") + Fmt.Num(v) + " mult", Palette.Mult, 1f);
        }

        public void MulMult(double v, Vector3 at)
        {
            if (v == 1) return;
            mult *= v;
            GameManager.Instance?.Juice.ScorePopup(at, "x" + Fmt.Num(v) + " mult", Palette.Gold, 1.2f);
        }

        /// <summary>Multiplicateur appliqué au score final (affiché à l'arrivée).</summary>
        public void FinalX(double v, Vector3 at)
        {
            if (v == 1) return;
            xmult *= v;
            GameManager.Instance?.Juice.ScorePopup(at, "x" + Fmt.Num(v), Palette.Gold, 1.3f);
        }
    }

    public static class Fmt
    {
        public static string Num(double v)
        {
            double a = System.Math.Abs(v);
            if (a < 1000)
                return System.Math.Abs(v - System.Math.Round(v)) < 0.05 ? System.Math.Round(v).ToString("0") : v.ToString("0.#");
            string[] s = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx" };
            int i = 0;
            while (a >= 1000 && i < s.Length - 1) { v /= 1000; a /= 1000; i++; }
            if (i == s.Length - 1 && a >= 1000) return v.ToString("0.00e0");
            return v.ToString(a >= 100 ? "0" : a >= 10 ? "0.#" : "0.##") + s[i];
        }
    }
}
