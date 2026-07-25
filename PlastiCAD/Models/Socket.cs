using PlastiCAD.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Socket
    {

        public Face Face { get; set; }
        public int Index { get; set; }

        public string Name { get; set; }

        // Position
        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; }


        public bool IsConnected { get; set; }

        public Socket ConnectedTo { get; set; }

        public Part Owner { get; set; }

        public bool CanRotate { get; set; }

        public double CurrentAngle { get; set; }

        public double MinAngle { get; set; }

        public double MaxAngle { get; set; }

        public Vector3 GetRotatedDirection(Transform transform)
        {
            return transform.ApplyRotation(Direction);
        }

        public Vector3 GetRotatedPosition(Transform transform)
        {
            return transform.ApplyRotation(Position);
        }

        public Vector3 GetWorldPosition(Transform transform)
        {
            Vector3 rotatedPosition = GetRotatedPosition(transform);

            return new Vector3(
                transform.Position.X + rotatedPosition.X,
                transform.Position.Y + rotatedPosition.Y,
                transform.Position.Z + rotatedPosition.Z);
        }

        public Face GetRotatedFace(Transform transform)
        {
            return FaceHelper.RotateFace3D(
                Face,
                transform.Rotation);
        }
    }
}
