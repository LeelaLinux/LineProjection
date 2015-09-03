using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace LineProjection
{
    public class Constraint
    {
        private Vector<double> _vector;
        private Vector<double> _unitVector;
        private double _length;

        public Vector<double> UnitVector { get; set; }
        public double Length { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Constraint(double x, double y)
        {
            _vector = Vector<double>.Build.DenseOfArray(new[] { x, y });
            X = x;
            Y = y;
            Length = _vector.L2Norm();
            UnitVector = _vector.Divide(Length);
        }

        public bool Violate(double x, double y)
        {
            var point = Vector<double>.Build.DenseOfArray(new[] { x, y });
            return point.DotProduct(UnitVector) > Length;
        }

        public bool Violate(Tuple<double, double> point)
        {
            return Violate(point.Item1, point.Item2);
        }
    }
}
