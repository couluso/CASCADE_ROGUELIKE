// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using PhysMat3D = UnityEngine.PhysicsMaterial;
#else
using PhysMat3D = UnityEngine.PhysicMaterial;
#endif

namespace Cascade
{
    /// <summary>Bille physique dans le bocal du sac.</summary>
    public class BagBall : MonoBehaviour
    {
        public BallInstance ball;
        public Renderer body;
    }

    /// <summary>
    /// Le sac de billes : un bocal en verre posé devant la borne, rempli des vraies billes du sac (physique 3D).
    /// Glisser la souris fait tourner le bocal (les billes roulent dedans). Survoler une bille affiche son effet.
    /// En boutique, cliquer une bille la retire du sac.
    /// </summary>
    public class BagView : MonoBehaviour
    {
        public static BagView Instance { get; private set; }
        public bool IsOpen { get; private set; }

        GameManager gm;
        Transform jar;
        Rigidbody jarBody;
        readonly List<BagBall> balls = new List<BagBall>();
        Quaternion targetRot;
        Vector2 lastMouse, downMouse;
        bool dragging;
        bool removeMode;
        BagBall hovered;
        PhysMat3D ballMat;

        static readonly Vector3 JarPos = new Vector3(0f, 5.1f, -5.5f);
        const float JarRadius = 2.1f, JarHalfHeight = 1.7f, BallR = 0.3f;

        public void Init(GameManager gm)
        {
            Instance = this;
            this.gm = gm;
            ballMat = new PhysMat3D("BagBall") { bounciness = 0.25f, dynamicFriction = 0.3f, staticFriction = 0.3f };
        }

        public void Open(bool allowRemove)
        {
            Close();
            IsOpen = true;
            removeMode = allowRemove;
            gm.ModalOpen = true;
            Build();
        }

        public void Close()
        {
            if (jar != null) Destroy(jar.gameObject);
            foreach (var b in balls) if (b != null) Destroy(b.gameObject);
            balls.Clear();
            hovered = null;
            if (IsOpen) GameUI.Instance?.HideTooltip();
            IsOpen = false;
            if (gm != null) gm.ModalOpen = false;
        }

        void Build()
        {
            jar = new GameObject("BagJar").transform;
            jar.position = JarPos;
            targetRot = Quaternion.Euler(18f, 0f, 0f);
            jar.rotation = targetRot;
            jarBody = jar.gameObject.AddComponent<Rigidbody>();
            jarBody.isKinematic = true;
            jarBody.interpolation = RigidbodyInterpolation.Interpolate;

            // Visuel : bocal en verre, cerclages en laiton, couvercle
            var glass = Mats.Prim(PrimitiveType.Cylinder, jar, Vector3.zero, new Vector3(JarRadius * 2f, JarHalfHeight, JarRadius * 2f), Mats.Glass, "Glass");
            glass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Mats.Prim(PrimitiveType.Cylinder, jar, new Vector3(0f, JarHalfHeight, 0f), new Vector3(JarRadius * 2.12f, 0.08f, JarRadius * 2.12f), Mats.Brass, "RingTop");
            Mats.Prim(PrimitiveType.Cylinder, jar, new Vector3(0f, -JarHalfHeight, 0f), new Vector3(JarRadius * 2.12f, 0.08f, JarRadius * 2.12f), Mats.Brass, "RingBottom");
            Mats.Prim(PrimitiveType.Cylinder, jar, new Vector3(0f, JarHalfHeight + 0.12f, 0f), new Vector3(JarRadius * 1.6f, 0.06f, JarRadius * 1.6f), Mats.Lacquer, "Lid");
            Mats.Prim(PrimitiveType.Sphere, jar, new Vector3(0f, JarHalfHeight + 0.32f, 0f), Vector3.one * 0.35f, Mats.Brass, "Knob");

            // Collisions : paroi en 16 panneaux + fond + couvercle
            const int sides = 16;
            float panelW = 2f * Mathf.PI * (JarRadius + 0.15f) / sides * 1.15f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var w = new GameObject("Wall").transform;
                w.SetParent(jar, false);
                w.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (JarRadius + 0.15f);
                w.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                var bc = w.gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(panelW, JarHalfHeight * 2f + 0.4f, 0.3f);
            }
            AddCap(JarHalfHeight + 0.15f);
            AddCap(-JarHalfHeight - 0.15f);

            // Les billes du sac
            var rnd = new System.Random(gm.Run.bag.Count * 31 + 7);
            int i0 = 0;
            foreach (var b in gm.Run.bag)
            {
                float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float rad = (float)rnd.NextDouble() * (JarRadius - 0.6f);
                float h = -JarHalfHeight + 0.4f + (i0 % 6) * 0.55f + (float)rnd.NextDouble() * 0.2f;
                var local = new Vector3(Mathf.Cos(ang) * rad, Mathf.Min(h, JarHalfHeight - 0.4f), Mathf.Sin(ang) * rad);
                SpawnBall(b, jar.TransformPoint(local));
                i0++;
            }
        }

        void AddCap(float y)
        {
            var c = new GameObject("Cap").transform;
            c.SetParent(jar, false);
            c.localPosition = new Vector3(0f, y, 0f);
            var bc = c.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(JarRadius * 2.6f, 0.3f, JarRadius * 2.6f);
        }

        void SpawnBall(BallInstance b, Vector3 pos)
        {
            var info = Catalog.Ball(b.kind);
            var go = new GameObject("BagBall_" + b.kind);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.radius = BallR * info.radiusMul;
            col.sharedMaterial = ballMat;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var color = Color.Lerp(new Color(0.85f, 0.86f, 0.9f), info.color, b.kind == BallKind.Standard ? 0f : 0.65f);
            var mat = Mats.Get("Ball_" + b.kind, color, 0.85f, 0.88f, new Color(0.01f, 0.01f, 0.01f));
            var body = Mats.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * BallR * 2f * info.radiusMul, mat, "Body").GetComponent<Renderer>();
            var bb = go.AddComponent<BagBall>();
            bb.ball = b;
            bb.body = body;
            Highlight(bb, false);
            balls.Add(bb);
        }

        static void Highlight(BagBall bb, bool on)
        {
            var info = Catalog.Ball(bb.ball.kind);
            var color = Color.Lerp(new Color(0.85f, 0.86f, 0.9f), info.color, bb.ball.kind == BallKind.Standard ? 0f : 0.65f);
            var e = bb.ball.edition != Edition.None ? Catalog.EditionColor(bb.ball.edition) * 0.8f : Color.black;
            if (on) e += Color.Lerp(Color.white, info.color, 0.5f) * 0.9f;
            Mats.Tint(bb.body, color, e);
        }

        void FixedUpdate()
        {
            if (!IsOpen || jarBody == null) return;
            jarBody.MoveRotation(Quaternion.Slerp(jarBody.rotation, targetRot, 0.2f));
            // Sécurité : une bille sortie du bocal y est remise.
            foreach (var b in balls)
            {
                if (b == null) continue;
                var local = jar.InverseTransformPoint(b.transform.position);
                if (new Vector2(local.x, local.z).magnitude > JarRadius + 0.5f || Mathf.Abs(local.y) > JarHalfHeight + 0.6f)
                {
                    b.transform.position = jar.TransformPoint(Vector3.zero);
                    var rb = b.GetComponent<Rigidbody>();
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                }
            }
        }

        void Update()
        {
            if (!IsOpen) return;
            var cam = Arcade.Instance != null ? Arcade.Instance.Cam : Camera.main;
            Vector2 mouse = InputShim.MousePosition;

            // Rotation à la souris (glisser), sinon rotation lente
            if (InputShim.LeftClick) { dragging = true; downMouse = mouse; lastMouse = mouse; }
            if (dragging && InputShim.LeftHeld)
            {
                var d = mouse - lastMouse;
                targetRot = Quaternion.AngleAxis(-d.x * 0.35f, Vector3.up) * Quaternion.AngleAxis(d.y * 0.35f, Vector3.right) * targetRot;
                lastMouse = mouse;
            }
            else targetRot = Quaternion.AngleAxis(10f * Time.unscaledDeltaTime, Vector3.up) * targetRot;

            // Survol d'une bille
            BagBall over = null;
            if (cam != null)
            {
                var ray = cam.ScreenPointToRay(new Vector3(mouse.x, mouse.y, 0f));
                float best = float.MaxValue;
                foreach (var h in Physics.RaycastAll(ray, 100f))
                {
                    var bb = h.collider.GetComponent<BagBall>();
                    if (bb != null && h.distance < best) { best = h.distance; over = bb; }
                }
            }
            if (over != hovered)
            {
                if (hovered != null) Highlight(hovered, false);
                hovered = over;
                if (hovered != null)
                {
                    Highlight(hovered, true);
                    string extra = removeMode ? $"\n<color=#E0414F>Clic : retirer du sac ({Shop.RemoveBallCost} tickets)</color>" : "";
                    GameUI.Instance?.ShowTooltip($"<b>{hovered.ball.Name}</b>\n{hovered.ball.Desc}{extra}");
                    gm.Juice.PlayClick();
                }
                else GameUI.Instance?.HideTooltip();
            }

            // Clic court (sans glisser) sur une bille en boutique : la retirer
            if (InputShim.LeftReleased)
            {
                bool click = (mouse - downMouse).magnitude < 6f;
                dragging = false;
                if (click && removeMode && hovered != null)
                {
                    var target = hovered;
                    if (gm.RemoveBallFromBag(target.ball))
                    {
                        gm.Juice.Burst(target.transform.position, Palette.Red, 20, 3f, keepZ: true);
                        balls.Remove(target);
                        Destroy(target.gameObject);
                        hovered = null;
                        GameUI.Instance?.HideTooltip();
                    }
                }
            }
        }
    }
}
