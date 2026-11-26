// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>Boutique : achats, packs, relance, vente et ordre des reliques, retrait de billes.</summary>
    public partial class GameManager
    {
        // ================= Boutique =================

        void OpenShop()
        {
            Shop.Open(Run);
            SetPhase(Phase.Shop);
            UI.ShowShop();
        }

        public bool Buy(ShopOffer o)
        {
            if (o.sold) return false;
            int price = Shop.Price(o, Run);
            if (Run.tickets < price) { Juice.PlayFail(); return false; }
            switch (o.kind)
            {
                case ShopOffer.Kind.Relic:
                    if (Run.relics.Count >= Run.relicSlots) { UI.Toast("Plus de place pour une relique (vends-en une)", Palette.Red); return false; }
                    Run.relics.Add(o.relic);
                    break;
                case ShopOffer.Kind.Ball:
                    Run.bag.Add(o.ball);
                    break;
                case ShopOffer.Kind.Charm:
                    if (Run.charms.Count >= Run.charmSlots) { UI.Toast("Plus de place pour un charme", Palette.Red); return false; }
                    Run.charms.Add(o.charm);
                    break;
                case ShopOffer.Kind.Pack:
                    // Sinon le pack est payé mais aucune relique ne peut être prise.
                    if (Run.relics.Count >= Run.relicSlots) { UI.Toast("Plus de place pour une relique (vends-en une)", Palette.Red); return false; }
                    Shop.OpenPack(Run);
                    if (Shop.packChoices.Count == 0) { UI.Toast("Plus aucune relique disponible", Palette.Red); return false; }
                    break;
            }
            Run.tickets -= price;
            o.sold = true;
            Juice.PlayBuy();
            if (o.kind == ShopOffer.Kind.Pack) UI.ShowPack();
            UI.Refresh();
            UI.RefreshShop();
            return true;
        }

        public void PickFromPack(Relic r)
        {
            if (r != null)
            {
                if (Run.relics.Count >= Run.relicSlots) { UI.Toast("Plus de place : vends une relique d'abord", Palette.Red); return; }
                Run.relics.Add(r);
                Juice.PlayBuy();
            }
            Shop.packChoices = null;
            UI.HidePack();
            UI.Refresh();
            UI.RefreshShop();
        }

        public void Reroll()
        {
            if (Run.freeRerolls > 0) Run.freeRerolls--;
            else
            {
                if (Run.tickets < Shop.rerollCost) { Juice.PlayFail(); return; }
                Run.tickets -= Shop.rerollCost;
                Shop.rerollCost++;
            }
            Shop.Generate(Run);
            Juice.PlayClick();
            UI.Refresh();
            UI.RefreshShop();
        }

        public void SellRelic(Relic r)
        {
            if (Phase != Phase.Shop || !Run.relics.Contains(r)) return;
            Run.relics.Remove(r);
            Run.tickets += r.SellValue;
            Juice.PlayCoin();
            UI.Toast($"{r.Name} vendue (+{r.SellValue})", Palette.Gold);
            UI.Refresh();
            UI.RefreshShop();
        }

        public void MoveRelic(Relic r, int dir)
        {
            int i = Run.relics.IndexOf(r);
            int j = i + dir;
            if (i < 0 || j < 0 || j >= Run.relics.Count) return;
            (Run.relics[i], Run.relics[j]) = (Run.relics[j], Run.relics[i]);
            Juice.PlayClick();
            UI.Refresh();
        }

        public bool RemoveBallFromBag(BallInstance b)
        {
            if (Phase != Phase.Shop) return false;
            if (Run.bag.Count <= 5) { UI.Toast("Le sac doit garder au moins 5 billes", Palette.Red); Juice.PlayFail(); return false; }
            if (Run.tickets < Shop.RemoveBallCost) { UI.Toast("Pas assez de tickets", Palette.Red); Juice.PlayFail(); return false; }
            Run.tickets -= Shop.RemoveBallCost;
            Run.bag.Remove(b);
            Juice.PlayBuy();
            UI.Refresh();
            UI.RefreshBag();
            UI.RefreshShop();
            return true;
        }

        public void LeaveShop()
        {
            Juice.PlayClick();
            Run.Advance();
            ShowSeuilSelect();
        }
    }
}
