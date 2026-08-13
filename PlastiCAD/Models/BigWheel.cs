using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlastiCAD.Models
{
    public class BigWheel : Part
    {
        // ------------------------------------------------------------
        // ABMESSUNGEN
        // ------------------------------------------------------------

        public double OuterDiameter { get; set; }

        public double RimDiameter { get; set; }

        public double Width { get; set; }

        public double TireWidth { get; set; }

        // Mittlere Bohrung
        public double HoleDiameter { get; set; }

        public double BoreDepth { get; set; }


        // ------------------------------------------------------------
        // VIER ZUSÄTZLICHE LÖCHER
        // ------------------------------------------------------------

        public int SideHoleCount { get; set; }

        public double SideHoleDiameter { get; set; }

        public double SideHoleRadius { get; set; }


        // ------------------------------------------------------------
        // FELGE
        // ------------------------------------------------------------

        public double RimEdgeWidth { get; set; }

        // Sehr flacher mittlerer Felgenbereich
        public double RimBodyThickness { get; set; }


        // ------------------------------------------------------------
        // RUNDFUGEN
        // ------------------------------------------------------------

        public int GrooveCount { get; set; }

        public double GrooveWidth { get; set; }

        public double GrooveAngle { get; set; }

        public double GrooveInset { get; set; }


        public BigWheel()
        {
            Id = "BW001";

            Name = "Big Rad";

            Description =
                "Großes Paksy-Rad";

            // Gesamtrad
            OuterDiameter = 64.0;

            // Felge
            RimDiameter = 46.0;

            // Gesamtbreite Rad
            Width = 9.0;

            // Schwarzer Gummi nur 8 mm breit
            TireWidth = 8.0;


            // --------------------------------------------------------
            // MITTLERE BOHRUNG
            // --------------------------------------------------------

            HoleDiameter = 9.5;

            // Nabe / Bohrung geht über die komplette Radbreite
            BoreDepth = 9.0;


            // --------------------------------------------------------
            // VIER WEITERE LÖCHER
            //
            // Durchmesser wie beim kleinen Rad:
            // Wheel.HoleDiameter = 9.5
            // --------------------------------------------------------

            SideHoleCount = 4;

            SideHoleDiameter = 9.5;

            // Erstmal geschätzter Abstand vom Mittelpunkt.
            // Den können wir anhand der 3D-Darstellung exakt anpassen.
            SideHoleRadius = 15.0;


            // --------------------------------------------------------
            // FELGE
            // --------------------------------------------------------

            RimEdgeWidth = 2.0;

            // Felgenkörper zwischen Rand und Nabe sehr flach
            RimBodyThickness = 2.0;


            // --------------------------------------------------------
            // VIER RUNDFUGEN
            // --------------------------------------------------------

            GrooveCount = 4;

            GrooveWidth = 2.0;

            GrooveAngle = 60.0;

            // ca. 3 mm vor dem Felgenrand
            GrooveInset = 3.0;


            InitializeSockets();
        }


        private void InitializeSockets()
        {
            // Genau derselbe Anschluss wie beim kleinen Rad.
            Sockets.Add(
                new Socket
                {
                    Index = 0,

                    Name = "Radarm",

                    Face = Face.Right,

                    Position =
                        new Vector3(),

                    Direction =
                        new Vector3(
                            1,
                            0,
                            0),

                    Owner = this
                });
        }
    }
}