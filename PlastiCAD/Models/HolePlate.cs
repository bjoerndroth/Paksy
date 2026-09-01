using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class HolePlate : Plate
    {
        public double HoleDiameter { get; set; }

        public HolePlate()
        {
            Id = "HP001";
            Name = "Lochplatte";
            Description = "Kleine rote Platte mit 9,6-mm-Durchgang";

            Width = 25.0;
            Height = 25.0;
            Thickness = 5;
            HoleDiameter = 9.6;
        }
    }
}