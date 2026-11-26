// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Un pic de la planche : tige chromée plantée dans le feutre + capuchon coloré selon son type.
    /// Collision en 2D (CircleCollider2D), rendu en 3D. Effets : enflammé, gelé, neutralisé, « pop » au contact.
    /// </summary>
    public class Peg : MonoBehaviour
    {
        [Tooltip("Position dans la grille de la RunState (col < 0 : butoir de mur).")]
        public int row, col;
        public PegType type;
        [System.NonSerialized] public bool disabled;     // Boss Gardien des Pics
        [System.NonSerialized] public bool fragile;      // Boss Mur de Glace : se brise au premier contact
        [System.NonSerialized] public int burning;       // lancers restants enflammé
        [System.NonSerialized] public int contacts;      // contacts pendant la manche (glace)
        [System.NonSerialized] public bool broken;
        public bool JustBroke { get; private set; }

        [SerializeField, HideInInspector] CircleCollider2D col2d;
        [SerializeField, HideInInspector] Renderer pin, hole;
        [SerializeField, HideInInspector] Transform capRoot;
        [SerializeField, HideInInspector] Renderer[] capParts = new Renderer[0];
        [SerializeField, HideInInspector] PegType builtCap = (PegType)(-1);
        float pop, flash;
        bool animating;

        public void Init(int row, int col, PegType type, PhysicsMaterial2D mat)
        {
            this.row = row; this.col = col;
            col2d = gameObject.AddComponent<CircleCollider2D>();
            col2d.radius = BoardLayout.PegRadius;
            col2d.sharedMaterial = mat;

            pin = Mats.CylinderZ(transform, new Vector3(0f, 0f, 0.17f), BoardLayout.PegRadius * 0.75f, 0.6f, Mats.Chrome, "Pin").GetComponent<Renderer>();
            hole = Mats.CylinderZ(transform, new Vector3(0f, 0f, 0.41f), 0.07f, 0.02f, Mats.Hole, "Hole").GetComponent<Renderer>();
            capRoot = new GameObject("Cap").transform;
            capRoot.SetParent(transform, false);
            SetType(type);
        }

        public void SetType(PegType t)
        {
            type = t;
            bool exists = t != PegType.None && !broken;
            col2d.enabled = exists;
            pin.enabled = exists;
            hole.enabled = !exists;   // un emplacement vide montre le trou : on peut y repeindre un pic
            pin.sharedMaterial = t == PegType.WallBump ? Mats.Brass : Mats.Chrome;
            BuildCap();
            capRoot.gameObject.SetActive(exists && t != PegType.WallBump);
            Refresh();
        }

        /// <summary>
        /// Chaque type a sa forme de capuchon (en plus de sa couleur) : on les reconnaît même à travers le filtre,
        /// et même si on distingue mal certaines couleurs.
        /// </summary>
        void BuildCap()
        {
            if (builtCap == type) return;
            builtCap = type;
            for (int i = capRoot.childCount - 1; i >= 0; i--)
            {
                var child = capRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            var parts = new System.Collections.Generic.List<Renderer>();
            const float z = -0.14f;
            float r = BoardLayout.PegRadius;
            switch (type)
            {
                case PegType.Multi:      // dôme rose
                    parts.Add(Mats.Prim(PrimitiveType.Sphere, capRoot, new Vector3(0f, 0f, z), new Vector3(r * 2.6f, r * 2.6f, r * 1.6f), Mats.Cap, "Dome").GetComponent<Renderer>());
                    break;
                case PegType.Gold:       // pièce dorée large
                    parts.Add(Mats.CylinderZ(capRoot, new Vector3(0f, 0f, z), r * 1.7f, 0.06f, Mats.Cap, "Coin").GetComponent<Renderer>());
                    break;
                case PegType.Bumper:     // anneau vert épais
                    parts.Add(Mats.CylinderZ(capRoot, new Vector3(0f, 0f, z + 0.02f), r * 1.9f, 0.1f, Mats.Cap, "Ring").GetComponent<Renderer>());
                    Mats.CylinderZ(capRoot, new Vector3(0f, 0f, z - 0.04f), r * 1.0f, 0.02f, Mats.Hole, "RingCenter");
                    break;
                case PegType.Ice:        // losange (cube tourné)
                    var d = Mats.Prim(PrimitiveType.Cube, capRoot, new Vector3(0f, 0f, z), new Vector3(r * 2f, r * 2f, 0.08f), Mats.Cap, "Diamond");
                    d.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    parts.Add(d.GetComponent<Renderer>());
                    break;
                case PegType.Spiky:      // étoile à 8 branches
                    for (int k = 0; k < 2; k++)
                    {
                        var s1 = Mats.Prim(PrimitiveType.Cube, capRoot, new Vector3(0f, 0f, z), new Vector3(r * 2.6f, r * 0.7f, 0.06f), Mats.Cap, "Spike");
                        s1.transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
                        var s2 = Mats.Prim(PrimitiveType.Cube, capRoot, new Vector3(0f, 0f, z), new Vector3(r * 0.7f, r * 2.6f, 0.06f), Mats.Cap, "Spike");
                        s2.transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
                        parts.Add(s1.GetComponent<Renderer>());
                        parts.Add(s2.GetComponent<Renderer>());
                    }
                    break;
                default:                 // tête de clou chromée
                    parts.Add(Mats.CylinderZ(capRoot, new Vector3(0f, 0f, z), r * 1.15f, 0.05f, Mats.Cap, "Head").GetComponent<Renderer>());
                    break;
            }
            capParts = parts.ToArray();
        }

        /// <summary>Nouvelle run : oublie aussi les flammes des lancers précédents.</summary>
        public void ResetForRun()
        {
            burning = 0;
            ResetForRound();
        }

        public void ResetForRound()
        {
            broken = false; contacts = 0; disabled = false; fragile = false; JustBroke = false;
            SetType(type);
        }

        public void TickBurning() { if (burning > 0) burning--; Refresh(); }
        public void Ignite(int throws) { burning = Mathf.Max(burning, throws); Refresh(); }

        Color BaseColor(out float glow)
        {
            Color c;
            switch (type)
            {
                // Teintes très séparées (rose, jaune, vert, cyan, orange) et lumineuses
                case PegType.Multi: c = new Color(1f, 0.1f, 0.55f); glow = 2.2f; break;
                case PegType.Gold: c = new Color(1f, 0.78f, 0.05f); glow = 1.6f; break;
                case PegType.Bumper: c = new Color(0.1f, 1f, 0.3f); glow = 1.6f; break;
                case PegType.Ice: c = new Color(0.35f, 0.9f, 1f); glow = 1.5f; break;
                case PegType.Spiky: c = new Color(1f, 0.35f, 0.02f); glow = 1.8f; break;
                default: c = new Color(0.78f, 0.78f, 0.82f); glow = 0f; break;
            }
            if (disabled) { c = new Color(0.12f, 0.12f, 0.14f); glow = 0f; }
            else
            {
                if (fragile) { c = Color.Lerp(c, new Color(0.7f, 0.92f, 1f), 0.6f); glow = Mathf.Max(glow, 0.5f); }
                if (burning > 0) { c = Color.Lerp(c, new Color(1f, 0.45f, 0.1f), 0.7f); glow = Mathf.Max(glow, 1.2f); }
            }
            return c;
        }

        void Refresh()
        {
            if (capRoot == null || !capRoot.gameObject.activeSelf) return;
            var c = BaseColor(out float glow);
            float flick = burning > 0 && !disabled ? 0.35f * Mathf.Sin(Time.unscaledTime * 17f + row * 3f + col) : 0f;
            float e = glow + flick + flash * 2.5f;
            foreach (var part in capParts) if (part != null) Mats.Tint(part, c, c * Mathf.Max(0f, e));
            capRoot.localPosition = new Vector3(0f, 0f, -pop * 0.06f);
            capRoot.localScale = Vector3.one * (1f + pop * 0.25f);
        }

        /// <summary>Appelé à chaque contact.</summary>
        public void Hit()
        {
            JustBroke = false;
            contacts++;
            pop = 1f;
            flash = 1f;
            animating = true;
            if (disabled || type == PegType.WallBump) return;
            if (fragile || (type == PegType.Ice && contacts >= 2)) Break();
        }

        void Break()
        {
            broken = true;
            JustBroke = true;
            col2d.enabled = false;
            pin.enabled = false;
            capRoot.gameObject.SetActive(false);
            hole.enabled = true;
            GameManager.Instance?.Juice.Burst(transform.position, Palette.Ice, 14, 3f);
            GameManager.Instance?.Juice.PlayIceBreak();
        }

        void Update()
        {
            if (!animating && burning <= 0) return;
            float dt = Time.unscaledDeltaTime;
            pop = Mathf.MoveTowards(pop, 0f, dt * 5f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 4f);
            Refresh();
            if (pop <= 0f && flash <= 0f) animating = false;
        }
    }
}
