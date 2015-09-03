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
        private Vector<double> _theVector = Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0 });
        private Matrix<double> _transform = Matrix<double>.Build.DenseOfArray(new[,] { { 0.0, 0.0 }, { 0.0, 0.0 } });
        public Projection Projection { get; set; }

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
            var test = Lines(XOneForm);

        }

        /// <summary>
        /// Returns lines associated with the supplied 1-form
        /// </summary>
        /// <param name="oneForm"></param>
        /// <returns></returns>
        public IEnumerable<LineSeries> Lines(OneForm oneForm)
        {
            var point = new Tuple<double, double>(0, 0);
            while(EntersWindow(point, oneForm))
            {
                var crossings = WindowCrossings(point, oneForm);
                var lineSeries = new LineSeries();
                crossings.ForEach(c => lineSeries.Points.Add(new DataPoint(c.Item1, c.Item2)));
                yield return lineSeries;
                point = Increment(point, oneForm);
            }
            point = new Tuple<double, double>(0, 0);
            while (EntersWindow(point, oneForm))
            {
                var crossings = WindowCrossings(point, oneForm);
                var lineSeries = new LineSeries();
                crossings.ForEach(c => lineSeries.Points.Add(new DataPoint(c.Item1, c.Item2)));
                yield return lineSeries;
                point = Decrement(point, oneForm);
            }
        }

        private Tuple<double, double> Decrement(Tuple<double, double> point, OneForm xOneForm)
        {
            return new Tuple<double, double>(point.Item1 - xOneForm.UnitVector[0], point.Item2 - xOneForm.UnitVector[1]);
        }

        private Tuple<double, double> Increment(Tuple<double, double> point, OneForm xOneForm)
        {
            return new Tuple<double, double>(point.Item1 + xOneForm.UnitVector[0], point.Item2 + xOneForm.UnitVector[1]);
        }

        private List<Tuple<double, double>> WindowCrossings(Tuple<double, double> point, OneForm xOneForm)
        {
            var allCrossings = new List<Tuple<double, double>>();
            var windowEnteringCrossings = new List<Tuple<double, double>>();
            foreach (var constraint in Constraints)
            {
                allCrossings.Add(Intersection(point, constraint));
            }
            foreach(var pt in allCrossings)
            {
                if (Constraints.Any(c => c.Violate(pt))) continue;
                windowEnteringCrossings.Add(pt);
            }
            return windowEnteringCrossings;
        }

        private Tuple<double, double> Intersection(Tuple<double, double> point, Constraint constraint)
        {
            // intersection betwen the line normal to a vector from the origin to point and the constraint
            var lineLength = Math.Sqrt(point.Item1 * point.Item1 + point.Item2 * point.Item2);
            var y = point.Item1 * constraint.Length * constraint.Length - constraint.X * lineLength * lineLength;
            y /= (point.Item1 * constraint.Y - point.Item2 * constraint.X);
            var x = (lineLength - point.Item2 * y ) / point.Item1;
            return new Tuple<double, double>(x, y);
        }

        private bool EntersWindow(Tuple<double, double> point, OneForm xOneForm)
        {
            var entersWindow = false;
            foreach (var constraint in Constraints)
            {
                var intersection = Intersection(point, constraint);
                 bool outside = false;
                foreach(var c in Constraints)
                {
                    if (c.Violate(intersection))
                        outside = true;
                }
                if (outside == false) entersWindow = true;
            }

            //Constraints.ForEach(constraint =>
            //{
                
            //    if (Constraints.All(c => !c.Violate(intersection)))
            //        entersWindow = true;
            //});
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

        public Vector<double> TheVector
        {
            get { return _theVector; }
            set
            {
                _theVector = value;
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
