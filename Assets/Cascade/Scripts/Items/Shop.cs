// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;

namespace Cascade
{
    public class ShopOffer
    {
        public enum Kind { Relic, Ball, Charm, Pack }
        public Kind kind;
        public Relic relic;
        public BallInstance ball;
        public CharmType charm;
        public int basePrice;
        public bool sold;

        public string Title => kind switch
        {
            Kind.Relic => relic.Name,
            Kind.Ball => ball.Name,
            Kind.Charm => Catalog.Charm(charm).name,
            _ => "Pack Mystère"
        };

        public string Desc => kind switch
        {
            Kind.Relic => relic.Desc,
            Kind.Ball => ball.Desc,
            Kind.Charm => Catalog.Charm(charm).desc,
            _ => "Ouvre-le : choisis 1 relique parmi 3."
        };

        public string Subtitle => kind switch
        {
            Kind.Relic => "Relique · " + Palette.RarityName(relic.Rarity),
            Kind.Ball => "Bille",
            Kind.Charm => "Charme",
            _ => "Pack"
        };
    }

    /// <summary>Boutique : 2 reliques, 2 billes/charmes, 1 pack mystère. Reroll à coût croissant.</summary>
    public class Shop
    {
        public readonly List<ShopOffer> offers = new List<ShopOffer>();
        public int rerollCost;
        public List<Relic> packChoices;

        const int RerollStart = 3;
        public const int RemoveBallCost = 2;
        public const int PackPrice = 4;

        public void Open(RunState run)
        {
            rerollCost = RerollStart;
            Generate(run);
        }

        public int Price(ShopOffer o, RunState run)
        {
            int discount = run.relics.Sum(r => r.ShopDiscount);
            return System.Math.Max(1, o.basePrice - discount);
        }

        public void Generate(RunState run)
        {
            offers.Clear();
            var exclude = new List<string>();
            for (int i = 0; i < 2; i++)
            {
                var r = RelicCatalog.Roll(run, exclude);
                if (r == null) continue;
                exclude.Add(r.Id);
                offers.Add(new ShopOffer { kind = ShopOffer.Kind.Relic, relic = r, basePrice = r.Price });
            }
            for (int i = 0; i < 2; i++)
            {
                if (run.RandomValue() < 0.5)
                {
                    var kind = Catalog.ShopBalls[run.RandomInt(Catalog.ShopBalls.Length)];
                    var ed = RollEdition(run);
                    var ball = new BallInstance(kind, ed);
                    offers.Add(new ShopOffer { kind = ShopOffer.Kind.Ball, ball = ball, basePrice = Catalog.Ball(kind).price + Catalog.EditionPrice(ed) });
                }
                else
                {
                    var c = Catalog.AllCharms[run.RandomInt(Catalog.AllCharms.Length)];
                    offers.Add(new ShopOffer { kind = ShopOffer.Kind.Charm, charm = c, basePrice = Catalog.Charm(c).price });
                }
            }
            offers.Add(new ShopOffer { kind = ShopOffer.Kind.Pack, basePrice = PackPrice });
        }

        static Edition RollEdition(RunState run)
        {
            double r = run.RandomValue();
            if (r < 0.04) return Edition.Polychrome;
            if (r < 0.12) return Edition.Holo;
            if (r < 0.25) return Edition.Foil;
            return Edition.None;
        }

        public void OpenPack(RunState run)
        {
            packChoices = new List<Relic>();
            var exclude = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                // Le pack a de meilleures chances de rareté.
                double x = run.RandomValue();
                Rarity rar = x < 0.4 ? Rarity.Common : x < 0.75 ? Rarity.Uncommon : x < 0.95 ? Rarity.Rare : Rarity.Legendary;
                var r = RelicCatalog.Roll(run, exclude, rar);
                if (r == null) break;
                exclude.Add(r.Id);
                packChoices.Add(r);
            }
        }
    }
}
