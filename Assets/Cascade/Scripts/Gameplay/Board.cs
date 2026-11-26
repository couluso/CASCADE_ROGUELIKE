// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// La planche : murs, cases et pics. Elle est construite dans la scène (Tools > Cascade > Construire la scène),
    /// où chaque pic est un objet qu'on peut voir et déplacer. En jeu, la RunState ne fait que changer le type des pics
    /// et les boss modifient la manche. S'il manque la planche (scène vide), elle est construite au lancement.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Board : MonoBehaviour
    {
        public List<Peg> pegs = new List<Peg>();
        [System.NonSerialized] public float[] slotValues = (float[])BoardLayout.BaseSlotValues.Clone();
        public PhysicsMaterial2D pegMaterial, wallMaterial;

        [Tooltip("Canvas monde qui porte les étiquettes des cases.")]
        [SerializeField] Transform worldCanvas;
        [SerializeField] Transform pegRoot;
        [SerializeField] Renderer[] slotRenderers;
        [SerializeField] UnityEngine.UI.Text[] slotLabels;
        readonly float[] slotFlash = new float[BoardLayout.SlotCount];

        public bool IsBuilt => pegs.Count > 0 && slotRenderers != null && slotRenderers.Length == BoardLayout.SlotCount;

        /// <summary>Construit murs, cases et pics (tous standard) sous cet objet.</summary>
        public void Build(Transform labelsCanvas)
        {
            worldCanvas = labelsCanvas;
            if (pegMaterial == null) pegMaterial = new PhysicsMaterial2D("Peg") { bounciness = BoardLayout.BallBounce, friction = 0.1f };
            if (wallMaterial == null) wallMaterial = new PhysicsMaterial2D("Wall") { bounciness = 0.35f, friction = 0.05f };

            BuildFrame();
            BuildSlots(worldCanvas);
            pegRoot = new GameObject("Pegs").transform;
            pegRoot.SetParent(transform, false);
            pegs.Clear();
            for (int r = 0; r < BoardLayout.Rows; r++)
            {
                for (int c = 0; c < BoardLayout.PegsInRow(r); c++)
                    CreatePeg(r, c, BoardLayout.PegPosition(r, c), PegType.Standard);
                if (r % 2 == 0)
                {
                    // Butoirs contre les murs : empêchent la bille de glisser jusqu'aux cases de bord.
                    CreatePeg(r, -1, new Vector2(-4.5f, BoardLayout.BottomRowY + r * BoardLayout.RowSpacing), PegType.WallBump);
                    CreatePeg(r, -2, new Vector2(4.5f, BoardLayout.BottomRowY + r * BoardLayout.RowSpacing), PegType.WallBump);
                }
            }
        }

        /// <summary>Début de run : remet les pics à neuf et leur donne les types de la RunState.</summary>
        public void Setup(RunState run, Transform labelsCanvas)
        {
            if (!IsBuilt) Build(labelsCanvas);
            foreach (var p in pegs) if (p != null) p.ResetForRun();
            ApplyPegTypes(run);
            RefreshSlots();
        }

        void BuildFrame()
        {
            float h = BoardLayout.WallTop;
            MakeWall("Wall_L", new Vector2(-BoardLayout.WallX - 0.1f, h / 2f), new Vector2(0.2f, h), true);
            MakeWall("Wall_R", new Vector2(BoardLayout.WallX + 0.1f, h / 2f), new Vector2(0.2f, h), true);
            MakeWall("Floor", new Vector2(0f, -0.15f), new Vector2(BoardLayout.WallX * 2f + 0.4f, 0.3f), false);
            for (int i = 1; i < BoardLayout.SlotCount; i++)
            {
                float x = -4.5f + i;
                MakeWall("Divider_" + i, new Vector2(x, BoardLayout.DividerHeight / 2f), new Vector2(0.06f, BoardLayout.DividerHeight), false);
            }
        }

        void MakeWall(string name, Vector2 pos, Vector2 size, bool isSideWall)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.sharedMaterial = wallMaterial;
            if (isSideWall) go.AddComponent<Wall>();

            // Rail en laiton (visuel 3D, la collision reste en 2D)
            Mats.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0f, 0.15f), new Vector3(size.x, size.y, 0.65f), Mats.Brass, "Rail");
        }

        void BuildSlots(Transform worldCanvas)
        {
            slotRenderers = new Renderer[BoardLayout.SlotCount];
            slotLabels = new UnityEngine.UI.Text[BoardLayout.SlotCount];
            for (int i = 0; i < BoardLayout.SlotCount; i++)
            {
                var go = new GameObject("Slot_" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector2(BoardLayout.SlotCenterX(i), 0.25f);
                var trig = go.AddComponent<BoxCollider2D>();
                trig.isTrigger = true;
                trig.size = new Vector2(BoardLayout.SlotWidth - 0.1f, 0.3f);
                go.AddComponent<Slot>().index = i;

                // Fond de case rétroéclairé (couleur selon la valeur)
                var back = Mats.Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.08f, 0.38f), new Vector3(BoardLayout.SlotWidth - 0.08f, 0.62f, 0.04f), Mats.Glow, "Backlight");
                slotRenderers[i] = back.GetComponent<Renderer>();

                var label = UIKit.WorldLabel(worldCanvas, new Vector2(BoardLayout.SlotCenterX(i), -0.45f), "x1", 30, Palette.Gold);
                var lp = label.transform.position; lp.z = -0.72f; label.transform.position = lp;
                slotLabels[i] = label;
            }
            RefreshSlots();
        }

        /// <summary>Donne à chaque pic de la scène le type enregistré dans la RunState (row / col du pic).</summary>
        public void ApplyPegTypes(RunState run)
        {
            foreach (var p in pegs)
            {
                if (p == null || p.col < 0) continue;   // butoirs de mur
                if (p.row < run.pegs.Length && p.col < run.pegs[p.row].Length) p.SetType(run.pegs[p.row][p.col]);
            }
        }

        void CreatePeg(int r, int c, Vector2 pos, PegType t)
        {
            var go = new GameObject($"Peg_{r}_{c}");
            go.transform.SetParent(pegRoot, false);
            go.transform.localPosition = pos;
            var peg = go.AddComponent<Peg>();
            peg.Init(r, c, t, pegMaterial);
            pegs.Add(peg);
        }

        /// <summary>Remet la planche à neuf pour une manche et applique les effets de boss.</summary>
        public void ResetForRound(RunState run)
        {
            pegs.RemoveAll(p => p == null);   // pic supprimé à la main dans la scène
            foreach (var p in pegs) p.ResetForRound();
            slotValues = (float[])BoardLayout.BaseSlotValues.Clone();

            if (run.HasBoss(BossType.PegWarden))
            {
                var editable = pegs.FindAll(p => p.type != PegType.WallBump && p.type != PegType.None);
                run.Shuffle(editable);
                int n = Mathf.RoundToInt(editable.Count * 0.3f);
                for (int i = 0; i < n; i++) { editable[i].disabled = true; editable[i].SetType(editable[i].type); }
            }
            if (run.HasBoss(BossType.IceWall))
                foreach (var p in pegs) if (p.type != PegType.WallBump) { p.fragile = true; p.SetType(p.type); }
            if (run.HasBoss(BossType.Cheater)) ShuffleSlots(run);
            RefreshSlots();
        }

        public void ShuffleSlots(RunState run)
        {
            var list = new List<float>(BoardLayout.BaseSlotValues);
            run.Shuffle(list);
            slotValues = list.ToArray();
            RefreshSlots();
            for (int i = 0; i < slotFlash.Length; i++) slotFlash[i] = 0.6f;
        }

        public void OnThrow() { foreach (var p in pegs) p.TickBurning(); }

        public float EffectiveSlotValue(int i, GameManager gm)
        {
            float v = slotValues[i];
            if (gm.Run == null) return v;
            foreach (var r in gm.Run.relics) v *= r.SlotValueMultiplier;
            if (gm.Run.HasBoss(BossType.Banker)) v = Mathf.Max(1f, v * 0.5f);
            return v;
        }

        public void RefreshSlots()
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < BoardLayout.SlotCount; i++)
            {
                float v = gm != null ? EffectiveSlotValue(i, gm) : slotValues[i];
                if (slotLabels != null && slotLabels[i] != null) slotLabels[i].text = "x" + Fmt.Num(v);
                if (slotRenderers != null && slotRenderers[i] != null) { var c = SlotColor(v); Mats.Tint(slotRenderers[i], c * 0.4f, c * 0.6f); }
            }
        }

        public static Color SlotColor(float v)
        {
            if (v >= 10) return Palette.Gold;
            if (v >= 5) return Palette.Pink;
            if (v >= 3) return Palette.Violet;
            if (v >= 2) return Palette.Cyan;
            return Palette.Dim;
        }

        public void FlashSlot(int i) { if (i >= 0 && i < slotFlash.Length) slotFlash[i] = 1f; }

        void Update()
        {
            if (slotRenderers == null) return;
            var gm = GameManager.Instance;
            for (int i = 0; i < slotRenderers.Length; i++)
            {
                if (slotRenderers[i] == null) continue;
                slotFlash[i] = Mathf.MoveTowards(slotFlash[i], 0f, Time.unscaledDeltaTime * 1.5f);
                float v = gm != null ? EffectiveSlotValue(i, gm) : slotValues[i];
                var c = SlotColor(v);
                Mats.Tint(slotRenderers[i], c * 0.4f, c * (0.5f + slotFlash[i] * 2.5f));
                if (slotLabels[i] != null) slotLabels[i].transform.localScale = Vector3.one * (UIKit.WorldTextScale * (1f + slotFlash[i] * 0.5f));
            }
        }

        public Peg PegAt(Vector2 world)
        {
            Peg best = null; float bestD = 0.35f;
            foreach (var p in pegs)
            {
                if (p == null || p.type == PegType.WallBump) continue;
                float d = Vector2.Distance(p.transform.position, world);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
