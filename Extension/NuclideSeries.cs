using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LiveCharts.Charts;
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
    public class NuclidePoint : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _weight;

        /// <summary>
        /// Creates a new instance of BubblePoint class
        /// </summary>
        public NuclidePoint()
        {
            
        }

        /// <summary>
        /// Create a new instance of BubblePoint class, giving x and y coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public NuclidePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a new instance of BubblePoint class, giving x, y and weight
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="weight"></param>
        public NuclidePoint(double x, double y, double weight)
        {
            X = x;
            Y = y;
            Weight = weight;
        }

        /// <summary>
        /// X coordinate in the chart
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged("X");
            }
        }

        /// <summary>
        /// Y coordinate in the chart
        /// </summary>
        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        /// <summary>
        /// Point's weight
        /// </summary>
        public double Weight
        {
            get { return _weight; }
            set
            {
                _weight = value;
                OnPropertyChanged("Weight");
            }
        }

        #region INotifyPropertyChangedImplementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

	public class NuclidePointView : IChartPointView
	{
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

                if (DataLabel != null)
                {
                    DataLabel.UpdateLayout();

                    var cx = CorrectXLabel(current.ChartLocation.X - DataLabel.ActualWidth*.5, chart);
                    var cy = CorrectYLabel(current.ChartLocation.Y - DataLabel.ActualHeight*.5, chart);

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

	public class NuclideAlgorithm : SeriesAlgorithm
	{
		public NuclideAlgorithm(ISeriesView view) : base(view)
		{
		}
		public override void Update()
		{
			var nuclideSeries = (NuclideSeries) View;
            foreach (var chartPoint in View.ActualValues.GetPoints(View))
            {
                //chartPoint.ChartLocation = ChartFunctions.ToDrawMargin(
                //    chartPoint, View.ScalesXAt, View.ScalesYAt, Chart) + uw;

                //chartPoint.SeriesView = View;

                //chartPoint.View = View.GetPointView(chartPoint,
                //    View.DataLabels ? View.GetLabelPointFormatter()(chartPoint) : null);

                //var bubbleView = (IScatterPointView) chartPoint.View;

                //bubbleView.Diameter = m*(chartPoint.Weight - p1.X) + p1.Y;

                chartPoint.View.DrawOrMove(null, chartPoint, 0, Chart);
            }
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
            return null;
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