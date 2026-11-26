// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Réglages du joueur enregistrés entre les sessions (PlayerPrefs) : base du futur menu d'options.
    /// Tant qu'un réglage n'a jamais été enregistré, c'est la valeur de l'inspecteur qui s'applique.
    /// </summary>
    public static class GameSettings
    {
        const string VhsIntensityKey = "cascade.vhs.intensity";
        const string VhsEnabledKey = "cascade.vhs.enabled";
        const string MasterVolumeKey = "cascade.audio.master";
        const string BestSeuilsKey = "cascade.stats.bestSeuils";
        const string BestBallKey = "cascade.stats.bestBall";

        public static bool TryGetVhsIntensity(out float v) => TryGet(VhsIntensityKey, out v);
        public static void SetVhsIntensity(float v) => Set(VhsIntensityKey, Mathf.Clamp01(v));

        public static bool VhsEnabled
        {
            get => PlayerPrefs.GetInt(VhsEnabledKey, 1) != 0;
            set { PlayerPrefs.SetInt(VhsEnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool TryGetMasterVolume(out float v) => TryGet(MasterVolumeKey, out v);
        public static void SetMasterVolume(float v) => Set(MasterVolumeKey, Mathf.Clamp01(v));

        // ---------- Records (affichés en fin de partie) ----------
        public static int BestSeuils => PlayerPrefs.GetInt(BestSeuilsKey, 0);
        public static double BestBall => double.TryParse(PlayerPrefs.GetString(BestBallKey, "0"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;

        /// <summary>Enregistre les records d'une run terminée ; renvoie vrai si l'un d'eux est battu.</summary>
        public static bool RecordRun(int seuilsCleared, double bestBall)
        {
            bool record = false;
            if (seuilsCleared > BestSeuils) { PlayerPrefs.SetInt(BestSeuilsKey, seuilsCleared); record = true; }
            if (bestBall > BestBall)
            {
                PlayerPrefs.SetString(BestBallKey, bestBall.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                record = true;
            }
            PlayerPrefs.Save();
            return record;
        }

        static bool TryGet(string key, out float v)
        {
            bool has = PlayerPrefs.HasKey(key);
            v = has ? PlayerPrefs.GetFloat(key) : 0f;
            return has;
        }

        static void Set(string key, float v)
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
        }
    }
}
