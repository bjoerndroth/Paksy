using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public abstract class Part
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public List<Socket> Sockets { get; set; } = new List<Socket>();

        public double Length { get; set; }

        public double OuterDiameter { get; set; }

        public double InnerDiameter { get; set; }

        public double SocketDepth { get; set; }

    
        public virtual List<Socket> CreateSockets()
        {
            List<Socket> sockets = new List<Socket>();

            foreach (Socket socket in Sockets)
            {
                sockets.Add(new Socket
                {
                    Index = socket.Index,
                    Name = socket.Name,

                    Position = new Vector3(
                        socket.Position.X,
                        socket.Position.Y,
                        socket.Position.Z),

                    Direction = new Vector3(
                        socket.Direction.X,
                        socket.Direction.Y,
                        socket.Direction.Z)
                });
            }

            return sockets;
        }
    }
}