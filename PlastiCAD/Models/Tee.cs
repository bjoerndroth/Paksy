using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class Tee : StructuralPart
    {
        public double LegLength => Length / 2;

        public Tee()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "T001";
            Name = "T-Stück";
            Description = "3-fach Verbinder";

            Length = 27.5;
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

            Sockets.Add(new Socket
            {
                Index = 2,
                Name = "Oben",
                Face = Face.Top,
                Position = new Vector3(Length / 2, -Length / 2, 0),
                Direction = new Vector3(0, -1, 0),
                Owner = this
            });
        }
    }

}