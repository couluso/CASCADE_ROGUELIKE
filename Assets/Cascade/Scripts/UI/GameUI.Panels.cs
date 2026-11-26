// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>Panneaux latéraux : à gauche seuil, score et ressources ; à droite présentoir des reliques, charmes, prochaine bille.</summary>
    public partial class GameUI
    {
        // ================================================================== GAUCHE

        void BuildLeft()
        {
            var p = UIKit.Fill(UIKit.Panel(leftRoot, "Left", new Color(0.03f, 0.025f, 0.035f, 0.55f)).rectTransform);
            leftPanel = p;

            var title = UIKit.Label(p, "CASCADE", 64, Palette.Pink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 950, 500, 80);
            UIKit.Neon(title, Palette.Pink);

            actText = UIKit.Label(p, "", 26, Palette.Cyan);
            UIKit.Place(actText.rectTransform, 0, 915, 500, 36);
            seuilText = UIKit.Label(p, "", 30, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(seuilText.rectTransform, 0, 878, 500, 40);

            bossBox = UIKit.Panel(p, "Boss", Palette.Red.WithAlpha(0.18f));
            UIKit.Place(bossBox.rectTransform, 20, 770, 460, 100);
            UIKit.Border(bossBox, Palette.Red.WithAlpha(0.7f));
            bossName = UIKit.Label(bossBox.transform, "", 26, Palette.Red, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(bossName.rectTransform, 10, 50, 440, 40);
            bossDesc = UIKit.Label(bossBox.transform, "", 20, Palette.White, TextAnchor.UpperCenter);
            UIKit.Place(bossDesc.rectTransform, 10, 5, 440, 50);

            var targetBox = UIKit.Panel(p, "Target", Palette.PanelLight);
            UIKit.Place(targetBox.rectTransform, 20, 580, 460, 180);
            var tl = UIKit.Label(targetBox.transform, "SCORE CIBLE", 20, Palette.Dim * 1.6f);
            UIKit.Place(tl.rectTransform, 0, 140, 460, 30);
            targetText = UIKit.Led(targetBox.transform, 30, 92, 400, 50, 38, Palette.Gold, 8);
            var bar = UIKit.Panel(targetBox.transform, "Bar", new Color(0, 0, 0, 0.5f));
            UIKit.Place(bar.rectTransform, 20, 70, 420, 16);
            progressFill = UIKit.Panel(bar.transform, "Fill", Palette.Gold);
            progressFill.rectTransform.anchorMin = Vector2.zero;
            progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            progressFill.rectTransform.offsetMin = progressFill.rectTransform.offsetMax = Vector2.zero;
            roundScoreText = UIKit.Led(targetBox.transform, 30, 10, 400, 56, 42, new Color(1f, 0.55f, 0.2f), 8);

            // Jetons x Mult (façon Balatro)
            chipsBox = UIKit.Panel(p, "Chips", new Color(0.02f, 0.04f, 0.08f, 0.95f));
            UIKit.Border(chipsBox, Palette.Chips, 3f);
            UIKit.Place(chipsBox.rectTransform, 20, 455, 205, 100);
            chipsText = UIKit.Led(chipsBox.transform, 8, 22, 185, 64, 36, new Color(0.35f, 0.7f, 1f), 5);
            var cl = UIKit.Label(chipsBox.transform, "jetons", 16, Palette.White.WithAlpha(0.8f), TextAnchor.LowerCenter);
            UIKit.Fill(cl.rectTransform, 4);
            var x = UIKit.Label(p, "x", 40, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(x.rectTransform, 225, 455, 50, 100);
            multBox = UIKit.Panel(p, "Mult", new Color(0.08f, 0.02f, 0.03f, 0.95f));
            UIKit.Border(multBox, Palette.Mult, 3f);
            UIKit.Place(multBox.rectTransform, 275, 455, 205, 100);
            multText = UIKit.Led(multBox.transform, 8, 22, 185, 64, 36, new Color(1f, 0.3f, 0.3f), 5);
            var ml = UIKit.Label(multBox.transform, "mult", 16, Palette.White.WithAlpha(0.8f), TextAnchor.LowerCenter);
            UIKit.Fill(ml.rectTransform, 4);

            resultText = UIKit.Label(p, "", 24, Palette.Gold, TextAnchor.MiddleCenter);
            UIKit.Place(resultText.rectTransform, 10, 400, 480, 50);

            // Ressources
            ballsText = Stat(p, 20, 300, "BILLES", Palette.Cyan, out _);
            relancesText = Stat(p, 175, 300, "RELANCES", Palette.Pink, out _);
            ticketsText = Stat(p, 330, 300, "TICKETS", Palette.Gold, out _);

            var tiltLabel = UIKit.Label(p, "TILT", 18, Palette.Red, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(tiltLabel.rectTransform, 24, 262, 80, 24);
            var tiltBar = UIKit.Panel(p, "TiltBar", new Color(0, 0, 0, 0.5f));
            UIKit.Place(tiltBar.rectTransform, 90, 266, 390, 16);
            tiltFill = UIKit.Panel(tiltBar.transform, "Fill", Palette.Red);
            tiltFill.rectTransform.anchorMin = Vector2.zero;
            tiltFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            tiltFill.rectTransform.offsetMin = tiltFill.rectTransform.offsetMax = Vector2.zero;

            windText = UIKit.Label(p, "", 22, Palette.Ice, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(windText.rectTransform, 0, 228, 500, 30);

            helpText = UIKit.Label(p,
                "<color=#19E6FF>Clic / Espace</color> lâcher (le lanceur va et vient)\n" +
                "<color=#19E6FF>A / D</color> coup de pouce (1 par bille)\n" +
                "<color=#19E6FF>R</color> secousse (attention au TILT)\n" +
                "<color=#19E6FF>E / clic droit</color> relance avant le 1er pic\n" +
                "<color=#19E6FF>F</color> frein (avec la relique)",
                19, Palette.White.WithAlpha(0.75f), TextAnchor.UpperLeft);
            UIKit.Place(helpText.rectTransform, 30, 30, 450, 190);
        }

        Text Stat(Transform p, float x, float y, string label, Color color, out Image box)
        {
            box = UIKit.Panel(p, label, Palette.PanelLight);
            UIKit.Place(box.rectTransform, x, y, 150, 80);
            var l = UIKit.Label(box.transform, label, 16, color, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(l.rectTransform, 0, 50, 150, 26);
            var v = UIKit.Led(box.transform, 12, 4, 126, 50, 30, color, 3, letters: false);
            return v;
        }

        // ================================================================== DROITE

        void BuildRight()
        {
            var p = UIKit.Fill(UIKit.Panel(rightRoot, "Right", new Color(0.03f, 0.025f, 0.035f, 0.55f)).rectTransform);

            relicHeader = UIKit.Label(p, "RELIQUES", 26, Palette.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(relicHeader.rectTransform, 24, 985, 450, 40);
            // Présentoir à bijoux : les reliques sont des pendentifs accrochés à une tringle en laiton
            relicList = UIKit.Place(UIKit.Rect(p, "Relics"), 20, 470, 460, 510);

            charmHeader = UIKit.Label(p, "CHARMES", 26, Palette.Pink, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(charmHeader.rectTransform, 24, 420, 450, 40);
            charmList = UIKit.Place(UIKit.Rect(p, "Charms"), 20, 290, 460, 125);
            UIKit.VList(charmList, 8).childAlignment = TextAnchor.UpperCenter;

            var nb = UIKit.Panel(p, "NextBall", Palette.PanelLight);
            UIKit.Place(nb.rectTransform, 20, 110, 460, 165);
            var nl = UIKit.Label(nb.transform, "PROCHAINE BILLE", 18, Palette.Dim * 1.8f, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(nl.rectTransform, 0, 130, 460, 28);
            nextBallName = UIKit.Label(nb.transform, "", 26, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(nextBallName.rectTransform, 10, 92, 440, 36);
            nextBallDesc = UIKit.Label(nb.transform, "", 19, Palette.White.WithAlpha(0.8f), TextAnchor.UpperCenter);
            UIKit.Place(nextBallDesc.rectTransform, 15, 5, 430, 85);

            var bagBtn = UIKit.Button(p, "Voir le sac", Palette.Violet, () => ShowBag(), 24);
            UIKit.Place((RectTransform)bagBtn.transform, 20, 30, 460, 64);
            bagText = bagBtn.GetComponentInChildren<Text>();
        }

        const int PendantsPerRow = 5;
        const float PendantRowH = 252f;

        void RebuildRelics()
        {
            UIKit.Clear(relicList);
            relicPendants.Clear();
            relicStatus.Clear();
            var run = gm.Run;
            if (knownRun != run) { knownRun = run; knownRelics.Clear(); }
            relicHeader.text = $"RELIQUES  <color=#5A4A7A>{run.relics.Count}/{run.relicSlots}</color>";
            bool inShop = gm.Phase == Phase.Shop;
            float slotW = 460f / PendantsPerRow;
            int rows = Mathf.Max(1, Mathf.CeilToInt(run.relicSlots / (float)PendantsPerRow));

            // Fond de velours et tringles
            var velvet = UIKit.Panel(relicList, "Velvet", new Color(0.09f, 0.04f, 0.07f, 0.9f));
            UIKit.Fill(velvet.rectTransform);
            UIKit.Border(velvet, Palette.Brass.WithAlpha(0.35f), 2f);
            for (int row = 0; row < rows; row++)
            {
                float top = 510f - row * PendantRowH - 14f;
                var rod = UIKit.Panel(relicList, "Rod", Palette.Brass, false);
                UIKit.Place(rod.rectTransform, 10, top - 4, 440, 6);
                var shine = UIKit.Panel(rod.transform, "Shine", new Color(1f, 0.95f, 0.75f, 0.6f), false);
                UIKit.Place(shine.rectTransform, 0, 4, 440, 2);
                for (int k = 0; k < 2; k++)
                {
                    var knob = UIKit.Panel(relicList, "Knob", Palette.Brass * 0.85f, false);
                    knob.sprite = Sprites.Circle;
                    UIKit.Place(knob.rectTransform, k == 0 ? 4 : 444, top - 7, 12, 12);
                }
            }

            bool anyNew = false;
            for (int i = 0; i < run.relicSlots; i++)
            {
                int row = i / PendantsPerRow, col = i % PendantsPerRow;
                float cx = slotW * (col + 0.5f);
                float top = 510f - row * PendantRowH - 14f;

                if (i >= run.relics.Count)
                {
                    // Crochet vide
                    var hook = UIKit.Panel(relicList, "Hook", Palette.Brass.WithAlpha(0.35f), false);
                    hook.sprite = Sprites.Ring;
                    UIKit.Place(hook.rectTransform, cx - 9, top - 20, 18, 18);
                    var e = UIKit.Label(relicList, "libre", 15, Palette.Dim);
                    UIKit.Place(e.rectTransform, cx - slotW / 2, top - 60, slotW, 30);
                    continue;
                }

                var relic = run.relics[i];
                var holder = UIKit.Rect(relicList, "Slot_" + relic.Id);
                holder.anchorMin = holder.anchorMax = new Vector2(0f, 0f);
                holder.pivot = new Vector2(0.5f, 1f);
                holder.sizeDelta = new Vector2(slotW, PendantRowH - 10f);
                holder.anchoredPosition = new Vector2(cx, top);

                var pendant = RelicArt.Build(holder, relic, 64f);
                var prt = (RectTransform)pendant.transform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -2f);
                UIKit.Tooltip(pendant.gameObject, () => $"<b>{relic.Name}</b>  <color=#{ColorUtility.ToHtmlStringRGB(Palette.ForRarity(relic.Rarity))}>{Palette.RarityName(relic.Rarity)}</color>\n{relic.Desc}" +
                    (relic.Status != null ? $"\n<color=#C9A24A>{relic.Status}</color>" : ""));
                relicPendants[relic] = pendant;
                if (knownRelics.Add(relic)) { pendant.Appear(); anyNew = true; }

                var name = UIKit.Label(holder, relic.Name, 15, Palette.White, TextAnchor.UpperCenter, FontStyle.Bold);
                name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                name.rectTransform.pivot = new Vector2(0.5f, 1f);
                name.rectTransform.sizeDelta = new Vector2(slotW - 4f, 40f);
                name.rectTransform.anchoredPosition = new Vector2(0f, -100f);
                var status = UIKit.Label(holder, relic.Status ?? "", 14, Palette.ForRarity(relic.Rarity), TextAnchor.UpperCenter);
                status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                status.rectTransform.pivot = new Vector2(0.5f, 1f);
                status.rectTransform.sizeDelta = new Vector2(slotW - 4f, 24f);
                status.rectTransform.anchoredPosition = new Vector2(0f, -140f);
                relicStatus[relic] = status;

                if (inShop)
                {
                    var sell = UIKit.Button(holder, $"+{relic.SellValue}", new Color(0.35f, 0.26f, 0.1f), () => gm.SellRelic(relic), 15);
                    var srt = (RectTransform)sell.transform;
                    srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
                    srt.sizeDelta = new Vector2(slotW - 12f, 28f); srt.anchoredPosition = new Vector2(0f, -180f);
                    UIKit.Tooltip(sell.gameObject, () => $"Vendre {relic.Name} : +{relic.SellValue} tickets");
                    var left = UIKit.Button(holder, "<", new Color(0.2f, 0.14f, 0.18f), () => gm.MoveRelic(relic, -1), 15);
                    var lrt = (RectTransform)left.transform;
                    lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
                    lrt.sizeDelta = new Vector2(slotW / 2f - 8f, 26f); lrt.anchoredPosition = new Vector2(-slotW / 4f + 1f, -212f);
                    var right = UIKit.Button(holder, ">", new Color(0.2f, 0.14f, 0.18f), () => gm.MoveRelic(relic, 1), 15);
                    var rrt = (RectTransform)right.transform;
                    rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f);
                    rrt.sizeDelta = new Vector2(slotW / 2f - 8f, 26f); rrt.anchoredPosition = new Vector2(slotW / 4f - 1f, -212f);
                }
            }
            if (anyNew) gm.Juice.PlayRelic();
        }

        void RebuildCharms()
        {
            UIKit.Clear(charmList);
            var run = gm.Run;
            charmHeader.text = $"CHARMES  <color=#5A4A7A>{run.charms.Count}/{run.charmSlots}</color>";
            for (int i = 0; i < run.charms.Count; i++)
            {
                int index = i;
                var info = Catalog.Charm(run.charms[i]);
                bool usable = gm.CanUseCharm(run.charms[i]);
                var b = UIKit.Button(charmList, "Utiliser : " + info.name, usable ? Palette.Pink * 0.85f : Palette.Dim, () => gm.UseCharm(index), 21);
                UIKit.Size(b, 54);
                var charmType = run.charms[i];
                UIKit.Tooltip(b.gameObject, () => $"<b>{info.name}</b>\n{Catalog.CharmFullDesc(charmType)}");
            }
            if (run.charms.Count == 0)
            {
                var t = UIKit.Label(charmList, "aucun charme", 18, Palette.Dim);
                UIKit.Size(t, 40);
            }
        }
    }
}
