using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace LineProjection.ViewModels
{
    public class MainViewModel
    {
        // todo -- add bold x, y lines to show coordinates of vector
        // redraw lines when mouse is lifted
        // scale graphs so they stay square
        // add lines to control and vector plots
        public event PropertyChangedEventHandler PropertyChanged;

        private PlotModel _controlPlot;
        private PlotModel _coordinateTransform;
        private PlotModel _vectorTransform;
        private PlotModel _rotationPlot;
        private MainModel _model = new MainModel();

        private bool changeXOneForm = false;
        private bool changeYOneForm = false;
        private bool changeV = false;
        private bool moving = false;

        public MainViewModel()
        {
            InitializeVectors();
            InitializePlots();
        }

        private void InitializePlots()
        {
            ControlPlot = PlotControl();
            CoordinateTransform = PlotCoordinate();
            VectorTransform = PlotVector();
            RotationPlot = PlotRotation();
        }

        private PlotModel PlotControl()
        {
            var pm = new PlotModel() {Title = "ControlPlot"};
            var x1F = XOneFormSeries();
            var y1F = YOneFormSeries();
            var v = VectorSeries();

            pm.MouseDown += (s, e) =>
            {
                var xHit = (x1F.Transform(x1F.Points[1]) - e.Position).Length;
                var yHit = (y1F.Transform(y1F.Points[1]) - e.Position).Length;
                var vHit = (v.Transform(v.Points[1]) - e.Position).Length;
                var hitList = new List<double>(new [] { xHit, yHit, vHit });

                moving = true;

                if (hitList.Min() == xHit) changeXOneForm = true;
                if (hitList.Min() == yHit) changeYOneForm = true;
                if (hitList.Min() == vHit) changeV = true;

                if (changeXOneForm) x1F.LineStyle = LineStyle.DashDot;
                if (changeYOneForm) y1F.LineStyle = LineStyle.DashDot;
                if (changeV) v.LineStyle = LineStyle.DashDot;

                pm.InvalidatePlot(false);
                e.Handled = true;
            };

            pm.MouseMove += (s, e) =>
            {
                if (!moving) e.Handled = true;
                if (changeXOneForm)
                {
                    var newX = x1F.InverseTransform(e.Position);
                    XOneForm = new OneForm(newX.X, newX.Y );
                    x1F.Points[1] = new DataPoint(newX.X, newX.Y);
                }

                if (changeYOneForm)
                {
                    var newY = y1F.InverseTransform(e.Position);
                    YOneForm = new OneForm(newY.X, newY.Y );
                    y1F.Points[1] = new DataPoint(newY.X, newY.Y);
                }

                if (changeV)
                {
                    var newV = v.InverseTransform(e.Position);
                    TheVector = Vector<double>.Build.DenseOfArray(new[] { newV.X, newV.Y });
                    v.Points[1] = new DataPoint(TheVector[0], TheVector[1]);
                }
                UpdateVectorTransform();
                UpdateCoordinateTransform();
                pm.InvalidatePlot(true);
                e.Handled = true;
            };

            pm.MouseUp += (s, e) =>
            {
                moving = false;
                x1F.LineStyle = y1F.LineStyle = v.LineStyle = LineStyle.Solid;
                changeXOneForm = changeYOneForm = changeV = false;
                pm.InvalidatePlot(true);
                e.Handled = true;
            };
            pm.Series.Add(XAxis());
            pm.Series.Add(YAxis());
            pm.Series.Add(x1F);
            pm.Series.Add(y1F);
            pm.Series.Add(v);
            pm.Axes.Add(X());
            pm.Axes.Add(Y());

            return pm;
        }

        private void UpdateCoordinateTransform()
        {
            CoordinateTransform.Series.Clear();
            CoordinateTransform.Series.Add(XOneFormSeries());
            CoordinateTransform.Series.Add(YOneFormSeries());
            foreach (var line in _model.Lines(XOneForm)) CoordinateTransform.Series.Add(line);
            foreach (var line in _model.Lines(YOneForm)) CoordinateTransform.Series.Add(line);
            foreach (var line in _model.TransformedCoordinates()) CoordinateTransform.Series.Add(line);
            CoordinateTransform.Series.Add(VectorSeries());
            CoordinateTransform.InvalidatePlot(true);
        }

        private void UpdateVectorTransform()
        {
            VectorTransform.Series.Clear();
            VectorTransform.Series.Add(XOneFormSeries());
            VectorTransform.Series.Add(YOneFormSeries());
            foreach (var line in _model.VectorCoordinates()) VectorTransform.Series.Add(line);
            VectorTransform.Series.Add(VectorSeries(true));
            VectorTransform.Series.Add(XAxis());
            VectorTransform.Series.Add(YAxis());
            VectorTransform.InvalidatePlot(true);
        }

        private PlotModel PlotCoordinate()
        {
            var pm = new PlotModel() { Title = "Coordinate Transformation" };

            foreach (var line in _model.Lines(XOneForm)) pm.Series.Add(line);
            foreach (var line in _model.Lines(YOneForm)) pm.Series.Add(line);

            pm.Series.Add(XOneFormSeries());
            pm.Series.Add(YOneFormSeries());
            pm.Series.Add(VectorSeries(true));
            pm.Axes.Add(X());
            pm.Axes.Add(Y());

            return pm;
        }

        private PlotModel PlotRotation()
        {
            var pm = new PlotModel() { Title = "Projection Model" };
            ArrowAnnotation arrow = null;
            var xAxis = X();
            var yAxis = Y();
            pm.Axes.Add(xAxis);
            pm.Axes.Add(yAxis);

            var shape = new LineSeries();
            foreach(var d in _model.Projection.OriginalPoints())
            {
                shape.Points.Add(d);
            }
            pm.Series.Add(shape);

            pm.MouseDown += (s, e) =>
            {
                arrow = new ArrowAnnotation();
                arrow.StartPoint = arrow.EndPoint = Axis.InverseTransform(e.Position, xAxis, yAxis);
                DX = arrow.StartPoint.X;
                DY = arrow.StartPoint.Y;
                shape.Points.Clear();
                foreach (var d in _model.Projection.TransformedPoints())
                {
                    shape.Points.Add(d);
                }
                pm.Annotations.Add(arrow);
                pm.InvalidatePlot(true);
                e.Handled = true;
            };

            pm.MouseMove += (s, e) =>
            {
                if(arrow != null)
                {
                    arrow.EndPoint = Axis.InverseTransform(e.Position, xAxis, yAxis);
                    var dy = arrow.EndPoint.Y - arrow.StartPoint.Y;
                    var dx = arrow.EndPoint.X - arrow.StartPoint.X;
                    Theta = Math.Atan2(-dy, dx);
                    shape.Points.Clear();
                    foreach (var d in _model.Projection.TransformedPoints())
                    {
                        shape.Points.Add(d);
                    }
                    pm.InvalidatePlot(true);
                    e.Handled = true;
                }
               
            };

            pm.MouseUp += (s, e) =>
            {
                pm.Annotations.Clear();
                arrow = null;
            };

            return pm;
        }

        private PlotModel PlotVector()
        {
            var pm = new PlotModel() { Title = "Vector Transformation" };

            pm.Series.Add(XOneFormSeries());
            pm.Series.Add(YOneFormSeries());
            pm.Series.Add(VectorSeries(true));
            pm.Series.Add(XAxis());
            pm.Series.Add(YAxis());
            pm.Axes.Add(X());
            pm.Axes.Add(Y());

            return pm;
        }


        private void InitializeVectors()
        {
            XOneForm = new OneForm(2.0, 1.0 );
            YOneForm = new OneForm(1.0, 2.0 );
            TheVector = Vector<double>.Build.DenseOfArray(new[] { 1.0, 1.0 });
        }

        private LinearAxis X()
        {
            return new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                Maximum = _model.Max,
                Minimum = _model.Min
            };
        }

        private LineSeries XAxis()
        {
            var x = new LineSeries();
            x.Points.Add(new DataPoint(_model.Min, 0));
            x.Points.Add(new DataPoint(_model.Max, 0));
            x.MarkerType = MarkerType.None;
            x.LineStyle = LineStyle.Solid;
            x.Color = OxyColors.Black;
            return x;
        }

        private LineSeries YAxis()
        {
            var x = new LineSeries();
            x.Points.Add(new DataPoint(0, _model.Min));
            x.Points.Add(new DataPoint(0, _model.Max));
            x.MarkerType = MarkerType.None;
            x.LineStyle = LineStyle.Solid;
            x.Color = OxyColors.Black;
            return x;
        }

        private LinearAxis Y()
        {
            return new LinearAxis()
            {
                Position = AxisPosition.Left,
                Maximum = _model.Max,
                Minimum = _model.Min
            };
        }

        private LineSeries VectorSeries(bool transform = false)
        {
            var theVector = new LineSeries()
            {
                MarkerType = MarkerType.Square,
                MarkerFill = OxyColors.Gold
            };
            theVector.Points.Add(new DataPoint(0, 0));
            if (transform)
                theVector.Points.Add(new DataPoint(Transform.Multiply(TheVector)[0], Transform.Multiply(TheVector)[1]));
            else
                theVector.Points.Add(new DataPoint(TheVector[0], TheVector[1]));
            return theVector;
        }

        private LineSeries XOneFormSeries()
        {
            var xOneForm = new LineSeries()
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.Blue
            };
            xOneForm.Points.Add(new DataPoint(0, 0));
            xOneForm.Points.Add(new DataPoint(_model.XOneForm.X, _model.XOneForm.Y));
            xOneForm.LineStyle = LineStyle.Dash;
            xOneForm.MarkerStrokeThickness = 1;
            xOneForm.Color = OxyColors.Yellow;
            return xOneForm;
        }

        private LineSeries YOneFormSeries()
        {
            var xOneForm = new LineSeries()
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.Red
            };
            xOneForm.Points.Add(new DataPoint(0, 0));
            xOneForm.Points.Add(new DataPoint(_model.YOneForm.X, _model.YOneForm.Y));
            xOneForm.LineStyle = LineStyle.Dash;
            xOneForm.MarkerStrokeThickness = 1;
            xOneForm.Color = OxyColors.Yellow;
            return xOneForm;
        }

        public double A { get { return _model.A; } set { _model.A = value; OnPropertyChanged("A"); } }
        public double B { get { return _model.B; } set { _model.B = value; OnPropertyChanged("B"); } }
        public double C { get { return _model.C; } set { _model.C = value; OnPropertyChanged("C"); } }
        public double D { get { return _model.D; } set { _model.D = value; OnPropertyChanged("D"); } }
        public double E { get { return _model.E; } set { _model.E = value; OnPropertyChanged("E"); } }
        public double F { get { return _model.F; } set { _model.F = value; OnPropertyChanged("F"); } }
        public double G { get { return _model.G; } set { _model.G = value; OnPropertyChanged("G"); } }
        public double H { get { return _model.H; } set { _model.H = value; OnPropertyChanged("H"); } }
        public double I { get { return _model.I; } set { _model.I = value; OnPropertyChanged("I"); } }
        public double Theta { get { return _model.Theta; } set { _model.Theta = value; OnPropertyChanged("Theta"); } }
        public double DX { get { return _model.DX; } set { _model.DX = value; OnPropertyChanged("DX"); } }
        public double DY { get { return _model.DY; } set { _model.DY = value; OnPropertyChanged("DY"); } }

        public OneForm XOneForm
        {
            get { return _model.XOneForm; }
            set
            {
                _model.XOneForm = value;
                OnPropertyChanged("XOneForm");
            }
        }

        public OneForm YOneForm
        {
            get { return _model.YOneForm; }
            set
            {
                _model.YOneForm = value;
                OnPropertyChanged("YOneForm");
            }
        }

        public Vector<double> TheVector
        {
            get { return _model.InitialVector; }
            set
            {
                _model.InitialVector = value;
                OnPropertyChanged("TheVector");
            }
        }

        public Matrix<double> Transform
        {
            get { return _model.Transform; }
            set
            {
                _model.Transform = value;
                OnPropertyChanged("Transform");
            }
        }

        public PlotModel ControlPlot
        {
            get { return _controlPlot; }
            set
            {
                _controlPlot = value;
                OnPropertyChanged("ControlPlot");
            }
        }


        public PlotModel VectorTransform
        {
            get { return _vectorTransform; }
            set
            {
                _vectorTransform = value;
                OnPropertyChanged("VectorTransform");
            }
        }
        public PlotModel RotationPlot
        {
            get { return _rotationPlot; }
            set
            {
                _rotationPlot = value;
                OnPropertyChanged("RotationPlot");
            }
        }

        public PlotModel CoordinateTransform
        {
            get { return _coordinateTransform; }
            set
            {
                _coordinateTransform = value;
                OnPropertyChanged("CoordinateTransform");
            }
        }

        protected void OnPropertyChanged (string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
