using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LiveCharts.Charts;
using LiveCharts.Defaults;
using LiveCharts.Definitions.Points;
using LiveCharts.Definitions.Series;
using LiveCharts.Dtos;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf.Points;

namespace LiveCharts.Wpf
{
    public class NuclidePoint : ObservablePoint
    {
        private string _nuclide;

        /// <summary>
        /// Creates a new instance of NuclidePoint class
        /// </summary>
        public NuclidePoint()
        {
            
        }

        /// <summary>
        /// X coordinate in the chart
        /// </summary>
        public string Nuclide
        {
            get { return _nuclide; }
            set
            {
                _nuclide = value;
                OnPropertyChanged("Nuclide");
            }
        }
    }

	public class NuclidePointView : IChartPointView, IScatterPointView
	{
        public static Geometry Arraw
        {
            get
            {
                var g = Geometry.Parse("M2,0 L2,4 M2,4 L1,3 M2,4 L3,3");
                g.Freeze();
                return g;
            }
        }
        
        public TextBlock TextBlock { get; set; }
	    public Shape Shape { get; set; }
        public double Diameter { get; set; }
        public Shape HoverShape { get; set; }
        public ContentControl DataLabel { get; set; }
        public bool IsNew { get; set; }
        public CoreRectangle ValidArea { get; internal set; }

        public virtual void DrawOrMove(ChartPoint previousDrawn, ChartPoint current, int index, ChartCore chart)
        {
			if (IsNew)
            {
                Canvas.SetTop(Shape, current.ChartLocation.Y);
                Canvas.SetLeft(Shape, current.ChartLocation.X);

                Canvas.SetTop(TextBlock, current.ChartLocation.Y);
                Canvas.SetLeft(TextBlock, current.ChartLocation.X);

                Shape.Width = 0;
                Shape.Height = 0;
            }

            Shape.Width = Diameter;
            Shape.Height = Diameter;
            Canvas.SetTop(Shape, current.ChartLocation.Y - Shape.Height * .5);
            Canvas.SetLeft(Shape, current.ChartLocation.X - Shape.Width * .5);

            TextBlock.UpdateLayout();
            var cx = CorrectXTextBlock(current.ChartLocation.X - TextBlock.ActualWidth * .5, chart);
            var cy = CorrectYTextBlock(current.ChartLocation.Y - Shape.Height - TextBlock.ActualHeight * .5, chart);
            Canvas.SetTop(TextBlock, cy);
            Canvas.SetLeft(TextBlock, cx);
        }

        public virtual void RemoveFromView(ChartCore chart)
        {
            chart.View.RemoveFromDrawMargin(HoverShape);
            chart.View.RemoveFromDrawMargin(Shape);
            chart.View.RemoveFromDrawMargin(DataLabel);
        }

        protected double CorrectXTextBlock(double desiredPosition, ChartCore chart)
        {
            if (desiredPosition + TextBlock.ActualWidth > chart.DrawMargin.Width)
                desiredPosition -= desiredPosition + TextBlock.ActualWidth - chart.DrawMargin.Width;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }

        protected double CorrectYTextBlock(double desiredPosition, ChartCore chart)
        {
            if (desiredPosition + TextBlock.ActualHeight > chart.DrawMargin.Height)
                desiredPosition -= desiredPosition + TextBlock.ActualHeight - chart.DrawMargin.Height;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }

        public virtual void OnHover(ChartPoint point)
        {
            var copy = Shape.Fill.Clone();
            copy.Opacity -= .15;
            Shape.Fill = copy;
        }

        public virtual void OnHoverLeave(ChartPoint point)
        {
			if (Shape == null) return;

            if (point.Fill != null)
            {
                Shape.Fill = (Brush) point.Fill;
            }
            else
            {
                Shape.Fill = ((Series) point.SeriesView).Fill;
            }       
        }
	}

	public class NuclideAlgorithm : ScatterAlgorithm
    {
		public NuclideAlgorithm(ISeriesView view) : base(view)
		{
		}
		public override void Update()
		{
            var bubbleSeries = (IScatterSeriesView)View;

            var p1 = new CorePoint();
            var p2 = new CorePoint();

            p1.X = Chart.WLimit.Max;
            p1.Y = bubbleSeries.MaxPointShapeDiameter;

            p2.X = Chart.WLimit.Min;
            p2.Y = bubbleSeries.MinPointShapeDiameter;

            var deltaX = p2.X - p1.X;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var m = (p2.Y - p1.Y) / (deltaX == 0 ? double.MinValue : deltaX);

            var uw = new CorePoint(
                    CurrentXAxis.EvaluatesUnitWidth
                        ? ChartFunctions.GetUnitWidth(AxisOrientation.X, Chart, View.ScalesXAt) / 2
                        : 0,
                    CurrentYAxis.EvaluatesUnitWidth
                        ? ChartFunctions.GetUnitWidth(AxisOrientation.Y, Chart, View.ScalesYAt) / 2
                        : 0);
            foreach (var chartPoint in View.ActualValues.GetPoints(View))
            {
                chartPoint.SetPrivateProperty("View", View.GetPointView(chartPoint, View.DataLabels ? View.GetLabelPointFormatter()(chartPoint) : null));
                chartPoint.SetPrivateProperty("SeriesView", View);
                chartPoint.SetPrivateProperty("ChartLocation", ChartFunctions.ToDrawMargin(chartPoint, View.ScalesXAt, View.ScalesYAt, Chart));
                var bubbleView = (IScatterPointView)chartPoint.View;
                bubbleView.Diameter = m * (chartPoint.Weight - p1.X) + p1.Y;
                chartPoint.View.DrawOrMove(null, chartPoint, 0, Chart);
            }
		}
    }

    public static class Helper
    {
        public static void SetPrivateProperty(this object instance, string propertyname, object value)
        {
            var type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname);//flag
            field.SetValue(instance, value, null);
        }
    }

	public class NuclideSeries : ScatterSeries
	{
        #region Constructors
        /// <summary>
        /// Initializes a new instance of ColumnSeries class
        /// </summary>
        public NuclideSeries() : base() { Model = new NuclideAlgorithm(this); }

        /// <summary>
        /// Initializes a new instance of ColumnSeries class, using a given mapper
        /// </summary>
        public NuclideSeries(object configuration) : base(configuration) { Model = new NuclideAlgorithm(this); }

        #endregion

        #region Private Properties

        #endregion

        #region Overridden Methods
		/// <summary>
        /// Gets the view of a given point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override IChartPointView GetPointView(ChartPoint point, string label)
		{
            var pbv = (NuclidePointView)point.View;
            var val = point.Instance as NuclidePoint;
            if (pbv == null)
            {
                pbv = new NuclidePointView
                {
                    IsNew = true,
                    Shape = new Path
                    {
                        Stretch = Stretch.Fill,
                        StrokeThickness = StrokeThickness
                    },
                    TextBlock = new TextBlock
                    {
                        Text = val == null ? "--" : val.Nuclide,
                    },
                };
                Model.Chart.View.AddToDrawMargin(pbv.Shape);
                Model.Chart.View.AddToDrawMargin(pbv.TextBlock);
            }
            else
            {
                pbv.IsNew = false;
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.TextBlock);
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.Shape);
            }

            var p = (Path)pbv.Shape;
            p.Data = PointGeometry;
            p.Fill = Fill;
            p.Stroke = Stroke;
            p.StrokeThickness = StrokeThickness;
            p.Visibility = Visibility;
            Panel.SetZIndex(p, Panel.GetZIndex(this));
            p.StrokeDashArray = StrokeDashArray;

            if (point.Stroke != null) pbv.Shape.Stroke = (Brush)point.Stroke;
            if (point.Fill != null) pbv.Shape.Fill = (Brush)point.Fill;

            return pbv;
		}
        #endregion
	}
}