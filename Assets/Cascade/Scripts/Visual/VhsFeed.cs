// CASCADE — Auteur : Coulu
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Filtre « moniteur d'arcade » (base VHS allégée) indépendant du pipeline : la caméra du jeu rend dans une RenderTexture,
    /// affichée plein écran derrière l'interface avec le shader Cascade/VHS.
    /// L'interface reste nette par-dessus. [V] active / désactive le filtre.
    /// La caméra « écran » et l'image plein écran sont dans la scène, désactivées hors du mode Play
    /// (sinon la vue Game de l'éditeur serait noire) : elles s'activent au lancement.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class VhsFeed : MonoBehaviour
    {
        [Tooltip("Caméra du jeu, rendue dans une texture.")]
        [SerializeField] Camera world;
        [Tooltip("Caméra qui ne dessine rien : nettoie l'écran derrière l'interface.")]
        [SerializeField] Camera screenCamera;
        [Tooltip("Image plein écran (premier enfant du canvas d'interface) qui affiche la texture filtrée.")]
        [SerializeField] RawImage image;
        [Tooltip("Force du filtre : 0 = image nette, 1 = valeurs du shader. (Futur réglage des options.)")]
        [SerializeField, Range(0f, 1f)] float intensity = 0.45f;
        /// <summary>Force du filtre (0 à 1), pour le futur menu d'options.</summary>
        public float Intensity { get => intensity; set { intensity = Mathf.Clamp01(value); GameSettings.SetVhsIntensity(intensity); } }

        // Réglages dosés par l'intensité : nom, valeur neutre (sans effet). La valeur pleine est celle du shader.
        static readonly (string name, float neutral)[] Scaled =
        {
            ("_Curvature", 0f), ("_Glow", 0f), ("_Smearing", 0f), ("_ColorOffset", 0f), ("_Sharpening", 0f),
            ("_Saturation", 1f), ("_Contrast", 1f), ("_StaticNoise", 0f), ("_ScanlineOpacity", 0f),
            ("_Mask", 0f), ("_Vignette", 0f), ("_VerticalJitter", 0f),
        };
        float[] fullValues;
        float appliedIntensity = -1f;
        Material mat;
        RenderTexture rt;
        int w, h;
        float glitch;
        bool enabledVhs = true;

        /// <summary>Crée la caméra écran et l'image plein écran (désactivées) et relie le tout.</summary>
        public void Build(Camera worldCamera, RectTransform canvasRoot)
        {
            world = worldCamera;

            // Caméra « écran » qui ne dessine rien : évite le message « No cameras rendering » et nettoie l'écran.
            screenCamera = new GameObject("ScreenCamera").AddComponent<Camera>();
            screenCamera.transform.SetParent(transform, false);
            screenCamera.cullingMask = 0;
            screenCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCamera.backgroundColor = Color.black;
            screenCamera.depth = world.depth + 10;
            screenCamera.orthographic = true;
            screenCamera.gameObject.SetActive(false);

            var rtTransform = UIKit.Rect(canvasRoot, "VHS Feed");
            rtTransform.SetAsFirstSibling();
            UIKit.Fill(rtTransform);
            image = rtTransform.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
        }

        void Awake()
        {
            // Réglages enregistrés du joueur (sinon valeurs de l'inspecteur)
            if (GameSettings.TryGetVhsIntensity(out var saved)) intensity = saved;
            enabledVhs = GameSettings.VhsEnabled;
            if (world == null) world = Arcade.Instance != null ? Arcade.Instance.Cam : Camera.main;
            if (screenCamera == null || image == null)
            {
                var canvas = GameUI.Instance != null ? (RectTransform)GameUI.Instance.transform : null;
                if (canvas == null) { Debug.LogError("[CASCADE] VhsFeed : aucun canvas d'interface dans la scène.", this); enabled = false; return; }
                Build(world, canvas);
            }
            screenCamera.gameObject.SetActive(true);
            image.gameObject.SetActive(true);

            var shader = Resources.Load<Shader>("Shaders/CascadeCRT");
            if (shader == null) shader = Shader.Find("Cascade/ArcadeCRT");
            if (shader != null)
            {
                mat = new Material(shader);
                fullValues = new float[Scaled.Length];
                for (int i = 0; i < Scaled.Length; i++) fullValues[i] = mat.GetFloat(Scaled[i].name);
            }
            else Debug.LogWarning("[CASCADE] Shader introuvable (Assets/Cascade/Resources/Shaders/CascadeCRT.shader).");
            image.material = mat;
            Resize();
        }

        void Resize()
        {
            w = Mathf.Max(64, Screen.width);
            h = Mathf.Max(64, Screen.height);
            if (rt != null) { world.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "VHS_RT" };
            rt.Create();
            world.targetTexture = rt;
            image.texture = rt;
        }

        /// <summary>Petit glitch de bande (gros scores, TILT...).</summary>
        public void Glitch(float amount) => glitch = Mathf.Max(glitch, Mathf.Clamp01(amount));

        void Update()
        {
            if (Screen.width != w || Screen.height != h) Resize();
            if (InputShim.ToggleVhs)
            {
                enabledVhs = !enabledVhs;
                GameSettings.VhsEnabled = enabledVhs;
                image.material = enabledVhs ? mat : null;
            }
            glitch = Mathf.MoveTowards(glitch, 0f, Time.unscaledDeltaTime * 1.5f);
            if (mat != null)
            {
                if (!Mathf.Approximately(appliedIntensity, intensity)) ApplyIntensity();
                mat.SetFloat("_VhsTime", Time.unscaledTime);
                mat.SetFloat("_Glitch", glitch * Mathf.Lerp(0.4f, 1f, intensity));
            }
        }

        void ApplyIntensity()
        {
            appliedIntensity = intensity;
            // La lueur garde un peu de présence même au minimum : c'est elle qui fait briller néons et ampoules.
            for (int i = 0; i < Scaled.Length; i++)
            {
                float k = Scaled[i].name == "_Glow" ? Mathf.Lerp(0.5f, 1f, intensity) : intensity;
                mat.SetFloat(Scaled[i].name, Mathf.Lerp(Scaled[i].neutral, fullValues[i], k));
            }
        }

        void OnDestroy()
        {
            if (world != null) world.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); }
            if (mat != null) Destroy(mat);
        }
    }
}
