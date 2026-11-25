// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Lanceur en laiton en haut de la planche : suit la souris (entre DropMinX et DropMaxX), montre la prochaine bille
    /// et une trajectoire fantôme partielle (2 rebonds de base, plus avec la Boussole, aucune contre L'Aveugle).
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        public bool active;
        public float AimX { get; private set; }

        GameManager gm;
        Transform carriage;
        Renderer nextBall;
        LineRenderer ghost;
        readonly List<Vector3> points = new List<Vector3>();
        Color nextColor = Color.white;

        public void Init(GameManager gm)
        {
            this.gm = gm;
            transform.position = new Vector3(0f, BoardLayout.DropY, 0f);

            // Rail du lanceur
            float span = BoardLayout.DropMaxX - BoardLayout.DropMinX + 1.2f;
            Mats.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.62f, -0.05f), new Vector3(span, 0.08f, 0.5f), Mats.Brass, "Rail");
            Mats.Prim(PrimitiveType.Cube, transform, new Vector3(-span / 2f, 0.45f, -0.05f), new Vector3(0.1f, 0.4f, 0.5f), Mats.Brass);
            Mats.Prim(PrimitiveType.Cube, transform, new Vector3(span / 2f, 0.45f, -0.05f), new Vector3(0.1f, 0.4f, 0.5f), Mats.Brass);

            // Chariot (pince qui tient la bille)
            carriage = new GameObject("Carriage").transform;
            carriage.SetParent(transform, false);
            Mats.Prim(PrimitiveType.Cube, carriage, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.5f, 0.2f, 0.6f), Mats.Lacquer, "Body");
            Mats.Prim(PrimitiveType.Cube, carriage, new Vector3(-0.32f, 0.3f, -0.05f), new Vector3(0.07f, 0.5f, 0.4f), Mats.Chrome, "ClawL");
            Mats.Prim(PrimitiveType.Cube, carriage, new Vector3(0.32f, 0.3f, -0.05f), new Vector3(0.07f, 0.5f, 0.4f), Mats.Chrome, "ClawR");
            var lamp = Mats.Get("LauncherLamp", new Color(1f, 0.3f, 0.2f), 0f, 0.8f, new Color(2f, 0.4f, 0.2f));
            Mats.Prim(PrimitiveType.Sphere, carriage, new Vector3(0f, 0.75f, -0.38f), Vector3.one * 0.12f, lamp, "Lamp");

            var mat = Mats.Get("NextBall", new Color(0.85f, 0.86f, 0.9f), 0.85f, 0.88f, new Color(0.01f, 0.01f, 0.01f));
            nextBall = Mats.Prim(PrimitiveType.Sphere, carriage, Vector3.zero, Vector3.one * BoardLayout.BallRadius * 2f, mat, "NextBall").GetComponent<Renderer>();

            var go = new GameObject("Ghost");
            go.transform.SetParent(transform, false);
            ghost = go.AddComponent<LineRenderer>();
            ghost.material = Sprites.TrailMaterial;
            ghost.widthMultiplier = 0.05f;
            ghost.numCapVertices = 4;
            ghost.useWorldSpace = true;
            ghost.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f), new GradientColorKey(Palette.Gold, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            ghost.colorGradient = grad;
        }

        public void ShowNext(BallInstance ball, bool hidden)
        {
            nextBall.enabled = ball != null;
            if (ball == null) return;
            var info = Catalog.Ball(ball.kind);
            nextColor = hidden ? new Color(0.15f, 0.15f, 0.17f)
                : Color.Lerp(new Color(0.85f, 0.86f, 0.9f), info.color, ball.kind == BallKind.Standard ? 0f : 0.65f);
            var e = !hidden && ball.edition != Edition.None ? Catalog.EditionColor(ball.edition) * 0.8f : Color.black;
            Mats.Tint(nextBall, nextColor, e);
            nextBall.transform.localScale = Vector3.one * BoardLayout.BallRadius * 2f * info.radiusMul;
        }

        void Update()
        {
            nextBall.gameObject.SetActive(active);
            ghost.enabled = active;
            if (!active) return;

            if (Arcade.Instance != null)
                AimX = Mathf.Clamp(Arcade.Instance.MouseOnBoard().x, BoardLayout.DropMinX, BoardLayout.DropMaxX);

            var target = new Vector3(AimX, 0f, 0f);
            carriage.localPosition = Vector3.Lerp(carriage.localPosition, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 25f));
            DrawGhost();
        }

        void DrawGhost()
        {
            int bounces = gm.PreviewBounces;
            if (bounces <= 0) { ghost.positionCount = 0; return; }

            var ball = gm.PeekNextBall();
            var info = Catalog.Ball(ball != null ? ball.kind : BallKind.Standard);
            float radius = BoardLayout.BallRadius * info.radiusMul * (gm.Run.HasBoss(BossType.Colossus) ? 1.2f : 1f);
            Vector2 g = Physics2D.gravity * BoardLayout.BallGravityScale * info.gravityScale;
            Vector2 p = new Vector2(AimX, BoardLayout.DropY);
            Vector2 v = Vector2.zero;
            float dt = 0.02f;
            const float z = -0.34f;
            points.Clear();
            points.Add(new Vector3(p.x, p.y, z));
            int hits = 0;
            for (int step = 0; step < 300 && hits < bounces; step++)
            {
                v += g * dt;
                v *= 1f / (1f + dt * BoardLayout.BallDrag);
                Vector2 delta = v * dt;
                float dist = delta.magnitude;
                if (dist < 1e-5f) continue;
                var hit = Physics2D.CircleCast(p, radius * 0.95f, delta / dist, dist);
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    p = hit.centroid + hit.normal * 0.01f;
                    float vn = Vector2.Dot(v, hit.normal);
                    if (vn < 0f) v -= (1f + info.bounce) * vn * hit.normal;
                    hits++;
                }
                else p += delta;
                if (step % 2 == 0) points.Add(new Vector3(p.x, p.y, z));
                if (p.y < 0.3f) break;
            }
            points.Add(new Vector3(p.x, p.y, z));
            ghost.positionCount = points.Count;
            ghost.SetPositions(points.ToArray());
        }
    }
}
