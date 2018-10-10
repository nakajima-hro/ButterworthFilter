using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filter
{
    /// <summary>
    /// フィルタ係数
    /// </summary>
    class FilterCoefficient
    {
        public double[,] Numerator;
        public double[,] Denominator;
        public int Sections;
        public int Order;
    }
}

