using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Vector3
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public Vector3()
        {
        }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public double DistanceTo(Vector3 other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }


    }
}