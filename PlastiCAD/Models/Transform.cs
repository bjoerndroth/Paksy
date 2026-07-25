using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Transform
    {
        public Vector3 Position { get; set; } = new Vector3();

        public Vector3 Rotation { get; set; } = new Vector3();

        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        public void RotateX90()
        {
            Rotation.X = (Rotation.X + 90) % 360;
        }

        public void RotateY90()
        {
            Rotation.Y = (Rotation.Y + 90) % 360;
        }

        public void RotateZ90()
        {
            Rotation.Z = (Rotation.Z + 90) % 360;
        }
    }
}
