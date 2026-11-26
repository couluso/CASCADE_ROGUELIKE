// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Chef d'orchestre d'une run : seuils, manches, lancers, score, boutique, charmes, fin de partie.
    /// Les autres systèmes (planche, lanceur, effets, interface) sont des objets de la scène, reliés dans l'inspecteur.
    /// </summary>
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] Board board;
        [SerializeField] Launcher launcher;
        [SerializeField] Juice juice;
        [SerializeField] GameUI ui;
        [Tooltip("0 = graine aléatoire. Mets une valeur fixe pour rejouer la même run (boutiques, boss, tirages).")]
        [SerializeField] int fixedSeed;

        public RunState Run { get; private set; }
        public Board Board => board;
        public Launcher Launcher => launcher;
        public Juice Juice => juice;
        public GameUI UI => ui;
        public Shop Shop { get; } = new Shop();

        public Phase Phase { get; private set; }

        // ---------- État de la manche ----------
        public readonly List<BallInstance> drawPile = new List<BallInstance>();
        public readonly List<BallController> activeBalls = new List<BallController>();
        /// <summary>Dernière bille lâchée : c'est elle que pilotent coup de pouce, relance et frein.</summary>
        BallController lastLaunched;
        float lastLaunchTime;
        /// <summary>Délai minimal entre deux lâchers (on peut lâcher pendant qu'une autre bille tombe).</summary>
        public const float LaunchCooldown = 0.35f;
        public int ThrowsLeft { get; private set; }
        public int RelancesLeft { get; private set; }
        public double RoundScore { get; private set; }
        public int NextThrowIndex { get; private set; } = 1;
        public float Wind { get; private set; }
        public float Tilt { get; private set; }
        public bool BrakeUsed { get; private set; }
        public bool RoundWon { get; private set; }
        /// <summary>La run qui vient de se terminer a battu un record (seuils franchis ou meilleure bille).</summary>
        public bool NewRecord { get; private set; }
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
        float baseFixedDeltaTime;

        /// <summary>Relie les systèmes de la scène (utilisé par l'outil qui construit la scène).</summary>
        public void Wire(Board board, Launcher launcher, Juice juice, GameUI ui)
        {
            this.board = board; this.launcher = launcher; this.juice = juice; this.ui = ui;
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 120;
            Physics2D.queriesHitTriggers = false;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            baseFixedDeltaTime = Time.fixedDeltaTime;

            if (board == null) board = FindAnyObjectByType<Board>();
            if (launcher == null) launcher = FindAnyObjectByType<Launcher>();
            if (juice == null) juice = FindAnyObjectByType<Juice>();
            if (ui == null) ui = FindAnyObjectByType<GameUI>();
            if (board == null || launcher == null || juice == null || ui == null)
                Debug.LogError("[CASCADE] Il manque un système dans la scène (Board, Launcher, Juice ou GameUI). Utilise Tools > Cascade > Construire la scène.", this);
        }

        void Start()
        {
            NewRun(fixedSeed != 0 ? fixedSeed : (int?)null);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }

        public void NewRun(int? seed = null)
        {
            foreach (var b in activeBalls) if (b != null) Destroy(b.gameObject);
            activeBalls.Clear();
            Run = new RunState(seed ?? System.Environment.TickCount);
            pendingTallies.Clear();
            Tally = null;
            Board.Setup(Run, UI.WorldCanvas);
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
                        Board.ApplyPegTypes(Run);
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
            // Tiré avec la graine de la run : une graine fixe rejoue aussi le vent.
            Wind = Run.HasBoss(BossType.NorthWind) ? (Run.RandomValue() < 0.5 ? -1f : 1f) * (4f + 5f * (float)Run.RandomValue()) : 0f;
        }

        public BallInstance PeekNextBall() => drawPile.Count > 0 ? drawPile[0] : null;

        /// <summary>Une manche est en cours (visée, chute, ou ciblage d'un charme lancé pendant la visée).</summary>
        public bool InRound => Phase == Phase.Aiming || Phase == Phase.Falling || (Phase == Phase.Targeting && phaseBeforeTargeting == Phase.Aiming);

        /// <summary>Multiplicateur de vitesse du va-et-vient du lanceur (reliques, L'Aveugle).</summary>
        public float SweepSpeed
        {
            get
            {
                float k = 1f;
                foreach (var r in Run.relics) k *= r.SweepSpeedMultiplier;
                if (Run.HasBoss(BossType.Blind)) k *= 1.35f;
                return k;
            }
        }

        /// <summary>
        /// « Chaleur » du lancer en cours, de 0 à 1 : score de la bille par rapport à la cible (échelle log).
        /// Plus elle monte, plus les sons, particules et secousses des rebonds sont intenses.
        /// </summary>
        public float Heat(ScoreContext c)
        {
            if (c == null || Run == null || Run.Target <= 0) return 0f;
            double ratio = System.Math.Max(0, c.Preview / Run.Target);
            return Mathf.Clamp01((float)(System.Math.Log10(1 + 10 * ratio) / System.Math.Log10(11)));
        }

        public bool HasBrake => Run.relics.Any(r => r.GrantsBrake);

        /// <summary>Une bille en jeu est coincée : l'interface demande de secouer (R).</summary>
        public bool AnyBallStuck
        {
            get { foreach (var b in activeBalls) if (b != null && !b.Resolved && b.IsStuck) return true; return false; }
        }

        /// <summary>
        /// On peut lâcher pendant la visée, mais aussi pendant qu'une bille tombe encore.
        /// La manche se joue jusqu'à la dernière bille, même cible atteinte : le surplus rapporte des tickets.
        /// </summary>
        public bool CanLaunch => (Phase == Phase.Aiming || Phase == Phase.Falling)
            && ThrowsLeft > 0 && drawPile.Count > 0 && !ModalOpen
            && Time.unscaledTime - lastLaunchTime > LaunchCooldown;

        public void Launch(float x)
        {
            if (!CanLaunch) return;
            lastLaunchTime = Time.unscaledTime;
            var ball = drawPile[0];
            drawPile.RemoveAt(0);
            ThrowsLeft--;
            Board.OnThrow();

            var ctx = new ScoreContext { ball = ball, throwIndex = NextThrowIndex++ };
            if (ball.edition == Edition.Foil) ctx.chips += 50;
            if (ball.edition == Edition.Holo) ctx.mult += 10;

            float jitter = (float)(Run.RandomValue() * 0.04 - 0.02);   // graine de la run
            float radiusScale = Run.HasBoss(BossType.Colossus) ? 1.2f : 1f;
            var bc = SpawnBall(ctx, new Vector2(x + jitter, BoardLayout.DropY), Vector2.zero, radiusScale);
            lastLaunched = bc;
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
                    if (Time.unscaledTime - phaseSince > 0.2f && DropPressed) Launch(Launcher.AimX);
                    break;

                case Phase.Falling:
                    // Le lanceur reste actif : on peut lâcher la bille suivante sans attendre.
                    Launcher.active = ThrowsLeft > 0 && drawPile.Count > 0;
                    if (Launcher.active && DropPressed) Launch(Launcher.AimX);
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
                // Flammes seulement si une bille ÉCLATE la cible à elle seule : 1,5x (enfer à 3x).
                TallyOnFire = Tally.finalScore >= 1.5 * Run.Target;
                TallyInferno = Tally.finalScore >= 3 * Run.Target;
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
                    if (ThrowsLeft > 0 && drawPile.Count > 0) UI.Toast("Cible atteinte ! Le surplus rapporte des tickets", Palette.Gold);
                }
                Tally = null;
                TallyOnFire = TallyInferno = false;
                UI.Refresh();
            }
        }

        /// <summary>Instants (secondes) des étapes du décompte : jetons x mult, x case, x bonus, = total.</summary>
        public static readonly float[] TallySteps = { 0.05f, 0.35f, 0.6f, 0.85f };

        bool DropPressed => (InputShim.LeftClick && !InputShim.PointerOverUI()) || InputShim.Drop;

        void HandleFallingInput()
        {
            var main = lastLaunched != null && activeBalls.Contains(lastLaunched) ? lastLaunched
                : activeBalls.Count > 0 ? activeBalls[activeBalls.Count - 1] : null;
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

            if ((InputShim.Relance || InputShim.RightClick) && !main.IsTwinChild && main.ctx.pegHits == 0 && RelancesLeft > 0)
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
            lastLaunchTime = 0f;
            if (activeBalls.Count == 0) SetPhase(Phase.Aiming);
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
            Time.fixedDeltaTime = Mathf.Max(0.004f, baseFixedDeltaTime * ts);
        }

        void AfterAllBallsResolved()
        {
            RollWind();
            // La manche ne se termine qu'une fois toutes les billes jouées.
            if (ThrowsLeft > 0 && drawPile.Count > 0) { SetPhase(Phase.Aiming); UI.Refresh(); return; }
            if (RoundScore >= Run.Target) WinRound(); else LoseRun();
        }

        void WinRound()
        {
            var lines = new List<(string, int)>();
            lines.Add((Run.SeuilName + " franchi", Run.BaseReward));
            int surplus = RunState.SurplusBonus(RoundScore, Run.Target);
            if (surplus > 0) lines.Add(($"Surplus x{RoundScore / Run.Target:0.0#}", surplus));
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
            if (Run.IsFinalSeuil) { NewRecord = GameSettings.RecordRun(Run.seuilsCleared, Run.bestBallScore); SetPhase(Phase.Victory); UI.ShowVictory(); return; }
            OpenShop();
        }

        public void ContinueEndless()
        {
            Run.endless = true;
            OpenShop();
        }

        void LoseRun()
        {
            NewRecord = GameSettings.RecordRun(Run.seuilsCleared, Run.bestBallScore);
            SetPhase(Phase.GameOver);
            Juice.PlayFail();
            UI.ShowGameOver();
        }

        // ================= Divers =================

        public void AddTickets(int n, string reason)
        {
            if (n == 0) return;
            Run.tickets += n;
            UI.TicketPulse(n);
        }

        public void NotifyRelicFired(Relic r) { if (UI != null) UI.RelicFired(r); }

        void SetPhase(Phase p)
        {
            if (p != Phase) phaseSince = Time.unscaledTime;
            Phase = p;
            if (UI != null) UI.Refresh();
        }
    }
}
