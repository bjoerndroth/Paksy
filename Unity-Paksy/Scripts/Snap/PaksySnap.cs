using UnityEngine;

namespace Paksy
{
    public struct PaksySnapCandidate
    {
        public PaksyPart Other;
        public PaksyFace OwnFace;
        public PaksyFace OtherFace;
        public float Distance;
    }

    public static class PaksySnap
    {
        public static bool TryFind(
            PaksyPart moving,
            PaksyFace movingFace,
            PaksyPart[] worldParts,
            out PaksySnapCandidate best)
        {
            best = default;
            float max = PaksyUnits.SnapDistanceMm / PaksyUnits.MillimetersPerUnit;
            float bestDist = float.MaxValue;
            bool found = false;

            Vector3 pos = moving.WorldSocketPosition(movingFace);

            for (int i = 0; i < worldParts.Length; i++)
            {
                var other = worldParts[i];
                if (other == null || other == moving || other.ActiveFaces == null)
                    continue;

                for (int f = 0; f < other.ActiveFaces.Length; f++)
                {
                    var otherFace = other.ActiveFaces[f];
                    if (!PaksyFaces.AreComplementary(movingFace, otherFace))
                        continue;

                    float d = Vector3.Distance(pos, other.WorldSocketPosition(otherFace));
                    if (d <= max && d < bestDist)
                    {
                        bestDist = d;
                        best = new PaksySnapCandidate
                        {
                            Other = other,
                            OwnFace = movingFace,
                            OtherFace = otherFace,
                            Distance = d
                        };
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Verschiebt das bewegte Teil so, dass der eigene Socket auf dem Ziel-Socket liegt.
        /// </summary>
        public static void AlignToSocket(PaksyPart moving, PaksyFace movingFace, PaksyPart target, PaksyFace targetFace)
        {
            Vector3 from = moving.WorldSocketPosition(movingFace);
            Vector3 to = target.WorldSocketPosition(targetFace);
            moving.transform.position += to - from;
        }
    }
}
