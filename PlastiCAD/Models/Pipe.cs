using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Pipe : StructuralPart
    {
        public Pipe()
        {
            InitializeProperties();

            InitializeSockets();
        }
        private void InitializeSockets()
        {
            Sockets.Add(new Socket
            {
                Index = 0,
                Name = "Links",
                Face = Face.Left,
                Position = new Vector3(0, 0, 0),
                Direction = new Vector3(-1, 0, 0),
                Owner = this
            });

            Sockets.Add(new Socket
            {
                Index = 1,
                Name = "Rechts",
                Face = Face.Right,
                Position = new Vector3(Length, 0, 0),
                Direction = new Vector3(1, 0, 0),
                Owner = this
            });
        }
        private void InitializeProperties()
        {
            Id = "P001";
            Name = "Rohr 27,5 mm";
            Description = "Standardrohr";

            Length = 27.5;
        }
    }
}