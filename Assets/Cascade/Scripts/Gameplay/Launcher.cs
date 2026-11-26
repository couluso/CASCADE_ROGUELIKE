// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Lanceur en laiton en haut de la planche : va et vient tout seul entre DropMinX et DropMaxX et montre la prochaine bille.
    /// Clic ou Espace : lâcher la bille là où se trouve le chariot (vitesse modifiée par la Boussole et L'Aveugle).
    /// Construit dans la scène (Tools > Cascade > Construire la scène) ; à défaut, au lancement.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class Launcher : MonoBehaviour
    {
        [System.NonSerialized] public bool active;
        public float AimX { get; private set; }

        [SerializeField] GameManager gm;
        [SerializeField] Transform carriage;
        [SerializeField] Renderer nextBall;
        [Tooltip("Vitesse du va-et-vient, en unités par seconde.")]
        [SerializeField] float sweepSpeed = 2.6f;
        float sweep;
        Color nextColor = Color.white;

        public bool IsBuilt => carriage != null && nextBall != null;

        void Awake()
        {
            if (!IsBuilt) Build();
            if (gm == null) gm = FindAnyObjectByType<GameManager>();
        }

        public void SetGameManager(GameManager manager) => gm = manager;

        /// <summary>Rail, chariot et bille suivante, sous cet objet.</summary>
        public void Build()
        {
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
            if (gm == null || gm.Run == null) return;
            nextBall.gameObject.SetActive(active);
            if (!active) return;

            // Va-et-vient automatique entre DropMinX et DropMaxX ; le temps de jeu (Horloger, ralenti) le ralentit aussi.
            float span = BoardLayout.DropMaxX - BoardLayout.DropMinX;
            sweep = Mathf.Repeat(sweep + Time.deltaTime * sweepSpeed * gm.SweepSpeed / span, 2f);
            AimX = BoardLayout.DropMinX + Mathf.PingPong(sweep, 1f) * span;
            carriage.localPosition = new Vector3(AimX, 0f, 0f);
        }
    }
}
