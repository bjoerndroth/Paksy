using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PlastiCAD.Models;

namespace PlastiCAD.Core
{
    public class SnapResult
    {
        public Socket MovingSocket { get; set; }

        public Socket OtherSocket { get; set; }

        public PlacedPart OtherPart { get; set; }

        public double Distance { get; set; }
    }
}