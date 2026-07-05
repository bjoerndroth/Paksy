using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public static class PartLibrary
    {
        public static List<Part> Parts = new List<Part>();

        public static void Initialize()
        {
            Parts.Clear();

            AddPipes();
            AddElbows();
            AddTees();
            AddCrosses();
            AddJoints();
            AddWheels();
            AddPlates();
        }

       
          private static void AddPipes()
        {
            Pipe pipe = new Pipe
            {
                Id = "P001",
                Name = "Rohr 27,5 mm",
                Description = "Standardrohr",

                Length = 27.5,
                OuterDiameter = 9.5,
                InnerDiameter = 7.0,
                SocketDepth = 10.5
            };

            pipe.Sockets.Add(new Socket
            {
                Index = 0,
                Name = "Links",
                Position = new Vector3(0, 0, 0),
                Direction = new Vector3(-1, 0, 0),
                Owner = pipe
            });

            pipe.Sockets.Add(new Socket
            {
                Index = 1,
                Name = "Rechts",
                Position = new Vector3(pipe.Length, 0, 0),
                Direction = new Vector3(1, 0, 0),
                Owner = pipe
            });
     // Hier kommen später weitere Rohre
       
            Parts.Add(pipe);
        }

        

        private static void AddElbows()
        {
        }

        private static void AddTees()
        {
        }

        private static void AddCrosses()
        {
        }

        private static void AddJoints()
        {
        }

        private static void AddWheels()
        {
        }

        private static void AddPlates()
        {
        }
    }
}
