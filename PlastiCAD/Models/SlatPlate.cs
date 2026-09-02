using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class SlatPlate : Plate
    {
        public double OuterSlatWidth { get; set; }
        public double InnerSlatWidth { get; set; }
        public double GapWidth { get; set; }
        public double GutterDiameter { get; set; } = 10.0;
        public SlatPlate()
        {
            Id = "SP001";
            Name = "Streifenplatte";
            Description = "Gelbe 1-mm-Streifenplatte mit 4 Lamellen";

            Width = 16.0;
            Height = 25.0;
            Thickness = 1.0;

            OuterSlatWidth = 2.5;
            InnerSlatWidth = 3;
            GapWidth = 2.5;

        }

        public double[] GetSlatWidths()
        {
            return new[]
            {
                OuterSlatWidth,
                InnerSlatWidth,
                InnerSlatWidth,
                OuterSlatWidth
            };
        }
    }
}