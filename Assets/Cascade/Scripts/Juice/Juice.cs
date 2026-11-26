// CASCADE — Auteur : Coulu
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cascade
{
    /// <summary>
    /// Le « juice » : sons synthétisés (chaque pic est une note d'une gamme pentatonique),
    /// particules, tremblement de caméra, popups de score, paliers WOW / INCROYABLE / BROKEN.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class Juice : MonoBehaviour
    {
        [Range(0f, 1f)] public float masterVolume = 0.55f;
        [Tooltip("Force des secousses de caméra (0 : aucune, 1 : forte). Futur réglage des options.")]
        [Range(0f, 1f)] public float shakeStrength = 0.5f;
        [Tooltip("Caméra secouée (enfant du CameraRig : le tremblement agit en position locale).")]
        [SerializeField] Camera cam;
        [Tooltip("Canvas monde des popups de score.")]
        [SerializeField] Transform worldCanvas;
        [SerializeField] VhsFeed vhs;

        AudioSource[] sources;
        int nextSource;
        AudioClip[] notes, multiNotes;
        AudioClip thud, wall, bumper, coin, iceBreak, drop, land, cash, win, fail, tilt, shake, click, buy, fire, relic;

        float shakeAmp;
        public VhsFeed Vhs => vhs;

        // Particules (pool léger, sans ParticleSystem)
        class Particle { public SpriteRenderer sr; public Vector2 v; public float life, max, size; }
        readonly List<Particle> particles = new List<Particle>();
        readonly Stack<Particle> freeParticles = new Stack<Particle>();
        Transform particleRoot;

        // Popups de score
        class Popup { public Text text; public Vector3 start; public float life, max, scale; }
        readonly List<Popup> popups = new List<Popup>();
        readonly Stack<Text> freePopups = new Stack<Text>();

        const int SampleRate = 44100;

        /// <summary>Relie les effets aux objets de la scène (utilisé par l'outil qui construit la scène).</summary>
        public void Wire(Camera camera, Transform popupCanvas, VhsFeed feed)
        {
            cam = camera; worldCanvas = popupCanvas; vhs = feed;
        }

        void Awake()
        {
            if (GameSettings.TryGetMasterVolume(out var vol)) masterVolume = vol;
            if (cam == null) cam = Arcade.Instance != null ? Arcade.Instance.Cam : Camera.main;
            if (worldCanvas == null && GameUI.Instance != null) worldCanvas = GameUI.Instance.WorldCanvas;
            if (vhs == null) vhs = FindAnyObjectByType<VhsFeed>();
            particleRoot = new GameObject("Particles").transform;
            particleRoot.SetParent(transform, false);

            sources = new AudioSource[14];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }
            BuildClips();
        }

        // ================= Audio =================

        void BuildClips()
        {
            // Gamme pentatonique majeure à partir de Do5 : la chute devient une mélodie.
            int[] steps = { 0, 2, 4, 7, 9 };
            notes = new AudioClip[15];
            multiNotes = new AudioClip[15];
            for (int i = 0; i < 15; i++)
            {
                int semis = steps[i % 5] + 12 * (i / 5);
                float f = 523.25f * Mathf.Pow(2f, semis / 12f);
                notes[i] = Tone("note" + i, f, 0.35f, 9f, new[] { 1f, 0.35f, 0.1f });
                multiNotes[i] = Tone("mnote" + i, f, 0.45f, 6f, new[] { 1f, 0.5f, 0.4f, 0.25f }, vibrato: 6f);
            }
            thud = Tone("thud", 140f, 0.12f, 30f, new[] { 1f, 0.2f });
            wall = Tone("wall", 110f, 0.15f, 25f, new[] { 1f, 0.5f }, noise: 0.15f);
            bumper = Sweep("bumper", 220f, 880f, 0.18f);
            coin = Chain("coin", new[] { 1318.5f, 1975.5f }, 0.07f, 0.25f);
            iceBreak = Tone("ice", 2600f, 0.25f, 18f, new[] { 1f, 0.6f }, noise: 0.5f);
            drop = Sweep("drop", 700f, 350f, 0.12f, 0.4f);
            land = Chord("land", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.7f);
            cash = Chain("cash", new[] { 1567.98f, 2093f, 2637f }, 0.06f, 0.5f, noise: 0.25f);
            win = Chain("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.09f, 0.6f);
            fail = Sweep("fail", 330f, 110f, 0.45f);
            tilt = Square("tilt", 90f, 0.45f);
            shake = Tone("shake", 180f, 0.18f, 14f, new[] { 1f }, noise: 0.8f);
            click = Tone("click", 1200f, 0.04f, 60f, new[] { 1f });
            buy = Chain("buy", new[] { 783.99f, 1174.66f }, 0.07f, 0.3f);
            fire = Sweep("fire", 90f, 260f, 0.9f, 0.9f);
            relic = Chain("relic", new[] { 1046.5f, 1318.5f, 1568f, 2093f }, 0.055f, 0.45f, noise: 0.05f);
        }

        void Play(AudioClip clip, float vol = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var s = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, vol * masterVolume);
        }

        /// <summary>Note d'un pic. heat (0 à 1) : chaleur du lancer, plus fort et une octave en renfort quand ça chauffe.</summary>
        public void PegNote(int index, PegType type, bool disabled, float heat = 0f)
        {
            if (disabled) { Play(thud, 0.5f); return; }
            index = Mathf.Clamp(index, 0, 14);
            var bank = type == PegType.Multi ? multiNotes : notes;
            float vol = (type == PegType.Multi ? 0.55f : 0.4f) + 0.45f * heat;
            Play(bank[index], vol, Random.Range(0.99f, 1.01f));
            // 5 notes de la gamme pentatonique = une octave
            if (heat > 0.66f) Play(bank[Mathf.Min(index + 5, 14)], vol * (heat - 0.5f), 1f);
        }

        public void PlayWall(float heat = 0f) => Play(wall, 0.4f + 0.4f * heat, 1f + 0.2f * heat);
        public void PlayBumper() => Play(bumper, 0.6f);
        public void PlayCoin() => Play(coin, 0.6f);
        public void PlayIceBreak() => Play(iceBreak, 0.5f);
        public void PlayDrop() => Play(drop, 0.5f);
        public void PlayCash() => Play(cash, 0.8f);
        public void PlayWin() => Play(win, 0.8f);
        public void PlayFail() => Play(fail, 0.6f);
        public void PlayTilt() { Play(tilt, 0.7f); Vhs?.Glitch(1f); }
        public void PlayShake() => Play(shake, 0.6f);
        public void PlayClick() => Play(click, 0.4f);
        public void PlayBuy() => Play(buy, 0.6f);
        public void PlayFire() => Play(fire, 0.55f);
        /// <summary>Carillon d'une nouvelle relique.</summary>
        public void PlayRelic() => Play(relic, 0.55f);
        /// <summary>Clic de décompte : chaque étape un peu plus aiguë.</summary>
        public void PlayTick(float step) => Play(click, 0.7f, 0.8f + step * 0.9f);

        // ---------- Synthèse ----------

        static AudioClip Make(string name, float dur, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)SampleRate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float decay) => Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * decay);

        static AudioClip Tone(string name, float freq, float dur, float decay, float[] harmonics, float noise = 0f, float vibrato = 0f)
        {
            var rnd = new System.Random(name.GetHashCode());
            return Make(name, dur, t =>
            {
                float s = 0f;
                float fm = vibrato > 0 ? 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * vibrato * t) : 1f;
                for (int h = 0; h < harmonics.Length; h++)
                    s += harmonics[h] * Mathf.Sin(2f * Mathf.PI * freq * fm * (h + 1) * t);
                if (noise > 0) s += noise * ((float)rnd.NextDouble() * 2f - 1f);
                return 0.35f * s * Env(t, decay);
            });
        }

        static AudioClip Sweep(string name, float f0, float f1, float dur, float noise = 0f)
        {
            var rnd = new System.Random(name.GetHashCode());
            float phase = 0f;
            float dt = 1f / SampleRate;
            return Make(name, dur, t =>
            {
                float f = Mathf.Lerp(f0, f1, t / dur);
                phase += 2f * Mathf.PI * f * dt;
                float s = Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f);
                if (noise > 0) s += noise * ((float)rnd.NextDouble() * 2f - 1f);
                return 0.35f * s * Env(t, 5f / dur);
            });
        }

        static AudioClip Chain(string name, float[] freqs, float step, float dur, float noise = 0f)
        {
            var rnd = new System.Random(name.GetHashCode());
            return Make(name, dur + step * freqs.Length, t =>
            {
                float s = 0f;
                for (int i = 0; i < freqs.Length; i++)
                {
                    float lt = t - i * step;
                    if (lt < 0) continue;
                    s += (Mathf.Sin(2f * Mathf.PI * freqs[i] * lt) + 0.3f * Mathf.Sin(4f * Mathf.PI * freqs[i] * lt)) * Env(lt, 8f);
                }
                if (noise > 0 && t < 0.05f) s += noise * ((float)rnd.NextDouble() * 2f - 1f);
                return 0.3f * s;
            });
        }

        static AudioClip Chord(string name, float[] freqs, float dur)
        {
            return Make(name, dur, t =>
            {
                float s = 0f;
                for (int i = 0; i < freqs.Length; i++)
                {
                    float lt = t - i * 0.025f;
                    if (lt < 0) continue;
                    s += Mathf.Sin(2f * Mathf.PI * freqs[i] * lt) * Env(lt, 4f);
                }
                return 0.22f * s;
            });
        }

        static AudioClip Square(string name, float freq, float dur)
        {
            return Make(name, dur, t => 0.25f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * Env(t, 4f));
        }

        // ================= Particules =================

        public void Burst(Vector3 pos, Color color, int count, float speed, bool keepZ = false)
        {
            for (int i = 0; i < count; i++)
            {
                var p = freeParticles.Count > 0 ? freeParticles.Pop() : NewParticle();
                p.sr.gameObject.SetActive(true);
                p.sr.transform.position = new Vector3(pos.x, pos.y, keepZ ? pos.z - 0.3f : -0.35f);
                p.sr.color = color;
                float a = Random.Range(0f, Mathf.PI * 2f);
                p.v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.3f, 1f);
                p.max = p.life = Random.Range(0.25f, 0.6f);
                p.size = Random.Range(0.06f, 0.16f);
                particles.Add(p);
            }
        }

        Particle NewParticle()
        {
            var sr = Sprites.MakeRenderer("P", particleRoot, Sprites.Glow, 30);
            return new Particle { sr = sr };
        }

        // ================= Popups =================

        public void ScorePopup(Vector3 world, string text, Color color, float scale)
        {
            if (popups.Count > 60) return;
            var t = freePopups.Count > 0 ? freePopups.Pop() : UIKit.WorldLabel(worldCanvas, Vector2.zero, "", 30, Color.white);
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = color;
            var start = new Vector3(world.x + Random.Range(-0.15f, 0.15f), world.y + 0.3f, -0.5f);
            t.transform.position = start;
            popups.Add(new Popup { text = t, start = start, life = 0.8f, max = 0.8f, scale = scale });
        }

        /// <summary>Célébration à l'arrivée d'une bille, proportionnelle au score par rapport à la cible.</summary>
        public void CelebrateBall(ScoreContext c, double target, Vector3 at)
        {
            double ratio = target > 0 ? c.finalScore / target : 0;
            Play(land, 0.7f, 1f + Mathf.Clamp01((float)ratio) * 0.25f);
            ScorePopup(at + Vector3.up * 0.4f, "+" + Fmt.Num(c.finalScore), Palette.Gold, 2.2f);
            int count = Mathf.Clamp(10 + (int)(ratio * 80), 10, 90);
            Burst(at, Palette.Gold, count, 4f + (float)ratio * 4f);
            Burst(at, Board.SlotColor(c.slotValue), count / 2, 3f);

            var ui = GameManager.Instance.UI;
            if (ratio >= 1.0) { ui.ShowTier("BROKEN", Palette.Gold); Shake(0.6f); PlayCash(); Vhs?.Glitch(1f); Arcade.Instance?.Frenzy(3f); }
            else if (ratio >= 0.5) { ui.ShowTier("INCROYABLE", Palette.Pink); Shake(0.4f); PlayCash(); Vhs?.Glitch(0.6f); Arcade.Instance?.Frenzy(2f); }
            else if (ratio >= 0.2) { ui.ShowTier("WOW", Palette.Cyan); Shake(0.22f); Vhs?.Glitch(0.25f); Arcade.Instance?.Frenzy(1f); }
            else Shake(0.08f);
        }

        // ================= Caméra =================

        public void Shake(float amount) => shakeAmp = Mathf.Max(shakeAmp, amount);

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float sdt = Time.deltaTime;

            shakeAmp = Mathf.MoveTowards(shakeAmp, 0f, dt * 1.6f);
            if (cam != null)
                cam.transform.localPosition = (Vector3)(Random.insideUnitCircle * Mathf.Min(shakeAmp, 0.8f) * 0.35f * shakeStrength);

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.life -= sdt;
                if (p.life <= 0f)
                {
                    p.sr.gameObject.SetActive(false);
                    freeParticles.Push(p);
                    particles.RemoveAt(i);
                    continue;
                }
                p.v *= 1f - sdt * 2.5f;
                p.v.y -= sdt * 3f;
                p.sr.transform.position += (Vector3)(p.v * sdt);
                float k = p.life / p.max;
                p.sr.transform.localScale = Vector3.one * p.size * (0.5f + k) * 3f;
                var c = p.sr.color; c.a = k; p.sr.color = c;
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.text.gameObject.SetActive(false);
                    freePopups.Push(p.text);
                    popups.RemoveAt(i);
                    continue;
                }
                float k = 1f - p.life / p.max;
                p.text.transform.position = p.start + Vector3.up * (0.7f * Mathf.Sqrt(k));
                float punch = k < 0.15f ? Mathf.Lerp(1.5f, 1f, k / 0.15f) : 1f;
                p.text.transform.localScale = Vector3.one * UIKit.WorldTextScale * p.scale * punch;
                var c = p.text.color; c.a = k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f; p.text.color = c;
            }
        }
    }

    /// <summary>Fond étoilé qui scintille.</summary>
    public class Starfield : MonoBehaviour
    {
        SpriteRenderer[] stars;
        float[] phase;

        public void Init(int count, Rect area)
        {
            var bg = Sprites.MakeRenderer("Gradient", transform, Sprites.Square, -100);
            bg.transform.position = new Vector3(area.center.x, area.center.y, 5f);
            bg.transform.localScale = new Vector3(area.width * 1.5f, area.height * 1.5f, 1f);
            bg.color = Palette.Background;
            var top = Sprites.MakeRenderer("Nebula", transform, Sprites.Glow, -99);
            top.transform.position = new Vector3(0f, area.yMax - 1f, 5f);
            top.transform.localScale = new Vector3(area.width * 1.2f, area.height * 0.9f, 1f);
            top.color = Palette.Violet.WithAlpha(0.18f);
            var pink = Sprites.MakeRenderer("Nebula2", transform, Sprites.Glow, -99);
            pink.transform.position = new Vector3(area.xMin + 3f, area.yMin + 2f, 5f);
            pink.transform.localScale = new Vector3(12f, 9f, 1f);
            pink.color = Palette.Pink.WithAlpha(0.07f);

            stars = new SpriteRenderer[count];
            phase = new float[count];
            var rnd = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                var s = Sprites.MakeRenderer("Star", transform, Sprites.Star, -90);
                s.transform.position = new Vector3(area.xMin + (float)rnd.NextDouble() * area.width, area.yMin + (float)rnd.NextDouble() * area.height, 5f);
                s.transform.localScale = Vector3.one * (0.08f + (float)rnd.NextDouble() * 0.25f);
                s.color = (rnd.NextDouble() < 0.3 ? Palette.Cyan : rnd.NextDouble() < 0.5 ? Palette.Pink : Palette.White).WithAlpha(0.6f);
                stars[i] = s;
                phase[i] = (float)rnd.NextDouble() * 10f;
            }
        }

        void Update()
        {
            if (stars == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < stars.Length; i++)
            {
                var c = stars[i].color;
                c.a = 0.25f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * (0.6f + i % 5 * 0.3f) + phase[i]));
                stars[i].color = c;
            }
        }
    }
}
