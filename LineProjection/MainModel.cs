using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using OxyPlot;
using OxyPlot.Series;


namespace LineProjection
{
    public class MainModel
    {
        private OneForm _xOneForm = new OneForm(0.0, 0.0);
        private OneForm _yOneForm = new OneForm(0.0, 0.0);
        private Vector<double> _initialVector = Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0 });
        private Vector<double> _finalVector = Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0 });
        private Matrix<double> _transform = Matrix<double>.Build.DenseOfArray(new[,] { { 0.0, 0.0 }, { 0.0, 0.0 } });
        public Projection Projection { get; set; }
        private Tuple<double, double> _zero = new  Tuple<double, double>(0.0, 0.0);

        public int Min { get; set; }
        public int Max { get; set; }

        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
        public double E { get; set; }
        public double F { get; set; }
        public double G { get; set; }
        public double H { get; set; }
        public double I { get; set; }
        public double Theta { get { return Projection.Theta; } set { Projection.Theta = value; Projection.Set(); } }
        public double DX { get { return Projection.DX; } set { Projection.DX = value; Projection.Set(); } }
        public double DY { get { return Projection.DY; } set { Projection.DY = value; Projection.Set(); } }

        public List<Constraint> Constraints = new List<Constraint>();

        public MainModel()
        {
            Min = -5;
            Max = 5;
            InitializeConstraints();
            Projection = new Projection();
        }


        private void InitializeConstraints()
        {
            Constraints.Add(new Constraint(Min, 0));
            Constraints.Add(new Constraint(Max, 0));
            Constraints.Add(new Constraint(0, Min));
            Constraints.Add(new Constraint(0, Max));
        }

        private void UpdateTransform()
        {
            Transform = Matrix<double>.Build.DenseOfRowVectors(_xOneForm.Vector, _yOneForm.Vector);
            FinalVector = Transform.Multiply(InitialVector);
        }

        /// <summary>
        /// Returns lines associated with the supplied 1-form
        /// </summary>
        /// <param name="oneForm"></param>
        /// <returns></returns>
        public IEnumerable<LineSeries> Lines(OneForm oneForm)
        {
            yield return WindowCrossings(oneForm, 0);
            var length = 1;
            while(EntersWindow(oneForm, length))
            {
                yield return WindowCrossings(oneForm, length++);
            }
            length = -1;
            while (EntersWindow(oneForm, length))
            {
                yield return WindowCrossings(oneForm, length--);
            }
        }

        public IEnumerable<LineSeries> TransformedCoordinates()
        {
            var line1 = new LineSeries() { StrokeThickness = 8, Color = OxyColors.Red};
            line1.Points.Add(new DataPoint(0, 0));
            var y = (InitialVector[0] * XOneForm.X + InitialVector[1] * XOneForm.Y);
            y /= (-YOneForm.Y * XOneForm.X / YOneForm.X + XOneForm.Y);
            var x = - y * YOneForm.Y / YOneForm.X;
            line1.Points.Add(new DataPoint(x, y));
            yield return line1;
            var line2 = new LineSeries() { StrokeThickness = 8, Color = OxyColors.Red };
            line2.Points.Clear();
            line2.Points.Add(new DataPoint(0, 0));
            y = (InitialVector[0] * YOneForm.X + InitialVector[1] * YOneForm.Y);
            y /= (-XOneForm.Y * YOneForm.X / XOneForm.X + YOneForm.Y);
            x = - y * XOneForm.Y / XOneForm.X;
            line2.Points.Add(new DataPoint(x, y));
            yield return line2;
        }

        public IEnumerable<LineSeries> VectorCoordinates()
        {
            var line1 = new LineSeries() { StrokeThickness = 8, Color = OxyColors.Red };
            line1.Points.Add(new DataPoint(0, 0));
            line1.Points.Add(new DataPoint(FinalVector[0], 0));
            yield return line1;
            var line2 = new LineSeries() { StrokeThickness = 8, Color = OxyColors.Red };
            line2.Points.Clear();
            line2.Points.Add(new DataPoint(0, 0));
            line2.Points.Add(new DataPoint(0, FinalVector[1]));
            yield return line2;
        }

        private Tuple<double, double> Decrement(OneForm xOneForm, Tuple<double, double> point)
        {
            return new Tuple<double, double>(point.Item1 - xOneForm.UnitVector[0], point.Item2 - xOneForm.UnitVector[1]);
        }

        private Tuple<double, double> Increment(OneForm xOneForm, Tuple<double, double> point)
        {
            return new Tuple<double, double>(point.Item1 + xOneForm.UnitVector[0], point.Item2 + xOneForm.UnitVector[1]);
        }

        private LineSeries WindowCrossings(OneForm xOneForm, int length)
        {
            var allCrossings = new List<Tuple<double, double>>();
            var windowEnteringCrossings = new List<Tuple<double, double>>();
            foreach (var constraint in Constraints)
            {
                allCrossings.Add(Intersection(xOneForm, length, constraint));
            }
            foreach(var pt in allCrossings)
            {
                if (Constraints.Any(c => c.Violate(pt))) continue;
                windowEnteringCrossings.Add(pt);
            }
            var lineSeries = new LineSeries()
            {
                LineStyle = (length == 0 ? LineStyle.Solid : LineStyle.Dash),
                Color = (length == 0 ? OxyColors.Black : OxyColors.Gray),
                StrokeThickness = (length == 0 ? 2 : 0.5)
            };
            windowEnteringCrossings.ForEach(c => lineSeries.Points.Add(new DataPoint(c.Item1, c.Item2)));
            return lineSeries;
        }

        private Tuple<double, double> Intersection(OneForm o, int z, Constraint c)
        {
            // intersection betwen the line normal to a vector from the origin to point and the constraint
            var y = o.X * c.Length * c.Length - c.X * z;
            y /= (o.X * c.Y - c.X * o.Y);
            var x = (z - o.Y * y ) / o.X;
            return new Tuple<double, double>(x, y);
        }

        private bool EntersWindow(OneForm xOneForm, int length)
        {
            var entersWindow = false;
            foreach (var constraint in Constraints)
            {
                var intersection = Intersection(xOneForm, length, constraint);
                bool outside = false;
                foreach(var c in Constraints)
                {
                    if (c.Violate(intersection))
                        outside = true;
                }
                if (outside == false) entersWindow = true;
            }
            return entersWindow;
        }

        public OneForm XOneForm
        {
            get { return _xOneForm; }
            set
            {
                _xOneForm = value;
                UpdateTransform();
            }
        }

        public OneForm YOneForm
        {
            get { return _yOneForm; }
            set
            {
                _yOneForm = value;
                UpdateTransform();
            }
        }

        public Vector<double> InitialVector
        {
            get { return _initialVector; }
            set
            {
                _initialVector = value;
                UpdateTransform();
            }
        }

        public Vector<double> FinalVector
        {
            get { return _finalVector; }
            set
            {
                _finalVector = value;
            }
        }

        public Matrix<double> Transform
        {
            get { return _transform; }
            set
            {
                _transform = value;
            }
        }
    }
}
