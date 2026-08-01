using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class WindowPlate : Plate
    {
        public double CenterBarWidth { get; set; }

        public WindowPlate()
        {
            Id = "WP001";
            Name = "Fenster";
            Description = "Transparente Paksy-Plexiglasscheibe";

            Width = 25.0;
            Height = 25.0;
            Thickness = 5.0;

            CenterBarWidth = 1.0;
        }
    }
}