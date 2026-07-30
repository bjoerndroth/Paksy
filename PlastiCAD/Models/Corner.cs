
using PlastiCAD.Models;

namespace PlastiCAD.Models
{
    public class Corner : StructuralPart
    {
        public Corner()
        {
            InitializeProperties();
            InitializeSockets();
        }

        private void InitializeProperties()
        {
            Id = "SC002";
            Name = "Corner";
            Description = "Ecke";

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
                Index = 2,
                Name = "Oben",
                Face = Face.Top,
                Position = new Vector3(),
                Direction = new Vector3(0, -1, 0),
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