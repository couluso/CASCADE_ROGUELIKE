// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>Boutique : rayons Vitrine (reliques, pack) et Comptoir (billes, charmes), services, choix d'un pack.</summary>
    public partial class GameUI
    {
        // ---------------- Boutique ----------------

        public void ShowShop()
        {
            HideOverlays();
            shopPanel.gameObject.SetActive(true);
            RefreshShop();
            Refresh();
        }

        // ---------- Boutique : laque sombre et laiton, couleurs posées (les articles ressortent, pas le décor) ----------

        static readonly Color Ink = new Color(0.1f, 0.06f, 0.07f);
        static readonly Color Cream = new Color(0.96f, 0.92f, 0.83f);
        static readonly Color ShopBg = new Color(0.065f, 0.045f, 0.06f, 0.97f);
        static readonly Color CardBg = new Color(0.12f, 0.09f, 0.11f, 1f);
        static readonly Color CardEdge = new Color(0.79f, 0.64f, 0.29f, 0.35f);
        static readonly Color SoftText = new Color(0.86f, 0.82f, 0.78f);
        static readonly Color BuyColor = new Color(0.45f, 0.13f, 0.15f);

        public void RefreshShop()
        {
            if (!shopPanel.gameObject.activeSelf) return;
            UIKit.Clear(shopPanel);

            var bg = UIKit.Panel(shopPanel, "Lacquer", ShopBg);
            UIKit.Place(bg.rectTransform, 0, 40, CenterW, 990);
            UIKit.Border(bg, Palette.Brass.WithAlpha(0.55f), 2f);
            var inner = UIKit.Panel(shopPanel, "Line", new Color(0, 0, 0, 0));
            UIKit.Place(inner.rectTransform, 14, 54, CenterW - 28, 962);
            UIKit.Border(inner, Palette.Brass.WithAlpha(0.18f), 1f);

            var title = UIKit.Label(shopPanel, "BOUTIQUE", 44, Palette.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 40, 950, 420, 60);
            var tkBox = UIKit.Panel(shopPanel, "Tickets", new Color(0.03f, 0.025f, 0.03f, 1f));
            UIKit.Place(tkBox.rectTransform, CenterW - 260, 952, 220, 54);
            UIKit.Border(tkBox, Palette.Brass.WithAlpha(0.6f), 1.5f);
            TicketIcon(tkBox.transform, 14, 13, 40, 28);
            var tk = UIKit.Led(tkBox.transform, 64, 4, 145, 46, 30, Palette.Gold, 4, letters: false);
            tk.text = gm.Run.tickets.ToString();

            var offers = gm.Shop.offers;
            var top = offers.Where(o => o.kind == ShopOffer.Kind.Relic || o.kind == ShopOffer.Kind.Pack).ToList();
            var items = offers.Where(o => o.kind == ShopOffer.Kind.Ball || o.kind == ShopOffer.Kind.Charm).ToList();

            float w = 240, gap = 26, x0 = (CenterW - (3 * w + 2 * gap)) / 2f;
            Section("VITRINE", 905);
            for (int i = 0; i < top.Count; i++) OfferCard(shopPanel, top[i], x0 + i * (w + gap), 515, w, 380);
            Section("COMPTOIR", 478);
            for (int i = 0; i < items.Count; i++) OfferCard(shopPanel, items[i], x0 + i * (w + gap), 130, w, 338);

            // Services
            float sx = x0 + 2 * (w + gap);
            string rr = gm.Run.freeRerolls > 0 ? $"RELANCER\n<size=16>gratuit x{gm.Run.freeRerolls}</size>" : $"RELANCER\n<size=16>{gm.Shop.rerollCost} tickets</size>";
            ServiceButton(rr, new Color(0.11f, 0.24f, 0.18f), () => gm.Reroll(), sx, 380, w, 88);
            ServiceButton($"SAC\n<size=16>retirer : {Shop.RemoveBallCost} tickets</size>", new Color(0.2f, 0.13f, 0.2f), () => ShowBag(), sx, 278, w, 88);
            var next = ServiceButton("SEUIL SUIVANT", BuyColor, () => gm.LeaveShop(), sx, 130, w, 134);
            UIKit.Border(next.GetComponent<Image>(), Palette.Gold.WithAlpha(0.8f), 2f);

            var hint = UIKit.Label(shopPanel, "Pinceaux et marteau s'utilisent ici, sur la planche (écran de droite).", 18, SoftText.WithAlpha(0.55f));
            UIKit.Place(hint.rectTransform, 20, 66, CenterW - 40, 44);
        }

        void Section(string text, float y)
        {
            var t = UIKit.Label(shopPanel, text, 17, Palette.Brass, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 40, y, 200, 26);
            var line = UIKit.Panel(shopPanel, "Divider", Palette.Brass.WithAlpha(0.25f), false);
            UIKit.Place(line.rectTransform, 160, y + 12, CenterW - 200, 2);
        }

        Button ServiceButton(string text, Color color, System.Action onClick, float x, float y, float w, float h)
        {
            var b = UIKit.Button(shopPanel, text, color, onClick, 22);
            UIKit.Place((RectTransform)b.transform, x, y, w, h);
            UIKit.Border(b.GetComponent<Image>(), Palette.Brass.WithAlpha(0.5f), 1.5f);
            return b;
        }

        void TicketIcon(Transform parent, float x, float y, float w, float h)
        {
            var t = Pic(parent, Sprites.Ticket, new Color(0.9f, 0.72f, 0.4f), x, y, w, h);
            t.preserveAspect = false;
        }

        void OfferCard(RectTransform parent, ShopOffer o, float x, float y, float w, float h)
        {
            RectTransform card;
            switch (o.kind)
            {
                case ShopOffer.Kind.Relic: card = JewelCase(parent, o.relic, x, y, w, h); break;
                case ShopOffer.Kind.Ball: card = BallCard(parent, o.ball, x, y, w, h); break;
                case ShopOffer.Kind.Charm: card = RaffleTicket(parent, o.charm, x, y, w, h); break;
                default: card = ScratchCard(parent, x, y, w, h); break;
            }

            string tip = o.kind switch
            {
                ShopOffer.Kind.Relic => $"<b>{o.relic.Name}</b>  ({Palette.RarityName(o.relic.Rarity)})\n{o.relic.Desc}",
                ShopOffer.Kind.Ball => $"<b>{o.ball.Name}</b>\n{o.ball.Desc}",
                ShopOffer.Kind.Charm => $"<b>{Catalog.Charm(o.charm).name}</b>\n{Catalog.CharmFullDesc(o.charm)}",
                _ => "<b>Pack Mystère</b>\nGratte-le : choisis 1 relique parmi 3 (meilleures chances de rareté)."
            };
            UIKit.Tooltip(card.gameObject, () => tip);

            int price = gm.Shop.Price(o, gm.Run);
            if (o.sold)
            {
                var veil = UIKit.Panel(card, "Sold", new Color(0, 0, 0, 0.65f));
                UIKit.Fill(veil.rectTransform);
                veil.raycastTarget = false;
                var stamp = UIKit.Label(card, "VENDU", 38, Palette.Red * 0.85f, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Fill(stamp.rectTransform);
                stamp.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
                return;
            }

            // Étiquette de prix en laiton (coin haut droit) + bouton d'achat sobre
            bool afford = gm.Run.tickets >= price;
            var tag = UIKit.Panel(card, "PriceTag", afford ? new Color(0.2f, 0.15f, 0.08f, 0.95f) : new Color(0.12f, 0.1f, 0.1f, 0.95f));
            UIKit.Place(tag.rectTransform, w - 86, h - 44, 76, 34);
            UIKit.Border(tag, afford ? Palette.Brass.WithAlpha(0.8f) : Palette.Dim, 1.5f);
            TicketIcon(tag.transform, 6, 8, 26, 18);
            var pt = UIKit.Label(tag.transform, price.ToString(), 20, afford ? Palette.Gold : Palette.Dim, TextAnchor.MiddleRight, FontStyle.Bold);
            UIKit.Place(pt.rectTransform, 30, 0, 38, 34);

            var b = UIKit.Button(card, afford ? "ACHETER" : "PAS ASSEZ", afford ? BuyColor : new Color(0.16f, 0.14f, 0.15f), () => gm.Buy(o), 19);
            UIKit.Place((RectTransform)b.transform, 16, 14, w - 32, 46);
            UIKit.Border(b.GetComponent<Image>(), afford ? Palette.Brass.WithAlpha(0.6f) : Palette.Dim.WithAlpha(0.4f), 1.5f);
            if (!afford) b.GetComponentInChildren<Text>().color = Palette.Dim * 1.4f;
        }

        Image Pic(Transform parent, Sprite sp, Color c, float x, float y, float w, float h)
        {
            var img = UIKit.Panel(parent, "Img", c, false);
            img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true;
            img.raycastTarget = false;
            UIKit.Place(img.rectTransform, x, y, w, h);
            return img;
        }

        RectTransform ShopCard(RectTransform parent, string name, Color bg, Color edge, float x, float y, float w, float h)
        {
            var img = UIKit.Panel(parent, name, bg);
            UIKit.Border(img, edge, 1.5f);
            return UIKit.Place(img.rectTransform, x, y, w, h);
        }

        /// <summary>Relique : écrin en velours, le pendentif accroché au crochet en laiton.</summary>
        RectTransform JewelCase(RectTransform parent, Relic r, float x, float y, float w, float h, bool withButtonSpace = true)
        {
            var rc = Palette.ForRarity(r.Rarity);
            var card = ShopCard(parent, "JewelCase", CardBg, Color.Lerp(CardEdge, rc, 0.35f), x, y, w, h);

            // Intérieur de l'écrin
            var velvet = UIKit.Panel(card, "Velvet", new Color(0.2f, 0.07f, 0.12f, 1f));
            UIKit.Place(velvet.rectTransform, 12, h - 182, w - 24, 170);
            velvet.raycastTarget = false;
            Pic(velvet.transform, Sprites.Glow, rc.WithAlpha(0.18f), (w - 24) / 2 - 85, 0, 170, 170);
            var hook = UIKit.Panel(velvet.transform, "Hook", Palette.Brass, false);
            UIKit.Place(hook.rectTransform, (w - 24) / 2 - 18, 160, 36, 4);

            var pendant = RelicArt.Build(velvet.transform, r, 96f);
            var prt = (RectTransform)pendant.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -8f);
            pendant.GetComponent<Image>().raycastTarget = false;

            var rarity = UIKit.Label(card, Palette.RarityName(r.Rarity).ToUpper(), 14, rc * 0.9f, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(rarity.rectTransform, 16, h - 40, w - 110, 24);
            var name = UIKit.Label(card, r.Name, 21, Cream, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 14, h - 240, w - 28, 54);
            var desc = UIKit.Label(card, r.Desc, 17, SoftText.WithAlpha(0.85f), TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, 16, withButtonSpace ? 66 : 20, w - 32, h - (withButtonSpace ? 306 : 260));
            return card;
        }

        /// <summary>Bille : la vraie bille posée sous un spot, sur un socle de feutre.</summary>
        RectTransform BallCard(RectTransform parent, BallInstance b, float x, float y, float w, float h)
        {
            var info = Catalog.Ball(b.kind);
            var card = ShopCard(parent, "BallCard", CardBg, Color.Lerp(CardEdge, info.color, 0.3f), x, y, w, h);
            var plate = UIKit.Panel(card, "Felt", new Color(0.05f, 0.13f, 0.1f, 1f));
            UIKit.Place(plate.rectTransform, 12, h - 182, w - 24, 170);
            plate.raycastTarget = false;
            Pic(plate.transform, Sprites.Glow, Color.Lerp(Color.white, info.color, 0.4f).WithAlpha(0.22f), (w - 24) / 2 - 90, -10, 180, 180);
            var ball = RelicArt.BallIcon(plate.transform, b, 130f);
            ball.anchorMin = ball.anchorMax = new Vector2(0.5f, 0.5f);
            ball.anchoredPosition = new Vector2(0f, 4f);

            var tag = UIKit.Label(card, b.edition != Edition.None ? $"BILLE · {Catalog.EditionName(b.edition).ToUpper()}" : "BILLE", 14,
                b.edition != Edition.None ? Catalog.EditionColor(b.edition) : Palette.Brass, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(tag.rectTransform, 16, h - 40, w - 110, 24);
            var name = UIKit.Label(card, info.name, 21, Cream, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 14, h - 238, w - 28, 50);
            var desc = UIKit.Label(card, info.desc, 17, SoftText.WithAlpha(0.85f), TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, 16, 66, w - 32, h - 300);
            return card;
        }

        /// <summary>Charme : ticket de tombola crème posé de biais.</summary>
        RectTransform RaffleTicket(RectTransform parent, CharmType c, float x, float y, float w, float h)
        {
            var info = Catalog.Charm(c);
            var card = ShopCard(parent, "CharmCard", CardBg, CardEdge, x, y, w, h);
            var t = Pic(card, Sprites.Ticket, new Color(0.86f, 0.74f, 0.56f), 14, h - 178, w - 28, 150);
            t.preserveAspect = false;
            t.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            var stub = UIKit.Label(t.transform, "ENTRÉE", 13, Ink.WithAlpha(0.7f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(stub.rectTransform, -16, 63, 90, 24);
            stub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var num = UIKit.Label(t.transform, $"N° {(int)c + 1:000}", 15, Ink.WithAlpha(0.6f), TextAnchor.UpperRight);
            UIKit.Place(num.rectTransform, 60, 112, w - 110, 24);
            var nm = UIKit.Label(t.transform, info.name, 20, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(nm.rectTransform, 56, 30, w - 100, 80);

            var tag = UIKit.Label(card, "CHARME", 14, Palette.Pink * 0.9f, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(tag.rectTransform, 16, h - 40, w - 110, 24);
            var desc = UIKit.Label(card, info.desc, 17, SoftText.WithAlpha(0.85f), TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, 16, 66, w - 32, h - 258);
            return card;
        }

        /// <summary>Pack mystère : carte à gratter dorée, patine sombre.</summary>
        RectTransform ScratchCard(RectTransform parent, float x, float y, float w, float h)
        {
            var card = ShopCard(parent, "ScratchCard", CardBg, Color.Lerp(CardEdge, Palette.Gold, 0.4f), x, y, w, h);
            var gold = UIKit.Panel(card, "Gold", new Color(0.55f, 0.42f, 0.18f, 1f));
            UIKit.Place(gold.rectTransform, 12, h - 182, w - 24, 170);
            gold.raycastTarget = false;
            var silver = UIKit.Panel(gold.transform, "Silver", new Color(0.62f, 0.63f, 0.66f));
            UIKit.Place(silver.rectTransform, 18, 18, w - 60, 134);
            silver.raycastTarget = false;
            for (int i = 0; i < 3; i++)
            {
                var q = UIKit.Label(silver.transform, "?", 40, new Color(0.42f, 0.43f, 0.47f), TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Place(q.rectTransform, 6 + i * ((w - 72) / 3f), 40, (w - 72) / 3f, 60);
            }
            var scratch = UIKit.Label(silver.transform, "GRATTER ICI", 15, Ink.WithAlpha(0.6f), TextAnchor.LowerCenter, FontStyle.Bold);
            UIKit.Place(scratch.rectTransform, 0, 8, w - 60, 30);

            var tag = UIKit.Label(card, "PACK", 14, Palette.Gold * 0.9f, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(tag.rectTransform, 16, h - 40, w - 110, 24);
            var name = UIKit.Label(card, "Pack Mystère", 21, Cream, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 14, h - 240, w - 28, 50);
            var sub = UIKit.Label(card, "Choisis 1 relique parmi 3, avec de meilleures chances de rareté.", 17, SoftText.WithAlpha(0.85f), TextAnchor.UpperCenter);
            UIKit.Place(sub.rectTransform, 16, 66, w - 32, h - 306);
            return card;
        }

        public void ShowPack()
        {
            UIKit.Clear(packPanel);
            packPanel.gameObject.SetActive(true);
            var dim = UIKit.Panel(packPanel, "Dim", new Color(0, 0, 0, 0.75f), false);
            UIKit.Fill(dim.rectTransform);
            var t = UIKit.Label(packPanel, "CHOISIS UN PENDENTIF", 40, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 860, CenterW, 64);
            UIKit.Neon(t, Palette.Gold);
            var choices = gm.Shop.packChoices ?? new List<Relic>();
            float w = 250, gap = 22, x0 = (CenterW - (3 * w + 2 * gap)) / 2f;
            for (int i = 0; i < choices.Count; i++)
            {
                var r = choices[i];
                var card = JewelCase(packPanel, r, x0 + i * (w + gap), 380, w, 440);
                UIKit.Tooltip(card.gameObject, () => $"<b>{r.Name}</b>\n{r.Desc}");
                var b = UIKit.Button(card, "PRENDRE", BuyColor, () => gm.PickFromPack(r), 22);
                UIKit.Place((RectTransform)b.transform, 16, 14, w - 32, 50);
                UIKit.Border(b.GetComponent<Image>(), Palette.Brass.WithAlpha(0.6f), 1.5f);
            }
            var skip = UIKit.Button(packPanel, "Ne rien prendre", Palette.Dim, () => gm.PickFromPack(null), 22);
            UIKit.Place((RectTransform)skip.transform, CenterW / 2 - 150, 280, 300, 60);
        }

        public void HidePack() => packPanel.gameObject.SetActive(false);
    }
}
