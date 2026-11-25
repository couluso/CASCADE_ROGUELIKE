// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;

namespace Cascade
{
    /// <summary>État complet d'une run : progression, sac, reliques, charmes, planche.</summary>
    public class RunState
    {
        public readonly int seed;
        public readonly System.Random rng;

        public int act = 1;
        public int seuilIndex;            // 0 = petit, 1 = moyen, 2 = boss
        public int tickets = 4;
        public int relicSlots = 5;
        public int charmSlots = 2;
        public int ballsPerRound = 5;
        public int relancesPerRound = 2;

        public readonly List<BallInstance> bag = new List<BallInstance>();
        public BallInstance StarterBall { get; }
        public readonly List<Relic> relics = new List<Relic>();
        public readonly List<CharmType> charms = new List<CharmType>();

        /// <summary>Type de chaque pic, par rangée (0 = bas) puis colonne. Modifiable entre les manches.</summary>
        public readonly PegType[][] pegs;

        public int freeRerolls;
        public bool endless;
        public double bestBallScore;
        public int seuilsCleared;

        readonly Dictionary<int, BossType[]> bosses = new Dictionary<int, BossType[]>();
        readonly Dictionary<int, SkipTag> tags = new Dictionary<int, SkipTag>();

        public RunState(int seed)
        {
            this.seed = seed;
            rng = new System.Random(seed);

            // 9 billes standard + 1 bille spéciale tirée au hasard (jamais la jumelle, trop forte d'entrée).
            StarterBall = new BallInstance(Catalog.StarterBalls[rng.Next(Catalog.StarterBalls.Length)]);
            bag.Add(StarterBall);
            for (int i = 0; i < 9; i++) bag.Add(new BallInstance(BallKind.Standard));

            pegs = new PegType[BoardLayout.Rows][];
            for (int r = 0; r < BoardLayout.Rows; r++)
            {
                pegs[r] = new PegType[BoardLayout.PegsInRow(r)];
                for (int c = 0; c < pegs[r].Length; c++) pegs[r][c] = PegType.Standard;
            }
            // Deux pics multiplicateurs de départ pour apprendre à viser.
            pegs[5][3] = PegType.Multi;
            pegs[5][5] = PegType.Multi;
        }

        public SeuilKind Kind => (SeuilKind)seuilIndex;
        public bool IsBoss => seuilIndex == 2;
        public bool IsFinalSeuil => act == 3 && IsBoss && !endless;

        // ---------------- Cibles ----------------
        // Base de chaque acte (petit seuil). Moyen = x1,5, Boss = x2. Endless : x4 par acte.
        static readonly double[] ActBase = { 800, 3600, 16000 };

        public double TargetFor(int a, int s)
        {
            double b = a <= 3 ? ActBase[a - 1] : ActBase[2] * System.Math.Pow(4, a - 3);
            double k = s == 0 ? 1 : s == 1 ? 1.5 : 2;
            return System.Math.Round(b * k / 50) * 50;
        }

        public double Target => TargetFor(act, seuilIndex);

        public int BaseReward => seuilIndex == 0 ? 3 : seuilIndex == 1 ? 4 : 5;

        public int Interest => Mathf.Min(5, tickets / 5);

        // ---------------- Boss ----------------
        public BossType[] BossesOfAct(int a)
        {
            if (bosses.TryGetValue(a, out var b)) return b;
            var pool = new List<BossType>(Catalog.BossPool);
            foreach (var kv in bosses) foreach (var used in kv.Value) pool.Remove(used);
            if (pool.Count < 2) pool = new List<BossType>(Catalog.BossPool);
            var first = pool[rng.Next(pool.Count)];
            pool.Remove(first);
            // L'acte 3 (et l'endless) : le boss final combine deux effets. Le Colosse ne se combine pas.
            if (a >= 3)
            {
                pool.Remove(BossType.Colossus);
                if (first == BossType.Colossus) first = pool[rng.Next(pool.Count)];
                pool.Remove(first);
                var second = pool[rng.Next(pool.Count)];
                b = new[] { first, second };
            }
            else b = new[] { first };
            bosses[a] = b;
            return b;
        }

        public BossType[] CurrentBosses => IsBoss ? BossesOfAct(act) : new BossType[0];

        public bool HasBoss(BossType t)
        {
            foreach (var b in CurrentBosses) if (b == t) return true;
            return false;
        }

        public SkipTag TagFor(int a, int s)
        {
            int key = a * 10 + s;
            if (!tags.TryGetValue(key, out var t)) { t = (SkipTag)rng.Next(4); tags[key] = t; }
            return t;
        }

        public void Advance()
        {
            seuilIndex++;
            if (seuilIndex > 2) { seuilIndex = 0; act++; }
        }

        public string SeuilName => seuilIndex == 0 ? "Petit seuil" : seuilIndex == 1 ? "Seuil moyen" : "Seuil boss";

        public int RandomInt(int maxExclusive) => rng.Next(maxExclusive);
        public double RandomValue() => rng.NextDouble();

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
