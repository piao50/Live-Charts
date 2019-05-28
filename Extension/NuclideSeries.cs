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
	/// <summary>
    /// An already configured weighted chart point, this class notifies the chart to update every time a property changes
    /// </summary>
    public class NuclidePoint : ObservablePoint
    {
        private double _channel;
        private double _count;
        private double _energy;
        private string _nuclide;

        /// <summary>
        /// Creates a new instance of BubblePoint class
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

        /// <summary>
        /// X coordinate in the chart
        /// </summary>
        public double Channel
        {
            get { return _channel; }
            set
            {
                _channel = value;
                OnPropertyChanged("Channel");
            }
        }

        /// <summary>
        /// Y coordinate in the chart
        /// </summary>
        public double Count
        {
            get { return _count; }
            set
            {
                _count = value;
                OnPropertyChanged("Count");
            }
        }

        /// <summary>
        /// Point's weight
        /// </summary>
        public double Energy
        {
            get { return _energy; }
            set
            {
                _energy = value;
                OnPropertyChanged("Energy");
            }
        }
    }

	public class NuclidePointView : IChartPointView
	{
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

            if (DataLabel != null && double.IsNaN(Canvas.GetLeft(DataLabel)))
            {
                Canvas.SetTop(DataLabel, current.ChartLocation.Y);
                Canvas.SetLeft(DataLabel, current.ChartLocation.X);
            }

            if (HoverShape != null)
            {
                HoverShape.Width = Diameter;
                HoverShape.Height = Diameter;
                Canvas.SetLeft(HoverShape, current.ChartLocation.X - Diameter / 2);
                Canvas.SetTop(HoverShape, current.ChartLocation.Y - Diameter / 2);
            }

            if (chart.View.DisableAnimations)
            {
                Shape.Width = Diameter;
                Shape.Height = Diameter;

                Canvas.SetTop(Shape, current.ChartLocation.Y - Shape.Height*.5);
                Canvas.SetLeft(Shape, current.ChartLocation.X - Shape.Width*.5);

                {
                    TextBlock.UpdateLayout();

                    var cx = CorrectXLabel(current.ChartLocation.X - TextBlock.ActualWidth * .5, chart);
                    var cy = CorrectYLabel(current.ChartLocation.Y - TextBlock.ActualHeight * .5, chart);

                    Canvas.SetTop(TextBlock, cy);
                    Canvas.SetLeft(TextBlock, cx);
                }

                if (DataLabel != null)
                {
                    DataLabel.UpdateLayout();

                    var cx = CorrectXTextBlock(current.ChartLocation.X - DataLabel.ActualWidth*.5, chart);
                    var cy = CorrectYTextBlock(current.ChartLocation.Y - DataLabel.ActualHeight*.5, chart);

                    Canvas.SetTop(DataLabel, cy);
                    Canvas.SetLeft(DataLabel, cx);
                }

                return;
            }

            var animSpeed = chart.View.AnimationsSpeed;

            if (DataLabel != null)
            {
                DataLabel.UpdateLayout();

                var cx = CorrectXLabel(current.ChartLocation.X - DataLabel.ActualWidth*.5, chart);
                var cy = CorrectYLabel(current.ChartLocation.Y - DataLabel.ActualHeight*.5, chart);

                DataLabel.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(cx, animSpeed));
                DataLabel.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(cy, animSpeed));
            }

            if (TextBlock != null)
            {
                TextBlock.UpdateLayout();

                var cx = CorrectXTextBlock(current.ChartLocation.X - TextBlock.ActualWidth * .5, chart);
                var cy = CorrectYTextBlock(current.ChartLocation.Y - TextBlock.ActualHeight * .5, chart);

                TextBlock.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(cx, animSpeed));
                TextBlock.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(cy, animSpeed));
            }

            Shape.BeginAnimation(FrameworkElement.WidthProperty,
                new DoubleAnimation(Diameter, animSpeed));
            Shape.BeginAnimation(FrameworkElement.HeightProperty,
                new DoubleAnimation(Diameter, animSpeed));

            Shape.BeginAnimation(Canvas.TopProperty,
                new DoubleAnimation(current.ChartLocation.Y - Diameter*.5, animSpeed));
            Shape.BeginAnimation(Canvas.LeftProperty,
                new DoubleAnimation(current.ChartLocation.X - Diameter*.5, animSpeed));
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

        protected double CorrectXLabel(double desiredPosition, ChartCore chart)
        {
            if (desiredPosition + DataLabel.ActualWidth > chart.DrawMargin.Width)
                desiredPosition -= desiredPosition + DataLabel.ActualWidth - chart.DrawMargin.Width;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }

        protected double CorrectYLabel(double desiredPosition, ChartCore chart)
        {
            if (desiredPosition + DataLabel.ActualHeight > chart.DrawMargin.Height)
                desiredPosition -= desiredPosition + DataLabel.ActualHeight - chart.DrawMargin.Height;

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

	public class NuclideAlgorithm : LineAlgorithm
    {
		public NuclideAlgorithm(ISeriesView view) : base(view)
		{
		}
		public override void Update()
		{
            foreach (var chartPoint in View.ActualValues.GetPoints(View))
            {
                chartPoint.SetPrivateProperty("View", View.GetPointView(chartPoint, View.DataLabels ? View.GetLabelPointFormatter()(chartPoint) : null));
                chartPoint.SetPrivateProperty("SeriesView", View);
                chartPoint.SetPrivateProperty("ChartLocation", ChartFunctions.ToDrawMargin(chartPoint, View.ScalesXAt, View.ScalesYAt, Chart));
                chartPoint.View.DrawOrMove(null, chartPoint, 0, Chart);
            }
		}
    }

    public static class TestClass
    {
        public static void SetPrivateProperty(this object instance, string propertyname, object value)
        {
            var type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname);//flag
            field.SetValue(instance, value, null);
        }

        public static T GetPrivateField<T>(this object instance, string fieldname)
        {
            var type = instance.GetType();
            FieldInfo field = type.GetField(fieldname);//, flag);
            return (T)field.GetValue(instance);
        }
    }

	public class NuclideSeries : Series
	{
        #region Constructors
        /// <summary>
        /// Initializes a new instance of ColumnSeries class
        /// </summary>
        public NuclideSeries()
        {
            Model = new NuclideAlgorithm(this);
            InitializeDefuaults();
        }

        /// <summary>
        /// Initializes a new instance of ColumnSeries class, using a given mapper
        /// </summary>
        public NuclideSeries(object configuration)
        {
            Model = new NuclideAlgorithm(this);
            Configuration = configuration;
            InitializeDefuaults();
        }

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
                    TextBlock = new TextBlock
                    {
                        Text = val == null ? "--" : val.Nuclide,
                        FontStyle = FontStyles.Italic,
                        LayoutTransform = new RotateTransform(-90),
                    },
                };
                Model.Chart.View.AddToDrawMargin(pbv.TextBlock);
            }
            else
            {
                pbv.IsNew = false;
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.TextBlock);
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.Shape);
                //point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.HoverShape);
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.DataLabel);
            }
            var PointGeometrySize = 15;
            if (PointGeometry != null && Math.Abs(PointGeometrySize) > 0.1 && pbv.Shape == null)
            {
                if (PointGeometry != null)
                {
                    pbv.Shape = new Path
                    {
                        Stretch = Stretch.Fill,
                        StrokeThickness = StrokeThickness
                    };
                }
                Model.Chart.View.AddToDrawMargin(pbv.Shape);
            }
            
            if (pbv.Shape != null)
            {
                //pbv.Shape.Fill = PointForeground;
                pbv.Shape.Stroke = Stroke;
                pbv.Shape.StrokeThickness = StrokeThickness;
                //pbv.Shape.Width = PointGeometrySize;
                //pbv.Shape.Height = PointGeometrySize;
                pbv.Shape.SetPrivateProperty("Data", PointGeometry);
                pbv.Shape.Visibility = Visibility;
                Panel.SetZIndex(pbv.Shape, Panel.GetZIndex(this) + 1);

                if (point.Stroke != null) pbv.Shape.Stroke = (Brush)point.Stroke;
                if (point.Fill != null) pbv.Shape.Fill = (Brush)point.Fill;
            }

            return pbv;
		}
        #endregion

        #region Private Methods

        private void InitializeDefuaults()
        {
            SetCurrentValue(StrokeThicknessProperty, 0d);

            Func<ChartPoint, string> defaultLabel = x => Model.CurrentYAxis.GetFormatter()(x.Y);
            SetCurrentValue(LabelPointProperty, defaultLabel);
        }

        #endregion
	}
}