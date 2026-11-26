// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Bille en chute : physique 2D, effets de son type, pouvoirs actifs (coup de pouce, secousse, frein),
    /// détection des pics / murs / changements de direction, arrivée dans une case.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BallController : MonoBehaviour
    {
        [System.NonSerialized] public ScoreContext ctx;
        [System.NonSerialized] public bool IsTwinChild;

        GameManager gm;
        Rigidbody2D rb;
        CircleCollider2D circle;
        Renderer body;
        Light glowLight;
        TrailRenderer trail;
        Color bodyColor;
        Catalog.BallInfo info;

        Peg lastPeg;
        float lastPegTime;
        float lastVxSign;
        // Détection de blocage : position relevée toutes les 0,3 s sur 1,5 s
        readonly Vector2[] stuckSamples = new Vector2[5];
        int stuckSampleCount, stuckSampleIndex;
        float stuckSampleTimer;
        /// <summary>La bille ne bouge presque plus depuis 1,5 s : le joueur doit secouer la table (R).</summary>
        public bool IsStuck { get; private set; }
        public float StuckTime { get; private set; }
        const float StuckSampleStep = 0.3f, StuckDistance = 0.35f, StuckAutoFreeAfter = 12f;
        float frozenUntil;
        bool frozen;
        Vector2 frozenVelocity;
        bool hasSplit;
        public bool NudgeUsed { get; private set; }
        public bool Resolved => ctx == null || ctx.resolved;

        public void Init(GameManager gm, ScoreContext ctx, Vector2 pos, Vector2 velocity, float radiusScale = 1f)
        {
            this.gm = gm;
            this.ctx = ctx;
            ctx.controller = this;
            info = Catalog.Ball(ctx.ball.kind);
            transform.position = pos;

            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = BoardLayout.BallGravityScale * info.gravityScale;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = BoardLayout.BallDrag;
            rb.linearVelocity = velocity;
#else
            rb.drag = BoardLayout.BallDrag;
            rb.velocity = velocity;
#endif
            float radius = BoardLayout.BallRadius * info.radiusMul * radiusScale;
            circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.sharedMaterial = new PhysicsMaterial2D("Ball") { bounciness = info.bounce, friction = 0.1f };

            // Bille d'acier chromée, teintée selon son type ; les éditions brillent de leur couleur.
            bodyColor = Color.Lerp(new Color(0.85f, 0.86f, 0.9f), info.color, ctx.ball.kind == BallKind.Standard ? 0f : 0.65f);
            var mat = Mats.Get("Ball_" + ctx.ball.kind, bodyColor, 0.85f, 0.88f, new Color(0.01f, 0.01f, 0.01f));
            body = Mats.Prim(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * radius * 2f, mat, "Body").GetComponent<Renderer>();
            var edition = ctx.ball.edition != Edition.None ? Catalog.EditionColor(ctx.ball.edition) : Color.black;
            Mats.Tint(body, bodyColor, RestEmission());

            // Petite lumière portée : la bille éclaire les pics qu'elle frôle.
            glowLight = new GameObject("BallLight").AddComponent<Light>();
            glowLight.transform.SetParent(transform, false);
            glowLight.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            glowLight.type = LightType.Point;
            glowLight.color = ctx.ball.edition != Edition.None ? edition : Color.Lerp(Color.white, info.color, 0.5f);
            glowLight.intensity = 0.9f;
            glowLight.range = 1.8f;
            glowLight.shadows = LightShadows.None;

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = radius * 1.2f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.material = Sprites.TrailMaterial;
            trail.startColor = Color.Lerp(Color.white, info.color, 0.5f).WithAlpha(0.25f);
            trail.endColor = info.color.WithAlpha(0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

#if UNITY_6000_0_OR_NEWER
        public Vector2 Velocity { get => rb.linearVelocity; set => rb.linearVelocity = value; }
#else
        public Vector2 Velocity { get => rb.velocity; set => rb.velocity = value; }
#endif

        public void IgnoreCollisionWith(BallController other)
        {
            if (other != null && other.circle != null && circle != null) Physics2D.IgnoreCollision(circle, other.circle, true);
        }

        void FixedUpdate()
        {
            if (Resolved) return;
            if (frozen)
            {
                if (Time.time >= frozenUntil) Unfreeze();
                else return;
            }

            ctx.airTime += Time.fixedDeltaTime;
            var v = Velocity;

            // Vent du Nord
            if (gm.Run.HasBoss(BossType.NorthWind))
                rb.AddForce(new Vector2(gm.Wind, 0f), ForceMode2D.Force);

            // Changement de direction horizontale
            if (Mathf.Abs(v.x) > 0.4f)
            {
                float s = Mathf.Sign(v.x);
                if (lastVxSign != 0f && s != lastVxSign)
                {
                    ctx.dirChanges++;
                    gm.OnDirectionChange(this);
                }
                lastVxSign = s;
            }

            UpdateStuck();

            // Sécurité
            if (transform.position.y < -2f || ctx.airTime > 60f) gm.ResolveBall(this, -1, lost: true, reason: "Perdue");
        }

        /// <summary>
        /// Bille coincée : elle a parcouru moins de StuckDistance en 1,5 s. On prévient le joueur (secouer avec R) ;
        /// au bout de 12 s sans succès, une petite poussée évite de bloquer la partie.
        /// </summary>
        void UpdateStuck()
        {
            stuckSampleTimer += Time.fixedDeltaTime;
            if (stuckSampleTimer >= StuckSampleStep)
            {
                stuckSampleTimer = 0f;
                stuckSamples[stuckSampleIndex] = transform.position;
                stuckSampleIndex = (stuckSampleIndex + 1) % stuckSamples.Length;
                stuckSampleCount = Mathf.Min(stuckSampleCount + 1, stuckSamples.Length);
                bool stuck = stuckSampleCount == stuckSamples.Length;
                if (stuck)
                {
                    Vector2 here = transform.position;
                    foreach (var p in stuckSamples)
                        if ((p - here).sqrMagnitude > StuckDistance * StuckDistance) { stuck = false; break; }
                }
                IsStuck = stuck;
            }
            StuckTime = IsStuck ? StuckTime + Time.fixedDeltaTime : 0f;
            if (StuckTime > StuckAutoFreeAfter)
            {
                StuckTime = 0f;
                stuckSampleCount = 0;
                IsStuck = false;
                Velocity = new Vector2(transform.position.x > 0 ? -1.6f : 1.6f, 1.2f);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (Resolved || frozen) return;
            ctx.collisions++;

            var peg = collision.collider.GetComponent<Peg>();
            if (peg != null)
            {
                if (peg == lastPeg && Time.time - lastPegTime < 0.06f) return;
                lastPeg = peg; lastPegTime = Time.time;
                gm.OnPegHit(this, peg);

                if (ctx.ball.kind == BallKind.Twin && !hasSplit && !IsTwinChild && !Resolved)
                {
                    hasSplit = true;
                    gm.SplitTwin(this);
                }
            }
            else if (collision.collider.GetComponent<Wall>() != null)
            {
                ctx.wallHits++;
                ctx.touchedWall = true;
                gm.OnWallHit(this);
            }

            if (ctx.ball.kind == BallKind.Crystal && ctx.collisions >= Catalog.CrystalBreakAt && !Resolved)
            {
                gm.Juice.Burst(transform.position, info.color, 30, 5f);
                gm.Juice.PlayIceBreak();
                gm.ResolveBall(this, -1, lost: true, reason: "Brisée !", keepChipsAndMult: true);
            }
        }

        public void Land(int slotIndex)
        {
            if (Resolved) return;
            gm.ResolveBall(this, slotIndex, lost: false);
        }

        // ---------------- Pouvoirs actifs ----------------

        public void Nudge(float dir)
        {
            if (NudgeUsed || Resolved) return;
            NudgeUsed = true;
            Velocity += new Vector2(dir * 2.8f, 0.6f);
            gm.Juice.Burst(transform.position, Palette.Cyan, 8, 2f);
        }

        public void Shake(float strength)
        {
            if (Resolved) return;
            float side = gm.Run.RandomValue() < 0.5 ? -1f : 1f;   // graine de la run
            Velocity += new Vector2(side * strength, strength * 0.8f);
        }

        public void Freeze(float seconds)
        {
            if (Resolved || frozen) return;
            frozen = true;
            frozenUntil = Time.time + seconds;
            frozenVelocity = Velocity;
            rb.bodyType = RigidbodyType2D.Kinematic;
            Velocity = Vector2.zero;
            Mats.Tint(body, Color.Lerp(bodyColor, Palette.Ice, 0.6f), Palette.Ice * 0.6f);
        }

        void Unfreeze()
        {
            frozen = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            Velocity = frozenVelocity * 0.5f;
            Mats.Tint(body, bodyColor, RestEmission());
        }

        /// <summary>Lueur normale de la bille : couleur de son édition + légère teinte de son type.</summary>
        Color RestEmission()
        {
            var edition = ctx.ball.edition != Edition.None ? Catalog.EditionColor(ctx.ball.edition) : Color.black;
            return edition * 0.8f + info.color * (ctx.ball.kind == BallKind.Standard ? 0f : 0.12f);
        }

        public void ApplyBumper(Vector2 from)
        {
            var dir = ((Vector2)transform.position - from).normalized;
            if (dir.y < 0.2f) dir.y = 0.2f;
            Velocity = dir.normalized * Catalog.BumperImpulse;
        }


    }
}
