using UnityEngine;

namespace Paksy
{
    public class PaksyPart : MonoBehaviour
    {
        public string PartName;
        public PaksyPartClass Class;
        public PaksyFace[] ActiveFaces;
        public int PlateOrientation;
        public Vector3 FileEuler90;

        public Vector3 CellCenterUnity => transform.position;

        public Vector3 WorldSocketPosition(PaksyFace face)
        {
            return transform.TransformPoint(PaksyFaces.LocalOffset(face));
        }

        public Vector3 WorldSocketDirection(PaksyFace face)
        {
            return transform.TransformDirection(PaksyFaces.LocalDirection(face));
        }

        public bool HasFace(PaksyFace face)
        {
            if (ActiveFaces == null) return false;
            for (int i = 0; i < ActiveFaces.Length; i++)
                if (ActiveFaces[i] == face) return true;
            return false;
        }
    }
}
