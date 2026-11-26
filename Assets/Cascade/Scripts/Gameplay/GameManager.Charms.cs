// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>Charmes : utilisation, ciblage d'un pic.</summary>
    public partial class GameManager
    {
        // ================= Charmes =================

        public bool CanUseCharm(CharmType c)
        {
            var info = Catalog.Charm(c);
            if (info.duringFall) return Phase == Phase.Falling;
            if (info.roundOnly) return Phase == Phase.Aiming;
            if (info.needsPegTarget) return Phase == Phase.Aiming || Phase == Phase.Shop || Phase == Phase.SeuilSelect;
            return Phase == Phase.Aiming || Phase == Phase.Shop || Phase == Phase.SeuilSelect;
        }

        public void UseCharm(int index)
        {
            if (index < 0 || index >= Run.charms.Count) return;
            var c = Run.charms[index];
            if (!CanUseCharm(c)) { UI.Toast("Pas maintenant", Palette.Red); Juice.PlayFail(); return; }
            var info = Catalog.Charm(c);

            if (info.needsPegTarget)
            {
                pendingCharmIndex = index;
                phaseBeforeTargeting = Phase;
                SetPhase(Phase.Targeting);
                UI.ShowTargeting(info.name + " : clique sur un pic (clic droit pour annuler)");
                return;
            }

            switch (c)
            {
                case CharmType.Hourglass: ThrowsLeft++; if (drawPile.Count == 0) drawPile.Add(new BallInstance(BallKind.Standard)); break;
                case CharmType.Clone:
                    var next = PeekNextBall();
                    if (next == null) { UI.Toast("Aucune bille à cloner", Palette.Red); return; }
                    Run.bag.Add(next.Clone());
                    break;
                case CharmType.Gift:
                    if (Run.relics.Count >= Run.relicSlots) { UI.Toast("Plus de place pour une relique", Palette.Red); return; }
                    var r = RelicCatalog.Roll(Run, new List<string>(), Rarity.Common);
                    if (r != null) { Run.relics.Add(r); UI.Toast("Cadeau : " + r.Name, Palette.Cyan); }
                    break;
                case CharmType.Gamble:
                    if (Run.RandomValue() < 0.5) { int gain = Mathf.Min(20, Run.tickets); AddTickets(gain, null); UI.Toast($"Pari gagné ! +{gain}", Palette.Gold); }
                    else { int loss = Run.tickets / 2; Run.tickets -= loss; UI.Toast($"Pari perdu... -{loss}", Palette.Red); }
                    break;
                case CharmType.SlowMo: charmSlowTimer = 3f; break;
            }
            Run.charms.RemoveAt(index);
            Juice.PlayBuy();
            UI.Refresh();
            if (Phase == Phase.Shop) UI.RefreshShop();
        }

        void HandleTargeting()
        {
            if (InputShim.RightClick) { EndTargeting(false); return; }
            if (!InputShim.LeftClick || InputShim.PointerOverUI()) return;

            Vector2 world = Arcade.Instance != null ? Arcade.Instance.MouseOnBoard() : Vector2.zero;
            var peg = Board.PegAt(world);
            if (peg == null || peg.col < 0) return;

            var charm = Run.charms[pendingCharmIndex];
            PegType newType = Catalog.BrushResult(charm);
            Run.pegs[peg.row][peg.col] = newType;
            peg.SetType(newType);
            Juice.Burst(peg.transform.position, Catalog.Peg(newType).color, 20, 3f);
            Juice.PlayBuy();
            Run.charms.RemoveAt(pendingCharmIndex);
            EndTargeting(true);
        }

        void EndTargeting(bool used)
        {
            pendingCharmIndex = -1;
            SetPhase(phaseBeforeTargeting);
            UI.HideTargeting();
            UI.Refresh();
            if (Phase == Phase.Shop) UI.RefreshShop();
            if (!used) Juice.PlayClick();
        }
    }
}
