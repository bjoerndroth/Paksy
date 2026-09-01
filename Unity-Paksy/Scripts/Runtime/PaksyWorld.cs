using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// Rasterwelt 27,5 mm. Keine WPF-Kamera übernehmen.
    /// </summary>
    public class PaksyWorld : MonoBehaviour
    {
        public int CellsX = 40;
        public int CellsZ = 40;
        public bool DrawGrid = true;
        public Color GridColor = new Color(0.3f, 0.35f, 0.4f, 0.6f);

        void OnDrawGizmos()
        {
            if (!DrawGrid) return;
            Gizmos.color = GridColor;
            float s = PaksyUnits.CellSize;
            float w = CellsX * s;
            float d = CellsZ * s;
            Vector3 origin = transform.position;

            for (int x = 0; x <= CellsX; x++)
            {
                var a = origin + new Vector3(x * s, 0f, 0f);
                var b = origin + new Vector3(x * s, 0f, d);
                Gizmos.DrawLine(a, b);
            }
            for (int z = 0; z <= CellsZ; z++)
            {
                var a = origin + new Vector3(0f, 0f, z * s);
                var b = origin + new Vector3(w, 0f, z * s);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
