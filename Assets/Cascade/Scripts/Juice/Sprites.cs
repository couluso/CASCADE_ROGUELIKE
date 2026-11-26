// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>Sprites générés par code (aucun asset nécessaire) : cercle, halo, carré, anneau, triangle, panneau arrondi.</summary>
    public static class Sprites
    {
        static Sprite circle, glow, square, softSquare, ring, triangle, rounded, star;
        static Sprite heart, diamond, club, spade, chip, ticket, cardFace, scratch, flame;

        // Les formes sont calculées en pixels (bord anti-aliasé de ~1,5 px) et générées en haute résolution
        // avec mipmaps : elles restent nettes en plein écran comme en petite fenêtre.
        public static Sprite Circle => circle = Alive(circle) ?? Make(256, (x, y, px) => Disc(x, y, 0.5f - px, px * 1.5f));
        public static Sprite Glow => glow = Alive(glow) ?? Make(256, (x, y, px) => { float d = Dist(x, y); float a = Mathf.Clamp01(1f - d / 0.5f); return a * a; });
        public static Sprite Square => square = Alive(square) ?? Make(16, (x, y, px) => 1f, mip: false);
        public static Sprite SoftSquare => softSquare = Alive(softSquare) ?? Make(128, (x, y, px) =>
        {
            float dx = Mathf.Abs(x - 0.5f) * 2f, dy = Mathf.Abs(y - 0.5f) * 2f;
            return Mathf.Clamp01((1f - dx) * 4f) * Mathf.Clamp01((1f - dy) * 4f);
        });
        public static Sprite Ring => ring = Alive(ring) ?? Make(256, (x, y, px) =>
        {
            float d = Dist(x, y);
            return Mathf.Clamp01((0.04f - Mathf.Abs(d - 0.44f)) / (px * 1.5f));
        });
        public static Sprite Triangle => triangle = Alive(triangle) ?? Make(128, (x, y, px) =>
        {
            // pointe vers le bas, bords anti-aliasés
            float half = y * 0.5f;
            float edge = half - Mathf.Abs(x - 0.5f);
            return Mathf.Clamp01(edge / (px * 1.5f)) * Mathf.Clamp01((1f - y) / (px * 1.5f) + 1f);
        });
        public static Sprite Star => star = Alive(star) ?? Make(128, (x, y, px) =>
        {
            float dx = Mathf.Abs(x - 0.5f), dy = Mathf.Abs(y - 0.5f);
            return Mathf.Clamp01(1f - (dx * dy) * 60f) * Mathf.Clamp01(1f - Dist(x, y) * 2f);
        });

        // ---------- Symboles de cartes (dessinés par code : les polices n'en ont pas) ----------
        public static Sprite Heart => heart = Alive(heart) ?? Make(128, (x, y, px) => HeartShape(x, y, px));
        public static Sprite Diamond => diamond = Alive(diamond) ?? Make(128, (x, y, px) =>
            Mathf.Clamp01((0.42f - (Mathf.Abs(x - 0.5f) * 1.25f + Mathf.Abs(y - 0.5f))) / (px * 1.5f)));
        public static Sprite Spade => spade = Alive(spade) ?? Make(128, (x, y, px) =>
        {
            float body = HeartShape(x, 1f - y + 0.08f, px);
            float stem = Mathf.Clamp01((0.06f - Mathf.Abs(x - 0.5f) + (0.3f - y) * 0.35f) / (px * 1.5f)) * (y < 0.42f && y > 0.1f ? 1f : 0f);
            return Mathf.Max(body, stem);
        });
        public static Sprite Club => club = Alive(club) ?? Make(128, (x, y, px) =>
        {
            float a = Disc(x, y - 0.17f, 0.17f, px * 1.5f, 0.5f, 0.5f);
            float b = Disc(x + 0.17f, y + 0.02f, 0.17f, px * 1.5f, 0.5f, 0.5f);
            float c = Disc(x - 0.17f, y + 0.02f, 0.17f, px * 1.5f, 0.5f, 0.5f);
            float stem = Mathf.Clamp01((0.05f - Mathf.Abs(x - 0.5f) + (0.3f - y) * 0.4f) / (px * 1.5f)) * (y < 0.45f && y > 0.1f ? 1f : 0f);
            return Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, stem));
        });

        static float HeartShape(float x, float y, float px)
        {
            // Courbe du cœur : (X² + Y² - 1)³ - X²Y³ <= 0
            float X = (x - 0.5f) * 2.6f, Y = (y - 0.45f) * 2.6f;
            float v = Mathf.Pow(X * X + Y * Y - 1f, 3f) - X * X * Y * Y * Y;
            return Mathf.Clamp01(-v / (px * 12f));
        }

        static float Disc(float x, float y, float r, float soft, float cx, float cy)
            => Mathf.Clamp01((r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy))) / soft);

        /// <summary>Décor de jeton de casino (à poser en blanc sur un disque coloré) : 8 encoches au bord + anneau intérieur.</summary>
        public static Sprite Chip => chip = Alive(chip) ?? Make(256, (x, y, px) =>
        {
            float d = Dist(x, y);
            float ang = Mathf.Atan2(y - 0.5f, x - 0.5f) / (Mathf.PI * 2f) + 0.5f;
            float notch = d > 0.37f && d < 0.5f - px * 2f && Mathf.Repeat(ang * 8f, 1f) < 0.4f ? 1f : 0f;
            float ring = Mathf.Clamp01((0.012f - Mathf.Abs(d - 0.32f)) / (px * 1.5f));
            return Mathf.Max(notch, ring);
        });

        /// <summary>Ticket de tombola : rectangle avec encoches demi-cercle sur les côtés et pointillés de découpe.</summary>
        public static Sprite Ticket => ticket = Alive(ticket) ?? Make(256, (x, y, px) =>
        {
            float notchL = Vector2.Distance(new Vector2(x, y), new Vector2(0f, 0.5f));
            float notchR = Vector2.Distance(new Vector2(x, y), new Vector2(1f, 0.5f));
            if (notchL < 0.11f || notchR < 0.11f) return 0f;
            bool perf = Mathf.Abs(x - 0.22f) < 0.006f && Mathf.Repeat(y * 18f, 1f) < 0.5f;
            float edge = Mathf.Min(Mathf.Min(x, 1f - x), Mathf.Min(y, 1f - y));
            return Mathf.Clamp01(edge / (px * 1.5f)) * (perf ? 0.25f : 1f);
        });

        /// <summary>Flammèche (flammes de dépassement de score).</summary>
        public static Sprite Flame => flame = Alive(flame) ?? Make(128, (x, y, px) =>
        {
            float dx = (x - 0.5f) / Mathf.Lerp(0.42f, 0.05f, y);
            float a = Mathf.Clamp01(1f - dx * dx - Mathf.Pow(Mathf.Abs(y - 0.35f) * 1.6f, 2f));
            return a * a;
        });

        /// <summary>Panneau arrondi 9-slice pour l'UI.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                int s = 128; float r = 32f;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float a = Mathf.Clamp01(r - d);
                        px[y * s + x] = new Color(1, 1, 1, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return rounded;
            }
        }

        public static Material TrailMaterial => Mats.Cached("Trail", () =>
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            return new Material(sh);
        });

        public static SpriteRenderer MakeRenderer(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = TrailMaterial; // Sprites/Default : non éclairé, marche en Built-in comme en URP (2D ou non)
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Les sprites générés vivent hors de la scène : quand Unity les décharge (sortie du mode Play sans rechargement
        /// du domaine), la référence statique devient un objet détruit que « ?? » ne voit pas. On la remet à null.
        /// </summary>
        static Sprite Alive(Sprite s) => s != null ? s : null;

        static float Dist(float x, float y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
        static float Disc(float x, float y, float r, float soft) => Mathf.Clamp01((r - Dist(x, y)) / soft);

        /// <summary>x, y dans [0,1] ; px = taille d'un pixel dans ces unités (pour l'anti-aliasing).</summary>
        delegate float Shape(float x, float y, float px);

        static Sprite Make(int size, Shape f, bool mip = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mip)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = mip ? FilterMode.Trilinear : FilterMode.Bilinear,
                anisoLevel = 1
            };
            var px = new Color[size * size];
            float p = 1f / size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color(1, 1, 1, f((x + 0.5f) / size, (y + 0.5f) / size, p));
            tex.SetPixels(px);
            tex.Apply(mip, true);
            // pixelsPerUnit = size : le sprite mesure 1 unité, on règle la taille avec l'échelle.
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
