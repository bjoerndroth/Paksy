using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Elbow : StructuralPart
    {
        public double LegLength => Length / 2 + OuterDiameter / 2;
        public Elbow()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "E001";
            Name = "90° Winkel";
            Description = "90° Verbindungswinkel";

            Length = 27.5;
        }

        private void InitializeSockets()
        {

            // linker Anschluss
            Sockets.Add(new Socket
            {
                Index = 0,
                Name = "Horizontal",

                Position = new Vector3(0, LegLength, 0),

                Direction = new Vector3(-1, 0, 0),

                Owner = this
            });

            // senkrechter Anschluss
            Sockets.Add(new Socket
            {
                Index = 1,
                Name = "Vertikal",

                Position = new Vector3(LegLength, 0, 0),

                Direction = new Vector3(0, 1, 0),

                Owner = this
            });
        }
    }
}