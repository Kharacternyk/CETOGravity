using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        readonly double[] alphaArray = { 0.01, 0.1, 0.5 };
        const double dt = 0.001;
        const double K = 1;
        const double ORBIT = 1;
        const double START_VELOCITY = 1;
        const double MASS = 1;
        const double TIME = 50;

        enum Entities { Ship, Planet }

        public MainWindow()
        {
            InitializeComponent();
            plotX.Model = new PlotModel() { Title = "x(t)", IsLegendVisible = true, LegendPlacement = LegendPlacement.Outside };
            plotY.Model = new PlotModel() { Title = "y(t)", IsLegendVisible = true, LegendPlacement = LegendPlacement.Outside };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var alpha in alphaArray)
            {
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
                        new AxisStatus(ORBIT),
                        new AxisStatus(0, START_VELOCITY),
                        new AxisStatus(),
                        MASS
                    ),
                    c => ModifiedGravityLaw<Entities>(c[Entities.Ship], c[Entities.Planet], K, alpha)
                );

                var xTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].X.Position);
                var yTracker = new ContextTracker<Entities, double>(context, c => c[Entities.Ship].Y.Position);

                context.Tick(TIME);

                var xSeries = new LineSeries() { Title = alpha.ToString() };
                xSeries.Points.AddRange(from p in xTracker select new DataPoint(p.Key * dt, p.Value));
                var ySeries = new LineSeries() { Title = alpha.ToString() };
                ySeries.Points.AddRange(from p in yTracker select new DataPoint(p.Key * dt, p.Value));

                plotX.Model.Series.Add(xSeries);
                plotY.Model.Series.Add(ySeries);
            }
        }

        private Force ModifiedGravityLaw<TEntityKey>(in PointMass ship, in PointMass planet, double k, double alpha)
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
    }
}
