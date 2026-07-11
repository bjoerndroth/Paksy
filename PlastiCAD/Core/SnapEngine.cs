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


        public static double GetSocketDistance(
    PlacedPart movingPart,
    Socket movingSocket,
    PlacedPart otherPart,
    Socket otherSocket,
    double scale)
        {
            Vector3 movingPos = GetWorldSocketPosition(
                movingPart,
                movingSocket,
                scale);

            Vector3 otherPos = GetWorldSocketPosition(
                otherPart,
                otherSocket,
                scale);

            return movingPos.DistanceTo(otherPos);
        }

        public static SnapResult FindBestSnap(    Assembly assembly,    PlacedPart movingPart,    double scale,    double snapDistance)
        {
            Socket bestMovingSocket = null;
            Socket bestOtherSocket = null;

            PlacedPart bestOtherPart = null;

            double bestDistance = double.MaxValue;

            foreach (PlacedPart otherPart in assembly.PlacedParts)
            {
                // Sich selbst überspringen
                if (otherPart == movingPart)
                    continue;

                foreach (Socket movingSocket in movingPart.Sockets)
                {
                    foreach (Socket otherSocket in otherPart.Sockets)
                    {

                        if (!FacesMatch(movingSocket.Face, otherSocket.Face))
                            continue;

                        double distance = GetSocketDistance(
                            movingPart,
                            movingSocket,
                            otherPart,
                            otherSocket,
                            scale);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;

                            bestMovingSocket = movingSocket;
                            bestOtherSocket = otherSocket;
                            bestOtherPart = otherPart;
                        }
                    }
                }



            }

            if (bestDistance < snapDistance)
            {
                return new SnapResult
                {
                    MovingSocket = bestMovingSocket,
                    OtherSocket = bestOtherSocket,
                    OtherPart = bestOtherPart,
                    Distance = bestDistance
                };
            }

            return null;

        }
        public static void ApplySnap(    PlacedPart movingPart,    SnapResult snap,    double scale)
        {
            Vector3 movingPos = GetWorldSocketPosition(
                movingPart,
                snap.MovingSocket,
                scale);

            Vector3 otherPos = GetWorldSocketPosition(
                snap.OtherPart,
                snap.OtherSocket,
                scale);

            movingPart.Transform.Position.X += otherPos.X - movingPos.X;
            movingPart.Transform.Position.Y += otherPos.Y - movingPos.Y;
        }

     
        private static bool FacesMatch(Face a, Face b)
        {
            return
                (a == Face.Left && b == Face.Right) ||
                (a == Face.Right && b == Face.Left) ||
                (a == Face.Top && b == Face.Bottom) ||
                (a == Face.Bottom && b == Face.Top) ||
                (a == Face.Front && b == Face.Back) ||
                (a == Face.Back && b == Face.Front);
        }

        public static Vector3 GetSocketWorldPosition(
    PlacedPart placed,
    Socket socket,
    double scale)
        {
            double centerX = placed.Transform.Position.X + Grider.CellSize * scale / 2;
            double centerY = placed.Transform.Position.Y + Grider.CellSize * scale / 2;

            double r = Grider.CellSize * scale / 2;

            switch (socket.Face)
            {
                case Face.Left:
                    return new Vector3(centerX - r, centerY, 0);

                case Face.Right:
                    return new Vector3(centerX + r, centerY, 0);

                case Face.Top:
                    return new Vector3(centerX, centerY - r, 0);

                case Face.Bottom:
                    return new Vector3(centerX, centerY + r, 0);

                default:
                    return new Vector3(centerX, centerY, 0);
            }
        }
        public static Vector3 GetWorldSocketPositionByFace(
    PlacedPart placed,
    Socket socket,
    double scale)
        {
            double centerX = placed.Transform.Position.X + Grider.CellSize * scale / 2;
            double centerY = placed.Transform.Position.Y + Grider.CellSize * scale / 2;

            double r = Grider.CellSize * scale / 2;

            switch (socket.Face)
            {
                case Face.Left:
                    return new Vector3(centerX - r, centerY, 0);

                case Face.Right:
                    return new Vector3(centerX + r, centerY, 0);

                case Face.Top:
                    return new Vector3(centerX, centerY - r, 0);

                case Face.Bottom:
                    return new Vector3(centerX, centerY + r, 0);

                case Face.Front:
                case Face.Back:
                default:
                    return new Vector3(centerX, centerY, 0);
            }
        }
    }

}