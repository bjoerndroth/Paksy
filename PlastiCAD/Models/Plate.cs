using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Plate : Part
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }

        public Plate()
        {
            Id = "P001";
            Name = "Platte";
            Description = "Paksy-Platte";

            Width = 25.0;
            Height = 25.0;
            Thickness = 5.0;

            InitializeSockets();
        }

        private void InitializeSockets()
        {
        }
    }
}
