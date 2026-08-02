using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class Cube : SpaceCross
    {
        public double Size { get; set; }

        public double HoleDiameter { get; set; }

        public double HoleDepth { get; set; }

        public double CornerRadius { get; set; }

        public Cube()
        {
            Id = "CU001";
            Name = "Würfel";
            Description = "6-fach Würfelverbinder";

            Size = 27.5;
            HoleDiameter = 9.5;
            HoleDepth = 10.0;
            CornerRadius = 3.0;

            // Verhindert, dass die normale StructuralPart-Darstellung
            // eine zusätzliche Mittelkugel zeichnet.
           // DrawCenter = false;
        }
    }
}