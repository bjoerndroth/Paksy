using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Socket
    {
        public int Index { get; set; }

        public string Name { get; set; }

        // Position
        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; }


        public bool IsConnected { get; set; }

        public Socket ConnectedTo { get; set; }

        public Part Owner { get; set; }

        public bool CanRotate { get; set; }

        public double CurrentAngle { get; set; }

        public double MinAngle { get; set; }

        public double MaxAngle { get; set; }

    }
}
