// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>
    /// Géométrie de la planche (calibrée par simulation physique) :
    /// bille de rayon 0,3, pics de rayon 0,12, 10 rangées décalées, 9 cases de largeur 1.
    /// Lâcher entre -1,5 et 1,5 : les cases de bord restent rares (~1 %) → frisson du « presque ! ».
    /// </summary>
    public static class BoardLayout
    {
        public const int Rows = 10;
        public const float BottomRowY = 0.9f;
        public const float RowSpacing = 0.85f;
        public const int SlotCount = 9;
        public const float SlotWidth = 1f;
        public const float WallX = 4.6f;
        public const float WallTop = 10.6f;
        public const float DividerHeight = 0.6f;
        public const float PegRadius = 0.12f;
        public const float BallRadius = 0.3f;
        public const float DropY = 9.9f;
        public const float DropMinX = -1.5f;
        public const float DropMaxX = 1.5f;
        public const float BallBounce = 0.3f;
        public const float BallGravityScale = 2.5f;
        public const float BallDrag = 0.22f;

        /// <summary>Valeurs initiales des cases (centre sûr, bords rares et énormes).</summary>
        public static readonly float[] BaseSlotValues = { 10, 5, 3, 2, 1, 2, 3, 5, 10 };

        public static int PegsInRow(int row) => row % 2 == 0 ? 8 : 9;

        /// <summary>row 0 = rangée du bas.</summary>
        public static Vector2 PegPosition(int row, int col)
        {
            float y = BottomRowY + row * RowSpacing;
            float x = row % 2 == 0 ? -3.5f + col : -4f + col;
            return new Vector2(x, y);
        }

        public static float SlotCenterX(int i) => -4f + i * SlotWidth;
        public static int CenterSlot => SlotCount / 2;
    }
}
