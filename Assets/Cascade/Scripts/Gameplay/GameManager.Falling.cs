// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>Chute des billes : rebonds sur les pics et murs, arrivée dans une case, décompte du score.</summary>
    public partial class GameManager
    {
        // ================= Événements de chute =================

        public void OnPegHit(BallController ball, Peg peg)
        {
            var c = ball.ctx;
            peg.Hit();
            if (peg.type == PegType.WallBump) return;
            c.pegHits++;
            Vector3 at = peg.transform.position;

            if (!peg.disabled)
            {
                double chipMul = 1;
                if (Run.HasBoss(BossType.Banker)) chipMul *= 2;
                if (Run.HasBoss(BossType.Colossus)) chipMul *= 5;

                switch (peg.type)
                {
                    case PegType.Standard: c.AddChips(Catalog.StandardChips * chipMul, at); break;
                    case PegType.Multi: c.AddMult(1, at); break;
                    case PegType.Gold: c.AddChips(Catalog.GoldChips * chipMul, at); AddTickets(1, null); Juice.PlayCoin(); break;
                    case PegType.Bumper: c.AddChips(Catalog.BumperChips * chipMul, at); ball.ApplyBumper(at); Juice.PlayBumper(); Juice.Shake(0.08f + 0.2f * Heat(c)); break;
                    case PegType.Ice: c.AddChips(Catalog.IceChips * chipMul, at); break;
                    case PegType.Spiky: c.AddChips(Catalog.SpikyChips * chipMul, at); c.AddMult(-1, at); break;
                }

                if (peg.burning > 0) c.AddChips(Catalog.BurningBonusChips * chipMul, at);
                if (peg.JustBroke && peg.type == PegType.Ice) c.MulMult(2, at);

                switch (c.ball.kind)
                {
                    case BallKind.Heavy: c.chips += 3 * chipMul; break;
                    case BallKind.Light: if (c.pegHits > 10) c.mult += 0.5; break;
                    case BallKind.Fire: peg.Ignite(3); break;
                }

                foreach (var r in Run.relics.ToList()) r.OnPegHit(this, c, peg);
            }

            // Musique : la note monte avec le score du lancer (pas de motif qui tourne en boucle),
            // et sons, particules et secousse s'intensifient avec lui.
            float heat = Heat(c);
            int note = Mathf.Clamp(Mathf.FloorToInt(heat * 12f) + Random.Range(0, 3), 0, 14);
            Juice.PegNote(note, peg.type, peg.disabled, heat);
            Juice.Burst(at, Catalog.Peg(peg.type).color, peg.disabled ? 2 : 4 + Mathf.RoundToInt(heat * 16f), 1.6f + heat * 3f);
            if (!peg.disabled) Juice.Shake(0.015f + 0.1f * heat * heat);
            UI.Refresh();
        }

        public void OnWallHit(BallController ball)
        {
            var c = ball.ctx;
            if (c.ball.kind == BallKind.Bouncy) c.AddMult(2, ball.transform.position);
            foreach (var r in Run.relics.ToList()) r.OnWallHit(this, c);
            Juice.PlayWall(Heat(c));
            UI.Refresh();
        }

        public void OnDirectionChange(BallController ball)
        {
            foreach (var r in Run.relics.ToList()) r.OnDirectionChange(this, ball.ctx);
        }

        /// <summary>Fin d'une bille : case atteinte (slot >= 0) ou perdue.</summary>
        public void ResolveBall(BallController ball, int slot, bool lost, string reason = null, bool keepChipsAndMult = false)
        {
            var c = ball.ctx;
            if (c.resolved) return;
            c.resolved = true;
            Vector3 at = ball.transform.position;

            if (lost)
            {
                c.slotIndex = -1;
                c.slotValue = 1;
                c.finalScore = keepChipsAndMult ? c.chips * c.mult : 0;
                UI.Toast(reason ?? "Bille perdue", Palette.Red);
                Juice.PlayFail();
            }
            else
            {
                c.slotIndex = slot;
                if (c.ball.kind == BallKind.Golden)
                {
                    AddTickets(Catalog.GoldenTickets, null);
                    c.slotValue = 1;
                    c.finalScore = c.chips;
                    Juice.PlayCoin();
                }
                else
                {
                    if (c.ball.edition == Edition.Polychrome) c.MulMult(1.5, at);
                    if (c.ball.kind == BallKind.Crystal) c.FinalX(3, at);
                    foreach (var r in Run.relics.ToList()) r.OnBallLanded(this, c, slot);
                    c.slotValue = Board.EffectiveSlotValue(slot, this);
                    c.finalScore = c.chips * c.mult * c.slotValue * c.xmult;
                }
                Board.FlashSlot(slot);
            }

            c.finalScore = System.Math.Floor(c.finalScore);
            Run.bestBallScore = System.Math.Max(Run.bestBallScore, c.finalScore);

            if (!lost) Juice.CelebrateBall(c, Run.Target, at);

            activeBalls.Remove(ball);
            Destroy(ball.gameObject, 0.05f);

            if (Run.HasBoss(BossType.Cheater)) Board.ShuffleSlots(Run);
            if (c.finalScore > 0) pendingTallies.Enqueue(c);
            else LastResolved = c;
            UI.Refresh();
            resolveDelay = 0.4f;
        }
    }
}
