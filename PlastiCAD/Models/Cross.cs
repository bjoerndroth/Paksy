using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class Cross : StructuralPart
    {
        public Cross()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "X001";
            Name = "Kreuz";
            Description = "4-fach Verbinder";

            Length = 27.5;
        }

        private void InitializeSockets()
        {
            Sockets.Add(new Socket
            {
                Index = 0,
                Name = "Links",
                Face = Face.Left,
                Position = new Vector3(),
                Direction = new Vector3(-1, 0, 0),
                Owner = this
            });

            Sockets.Add(new Socket
            {
                Index = 1,
                Name = "Rechts",
                Face = Face.Right,
                Position = new Vector3(),
                Direction = new Vector3(1, 0, 0),
                Owner = this
            });

            Sockets.Add(new Socket
            {
                Index = 2,
                Name = "Oben",
                Face = Face.Top,
                Position = new Vector3(),
                Direction = new Vector3(0, -1, 0),
                Owner = this
            });

            Sockets.Add(new Socket
            {
                Index = 3,
                Name = "Unten",
                Face = Face.Bottom,
                Position = new Vector3(),
                Direction = new Vector3(0, 1, 0),
                Owner = this
            });
        }
    }
}