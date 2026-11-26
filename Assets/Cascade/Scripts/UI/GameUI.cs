// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// L'interface. À poser sur un Canvas écran de la scène (référence 1920x1080), relié au GameManager, à la borne
    /// (Arcade, dont les écrans portent le HUD) et au canvas monde (étiquettes des cases, popups).
    /// Le contenu des panneaux est encore construit par code au lancement (boutons, listes dynamiques).
    /// Gauche : seuil, score (jetons x mult façon Balatro), ressources. Droite : reliques, charmes, sac.
    /// Centre : écrans de seuil, résultats, boutique, packs, fin de partie.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Canvas))]
    public partial class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [SerializeField] GameManager gm;
        [SerializeField] Arcade arcade;
        [Tooltip("Canvas en espace monde : étiquettes des cases et popups de score.")]
        [SerializeField] RectTransform worldCanvas;
        public Transform WorldCanvas => worldCanvas;

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
        readonly Dictionary<Relic, PendantMotion> relicPendants = new Dictionary<Relic, PendantMotion>();
        readonly Dictionary<Relic, Text> relicStatus = new Dictionary<Relic, Text>();
        /// <summary>Reliques déjà affichées : une nouvelle venue a droit à son entrée animée.</summary>
        readonly HashSet<Relic> knownRelics = new HashSet<Relic>();
        RunState knownRun;

        // Centre
        RectTransform overlay, seuilPanel, resultsPanel, shopPanel, packPanel, bagPanel, endPanel, targetingBanner;
        Text tierText, toastText, targetingText;
        float tierTimer, toastTimer;
        // Alerte « bille coincée » : secouer avec R, jauge de tilt juste dessous
        RectTransform stuckAlert;
        Text stuckText;
        Image stuckTiltFill;
        RectTransform tooltip;
        Text tooltipText;

        const float CenterW = 824f;
        const float SidePanel = 540f; // largeur d'un panneau latéral + marges, en pixels de référence

        /// <summary>Relie l'interface aux autres objets de la scène (utilisé par l'outil qui construit la scène).</summary>
        public void Wire(GameManager manager, Arcade cabinet, RectTransform world)
        {
            gm = manager; arcade = cabinet; worldCanvas = world;
        }

        /// <summary>Réglages du canvas écran : overlay, 1920x1080, texte calé sur les pixels.</summary>
        public static void ConfigureScreenCanvas(GameObject go)
        {
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null) canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = true;   // texte calé sur les pixels : net
            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        }

        /// <summary>Canvas monde (étiquettes des cases, popups), à l'origine de la planche.</summary>
        public static RectTransform CreateWorldCanvas(Camera cam)
        {
            var wgo = new GameObject("WorldCanvas", typeof(RectTransform));
            var wc = wgo.AddComponent<Canvas>();
            wc.renderMode = RenderMode.WorldSpace;
            wc.worldCamera = cam;
            wc.sortingOrder = 40;
            var wrt = (RectTransform)wgo.transform;
            wrt.sizeDelta = new Vector2(24, 14);
            wrt.position = Vector3.zero;
            return wrt;
        }

        void Awake()
        {
            Instance = this;
            if (gm == null) gm = FindAnyObjectByType<GameManager>();
            if (arcade == null) arcade = Arcade.Instance != null ? Arcade.Instance : FindAnyObjectByType<Arcade>();
            if (arcade == null)
            {
                Debug.LogError("[CASCADE] GameUI : aucune borne (Arcade) dans la scène pour porter le HUD.", this);
                enabled = false;
                return;
            }
            if (worldCanvas == null) worldCanvas = CreateWorldCanvas(arcade.Cam);

            // Les réglages du canvas posés dans la scène sont gardés ; on ne complète que ce qui manque.
            scaler = GetComponent<CanvasScaler>();
            if (scaler == null) { ConfigureScreenCanvas(gameObject); scaler = GetComponent<CanvasScaler>(); }
            root = (RectTransform)transform;

            // Le HUD vit sur les écrans physiques de la borne (canvas en espace monde)
            leftRoot = arcade.LeftDisplay;
            rightRoot = arcade.RightDisplay;
            centerRoot = arcade.OverlayDisplay;

            BuildLeft();
            BuildRight();
            BuildCenter();
            BuildTooltip();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

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

        public void RelicFired(Relic r)
        {
            if (relicPendants.TryGetValue(r, out var p) && p != null) p.Fire();
        }

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

            bool inRound = gm.InRound;
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

            // Le HUD est sur la borne : on cadre toute la borne et ses deux tours.
            if (arcade != null) arcade.Fit(aspect, 1f);
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
                if (peg.burning > 0) state += $"\n<color=#FF8A2B>Enflammé ({peg.burning} lancers) : +{Fmt.Num(Catalog.BurningBonusChips)} jetons.</color>";
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
            UpdateStuckAlert();
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

            // Compteurs des reliques (le pendentif s'anime tout seul, voir PendantMotion)
            foreach (var kv in relicStatus)
                if (kv.Value != null && kv.Key.Status != null) kv.Value.text = kv.Key.Status;

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
