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
        // Une manche se joue toujours jusqu'à la dernière bille : le surplus rapporte des tickets.
        // Courbe continue d'un seuil au suivant (x1,6 par seuil), les boss demandent 20 % de plus,
        // et l'endless accélère (+25 % de croissance par acte au-delà du 3e).
        public const double TargetBase = 650, TargetGrowth = 1.6, BossFactor = 1.2, EndlessAcceleration = 1.25;

        public static double TargetFor(int a, int s)
        {
            int i = (a - 1) * 3 + s;
            double t = TargetBase * System.Math.Pow(TargetGrowth, i);
            if (s == 2) t *= BossFactor;
            if (a > 3) t *= System.Math.Pow(EndlessAcceleration, (a - 3) * (a - 2) / 2.0);
            return RoundNice(t);
        }

        /// <summary>Arrondi lisible : deux chiffres significatifs (1 997 → 2 000, 33 503 → 34 000).</summary>
        public static double RoundNice(double v)
        {
            if (v < 100) return System.Math.Round(v);
            double mag = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(v)) - 1);
            return System.Math.Round(v / mag) * mag;
        }

        public double Target => TargetFor(act, seuilIndex);

        public static int RewardFor(int s) => s == 0 ? 3 : s == 1 ? 4 : 5;
        public int BaseReward => RewardFor(seuilIndex);

        /// <summary>Tickets bonus selon le surplus : 4 par doublement de la cible (x1,19 → 1, x2 → 4, x4 → 8), 10 au plus.</summary>
        public const int SurplusCap = 10;

        public static int SurplusBonus(double score, double target)
        {
            if (target <= 0 || score <= target) return 0;
            return System.Math.Min(SurplusCap, (int)System.Math.Floor(4 * System.Math.Log(score / target, 2) + 1e-9));
        }

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
