using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public abstract class StructuralPart : Part
    {
        public double Length { get; set; }

        public double OuterDiameter { get; protected set; } = 9.5;

        public double InnerDiameter { get; protected set; } = 7.0;

        public double SocketDepth { get; protected set; } = 10.5;
    }
}