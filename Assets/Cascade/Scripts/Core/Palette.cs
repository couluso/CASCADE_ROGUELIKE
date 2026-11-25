// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>Salle d'arcade : laque rouge, laiton, feutre vert, néons rose/cyan en accent, doré pour les gros scores.</summary>
    public static class Palette
    {
        public static readonly Color Background = Hex(0x0B0618);
        public static readonly Color BackgroundTop = Hex(0x1E0B3D);
        public static readonly Color Violet = Hex(0x8E2436);   // laque rouge casino (boutons)
        public static readonly Color Cyan = Hex(0x46C8D8);
        public static readonly Color Pink = Hex(0xE2507E);
        public static readonly Color Gold = Hex(0xF0BE4C);
        public static readonly Color Orange = Hex(0xFF8A2B);
        public static readonly Color Green = Hex(0x3DFF8E);
        public static readonly Color Red = Hex(0xFF3B5C);
        public static readonly Color Ice = Hex(0xAEEBFF);
        public static readonly Color White = Hex(0xF4F0FF);
        public static readonly Color Dim = Hex(0x6B6070);
        public static readonly Color Brass = Hex(0xC9A24A);
        public static readonly Color Panel = new Color(0.055f, 0.045f, 0.06f, 0.94f);
        public static readonly Color PanelLight = new Color(0.11f, 0.09f, 0.12f, 0.96f);
        public static readonly Color Chips = Hex(0x2F8FE0);   // bleu « jetons » façon Balatro
        public static readonly Color Mult = Hex(0xE0414F);    // rouge « mult »

        public static Color Hex(int rgb, float a = 1f)
            => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

        public static Color ForRarity(Rarity r) => r switch
        {
            Rarity.Common => Cyan,
            Rarity.Uncommon => Green,
            Rarity.Rare => Pink,
            _ => Gold
        };

        public static string RarityName(Rarity r) => r switch
        {
            Rarity.Common => "Commune",
            Rarity.Uncommon => "Peu commune",
            Rarity.Rare => "Rare",
            _ => "Légendaire"
        };
    }
}
