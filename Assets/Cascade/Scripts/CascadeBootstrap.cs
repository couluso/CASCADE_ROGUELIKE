// CASCADE — Auteur : Coulu
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cascade
{
    /// <summary>
    /// Point d'entrée : pose ce composant sur un GameObject vide d'une scène vide, puis Play.
    /// Tout le reste (borne 3D, salle, lumières, UI, sons, filtre VHS) est créé par code.
    /// </summary>
    public class CascadeBootstrap : MonoBehaviour
    {
        [Tooltip("0 = graine aléatoire. Mets une valeur fixe pour rejouer la même run (boutiques, boss, tirages).")]
        public int fixedSeed = 0;

        void Awake()
        {
            Application.targetFrameRate = 120;
            Physics2D.queriesHitTriggers = false;
            Physics2D.gravity = new Vector2(0f, -9.81f);

            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = cgo.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null && FindAnyAudioListener() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.allowHDR = true;
            cam.allowMSAA = true;

            if (EventSystem.current == null && FindEventSystem() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            // Décor 3D (borne, salle, lumières) + caméra en perspective
            var arcade = new GameObject("Arcade").AddComponent<Arcade>();
            arcade.Init(cam);

            var root = new GameObject("Cascade");
            var board = new GameObject("Board").AddComponent<Board>();
            var launcher = new GameObject("Launcher").AddComponent<Launcher>();
            var juice = root.AddComponent<Juice>();
            var ui = root.AddComponent<GameUI>();
            var gm = root.AddComponent<GameManager>();

            gm.Init(board, launcher, juice, ui);
            ui.Init(gm);
            juice.Init(cam, ui.WorldCanvas);
            launcher.Init(gm);

            // Filtre VHS (la caméra rend dans une texture, affichée derrière l'interface)
            var vhs = root.AddComponent<VhsFeed>();
            vhs.Init(cam, ui.RootRect);
            juice.Vhs = vhs;

            // Sac de billes en 3D (bocal en verre)
            root.AddComponent<BagView>().Init(gm);
            if (fixedSeed != 0) gm.PendingSeed = fixedSeed;
        }

        static AudioListener FindAnyAudioListener()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<AudioListener>();
#else
            return Object.FindObjectOfType<AudioListener>();
#endif
        }

        static EventSystem FindEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<EventSystem>();
#else
            return Object.FindObjectOfType<EventSystem>();
#endif
        }
    }
}
