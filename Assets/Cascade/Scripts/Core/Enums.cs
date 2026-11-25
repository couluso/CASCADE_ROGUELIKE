// CASCADE — Auteur : Coulu
namespace Cascade
{
    public enum PegType { None, Standard, Multi, Gold, Bumper, Ice, Spiky, WallBump }
    public enum BallKind { Standard, Heavy, Light, Bouncy, Twin, Fire, Crystal, Golden }
    public enum Edition { None, Foil, Holo, Polychrome }
    public enum SeuilKind { Small = 0, Big = 1, Boss = 2 }
    public enum BossType { None, PegWarden, NorthWind, IceWall, Cheater, Blind, Banker, Colossus }
    public enum SkipTag { Gold, Charm, Shop, Board }
    public enum CharmType { BrushMulti, BrushGold, BrushBumper, BrushIce, Hammer, Hourglass, Clone, Gift, Gamble, SlowMo }
    public enum Rarity { Common, Uncommon, Rare, Legendary }
    public enum Phase { None, SeuilSelect, Aiming, Falling, RoundResult, Shop, Targeting, GameOver, Victory }
}
