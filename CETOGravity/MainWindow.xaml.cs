using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Mechanix;
using OxyPlot.Series;
using OxyPlot;

namespace CETOGravity
{
    public partial class MainWindow : Window
    {
        enum Entities { Ship, Planet }

        bool _isCancelationRequested;

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            plotX.Model = new PlotModel()
            {
                Title = "x(t)",
                IsLegendVisible = false
            };
            plotY.Model = new PlotModel()
            {
                Title = "y(t)",
                IsLegendVisible = false
            };
            orbitPlot.Model = new PlotModel()
            {
                Title = "Orbit",
                LegendPosition = LegendPosition.TopLeft,
                LegendFontSize = 10
            };
        }

        private async void EvalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isCancelationRequested = false;

                var k = double.Parse(kValBox.Text);
                var alpha = double.Parse(alphaValueBox.Text);
                var dt = double.Parse(dtBox.Text);
                var timeSpan = double.Parse(timeSpanBox.Text);
                var planetRadius = double.Parse(planetRadiusBox.Text);

                var interval = ulong.Parse(renderingIntervalBox.Text);
                var progressBarSteps = uint.Parse(progressBarStepsBox.Text);

                var context = new PhysicalContext<Entities>(timePerTick: dt, capacity: 2);
                context.AddEntity
                (
                    Entities.Planet,
                    new PointMass(0, 0, 0, 1)
                );
                context.AddEntity
                (
                    Entities.Ship,
                    new PointMass
                    (
                        new AxisStatus(double.Parse(xPosBox.Text), double.Parse(xVelBox.Text)),
                        new AxisStatus(double.Parse(yPosBox.Text), double.Parse(yVelBox.Text)),
                        new AxisStatus(),
                        1 
                    ),
                    c => ModifiedGravityLaw<Entities>(c[Entities.Ship], c[Entities.Planet], alpha, k)
                );

                var xTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].X.Position, interval);
                var yTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].Y.Position, interval);

                var progress = ContextProgressTracker<Entities>.FromTime
                (
                    context,
                    timeSpan,
                    Enumerable.Range(1, (int)progressBarSteps).Select(i => timeSpan * i / progressBarSteps).ToArray()
                );
                progress.OnCheckPoint += (c, _) => Dispatcher.Invoke(() => progressBar.Value = progress.Progress);

                bool isSuccesful = await Task.Run
                (
                    () => context.Tick
                    (
                        timeSpan,
                        c =>
                            !_isCancelationRequested &&
                            (
                                c[Entities.Ship].X.Position * c[Entities.Ship].X.Position +
                                c[Entities.Ship].Y.Position * c[Entities.Ship].Y.Position >=
                                planetRadius * planetRadius
                            ),
                        false
                    )
                );

                if (!isSuccesful && !_isCancelationRequested)
                {
                    Task.Run(() => MessageBox.Show("Ship has crashed"));
                }

                var title = $"Start position = ({xPosBox.Text}; {yPosBox.Text})\n" +
                            $"Start velocity = ({xVelBox.Text}; {yVelBox.Text})\n" +
                            $"dt = {dtBox.Text}, K = {kValBox.Text}, Alpha = {alphaValueBox.Text}";

                var xSeries = new LineSeries()
                {
                    CanTrackerInterpolatePoints = false,
                    Title = title
                };
                var ySeries = new LineSeries()
                {
                    CanTrackerInterpolatePoints = false,
                    Title = title
                };
                var orbitSeries = new LineSeries()
                {
                    CanTrackerInterpolatePoints = false,
                    Title = title
                };

                xSeries.Points.AddRange(from p in xTracker select new DataPoint(p.Key * dt, p.Value));
                ySeries.Points.AddRange(from p in yTracker select new DataPoint(p.Key * dt, p.Value));
                orbitSeries.Points.AddRange(xTracker.Zip(yTracker, (x, y) => new DataPoint(x.Value, y.Value)));

                plotX.Model.Series.Add(xSeries);
                plotY.Model.Series.Add(ySeries);
                orbitPlot.Model.Series.Add(orbitSeries);
                plotX.InvalidatePlot();
                plotY.InvalidatePlot();
                orbitPlot.InvalidatePlot();

                progressBar.Value = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Force ModifiedGravityLaw<TEntityKey>(in PointMass ship, in PointMass planet, double alpha, double k)
        {
            var xDistance = ship.X.Position - planet.X.Position;
            var yDistance = ship.Y.Position - planet.Y.Position;
            var distance = Math.Sqrt(xDistance * xDistance + yDistance * yDistance);

            var forceValue = k / Math.Pow(distance, 2 + alpha);
            return new Force
            (
                -forceValue * xDistance / distance,
                -forceValue * yDistance / distance,
                0
            );
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            plotX.Model.Series.Clear();
            plotY.Model.Series.Clear();
            orbitPlot.Model.Series.Clear();
            plotX.InvalidatePlot();
            plotY.InvalidatePlot();
            orbitPlot.InvalidatePlot();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelationRequested = true;
        }
    }
}
