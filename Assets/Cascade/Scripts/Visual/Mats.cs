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
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit) { name = key };
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (emission.maxColorComponent > 0f) SetEmission(m, emission);
            cache[key] = m;
            return m;
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

        /// <summary>Verre transparent (Standard en mode Transparent, ou URP Lit en surface transparente).</summary>
        public static Material Glass
        {
            get
            {
                if (cache.TryGetValue("Glass", out var g) && g != null) return g;
                var m = new Material(Lit) { name = "Glass" };
                var c = new Color(0.75f, 0.88f, 1f, 0.16f);
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.95f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.95f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.1f);
                if (IsURP)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }
                else
                {
                    m.SetFloat("_Mode", 3f);
                    m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                }
                m.SetInt("_SrcBlend", IsURP ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
                cache["Glass"] = m;
                return m;
            }
        }

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
            if (immediate) Object.DestroyImmediate(tmp); else Object.Destroy(tmp);
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
