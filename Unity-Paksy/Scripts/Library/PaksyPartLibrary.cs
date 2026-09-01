using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paksy
{
    public enum PaksyPartClass
    {
        Pipe,
        Elbow,
        Tee,
        Cross,
        SpaceCross,
        Corner,
        Edge,
        Stand,
        Cube,
        BallConnector,
        Plate,
        BigPlate,
        WindowPlate,
        Wheel,
        BigWheel,
        EndCap,
        Unknown
    }

    [Serializable]
    public class PaksyPartDefinition
    {
        public string PartName;
        public PaksyPartClass Class;
        public PaksyFace[] Faces;
        public GameObject Prefab;
        public bool UsesHalfCellOffset;
    }

    [CreateAssetMenu(menuName = "Paksy/Part Library", fileName = "PaksyPartLibrary")]
    public class PaksyPartLibrary : ScriptableObject
    {
        public List<PaksyPartDefinition> Parts = new List<PaksyPartDefinition>();

        static readonly Dictionary<string, PaksyFace[]> DefaultFaces = new Dictionary<string, PaksyFace[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Rohr 27,5 mm", new[] { PaksyFace.Left, PaksyFace.Right } },
            { "90° Winkel", new[] { PaksyFace.Left, PaksyFace.Top } },
            { "T-Stück", new[] { PaksyFace.Left, PaksyFace.Right, PaksyFace.Top } },
            { "Kreuz", new[] { PaksyFace.Left, PaksyFace.Right, PaksyFace.Top, PaksyFace.Bottom } },
            { "Raumkreuz", new[] { PaksyFace.Left, PaksyFace.Right, PaksyFace.Top, PaksyFace.Bottom, PaksyFace.Front, PaksyFace.Back } },
            { "SpaceCross", new[] { PaksyFace.Left, PaksyFace.Right, PaksyFace.Top, PaksyFace.Bottom, PaksyFace.Front, PaksyFace.Back } },
            { "Endkappe", new[] { PaksyFace.Right } },
        };

        public PaksyPartDefinition Find(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return null;
            foreach (var p in Parts)
            {
                if (p != null && string.Equals(p.PartName, partName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        public PaksyFace[] FacesFor(string partName)
        {
            var def = Find(partName);
            if (def != null && def.Faces != null && def.Faces.Length > 0)
                return def.Faces;

            foreach (var kv in DefaultFaces)
            {
                if (partName.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return Array.Empty<PaksyFace>();
        }
    }
}
