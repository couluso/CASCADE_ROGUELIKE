// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Décor 3D : une vraie borne de plinko au milieu d'une salle de casino / arcade.
    /// Planche en feutre vert, pics chromés, rails en laiton, cadre laqué, fronton à ampoules ;
    /// derrière, des rangées de machines avec joueurs et passants (Crowd), enseignes néon,
    /// éclairage avec ombres et sonde de reflets pour le chrome.
    /// Gère aussi la caméra en perspective et son cadrage selon la taille de la fenêtre.
    /// Le décor est construit dans la scène par Tools > Cascade > Construire la scène (voir Build) :
    /// en jeu, il est seulement animé. S'il manque (scène vide), il est construit au lancement.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Arcade : MonoBehaviour
    {
        public static Arcade Instance { get; private set; }
        public Camera Cam => cam;
        public Transform Rig => rig;

        // Zone à cadrer : la borne + les deux tours d'affichage
        // (avec de la marge autour : on voit la salle derrière la borne)
        static readonly Vector3 FrameCenter = new Vector3(0f, 5.8f, 0f);
        const float FrameHalfW = 15.6f, FrameHalfH = 8.8f;
        // FOV plus large + caméra plus proche et légèrement plongeante : la profondeur des pics se voit.
        const float Fov = 50f, Tilt = 7f;
        const float TowerX = 10.4f;

        [SerializeField] Camera cam;
        [SerializeField] Transform rig;

        /// <summary>Écrans physiques (canvas en espace monde) posés sur la borne.</summary>
        [SerializeField] RectTransform leftDisplay, rightDisplay, overlayDisplay;
        public RectTransform LeftDisplay => leftDisplay;
        public RectTransform RightDisplay => rightDisplay;
        public RectTransform OverlayDisplay => overlayDisplay;

        [SerializeField] List<Renderer> blinkers = new List<Renderer>();
        [SerializeField] List<Renderer> bulbs = new List<Renderer>();
        [SerializeField] ReflectionProbe probe;

        float bulbTimer;
        int bulbStep;
        float frenzy;
        int probeFrames = 3;

        public bool IsBuilt => cam != null && rig != null && leftDisplay != null && rightDisplay != null && overlayDisplay != null;

        void Awake()
        {
            Instance = this;
            Mats.Warmup();
            if (!IsBuilt) Build(cam != null ? cam : Camera.main);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Construit la borne, la salle et les lumières sous cet objet, et range la caméra dans son rig.</summary>
        public void Build(Camera camera)
        {
            if (camera == null)
            {
                camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
                camera.gameObject.AddComponent<AudioListener>();
            }
            cam = camera;
            blinkers.Clear();
            bulbs.Clear();
            rig = new GameObject("CameraRig").transform;
            cam.transform.SetParent(rig, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.orthographic = false;
            cam.fieldOfView = Fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.035f, 0.05f);
            cam.allowHDR = true;
            cam.allowMSAA = true;
            Fit(16f / 9f, 1f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.095f, 0.12f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.07f, 0.035f, 0.05f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 100f;

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
            rig.rotation = rot;
            rig.position = FrameCenter - rot * Vector3.forward * d;
        }

        /// <summary>Point de la planche (z = 0) sous la souris.</summary>
        public Vector2 MouseOnBoard()
        {
            var ray = cam.ScreenPointToRay(new Vector3(InputShim.MousePosition.x, InputShim.MousePosition.y, 0f));
            if (Mathf.Abs(ray.direction.z) < 1e-4f) return Vector2.zero;
            float t = -ray.origin.z / ray.direction.z;
            var p = ray.origin + ray.direction * t;
            return new Vector2(p.x, p.y);
        }

        public void Frenzy(float seconds) => frenzy = Mathf.Max(frenzy, seconds);
        /// <summary>La borne s'emballe (gros coup) : les joueurs de la salle lèvent les bras.</summary>
        public bool InFrenzy => frenzy > 0f;

        // ------------------------------------------------------------------ Borne

        void BuildCabinet()
        {
            var root = new GameObject("Cabinet").transform;
            root.SetParent(transform, false);
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
                tower.SetParent(transform, false);
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
                if (side < 0) leftDisplay = canvas; else rightDisplay = canvas;
            }

            // Écran central (choix des seuils, boutique, résultats) : une vitre devant la planche
            overlayDisplay = MakeWorldCanvas("CenterDisplay", transform, new Vector3(0f, 5.3f, -1.9f), new Vector2(824f, 1080f), 0.0116f, 60);
        }

        RectTransform MakeWorldCanvas(string name, Transform parent, Vector3 localPos, Vector2 size, float unitsPerPixel, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam;
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
            root.SetParent(transform, false);
            var carpet = Mats.Get("Carpet", new Color(0.22f, 0.05f, 0.09f), 0f, 0.15f);
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, Crowd.FloorY - 0.1f, 14f), new Vector3(120f, 0.2f, 60f), carpet, "Floor");
            // Motif du tapis : losanges dorés en quinconce
            var motif = Mats.Get("CarpetMotif", new Color(0.45f, 0.3f, 0.1f), 0.2f, 0.3f);
            int line = 0;
            for (float z = 4f; z <= 36f; z += 4.5f, line++)
                for (float x = -48f; x <= 48f; x += 6f)
                {
                    var d = Mats.Prim(PrimitiveType.Cube, root, new Vector3(x + (line % 2) * 3f, Crowd.FloorY + 0.005f, z),
                        new Vector3(0.9f, 0.01f, 0.9f), motif, "Motif");
                    d.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    d.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

            // Murs et plafond (dalles lumineuses)
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, 6f, 37f), new Vector3(120f, 30f, 0.2f), Mats.Wall, "BackWall");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, Crowd.FloorY + 3f, 36.8f), new Vector3(120f, 6f, 0.2f), Mats.DarkWood, "Wainscot");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(-55f, 6f, 14f), new Vector3(0.2f, 30f, 60f), Mats.Wall, "SideWall_L");
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(55f, 6f, 14f), new Vector3(0.2f, 30f, 60f), Mats.Wall, "SideWall_R");
            float ceilY = Crowd.FloorY + 26f;
            Mats.Prim(PrimitiveType.Cube, root, new Vector3(0f, ceilY, 14f), new Vector3(120f, 0.2f, 60f), Mats.Wall, "Ceiling");
            var panel = Mats.Get("CeilingPanel", new Color(1f, 0.85f, 0.6f), 0f, 0.5f, new Color(0.9f, 0.65f, 0.35f));
            for (float z = 9f; z <= 33f; z += 8f)
                for (float x = -40f; x <= 40f; x += 10f)
                {
                    var p = Mats.Prim(PrimitiveType.Cube, root, new Vector3(x, ceilY - 0.12f, z), new Vector3(5f, 0.05f, 2f), panel, "CeilingPanel");
                    p.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

            // Machines, joueurs et passants
            var crowd = new GameObject("Crowd").AddComponent<Crowd>();
            crowd.transform.SetParent(root, false);
            if (!crowd.IsBuilt) crowd.Build();

            // Enseignes néon au mur du fond (sur les côtés : le centre est caché par la borne)
            Neon(root, "JACKPOT", new Vector3(-36f, 13f, 36.8f), Palette.Pink, 5f);
            Neon(root, "CASINO", new Vector3(-17f, 16f, 36.8f), Palette.Gold, 6f);
            Neon(root, "ARCADE", new Vector3(17f, 16f, 36.8f), Palette.Cyan, 6f);
            Neon(root, "OUVERT 24H", new Vector3(36f, 13f, 36.8f), new Color(0.7f, 0.4f, 1f), 4f);

            // Lampes au-dessus des rangées
            for (int i = 0; i < Crowd.RowZ.Length - 1; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var l = new GameObject("RowLight").AddComponent<Light>();
                    l.transform.SetParent(root, false);
                    l.transform.position = new Vector3(side * 17f, ceilY - 4f, Crowd.RowZ[i] - 2f);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.75f, 0.5f);
                    l.intensity = 2.4f;
                    l.range = 20f;
                    l.shadows = LightShadows.None;
                }
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
            var root = new GameObject("Lights").transform;
            root.SetParent(transform, false);
            var key = new GameObject("KeySpot").AddComponent<Light>();
            key.transform.SetParent(root, false);
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
            fill.transform.SetParent(root, false);
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.5f, 0.75f);
            fill.intensity = 0.25f;
            fill.transform.rotation = Quaternion.Euler(25f, -30f, 0f);

            var marquee = new GameObject("MarqueeLight").AddComponent<Light>();
            marquee.transform.SetParent(root, false);
            marquee.type = LightType.Point;
            marquee.color = new Color(1f, 0.7f, 0.4f);
            marquee.intensity = 1.6f;
            marquee.range = 6f;
            marquee.transform.position = new Vector3(0f, 12.5f, -2f);

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(root, false);
            probeGo.transform.position = new Vector3(0f, 5f, -4f);
            probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(40f, 30f, 30f);
            probe.resolution = 128;
            probe.intensity = 1f;
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
                if (bulbs[i] != null) bulbs[i].sharedMaterial = on ? Mats.BulbOn : Mats.BulbOff;
            }
        }
    }
}
