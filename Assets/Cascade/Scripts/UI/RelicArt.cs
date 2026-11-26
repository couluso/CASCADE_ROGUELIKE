// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Les reliques sont des petits pendentifs : chaque relique a sa forme, son émail et son symbole,
    /// le métal dit la rareté (bronze, argent, or, or rose). Tout est dessiné par code (formes à distance signée).
    /// </summary>
    public static class RelicArt
    {
        public enum Body { Round, Oval, Shield, Drop, Hexagon, Diamond, Heart, Star, Octagon, Square }

        public enum Glyph
        {
            Pig, Percent, Bolt, Rope, Pendulum, Hourglass, Sparkle, Anvil, Gems, Target,
            Dot, Dots, Snowflake, Flame, Bell, Needle, Bar, Pins, Clock, Twins, Question
        }

        public struct Design
        {
            public Body body;
            public Glyph glyph;
            public Color enamel, ink;
            public Design(Body b, Glyph g, Color enamel, Color ink) { body = b; glyph = g; this.enamel = enamel; this.ink = ink; }
        }

        static readonly Color Cream = new Color(0.98f, 0.93f, 0.8f);
        static readonly Color InkDark = new Color(0.12f, 0.07f, 0.06f);

        static readonly Dictionary<string, Design> designs = new Dictionary<string, Design>
        {
            { "piggy",     new Design(Body.Round,   Glyph.Pig,       new Color(0.93f, 0.55f, 0.62f), InkDark) },
            { "broker",    new Design(Body.Octagon, Glyph.Percent,   new Color(0.13f, 0.42f, 0.27f), Cream) },
            { "chaos",     new Design(Body.Diamond, Glyph.Bolt,      new Color(0.38f, 0.16f, 0.52f), new Color(1f, 0.9f, 0.35f)) },
            { "tightrope", new Design(Body.Oval,    Glyph.Rope,      new Color(0.42f, 0.66f, 0.85f), InkDark) },
            { "pendulum",  new Design(Body.Drop,    Glyph.Pendulum,  new Color(0.2f, 0.22f, 0.42f), Cream) },
            { "hourglass", new Design(Body.Oval,    Glyph.Hourglass, new Color(0.86f, 0.68f, 0.38f), InkDark) },
            { "star",      new Design(Body.Star,    Glyph.Sparkle,   new Color(0.1f, 0.14f, 0.36f), new Color(1f, 0.95f, 0.7f)) },
            { "forge",     new Design(Body.Shield,  Glyph.Anvil,     new Color(0.7f, 0.22f, 0.1f), Cream) },
            { "collector", new Design(Body.Hexagon, Glyph.Gems,      new Color(0.1f, 0.42f, 0.45f), new Color(0.85f, 1f, 0.95f)) },
            { "perfect",   new Design(Body.Round,   Glyph.Target,    new Color(0.75f, 0.12f, 0.16f), Cream) },
            { "minimal",   new Design(Body.Square,  Glyph.Dot,       new Color(0.14f, 0.13f, 0.15f), Cream) },
            { "maximal",   new Design(Body.Square,  Glyph.Dots,      new Color(0.9f, 0.5f, 0.15f), InkDark) },
            { "frost",     new Design(Body.Hexagon, Glyph.Snowflake, new Color(0.55f, 0.82f, 0.95f), new Color(0.1f, 0.2f, 0.35f)) },
            { "cauldron",  new Design(Body.Shield,  Glyph.Flame,     new Color(0.18f, 0.1f, 0.1f), new Color(1f, 0.55f, 0.15f)) },
            { "chime",     new Design(Body.Drop,    Glyph.Bell,      new Color(0.85f, 0.25f, 0.32f), Cream) },
            { "compass",   new Design(Body.Round,   Glyph.Needle,    new Color(0.12f, 0.2f, 0.33f), new Color(0.95f, 0.35f, 0.3f)) },
            { "brake",     new Design(Body.Octagon, Glyph.Bar,       new Color(0.78f, 0.1f, 0.12f), Cream) },
            { "pachinko",  new Design(Body.Shield,  Glyph.Pins,      new Color(0.05f, 0.25f, 0.16f), new Color(0.95f, 0.85f, 0.55f)) },
            { "clock",     new Design(Body.Round,   Glyph.Clock,     new Color(0.95f, 0.9f, 0.78f), InkDark) },
            { "twin",      new Design(Body.Heart,   Glyph.Twins,     new Color(0.45f, 0.22f, 0.6f), Cream) },
        };

        public static Design DesignOf(Relic r)
            => r != null && designs.TryGetValue(r.Id, out var d) ? d : new Design(Body.Round, Glyph.Question, new Color(0.3f, 0.3f, 0.35f), Cream);

        /// <summary>Métal du pendentif selon la rareté : bronze, argent, or, or rose.</summary>
        public static Color Metal(Rarity r) => r switch
        {
            Rarity.Common => new Color(0.74f, 0.47f, 0.27f),
            Rarity.Uncommon => new Color(0.8f, 0.82f, 0.87f),
            Rarity.Rare => new Color(0.98f, 0.78f, 0.32f),
            _ => new Color(1f, 0.72f, 0.6f)
        };

        // ================================================================= Construction UI

        /// <summary>
        /// Pendentif complet (chaîne, bélière, corps en métal, émail, symbole, reflet), pivot en haut (point d'accroche).
        /// size = largeur du corps en pixels de référence ; hauteur totale ≈ size * 1.45.
        /// </summary>
        public static PendantMotion Build(Transform parent, Relic r, float size)
        {
            var d = DesignOf(r);
            var metal = Metal(r.Rarity);
            var rt = UIKit.Rect(parent, "Pendant_" + r.Id);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(size, size * 1.45f);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);   // zone de survol

            var swing = UIKit.Rect(rt, "Swing");
            swing.anchorMin = swing.anchorMax = new Vector2(0.5f, 1f);
            swing.pivot = new Vector2(0.5f, 1f);
            swing.sizeDelta = rt.sizeDelta;
            swing.anchoredPosition = Vector2.zero;

            // Halo (légendaire : pulse en continu ; tous : flash quand la relique se déclenche)
            var halo = Img(swing, Sprites.Glow, Palette.ForRarity(r.Rarity).WithAlpha(0f), 0f, -size * 0.25f - size * 0.5f, size * 1.9f);

            // Chaîne : maillons alternés
            var chainCol = metal * 0.75f; chainCol.a = 1f;
            for (int i = 0; i < 3; i++)
            {
                var link = Img(swing, Sprites.Ring, chainCol, 0f, -size * (0.035f + i * 0.07f), size * 0.1f);
                link.rectTransform.sizeDelta = new Vector2(size * (i % 2 == 0 ? 0.1f : 0.06f), size * 0.1f);
            }
            // Bélière
            Img(swing, Sprites.Ring, metal, 0f, -size * 0.25f, size * 0.17f);

            float cy = -size * 0.3f - size * 0.5f;
            var bodySprite = BodySprite(d.body);
            Img(swing, bodySprite, new Color(0f, 0f, 0f, 0.4f), size * 0.04f, cy - size * 0.05f, size);    // ombre
            Img(swing, bodySprite, metal, 0f, cy, size);                                                 // métal
            var bevel = metal * 0.62f; bevel.a = 1f;
            Img(swing, bodySprite, bevel, 0f, cy, size * 0.88f);                                         // biseau
            Img(swing, bodySprite, d.enamel, 0f, cy, size * 0.8f);                                       // émail
            Img(swing, Sprites.Glow, new Color(0f, 0f, 0f, 0.28f), 0f, cy - size * 0.18f, size * 0.7f);  // creux de l'émail
            Img(swing, GlyphSprite(d.glyph), d.ink, 0f, cy, size * 0.52f);                               // symbole
            Img(swing, Sprites.Glow, new Color(1f, 1f, 1f, 0.4f), -size * 0.2f, cy + size * 0.2f, size * 0.38f);   // reflet
            Img(swing, Sprites.Circle, Palette.ForRarity(r.Rarity), 0f, cy + size * 0.42f, size * 0.12f);         // pierre de rareté

            var motion = rt.gameObject.AddComponent<PendantMotion>();
            motion.Init(swing, halo, r.Rarity == Rarity.Legendary, Palette.ForRarity(r.Rarity));
            return motion;
        }

        static Image Img(RectTransform parent, Sprite sp, Color c, float x, float y, float size)
        {
            var img = UIKit.Rect(parent, "Img").gameObject.AddComponent<Image>();
            img.sprite = sp;
            img.color = c;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, y);
            return img;
        }

        // ================================================================= Formes (SDF)

        static readonly Dictionary<Body, Sprite> bodies = new Dictionary<Body, Sprite>();
        static readonly Dictionary<Glyph, Sprite> glyphs = new Dictionary<Glyph, Sprite>();

        /// <summary>Distance signée : négatif à l'intérieur. p dans [-1, 1].</summary>
        delegate float Sdf(Vector2 p);

        public static Sprite BodySprite(Body b)
        {
            if (bodies.TryGetValue(b, out var s) && s != null) return s;
            Sdf f = b switch
            {
                Body.Round => p => p.magnitude - 0.9f,
                Body.Oval => p => Ellipse(p, 0.72f, 0.92f),
                Body.Shield => p => Mathf.Min(Box(p - new Vector2(0f, 0.25f), new Vector2(0.78f, 0.6f)), Mathf.Max(p.magnitude - 0.8f, p.y - 0.25f)),
                Body.Drop => p => Mathf.Min((p - new Vector2(0f, -0.25f)).magnitude - 0.62f, Triangle(p, new Vector2(0f, 0.95f), new Vector2(-0.56f, -0.02f), new Vector2(0.56f, -0.02f))),
                Body.Hexagon => p => Polygon(p, 6, 0.86f, 0f),
                Body.Diamond => p => (Mathf.Abs(p.x) / 0.72f + Mathf.Abs(p.y) / 0.95f - 1f) * 0.58f,
                Body.Heart => p => HeartSdf(p),
                Body.Star => p => Star(p, 0.95f, 0.5f),
                Body.Octagon => p => Polygon(p, 8, 0.88f, Mathf.PI / 8f),
                _ => p => Box(p, new Vector2(0.7f, 0.7f)) - 0.15f,
            };
            s = Make(f, 256);
            bodies[b] = s;
            return s;
        }

        public static Sprite GlyphSprite(Glyph g)
        {
            if (glyphs.TryGetValue(g, out var s) && s != null) return s;
            Sdf f = g switch
            {
                Glyph.Pig => p => Sub(Union(Ellipse(p - new Vector2(0f, -0.1f), 0.55f, 0.42f),
                                            Mathf.Min((p - new Vector2(-0.5f, 0.45f)).magnitude - 0.2f, (p - new Vector2(0.5f, 0.45f)).magnitude - 0.2f)),
                                      Mathf.Min((p - new Vector2(-0.14f, -0.12f)).magnitude - 0.09f, (p - new Vector2(0.14f, -0.12f)).magnitude - 0.09f)),
                Glyph.Percent => p => Union(Seg(p, new Vector2(-0.55f, -0.75f), new Vector2(0.55f, 0.75f), 0.11f),
                                            Mathf.Abs((p - new Vector2(-0.45f, 0.45f)).magnitude - 0.22f) - 0.08f,
                                            Mathf.Abs((p - new Vector2(0.45f, -0.45f)).magnitude - 0.22f) - 0.08f),
                Glyph.Bolt => p => Union(Triangle(p, new Vector2(0.25f, 0.95f), new Vector2(-0.5f, -0.05f), new Vector2(0.1f, -0.05f)),
                                         Triangle(p, new Vector2(-0.25f, -0.95f), new Vector2(0.5f, 0.1f), new Vector2(-0.1f, 0.1f))),
                Glyph.Rope => p => Union(Seg(p, new Vector2(-0.95f, -0.55f), new Vector2(0.95f, -0.55f), 0.05f),
                                         (p - new Vector2(0f, 0.55f)).magnitude - 0.18f,
                                         Seg(p, new Vector2(0f, 0.35f), new Vector2(0f, -0.15f), 0.08f),
                                         Seg(p, new Vector2(0f, -0.15f), new Vector2(-0.18f, -0.5f), 0.07f),
                                         Seg(p, new Vector2(0f, -0.15f), new Vector2(0.18f, -0.5f), 0.07f),
                                         Seg(p, new Vector2(-0.7f, 0.05f), new Vector2(0.7f, 0.28f), 0.05f)),
                Glyph.Pendulum => p => Union(Seg(p, new Vector2(0f, 0.9f), new Vector2(0.35f, -0.35f), 0.07f),
                                             (p - new Vector2(0.38f, -0.5f)).magnitude - 0.25f,
                                             // arc de balancement sous la bille
                                             Mathf.Max(Mathf.Abs((p - new Vector2(0f, 0.9f)).magnitude - 1.55f) - 0.04f, p.y + 0.3f, Mathf.Abs(p.x) - 0.8f)),
                Glyph.Hourglass => p => Union(Triangle(p, new Vector2(-0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0f, 0.02f)),
                                              Triangle(p, new Vector2(-0.5f, -0.7f), new Vector2(0.5f, -0.7f), new Vector2(0f, -0.02f)),
                                              Box(p - new Vector2(0f, 0.8f), new Vector2(0.62f, 0.08f)),
                                              Box(p - new Vector2(0f, -0.8f), new Vector2(0.62f, 0.08f))),
                Glyph.Sparkle => p => Union(Star4(p, 0.85f, 0.18f), (p - new Vector2(0.62f, 0.62f)).magnitude - 0.1f, (p - new Vector2(-0.6f, -0.55f)).magnitude - 0.07f),
                Glyph.Anvil => p => Union(Box(p - new Vector2(0.05f, 0.3f), new Vector2(0.62f, 0.16f)),
                                          Triangle(p, new Vector2(-0.57f, 0.46f), new Vector2(-0.95f, 0.36f), new Vector2(-0.57f, 0.14f)),
                                          Box(p - new Vector2(0.05f, -0.05f), new Vector2(0.25f, 0.22f)),
                                          Box(p - new Vector2(0.05f, -0.42f), new Vector2(0.5f, 0.14f))),
                Glyph.Gems => p => Union(Gem(p - new Vector2(0f, 0.38f)), Gem(p - new Vector2(-0.45f, -0.35f)), Gem(p - new Vector2(0.45f, -0.35f))),
                Glyph.Target => p => Union(Mathf.Abs(p.magnitude - 0.75f) - 0.1f, Mathf.Abs(p.magnitude - 0.42f) - 0.1f, p.magnitude - 0.14f),
                Glyph.Dot => p => p.magnitude - 0.3f,
                Glyph.Dots => p =>
                {
                    float m = 9f;
                    for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) m = Mathf.Min(m, (p - new Vector2(i * 0.55f, j * 0.55f)).magnitude - 0.17f);
                    return m;
                },
                Glyph.Snowflake => p =>
                {
                    float m = 9f;
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * Mathf.PI / 3f + Mathf.PI / 2f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        m = Mathf.Min(m, Seg(p, -dir * 0.9f, dir * 0.9f, 0.07f));
                        for (int s2 = -1; s2 <= 1; s2 += 2)
                        {
                            var basePt = dir * 0.55f * s2;
                            var perp = new Vector2(-dir.y, dir.x);
                            m = Mathf.Min(m, Seg(p, basePt, basePt + (dir * s2 * 0.25f + perp * 0.25f), 0.05f));
                            m = Mathf.Min(m, Seg(p, basePt, basePt + (dir * s2 * 0.25f - perp * 0.25f), 0.05f));
                        }
                    }
                    return m;
                },
                Glyph.Flame => p => Union((p - new Vector2(0f, -0.3f)).magnitude - 0.5f,
                                          Triangle(p, new Vector2(0.05f, 0.95f), new Vector2(-0.48f, -0.15f), new Vector2(0.48f, -0.15f)),
                                          Triangle(p, new Vector2(-0.55f, 0.35f), new Vector2(-0.5f, -0.3f), new Vector2(-0.15f, -0.2f))),
                Glyph.Bell => p => Union(Mathf.Max(Ellipse(p - new Vector2(0f, -0.05f), 0.55f, 0.75f), -p.y - 0.45f),
                                         Box(p - new Vector2(0f, -0.5f), new Vector2(0.75f, 0.08f)),
                                         (p - new Vector2(0f, -0.72f)).magnitude - 0.15f,
                                         Mathf.Abs((p - new Vector2(0f, 0.78f)).magnitude - 0.13f) - 0.05f),
                Glyph.Needle => p => Union(Diamond(p, 0.2f, 0.85f),
                                           Seg(p, new Vector2(0.75f, 0f), new Vector2(0.95f, 0f), 0.05f),
                                           Seg(p, new Vector2(-0.75f, 0f), new Vector2(-0.95f, 0f), 0.05f),
                                           p.magnitude - 0.08f),
                Glyph.Bar => p => Box(p, new Vector2(0.72f, 0.18f)) - 0.04f,
                Glyph.Pins => p =>
                {
                    float m = 9f;
                    for (int row = 0; row < 4; row++)
                        for (int c = 0; c <= row; c++)
                            m = Mathf.Min(m, (p - new Vector2((c - row * 0.5f) * 0.44f, 0.65f - row * 0.42f)).magnitude - 0.11f);
                    return m;
                },
                Glyph.Clock => p => Union(Mathf.Abs(p.magnitude - 0.82f) - 0.07f,
                                          Seg(p, Vector2.zero, new Vector2(0f, 0.6f), 0.07f),
                                          Seg(p, Vector2.zero, new Vector2(0.42f, -0.12f), 0.07f),
                                          p.magnitude - 0.11f),
                Glyph.Twins => p => Union(Mathf.Abs((p - new Vector2(-0.3f, 0f)).magnitude - 0.48f) - 0.09f,
                                          Mathf.Abs((p - new Vector2(0.3f, 0f)).magnitude - 0.48f) - 0.09f),
                _ => p => Union(Mathf.Abs(p.magnitude - 0.7f) - 0.1f, p.magnitude - 0.18f),
            };
            s = Make(f, 128);
            glyphs[g] = s;
            return s;
        }

        static Sprite Make(Sdf f, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };
            var px = new Color[size * size];
            float pix = 2f / size;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - f(p) / (pix * 1.2f)));
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // ---------- Primitives ----------
        static float Union(params float[] d) { float m = d[0]; for (int i = 1; i < d.Length; i++) m = Mathf.Min(m, d[i]); return m; }
        static float Sub(float a, float b) => Mathf.Max(a, -b);

        static float Box(Vector2 p, Vector2 b)
        {
            var d = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        /// <summary>Ellipse approchée (assez juste pour un bord anti-aliasé).</summary>
        static float Ellipse(Vector2 p, float a, float b)
        {
            float k = new Vector2(p.x / a, p.y / b).magnitude;
            return (k - 1f) * Mathf.Min(a, b);
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Distance signée à un triangle (Inigo Quilez)
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(new Vector2(pq0.sqrMagnitude, s * (v0.x * e0.y - v0.y * e0.x)),
                                                new Vector2(pq1.sqrMagnitude, s * (v1.x * e1.y - v1.y * e1.x))),
                                    new Vector2(pq2.sqrMagnitude, s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        /// <summary>Polygone régulier à n côtés, rayon r (sommets), tourné de rot.</summary>
        static float Polygon(Vector2 p, int n, float r, float rot)
        {
            float a = Mathf.Atan2(p.y, p.x) - rot;
            float seg = Mathf.PI * 2f / n;
            float local = Mathf.Repeat(a + seg * 0.5f, seg) - seg * 0.5f;
            float apothem = r * Mathf.Cos(seg * 0.5f);
            return p.magnitude * Mathf.Cos(local) - apothem;
        }

        static float Star(Vector2 p, float outer, float inner)
        {
            // Étoile à 5 branches : on mesure la distance au contour polygonal entre sommets externes et internes.
            float a = Mathf.Atan2(p.x, p.y);
            float seg = Mathf.PI * 2f / 5f;
            float local = Mathf.Repeat(a, seg) / seg;           // 0..1 entre deux branches
            float t = Mathf.Abs(local - 0.5f) * 2f;             // 1 sur une branche, 0 au creux
            float edge = Mathf.Lerp(inner, outer, t);
            return (p.magnitude - edge) * 0.7f;
        }

        static float Star4(Vector2 p, float outer, float inner)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            return Mathf.Min(Diamond(q, inner, outer), Diamond(q, outer, inner));
        }

        static float Diamond(Vector2 p, float a, float b) => (Mathf.Abs(p.x) / a + Mathf.Abs(p.y) / b - 1f) * Mathf.Min(a, b) * 0.9f;

        static float Gem(Vector2 p) => Union(Diamond(p, 0.3f, 0.32f), Box(p - new Vector2(0f, 0.12f), new Vector2(0.3f, 0.06f)));

        static float HeartSdf(Vector2 p)
        {
            var q = new Vector2(Mathf.Abs(p.x), p.y);
            float lobes = (q - new Vector2(0.38f, 0.3f)).magnitude - 0.5f;
            float tip = Triangle(new Vector2(q.x, p.y), new Vector2(0f, -0.92f), new Vector2(0.84f, 0.15f), new Vector2(0f, 0.3f));
            return Mathf.Min(lobes, tip);
        }

        // ================================================================= Bille (boutique)

        static Sprite ballShade, ballSpec;

        /// <summary>Sphère ombrée en niveaux de gris (à teinter à la couleur de la bille).</summary>
        public static Sprite BallShade
        {
            get
            {
                if (ballShade != null) return ballShade;
                ballShade = MakeColor(160, (u, v) =>
                {
                    float r2 = u * u + v * v;
                    if (r2 > 1f) return new Color(1, 1, 1, 0);
                    float z = Mathf.Sqrt(1f - r2);
                    var n = new Vector3(u, v, z);
                    float lambert = Mathf.Clamp01(Vector3.Dot(n, new Vector3(-0.45f, 0.55f, 0.7f).normalized));
                    float rim = Mathf.Pow(1f - z, 3f) * 0.35f;
                    float l = 0.22f + 0.85f * lambert + rim;
                    float edge = Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 80f);
                    return new Color(l, l, l, edge);
                });
                return ballShade;
            }
        }

        /// <summary>Reflet spéculaire blanc (non teinté) posé sur la bille.</summary>
        public static Sprite BallSpec
        {
            get
            {
                if (ballSpec != null) return ballSpec;
                ballSpec = MakeColor(160, (u, v) =>
                {
                    float r2 = u * u + v * v;
                    if (r2 > 1f) return new Color(1, 1, 1, 0);
                    float z = Mathf.Sqrt(1f - r2);
                    var n = new Vector3(u, v, z);
                    var h = new Vector3(-0.35f, 0.5f, 1f).normalized;
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(n, h)), 60f);
                    // reflet de la salle en bas (bande claire)
                    float band = Mathf.Clamp01(1f - Mathf.Abs(v + 0.62f) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(u) * 1.6f) * 0.25f;
                    return new Color(1, 1, 1, Mathf.Clamp01(spec + band));
                });
                return ballSpec;
            }
        }

        static Sprite MakeColor(int size, System.Func<float, float, Color> f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = f((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
            tex.SetPixels(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Bille dessinée : sphère ombrée à la couleur de la bille, reflet, liseré d'édition.</summary>
        public static RectTransform BallIcon(Transform parent, BallInstance b, float size)
        {
            var info = Catalog.Ball(b.kind);
            var rt = UIKit.Rect(parent, "Ball_" + b.kind);
            rt.sizeDelta = new Vector2(size, size);
            float s = size * Mathf.Clamp(info.radiusMul, 0.75f, 1.3f) * 0.8f;
            var color = Color.Lerp(new Color(0.85f, 0.86f, 0.9f), info.color, b.kind == BallKind.Standard ? 0f : 0.75f);
            // ombre portée au sol
            var shadow = Center(rt, Sprites.Glow, new Color(0, 0, 0, 0.55f), new Vector2(s * 1.1f, s * 0.35f), new Vector2(0f, -s * 0.5f));
            if (b.edition != Edition.None)
                Center(rt, Sprites.Glow, Catalog.EditionColor(b.edition).WithAlpha(0.7f), Vector2.one * s * 1.6f, Vector2.zero);
            Center(rt, BallShade, color, Vector2.one * s, Vector2.zero);
            Center(rt, BallSpec, Color.white, Vector2.one * s, Vector2.zero);
            if (b.edition != Edition.None)
                Center(rt, Sprites.Ring, Catalog.EditionColor(b.edition), Vector2.one * s * 1.18f, Vector2.zero);
            shadow.transform.SetAsFirstSibling();
            return rt;
        }

        static Image Center(RectTransform parent, Sprite sp, Color c, Vector2 size, Vector2 pos)
        {
            var img = UIKit.Rect(parent, "Img").gameObject.AddComponent<Image>();
            img.sprite = sp; img.color = c; img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size; r.anchoredPosition = pos;
            return img;
        }
    }
}
