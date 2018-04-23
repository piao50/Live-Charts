#region License
// The MIT License (MIT)
// 
// Copyright (c) 2016 Alberto Rodr�guez Orozco & LiveCharts contributors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights to 
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
// of the Software, and to permit persons to whom the Software is furnished to 
// do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using LiveCharts.Core.Charts;
using LiveCharts.Core.Coordinates;
using LiveCharts.Core.Drawing;
using LiveCharts.Core.Interaction.ChartAreas;
using LiveCharts.Core.Interaction.Points;
using LiveCharts.Core.Interaction.Series;
using LiveCharts.Core.Updating;
using LiveCharts.Core.ViewModels;

#endregion

namespace LiveCharts.Core.DataSeries
{
    /// <summary>
    /// The heat series class.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <seealso cref="CartesianStrokeSeries{TModel,TCoordinate,TViewModel, TSeries}" />
    /// <seealso cref="IHeatSeries" />
    public class HeatSeries<TModel>
        : CartesianStrokeSeries<TModel, WeightedCoordinate, HeatViewModel, IHeatSeries>, IHeatSeries
    {
        private ISeriesViewProvider<TModel, WeightedCoordinate, HeatViewModel, IHeatSeries> _provider;
        private IEnumerable<GradientStop> _gradient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatSeries{TModel}"/> class.
        /// </summary>
        public HeatSeries()
        {
            ScalesAt = new[] {0, 0, 0};
            DefaultFillOpacity = .2f;
            Charting.BuildFromSettings<IHeatSeries>(this);
        }

        /// <inheritdoc />
        public IEnumerable<GradientStop> Gradient
        {
            get => _gradient;
            set
            {
                _gradient = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public override Type ResourceKey => typeof(IHeatSeries);

        /// <inheritdoc />
        public override float[] DefaultPointWidth => new[] {1f, 1f};

        /// <inheritdoc />
        public override float[] PointMargin => new[] {0f, 0f};

        /// <inheritdoc />
        protected override ISeriesViewProvider<TModel, WeightedCoordinate, HeatViewModel, IHeatSeries>
            DefaultViewProvider => _provider ?? (_provider = Charting.Current.UiProvider.HeatViewProvider<TModel>());

        public override SeriesStyle Style => new SeriesStyle(); // ToDo: How do we display it in the legend/tooltip ??

        /// <inheritdoc />
        public override void UpdateView(ChartModel chart, UpdateContext context)
        {
            var cartesianChart = (CartesianChartModel) chart;
            var x = cartesianChart.Dimensions[0][ScalesAt[0]];
            var y = cartesianChart.Dimensions[1][ScalesAt[1]];

            var uw = chart.Get2DUiUnitWidth(x, y);

            int xi = 0, yi = 1;
            if (chart.InvertXy)
            {
                xi = 1;
                yi = 0;
            }

            // ReSharper disable CompareOfFloatsByEqualityOperator

            var d = new []
            {
                x.ActualMaxValue - x.ActualMinValue == 0
                    ? float.MaxValue
                    : x.ActualMaxValue - x.ActualMinValue,
                y.ActualMaxValue - y.ActualMinValue == 0
                    ? float.MaxValue
                    : y.ActualMaxValue - y.ActualMinValue
            };

            // ReSharper restore CompareOfFloatsByEqualityOperator

            var wp = cartesianChart.DrawAreaSize[0] / (d[xi] + 1);
            var hp = cartesianChart.DrawAreaSize[1] / (d[yi] + 1);

            var minW = context.Ranges[2][ScalesAt[2]][0];
            var maxW = context.Ranges[2][ScalesAt[2]][1];

            Point<TModel, WeightedCoordinate, HeatViewModel, IHeatSeries> previous = null;

            foreach (var current in Points)
            {
                if (current.View == null)
                {
                    current.View = ViewProvider.GetNewPoint();
                    current.ViewModel = new HeatViewModel
                    {
                        To = Color.FromArgb(0, 0, 0, 0)
                    };
                }

                var p = new[]
                {
                    chart.ScaleToUi(current.Coordinate[0][0], x),
                    chart.ScaleToUi(current.Coordinate[1][0], y)
                };

                var vm = new HeatViewModel
                {
                    Rectangle = new RectangleF(p[xi], p[yi] - uw[yi], wp, hp),
                    From = current.ViewModel.To,
                    To = ColorInterpolation(minW, maxW, current.Coordinate.Weight)
                };

                current.ViewModel = vm;
                current.View.DrawShape(current, previous);
                if (DataLabels) current.View.DrawLabel(current, DataLabelsPosition, LabelsStyle);
                Mapper.EvaluateModelDependentActions(current.Model, current.View.VisualElement, current);
                current.InteractionArea = new RectangleInteractionArea(vm.Rectangle);

                previous = current;
            }
        }

        /// <inheritdoc />
        protected override void SetDefaultColors(ChartModel chart)
        {
            if (Gradient != null) return;

            var nextColor = chart.GetNextColor();

            Gradient = new List<GradientStop>
            {
                new GradientStop
                {
                    Color = nextColor.SetOpacity(DefaultFillOpacity),
                    Offset = 0
                },
                new GradientStop
                {
                    Color = nextColor,
                    Offset = 1
                }
            };
        }

        private Color ColorInterpolation(float min, float max, float current)
        {
            var currentOffset = (current - min) / (max - min);
            var enumerator = Gradient.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                throw new LiveChartsException(
                    $"At least 2 elements must be present at the property {nameof(IHeatSeries)}.{nameof(Gradient)}.",
                    220);
            }

            var from = enumerator.Current;

            while (enumerator.MoveNext())
            {
                var to = enumerator.Current;

                if (currentOffset >= from.Offset && currentOffset <= to.Offset)
                {
                    enumerator.Dispose();

                    var p = from.Offset + (from.Offset - to.Offset) *
                            ((currentOffset - from.Offset) / (from.Offset - to.Offset));

                    return Color.FromArgb(
                        (int) Math.Round(from.Color.A + p * (to.Color.A - from.Color.A)),
                        (int) Math.Round(from.Color.R + p * (to.Color.R - from.Color.R)),
                        (int) Math.Round(from.Color.G + p * (to.Color.G - from.Color.G)),
                        (int) Math.Round(from.Color.B + p * (to.Color.B - from.Color.B)));
                }

                from = to;
            }

            throw new LiveChartsException(
                $"The property {nameof(IHeatSeries)}.{nameof(Gradient)} must contain at " +
                "least 2 elements and at the offset range should explicitly go from 0 to 1.",
                220);
        }
    }
}