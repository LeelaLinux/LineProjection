using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using OxyPlot;

namespace LineProjection
{
    public class Projection
    {
        public double Theta { get; set; }
        public double DX { get; set; }
        public double DY { get; set; }
        public Matrix<double> Matrix { get; set; }
        public List<Vector< double>> OriginalShape = new List<Vector<double>>();
        public List<Vector< double>> TransformedShape = new List<Vector<double>>();

        public Projection()
        {
            Initialize();
            Set();
        }

        private void Initialize()
        {
            OriginalShape.Add(v(-1.0, -2.0));
            OriginalShape.Add(v(-1.0, 2.0));
            OriginalShape.Add(v(1.0, 2.0));
            OriginalShape.Add(v(1.0, -2.0));
            OriginalShape.Add(v(-1.0, -2.0));

        }

        public void Set()
        {
            Matrix = m();
            Transform();
        }

        private Matrix<double> m()
        {
            return Matrix<double>.Build.DenseOfArray(new[,] {
                { Math.Cos(Theta), Math.Sin(Theta), DX},
                {-1.0*Math.Sin(Theta), Math.Cos(Theta), DY },
                {0.0, 0.0, 1.0 } });
        }

        public IEnumerable<DataPoint> OriginalPoints()
        {
            foreach (var v in OriginalShape)
            {
                yield return new DataPoint(v[0], v[1]);
            };
        }

        public IEnumerable<DataPoint> TransformedPoints()
        {
            foreach (var v in TransformedShape)
            {
                yield return new DataPoint(v[0], v[1]);
            };
        }

        private Vector<double> v (double x, double y)
        {
            return Vector<double>.Build.DenseOfArray(new[] { x, y, 1.0 });
        }

        private void Transform()
        {
            TransformedShape.Clear();
            OriginalShape.ForEach(v => TransformedShape.Add(Matrix.Multiply(v)));
        }
    }
}
