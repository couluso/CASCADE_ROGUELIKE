// CASCADE — Auteur : Coulu
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Filtre « moniteur d'arcade » (base VHS allégée) indépendant du pipeline : la caméra du jeu rend dans une RenderTexture,
    /// affichée plein écran derrière l'interface avec le shader Cascade/VHS.
    /// L'interface reste nette par-dessus. [V] active / désactive le filtre.
    /// </summary>
    public class VhsFeed : MonoBehaviour
    {
        Camera world;
        RawImage image;
        Material mat;
        RenderTexture rt;
        int w, h;
        float glitch;
        bool enabledVhs = true;

        public void Init(Camera worldCamera, RectTransform canvasRoot)
        {
            world = worldCamera;

            // Caméra « écran » qui ne dessine rien : évite le message « No cameras rendering » et nettoie l'écran.
            var sc = new GameObject("ScreenCamera").AddComponent<Camera>();
            sc.cullingMask = 0;
            sc.clearFlags = CameraClearFlags.SolidColor;
            sc.backgroundColor = Color.black;
            sc.depth = world.depth + 10;
            sc.orthographic = true;

            var rtTransform = UIKit.Rect(canvasRoot, "VHS Feed");
            rtTransform.SetAsFirstSibling();
            UIKit.Fill(rtTransform);
            image = rtTransform.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;

            var shader = Resources.Load<Shader>("Shaders/CascadeCRT");
            if (shader == null) shader = Shader.Find("Cascade/ArcadeCRT");
            if (shader != null) mat = new Material(shader);
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
                image.material = enabledVhs ? mat : null;
            }
            glitch = Mathf.MoveTowards(glitch, 0f, Time.unscaledDeltaTime * 1.5f);
            if (mat != null)
            {
                mat.SetFloat("_VhsTime", Time.unscaledTime);
                mat.SetFloat("_Glitch", glitch);
            }
        }

        void OnDestroy()
        {
            if (world != null) world.targetTexture = null;
            if (rt != null) rt.Release();
        }
    }
}
