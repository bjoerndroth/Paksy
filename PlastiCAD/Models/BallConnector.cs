using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class BallConnector : SpaceCross
    {
        public double Diameter { get; set; }

        public double HoleDiameter { get; set; }

        public double HoleDepth { get; set; }

        public BallConnector()
        {
            Id = "BC001";
            Name = "Kugel";
            Description = "6-fach Kugelverbinder";

            Diameter = 27.5;
            HoleDiameter = 9.5;
            HoleDepth = 10.0;

            // Keine zusätzlichen Arme oder Mittelkugel
            // durch die normale StructuralPart-Darstellung.
            
        }
    }
}