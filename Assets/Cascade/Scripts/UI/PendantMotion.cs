// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Anime un pendentif de relique : balancement amorti autour de la bélière, petit roulis au repos,
    /// arrivée qui tombe et rebondit avec des étincelles, flash et secousse quand la relique se déclenche.
    /// Créé par RelicArt.Build (interface construite en jeu, jamais enregistrée dans la scène).
    /// </summary>
    public class PendantMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform swing;
        Image halo;
        bool legendary;
        Color haloColor;

        float angle, velocity, phase;
        float flash, hover, scale = 1f;
        float appear = 1f;          // 0 → 1 pendant l'arrivée
        bool appearing;

        class Spark { public Image img; public Vector2 v; public float life, max; }
        readonly List<Spark> sparks = new List<Spark>();

        public void Init(RectTransform swingRoot, Image haloImage, bool isLegendary, Color glow)
        {
            swing = swingRoot;
            halo = haloImage;
            legendary = isLegendary;
            haloColor = glow;
            phase = Random.value * 10f;
        }

        /// <summary>La relique vient d'être obtenue : elle tombe, rebondit, se balance et lance des étincelles.</summary>
        public void Appear()
        {
            appearing = true;
            appear = 0f;
            velocity = Random.value < 0.5f ? -260f : 260f;
            flash = 1f;
            for (int i = 0; i < 14; i++) SpawnSpark();
        }

        /// <summary>La relique se déclenche : flash du halo et secousse.</summary>
        public void Fire()
        {
            flash = 1f;
            velocity += (Random.value < 0.5f ? -1f : 1f) * 140f;
            scale = 1.18f;
        }

        public void OnPointerEnter(PointerEventData e) { hover = 1f; velocity += 70f; }
        public void OnPointerExit(PointerEventData e) => hover = 0f;

        void SpawnSpark()
        {
            var img = UIKit.Rect(swing, "Spark").gameObject.AddComponent<Image>();
            img.sprite = Random.value < 0.5f ? Sprites.Star : Sprites.Glow;
            img.raycastTarget = false;
            img.color = Color.Lerp(Color.white, haloColor, Random.value * 0.6f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            float size = swing.sizeDelta.x;
            rt.sizeDelta = Vector2.one * Random.Range(0.12f, 0.26f) * size;
            rt.anchoredPosition = new Vector2(0f, -size * 0.8f);
            float a = Random.Range(0f, Mathf.PI * 2f);
            sparks.Add(new Spark { img = img, v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.2f, 3f) * size, life = 0f, max = Random.Range(0.4f, 0.8f) });
        }

        void Update()
        {
            if (swing == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            phase += dt;

            // Ressort amorti + léger roulis au repos (comme suspendu à un fil)
            float idle = Mathf.Sin(phase * 1.3f) * 2.2f + Mathf.Sin(phase * 0.7f + 1f) * 1.2f;
            float acc = -60f * (angle - idle) - 4.5f * velocity;
            velocity += acc * dt;
            angle += velocity * dt;

            // Arrivée : chute depuis le haut avec rebond élastique
            float drop = 0f, appearScale = 1f;
            if (appearing)
            {
                appear = Mathf.Min(1f, appear + dt * 2.2f);
                float t = appear;
                float bounce = 1f - Mathf.Pow(1f - t, 3f) * Mathf.Cos(t * Mathf.PI * 3.5f);
                drop = (1f - bounce) * 90f;
                appearScale = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t * 2.5f));
                if (appear >= 1f) appearing = false;
            }

            scale = Mathf.Lerp(scale, 1f + hover * 0.1f, 1f - Mathf.Exp(-dt * 12f));
            swing.localRotation = Quaternion.Euler(0f, 0f, angle);
            swing.anchoredPosition = new Vector2(0f, drop);
            swing.localScale = Vector3.one * scale * appearScale;

            flash = Mathf.MoveTowards(flash, 0f, dt * 2f);
            if (halo != null)
            {
                float baseGlow = legendary ? 0.25f + 0.15f * Mathf.Sin(phase * 2.5f) : 0f;
                halo.color = haloColor.WithAlpha(Mathf.Clamp01(baseGlow + flash * 0.85f + hover * 0.15f));
                halo.rectTransform.localScale = Vector3.one * (1f + flash * 0.35f);
            }

            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                s.life += dt;
                if (s.life >= s.max || s.img == null)
                {
                    if (s.img != null) Destroy(s.img.gameObject);
                    sparks.RemoveAt(i);
                    continue;
                }
                s.v *= 1f - dt * 3f;
                s.v.y -= dt * 120f;
                s.img.rectTransform.anchoredPosition += s.v * dt;
                s.img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, s.life * 400f);
                float k = 1f - s.life / s.max;
                var c = s.img.color; c.a = k; s.img.color = c;
            }
        }
    }
}
