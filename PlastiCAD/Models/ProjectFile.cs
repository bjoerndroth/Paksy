using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public class ProjectFile
    {
        public int Version { get; set; } = 1;

        public List<PlacedPartData> Parts { get; set; }
            = new List<PlacedPartData>();
    }

    public class PlacedPartData
    {
        public string PartName { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public int Rotation { get; set; }
    }
}
