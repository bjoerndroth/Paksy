using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Models
{
    public class Transform
    {
        public Vector3 Position { get; set; } = new Vector3();

        public Vector3 Rotation { get; set; } = new Vector3();

        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        public void RotateWorldX90()
        {
            ApplyWorldRotation(
                vector => vector.RotateX90());
        }

        public void RotateWorldY90()
        {
            ApplyWorldRotation(
                vector => vector.RotateY90());
        }

        public void RotateWorldZ90()
        {
            ApplyWorldRotation(
                vector => vector.RotateZ90());
        }
        private void ApplyWorldRotation(
    Func<Vector3, Vector3> worldRotation)
        {
            // Aktuelle Orientierung der drei lokalen Basisachsen
            Vector3 currentX =
                ApplyRotation(new Vector3(1, 0, 0));

            Vector3 currentY =
                ApplyRotation(new Vector3(0, 1, 0));

            Vector3 currentZ =
                ApplyRotation(new Vector3(0, 0, 1));

            // Diese Orientierung um die gewünschte Weltachse drehen
            Vector3 targetX =
                worldRotation(currentX);

            Vector3 targetY =
                worldRotation(currentY);

            Vector3 targetZ =
                worldRotation(currentZ);

            /*
             * Eine passende Kombination aus den möglichen
             * 90°-Euler-Drehungen suchen.
             */
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        Transform candidate =
                            new Transform
                            {
                                Rotation =
                                    new Vector3(
                                        x * 90,
                                        y * 90,
                                        z * 90)
                            };

                        Vector3 candidateX =
                            candidate.ApplyRotation(
                                new Vector3(1, 0, 0));

                        Vector3 candidateY =
                            candidate.ApplyRotation(
                                new Vector3(0, 1, 0));

                        Vector3 candidateZ =
                            candidate.ApplyRotation(
                                new Vector3(0, 0, 1));

                        if (SameDirection(candidateX, targetX) &&
                            SameDirection(candidateY, targetY) &&
                            SameDirection(candidateZ, targetZ))
                        {
                            Rotation.X = x * 90;
                            Rotation.Y = y * 90;
                            Rotation.Z = z * 90;

                            return;
                        }
                    }
                }
            }
        }

        private static bool SameDirection(
    Vector3 first,
    Vector3 second)
        {
            const double tolerance = 0.001;

            return
                Math.Abs(first.X - second.X) < tolerance &&
                Math.Abs(first.Y - second.Y) < tolerance &&
                Math.Abs(first.Z - second.Z) < tolerance;
        }
        public void RotateX90()
        {
            Rotation.X = (Rotation.X + 90) % 360;
        }

        public void RotateY90()
        {
            Rotation.Y = (Rotation.Y + 90) % 360;
        }

        public void RotateZ90()
        {
            Rotation.Z = (Rotation.Z + 90) % 360;
        }

        public Vector3 ApplyRotation(Vector3 vector)
        {
            Vector3 result = new Vector3(
                vector.X,
                vector.Y,
                vector.Z);

            int xSteps = ((int)Rotation.X / 90) % 4;
            int ySteps = ((int)Rotation.Y / 90) % 4;
            int zSteps = ((int)Rotation.Z / 90) % 4;

            for (int i = 0; i < xSteps; i++)
                result = result.RotateX90();

            for (int i = 0; i < ySteps; i++)
                result = result.RotateY90();

            for (int i = 0; i < zSteps; i++)
                result = result.RotateZ90();

            return result;
        }
    }
}
