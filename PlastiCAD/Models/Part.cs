using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public class Part
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<Socket> Sockets { get; set; } = new List<Socket>();

        public double Length { get; set; }

        public double OuterDiameter { get; set; }

        public double InnerDiameter { get; set; }

        public double SocketDepth { get; set; }
    }
}