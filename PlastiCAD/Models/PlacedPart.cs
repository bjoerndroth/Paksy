using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class PlacedPart
    {
        public Part Part { get; set; }

        public Transform Transform { get; set; } = new Transform();

        public List<Socket> Sockets { get; set; } = new List<Socket>();

        public int Rotation { get; set; }
    }
}
