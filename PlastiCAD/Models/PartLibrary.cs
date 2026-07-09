using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
            AddJoints();
            AddWheels();
            AddPlates();
        }


        private static void AddPipes()
        {
            Parts.Add(new Pipe());
        }


        private static void AddElbows()
        {
            Parts.Add(new Elbow());
        }

        private static void AddTees()
        {
        }

        private static void AddCrosses()
        {
        }

        private static void AddJoints()
        {
        }

        private static void AddWheels()
        {
        }

        private static void AddPlates()
        {
        }
    }
}
