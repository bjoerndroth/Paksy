using System;
using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// Sechs Anschlüsse am unsichtbaren 27,5-mm-Würfel.
    /// Snap nur zwischen komplementären Faces.
    /// </summary>
    public enum PaksyFace
    {
        Left = 0,   // -X
        Right = 1,  // +X
        Bottom = 2, // -Y (Teil-unten)
        Top = 3,    // +Y (Teil-oben)
        Back = 4,   // -Z
        Front = 5   // +Z
    }

    public static class PaksyFaces
    {
        public static PaksyFace Opposite(PaksyFace face)
        {
            switch (face)
            {
                case PaksyFace.Left: return PaksyFace.Right;
                case PaksyFace.Right: return PaksyFace.Left;
                case PaksyFace.Bottom: return PaksyFace.Top;
                case PaksyFace.Top: return PaksyFace.Bottom;
                case PaksyFace.Back: return PaksyFace.Front;
                case PaksyFace.Front: return PaksyFace.Back;
                default: throw new ArgumentOutOfRangeException(nameof(face));
            }
        }

        public static bool AreComplementary(PaksyFace a, PaksyFace b) => Opposite(a) == b;

        /// <summary>Offset vom Zellenmittelpunkt in Teil-Lokalraum (mm, Y-up).</summary>
        public static Vector3 LocalOffset(PaksyFace face)
        {
            float r = PaksyUnits.HalfCell;
            switch (face)
            {
                case PaksyFace.Left: return new Vector3(-r, 0f, 0f);
                case PaksyFace.Right: return new Vector3(r, 0f, 0f);
                case PaksyFace.Bottom: return new Vector3(0f, -r, 0f);
                case PaksyFace.Top: return new Vector3(0f, r, 0f);
                case PaksyFace.Back: return new Vector3(0f, 0f, -r);
                case PaksyFace.Front: return new Vector3(0f, 0f, r);
                default: throw new ArgumentOutOfRangeException(nameof(face));
            }
        }

        public static Vector3 LocalDirection(PaksyFace face) => LocalOffset(face).normalized;
    }
}
