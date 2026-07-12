using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlastiCAD.Models;

namespace PlastiCAD.Core
{
    public static class FaceHelper
    {
        public static Face RotateFace(Face face, int rotation)
        {
            int steps = ((rotation % 360) + 360) % 360 / 90;

            for (int i = 0; i < steps; i++)
            {
                switch (face)
                {
                    case Face.Left:
                        face = Face.Top;
                        break;

                    case Face.Top:
                        face = Face.Right;
                        break;

                    case Face.Right:
                        face = Face.Bottom;
                        break;

                    case Face.Bottom:
                        face = Face.Left;
                        break;
                }
            }

            return face;
        }
    }
}