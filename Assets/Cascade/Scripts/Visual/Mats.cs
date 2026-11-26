// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cascade
{
    /// <summary>
    /// Matériaux 3D compatibles Built-in (Standard) et URP (Lit), créés par code et mis en cache.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit;
        static MaterialPropertyBlock mpb;

        public static bool IsURP => GraphicsSettings.currentRenderPipeline != null;

        static Shader Lit
        {
            get
            {
                if (lit != null) return lit;
                if (IsURP) lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                return lit;
            }
        }

        /// <summary>Matériau éclairé. metal/smooth entre 0 et 1, émission optionnelle (néons, ampoules).</summary>
        public static Material Get(string key, Color color, float metal, float smooth, Color emission = default)
        {
            return Cached(key, () =>
            {
                var m = new Material(Lit) { name = key };
                m.color = color;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
                if (emission.maxColorComponent > 0f) SetEmission(m, emission);
                return m;
            });
        }

        /// <summary>Dossier des matériaux enregistrés quand la scène est construite dans l'éditeur.</summary>
        public const string AssetFolder = "Assets/Cascade/Resources/Materials";

        /// <summary>
        /// Matériau partagé : d'abord l'asset (Resources/Materials/key), sinon créé par code.
        /// Hors mode Play (construction de la scène), le matériau créé est enregistré comme asset :
        /// la scène peut alors le référencer et on peut le retoucher dans l'inspecteur.
        /// </summary>
        public static Material Cached(string key, System.Func<Material> create)
        {
            if (cache.TryGetValue(key, out var m) && m != null && IsUsable(m)) return m;
            m = Resources.Load<Material>("Materials/" + key);
            if (m == null)
            {
                m = create();
                m.name = key;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (!UnityEditor.AssetDatabase.IsValidFolder(AssetFolder))
                        UnityEditor.AssetDatabase.CreateFolder("Assets/Cascade/Resources", "Materials");
                    UnityEditor.AssetDatabase.CreateAsset(m, $"{AssetFolder}/{key}.mat");
                }
#endif
            }
            cache[key] = m;
            return m;
        }

        /// <summary>
        /// Hors mode Play, seul un asset peut être référencé par la scène : un matériau créé pendant une partie
        /// (resté en cache, le domaine n'étant pas rechargé) serait perdu à l'enregistrement.
        /// </summary>
        static bool IsUsable(Material m)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return UnityEditor.EditorUtility.IsPersistent(m);
#endif
            return true;
        }

        public static void SetEmission(Material m, Color emission)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission);
        }

        /// <summary>Change couleur / émission d'un seul objet sans dupliquer le matériau.</summary>
        public static void Tint(Renderer r, Color color, Color emission)
        {
            mpb ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_EmissionColor", emission);
            r.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Matériau translucide (alpha de la couleur), URP Lit en surface transparente ou Standard en mode Fade.
        /// doubleSided : on voit aussi la face intérieure (tissu, verre), en URP seulement.
        /// </summary>
        public static Material Transparent(string key, Color color, float smooth, bool doubleSided = false, Color emission = default)
        {
            return Cached(key, () =>
            {
                var m = new Material(Lit) { name = key };
                m.color = color;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                if (IsURP)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    if (doubleSided && m.HasProperty("_Cull")) { m.SetFloat("_Cull", (float)CullMode.Off); m.doubleSidedGI = true; }
                }
                else
                {
                    m.SetFloat("_Mode", 2f);
                    m.EnableKeyword("_ALPHABLEND_ON");
                }
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
                if (emission.maxColorComponent > 0f) SetEmission(m, emission);
                return m;
            });
        }

        // ---------- Bocal du sac : verre gris fumé, bien visible mais on voit les billes à travers ----------
        public static Material JarGlass => Transparent("JarGlass", new Color(0.55f, 0.58f, 0.62f, 0.38f), 0.92f, true, new Color(0.03f, 0.035f, 0.04f));
        /// <summary>Fond et lèvre du bocal : verre épais, plus dense.</summary>
        public static Material JarGlassThick => Transparent("JarGlassThick", new Color(0.5f, 0.55f, 0.6f, 0.7f), 0.95f, false, new Color(0.04f, 0.05f, 0.055f));
        /// <summary>Reflets verticaux sur la paroi (bandes claires).</summary>
        public static Material GlassHighlight => Transparent("GlassHighlight", new Color(1f, 1f, 1f, 0.55f), 1f, false, new Color(0.35f, 0.37f, 0.4f));

        // ---------- Matériaux de la borne ----------
        public static Material Chrome => Get("Chrome", new Color(0.82f, 0.84f, 0.88f), 0.9f, 0.82f);
        public static Material Brass => Get("Brass", new Color(0.78f, 0.6f, 0.28f), 0.85f, 0.65f);
        public static Material Felt => Get("Felt", new Color(0.05f, 0.2f, 0.14f), 0f, 0.15f);
        public static Material Lacquer => Get("Lacquer", new Color(0.32f, 0.07f, 0.06f), 0.1f, 0.78f);
        public static Material DarkWood => Get("DarkWood", new Color(0.12f, 0.06f, 0.04f), 0f, 0.45f);
        public static Material Floor => Get("Floor", new Color(0.07f, 0.06f, 0.07f), 0f, 0.55f);
        public static Material Wall => Get("Wall", new Color(0.09f, 0.07f, 0.08f), 0f, 0.2f);
        public static Material Hole => Get("Hole", new Color(0.01f, 0.01f, 0.01f), 0f, 0.1f);
        public static Material BulbOn => Get("BulbOn", new Color(1f, 0.85f, 0.55f), 0f, 0.9f, new Color(2.2f, 1.6f, 0.7f));
        public static Material BulbOff => Get("BulbOff", new Color(0.35f, 0.28f, 0.2f), 0f, 0.9f, new Color(0.08f, 0.05f, 0.02f));
        public static Material Cap => Get("Cap", Color.white, 0.2f, 0.7f, new Color(0.01f, 0.01f, 0.01f));
        public static Material Glow => Get("Glow", Color.white, 0f, 0.5f, new Color(0.01f, 0.01f, 0.01f));

        static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>À appeler une fois au démarrage (hors callback physique) : met en cache les maillages des primitives.</summary>
        public static void Warmup()
        {
            foreach (PrimitiveType t in System.Enum.GetValues(typeof(PrimitiveType)))
            {
                if ((int)t > (int)PrimitiveType.Quad) continue;
                MeshOf(t, immediate: true);
            }
        }

        static Mesh MeshOf(PrimitiveType t, bool immediate = false)
        {
            if (meshes.TryGetValue(t, out var m) && m != null) return m;
            var tmp = GameObject.CreatePrimitive(t);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (immediate || !Application.isPlaying) Object.DestroyImmediate(tmp); else Object.Destroy(tmp);
            meshes[t] = m;
            return m;
        }

        /// <summary>
        /// Objet 3D purement visuel (aucun collider 3D : la physique est en 2D).
        /// N'utilise pas CreatePrimitive : utilisable même pendant une collision (ex. bille jumelle).
        /// </summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name = null)
        {
            var go = new GameObject(name ?? type.ToString());
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(type);
            go.AddComponent<MeshRenderer>();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return go;
        }

        /// <summary>Cylindre couché le long de l'axe Z (pics, ampoules plates).</summary>
        public static GameObject CylinderZ(Transform parent, Vector3 localPos, float radius, float length, Material mat, string name = null)
        {
            var go = Prim(PrimitiveType.Cylinder, parent, localPos, new Vector3(radius * 2f, length * 0.5f, radius * 2f), mat, name);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return go;
        }
    }
}
