// CASCADE — Auteur : Coulu
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Relique (équivalent des Jokers). Chaque crochet est appelé dans l'ordre des reliques (gauche → droite).
    /// Appelle Fire(gm) quand la relique agit : son icône clignote dans l'interface.
    /// </summary>
    public abstract class Relic
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Desc { get; }
        public abstract Rarity Rarity { get; }
        public virtual int Price => Rarity switch { Rarity.Common => 5, Rarity.Uncommon => 7, Rarity.Rare => 9, _ => 14 };
        public int SellValue => Mathf.Max(1, Price / 2);
        /// <summary>Texte d'état affiché sous la relique (compteur, alternance...).</summary>
        public virtual string Status => null;

        public virtual void OnRoundStart(GameManager gm) { }
        public virtual void OnBallLaunched(GameManager gm, ScoreContext c) { }
        public virtual void OnPegHit(GameManager gm, ScoreContext c, Peg p) { }
        public virtual void OnWallHit(GameManager gm, ScoreContext c) { }
        public virtual void OnDirectionChange(GameManager gm, ScoreContext c) { }
        /// <summary>Juste avant le calcul final : ajouter jetons / mult / x.</summary>
        public virtual void OnBallLanded(GameManager gm, ScoreContext c, int slot) { }
        public virtual void OnSeuilCleared(GameManager gm) { }

        public virtual float SlotValueMultiplier => 1f;
        public virtual int ShopDiscount => 0;
        public virtual int ExtraPreviewBounces => 0;
        public virtual float TimeScale => 1f;
        public virtual bool GrantsBrake => false;

        protected void Fire(GameManager gm) => gm.NotifyRelicFired(this);
        protected static Vector3 At(ScoreContext c) => c.controller != null ? c.controller.transform.position : Vector3.zero;
    }

    public static class RelicCatalog
    {
        static readonly List<Func<Relic>> factories = new List<Func<Relic>>
        {
            () => new PiggyBank(), () => new Broker(), () => new Chaos(), () => new Tightrope(), () => new Pendulum(),
            () => new Hourglass(), () => new ShootingStar(), () => new ChipForge(), () => new Collector(),
            () => new Perfectionist(), () => new Minimalist(), () => new Maximalist(), () => new Frost(),
            () => new Cauldron(), () => new Chime(), () => new Compass(), () => new Brake(),
            () => new GrandPachinko(), () => new Clockmaker(), () => new TwinRelic()
        };

        static List<Relic> prototypes;
        public static IReadOnlyList<Relic> All => prototypes ??= factories.Select(f => f()).ToList();

        public static Relic Create(string id)
        {
            foreach (var f in factories) { var r = f(); if (r.Id == id) return r; }
            return null;
        }

        /// <summary>Tire une relique non possédée. Poids : commune 60, peu commune 28, rare 10, légendaire 2.</summary>
        public static Relic Roll(RunState run, ICollection<string> exclude, Rarity? forced = null)
        {
            Rarity rarity;
            if (forced.HasValue) rarity = forced.Value;
            else
            {
                double r = run.RandomValue() * 100;
                rarity = r < 60 ? Rarity.Common : r < 88 ? Rarity.Uncommon : r < 98 ? Rarity.Rare : Rarity.Legendary;
            }
            var owned = new HashSet<string>(run.relics.Select(x => x.Id));
            var candidates = All.Where(x => x.Rarity == rarity && !owned.Contains(x.Id) && !exclude.Contains(x.Id)).ToList();
            if (candidates.Count == 0) candidates = All.Where(x => !owned.Contains(x.Id) && !exclude.Contains(x.Id)).ToList();
            if (candidates.Count == 0) return null;
            return Create(candidates[run.RandomInt(candidates.Count)].Id);
        }
    }

    // ================= Économie =================

    public class PiggyBank : Relic
    {
        public override string Id => "piggy";
        public override string Name => "La Tirelire";
        public override string Desc => "+1 ticket par seuil franchi, +1 de plus par tranche de 10 tickets en réserve.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnSeuilCleared(GameManager gm)
        {
            gm.AddTickets(1 + gm.Run.tickets / 10, "Tirelire");
            Fire(gm);
        }
    }

    public class Broker : Relic
    {
        public override string Id => "broker";
        public override string Name => "Le Courtier";
        public override string Desc => "Tous les articles de la boutique coûtent 1 ticket de moins.";
        public override Rarity Rarity => Rarity.Common;
        public override int ShopDiscount => 1;
    }

    // ================= Score =================

    public class Chaos : Relic
    {
        public override string Id => "chaos";
        public override string Name => "Le Chaos";
        public override string Desc => "+0,5 mult chaque fois que la bille change de direction.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnDirectionChange(GameManager gm, ScoreContext c) { c.AddMult(0.5, At(c)); Fire(gm); }
    }

    public class Tightrope : Relic
    {
        public override string Id => "tightrope";
        public override string Name => "Le Funambule";
        public override string Desc => "x2 si la bille touche un mur de la planche.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            if (c.touchedWall) { c.FinalX(2, At(c)); Fire(gm); }
        }
    }

    public class Pendulum : Relic
    {
        public override string Id => "pendulum";
        public override string Name => "Le Pendule";
        public override string Desc => "Lancers impairs : x2. Lancers pairs : x0,75.";
        public override Rarity Rarity => Rarity.Common;
        public override string Status => GameManager.Instance != null && GameManager.Instance.NextThrowIndex % 2 == 1 ? "prochain : x2" : "prochain : x0,75";
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            c.FinalX(c.throwIndex % 2 == 1 ? 2 : 0.75, At(c));
            Fire(gm);
        }
    }

    public class Hourglass : Relic
    {
        public override string Id => "hourglass";
        public override string Name => "Le Sablier";
        public override string Desc => "+2 mult par seconde de chute au-delà de 3 secondes.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            float extra = c.airTime - 3f;
            if (extra > 0f) { c.AddMult(Math.Round(extra * 2, 1), At(c)); Fire(gm); }
        }
    }

    public class ShootingStar : Relic
    {
        public override string Id => "star";
        public override string Name => "L'Étoile Filante";
        public override string Desc => "+4 mult pour chaque bille.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot) { c.AddMult(4, At(c)); Fire(gm); }
    }

    public class ChipForge : Relic
    {
        public override string Id => "forge";
        public override string Name => "La Forge à Jetons";
        public override string Desc => "Les pics standard donnent +3 jetons de plus.";
        public override Rarity Rarity => Rarity.Common;
        public override void OnPegHit(GameManager gm, ScoreContext c, Peg p)
        {
            if (p.type == PegType.Standard) { c.chips += 3; Fire(gm); }
        }
    }

    public class Collector : Relic
    {
        public override string Id => "collector";
        public override string Name => "Le Collectionneur";
        public override string Desc => "+2 mult par relique possédée.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot) { c.AddMult(2 * gm.Run.relics.Count, At(c)); Fire(gm); }
    }

    public class Perfectionist : Relic
    {
        public override string Id => "perfect";
        public override string Name => "Le Perfectionniste";
        public override string Desc => "x3 tant qu'aucune bille n'est tombée dans la case centrale pendant la manche.";
        public override Rarity Rarity => Rarity.Uncommon;
        bool broken;
        public override string Status => broken ? "raté cette manche" : "actif";
        public override void OnRoundStart(GameManager gm) => broken = false;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            if (slot == BoardLayout.CenterSlot) broken = true;
            if (!broken) { c.FinalX(3, At(c)); Fire(gm); }
        }
    }

    public class Minimalist : Relic
    {
        public override string Id => "minimal";
        public override string Name => "Le Minimaliste";
        public override string Desc => "x4 si le sac contient 8 billes ou moins.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override string Status => GameManager.Instance != null ? $"sac : {GameManager.Instance.Run.bag.Count}" : null;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            if (gm.Run.bag.Count <= 8) { c.FinalX(4, At(c)); Fire(gm); }
        }
    }

    public class Maximalist : Relic
    {
        public override string Id => "maximal";
        public override string Name => "Le Maximaliste";
        public override string Desc => "+1 mult par bille dans le sac au-delà de 12.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot)
        {
            int extra = gm.Run.bag.Count - 12;
            if (extra > 0) { c.AddMult(extra, At(c)); Fire(gm); }
        }
    }

    public class Frost : Relic
    {
        public override string Id => "frost";
        public override string Name => "Le Givre";
        public override string Desc => "Famille Glace : chaque pic gelé brisé ajoute +3 mult.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnPegHit(GameManager gm, ScoreContext c, Peg p)
        {
            if (p.JustBroke && (p.type == PegType.Ice || p.fragile)) { c.AddMult(3, p.transform.position); Fire(gm); }
        }
    }

    public class Cauldron : Relic
    {
        public override string Id => "cauldron";
        public override string Name => "Le Chaudron";
        public override string Desc => "Famille Feu : chaque pic enflammé touché donne x1,2 mult.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnPegHit(GameManager gm, ScoreContext c, Peg p)
        {
            if (p.burning > 0) { c.MulMult(1.2, p.transform.position); Fire(gm); }
        }
    }

    public class Chime : Relic
    {
        public override string Id => "chime";
        public override string Name => "Le Carillon";
        public override string Desc => "Les pics multiplicateurs donnent +1 mult de plus.";
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnPegHit(GameManager gm, ScoreContext c, Peg p)
        {
            if (p.type == PegType.Multi && !p.disabled) { c.mult += 1; Fire(gm); }
        }
    }

    // ================= Contrôle =================

    public class Compass : Relic
    {
        public override string Id => "compass";
        public override string Name => "La Boussole";
        public override string Desc => "Affiche une trajectoire fantôme beaucoup plus longue.";
        public override Rarity Rarity => Rarity.Rare;
        public override int ExtraPreviewBounces => 10;
    }

    public class Brake : Relic
    {
        public override string Id => "brake";
        public override string Name => "Le Frein";
        public override string Desc => "Une fois par manche, [F] fige la bille 1 seconde en l'air.";
        public override Rarity Rarity => Rarity.Rare;
        public override bool GrantsBrake => true;
    }

    // ================= Légendaires =================

    public class GrandPachinko : Relic
    {
        public override string Id => "pachinko";
        public override string Name => "Le Grand Pachinko";
        public override string Desc => "Tous les multiplicateurs de cases sont doublés.";
        public override Rarity Rarity => Rarity.Legendary;
        public override float SlotValueMultiplier => 2f;
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot) => Fire(gm);
    }

    public class Clockmaker : Relic
    {
        public override string Id => "clock";
        public override string Name => "L'Horloger";
        public override string Desc => "Le temps ralentit de 30 % en permanence et la trajectoire fantôme s'allonge.";
        public override Rarity Rarity => Rarity.Legendary;
        public override float TimeScale => 0.7f;
        public override int ExtraPreviewBounces => 3;
    }

    /// <summary>Copie la relique à sa droite.</summary>
    public class TwinRelic : Relic
    {
        public override string Id => "twin";
        public override string Name => "Le Jumeau";
        public override string Desc => "Copie l'effet de la relique à sa droite.";
        public override Rarity Rarity => Rarity.Legendary;

        Relic Right
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return null;
                int i = gm.Run.relics.IndexOf(this);
                if (i < 0 || i + 1 >= gm.Run.relics.Count) return null;
                var r = gm.Run.relics[i + 1];
                return r is TwinRelic ? null : r;
            }
        }

        public override string Status => Right != null ? "copie : " + Right.Name : "rien à copier";
        public override void OnRoundStart(GameManager gm) => Right?.OnRoundStart(gm);
        public override void OnBallLaunched(GameManager gm, ScoreContext c) => Right?.OnBallLaunched(gm, c);
        public override void OnPegHit(GameManager gm, ScoreContext c, Peg p) => Right?.OnPegHit(gm, c, p);
        public override void OnWallHit(GameManager gm, ScoreContext c) => Right?.OnWallHit(gm, c);
        public override void OnDirectionChange(GameManager gm, ScoreContext c) => Right?.OnDirectionChange(gm, c);
        public override void OnBallLanded(GameManager gm, ScoreContext c, int slot) { if (Right != null) { Right.OnBallLanded(gm, c, slot); Fire(gm); } }
        public override void OnSeuilCleared(GameManager gm) => Right?.OnSeuilCleared(gm);
        public override float SlotValueMultiplier => Right?.SlotValueMultiplier ?? 1f;
        public override int ShopDiscount => Right?.ShopDiscount ?? 0;
        public override int ExtraPreviewBounces => Right?.ExtraPreviewBounces ?? 0;
        public override float TimeScale => Right?.TimeScale ?? 1f;
        public override bool GrantsBrake => Right?.GrantsBrake ?? false;
    }
}
