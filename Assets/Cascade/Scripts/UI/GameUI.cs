// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Toute l'interface, construite par code. Référence 1920x1080.
    /// Gauche : seuil, score (jetons x mult façon Balatro), ressources. Droite : reliques, charmes, sac.
    /// Centre : écrans de seuil, résultats, boutique, packs, fin de partie.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }
        public Transform WorldCanvas { get; private set; }

        GameManager gm;
        RectTransform root, leftRoot, rightRoot, centerRoot;
        CanvasScaler scaler;
        int lastScreenW, lastScreenH;
        public RectTransform RootRect => root;

        // Gauche
        Text actText, seuilText, bossName, bossDesc, targetText, roundScoreText, chipsText, multText, resultText;
        Text ballsText, relancesText, ticketsText, windText, helpText;
        Image progressFill, tiltFill, bossBox, chipsBox, multBox;
        double shownRoundScore;
        float ticketPulse, chipsPunch, multPunch;
        double lastChips, lastMult;
        RectTransform leftPanel;
        float resultPunch;
        int lastTallyStep = -1;
        bool pegTooltip;

        // Flammes (score qui éclate la cible)
        class FlameBit { public Image img; public Vector2 v; public float life, max, size; }
        readonly List<FlameBit> flames = new List<FlameBit>();
        readonly Stack<FlameBit> freeFlames = new Stack<FlameBit>();
        float flameAcc;

        // Droite
        RectTransform relicList, charmList;
        Text relicHeader, charmHeader, nextBallName, nextBallDesc, bagText;
        string relicSignature, charmSignature;
        readonly Dictionary<Relic, Image> relicCards = new Dictionary<Relic, Image>();
        readonly Dictionary<Relic, float> relicFlash = new Dictionary<Relic, float>();
        readonly Dictionary<Relic, Text> relicStatus = new Dictionary<Relic, Text>();

        // Centre
        RectTransform overlay, seuilPanel, resultsPanel, shopPanel, packPanel, bagPanel, endPanel, targetingBanner;
        Text tierText, toastText, targetingText;
        float tierTimer, toastTimer;
        RectTransform tooltip;
        Text tooltipText;

        const float CenterW = 824f;
        const float SidePanel = 540f; // largeur d'un panneau latéral + marges, en pixels de référence

        public void Init(GameManager gm)
        {
            Instance = this;
            this.gm = gm;

            // Canvas écran
            var canvasGo = new GameObject("UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = true;   // texte calé sur les pixels : net
            scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            root = (RectTransform)canvasGo.transform;

            // Canvas monde (étiquettes des cases, popups)
            var wgo = new GameObject("WorldCanvas", typeof(RectTransform));
            var wc = wgo.AddComponent<Canvas>();
            wc.renderMode = RenderMode.WorldSpace;
            wc.worldCamera = Camera.main;
            wc.sortingOrder = 40;
            var wrt = (RectTransform)wgo.transform;
            wrt.sizeDelta = new Vector2(24, 14);
            wrt.position = Vector3.zero;
            WorldCanvas = wrt;

            // Le HUD vit sur les écrans physiques de la borne (canvas en espace monde)
            leftRoot = Arcade.Instance.LeftDisplay;
            rightRoot = Arcade.Instance.RightDisplay;
            centerRoot = Arcade.Instance.OverlayDisplay;

            BuildLeft();
            BuildRight();
            BuildCenter();
            BuildTooltip();
        }

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
                "<color=#19E6FF>Souris</color> viser · <color=#19E6FF>Clic</color> lâcher\n" +
                "<color=#19E6FF>A / D</color> coup de pouce (1 par bille)\n" +
                "<color=#19E6FF>Espace</color> secousse (attention au TILT)\n" +
                "<color=#19E6FF>R / clic droit</color> relance avant le 1er pic\n" +
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
            relicList = UIKit.Place(UIKit.Rect(p, "Relics"), 20, 470, 460, 510);
            UIKit.VList(relicList, 8);
            relicList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            // le VerticalLayoutGroup remplit depuis le haut
            relicList.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

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

        void RebuildRelics()
        {
            UIKit.Clear(relicList);
            relicCards.Clear();
            relicStatus.Clear();
            var run = gm.Run;
            relicHeader.text = $"RELIQUES  <color=#5A4A7A>{run.relics.Count}/{run.relicSlots}</color>";
            bool inShop = gm.Phase == Phase.Shop;
            foreach (var r in run.relics)
            {
                var relic = r;
                var card = UIKit.Panel(relicList, relic.Name, Palette.PanelLight);
                UIKit.Size(card, inShop ? 92 : 78);
                UIKit.Border(card, Palette.ForRarity(relic.Rarity).WithAlpha(0.8f));
                var bar = UIKit.Panel(card.transform, "Rarity", Palette.ForRarity(relic.Rarity));
                UIKit.Place(bar.rectTransform, 0, 0, 8, inShop ? 92 : 78);
                var name = UIKit.Label(card.transform, relic.Name, 24, Palette.White, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.anchorMin = new Vector2(0, 0.45f); name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(20, 0); name.rectTransform.offsetMax = new Vector2(inShop ? -150 : -10, -4);
                var status = UIKit.Label(card.transform, relic.Status ?? Palette.RarityName(relic.Rarity), 17, Palette.ForRarity(relic.Rarity), TextAnchor.MiddleLeft);
                status.rectTransform.anchorMin = new Vector2(0, 0); status.rectTransform.anchorMax = new Vector2(1, 0.45f);
                status.rectTransform.offsetMin = new Vector2(20, 4); status.rectTransform.offsetMax = new Vector2(inShop ? -150 : -10, 0);
                relicStatus[relic] = status;
                UIKit.Tooltip(card.gameObject, () => $"<b>{relic.Name}</b>  <color=#{ColorUtility.ToHtmlStringRGB(Palette.ForRarity(relic.Rarity))}>{Palette.RarityName(relic.Rarity)}</color>\n{relic.Desc}");

                if (inShop)
                {
                    var sell = UIKit.Button(card.transform, $"Vendre +{relic.SellValue}", Palette.Gold * 0.8f, () => gm.SellRelic(relic), 18);
                    var srt = (RectTransform)sell.transform;
                    srt.anchorMin = srt.anchorMax = new Vector2(1, 0.5f);
                    srt.sizeDelta = new Vector2(130, 36); srt.anchoredPosition = new Vector2(-74, 20);
                    var left = UIKit.Button(card.transform, "<", Palette.Violet, () => gm.MoveRelic(relic, -1), 18);
                    var lrt = (RectTransform)left.transform;
                    lrt.anchorMin = lrt.anchorMax = new Vector2(1, 0.5f);
                    lrt.sizeDelta = new Vector2(60, 32); lrt.anchoredPosition = new Vector2(-108, -22);
                    var right = UIKit.Button(card.transform, ">", Palette.Violet, () => gm.MoveRelic(relic, 1), 18);
                    var rrt = (RectTransform)right.transform;
                    rrt.anchorMin = rrt.anchorMax = new Vector2(1, 0.5f);
                    rrt.sizeDelta = new Vector2(60, 32); rrt.anchoredPosition = new Vector2(-42, -22);
                }
                relicCards[relic] = card;
            }
            for (int i = run.relics.Count; i < run.relicSlots; i++)
            {
                var empty = UIKit.Panel(relicList, "Empty", new Color(1, 1, 1, 0.04f));
                UIKit.Size(empty, 50);
                var t = UIKit.Label(empty.transform, "emplacement libre", 18, Palette.Dim);
                UIKit.Fill(t.rectTransform);
            }
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

        // ================================================================== CENTRE

        void BuildCenter()
        {
            overlay = UIKit.Fill(UIKit.Rect(centerRoot, "Overlay"));

            seuilPanel = UIKit.Place(UIKit.Rect(overlay, "SeuilSelect"), 0, 0, CenterW, 1080);
            resultsPanel = UIKit.Place(UIKit.Rect(overlay, "Results"), 0, 0, CenterW, 1080);
            shopPanel = UIKit.Place(UIKit.Rect(overlay, "Shop"), 0, 0, CenterW, 1080);
            packPanel = UIKit.Place(UIKit.Rect(overlay, "Pack"), 0, 0, CenterW, 1080);
            bagPanel = UIKit.Place(UIKit.Rect(overlay, "Bag"), 0, 0, CenterW, 1080);
            endPanel = UIKit.Place(UIKit.Rect(overlay, "End"), 0, 0, CenterW, 1080);

            targetingBanner = UIKit.Place(UIKit.Panel(overlay, "Targeting", Palette.Pink.WithAlpha(0.85f)).rectTransform, 40, 990, CenterW - 80, 60);
            targetingText = UIKit.Label(targetingBanner, "", 24, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(targetingText.rectTransform, 6);
            targetingBanner.gameObject.SetActive(false);

            tierText = UIKit.Label(overlay, "", 110, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(tierText.rectTransform, -200, 560, CenterW + 400, 200);
            UIKit.Neon(tierText, Palette.Gold);

            toastText = UIKit.Label(overlay, "", 30, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(toastText.rectTransform, 0, 920, CenterW, 50);
            toastText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            HideOverlays();
        }

        public void HideOverlays()
        {
            seuilPanel.gameObject.SetActive(false);
            resultsPanel.gameObject.SetActive(false);
            shopPanel.gameObject.SetActive(false);
            packPanel.gameObject.SetActive(false);
            bagPanel.gameObject.SetActive(false);
            endPanel.gameObject.SetActive(false);
            BagView.Instance?.Close();
            HideTooltip();
        }

        RectTransform Card(RectTransform parent, float x, float y, float w, float h, Color border)
        {
            var c = UIKit.Panel(parent, "Card", Palette.Panel);
            UIKit.Border(c, border.WithAlpha(0.9f), 3f);
            return UIKit.Place(c.rectTransform, x, y, w, h);
        }

        // ---------------- Choix du seuil ----------------

        public void ShowSeuilSelect()
        {
            HideOverlays();
            UIKit.Clear(seuilPanel);
            seuilPanel.gameObject.SetActive(true);
            var run = gm.Run;

            var dim = UIKit.Panel(seuilPanel, "Dim", new Color(0, 0, 0, 0.55f), false);
            UIKit.Fill(dim.rectTransform);

            var head = UIKit.Label(seuilPanel, $"ACTE {run.act}" + (run.endless ? "  ·  ENDLESS" : ""), 44, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(head.rectTransform, 0, 900, CenterW, 70);
            UIKit.Neon(head, Palette.Cyan);

            float w = 250, gap = 22, x0 = (CenterW - (3 * w + 2 * gap)) / 2f;
            for (int s = 0; s < 3; s++)
            {
                bool current = s == run.seuilIndex, done = s < run.seuilIndex;
                bool boss = s == 2;
                var color = boss ? Palette.Red : s == 1 ? Palette.Pink : Palette.Cyan;
                float h = current ? 560 : 470;
                var card = Card(seuilPanel, x0 + s * (w + gap), current ? 260 : 300, w, h, current ? color : Palette.Dim);
                if (!current) card.GetComponent<Image>().color = Palette.Panel * 0.8f;

                string name = s == 0 ? "PETIT SEUIL" : s == 1 ? "SEUIL MOYEN" : "BOSS";
                var n = UIKit.Label(card, name, 28, current ? color : Palette.Dim * 1.6f, TextAnchor.UpperCenter, FontStyle.Bold);
                UIKit.Place(n.rectTransform, 0, h - 60, w, 44);

                var tl = UIKit.Label(card, "Score cible", 18, Palette.White.WithAlpha(0.6f), TextAnchor.UpperCenter);
                UIKit.Place(tl.rectTransform, 0, h - 100, w, 28);
                var tv = UIKit.Label(card, Fmt.Num(run.TargetFor(run.act, s)), 38, Palette.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
                UIKit.Place(tv.rectTransform, 0, h - 150, w, 50);

                int reward = s == 0 ? 3 : s == 1 ? 4 : 5;
                var rw = UIKit.Label(card, $"Récompense : {reward} tickets", 18, Palette.Gold.WithAlpha(0.8f), TextAnchor.UpperCenter);
                UIKit.Place(rw.rectTransform, 0, h - 185, w, 28);

                string body;
                if (boss)
                    body = string.Join("\n\n", run.BossesOfAct(run.act).Select(b => $"<color=#FF3B5C><b>{Catalog.Boss(b).name}</b></color>\n{Catalog.Boss(b).desc}"));
                else
                {
                    var tag = Catalog.Tag(run.TagFor(run.act, s));
                    body = $"<color=#5A4A7A>Passer ce seuil :</color>\n<color=#FFC83D><b>{tag.name}</b></color>\n{tag.desc}";
                }
                var bt = UIKit.Label(card, body, 19, Palette.White, TextAnchor.UpperCenter);
                UIKit.Place(bt.rectTransform, 14, 110, w - 28, h - 310);

                if (done)
                {
                    var d = UIKit.Label(card, "FRANCHI", 30, Palette.Green, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Place(d.rectTransform, 0, 20, w, 60);
                }
                else if (current)
                {
                    var play = UIKit.Button(card, "JOUER", color * 0.9f, () => gm.PlaySeuil(), 30);
                    UIKit.Place((RectTransform)play.transform, 20, 85, w - 40, 64);
                    if (!boss)
                    {
                        var skip = UIKit.Button(card, "Passer", Palette.Dim, () => gm.SkipSeuil(), 22);
                        UIKit.Place((RectTransform)skip.transform, 20, 18, w - 40, 52);
                    }
                }
                else
                {
                    var d = UIKit.Label(card, "à venir", 24, Palette.Dim * 1.5f, TextAnchor.MiddleCenter);
                    UIKit.Place(d.rectTransform, 0, 20, w, 60);
                }
            }

            var seed = UIKit.Label(seuilPanel, $"Graine : {run.seed}", 16, Palette.Dim * 1.5f);
            UIKit.Place(seed.rectTransform, 0, 200, CenterW, 30);
            Refresh();
        }

        // ---------------- Résultats ----------------

        public void ShowResults(List<(string label, int amount)> lines, int total)
        {
            HideOverlays();
            UIKit.Clear(resultsPanel);
            resultsPanel.gameObject.SetActive(true);
            var card = Card(resultsPanel, 160, 300, CenterW - 320, 480, Palette.Gold);
            var t = UIKit.Label(card, "SEUIL FRANCHI !", 46, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 400, CenterW - 320, 64);
            UIKit.Neon(t, Palette.Gold);
            var sc = UIKit.Label(card, $"{Fmt.Num(gm.RoundScore)} / {Fmt.Num(gm.Run.Target)}", 26, Palette.White);
            UIKit.Place(sc.rectTransform, 0, 355, CenterW - 320, 36);
            float y = 300;
            foreach (var l in lines)
            {
                var a = UIKit.Label(card, l.label, 24, Palette.White, TextAnchor.MiddleLeft);
                UIKit.Place(a.rectTransform, 40, y, 330, 36);
                var b = UIKit.Label(card, "+" + l.amount, 26, Palette.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
                UIKit.Place(b.rectTransform, 300, y, 150, 36);
                y -= 44;
            }
            var tot = UIKit.Label(card, $"Total : +{total} tickets", 32, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(tot.rectTransform, 0, 110, CenterW - 320, 50);
            var cont = UIKit.Button(card, gm.Run.IsFinalSeuil ? "VICTOIRE !" : "BOUTIQUE", Palette.Gold * 0.85f, () => gm.ContinueAfterResults(), 30);
            UIKit.Place((RectTransform)cont.transform, 60, 25, CenterW - 440, 70);
            Refresh();
        }

        // ---------------- Boutique ----------------

        public void ShowShop()
        {
            HideOverlays();
            shopPanel.gameObject.SetActive(true);
            RefreshShop();
            Refresh();
        }

        public void RefreshShop()
        {
            if (!shopPanel.gameObject.activeSelf) return;
            UIKit.Clear(shopPanel);

            // Tapis de jeu en feutre vert, bordé de laiton
            var bg = UIKit.Panel(shopPanel, "Felt", new Color(0.04f, 0.2f, 0.13f, 0.96f));
            UIKit.Place(bg.rectTransform, 0, 40, CenterW, 990);
            UIKit.Border(bg, Palette.Brass, 5f);
            var inner = UIKit.Panel(shopPanel, "FeltLine", new Color(0, 0, 0, 0));
            UIKit.Place(inner.rectTransform, 18, 58, CenterW - 36, 954);
            UIKit.Border(inner, Palette.Brass.WithAlpha(0.45f), 2f);

            var title = UIKit.Label(shopPanel, "LA BOUTIQUE", 54, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 945, CenterW, 70);
            UIKit.Neon(title, Palette.Gold);
            var tkBox = UIKit.Panel(shopPanel, "Tickets", new Color(0.02f, 0.02f, 0.02f, 0.95f));
            UIKit.Place(tkBox.rectTransform, CenterW / 2 - 130, 885, 260, 54);
            UIKit.Border(tkBox, Palette.Gold, 2f);
            var tkLabel = UIKit.Label(tkBox.transform, "TICKETS", 16, Palette.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(tkLabel.rectTransform, 14, 0, 100, 54);
            var tk = UIKit.Led(tkBox.transform, 100, 4, 145, 46, 30, Palette.Gold, 4, letters: false);
            tk.text = gm.Run.tickets.ToString();

            var offers = gm.Shop.offers;
            var top = offers.Where(o => o.kind == ShopOffer.Kind.Relic || o.kind == ShopOffer.Kind.Pack).ToList();
            var items = offers.Where(o => o.kind == ShopOffer.Kind.Ball || o.kind == ShopOffer.Kind.Charm).ToList();

            float w = 250, gap = 22, x0 = (CenterW - (3 * w + 2 * gap)) / 2f;
            for (int i = 0; i < top.Count; i++) OfferCard(shopPanel, top[i], x0 + i * (w + gap), 495, w, 375);
            for (int i = 0; i < items.Count; i++) OfferCard(shopPanel, items[i], x0 + i * (w + gap), 130, w, 345);

            // Services : gros boutons de borne
            float sx = x0 + 2 * (w + gap);
            string rr = gm.Run.freeRerolls > 0 ? $"RELANCER\n(gratuit x{gm.Run.freeRerolls})" : $"RELANCER\n{gm.Shop.rerollCost} tickets";
            var reroll = UIKit.Button(shopPanel, rr, new Color(0.15f, 0.45f, 0.25f), () => gm.Reroll(), 22);
            UIKit.Place((RectTransform)reroll.transform, sx, 380, w, 95);
            UIKit.Border(reroll.GetComponent<Image>(), Palette.Brass, 3f);
            var bag = UIKit.Button(shopPanel, $"SAC\nretirer : {Shop.RemoveBallCost}", Palette.Violet, () => ShowBag(), 22);
            UIKit.Place((RectTransform)bag.transform, sx, 265, w, 95);
            UIKit.Border(bag.GetComponent<Image>(), Palette.Brass, 3f);
            var next = UIKit.Button(shopPanel, "SEUIL SUIVANT >>", new Color(0.75f, 0.12f, 0.2f), () => gm.LeaveShop(), 24);
            UIKit.Place((RectTransform)next.transform, sx, 130, w, 115);
            UIKit.Border(next.GetComponent<Image>(), Palette.Gold, 4f);

            var hint = UIKit.Label(shopPanel, "Pinceaux et marteau s'utilisent ici, sur la planche (écran de droite).", 18, Palette.White.WithAlpha(0.65f));
            UIKit.Place(hint.rectTransform, 20, 62, CenterW - 40, 50);
        }

        // ---------- Un visuel par type d'article ----------
        //  Relique = carte à jouer · Bille = jeton de casino · Charme = ticket de tombola · Pack = carte à gratter

        static readonly Color Ink = new Color(0.1f, 0.06f, 0.07f);
        static readonly Color Cream = new Color(0.96f, 0.92f, 0.83f);

        void OfferCard(RectTransform parent, ShopOffer o, float x, float y, float w, float h)
        {
            RectTransform card;
            switch (o.kind)
            {
                case ShopOffer.Kind.Relic: card = PlayingCard(parent, o.relic, x, y, w, h); break;
                case ShopOffer.Kind.Ball: card = ChipCard(parent, o.ball, x, y, w, h); break;
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
                var veil = UIKit.Panel(card, "Sold", new Color(0, 0, 0, 0.6f));
                UIKit.Fill(veil.rectTransform);
                veil.raycastTarget = false;
                var stamp = UIKit.Label(card, "VENDU", 44, Palette.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Fill(stamp.rectTransform);
                stamp.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            }
            else
            {
                bool afford = gm.Run.tickets >= price;
                var b = UIKit.Button(card, $"ACHETER  {price}", afford ? new Color(0.8f, 0.6f, 0.15f) : Palette.Dim, () => gm.Buy(o), 22);
                UIKit.Place((RectTransform)b.transform, 18, 14, w - 36, 56);
                UIKit.Border(b.GetComponent<Image>(), afford ? Palette.Gold : Palette.Dim, 2f);
            }
        }

        static Sprite SuitOf(Rarity r) => r switch
        {
            Rarity.Common => Sprites.Club,
            Rarity.Uncommon => Sprites.Diamond,
            Rarity.Rare => Sprites.Heart,
            _ => Sprites.Spade
        };

        static string RankOf(Rarity r) => r switch
        {
            Rarity.Common => "J", Rarity.Uncommon => "Q", Rarity.Rare => "K", _ => "A"
        };

        static Color SuitColor(Rarity r) => r == Rarity.Uncommon || r == Rarity.Rare ? new Color(0.78f, 0.1f, 0.15f) : Ink;

        Image Pic(Transform parent, Sprite sp, Color c, float x, float y, float w, float h)
        {
            var img = UIKit.Panel(parent, "Img", c, false);
            img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true;
            img.raycastTarget = false;
            UIKit.Place(img.rectTransform, x, y, w, h);
            return img;
        }

        /// <summary>Relique : carte à jouer (valet ♣ commune, dame ♦ peu commune, roi ♥ rare, as ♠ légendaire).</summary>
        RectTransform PlayingCard(RectTransform parent, Relic r, float x, float y, float w, float h, bool withButtonSpace = true)
        {
            var img = UIKit.Panel(parent, "PlayingCard", Cream);
            UIKit.Border(img, new Color(0, 0, 0, 0.5f), 3f);
            var card = UIKit.Place(img.rectTransform, x, y, w, h);
            var frame = UIKit.Panel(card, "Frame", new Color(0, 0, 0, 0));
            UIKit.Place(frame.rectTransform, 10, 10, w - 20, h - 20);
            UIKit.Border(frame, Palette.ForRarity(r.Rarity) * 0.8f, 2f);

            var sc = SuitColor(r.Rarity);
            var rank = UIKit.Label(card, RankOf(r.Rarity), 34, sc, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(rank.rectTransform, 18, h - 54, 40, 40);
            Pic(card, SuitOf(r.Rarity), sc, 20, h - 84, 30, 30);
            var rank2 = UIKit.Label(card, RankOf(r.Rarity), 34, sc, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(rank2.rectTransform, w - 58, 36, 40, 40);
            rank2.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Grand symbole en filigrane
            Pic(card, SuitOf(r.Rarity), sc.WithAlpha(0.1f), w / 2 - 70, h / 2 - 40, 140, 140);

            var rarity = UIKit.Label(card, Palette.RarityName(r.Rarity).ToUpper(), 15, Palette.ForRarity(r.Rarity) * 0.75f, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(rarity.rectTransform, 50, h - 44, w - 100, 24);
            var name = UIKit.Label(card, r.Name, 24, Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 30, h - 125, w - 60, 72);
            var desc = UIKit.Label(card, r.Desc, 19, Ink.WithAlpha(0.9f), TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, 26, withButtonSpace ? 80 : 30, w - 52, h - (withButtonSpace ? 215 : 165));
            return card;
        }

        /// <summary>Bille : jeton de casino à la couleur de la bille (édition = liseré lumineux).</summary>
        RectTransform ChipCard(RectTransform parent, BallInstance b, float x, float y, float w, float h)
        {
            var baseImg = UIKit.Panel(parent, "ChipSlot", new Color(0.02f, 0.12f, 0.08f, 0.9f));
            UIKit.Border(baseImg, Palette.Brass.WithAlpha(0.7f), 2f);
            var card = UIKit.Place(baseImg.rectTransform, x, y, w, h);

            float d = 150f, cx = w / 2 - d / 2, cy = h - d - 20;
            Pic(card, Sprites.Circle, new Color(0, 0, 0, 0.45f), cx + 6, cy - 8, d, d);   // ombre
            Pic(card, Sprites.Circle, Color.Lerp(b.Color, Color.black, 0.15f), cx, cy, d, d);
            Pic(card, Sprites.Chip, new Color(1f, 1f, 1f, 0.9f), cx, cy, d, d);
            if (b.edition != Edition.None)
                Pic(card, Sprites.Ring, Catalog.EditionColor(b.edition), cx - 10, cy - 10, d + 20, d + 20);
            var center = UIKit.Label(card, "BILLE", 20, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(center.rectTransform, cx, cy, d, d);

            var name = UIKit.Label(card, b.Name, 21, Palette.White, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 12, cy - 62, w - 24, 58);
            return card;
        }

        /// <summary>Charme : ticket de tombola (talon perforé + numéro).</summary>
        RectTransform RaffleTicket(RectTransform parent, CharmType c, float x, float y, float w, float h)
        {
            var holder = UIKit.Rect(parent, "TicketHolder");
            var card = UIKit.Place(holder, x, y, w, h);
            var hit = holder.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);   // zone de survol

            // Le ticket est posé de biais sur le tapis
            var t = Pic(card, Sprites.Ticket, new Color(0.98f, 0.55f, 0.35f), 4, 90, w - 8, h - 110);
            t.preserveAspect = false;
            t.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var info = Catalog.Charm(c);
            var stub = UIKit.Label(t.transform, "ENTRÉE", 14, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(stub.rectTransform, -20, (h - 110) / 2 - 12, 90, 24);
            stub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var num = UIKit.Label(t.transform, $"N° {(int)c + 1:000}", 16, Ink.WithAlpha(0.7f), TextAnchor.UpperRight);
            UIKit.Place(num.rectTransform, 60, h - 150, w - 90, 24);
            var name = UIKit.Label(t.transform, info.name, 21, Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(name.rectTransform, 60, h - 205, w - 80, 56);
            var desc = UIKit.Label(t.transform, info.desc, 17, Ink.WithAlpha(0.9f), TextAnchor.UpperCenter);
            UIKit.Place(desc.rectTransform, 58, 20, w - 76, h - 235);
            var tag = UIKit.Label(card, "CHARME", 16, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(tag.rectTransform, 0, 70, w, 22);
            return card;
        }

        /// <summary>Pack mystère : carte à gratter dorée.</summary>
        RectTransform ScratchCard(RectTransform parent, float x, float y, float w, float h)
        {
            var img = UIKit.Panel(parent, "ScratchCard", new Color(0.85f, 0.65f, 0.2f));
            UIKit.Border(img, new Color(0.4f, 0.25f, 0.05f), 3f);
            var card = UIKit.Place(img.rectTransform, x, y, w, h);
            var title = UIKit.Label(card, "PACK\nMYSTÈRE", 30, Ink, TextAnchor.UpperCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 10, h - 100, w - 20, 84);
            var silver = UIKit.Panel(card, "Silver", new Color(0.7f, 0.72f, 0.76f));
            UIKit.Place(silver.rectTransform, 24, 95, w - 48, h - 210);
            UIKit.Border(silver, new Color(0.3f, 0.3f, 0.35f), 2f);
            silver.raycastTarget = false;
            for (int i = 0; i < 3; i++)
                Pic(silver.transform, Sprites.Club, new Color(0.55f, 0.56f, 0.6f), 18 + i * ((w - 48 - 36) / 3f), (h - 210) / 2 + 10, 40, 40);
            var scratch = UIKit.Label(silver.transform, "GRATTER ICI", 20, Ink.WithAlpha(0.75f), TextAnchor.LowerCenter, FontStyle.Bold);
            UIKit.Place(scratch.rectTransform, 0, 10, w - 48, 40);
            var sub = UIKit.Label(card, "1 relique parmi 3", 18, Ink, TextAnchor.MiddleCenter);
            UIKit.Place(sub.rectTransform, 0, 72, w, 24);
            return card;
        }

        public void ShowPack()
        {
            UIKit.Clear(packPanel);
            packPanel.gameObject.SetActive(true);
            var dim = UIKit.Panel(packPanel, "Dim", new Color(0, 0, 0, 0.75f), false);
            UIKit.Fill(dim.rectTransform);
            var t = UIKit.Label(packPanel, "TIRE TA CARTE", 46, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 860, CenterW, 64);
            UIKit.Neon(t, Palette.Gold);
            var choices = gm.Shop.packChoices ?? new List<Relic>();
            float w = 250, gap = 22, x0 = (CenterW - (3 * w + 2 * gap)) / 2f;
            for (int i = 0; i < choices.Count; i++)
            {
                var r = choices[i];
                var card = PlayingCard(packPanel, r, x0 + i * (w + gap), 380, w, 440);
                UIKit.Tooltip(card.gameObject, () => $"<b>{r.Name}</b>\n{r.Desc}");
                var b = UIKit.Button(card, "PRENDRE", new Color(0.8f, 0.6f, 0.15f), () => gm.PickFromPack(r), 24);
                UIKit.Place((RectTransform)b.transform, 18, 14, w - 36, 56);
            }
            var skip = UIKit.Button(packPanel, "Ne rien prendre", Palette.Dim, () => gm.PickFromPack(null), 22);
            UIKit.Place((RectTransform)skip.transform, CenterW / 2 - 150, 280, 300, 60);
        }

        public void HidePack() => packPanel.gameObject.SetActive(false);

        // ---------------- Sac ----------------

        public void ShowBag()
        {
            HideTooltip();
            bagPanel.gameObject.SetActive(true);
            BagView.Instance?.Open(gm.Phase == Phase.Shop);
            RefreshBag();
        }

        public void HideBag()
        {
            bagPanel.gameObject.SetActive(false);
            BagView.Instance?.Close();
            HideTooltip();
        }

        /// <summary>Cadre du sac : le bocal 3D est devant, ici seulement le titre, l'aide et le bouton Fermer.</summary>
        public void RefreshBag()
        {
            if (!bagPanel.gameObject.activeSelf) return;
            UIKit.Clear(bagPanel);
            var dim = UIKit.Panel(bagPanel, "Dim", new Color(0, 0, 0, 0.72f), false);
            UIKit.Fill(dim.rectTransform);
            bool inShop = gm.Phase == Phase.Shop;
            var t = UIKit.Label(bagPanel, $"SAC DE BILLES · {gm.Run.bag.Count}", 40, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 960, CenterW, 64);
            UIKit.Neon(t, Palette.Gold);
            string help = "Glisse pour faire tourner le bocal · survole une bille pour voir son effet";
            if (inShop) help += $"\nClic sur une bille : la retirer ({Shop.RemoveBallCost} tickets, minimum 5 billes)";
            else if (gm.Phase == Phase.Aiming || gm.Phase == Phase.Falling) help += $"\nEncore dans la pioche cette manche : {gm.drawPile.Count}";
            var h = UIKit.Label(bagPanel, help, 20, Palette.White.WithAlpha(0.8f));
            UIKit.Place(h.rectTransform, 20, 880, CenterW - 40, 70);

            var close = UIKit.Button(bagPanel, "Fermer", Palette.Violet, HideBag, 26);
            UIKit.Place((RectTransform)close.transform, CenterW / 2 - 130, 60, 260, 64);
        }

        // ---------------- Fin de partie ----------------

        public void ShowGameOver()
        {
            HideOverlays();
            UIKit.Clear(endPanel);
            endPanel.gameObject.SetActive(true);
            var card = Card(endPanel, 110, 280, CenterW - 220, 520, Palette.Red);
            var t = UIKit.Label(card, "LA MACHINE T'A EU", 50, Palette.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 420, CenterW - 220, 70);
            UIKit.Neon(t, Palette.Red);
            var r = gm.Run;
            var s = UIKit.Label(card,
                $"Acte {r.act} · {r.SeuilName}\nScore {Fmt.Num(gm.RoundScore)} / {Fmt.Num(r.Target)}\n\nSeuils franchis : {r.seuilsCleared}\nMeilleure bille : {Fmt.Num(r.bestBallScore)}\nReliques : {r.relics.Count}  ·  Sac : {r.bag.Count} billes\nGraine : {r.seed}",
                26, Palette.White);
            UIKit.Place(s.rectTransform, 0, 140, CenterW - 220, 270);
            var b = UIKit.Button(card, "NOUVELLE RUN", Palette.Pink * 0.9f, () => { HideOverlays(); gm.NewRun(); }, 30);
            UIKit.Place((RectTransform)b.transform, 90, 30, CenterW - 400, 80);
            Refresh();
        }

        public void ShowVictory()
        {
            HideOverlays();
            UIKit.Clear(endPanel);
            endPanel.gameObject.SetActive(true);
            var card = Card(endPanel, 110, 280, CenterW - 220, 520, Palette.Gold);
            var t = UIKit.Label(card, "LA MACHINE EST VAINCUE", 46, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 420, CenterW - 220, 70);
            UIKit.Neon(t, Palette.Gold);
            var r = gm.Run;
            var s = UIKit.Label(card, $"Les 3 actes sont franchis, Croupier.\n\nMeilleure bille : {Fmt.Num(r.bestBallScore)}\nReliques : {r.relics.Count}\n\nContinuer en mode infini ?", 26, Palette.White);
            UIKit.Place(s.rectTransform, 0, 160, CenterW - 220, 250);
            var e = UIKit.Button(card, "ENDLESS", Palette.Gold * 0.85f, () => gm.ContinueEndless(), 28);
            UIKit.Place((RectTransform)e.transform, 40, 30, 260, 80);
            var n = UIKit.Button(card, "Nouvelle run", Palette.Violet, () => { HideOverlays(); gm.NewRun(); }, 26);
            UIKit.Place((RectTransform)n.transform, CenterW - 220 - 300, 30, 260, 80);
            Refresh();
        }

        // ---------------- Ciblage, paliers, toasts ----------------

        public void ShowTargeting(string text)
        {
            targetingText.text = text;
            targetingBanner.gameObject.SetActive(true);
            shopPanel.gameObject.SetActive(false);
            seuilPanel.gameObject.SetActive(false);
        }

        public void HideTargeting()
        {
            targetingBanner.gameObject.SetActive(false);
            if (gm.Phase == Phase.Shop) { shopPanel.gameObject.SetActive(true); RefreshShop(); }
            if (gm.Phase == Phase.SeuilSelect) ShowSeuilSelect();
        }

        public void ShowTier(string text, Color color)
        {
            tierText.text = text;
            tierText.color = color;
            foreach (var o in tierText.GetComponents<Outline>()) o.effectColor = color.WithAlpha(0.35f);
            tierTimer = 1.2f;
        }

        public void Toast(string text, Color color)
        {
            toastText.text = text;
            toastText.color = color;
            toastTimer = 2f;
        }

        public void TicketPulse(int n) => ticketPulse = 1f;

        public void RelicFired(Relic r) => relicFlash[r] = 1f;

        // ---------------- Infobulle ----------------

        void BuildTooltip()
        {
            var img = UIKit.Panel(root, "Tooltip", new Color(0.05f, 0.02f, 0.12f, 0.97f));
            UIKit.Border(img, Palette.Cyan.WithAlpha(0.8f));
            tooltip = img.rectTransform;
            tooltip.pivot = new Vector2(0f, 1f);
            tooltip.anchorMin = tooltip.anchorMax = Vector2.zero;
            tooltip.sizeDelta = new Vector2(380, 150);
            img.raycastTarget = false;
            tooltipText = UIKit.Label(tooltip, "", 20, Palette.White, TextAnchor.UpperLeft);
            UIKit.Fill(tooltipText.rectTransform, 12);
            tooltip.gameObject.SetActive(false);
        }

        public void ShowTooltip(string text)
        {
            tooltipText.text = text;
            float h = Mathf.Max(70f, tooltipText.preferredHeight + 28f);
            tooltip.sizeDelta = new Vector2(380, h);
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
        }

        public void HideTooltip() { if (tooltip != null) tooltip.gameObject.SetActive(false); }

        // ================================================================== RAFRAÎCHISSEMENT

        public void Refresh()
        {
            if (gm == null || gm.Run == null || actText == null) return;
            var run = gm.Run;
            actText.text = $"ACTE {run.act}" + (run.endless ? " · ENDLESS" : "") + $"   ·   seuil {run.seuilIndex + 1}/3";
            seuilText.text = run.SeuilName.ToUpper();
            targetText.text = Fmt.Num(run.Target);

            var bosses = run.CurrentBosses;
            bossBox.gameObject.SetActive(bosses.Length > 0);
            if (bosses.Length > 0)
            {
                bossName.text = string.Join(" + ", bosses.Select(b => Catalog.Boss(b).name));
                bossDesc.text = string.Join(" ", bosses.Select(b => Catalog.Boss(b).desc));
            }

            bool inRound = gm.Phase == Phase.Aiming || gm.Phase == Phase.Falling || gm.Phase == Phase.Targeting && gm.drawPile != null;
            ballsText.text = inRound ? gm.ThrowsLeft.ToString() : run.ballsPerRound.ToString();
            relancesText.text = inRound ? gm.RelancesLeft.ToString() : run.relancesPerRound.ToString();
            ticketsText.text = run.tickets.ToString();

            var next = gm.PeekNextBall();
            bool hidden = run.HasBoss(BossType.Blind);
            if (gm.Phase == Phase.Aiming || gm.Phase == Phase.Falling)
            {
                nextBallName.text = next == null ? "—" : hidden ? "???" : next.Name;
                nextBallDesc.text = next == null || hidden ? "" : next.Desc;
                nextBallName.color = next == null || hidden ? Palette.Dim : next.Color;
            }
            else
            {
                nextBallName.text = "—";
                nextBallDesc.text = "Les billes sont tirées au hasard dans ton sac au début de chaque lancer.";
                nextBallName.color = Palette.Dim;
            }
            gm.Launcher.ShowNext(next, hidden);
            UIKit.SetButtonText(bagText.GetComponentInParent<Button>(), $"Voir le sac ({run.bag.Count})");

            string rs = string.Join("|", run.relics.Select(r => r.Id)) + gm.Phase + run.relicSlots;
            if (rs != relicSignature) { relicSignature = rs; RebuildRelics(); }
            string cs = string.Join("|", run.charms) + gm.Phase + run.charmSlots;
            if (cs != charmSignature) { charmSignature = cs; RebuildCharms(); }

            var last = gm.LastResolved;
            if (gm.Tally == null) resultText.text = last == null ? "" :
                last.slotIndex < 0 ? $"Dernière bille : {Fmt.Num(last.finalScore)}" :
                $"{Fmt.Num(last.chips)} x {Fmt.Num(last.mult)} x case {Fmt.Num(last.slotValue)}" + (last.xmult != 1 ? $" x {Fmt.Num(last.xmult)}" : "") + $" = <b>{Fmt.Num(last.finalScore)}</b>";
        }

        /// <summary>
        /// Adapte l'interface et la caméra à la taille de la fenêtre :
        /// les panneaux restent collés aux bords, la planche est toujours entière entre les deux.
        /// </summary>
        void FitToScreen()
        {
            if (Screen.width == lastScreenW && Screen.height == lastScreenH) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            const float refAspect = 16f / 9f;

            // Écran plus étroit que 16:9 : on garde 1920 de large (l'UI ne déborde pas), sinon 1080 de haut.
            bool narrow = aspect < refAspect;
            scaler.matchWidthOrHeight = narrow ? 0f : 1f;
            float canvasH = narrow ? 1920f / aspect : 1080f;
            float canvasW = canvasH * aspect;

            // Le HUD est sur la borne : on cadre toute la borne et ses deux tours.
            Arcade.Instance?.Fit(aspect, 1f);
        }

        void UpdateTallyText(ScoreContext t, float dt)
        {
            resultPunch = Mathf.MoveTowards(resultPunch, 0f, dt * 4f);
            resultText.transform.localScale = Vector3.one * (1f + resultPunch * 0.35f);
            if (t == null) { lastTallyStep = -1; resultText.color = Palette.Gold; return; }

            float tt = gm.TallyTime;
            int step = 0;
            for (int i = 0; i < GameManager.TallySteps.Length; i++) if (tt >= GameManager.TallySteps[i]) step = i + 1;
            if (step != lastTallyStep) { lastTallyStep = step; resultPunch = 1f; }

            string line = $"<color=#5AA9FF>{Fmt.Num(t.chips)}</color> x <color=#FF5A5A>{Fmt.Num(t.mult)}</color>";
            if (step >= 2) line += $"  x case <color=#F0BE4C>{Fmt.Num(t.slotValue)}</color>";
            if (step >= 3 && t.xmult != 1) line += $"  x {Fmt.Num(t.xmult)}";
            if (step >= 4) line += $"\n= {Fmt.Num(t.finalScore)}";
            resultText.text = line;
            resultText.color = gm.TallyOnFire ? Color.Lerp(Palette.Gold, Palette.Orange, Mathf.PingPong(Time.unscaledTime * 6f, 1f)) : Palette.White;

            // Les compteurs jetons / mult brûlent quand la bille fait franchir le seuil
            if (gm.TallyOnFire)
            {
                var hot = Color.Lerp(Palette.Orange, Palette.Gold, Mathf.PingPong(Time.unscaledTime * 8f, 1f));
                foreach (var o in chipsBox.GetComponents<Outline>()) o.effectColor = hot;
                foreach (var o in multBox.GetComponents<Outline>()) o.effectColor = hot;
            }
            else
            {
                foreach (var o in chipsBox.GetComponents<Outline>()) o.effectColor = Palette.Chips;
                foreach (var o in multBox.GetComponents<Outline>()) o.effectColor = Palette.Mult;
            }
        }

        void UpdateFlames(float dt)
        {
            bool fire = gm.Tally != null && gm.TallyOnFire && gm.TallyTime < gm.TallyDuration - 0.1f;
            if (fire)
            {
                flameAcc += dt * (gm.TallyInferno ? 110f : 55f);
                while (flameAcc >= 1f)
                {
                    flameAcc -= 1f;
                    var f = freeFlames.Count > 0 ? freeFlames.Pop() : NewFlame();
                    f.img.gameObject.SetActive(true);
                    // Les flammes sortent du haut des compteurs jetons et mult (et du score si c'est l'enfer)
                    float x, y;
                    int zone = Random.Range(0, gm.TallyInferno ? 3 : 2);
                    if (zone == 0) { x = Random.Range(28f, 217f); y = 548f; }
                    else if (zone == 1) { x = Random.Range(283f, 472f); y = 548f; }
                    else { x = Random.Range(60f, 440f); y = 740f; }
                    f.size = Random.Range(26f, gm.TallyInferno ? 70f : 50f);
                    f.img.rectTransform.anchoredPosition = new Vector2(x, y);
                    f.v = new Vector2(Random.Range(-15f, 15f), Random.Range(90f, 170f));
                    f.max = f.life = Random.Range(0.35f, 0.7f);
                    flames.Add(f);
                }
            }
            for (int i = flames.Count - 1; i >= 0; i--)
            {
                var f = flames[i];
                f.life -= dt;
                if (f.life <= 0f) { f.img.gameObject.SetActive(false); freeFlames.Push(f); flames.RemoveAt(i); continue; }
                float k = f.life / f.max;
                f.img.rectTransform.anchoredPosition += f.v * dt;
                f.img.rectTransform.sizeDelta = new Vector2(f.size * 0.7f, f.size) * (0.4f + 0.6f * k);
                f.img.color = Color.Lerp(new Color(1f, 0.15f, 0.05f, 0f), new Color(1f, 0.85f, 0.3f, 0.95f), k);
            }
        }

        FlameBit NewFlame()
        {
            var img = UIKit.Panel(leftPanel, "Flame", Color.white, false);
            img.sprite = Sprites.Flame; img.type = Image.Type.Simple; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            return new FlameBit { img = img };
        }

        /// <summary>Survoler un pic (en visée ou en ciblage d'un charme) affiche son effet.</summary>
        void UpdatePegTooltip()
        {
            bool allowed = (gm.Phase == Phase.Aiming || gm.Phase == Phase.Targeting) && !gm.ModalOpen && Arcade.Instance != null;
            Peg peg = null;
            if (allowed && !InputShim.PointerOverUI())
            {
                var m = Arcade.Instance.MouseOnBoard();
                if (m.y < BoardLayout.DropY - 0.6f && m.y > 0.5f) peg = gm.Board.PegAt(m);
            }
            if (peg != null)
            {
                var info = Catalog.Peg(peg.type);
                string state = "";
                if (peg.disabled) state += "\n<color=#E0414F>Neutralisé par le boss.</color>";
                if (peg.fragile) state += "\n<color=#46C8D8>Gelé : se brise au premier contact.</color>";
                if (peg.burning > 0) state += $"\n<color=#FF8A2B>Enflammé ({peg.burning} lancers) : +5 jetons.</color>";
                ShowTooltip($"<b>{info.name}</b>\n{info.desc}{state}");
                pegTooltip = true;
            }
            else if (pegTooltip) { HideTooltip(); pegTooltip = false; }
        }

        void Update()
        {
            FitToScreen();
            if (gm == null || gm.Run == null) return;
            float dt = Time.unscaledDeltaTime;

            // Score de manche qui grimpe
            double target = gm.RoundScore;
            if (gm.Phase == Phase.SeuilSelect) { shownRoundScore = 0; target = 0; }
            shownRoundScore = shownRoundScore < target ? System.Math.Min(target, shownRoundScore + System.Math.Max(1, (target - shownRoundScore) * dt * 6)) : target;
            roundScoreText.text = Fmt.Num(shownRoundScore);
            float pct = (float)System.Math.Min(1.0, shownRoundScore / System.Math.Max(1, gm.Run.Target));
            progressFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            progressFill.color = pct >= 1f ? Palette.Green : Palette.Gold;

            // Jetons x mult : la bille en chute, ou le décompte en cours (façon Balatro)
            var tally = gm.Tally;
            var c = tally ?? (gm.activeBalls.Count > 0 ? gm.activeBalls[gm.activeBalls.Count - 1].ctx : null);
            double chips = c?.chips ?? 0, mult = c?.mult ?? 1;
            UpdateTallyText(tally, dt);
            UpdateFlames(dt);
            UpdatePegTooltip();
            if (chips != lastChips) chipsPunch = 1f;
            if (mult != lastMult) multPunch = 1f;
            lastChips = chips; lastMult = mult;
            chipsText.text = Fmt.Num(chips);
            multText.text = Fmt.Num(mult);
            chipsPunch = Mathf.MoveTowards(chipsPunch, 0f, dt * 5f);
            multPunch = Mathf.MoveTowards(multPunch, 0f, dt * 5f);
            chipsBox.transform.localScale = Vector3.one * (1f + chipsPunch * 0.08f);
            multBox.transform.localScale = Vector3.one * (1f + multPunch * 0.12f);

            // Tilt, vent
            tiltFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(gm.Tilt / GameManager.TiltMax), 1f);
            windText.text = gm.Run.HasBoss(BossType.NorthWind) && (gm.Phase == Phase.Aiming || gm.Phase == Phase.Falling)
                ? (gm.Wind < 0 ? "<< VENT " : "VENT >> ") + Mathf.Abs(gm.Wind).ToString("0") : "";

            // Tickets
            ticketPulse = Mathf.MoveTowards(ticketPulse, 0f, dt * 3f);
            ticketsText.transform.localScale = Vector3.one * (1f + ticketPulse * 0.3f);
            ticketsText.color = Color.Lerp(Palette.White, Palette.Gold, ticketPulse);

            // Palier
            tierTimer = Mathf.Max(0f, tierTimer - dt);
            float k = tierTimer / 1.2f;
            tierText.gameObject.SetActive(tierTimer > 0f);
            tierText.transform.localScale = Vector3.one * (k > 0.85f ? Mathf.Lerp(1f, 1.6f, (k - 0.85f) / 0.15f) : 1f + Mathf.Sin(Time.unscaledTime * 20f) * 0.02f);
            var tc = tierText.color; tc.a = Mathf.Clamp01(k * 3f); tierText.color = tc;

            toastTimer = Mathf.Max(0f, toastTimer - dt);
            var toc = toastText.color; toc.a = Mathf.Clamp01(toastTimer * 2f); toastText.color = toc;

            // Reliques qui clignotent
            foreach (var kv in relicCards)
            {
                relicFlash.TryGetValue(kv.Key, out float f);
                f = Mathf.MoveTowards(f, 0f, dt * 3f);
                relicFlash[kv.Key] = f;
                if (kv.Value == null) continue;
                kv.Value.color = Color.Lerp(Palette.PanelLight, Palette.ForRarity(kv.Key.Rarity), f * 0.6f);
                kv.Value.transform.localScale = Vector3.one * (1f + f * 0.05f);
                if (relicStatus.TryGetValue(kv.Key, out var st) && st != null && kv.Key.Status != null) st.text = kv.Key.Status;
            }

            // Infobulle suit la souris
            if (tooltip.gameObject.activeSelf)
            {
                var scale = root.localScale.x;
                Vector2 m = InputShim.MousePosition / scale;
                float x = m.x + 18f, y = m.y - 18f;
                if (x + tooltip.sizeDelta.x > root.rect.width) x = m.x - tooltip.sizeDelta.x - 18f;
                if (y - tooltip.sizeDelta.y < 0f) y = tooltip.sizeDelta.y;
                tooltip.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
