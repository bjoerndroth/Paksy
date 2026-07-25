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


        public static Face RotateFace3D(Face face, char axis)
        {
            switch (axis)
            {
                case 'X':
                    switch (face)
                    {
                        case Face.Top:
                            return Face.Front;
                        case Face.Front:
                            return Face.Bottom;
                        case Face.Bottom:
                            return Face.Back;
                        case Face.Back:
                            return Face.Top;
                        default:
                            return face;
                    }

                case 'Y':
                    switch (face)
                    {
                        case Face.Front:
                            return Face.Right;
                        case Face.Right:
                            return Face.Back;
                        case Face.Back:
                            return Face.Left;
                        case Face.Left:
                            return Face.Front;
                        default:
                            return face;
                    }

                case 'Z':
                    switch (face)
                    {
                        case Face.Left:
                            return Face.Top;
                        case Face.Top:
                            return Face.Right;
                        case Face.Right:
                            return Face.Bottom;
                        case Face.Bottom:
                            return Face.Left;
                        default:
                            return face;
                    }

                default:
                    return face;
            }
        }

        public static Face ApplyRotation3D(Face face, Vector3 rotation)
        {
            Face result = face;

            int xSteps = ((int)rotation.X / 90) % 4;
            int ySteps = ((int)rotation.Y / 90) % 4;
            int zSteps = ((int)rotation.Z / 90) % 4;

            for (int i = 0; i < xSteps; i++)
                result = RotateFace3D(result, 'X');

            for (int i = 0; i < ySteps; i++)
                result = RotateFace3D(result, 'Y');

            for (int i = 0; i < zSteps; i++)
                result = RotateFace3D(result, 'Z');

            return result;
        }


    }
}