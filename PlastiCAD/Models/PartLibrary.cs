using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;


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
            AddSpaceCrosses();
            AddCorner();
            AddEdge();
            AddStand();
            AddJoints();
            AddWheels();
            AddBigWheels();
            AddEndCaps();
            AddWindows();
            AddCubes();
            AddBallConnector();
            AddBigPlates();
            AddPlates();
            AddHolePlates();
            AddSlatPlates();
        }
        
        private static void AddSlatPlates()
        {
            Parts.Add(new SlatPlate());
        }
        private static void AddHolePlates()
        {
            Parts.Add(new HolePlate());
        }
        private static void AddBigWheels()
        {
            Parts.Add(new BigWheel());
        }
        private static void AddBallConnector()
        {
            Parts.Add(new BallConnector());
        }
        private static void AddBigPlates()
        {
            Parts.Add(new BigPlate());
        }
        private static void AddWindows()
        {
            Parts.Add(new WindowPlate());
        }

        private static void AddCubes()
        {
            Parts.Add(new Cube());
        }

        private static void AddPipes()
        {
            Parts.Add(new Pipe());
        }
        private static void AddEndCaps()
        {
            Parts.Add(new EndCap());
        }

        private static void AddElbows()
        {
            Parts.Add(new Elbow());
        }

        private static void AddTees()
        {
            Parts.Add(new Tee());
        }

        private static void AddCrosses()
        {
            Parts.Add(new Cross());
        }
        private static void AddStand()
        {
            Parts.Add(new Stand());
        }
        private static void AddSpaceCrosses()
        {
            Parts.Add(new SpaceCross());
        }
        private static void AddEdge()
        {
            Parts.Add(new Edge());
        }
        private static void AddCorner()
        {
            Parts.Add(new Corner());
        }
       


        private static void AddJoints()
        {
        }

        private static void AddWheels()
        {
            Parts.Add(new Wheel());
        }

        private static void AddPlates()
        {
            Parts.Add(new Plate());
        }
        
    }
}
