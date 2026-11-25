// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Chef d'orchestre d'une run : seuils, manches, lancers, score, boutique, charmes, fin de partie.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState Run { get; private set; }
        public Board Board { get; private set; }
        public Launcher Launcher { get; private set; }
        public Juice Juice { get; private set; }
        public GameUI UI { get; private set; }
        public Shop Shop { get; } = new Shop();

        public Phase Phase { get; private set; }

        // ---------- État de la manche ----------
        public readonly List<BallInstance> drawPile = new List<BallInstance>();
        public readonly List<BallController> activeBalls = new List<BallController>();
        public int ThrowsLeft { get; private set; }
        public int RelancesLeft { get; private set; }
        public double RoundScore { get; private set; }
        public int NextThrowIndex { get; private set; } = 1;
        public float Wind { get; private set; }
        public float Tilt { get; private set; }
        public bool BrakeUsed { get; private set; }
        public bool RoundWon { get; private set; }
        public ScoreContext LastContext { get; private set; }
        public ScoreContext LastResolved { get; private set; }

        public const float TiltMax = 3f;

        // ---------- Décompte façon Balatro ----------
        // Une bille arrivée n'est pas ajoutée tout de suite : son calcul s'affiche étape par étape
        // (jetons x mult, x case, x bonus, = total), puis le total rejoint le score de la manche.
        readonly Queue<ScoreContext> pendingTallies = new Queue<ScoreContext>();
        public ScoreContext Tally { get; private set; }
        public float TallyTime { get; private set; }
        public float TallyDuration { get; private set; }
        /// <summary>Cette bille fait franchir le seuil : flammes !</summary>
        public bool TallyOnFire { get; private set; }
        /// <summary>Cette bille dépasse à elle seule la cible : grosses flammes.</summary>
        public bool TallyInferno { get; private set; }
        public bool Tallying => Tally != null || pendingTallies.Count > 0;
        double PendingTotal { get { double t = 0; foreach (var p in pendingTallies) t += p.finalScore; if (Tally != null) t += Tally.finalScore; return t; } }

        /// <summary>Une fenêtre modale (sac) est ouverte : pas de lancer.</summary>
        public bool ModalOpen { get; set; }

        // Charmes en attente de cible
        int pendingCharmIndex = -1;
        Phase phaseBeforeTargeting;

        // Temps
        float slowMoTimer;      // ralenti dramatique (temps réel)
        float charmSlowTimer;   // Charme du Ralenti (temps réel)
        float resolveDelay;
        float phaseSince;       // évite qu'un clic sur un bouton lâche aussi une bille

        public void Init(Board board, Launcher launcher, Juice juice, GameUI ui)
        {
            Instance = this;
            Board = board; Launcher = launcher; Juice = juice; UI = ui;
        }

        /// <summary>Graine imposée pour la prochaine run (0 = aléatoire).</summary>
        public int PendingSeed { get; set; }

        void Start()
        {
            NewRun(PendingSeed != 0 ? PendingSeed : (int?)null);
            PendingSeed = 0;
        }

        public void NewRun(int? seed = null)
        {
            foreach (var b in activeBalls) if (b != null) Destroy(b.gameObject);
            activeBalls.Clear();
            Run = new RunState(seed ?? System.Environment.TickCount);
            pendingTallies.Clear();
            Tally = null;
            Board.Build(Run, UI.WorldCanvas);
            ShowSeuilSelect();
            UI.Toast("Bille de départ : " + Run.StarterBall.Name, Run.StarterBall.Color);
        }

        // ================= Seuils =================

        public void ShowSeuilSelect()
        {
            SetPhase(Phase.SeuilSelect);
            Board.ResetForRound(Run);
            UI.ShowSeuilSelect();
        }

        public void PlaySeuil()
        {
            Juice.PlayClick();
            StartRound();
        }

        public void SkipSeuil()
        {
            if (Run.IsBoss) return;
            var tag = Run.TagFor(Run.act, Run.seuilIndex);
            ApplyTag(tag);
            Juice.PlayBuy();
            UI.Toast(Catalog.Tag(tag).name + " : " + Catalog.Tag(tag).desc, Palette.Gold);
            Run.Advance();
            ShowSeuilSelect();
        }

        void ApplyTag(SkipTag tag)
        {
            switch (tag)
            {
                case SkipTag.Gold: AddTickets(10, null); break;
                case SkipTag.Charm:
                    if (Run.charms.Count < Run.charmSlots) Run.charms.Add(Catalog.AllCharms[Run.RandomInt(Catalog.AllCharms.Length)]);
                    else AddTickets(3, null);
                    break;
                case SkipTag.Shop: Run.freeRerolls += 2; break;
                case SkipTag.Board:
                    var spots = new List<(int r, int c)>();
                    for (int r = 0; r < Run.pegs.Length; r++)
                        for (int c = 0; c < Run.pegs[r].Length; c++)
                            if (Run.pegs[r][c] == PegType.Standard) spots.Add((r, c));
                    if (spots.Count > 0)
                    {
                        var s = spots[Run.RandomInt(spots.Count)];
                        Run.pegs[s.r][s.c] = PegType.Multi;
                        Board.RebuildPegs(Run);
                    }
                    break;
            }
        }

        // ================= Manche =================

        void StartRound()
        {
            drawPile.Clear();
            drawPile.AddRange(Run.bag);
            Run.Shuffle(drawPile);
            ThrowsLeft = Run.HasBoss(BossType.Colossus) ? 1 : Run.ballsPerRound;
            RelancesLeft = Run.relancesPerRound;
            RoundScore = 0;
            NextThrowIndex = 1;
            Tilt = 0;
            BrakeUsed = false;
            RoundWon = false;
            LastContext = null;
            LastResolved = null;
            pendingTallies.Clear();
            Tally = null;
            Board.ResetForRound(Run);
            foreach (var r in Run.relics.ToList()) r.OnRoundStart(this);
            RollWind();
            UI.HideOverlays();
            SetPhase(Phase.Aiming);
        }

        void RollWind()
        {
            Wind = Run.HasBoss(BossType.NorthWind) ? (Random.value < 0.5f ? -1f : 1f) * Random.Range(4f, 9f) : 0f;
        }

        public BallInstance PeekNextBall() => drawPile.Count > 0 ? drawPile[0] : null;

        public int PreviewBounces
        {
            get
            {
                if (Run.HasBoss(BossType.Blind)) return 0;
                return 2 + Run.relics.Sum(r => r.ExtraPreviewBounces);
            }
        }

        public bool HasBrake => Run.relics.Any(r => r.GrantsBrake);

        public void Launch(float x)
        {
            if (Phase != Phase.Aiming || ThrowsLeft <= 0 || drawPile.Count == 0) return;
            var ball = drawPile[0];
            drawPile.RemoveAt(0);
            ThrowsLeft--;
            Board.OnThrow();

            var ctx = new ScoreContext { ball = ball, throwIndex = NextThrowIndex++ };
            if (ball.edition == Edition.Foil) ctx.chips += 50;
            if (ball.edition == Edition.Holo) ctx.mult += 10;

            float jitter = Random.Range(-0.02f, 0.02f);
            float radiusScale = Run.HasBoss(BossType.Colossus) ? 1.2f : 1f;
            var bc = SpawnBall(ctx, new Vector2(x + jitter, BoardLayout.DropY), Vector2.zero, radiusScale);
            LastContext = ctx;
            foreach (var r in Run.relics.ToList()) r.OnBallLaunched(this, ctx);

            Juice.PlayDrop();
            SetPhase(Phase.Falling);
            UI.Refresh();
        }

        BallController SpawnBall(ScoreContext ctx, Vector2 pos, Vector2 vel, float radiusScale)
        {
            var go = new GameObject("Ball_" + ctx.ball.kind);
            var bc = go.AddComponent<BallController>();
            bc.Init(this, ctx, pos, vel, radiusScale);
            foreach (var other in activeBalls) bc.IgnoreCollisionWith(other);
            activeBalls.Add(bc);
            return bc;
        }

        public void SplitTwin(BallController parent)
        {
            var ctx = parent.ctx.CloneForTwin();
            var v = parent.Velocity;
            var child = SpawnBall(ctx, (Vector2)parent.transform.position + new Vector2(0.05f, 0f), new Vector2(-v.x + 1.5f, v.y), 1f);
            child.IsTwinChild = true;
            parent.Velocity = new Vector2(v.x - 1.5f, v.y);
            Juice.Burst(parent.transform.position, Palette.Violet, 16, 3f);
            Juice.PlayBumper();
        }

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
                    case PegType.Bumper: c.AddChips(Catalog.BumperChips * chipMul, at); ball.ApplyBumper(at); Juice.PlayBumper(); Juice.Shake(0.08f); break;
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

            // Musique : chaque pic joue une note, la mélodie monte avec le mult.
            int note = Mathf.Clamp(c.pegHits % 6 + Mathf.FloorToInt((float)System.Math.Log(c.mult, 1.6)), 0, 14);
            Juice.PegNote(note, peg.type, peg.disabled);
            Juice.Burst(at, Catalog.Peg(peg.type).color, peg.disabled ? 2 : 5, 1.6f);
            UI.Refresh();
        }

        public void OnWallHit(BallController ball)
        {
            var c = ball.ctx;
            if (c.ball.kind == BallKind.Bouncy) c.AddMult(2, ball.transform.position);
            foreach (var r in Run.relics.ToList()) r.OnWallHit(this, c);
            Juice.PlayWall();
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

        // ================= Boucle =================

        void Update()
        {
            UpdateTime();
            if (Run == null) return;

            Tilt = Mathf.MoveTowards(Tilt, 0f, Time.unscaledDeltaTime * 0.6f);
            UpdateTally();

            switch (Phase)
            {
                case Phase.Aiming:
                    Launcher.active = true;
                    bool armed = Time.unscaledTime - phaseSince > 0.2f && !ModalOpen;
                    if (armed && ((InputShim.LeftClick && !InputShim.PointerOverUI()) || InputShim.Drop)) Launch(Launcher.AimX);
                    break;

                case Phase.Falling:
                    Launcher.active = false;
                    HandleFallingInput();
                    UpdateDramaticSlowMo();
                    if (activeBalls.Count == 0 && !Tallying)
                    {
                        resolveDelay -= Time.unscaledDeltaTime;
                        if (resolveDelay <= 0f) AfterAllBallsResolved();
                    }
                    break;

                case Phase.Targeting:
                    Launcher.active = false;
                    HandleTargeting();
                    break;

                default:
                    Launcher.active = false;
                    break;
            }
        }

        void UpdateTally()
        {
            if (Tally == null)
            {
                if (pendingTallies.Count == 0) return;
                Tally = pendingTallies.Dequeue();
                TallyTime = 0f;
                double before = RoundScore;
                TallyOnFire = before < Run.Target && before + Tally.finalScore >= Run.Target;
                TallyInferno = Tally.finalScore >= Run.Target;
                // Les gros coups durent un peu plus longtemps : on savoure.
                TallyDuration = TallyInferno ? 2.3f : TallyOnFire ? 1.9f : 1.25f;
                if (TallyOnFire || TallyInferno) Juice.PlayFire();
                LastResolved = Tally;
                UI.Refresh();
            }

            float prev = TallyTime;
            TallyTime += Time.unscaledDeltaTime;
            // Petits « clics » à chaque étape du calcul
            foreach (float step in TallySteps)
                if (prev < step && TallyTime >= step) Juice.PlayTick(step);

            if (TallyTime >= TallyDuration)
            {
                RoundScore += Tally.finalScore;
                Juice.PlayCoin();
                if (RoundScore >= Run.Target && !RoundWon)
                {
                    RoundWon = true;
                    Juice.PlayWin();
                }
                Tally = null;
                TallyOnFire = TallyInferno = false;
                UI.Refresh();
            }
        }

        /// <summary>Instants (secondes) des étapes du décompte : jetons x mult, x case, x bonus, = total.</summary>
        public static readonly float[] TallySteps = { 0.05f, 0.35f, 0.6f, 0.85f };

        void HandleFallingInput()
        {
            var main = activeBalls.Count > 0 ? activeBalls[0] : null;
            if (main == null) return;

            if (InputShim.NudgeLeft) main.Nudge(-1f);
            if (InputShim.NudgeRight) main.Nudge(1f);

            if (InputShim.Shake)
            {
                Tilt += 1f;
                Juice.Shake(0.25f);
                Juice.PlayShake();
                foreach (var b in activeBalls) b.Shake(2.2f);
                if (Tilt >= TiltMax)
                {
                    Tilt = 0f;
                    UI.ShowTier("TILT !", Palette.Red);
                    Juice.PlayTilt();
                    foreach (var b in activeBalls.ToList()) ResolveBall(b, -1, lost: true, reason: "TILT");
                }
            }

            if ((InputShim.Relance || InputShim.RightClick) && activeBalls.Count == 1 && main.ctx.pegHits == 0 && RelancesLeft > 0)
                DoRelance(main);

            if (InputShim.Brake && HasBrake && !BrakeUsed)
            {
                BrakeUsed = true;
                main.Freeze(1f);
                Juice.PlayIceBreak();
                UI.Toast("Frein !", Palette.Ice);
            }
        }

        void DoRelance(BallController ball)
        {
            RelancesLeft--;
            drawPile.Insert(0, ball.ctx.ball);
            ThrowsLeft++;
            NextThrowIndex--;
            ball.ctx.resolved = true;
            activeBalls.Remove(ball);
            Destroy(ball.gameObject);
            Juice.PlayClick();
            UI.Toast("Relance", Palette.Cyan);
            SetPhase(Phase.Aiming);
        }

        void UpdateDramaticSlowMo()
        {
            if (slowMoTimer > 0f) return;
            foreach (var b in activeBalls)
            {
                var p = b.transform.position;
                if (p.y < BoardLayout.BottomRowY + 0.2f && p.y > 0.45f && b.Velocity.y < 0f)
                {
                    double remaining = System.Math.Max(1, Run.Target - RoundScore);
                    double potential = b.ctx.Preview * 10;
                    if (potential >= remaining * 0.25) { slowMoTimer = 0.45f; break; }
                }
            }
        }

        void UpdateTime()
        {
            slowMoTimer = Mathf.Max(0f, slowMoTimer - Time.unscaledDeltaTime);
            charmSlowTimer = Mathf.Max(0f, charmSlowTimer - Time.unscaledDeltaTime);
            float ts = 1f;
            if (Run != null) foreach (var r in Run.relics) ts *= r.TimeScale;
            if (slowMoTimer > 0f) ts *= 0.3f;
            if (charmSlowTimer > 0f) ts *= 0.4f;
            Time.timeScale = ts;
            Time.fixedDeltaTime = Mathf.Max(0.004f, 0.02f * ts);
        }

        void AfterAllBallsResolved()
        {
            RollWind();
            if (RoundWon) { WinRound(); return; }
            if (ThrowsLeft <= 0 || drawPile.Count == 0) { LoseRun(); return; }
            SetPhase(Phase.Aiming);
            UI.Refresh();
        }

        void WinRound()
        {
            var lines = new List<(string, int)>();
            lines.Add((Run.SeuilName + " franchi", Run.BaseReward));
            if (ThrowsLeft > 0) lines.Add(($"Billes restantes ({ThrowsLeft})", ThrowsLeft));
            int interest = Run.Interest;
            if (interest > 0) lines.Add(("Intérêts (1 par 5 tickets, max 5)", interest));
            int total = lines.Sum(l => l.Item2);
            Run.tickets += total;
            Run.seuilsCleared++;
            foreach (var r in Run.relics.ToList()) r.OnSeuilCleared(this);
            Juice.PlayCash();
            SetPhase(Phase.RoundResult);
            UI.ShowResults(lines, total);
        }

        public void ContinueAfterResults()
        {
            Juice.PlayClick();
            if (Run.IsFinalSeuil) { SetPhase(Phase.Victory); UI.ShowVictory(); return; }
            OpenShop();
        }

        public void ContinueEndless()
        {
            Run.endless = true;
            OpenShop();
        }

        void LoseRun()
        {
            SetPhase(Phase.GameOver);
            Juice.PlayFail();
            UI.ShowGameOver();
        }

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

        // ================= Divers =================

        public void AddTickets(int n, string reason)
        {
            if (n == 0) return;
            Run.tickets += n;
            UI.TicketPulse(n);
        }

        public void NotifyRelicFired(Relic r) => UI.RelicFired(r);

        void SetPhase(Phase p)
        {
            if (p != Phase) phaseSince = Time.unscaledTime;
            Phase = p;
            UI?.Refresh();
        }
    }
}
