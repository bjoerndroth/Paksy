using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
    {
        public class Wheel : Part
        {

        // Hallo Chatty da bin ich

            public double OuterDiameter { get; set; }

            public double RimDiameter { get; set; }

            public double HoleDiameter { get; set; }

            public double Width { get; set; }

            public double BoreDepth { get; set; }

            public Wheel()
            {
                Id = "W001";
                Name = "Rad";
                Description = "Paksy-Rad";

                OuterDiameter = 32.0;
                RimDiameter = 23.0;
                HoleDiameter = 9.5;
                Width = 9.0;
                BoreDepth = 9.0;

                InitializeSockets();
            }

            private void InitializeSockets()
            {
                Sockets.Add(new Socket
                {
                    Index = 0,
                    Name = "Radarm",
                    Face = Face.Right,

                    Position = new Vector3(),
                    Direction = new Vector3(1, 0, 0),

                    Owner = this
                });
            }
        }
    
}
