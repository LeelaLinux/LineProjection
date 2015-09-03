using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace LineProjection
{
    public class OneForm
    {
        public Vector<double> Vector { get; set; }
        public Vector<double> UnitVector { get; set; }
        public double Length { get; set; }
        public double X;
        public double Y;

        public OneForm(double x, double y)
        {
            X = x;
            Y = y;
            Vector = Vector<double>.Build.DenseOfArray(new[] { x, y });
            Length = Vector.L2Norm();
            UnitVector = Vector<double>.Build.DenseOfArray(new[] { x / Length, y / Length});
        }
    }
}
