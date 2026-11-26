// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>Écrans du centre : alerte de bille coincée, choix du seuil, résultats, sac, fin de partie.</summary>
    public partial class GameUI
    {
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

            BuildStuckAlert();
            HideOverlays();
        }

        void BuildStuckAlert()
        {
            var panel = UIKit.Panel(overlay, "StuckAlert", new Color(0.08f, 0.03f, 0.04f, 0.9f));
            stuckAlert = UIKit.Place(panel.rectTransform, CenterW / 2 - 260, 800, 520, 104);
            UIKit.Border(panel, Palette.Red.WithAlpha(0.8f), 2f);
            panel.raycastTarget = false;
            stuckText = UIKit.Label(stuckAlert, "BILLE COINCÉE !  Secoue avec <color=#FFC83D>[R]</color>", 26, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(stuckText.rectTransform, 0, 52, 520, 46);
            var label = UIKit.Label(stuckAlert, "TILT", 16, Palette.Red, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(label.rectTransform, 22, 16, 70, 24);
            var bar = UIKit.Panel(stuckAlert, "TiltBar", new Color(0, 0, 0, 0.6f));
            UIKit.Place(bar.rectTransform, 86, 20, 410, 16);
            stuckTiltFill = UIKit.Panel(bar.transform, "Fill", Palette.Red);
            stuckTiltFill.rectTransform.anchorMin = Vector2.zero;
            stuckTiltFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            stuckTiltFill.rectTransform.offsetMin = stuckTiltFill.rectTransform.offsetMax = Vector2.zero;
            // Repères : chaque secousse remplit un tiers ; la 3e d'affilée = TILT
            for (int i = 1; i < (int)GameManager.TiltMax; i++)
            {
                var tick = UIKit.Panel(bar.transform, "Tick", new Color(1f, 1f, 1f, 0.35f), false);
                tick.rectTransform.anchorMin = tick.rectTransform.anchorMax = new Vector2(i / GameManager.TiltMax, 0.5f);
                tick.rectTransform.sizeDelta = new Vector2(2f, 16f);
                tick.rectTransform.anchoredPosition = Vector2.zero;
            }
            stuckAlert.gameObject.SetActive(false);
        }

        void UpdateStuckAlert()
        {
            bool show = gm.Phase == Phase.Falling && gm.AnyBallStuck && !gm.ModalOpen;
            if (stuckAlert.gameObject.activeSelf != show) stuckAlert.gameObject.SetActive(show);
            if (!show) return;
            float k = Mathf.Clamp01(gm.Tilt / GameManager.TiltMax);
            stuckTiltFill.rectTransform.anchorMax = new Vector2(k, 1f);
            // Jauge orange tant que c'est raisonnable, rouge clignotant au bord du TILT
            bool danger = gm.Tilt >= GameManager.TiltMax - 1f;
            stuckTiltFill.color = danger ? Color.Lerp(Palette.Red, Color.white, Mathf.PingPong(Time.unscaledTime * 6f, 1f) * 0.5f) : Palette.Orange;
            stuckText.text = danger ? "Attends un peu… <color=#FF3B5C>TILT</color> au prochain coup !" : "BILLE COINCÉE !  Secoue avec <color=#FFC83D>[R]</color>";
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.03f;
            stuckAlert.localScale = Vector3.one * pulse;
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
                var tv = UIKit.Label(card, Fmt.Num(RunState.TargetFor(run.act, s)), 38, Palette.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
                UIKit.Place(tv.rectTransform, 0, h - 150, w, 50);

                int reward = RunState.RewardFor(s);
                var rw = UIKit.Label(card, $"Récompense : {reward} tickets + surplus", 18, Palette.Gold.WithAlpha(0.8f), TextAnchor.UpperCenter);
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
                $"Acte {r.act} · {r.SeuilName}\nScore {Fmt.Num(gm.RoundScore)} / {Fmt.Num(r.Target)}\n\nSeuils franchis : {r.seuilsCleared}\nMeilleure bille : {Fmt.Num(r.bestBallScore)}\nReliques : {r.relics.Count}  ·  Sac : {r.bag.Count} billes\nGraine : {r.seed}\n" + RecordLine(),
                26, Palette.White);
            UIKit.Place(s.rectTransform, 0, 140, CenterW - 220, 270);
            var b = UIKit.Button(card, "NOUVELLE RUN", Palette.Pink * 0.9f, () => { HideOverlays(); gm.NewRun(); }, 30);
            UIKit.Place((RectTransform)b.transform, 90, 30, CenterW - 400, 80);
            Refresh();
        }

        string RecordLine() => gm.NewRecord
            ? "<color=#FFC83D>NOUVEAU RECORD !</color>"
            : $"<color=#6B6070>Record : {GameSettings.BestSeuils} seuils · {Fmt.Num(GameSettings.BestBall)} en une bille</color>";

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
    }
}
