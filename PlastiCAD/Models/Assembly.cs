using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Assembly
    {
        public List<PlacedPart> PlacedParts { get; set; } = new List<PlacedPart>();

        public List<Connection> Connections { get; set; } = new List<Connection>();
    }
}
