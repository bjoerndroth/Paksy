
using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class SpaceCross : StructuralPart
    {
        public SpaceCross()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "SC001";
            Name = "SpaceCross";
            Description = "6-fach Verbinder";

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

            Sockets.Add(new Socket
            {
                Index = 4,
                Name = "Vorne",
                Face = Face.Front,
                Position = new Vector3(),
                Direction = new Vector3(0, 0, -1),
                Owner = this
            });

            Sockets.Add(new Socket
            {
                Index = 5,
                Name = "Hinten",
                Face = Face.Back,
                Position = new Vector3(),
                Direction = new Vector3(0, 0, 1),
                Owner = this
            });
        }
    }
}