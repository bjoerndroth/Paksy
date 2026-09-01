using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// Kanonische Maße aus PlastiCAD. Empfehlung: 1 Unity-Unit = 1 mm.
    /// </summary>
    public static class PaksyUnits
    {
        public const float MillimetersPerUnit = 1f;

        public const float CellSizeMm = 27.5f;
        public const float HalfCellMm = 13.75f;
        public const float QuarterCellMm = 6.875f;

        public const float TubeOuterDiameterMm = 9.5f;
        public const float TubeInnerDiameterMm = 7.0f;
        public const float TubeInsertDepthMm = 10.5f;
        public const float TubeLengthMm = 27.5f;

        public const float PlateSizeMm = 25f;
        public const float PlateThicknessMm = 5f;

        /// <summary>JSON-X/Y stammen intern oft aus Canvas = mm * Scale.</summary>
        public const float PlastiCadCanvasScale = 2f;

        public const float SnapDistanceMm = 0.5f;

        public static float CellSize => CellSizeMm / MillimetersPerUnit;
        public static float HalfCell => HalfCellMm / MillimetersPerUnit;
        public static float TubeOuterRadius => (TubeOuterDiameterMm * 0.5f) / MillimetersPerUnit;

        public static Vector3 SnapToCellCorner(Vector3 worldMm)
        {
            return new Vector3(
                Mathf.Round(worldMm.x / CellSize) * CellSize,
                Mathf.Round(worldMm.y / CellSize) * CellSize,
                Mathf.Round(worldMm.z / CellSize) * CellSize);
        }

        public static Vector3 SnapToHalfCell(Vector3 worldMm)
        {
            return new Vector3(
                Mathf.Round(worldMm.x / HalfCell) * HalfCell,
                Mathf.Round(worldMm.y / HalfCell) * HalfCell,
                Mathf.Round(worldMm.z / HalfCell) * HalfCell);
        }
    }
}
