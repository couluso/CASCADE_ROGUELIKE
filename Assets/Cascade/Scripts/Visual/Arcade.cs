// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Décor 3D : une vraie borne de plinko dans une salle d'arcade sombre.
    /// Planche en feutre vert, pics chromés, rails en laiton, cadre laqué, fronton à ampoules,
    /// bornes voisines, enseignes au mur, éclairage avec ombres et sonde de reflets pour le chrome.
    /// Gère aussi la caméra en perspective et son cadrage selon la taille de la fenêtre.
    /// </summary>
    public class Arcade : MonoBehaviour
    {
        public static Arcade Instance { get; private set; }
        public Camera Cam { get; private set; }
        public Transform Rig { get; private set; }

        // Zone à cadrer : la borne + les deux tours d'affichage
        static readonly Vector3 FrameCenter = new Vector3(0f, 5.3f, 0f);
        const float FrameHalfW = 13.6f, FrameHalfH = 7.7f;
        // FOV plus large + caméra plus proche et légèrement plongeante : la profondeur des pics se voit.
        const float Fov = 50f, Tilt = 7f;
        const float TowerX = 10.4f;

        /// <summary>Écrans physiques (canvas en espace monde) posés sur la borne.</summary>
        public RectTransform LeftDisplay { get; private set; }
        public RectTransform RightDisplay { get; private set; }
        public RectTransform OverlayDisplay { get; private set; }
        readonly List<Renderer> blinkers = new List<Renderer>();

        readonly List<Renderer> bulbs = new List<Renderer>();
        float bulbTimer;
        int bulbStep;
        float frenzy;
        ReflectionProbe probe;
        int probeFrames;

        public void Init(Camera cam)
        {
            Instance = this;
            Cam = cam;
            Mats.Warmup();
            Rig = new GameObject("CameraRig").transform;
            cam.transform.SetParent(Rig, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.orthographic = false;
            cam.fieldOfView = Fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.015f, 0.025f);
            Fit(16f / 9f, 1f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.09f, 0.075f, 0.1f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.035f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 60f;

            BuildCabinet();
            BuildTowers();
            BuildRoom();
            BuildLights();
        }

        /// <summary>Cadre la borne dans la partie centrale de l'écran (viewportFraction = part de largeur libre entre les panneaux).</summary>
        public void Fit(float aspect, float viewportFraction)
        {
            float tanHalf = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            float dH = FrameHalfH / tanHalf;
            float dW = FrameHalfW / (tanHalf * aspect * Mathf.Clamp(viewportFraction, 0.2f, 1f));
            float d = Mathf.Max(dH, dW) * 1.03f;
            var rot = Quaternion.Euler(Tilt, 0f, 0f);
            Rig.rotation = rot;
            Rig.position = FrameCenter - rot * Vector3.forward * d;
        }

        /// <summary>Point de la planche (z = 0) sous la souris.</summary>
        public Vector2 MouseOnBoard()
        {
            var ray = Cam.ScreenPointToRay(new Vector3(InputShim.MousePosition.x, InputShim.MousePosition.y, 0f));
            if (Mathf.Abs(ray.direction.z) < 1e-4f) return Vector2.zero;
            float t = -ray.origin.z / ray.direction.z;
            var p = ray.origin + ray.direction * t;
            return new Vector2(p.x, p.y);
        }

        public void Frenzy(float seconds) => frenzy = Mathf.Max(frenzy, seconds);

        // ------------------------------------------------------------------ Borne

        void BuildCabinet()
        {
            var root = new GameObject("Cabinet").transform;
            float wx = BoardLayout.WallX;

            // Planche (feutre) et liseré doré imprimé
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 5.0f, 0.47f), new Vector3(wx * 2f + 0.4f, 11.4f, 0.1f), Mats.Felt, "Backboard");
            var gold = Mats.Get("PrintGold", new Color(0.55f, 0.42f, 0.18f), 0.6f, 0.5f);
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 10.3f, 0.41f), new Vector3(wx * 2f - 0.4f, 0.03f, 0.01f), gold);
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.75f, 0.41f), new Vector3(wx * 2f - 0.4f, 0.03f, 0.01f), gold);

            // Cadre laqué
            float fh = 13.2f, fy = 5.3f;
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(-wx - 0.75f, fy, 0f), new Vector3(1.1f, fh, 1.1f), Mats.Lacquer, "Frame_L");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(wx + 0.75f, fy, 0f), new Vector3(1.1f, fh, 1.1f), Mats.Lacquer, "Frame_R");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, -1.05f, -0.05f), new Vector3(wx * 2f + 2.6f, 1.4f, 1.2f), Mats.Lacquer, "Frame_Bottom");
            // Plaque des gains (sous les cases)
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.45f, -0.68f), new Vector3(wx * 2f, 0.62f, 0.04f), Mats.DarkWood, "PayoutPlate");
            // Rebord avant (le plateau de la borne) et pied
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, -1.85f, -1.2f), new Vector3(wx * 2f + 3.2f, 0.25f, 2.6f), Mats.DarkWood, "Ledge");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, -5f, 0.2f), new Vector3(wx * 2f + 2.4f, 6f, 2f), Mats.Lacquer, "Base");
            // Côtés en profondeur
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(-wx - 1.25f, fy, 1.4f), new Vector3(0.12f, fh, 2.8f), Mats.DarkWood);
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(wx + 1.25f, fy, 1.4f), new Vector3(0.12f, fh, 2.8f), Mats.DarkWood);

            // Fronton
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 12.5f, 0f), new Vector3(wx * 2f + 2.6f, 1.9f, 1.3f), Mats.Lacquer, "Marquee");
            var sign = Mats.Get("MarqueeSign", new Color(0.12f, 0.02f, 0.03f), 0f, 0.6f, new Color(0.35f, 0.04f, 0.06f));
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 12.5f, -0.66f), new Vector3(wx * 2f + 0.4f, 1.3f, 0.02f), sign, "MarqueeFace");

            // Ampoules qui défilent autour du fronton et du cadre
            for (float x = -wx - 0.9f; x <= wx + 0.9f; x += 0.55f)
            {
                AddBulb(root, new Vector3(x, 13.32f, -0.67f));
                AddBulb(root, new Vector3(x, 11.68f, -0.67f));
            }
            for (float y = 0.2f; y <= 11.2f; y += 0.6f)
            {
                AddBulb(root, new Vector3(-wx - 0.75f, y, -0.57f));
                AddBulb(root, new Vector3(wx + 0.75f, y, -0.57f));
            }
        }

        void AddBulb(Transform root, Vector3 pos)
        {
            var b = Mats.Prim(PrimitiveType.Sphere, root, pos, Vector3.one * 0.16f, Mats.BulbOff, "Bulb");
            var r = b.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            bulbs.Add(r);
        }

        // ------------------------------------------------------------------ Tours d'affichage (HUD physique)

        void BuildTowers()
        {
            var screenMat = Mats.Get("TowerGlass", new Color(0.02f, 0.02f, 0.03f), 0.2f, 0.92f, new Color(0.015f, 0.012f, 0.02f));
            var ledRed = Mats.Get("LedRed", new Color(1f, 0.15f, 0.1f), 0f, 0.8f, new Color(2.5f, 0.3f, 0.2f));
            var ledGreen = Mats.Get("LedGreen", new Color(0.2f, 1f, 0.3f), 0f, 0.8f, new Color(0.3f, 2.2f, 0.4f));

            for (int side = -1; side <= 1; side += 2)
            {
                // Légèrement tournées vers le joueur : effet de profondeur
                var tower = new GameObject(side < 0 ? "Tower_L" : "Tower_R").transform;
                tower.position = new Vector3(side * TowerX, 5.3f, -0.6f);
                tower.rotation = Quaternion.Euler(0f, side * 16f, 0f);

                Mats.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 0f, 0.25f), new Vector3(5.7f, 11.5f, 1.4f), Mats.Lacquer, "Housing");
                Mats.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 0f, -0.48f), new Vector3(5.35f, 11f, 0.06f), Mats.Brass, "Bezel");
                Mats.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 0f, -0.52f), new Vector3(5.1f, 10.6f, 0.03f), screenMat, "Glass");
                Mats.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 6.15f, 0.1f), new Vector3(5.9f, 0.8f, 1.6f), Mats.Lacquer, "Crown");
                Mats.Prim(PrimitiveType.Cube, tower, new Vector3(0f, -6.2f, 0.1f), new Vector3(6f, 0.7f, 1.7f), Mats.DarkWood, "Foot");

                for (float x = -2.6f; x <= 2.61f; x += 0.52f) AddBulb(tower, new Vector3(x, 6.15f, -0.72f));

                // Voyants qui clignotent dans les coins
                var l1 = Mats.Prim(PrimitiveType.Sphere, tower, new Vector3(-2.35f, -5.6f, -0.56f), Vector3.one * 0.13f, ledRed, "Led");
                var l2 = Mats.Prim(PrimitiveType.Sphere, tower, new Vector3(2.35f, -5.6f, -0.56f), Vector3.one * 0.13f, ledGreen, "Led");
                blinkers.Add(l1.GetComponent<Renderer>());
                blinkers.Add(l2.GetComponent<Renderer>());

                var glow = new GameObject("TowerLight").AddComponent<Light>();
                glow.transform.SetParent(tower, false);
                glow.transform.localPosition = new Vector3(0f, 2f, -2.2f);
                glow.type = LightType.Point;
                glow.color = side < 0 ? new Color(0.35f, 0.75f, 1f) : new Color(1f, 0.4f, 0.65f);
                glow.intensity = 1.1f;
                glow.range = 7f;

                var canvas = MakeWorldCanvas(side < 0 ? "LeftDisplay" : "RightDisplay", tower, new Vector3(0f, 0f, -0.555f), new Vector2(500f, 1040f), 0.01f, 20);
                if (side < 0) LeftDisplay = canvas; else RightDisplay = canvas;
            }

            // Écran central (choix des seuils, boutique, résultats) : une vitre devant la planche
            OverlayDisplay = MakeWorldCanvas("CenterDisplay", null, new Vector3(0f, 5.3f, -1.9f), new Vector2(824f, 1080f), 0.0116f, 60);
        }

        RectTransform MakeWorldCanvas(string name, Transform parent, Vector3 localPos, Vector2 size, float unitsPerPixel, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = Cam;
            c.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            go.AddComponent<GraphicRaycaster>();
            rt.sizeDelta = size;
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * unitsPerPixel;
            return rt;
        }

        // ------------------------------------------------------------------ Salle

        void BuildRoom()
        {
            var root = new GameObject("Room").transform;
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, -8.1f, 4f), new Vector3(80f, 0.2f, 40f), Mats.Floor, "Floor");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 6f, 9f), new Vector3(80f, 30f, 0.2f), Mats.Wall, "BackWall");

            // Bornes voisines (silhouettes avec écran allumé)
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 1; k <= 2; k++)
                {
                    float x = side * (17f + (k - 1) * 7f);
                    float z = 1.5f + k * 1.2f;
                    Mats.Prim(PrimitiveType.Cube, root, new Vector3(x, 1.5f, z), new Vector3(5.5f, 19f, 3f), Mats.Lacquer, "Neighbor");
                    var screenCol = k == 1 ? new Color(0.05f, 0.25f, 0.3f) : new Color(0.3f, 0.06f, 0.2f);
                    var scr = Mats.Get("NeighborScreen" + k, screenCol * 0.3f, 0f, 0.8f, screenCol);
                    Mats.Prim(PrimitiveType.Cube, root, new Vector3(x, 5f, z - 1.52f), new Vector3(4.2f, 6f, 0.02f), scr);
                }
            }

            // Enseignes néon au mur
            Neon(root, "JACKPOT", new Vector3(-13f, 13.5f, 8.85f), Palette.Pink, 4.5f);
            Neon(root, "OUVERT 24H", new Vector3(13.5f, 13.8f, 8.85f), Palette.Cyan, 3.2f);
        }

        void Neon(Transform root, string text, Vector3 pos, Color color, float scale)
        {
            var canvasGo = new GameObject("Neon_" + text, typeof(RectTransform));
            canvasGo.transform.SetParent(root, false);
            var c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(1200, 300);
            rt.position = pos;
            rt.localScale = Vector3.one * 0.01f * scale / 2f;
            var t = UIKit.Label(rt, text, 160, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(t.rectTransform);

            var l = new GameObject("NeonLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = pos + new Vector3(0f, 0f, -1.5f);
            l.type = LightType.Point;
            l.color = color;
            l.intensity = 2.2f;
            l.range = 10f;
        }

        // ------------------------------------------------------------------ Lumières

        void BuildLights()
        {
            var key = new GameObject("KeySpot").AddComponent<Light>();
            key.type = LightType.Spot;
            key.color = new Color(1f, 0.9f, 0.78f);
            key.intensity = 3.2f;
            key.range = 40f;
            key.spotAngle = 55f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.75f;
            key.transform.position = new Vector3(0f, 17f, -11f);
            key.transform.LookAt(new Vector3(0f, 4.5f, 0f));

            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.5f, 0.75f);
            fill.intensity = 0.25f;
            fill.transform.rotation = Quaternion.Euler(25f, -30f, 0f);

            var marquee = new GameObject("MarqueeLight").AddComponent<Light>();
            marquee.type = LightType.Point;
            marquee.color = new Color(1f, 0.7f, 0.4f);
            marquee.intensity = 1.6f;
            marquee.range = 6f;
            marquee.transform.position = new Vector3(0f, 12.5f, -2f);

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0f, 5f, -4f);
            probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(40f, 30f, 30f);
            probe.resolution = 128;
            probe.intensity = 1f;
            probeFrames = 3;
        }

        void Update()
        {
            // La sonde de reflets est rendue une fois le décor en place (le chrome reflète les néons).
            if (probeFrames > 0 && --probeFrames == 0 && probe != null) probe.RenderProbe();

            float blink = Mathf.Repeat(Time.unscaledTime, 1.2f);
            for (int i = 0; i < blinkers.Count; i++)
                if (blinkers[i] != null) blinkers[i].enabled = (i % 2 == 0) ? blink < 0.6f : blink >= 0.6f;

            frenzy = Mathf.Max(0f, frenzy - Time.unscaledDeltaTime);
            bulbTimer -= Time.unscaledDeltaTime;
            if (bulbTimer > 0f) return;
            bulbTimer = frenzy > 0f ? 0.05f : 0.14f;
            bulbStep++;
            for (int i = 0; i < bulbs.Count; i++)
            {
                bool on = frenzy > 0f ? (i + bulbStep) % 2 == 0 : (i + bulbStep) % 4 != 0;
                bulbs[i].sharedMaterial = on ? Mats.BulbOn : Mats.BulbOff;
            }
        }
    }
}
