using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paksy
{
    [Serializable]
    public class PaksyProjectFile
    {
        public int Version = 1;
        public List<PaksyPlacedPart> Parts = new List<PaksyPlacedPart>();
    }

    [Serializable]
    public class PaksyPlacedPart
    {
        public string PartName;
        public float X;
        public float Y;
        public float Z;
        public int Rotation;
        public int PlateOrientation;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
    }

    public static class PaksyProjectJson
    {
        public static PaksyProjectFile Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Leeres Projekt-JSON.");

            var file = JsonUtility.FromJson<PaksyProjectFile>(json);
            if (file == null)
                throw new InvalidOperationException("JSON konnte nicht gelesen werden.");
            if (file.Parts == null)
                file.Parts = new List<PaksyPlacedPart>();
            return file;
        }
    }
}
