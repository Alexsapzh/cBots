using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AutoRescale = false, AccessRights = AccessRights.None)]
    public class ChannelSketcher : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 1)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 0, MinValue = 0)]
        public int Extend { get; set; }

        [Parameter(DefaultValue = 0.1, MinValue = 0.01, Step = 0.01)]
        public double Thickness { get; set; }

        [Output("Diff", PlotType = PlotType.Points)]
        public IndicatorDataSeries Diff { get; set; }

        protected override void Initialize()
        {
        }

        public override void Calculate(int index)
        {
            // Starting and ending bars

            int x1 = Source.Count - Period;
            int x2 = Source.Count - 1;

            // Linear regression parameters

            double sumX = 0;
            double sumX2 = 0;
            double sumY = 0;
            double sumXY = 0;

            for (int count = x1; count <= x2; count++)
            {
                sumX += count;
                sumX2 += count * count;
                sumY += Source[count];
                sumXY += Source[count] * count;
            }

            double divisor = (Period * sumX2 - sumX * sumX);
            double slope = (Period * sumXY - sumX * sumY) / divisor;
            double intercept = (sumY - slope * sumX) / Period;

            // Distance to the regression line

            double deviation = 0;

            for (int count = x1; count <= x2; count++)
            {
                double regression = slope * count + intercept;
                deviation = Math.Max(Math.Abs(Source[count] - regression), deviation);
            }

            // Linear regression channel

            x2 += Extend;

            double y1 = slope * x1 + intercept;
            double y2 = slope * x2 + intercept;

            var color = y1 < y2 ? Colors.LimeGreen : Colors.Red;

            ChartObjects.DrawLine("Upper" + index, x1, y1 + deviation, x2, y2 + deviation, color, Thickness, LineStyle.Solid);
            ChartObjects.DrawLine("Lower" + index, x1, y1 - deviation, x2, y2 - deviation, color, Thickness, LineStyle.Solid);

            Diff[index] = y2 - y1;
        }
    }
}
