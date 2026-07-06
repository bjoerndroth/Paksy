using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlastiCAD.Models;

namespace PlastiCAD.Core
{
    public static class SnapEngine
    {
        public static Vector3 GetWorldSocketPosition(PlacedPart placed, Socket socket,
            double Scale)
        {
            return new Vector3(
                placed.Transform.Position.X + socket.Position.X * Scale,
                placed.Transform.Position.Y + socket.Position.Y * Scale,
                0);
        }
    }
}