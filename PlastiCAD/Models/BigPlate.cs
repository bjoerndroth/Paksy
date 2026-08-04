using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class BigPlate : Plate
    {
        public double OuterSize { get; set; }

        public double InnerSize { get; set; }

        public double PlateThickness { get; set; }

        public double TotalThickness { get; set; }

        public double RibLength { get; set; }

        public double RibHeight { get; set; }

        public double RibThickness { get; set; }

        // Freier Abstand zwischen den beiden Stegen
        public double RibClearDistance { get; set; }

        public BigPlate()
        {
            Id = "BP001";
            Name = "Große Platte";
            Description = "Große Paksy-Doppelplatte";

            OuterSize = 28.0;
            InnerSize = 20.0;

            PlateThickness = 1.0;
            TotalThickness = 10.0;

            RibLength = 15.0;
            RibHeight = 8.0;
            RibThickness = 1.0;

            RibClearDistance = 15.0;

            // Für bestehende Plate-Funktionen
            Width = OuterSize;
            Height = OuterSize;
            Thickness = TotalThickness;
        }
    }
}