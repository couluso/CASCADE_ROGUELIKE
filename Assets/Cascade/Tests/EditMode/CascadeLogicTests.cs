// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Cascade.Tests
{
    /// <summary>
    /// Tests de la logique pure (sans scène) : cibles, tickets bonus, graine, score, reliques, boutique.
    /// Window > General > Test Runner > EditMode > Run All.
    /// </summary>
    public class CascadeLogicTests
    {
        // ---------------- Cibles et récompenses ----------------

        [Test]
        public void Targets_IncreaseAtEverySeuil()
        {
            double previous = 0;
            for (int act = 1; act <= 6; act++)
                for (int s = 0; s < 3; s++)
                {
                    double t = RunState.TargetFor(act, s);
                    Assert.Greater(t, previous, $"acte {act}, seuil {s}");
                    previous = t;
                }
        }

        [Test]
        public void Targets_AreReadableRoundNumbers()
        {
            for (int act = 1; act <= 6; act++)
                for (int s = 0; s < 3; s++)
                {
                    double t = RunState.TargetFor(act, s);
                    Assert.AreEqual(t, RunState.RoundNice(t), $"acte {act}, seuil {s} : {t}");
                }
        }

        [TestCase(2000, 2000)]
        [TestCase(1997, 2000)]
        [TestCase(33503, 34000)]
        [TestCase(650, 650)]
        [TestCase(42, 42)]
        public void RoundNice_KeepsTwoSignificantDigits(double input, double expected)
            => Assert.AreEqual(expected, RunState.RoundNice(input));

        [TestCase(500, 1000, 0)]
        [TestCase(1000, 1000, 0)]
        [TestCase(1190, 1000, 1)]
        [TestCase(1500, 1000, 2)]
        [TestCase(2000, 1000, 4)]
        [TestCase(4000, 1000, 8)]
        [TestCase(1000000, 1000, RunState.SurplusCap)]
        public void SurplusBonus_FourTicketsPerDoubling(double score, double target, int expected)
            => Assert.AreEqual(expected, RunState.SurplusBonus(score, target));

        [Test]
        public void Rewards_GrowWithSeuil()
        {
            Assert.Less(RunState.RewardFor(0), RunState.RewardFor(1));
            Assert.Less(RunState.RewardFor(1), RunState.RewardFor(2));
        }

        // ---------------- Graine ----------------

        [Test]
        public void SameSeed_GivesSameRun()
        {
            var a = new RunState(1234);
            var b = new RunState(1234);
            Assert.AreEqual(a.StarterBall.kind, b.StarterBall.kind);
            for (int act = 1; act <= 3; act++)
            {
                CollectionAssert.AreEqual(a.BossesOfAct(act), b.BossesOfAct(act));
                Assert.AreEqual(a.TagFor(act, 0), b.TagFor(act, 0));
            }
            var la = Enumerable.Range(0, 20).ToList();
            var lb = Enumerable.Range(0, 20).ToList();
            a.Shuffle(la);
            b.Shuffle(lb);
            CollectionAssert.AreEqual(la, lb);
        }

        [Test]
        public void NewRun_HasTenBallsAndNoStarterTwin()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var run = new RunState(seed);
                Assert.AreEqual(10, run.bag.Count);
                Assert.AreNotEqual(BallKind.Twin, run.StarterBall.kind);
            }
        }

        [Test]
        public void FinalBoss_CombinesTwoDifferentEffects_WithoutColossus()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var bosses = new RunState(seed).BossesOfAct(3);
                Assert.AreEqual(2, bosses.Length);
                Assert.AreNotEqual(bosses[0], bosses[1]);
                CollectionAssert.DoesNotContain(bosses, BossType.Colossus);
            }
        }

        // ---------------- Score ----------------

        [Test]
        public void ScoreContext_ClampsChipsAndMult()
        {
            var c = new ScoreContext();
            c.AddChips(10, default);
            c.AddChips(-50, default);
            Assert.AreEqual(0, c.chips);
            c.AddMult(-5, default);
            Assert.AreEqual(1, c.mult);
            c.AddChips(20, default);
            c.AddMult(2, default);
            c.MulMult(2, default);
            Assert.AreEqual(20 * 6, c.Preview);
        }

        [Test]
        public void Twin_CopiesTheScoreSoFar()
        {
            var c = new ScoreContext { chips = 40, mult = 3, pegHits = 7, throwIndex = 2 };
            var twin = c.CloneForTwin();
            Assert.AreEqual(c.Preview, twin.Preview);
            Assert.AreEqual(7, twin.pegHits);
            Assert.AreEqual(2, twin.throwIndex);
            Assert.IsFalse(twin.resolved);
        }

        [TestCase(999, "999")]
        [TestCase(1000, "1K")]
        [TestCase(12000, "12K")]
        [TestCase(3000000, "3M")]
        public void Fmt_ShortensBigNumbers(double v, string expected) => Assert.AreEqual(expected, Fmt.Num(v));

        // ---------------- Reliques ----------------

        [Test]
        public void Relics_HaveUniqueIds_AndCanBeRecreated()
        {
            var ids = RelicCatalog.All.Select(r => r.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            foreach (var id in ids) Assert.AreEqual(id, RelicCatalog.Create(id).Id);
        }

        [Test]
        public void Relics_EachHaveTheirOwnPendant()
        {
            foreach (var r in RelicCatalog.All)
                Assert.AreNotEqual(RelicArt.Glyph.Question, RelicArt.DesignOf(r).glyph, $"{r.Id} n'a pas de pendentif");
        }

        [Test]
        public void Pendulum_DoublesOddThrows()
        {
            var odd = new ScoreContext { throwIndex = 1 };
            var even = new ScoreContext { throwIndex = 2 };
            new Pendulum().OnBallLanded(null, odd, 0);
            new Pendulum().OnBallLanded(null, even, 0);
            Assert.AreEqual(2, odd.xmult);
            Assert.AreEqual(0.75, even.xmult);
        }

        [Test]
        public void Tightrope_OnlyPaysWhenTheWallWasTouched()
        {
            var wall = new ScoreContext { touchedWall = true };
            var none = new ScoreContext();
            new Tightrope().OnBallLanded(null, wall, 0);
            new Tightrope().OnBallLanded(null, none, 0);
            Assert.AreEqual(2, wall.xmult);
            Assert.AreEqual(1, none.xmult);
        }

        [Test]
        public void RelicRoll_NeverGivesAnOwnedRelic()
        {
            var run = new RunState(7);
            run.relics.Add(RelicCatalog.Create("piggy"));
            for (int i = 0; i < 200; i++)
            {
                var r = RelicCatalog.Roll(run, new List<string>());
                Assert.IsNotNull(r);
                Assert.AreNotEqual("piggy", r.Id);
            }
        }

        // ---------------- Boutique ----------------

        [Test]
        public void Broker_DiscountsPrices_ButNeverBelowOne()
        {
            var run = new RunState(1);
            var shop = new Shop();
            var offer = new ShopOffer { kind = ShopOffer.Kind.Charm, basePrice = 5 };
            Assert.AreEqual(5, shop.Price(offer, run));
            run.relics.Add(new Broker());
            Assert.AreEqual(4, shop.Price(offer, run));
            offer.basePrice = 1;
            Assert.AreEqual(1, shop.Price(offer, run));
        }
    }
}
