using System.Collections.Generic;
using UnityEngine;

namespace Paksy
{
    /// <summary>
    /// Lädt ein PlastiCAD-ProjectFile (JSON Version 1) und spawnt Prefabs.
    /// Pivot-Konvention: Prefabs sind auf die Zellenmitte geeicht.
    /// </summary>
    public class PaksyJsonImporter : MonoBehaviour
    {
        public PaksyPartLibrary Library;
        public Transform Root;
        public TextAsset ProjectJson;
        public bool PositionIsCellCorner = true;
        public bool ApplyPlateHalfCellOffset = true;
        public bool ClearRootBeforeImport = true;

        public List<PaksyPart> LastImported { get; private set; } = new List<PaksyPart>();

        [ContextMenu("Import Project JSON")]
        public void ImportFromAssignedAsset()
        {
            if (ProjectJson == null)
            {
                Debug.LogError("Kein ProjectJson zugewiesen.");
                return;
            }
            Import(ProjectJson.text);
        }

        public List<PaksyPart> Import(string json)
        {
            if (Root == null) Root = transform;
            if (ClearRootBeforeImport)
            {
                for (int i = Root.childCount - 1; i >= 0; i--)
                    DestroyImmediate(Root.GetChild(i).gameObject);
            }

            LastImported.Clear();
            var file = PaksyProjectJson.Parse(json);
            if (file.Version != 1)
                Debug.LogWarning($"Unbekannte Projektversion {file.Version}, Import wird trotzdem versucht.");

            foreach (var placed in file.Parts)
                LastImported.Add(Spawn(placed));

            return LastImported;
        }

        public PaksyPart Spawn(PaksyPlacedPart placed)
        {
            var def = Library != null ? Library.Find(placed.PartName) : null;
            GameObject go;
            if (def != null && def.Prefab != null)
                go = Instantiate(def.Prefab, Root);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(Root, false);
                go.transform.localScale = Vector3.one * (PaksyUnits.TubeOuterDiameterMm);
                Debug.LogWarning($"Kein Prefab für '{placed.PartName}', Platzhalter erzeugt.");
            }

            go.name = placed.PartName ?? "Teil";

            Vector3 pos = PaksyCoords.ImportPositionUnity(placed.X, placed.Y, placed.Z, PositionIsCellCorner);
            if (ApplyPlateHalfCellOffset && def != null && def.UsesHalfCellOffset)
                pos += new Vector3(PaksyUnits.HalfCell, 0f, 0f);

            go.transform.position = pos;
            go.transform.rotation = BuildRotation(placed);

            var part = go.GetComponent<PaksyPart>();
            if (part == null) part = go.AddComponent<PaksyPart>();
            part.PartName = placed.PartName;
            part.Class = def != null ? def.Class : PaksyPartClass.Unknown;
            part.ActiveFaces = Library != null ? Library.FacesFor(placed.PartName) : null;
            part.PlateOrientation = placed.PlateOrientation;
            part.FileEuler90 = new Vector3(placed.RotationX, placed.RotationY, placed.RotationZ);
            return part;
        }

        static Quaternion BuildRotation(PaksyPlacedPart placed)
        {
            float rx = placed.RotationX;
            float ry = placed.RotationY;
            float rz = placed.RotationZ;
            if (Mathf.Approximately(rx, 0f) && Mathf.Approximately(ry, 0f) && Mathf.Approximately(rz, 0f) && placed.Rotation != 0)
                rz = PaksyRotation.LegacyRotationToZ(placed.Rotation);

            Quaternion q = PaksyRotation.FromEuler90(rx, ry, rz);
            q *= PlateOrientation(placed.PlateOrientation);
            return q;
        }

        /// <summary>0 = Fläche in XY, 1 = XZ, 2 = YZ — plus Weltrotation.</summary>
        static Quaternion PlateOrientation(int orientation)
        {
            switch (orientation)
            {
                case 1: return Quaternion.Euler(90f, 0f, 0f);
                case 2: return Quaternion.Euler(0f, 0f, 90f);
                default: return Quaternion.identity;
            }
        }
    }
}
