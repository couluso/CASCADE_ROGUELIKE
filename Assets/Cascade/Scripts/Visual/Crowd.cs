// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;

namespace Cascade
{
    /// <summary>Un client de la salle : les articulations animées par Crowd.</summary>
    [System.Serializable]
    public class CrowdMember
    {
        public Transform root, body, torso, head, armL, armR, legL, legR, kneeL, kneeR;
        public bool seated;
    }

    /// <summary>
    /// Le fond de la salle : des rangées de machines à sous et de bornes d'arcade, des joueurs assis qui s'agitent
    /// et des passants qui flânent au hasard dans les allées (ils s'arrêtent, regardent une machine, repartent).
    /// Construit dans la scène avec le décor (Arcade.BuildRoom) ; en jeu, il est seulement animé.
    /// </summary>
    public class Crowd : MonoBehaviour
    {
        public const float FloorY = -8f;
        /// <summary>Rangées de machines (z) ; les joueurs sont assis devant, côté caméra.</summary>
        public static readonly float[] RowZ = { 13f, 22f, 31f };
        /// <summary>Allées de circulation : le long des rangées (z) et en travers (x, là où une rangée est coupée).</summary>
        public static readonly float[] LaneZ = { 6.5f, 16.5f, 25.5f };
        public static readonly float[] AisleX = { -39.6f, -21.6f, 0f, 21.6f, 39.6f };
        const float SlotStep = 3.6f;
        const int SlotsPerSide = 10;

        [SerializeField] List<CrowdMember> members = new List<CrowdMember>();
        [SerializeField] List<Renderer> toppers = new List<Renderer>();
        [SerializeField] List<Material> topperOn = new List<Material>();
        [SerializeField, Range(0f, 1f)] float seatedChance = 0.45f;
        [SerializeField] int walkers = 12;

        public bool IsBuilt => members.Count > 0;

        // ---------- État en jeu (jamais enregistré) ----------
        class Brain
        {
            public CrowdMember m;
            public float phase, speed, wait, celebrate;
            public int ix, iz, toX, toZ;
            public Vector2 offset;
            public Vector3 target;
            public bool midStop;
            public float faceYaw;
            public float pullTimer, pull;
        }
        readonly List<Brain> brains = new List<Brain>();
        float topperTimer;
        Material topperOff;

        void Awake()
        {
            if (!IsBuilt) Build();
            topperOff = Mats.Get("SlotTopOff", new Color(0.1f, 0.08f, 0.1f), 0f, 0.6f, new Color(0.02f, 0.015f, 0.02f));
            brains.Clear();
            foreach (var m in members)
            {
                if (m == null || m.root == null) continue;
                var b = new Brain
                {
                    m = m,
                    phase = Random.value * 10f,
                    speed = Random.Range(3.5f, 5.5f),
                    pullTimer = Random.Range(1f, 6f),
                    offset = new Vector2(Random.Range(-0.9f, 0.9f), Random.Range(-0.9f, 0.9f)),
                };
                if (!m.seated)
                {
                    b.ix = Random.Range(0, AisleX.Length);
                    b.iz = Random.Range(0, LaneZ.Length);
                    b.toX = b.toZ = -1;
                    m.root.position = Node(b, b.ix, b.iz);
                    ChooseNext(b);
                }
                brains.Add(b);
            }
        }

        // ================================================================= Construction

        /// <summary>Machines, tabourets, joueurs assis et passants, sous cet objet.</summary>
        public void Build()
        {
            members.Clear();
            toppers.Clear();
            topperOn.Clear();
            var machines = new GameObject("Machines").transform;
            machines.SetParent(transform, false);
            var people = new GameObject("People").transform;
            people.SetParent(transform, false);

            var rnd = new System.Random(1234);
            int n = 0;
            foreach (float z in RowZ)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int k = 0; k < SlotsPerSide; k++)
                    {
                        if (k == 5) continue;   // allée transversale
                        float x = side * SlotStep * (k + 1);
                        bool arcade = rnd.NextDouble() < 0.3;
                        BuildMachine(machines, new Vector3(x, FloorY, z), n++, arcade);
                        if (rnd.NextDouble() < seatedChance)
                            members.Add(BuildPerson(people, new Vector3(x, FloorY, z - 3.5f), 0f, true, rnd));
                    }
                }
            }
            for (int i = 0; i < walkers; i++)
                members.Add(BuildPerson(people, new Vector3(AisleX[i % AisleX.Length], FloorY, LaneZ[i % LaneZ.Length]),
                    (float)rnd.NextDouble() * 360f, false, rnd));
        }

        static readonly Color[] ScreenColors =
        {
            new Color(0.1f, 0.9f, 1f), new Color(1f, 0.25f, 0.7f), new Color(1f, 0.75f, 0.2f),
            new Color(0.3f, 1f, 0.45f), new Color(0.6f, 0.35f, 1f),
        };

        void BuildMachine(Transform parent, Vector3 pos, int index, bool arcade)
        {
            var root = new GameObject(arcade ? "ArcadeCabinet" : "SlotMachine").transform;
            root.SetParent(parent, false);
            root.position = pos;
            int c = index % ScreenColors.Length;
            var col = ScreenColors[c];
            var body = arcade ? Mats.Get("ArcadeBody" + c, col * 0.18f, 0.1f, 0.6f) : Mats.Get("SlotBody", new Color(0.1f, 0.06f, 0.1f), 0.2f, 0.7f);
            var screen = Mats.Get("SlotScreen" + c, col * 0.2f, 0f, 0.9f, col * 0.9f);
            var top = Mats.Get("SlotTop" + c, col * 0.4f, 0f, 0.8f, col * 1.8f);

            // Socle, pupitre incliné, corps haut avec l'écran, fronton lumineux
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.8f, 0f), new Vector3(3.2f, 3.6f, 2.6f), body, "Base");
            var deck = Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 3.75f, -1.0f), new Vector3(3.3f, 0.35f, 1.3f), Mats.Chrome, "Deck");
            deck.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 6.3f, 0.3f), new Vector3(3.2f, 4.6f, 2.0f), body, "Upper");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 6.4f, -0.72f), new Vector3(2.6f, 3.2f, 0.05f), screen, "Screen");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 8.65f, 0.3f), new Vector3(3.3f, 0.12f, 2.1f), Mats.Chrome, "Trim");
            if (arcade)
            {
                // Borne d'arcade : joystick et boutons sur le pupitre
                Mats.Prim(PrimitiveType.Cylinder, root, new Vector3(-0.7f, 4.1f, -1.1f), new Vector3(0.12f, 0.25f, 0.12f), Mats.Chrome, "Stick");
                Mats.Prim(PrimitiveType.Sphere, root, new Vector3(-0.7f, 4.4f, -1.1f), Vector3.one * 0.3f, top, "Ball");
            }
            else
            {
                // Machine à sous : levier sur le côté
                Mats.Prim(PrimitiveType.Cylinder, root, new Vector3(1.75f, 6f, -0.2f), new Vector3(0.12f, 1.1f, 0.12f), Mats.Chrome, "Lever");
                Mats.Prim(PrimitiveType.Sphere, root, new Vector3(1.75f, 7.15f, -0.2f), Vector3.one * 0.42f, Mats.Get("LeverKnob", new Color(0.7f, 0.05f, 0.05f), 0.1f, 0.8f), "Knob");
            }
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 9.4f, 0.3f), new Vector3(3.4f, 1.4f, 2.2f), body, "Topper");
            var face = Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 9.4f, -0.82f), new Vector3(3.0f, 1.0f, 0.05f), top, "TopperFace");
            toppers.Add(face.GetComponent<Renderer>());
            topperOn.Add(top);

            // Tabouret devant la machine
            Mats.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 1.2f, -3.5f), new Vector3(0.25f, 1.2f, 0.25f), Mats.Chrome, "StoolPole");
            Mats.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 2.45f, -3.5f), new Vector3(1.3f, 0.12f, 1.3f), Mats.Get("StoolSeat", new Color(0.35f, 0.04f, 0.05f), 0f, 0.5f), "StoolSeat");
            Mats.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.05f, -3.5f), new Vector3(1f, 0.05f, 1f), Mats.Chrome, "StoolFoot");
        }

        static readonly Color[] ClothColors =
        {
            new Color(0.55f, 0.08f, 0.1f), new Color(0.1f, 0.2f, 0.45f), new Color(0.12f, 0.35f, 0.2f), new Color(0.6f, 0.5f, 0.15f),
            new Color(0.35f, 0.12f, 0.45f), new Color(0.75f, 0.72f, 0.68f), new Color(0.15f, 0.15f, 0.17f), new Color(0.65f, 0.3f, 0.1f),
        };
        static readonly Color[] PantsColors = { new Color(0.1f, 0.1f, 0.12f), new Color(0.12f, 0.15f, 0.3f), new Color(0.3f, 0.25f, 0.2f), new Color(0.2f, 0.2f, 0.2f) };
        static readonly Color[] SkinColors = { new Color(0.95f, 0.78f, 0.65f), new Color(0.8f, 0.6f, 0.45f), new Color(0.55f, 0.38f, 0.26f), new Color(0.33f, 0.22f, 0.15f) };
        static readonly Color[] HairColors = { new Color(0.08f, 0.06f, 0.05f), new Color(0.35f, 0.2f, 0.1f), new Color(0.75f, 0.6f, 0.35f), new Color(0.6f, 0.6f, 0.6f) };

        static T Pick<T>(T[] a, System.Random rnd) => a[rnd.Next(a.Length)];

        /// <summary>Un bonhomme en primitives (environ 9 unités de haut), regard vers +z local.</summary>
        CrowdMember BuildPerson(Transform parent, Vector3 pos, float yaw, bool seated, System.Random rnd)
        {
            int ci = rnd.Next(ClothColors.Length), pi = rnd.Next(PantsColors.Length);
            int si = rnd.Next(SkinColors.Length), hi = rnd.Next(HairColors.Length);
            var cloth = Mats.Get("Cloth" + ci, ClothColors[ci], 0f, 0.3f);
            var pants = Mats.Get("Pants" + pi, PantsColors[pi], 0f, 0.25f);
            var skin = Mats.Get("Skin" + si, SkinColors[si], 0f, 0.35f);
            var hair = Mats.Get("Hair" + hi, HairColors[hi], 0f, 0.4f);
            var shoes = Mats.Get("Shoes", new Color(0.05f, 0.04f, 0.04f), 0f, 0.6f);
            float girth = 0.9f + (float)rnd.NextDouble() * 0.3f;

            var m = new CrowdMember { seated = seated };
            m.root = new GameObject(seated ? "Player" : "Walker").transform;
            m.root.SetParent(parent, false);
            m.root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

            float hipY = seated ? 2.9f : 4.3f;
            m.body = Pivot(m.root, "Body", new Vector3(0f, hipY, 0f));
            Mats.Prim(PrimitiveType.Cube, m.body, Vector3.zero, new Vector3(1.6f * girth, 0.8f, 0.95f * girth), pants, "Hips");

            m.torso = Pivot(m.body, "Torso", new Vector3(0f, 0.2f, 0f));
            Mats.Prim(PrimitiveType.Capsule, m.torso, new Vector3(0f, 1.5f, 0f), new Vector3(1.75f * girth, 1.55f, 1.05f * girth), cloth, "Chest");
            m.head = Pivot(m.torso, "Head", new Vector3(0f, 3.2f, 0f));
            Mats.Prim(PrimitiveType.Cylinder, m.head, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.2f, 0.5f), skin, "Neck");
            Mats.Prim(PrimitiveType.Sphere, m.head, new Vector3(0f, 0.75f, 0f), new Vector3(1.15f, 1.25f, 1.15f), skin, "Face");
            Mats.Prim(PrimitiveType.Sphere, m.head, new Vector3(0f, 1.02f, -0.1f), new Vector3(1.24f, 0.95f, 1.24f), hair, "Hair");

            m.armL = Arm(m.torso, -1f, girth, cloth, skin);
            m.armR = Arm(m.torso, 1f, girth, cloth, skin);
            m.legL = Leg(m.body, -1f, pants, shoes, out m.kneeL);
            m.legR = Leg(m.body, 1f, pants, shoes, out m.kneeR);

            if (seated)
            {
                // Assis : cuisses à l'horizontale, tibias à la verticale, mains sur le pupitre
                m.legL.localRotation = m.legR.localRotation = Quaternion.Euler(-88f, 0f, 0f);
                m.kneeL.localRotation = m.kneeR.localRotation = Quaternion.Euler(88f, 0f, 0f);
                m.armL.localRotation = m.armR.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            }
            return m;
        }

        static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        static Transform Arm(Transform torso, float side, float girth, Material cloth, Material skin)
        {
            var shoulder = Pivot(torso, side < 0 ? "ArmL" : "ArmR", new Vector3(side * 1.05f * girth, 2.75f, 0f));
            Mats.Prim(PrimitiveType.Capsule, shoulder, new Vector3(0f, -1.3f, 0f), new Vector3(0.5f, 1.35f, 0.5f), cloth, "Sleeve");
            Mats.Prim(PrimitiveType.Sphere, shoulder, new Vector3(0f, -2.75f, 0f), Vector3.one * 0.5f, skin, "Hand");
            return shoulder;
        }

        static Transform Leg(Transform body, float side, Material pants, Material shoes, out Transform knee)
        {
            var hip = Pivot(body, side < 0 ? "LegL" : "LegR", new Vector3(side * 0.45f, -0.1f, 0f));
            Mats.Prim(PrimitiveType.Capsule, hip, new Vector3(0f, -1.05f, 0f), new Vector3(0.8f, 1.1f, 0.8f), pants, "Thigh");
            knee = Pivot(hip, "Knee", new Vector3(0f, -2.1f, 0f));
            Mats.Prim(PrimitiveType.Capsule, knee, new Vector3(0f, -1.0f, 0f), new Vector3(0.7f, 1.05f, 0.7f), pants, "Shin");
            Mats.Prim(PrimitiveType.Cube, knee, new Vector3(0f, -2.05f, 0.25f), new Vector3(0.75f, 0.35f, 1.15f), shoes, "Shoe");
            return hip;
        }

        // ================================================================= Animation

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool frenzy = Arcade.Instance != null && Arcade.Instance.InFrenzy;
            foreach (var b in brains)
            {
                if (b.m.root == null) continue;
                if (b.m.seated) AnimateSeated(b, dt, frenzy);
                else AnimateWalker(b, dt);
            }
            FlickerToppers(dt);
        }

        void AnimateSeated(Brain b, float dt, bool frenzy)
        {
            var m = b.m;
            b.phase += dt;
            // Coup de levier / de bouton de temps en temps, bras levés quand la borne s'emballe
            b.pullTimer -= dt;
            if (b.pullTimer <= 0f) { b.pull = 1f; b.pullTimer = Random.Range(2f, 7f); }
            b.pull = Mathf.MoveTowards(b.pull, 0f, dt * 1.8f);
            b.celebrate = Mathf.MoveTowards(b.celebrate, frenzy && (b.offset.x > -0.2f) ? 1f : 0f, dt * 3f);

            m.torso.localRotation = Quaternion.Euler(4f + Mathf.Sin(b.phase * 0.8f) * 3f, 0f, Mathf.Sin(b.phase * 0.55f + b.offset.y) * 3f);
            m.head.localRotation = Quaternion.Euler(Mathf.Sin(b.phase * 0.3f) * 6f, Mathf.Sin(b.phase * 0.37f + b.offset.x * 3f) * 20f, 0f);
            float pullArc = Mathf.Sin(b.pull * Mathf.PI) * 45f;
            float wave = Mathf.Sin(b.phase * 9f) * 12f;
            m.armL.localRotation = Quaternion.Euler(Mathf.Lerp(-60f, -165f, b.celebrate), 0f, Mathf.Lerp(0f, -15f + wave, b.celebrate));
            m.armR.localRotation = Quaternion.Euler(Mathf.Lerp(-60f - pullArc, -165f, b.celebrate), 0f, Mathf.Lerp(0f, 15f - wave, b.celebrate));
            m.body.localPosition = new Vector3(0f, 2.9f + b.celebrate * Mathf.Abs(Mathf.Sin(b.phase * 9f)) * 0.25f, 0f);
        }

        Vector3 Node(Brain b, int ix, int iz) => new Vector3(AisleX[ix] + b.offset.x, FloorY, LaneZ[iz] + b.offset.y);

        /// <summary>Prochaine étape au hasard : un carrefour voisin, parfois avec un arrêt devant une machine en chemin.</summary>
        void ChooseNext(Brain b)
        {
            var options = new List<Vector2Int>();
            if (b.ix > 0) options.Add(new Vector2Int(b.ix - 1, b.iz));
            if (b.ix < AisleX.Length - 1) options.Add(new Vector2Int(b.ix + 1, b.iz));
            if (b.iz > 0) options.Add(new Vector2Int(b.ix, b.iz - 1));
            if (b.iz < LaneZ.Length - 1) options.Add(new Vector2Int(b.ix, b.iz + 1));
            // Évite de faire demi-tour si on peut (toX / toZ : le carrefour d'où l'on vient)
            if (options.Count > 1) options.RemoveAll(o => o.x == b.toX && o.y == b.toZ);
            var next = options[Random.Range(0, options.Count)];
            b.toX = next.x; b.toZ = next.y;
            var dest = Node(b, b.toX, b.toZ);
            b.midStop = next.y == b.iz && Random.value < 0.4f;
            b.target = b.midStop ? Vector3.Lerp(Node(b, b.ix, b.iz), dest, Random.Range(0.2f, 0.8f)) : dest;
        }

        void AnimateWalker(Brain b, float dt)
        {
            var m = b.m;
            if (b.wait > 0f)
            {
                b.wait -= dt;
                m.root.rotation = Quaternion.Slerp(m.root.rotation, Quaternion.Euler(0f, b.faceYaw, 0f), 1f - Mathf.Exp(-dt * 4f));
                Stand(b, dt);
                return;
            }

            var pos = m.root.position;
            var to = b.target - pos;
            to.y = 0f;
            float dist = to.magnitude;
            float step = b.speed * dt;
            if (dist <= step)
            {
                m.root.position = b.target;
                if (b.midStop)
                {
                    // Regarde une machine de la rangée devant (ou derrière) l'allée
                    b.midStop = false;
                    b.wait = Random.Range(2f, 5f);
                    b.faceYaw = Random.value < 0.75f ? 0f : 180f;
                    b.target = Node(b, b.toX, b.toZ);
                }
                else
                {
                    int prevX = b.ix, prevZ = b.iz;
                    b.ix = b.toX; b.iz = b.toZ;
                    b.toX = prevX; b.toZ = prevZ;   // ChooseNext évite ce carrefour
                    if (Random.value < 0.3f) { b.wait = Random.Range(0.6f, 2.5f); b.faceYaw = Random.Range(0f, 360f); }
                    ChooseNext(b);
                }
                return;
            }

            var dir = to / dist;
            m.root.position = pos + dir * step;
            m.root.rotation = Quaternion.Slerp(m.root.rotation, Quaternion.LookRotation(dir, Vector3.up), 1f - Mathf.Exp(-dt * 8f));

            // Cycle de marche : jambes et bras en opposition, petit rebond du corps
            b.phase += dt * b.speed * 0.75f;
            float s = Mathf.Sin(b.phase);
            m.legL.localRotation = Quaternion.Euler(-s * 28f, 0f, 0f);
            m.legR.localRotation = Quaternion.Euler(s * 28f, 0f, 0f);
            m.kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, s) * 40f, 0f, 0f);
            m.kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, -s) * 40f, 0f, 0f);
            m.armL.localRotation = Quaternion.Euler(s * 24f, 0f, -4f);
            m.armR.localRotation = Quaternion.Euler(-s * 24f, 0f, 4f);
            m.torso.localRotation = Quaternion.Euler(3f, s * 4f, 0f);
            m.head.localRotation = Quaternion.identity;
            m.body.localPosition = new Vector3(0f, 4.3f + Mathf.Abs(Mathf.Cos(b.phase)) * 0.15f, 0f);
        }

        /// <summary>Debout immobile : les membres reviennent au repos, la tête balaie les machines.</summary>
        void Stand(Brain b, float dt)
        {
            var m = b.m;
            float k = 1f - Mathf.Exp(-dt * 6f);
            m.legL.localRotation = Quaternion.Slerp(m.legL.localRotation, Quaternion.identity, k);
            m.legR.localRotation = Quaternion.Slerp(m.legR.localRotation, Quaternion.identity, k);
            m.kneeL.localRotation = Quaternion.Slerp(m.kneeL.localRotation, Quaternion.identity, k);
            m.kneeR.localRotation = Quaternion.Slerp(m.kneeR.localRotation, Quaternion.identity, k);
            m.armL.localRotation = Quaternion.Slerp(m.armL.localRotation, Quaternion.Euler(0f, 0f, -5f), k);
            m.armR.localRotation = Quaternion.Slerp(m.armR.localRotation, Quaternion.Euler(0f, 0f, 5f), k);
            b.phase += dt;
            m.head.localRotation = Quaternion.Euler(-5f, Mathf.Sin(b.phase * 0.9f) * 25f, 0f);
            m.body.localPosition = Vector3.Lerp(m.body.localPosition, new Vector3(0f, 4.3f, 0f), k);
        }

        void FlickerToppers(float dt)
        {
            topperTimer -= dt;
            if (topperTimer > 0f || toppers.Count == 0) return;
            topperTimer = 0.12f;
            for (int i = 0; i < 3; i++)
            {
                int j = Random.Range(0, toppers.Count);
                if (toppers[j] == null || j >= topperOn.Count) continue;
                bool on = toppers[j].sharedMaterial != topperOn[j];
                toppers[j].sharedMaterial = on ? topperOn[j] : (Random.value < 0.5f ? topperOff : topperOn[j]);
            }
        }
    }
}
