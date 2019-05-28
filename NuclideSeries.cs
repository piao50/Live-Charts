using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Definitions.Points;
using LiveCharts.Definitions.Series;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf;
using LiveCharts.Charts;
using LiveCharts.Dtos;
using System.Windows.Media.Animation;
using System.Windows.Data;
using LiveCharts.Defaults;
using LiveCharts.Wpf.Charts.Base;
using System.Reflection;

namespace LiveCharts.Extension
{
    /// <summary>
    /// 核素标注信息
    /// </summary>
    public class NuclidePoint : ObservablePoint
    {
        public string NuclideName { get; set; } = "--";
        public double Energy { get; set; } = double.NaN;
        public double Confidence { get; set; } = double.NaN;

        public NuclidePoint()
        {
        }
        public NuclidePoint(double X, double Y, string NuclideName = "--")
            : base(X, Y)
        {
            this.NuclideName = NuclideName;
        }
    }

    public class NuclidePointView : IChartPointView
    {
        public TextBlock TextBlock { get; set; }
        public Shape Shape { get; set; }
        public double Diameter { get; set; }

        #region IChartPointView
        public Shape HoverShape { get; set; }
        public ContentControl DataLabel { get; set; }
        public bool IsNew { get; set; }
        public CoreRectangle ValidArea { get; internal set; }

        public void DrawOrMove(ChartPoint previousDrawn, ChartPoint current, int index, ChartCore chart)
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

                Canvas.SetTop(Shape, current.ChartLocation.Y - Shape.Height * .5);
                Canvas.SetLeft(Shape, current.ChartLocation.X - Shape.Width * .5);
                if (TextBlock != null)
                {
                    TextBlock.UpdateLayout();
                    Canvas.SetTop(TextBlock, current.ChartLocation.Y - TextBlock.ActualHeight * .5);
                    Canvas.SetLeft(TextBlock, current.ChartLocation.X - TextBlock.ActualWidth * .5);
                }

                if (DataLabel != null)
                {
                    DataLabel.UpdateLayout();

                    var cx = CorrectXLabel(current.ChartLocation.X - DataLabel.ActualWidth * .5, chart);
                    var cy = CorrectYLabel(current.ChartLocation.Y - DataLabel.ActualHeight * .5, chart);

                    Canvas.SetTop(DataLabel, cy);
                    Canvas.SetLeft(DataLabel, cx);
                }

                return;
            }

            var animSpeed = chart.View.AnimationsSpeed;

            if (DataLabel != null)
            {
                DataLabel.UpdateLayout();

                var cx = CorrectXLabel(current.ChartLocation.X - DataLabel.ActualWidth * .5, chart);
                var cy = CorrectYLabel(current.ChartLocation.Y - DataLabel.ActualHeight * .5, chart);

                DataLabel.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(cx, animSpeed));
                DataLabel.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(cy, animSpeed));
            }

            Shape.BeginAnimation(FrameworkElement.WidthProperty,
                new DoubleAnimation(Diameter, animSpeed));
            Shape.BeginAnimation(FrameworkElement.HeightProperty,
                new DoubleAnimation(Diameter, animSpeed));


            Shape.BeginAnimation(Canvas.TopProperty,
                new DoubleAnimation(current.ChartLocation.Y - Diameter * .5, animSpeed));
            Shape.BeginAnimation(Canvas.LeftProperty,
                new DoubleAnimation(current.ChartLocation.X - Diameter * .5, animSpeed));

            if (TextBlock != null)
            {
                TextBlock.UpdateLayout();

                //var cx = CorrectXLabel(current.ChartLocation.X - TextBlock.ActualWidth * .5, chart);
                //var cy = CorrectYLabel(current.ChartLocation.Y - TextBlock.ActualHeight * .5, chart);

                TextBlock.BeginAnimation(Canvas.LeftProperty,
                    new DoubleAnimation(current.ChartLocation.Y - TextBlock.ActualHeight * .5, animSpeed));
                TextBlock.BeginAnimation(Canvas.TopProperty, 
                    new DoubleAnimation(current.ChartLocation.X - TextBlock.ActualWidth * .5, animSpeed));
            }
        }

        public void RemoveFromView(ChartCore chart)
        {
            chart.View.RemoveFromDrawMargin(TextBlock);
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

        public void OnHover(ChartPoint point)
        {
            var copy = Shape.Fill.Clone();
            copy.Opacity -= .15;
            Shape.Fill = copy;
        }

        public void OnHoverLeave(ChartPoint point)
        {
            if (Shape == null) return;

            if (point.Fill != null)
            {
                Shape.Fill = (Brush)point.Fill;
            }
            else
            {
                Shape.Fill = ((Series)point.SeriesView).Fill;
            }
        }
        #endregion
    }

    public class NuclideSeries : LineSeries
    {
        public NuclideSeries() : base()
        {
            Model = new NuclideAlgorithm(this);
        }
        public NuclideSeries(object configuration) : base(configuration)
        {
            Model = new NuclideAlgorithm(this);
        }
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
                        Text = val == null ? "--" : val.NuclideName,
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
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.HoverShape);
                point.SeriesView.Model.Chart.View.EnsureElementBelongsToCurrentDrawMargin(pbv.DataLabel);
            }

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
                pbv.Shape.Fill = PointForeground;
                pbv.Shape.Stroke = Stroke;
                pbv.Shape.StrokeThickness = StrokeThickness;
                pbv.Shape.Width = PointGeometrySize;
                pbv.Shape.Height = PointGeometrySize;
                pbv.Shape.SetPrivateProperty("Data", PointGeometry);
                pbv.Shape.Visibility = Visibility;
                Panel.SetZIndex(pbv.Shape, Panel.GetZIndex(this) + 1);

                if (point.Stroke != null) pbv.Shape.Stroke = (Brush)point.Stroke;
                if (point.Fill != null) pbv.Shape.Fill = (Brush)point.Fill;
            }

            if (Model.Chart.RequiresHoverShape && pbv.HoverShape == null)
            {
                //pbv.HoverShape = new Rectangle
                //{
                //    Fill = Brushes.Transparent,
                //    StrokeThickness = 0
                //};
                //Panel.SetZIndex(pbv.HoverShape, int.MaxValue);
                //var wpfChart = (Chart)Model.Chart.View;
                //wpfChart.AttachHoverableEventTo(pbv.HoverShape);
                //Model.Chart.View.AddToDrawMargin(pbv.HoverShape);
            }
            //if (pbv.HoverShape != null) pbv.HoverShape.Visibility = Visibility;
            if (DataLabels)
            {
                //pbv.DataLabel = UpdateLabelContent(new DataLabelViewModel
                //{
                //    FormattedText = label,
                //    Point = point
                //}, pbv.DataLabel);
            }

            if (!DataLabels && pbv.DataLabel != null)
            {
                Model.Chart.View.RemoveFromDrawMargin(pbv.DataLabel);
                pbv.DataLabel = null;
            }

            //if (point.Stroke != null) pbv.Rectangle.Stroke = (Brush)point.Stroke;
            //if (point.Fill != null) pbv.Rectangle.Fill = (Brush)point.Fill;

            //pbv.LabelPosition = LabelsPosition;

            return pbv;
        }
    }

    public static class TestClass
    {
        public static void SetPrivateProperty(this object instance, string propertyname, object value)
        {
            //BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = instance.GetType();
            PropertyInfo field = type.GetProperty(propertyname);//flag
            field.SetValue(instance, value, null);
        }

        public static T GetPrivateField<T>(this object instance, string fieldname)
        {
            //BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = instance.GetType();
            FieldInfo field = type.GetField(fieldname);//, flag);
            return (T)field.GetValue(instance);
        }
    }

    public class NuclideAlgorithm : LineAlgorithm
    {
        public NuclideAlgorithm(ISeriesView view) : base(view)
        {
        }

        /// <summary>
        /// 绘制每一个数据
        /// </summary>
        public override void Update()
        {
            var points = View.ActualValues.GetPoints(View).ToArray();
            var lineView = (ILineSeriesView)View;

            //var startAt = 0;
            //var padding = 0;
            //var relativeLeft = 0;
            //var singleColWidth = 2;

            //var zero = ChartFunctions.ToDrawMargin(startAt, AxisOrientation.Y, Chart, View.ScalesYAt);

            foreach (var chartPoint in View.ActualValues.GetPoints(View))
            {
                var reference = ChartFunctions.ToDrawMargin(chartPoint, View.ScalesXAt, View.ScalesYAt, Chart);
                chartPoint.SetPrivateProperty("View", View.GetPointView(chartPoint,
                    View.DataLabels ? View.GetLabelPointFormatter()(chartPoint) : null));
                chartPoint.SetPrivateProperty("SeriesView", View);

                var rectangleView = (NuclidePointView)chartPoint.View;
                //var h = Math.Abs(reference.Y - zero);
                //var t = reference.Y < zero
                //    ? reference.Y
                //    : zero;
                //rectangleView.Data.Height = h;
                //rectangleView.Data.Top = t;

                //rectangleView.Data.Left = reference.X + relativeLeft;
                //rectangleView.Data.Width = singleColWidth - padding;

                //rectangleView.ZeroReference = zero;

                chartPoint.SetPrivateProperty("ChartLocation", ChartFunctions.ToDrawMargin(chartPoint, View.ScalesXAt, View.ScalesYAt, Chart));
                chartPoint.View.DrawOrMove(null, chartPoint, 0, Chart);
            }
        }
    }
}
