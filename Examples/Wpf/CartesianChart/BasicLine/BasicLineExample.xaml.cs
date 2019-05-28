using System;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Wpf.CartesianChart.BasicLine
{
    public partial class BasicLineExample : UserControl
    {
        public BasicLineExample()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<double> { 4, 6, 5, 2 ,7 }
                },
                new LineSeries
                {
                    Title = "Series 2",
                    Values = new ChartValues<double> { 6, 7, 3, 4 ,6 }
                },
                new NuclideSeries
                {
                    Title = "Gu Shi",
                    Values = new ChartValues<ObservablePoint> {
                        new NuclidePoint() { X = 1, Y = 2, Energy = 12.1f, Nuclide = "Gushi1"},
                        new NuclidePoint() { X = 2, Y = 4, Energy = 12.1f, Nuclide = "Gushi1"},
                        new NuclidePoint() { X = 3, Y = 8, Energy = 12.1f, Nuclide = "Gushi1"},
                        new NuclidePoint() { X = 4, Y = 12, Energy = 12.1f, Nuclide = "Gushi1"},
                    }
                }
            };

            Labels = new[] {"Jan", "Feb", "Mar", "Apr", "May"};
            YFormatter = value => value.ToString("C");

            //modifying the series collection will animate and update the chart
            SeriesCollection.Add(new LineSeries
            {
                Values = new ChartValues<double> {5, 3, 2, 4},
                LineSmoothness = 0 //straight lines, 1 really smooth lines
            });

            //modifying any series values will also animate and update the chart
            //SeriesCollection[2].Values.Add(5d);

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

    }
}
