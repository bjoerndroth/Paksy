
using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class Edge : StructuralPart
    {
        public Edge()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "SC003";
            Name = "Edge";
            Description = "Kante";

            Length = 27.5;
        }

        private void InitializeSockets()
        {

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

        }
    }
}