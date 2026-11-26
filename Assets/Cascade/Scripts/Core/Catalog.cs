// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>Toutes les données « fixes » : noms, descriptions, couleurs, prix. Équilibrage centralisé ici.</summary>
    public static class Catalog
    {
        // ---------------- Pics ----------------
        public struct PegInfo { public string name, desc; public Color color; }

        public static PegInfo Peg(PegType t) => t switch
        {
            PegType.Standard => new PegInfo { name = "Pic standard", desc = "+5 jetons.", color = Palette.Cyan },
            PegType.Multi => new PegInfo { name = "Pic multiplicateur", desc = "+1 mult.", color = Palette.Pink },
            PegType.Gold => new PegInfo { name = "Pic doré", desc = "+5 jetons, +1 ticket.", color = Palette.Gold },
            PegType.Bumper => new PegInfo { name = "Pic bumper", desc = "+15 jetons, propulse la bille.", color = Palette.Green },
            PegType.Ice => new PegInfo { name = "Pic de glace", desc = "+5 jetons. Se brise au 2e contact : x2 mult.", color = Palette.Ice },
            PegType.Spiky => new PegInfo { name = "Pic épineux", desc = "+20 jetons, -1 mult (min 1).", color = Palette.Orange },
            PegType.WallBump => new PegInfo { name = "Butoir", desc = "Empêche la bille de glisser le long du mur.", color = Palette.Dim },
            _ => new PegInfo { name = "Vide", desc = "Aucun pic.", color = Palette.Dim }
        };

        public const double StandardChips = 5, GoldChips = 5, BumperChips = 15, IceChips = 5, SpikyChips = 20;
        public const double BurningBonusChips = 5;
        public const float BumperImpulse = 5f;

        // ---------------- Billes ----------------
        public struct BallInfo
        {
            public string name, desc; public Color color; public int price;
            public float gravityScale, bounce, radiusMul;
        }

        public static BallInfo Ball(BallKind k) => k switch
        {
            BallKind.Standard => new BallInfo { name = "Bille standard", desc = "Fiable. Rien de spécial.", color = Palette.White, price = 2, gravityScale = 1f, bounce = BoardLayout.BallBounce, radiusMul = 1f },
            BallKind.Heavy => new BallInfo { name = "Bille lourde", desc = "Tombe vite, rebondit peu. +3 jetons par pic.", color = Hex(0x8C9BB5), price = 4, gravityScale = 1.5f, bounce = 0.1f, radiusMul = 1.05f },
            BallKind.Light => new BallInfo { name = "Bille légère", desc = "Très rebondissante. +0,5 mult par rebond au-delà du 10e.", color = Hex(0xC9F2FF), price = 5, gravityScale = 0.8f, bounce = 0.55f, radiusMul = 0.9f },
            BallKind.Bouncy => new BallInfo { name = "Bille rebondissante", desc = "Chaque rebond sur un mur : +2 mult.", color = Palette.Green, price = 4, gravityScale = 1f, bounce = 0.45f, radiusMul = 1f },
            BallKind.Twin => new BallInfo { name = "Bille jumelle", desc = "Se divise en deux au premier pic. Les deux comptent.", color = Palette.Violet, price = 6, gravityScale = 1f, bounce = BoardLayout.BallBounce, radiusMul = 0.95f },
            BallKind.Fire => new BallInfo { name = "Bille de feu", desc = "Enflamme les pics touchés pendant 3 lancers (+5 jetons chacun).", color = Palette.Orange, price = 5, gravityScale = 1f, bounce = BoardLayout.BallBounce, radiusMul = 1f },
            BallKind.Crystal => new BallInfo { name = "Bille cristal", desc = "x3 mult à l'arrivée... si elle survit. Se brise à la 22e collision.", color = Hex(0x9DFFF5), price = 5, gravityScale = 1f, bounce = BoardLayout.BallBounce, radiusMul = 1f },
            BallKind.Golden => new BallInfo { name = "Bille dorée", desc = "+3 tickets à l'arrivée, mais ne marque que ses jetons.", color = Palette.Gold, price = 4, gravityScale = 1f, bounce = BoardLayout.BallBounce, radiusMul = 1f },
            _ => default
        };

        public const int CrystalBreakAt = 22;
        public const int GoldenTickets = 3;

        // ---------------- Éditions ----------------
        public static string EditionName(Edition e) => e switch
        {
            Edition.Foil => "Foil", Edition.Holo => "Holographique", Edition.Polychrome => "Polychrome", _ => ""
        };

        public static string EditionDesc(Edition e) => e switch
        {
            Edition.Foil => "+50 jetons", Edition.Holo => "+10 mult", Edition.Polychrome => "x1,5 mult", _ => ""
        };

        public static Color EditionColor(Edition e) => e switch
        {
            Edition.Foil => Palette.Chips, Edition.Holo => Palette.Mult, Edition.Polychrome => Palette.Gold, _ => Color.clear
        };

        public static int EditionPrice(Edition e) => e switch
        {
            Edition.Foil => 2, Edition.Holo => 3, Edition.Polychrome => 5, _ => 0
        };

        // ---------------- Boss ----------------
        public struct BossInfo { public string name, desc; }

        public static BossInfo Boss(BossType b) => b switch
        {
            BossType.PegWarden => new BossInfo { name = "Le Gardien des Pics", desc = "30 % des pics sont neutralisés." },
            BossType.NorthWind => new BossInfo { name = "Le Vent du Nord", desc = "Un vent latéral aléatoire souffle à chaque lancer." },
            BossType.IceWall => new BossInfo { name = "Le Mur de Glace", desc = "Tous les pics se brisent au premier contact." },
            BossType.Cheater => new BossInfo { name = "Le Tricheur", desc = "Les cases changent de valeur après chaque bille." },
            BossType.Blind => new BossInfo { name = "L'Aveugle", desc = "Prochaine bille cachée, le lanceur va plus vite." },
            BossType.Banker => new BossInfo { name = "Le Banquier", desc = "Les cases valent moitié, les jetons sont doublés." },
            BossType.Colossus => new BossInfo { name = "Le Colosse", desc = "Une seule bille géante. Jetons x5. Tout ou rien." },
            _ => new BossInfo { name = "", desc = "" }
        };

        public static readonly BossType[] BossPool =
        {
            BossType.PegWarden, BossType.NorthWind, BossType.IceWall, BossType.Cheater,
            BossType.Blind, BossType.Banker, BossType.Colossus
        };

        // ---------------- Étiquettes de skip ----------------
        public struct TagInfo { public string name, desc; }

        public static TagInfo Tag(SkipTag t) => t switch
        {
            SkipTag.Gold => new TagInfo { name = "Étiquette Or", desc = "+10 tickets." },
            SkipTag.Charm => new TagInfo { name = "Étiquette Charme", desc = "Un charme gratuit." },
            SkipTag.Shop => new TagInfo { name = "Étiquette Boutique", desc = "2 rerolls gratuits dans la prochaine boutique." },
            _ => new TagInfo { name = "Étiquette Planche", desc = "Un pic standard devient multiplicateur." }
        };

        // ---------------- Charmes ----------------
        public struct CharmInfo
        {
            public string name, desc; public int price;
            public bool needsPegTarget, roundOnly, duringFall;
        }

        public static CharmInfo Charm(CharmType c) => c switch
        {
            CharmType.BrushMulti => new CharmInfo { name = "Pinceau rose", desc = "Transforme un pic en pic multiplicateur.", price = 3, needsPegTarget = true },
            CharmType.BrushGold => new CharmInfo { name = "Pinceau doré", desc = "Transforme un pic en pic doré.", price = 3, needsPegTarget = true },
            CharmType.BrushBumper => new CharmInfo { name = "Pinceau vert", desc = "Transforme un pic en bumper.", price = 3, needsPegTarget = true },
            CharmType.BrushIce => new CharmInfo { name = "Pinceau givré", desc = "Transforme un pic en pic de glace.", price = 3, needsPegTarget = true },
            CharmType.Hammer => new CharmInfo { name = "Marteau", desc = "Détruit un pic (ouvre un passage).", price = 2, needsPegTarget = true },
            CharmType.Hourglass => new CharmInfo { name = "Charme du Sablier", desc = "+1 bille pour la manche en cours.", price = 4, roundOnly = true },
            CharmType.Clone => new CharmInfo { name = "Charme du Clone", desc = "Ajoute au sac une copie de la prochaine bille.", price = 4, roundOnly = true },
            CharmType.Gift => new CharmInfo { name = "Charme du Cadeau", desc = "Donne une relique commune.", price = 4 },
            CharmType.Gamble => new CharmInfo { name = "Charme du Pari", desc = "50 % : double tes tickets (max +20). 50 % : en perd la moitié.", price = 2 },
            CharmType.SlowMo => new CharmInfo { name = "Charme du Ralenti", desc = "Pendant une chute : ralentit le temps 3 secondes.", price = 2, duringFall = true },
            _ => default
        };

        /// <summary>Pic créé par un pinceau (None pour les autres charmes).</summary>
        public static PegType BrushResult(CharmType c) => c switch
        {
            CharmType.BrushMulti => PegType.Multi,
            CharmType.BrushGold => PegType.Gold,
            CharmType.BrushBumper => PegType.Bumper,
            CharmType.BrushIce => PegType.Ice,
            _ => PegType.None
        };

        /// <summary>Description complète d'un charme, avec l'effet du pic qu'il crée (pour les infobulles).</summary>
        public static string CharmFullDesc(CharmType c)
        {
            var info = Charm(c);
            string s = info.desc;
            var peg = BrushResult(c);
            if (peg != PegType.None)
                s += $"\n<color=#F0BE4C>{Peg(peg).name}</color> : {Peg(peg).desc}";
            if (c == CharmType.Hammer)
                s += "\n<color=#6B6070>L'emplacement vide reste visible : un pinceau peut y remettre un pic.</color>";
            if (info.duringFall) s += "\n<color=#46C8D8>Utilisable pendant une chute.</color>";
            else if (info.roundOnly) s += "\n<color=#46C8D8>Utilisable pendant une manche, avant un lancer.</color>";
            else if (info.needsPegTarget) s += "\n<color=#46C8D8>Utilisable en manche, au choix du seuil ou en boutique.</color>";
            return s;
        }

        public static readonly CharmType[] AllCharms =
        {
            CharmType.BrushMulti, CharmType.BrushGold, CharmType.BrushBumper, CharmType.BrushIce, CharmType.Hammer,
            CharmType.Hourglass, CharmType.Clone, CharmType.Gift, CharmType.Gamble, CharmType.SlowMo
        };

        /// <summary>Billes possibles au départ : toutes les spéciales sauf la plus forte (la jumelle double le score).</summary>
        public static readonly BallKind[] StarterBalls =
        {
            BallKind.Heavy, BallKind.Light, BallKind.Bouncy, BallKind.Fire, BallKind.Crystal, BallKind.Golden
        };

        public static readonly BallKind[] ShopBalls =
        {
            BallKind.Heavy, BallKind.Light, BallKind.Bouncy, BallKind.Twin, BallKind.Fire, BallKind.Crystal, BallKind.Golden
        };

        static Color Hex(int v) => Palette.Hex(v);
    }

    /// <summary>Une bille du sac (type + édition).</summary>
    [System.Serializable]
    public class BallInstance
    {
        public BallKind kind;
        public Edition edition;

        public BallInstance(BallKind kind, Edition edition = Edition.None) { this.kind = kind; this.edition = edition; }
        public BallInstance Clone() => new BallInstance(kind, edition);

        public string Name => Catalog.Ball(kind).name + (edition != Edition.None ? $" ({Catalog.EditionName(edition)})" : "");
        public string Desc => Catalog.Ball(kind).desc + (edition != Edition.None ? $"\n<color=#FFC83D>{Catalog.EditionName(edition)} : {Catalog.EditionDesc(edition)}</color>" : "");
        public Color Color => Catalog.Ball(kind).color;
    }
}
