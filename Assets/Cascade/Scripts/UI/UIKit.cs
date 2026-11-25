// CASCADE — Auteur : Coulu
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>Petits utilitaires pour construire l'interface par code (uGUI + police intégrée).</summary>
    public static class UIKit
    {
        static Font font, display, body, seg7, seg14;

        /// <summary>Afficheur 7 segments (chiffres) : DSEG7, licence OFL.</summary>
        public static Font Seg7 { get { if (seg7 == null) seg7 = Resources.Load<Font>("Fonts/DSEG7Classic-Bold"); return seg7 != null ? seg7 : Display; } }
        /// <summary>Afficheur 14 segments (chiffres + lettres K, M...) : DSEG14, licence OFL.</summary>
        public static Font Seg14 { get { if (seg14 == null) seg14 = Resources.Load<Font>("Fonts/DSEG14Classic-Bold"); return seg14 != null ? seg14 : Display; } }

        /// <summary>
        /// Afficheur LED : segments éteints en fond (« ~ » = tous les segments) + valeur allumée par-dessus.
        /// </summary>
        public static Text Led(Transform parent, float x, float y, float w, float h, int size, Color color, int digits, bool letters = true)
        {
            var f = letters ? Seg14 : Seg7;
            var ghost = Label(parent, new string('~', digits), size, color, TextAnchor.MiddleRight);
            ghost.font = f; ghost.fontSize = size;
            ghost.color = new Color(color.r, color.g, color.b, 0.09f);
            Place(ghost.rectTransform, x, y, w, h);
            var t = Label(parent, "", size, color, TextAnchor.MiddleRight);
            t.font = f; t.fontSize = size;
            Place(t.rectTransform, x, y, w, h);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(color.r, color.g, color.b, 0.35f);
            sh.effectDistance = new Vector2(0f, 0f);
            return t;
        }

        /// <summary>Police d'enseigne (titres, chiffres, boutons) : Bungee, licence OFL.</summary>
        public static Font Display
        {
            get
            {
                if (display == null) display = Resources.Load<Font>("Fonts/Bungee-Regular");
                return display != null ? display : Font;
            }
        }

        /// <summary>Police d'afficheur rétro (textes) : VT323, licence OFL.</summary>
        public static Font Body
        {
            get
            {
                if (body == null) body = Resources.Load<Font>("Fonts/VT323-Regular");
                return body != null ? body : Font;
            }
        }

        /// <summary>Échelle des textes dans le monde : police rendue 4x plus grande puis réduite = texte net.</summary>
        public const float WorldTextScale = 0.0025f;
        public const int WorldFontFactor = 4;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
#if UNITY_2022_2_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                return font;
            }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Place un RectTransform en pixels (référence 1920x1080), origine en bas à gauche.</summary>
        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w / 2f, y + h / 2f);
            return rt;
        }

        public static RectTransform Fill(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding); rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded) { img.sprite = Sprites.Rounded; img.type = Image.Type.Sliced; }
            return img;
        }

        public static Image Border(Image panel, Color color, float width = 2f)
        {
            var o = panel.gameObject.AddComponent<Outline>();
            o.useGraphicAlpha = false;   // le liseré reste visible même sur un fond transparent
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
            return panel;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            // Gras = police d'enseigne (Bungee, déjà épaisse). Normal = police d'afficheur (VT323, plus petite à taille égale).
            bool display = style == FontStyle.Bold;
            t.font = display ? Display : Body;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(display ? size * 0.82f : size * 1.3f);
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Normal;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Ombre portée dure (style enseigne imprimée) : reste nette, contrairement à un halo flou.</summary>
        public static Text Neon(Text t, Color glow)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(glow.r * 0.25f, glow.g * 0.25f, glow.b * 0.25f, 1f);
            sh.effectDistance = new Vector2(4f, -4f);
            return t;
        }

        public static Button Button(Transform parent, string text, Color color, Action onClick, int size = 26)
        {
            var img = Panel(parent, "Button", color);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            colors.colorMultiplier = 1.2f;
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                // Désélectionne : sinon Espace/Entrée (secousse, lâcher) recliqueraient ce bouton.
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                GameUI.Instance?.HideTooltip();
                onClick?.Invoke();
            });
            var label = Label(img.transform, text, size, Palette.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(label.rectTransform, 4f);
            var lsh = label.gameObject.AddComponent<Shadow>();
            lsh.effectColor = new Color(0, 0, 0, 0.9f);
            lsh.effectDistance = new Vector2(2f, -2f);
            btn.gameObject.AddComponent<ButtonJuice>();
            return btn;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static Text WorldLabel(Transform worldCanvas, Vector2 pos, string text, int size, Color color)
        {
            var rt = Rect(worldCanvas, "WorldText");
            rt.sizeDelta = new Vector2(400, 100) * WorldFontFactor;
            rt.position = new Vector3(pos.x, pos.y, 0f);
            rt.localScale = Vector3.one * WorldTextScale;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Display;
            t.fontSize = Mathf.RoundToInt(size * 0.82f) * WorldFontFactor;
            t.color = color;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.9f);
            sh.effectDistance = new Vector2(2f, -2f) * WorldFontFactor;
            return t;
        }

        /// <summary>Place un élément par rapport à un point d'ancrage du parent (0..1), en pixels de référence.</summary>
        public static RectTransform Anchored(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        /// <summary>Affiche une infobulle au survol.</summary>
        public static void Tooltip(GameObject go, Func<string> content)
        {
            var g = go.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => GameUI.Instance?.ShowTooltip(content()));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => GameUI.Instance?.HideTooltip());
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        public static VerticalLayoutGroup VList(RectTransform rt, float spacing, int padding = 0)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(Component c, float height, float width = -1f)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            if (width > 0) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>Petit rebond au survol / clic des boutons.</summary>
    public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        float target = 1f, scale = 1f;
        public void OnPointerEnter(PointerEventData e) { target = 1.06f; GameManager.Instance?.Juice.PlayClick(); }
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnPointerDown(PointerEventData e) => scale = 0.92f;
        void Update()
        {
            scale = Mathf.Lerp(scale, target, Time.unscaledDeltaTime * 14f);
            transform.localScale = Vector3.one * scale;
        }
    }
}
