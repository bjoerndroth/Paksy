using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// Mapping PlastiCAD-Datei (mm nach Scale-Korrektur) → Unity Y-up.
    ///
    /// unity.x = x_mm
    /// unity.y = z_mm          // PlastiCAD-Z wird Höhe
    /// unity.z = -y_mm         // Plan-Y gespiegelt
    ///
    /// Vorzeichen von Z nach dem ersten Testmodell (Rohr + Winkel) festnageln.
    /// </summary>
    public static class PaksyCoords
    {
        public static bool DivideXyByCanvasScale = true;
        public static bool FlipPlanY = true;

        public static Vector3 FileToMm(float x, float y, float z)
        {
            float scale = DivideXyByCanvasScale ? PaksyUnits.PlastiCadCanvasScale : 1f;
            return new Vector3(x / scale, y / scale, z);
        }

        public static Vector3 MmFileToUnity(Vector3 fileMm)
        {
            float planY = FlipPlanY ? -fileMm.y : fileMm.y;
            return new Vector3(fileMm.x, fileMm.z, planY);
        }

        public static Vector3 UnityToMmFile(Vector3 unity)
        {
            float planY = FlipPlanY ? -unity.z : unity.z;
            return new Vector3(unity.x, planY, unity.y);
        }

        /// <summary>
        /// PlastiCAD-Position ist oft die Zellenecke.
        /// Zellenmitte in Datei-mm: Position + (13.75, 13.75, 0).
        /// </summary>
        public static Vector3 CellCenterFromCornerFileMm(Vector3 cornerFileMm)
        {
            return cornerFileMm + new Vector3(PaksyUnits.HalfCellMm, PaksyUnits.HalfCellMm, 0f);
        }

        public static Vector3 ImportPositionUnity(float x, float y, float z, bool positionIsCellCorner = true)
        {
            Vector3 fileMm = FileToMm(x, y, z);
            if (positionIsCellCorner)
                fileMm = CellCenterFromCornerFileMm(fileMm);
            return MmFileToUnity(fileMm);
        }
    }
}
