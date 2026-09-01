using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// 90°-Euler wie in PlastiCAD Transform.ApplyRotation: erst X, dann Y, dann Z.
    ///
    /// RotateX90: (x, y, z) → (x,  z, -y)
    /// RotateY90: (x, y, z) → (z,  y, -x)
    /// RotateZ90: (x, y, z) → (-y, x,  z)
    ///
    /// PlastiCAD-Y ist Datei-Y (Plan). Nach dem Mapping in Unity gilt dieselbe
    /// Reihenfolge auf den gemappten Achsen — erst am Testmodell prüfen.
    /// </summary>
    public static class PaksyRotation
    {
        public static Quaternion FromEuler90(float rotationX, float rotationY, float rotationZ)
        {
            return Quaternion.Euler(rotationX, rotationY, 0f) *
                   Quaternion.Euler(0f, 0f, rotationZ);
        }

        /// <summary>
        /// Explizite 90°-Vektorregeln aus PlastiCAD, angewendet in XYZ-Reihenfolge.
        /// Nützlich zum Transformieren von Face-Offsets im Dateiraum.
        /// </summary>
        public static Vector3 ApplyFileSpace90(Vector3 v, int stepsX, int stepsY, int stepsZ)
        {
            for (int i = 0; i < Mod4(stepsX); i++)
                v = new Vector3(v.x, v.z, -v.y);
            for (int i = 0; i < Mod4(stepsY); i++)
                v = new Vector3(v.z, v.y, -v.x);
            for (int i = 0; i < Mod4(stepsZ); i++)
                v = new Vector3(-v.y, v.x, v.z);
            return v;
        }

        public static int LegacyRotationToZ(int rotation)
        {
            return ((rotation % 4) + 4) % 4 * 90;
        }

        static int Mod4(int steps)
        {
            int n = steps % 4;
            return n < 0 ? n + 4 : n;
        }
    }
}
