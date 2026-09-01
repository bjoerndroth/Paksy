using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlastiCAD.Core
{
    public static class Grider
    {
        public const double CellSize = 27.5;

        public static bool UseHalfGrid { get; set; } = false;

        public static double StepSize =>
            UseHalfGrid ? CellSize / 2.0 : CellSize;
    }
}