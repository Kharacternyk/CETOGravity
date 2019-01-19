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

        CancellationTokenSource _currOperation;

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
                LegendPosition = LegendPosition.BottomCenter,
                LegendFontSize = 10
            };
        }

        private async void EvalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var k = double.Parse(kValBox.Text);
                var alpha = double.Parse(alphaValueBox.Text);
                var dt = double.Parse(dtBox.Text);
                var timeSpan = double.Parse(timeSpanBox.Text);

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
                        double.Parse(shipMassBox.Text)
                    ),
                    c => ModifiedGravityLaw<Entities>(c[Entities.Ship], c[Entities.Planet], alpha, k)
                );

                var xTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].X.Position);
                var yTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].Y.Position);

                progressBar.Maximum = timeSpan;
                context.OnTick += (c, _) => Dispatcher.Invoke(() => progressBar.Value = ((PhysicalContext<Entities>)c).Timer);
                _currOperation = new CancellationTokenSource();

                await Task.Run(() => context.Tick(timeSpan, _currOperation.Token));

                var title = $"Start position = ({xPosBox.Text}; {yPosBox.Text})\n" +
                            $"Start velocity = ({xVelBox.Text}; {yVelBox.Text})\n" +
                            $"Mass = {shipMassBox.Text}, K = {kValBox.Text}, Alpha = {alphaValueBox.Text}";

                var xSeries = new LineSeries()
                {
                    Title = title
                };
                xSeries.Points.AddRange(from p in xTracker select new DataPoint(p.Key * dt, p.Value));
                var ySeries = new LineSeries()
                {
                    Title = title
                };
                ySeries.Points.AddRange(from p in yTracker select new DataPoint(p.Key * dt, p.Value));
                var orbitSeries = new LineSeries()
                {
                    Title = title
                };
                for (ulong i = 0; i < context.Ticks; ++i)
                {
                    orbitSeries.Points.Add(new DataPoint(xTracker[i], yTracker[i]));
                }

                plotX.Model.Series.Add(xSeries);
                plotY.Model.Series.Add(ySeries);
                orbitPlot.Model.Series.Add(orbitSeries);
                plotX.InvalidatePlot();
                plotY.InvalidatePlot();
                orbitPlot.InvalidatePlot();

                progressBar.Value = 0;
            }
            catch (FormatException ex)
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
            _currOperation?.Cancel();
        }
    }
}
