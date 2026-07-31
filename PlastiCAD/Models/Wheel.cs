using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public class Wheel : Part
    {
        public Wheel()
        {
            Id = "W001";
            Name = "Rad";
            Description = "Paksy-Rad";

            OuterDiameter = 32.0;
            RimDiameter = 23.0;
            HoleDiameter = 9.5;
            Width = 9.0;
        }

        public double OuterDiameter { get; set; }

        public double RimDiameter { get; set; }

        public double HoleDiameter { get; set; }

        public double Width { get; set; }

        public override List<Socket> CreateSockets()
        {
            return new List<Socket>();
        }
    }
}