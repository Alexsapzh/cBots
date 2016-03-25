using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    [Levels(-1, -0.5, 0, 0.5, 1)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, ScalePrecision = 2)]
    public class CorrelationCoefficient : Indicator
    {
        private MarketSeries series2;
        private Symbol symbol2;

        [Parameter(DefaultValue = "EURUSD")]
        public string Symbol2 { get; set; }

        [Parameter(DefaultValue = 22)]
        public int Period { get; set; }

        [Output("Correlation Coefficient", Color = Colors.Yellow)]
        public IndicatorDataSeries Result { get; set; }

        protected override void Initialize()
        {
            symbol2 = MarketData.GetSymbol(Symbol2);
            series2 = MarketData.GetSeries(symbol2, TimeFrame);
        }

        public override void Calculate(int index)
        {
            if (index < Period)
                return;

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;
            double x, y, x2, y2;

            int index2 = GetIndexByDate(series2, MarketSeries.OpenTime[index]);

            if (index2 == -1)
                return;

            for (int i = 0; i < Period; i++)
            {
                x = MarketSeries.Close[index - i];
                y = series2.Close[index2 - i];

                x2 = x * x;
                y2 = y * y;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x2;
                sumY2 += y2;
            }

            Result[index] = (Period * (sumXY) - sumX * sumY) / Math.Sqrt((Period * sumX2 - sumX * sumX) * (Period * sumY2 - sumY * sumY));
        }

        private int GetIndexByDate(MarketSeries series, DateTime time)
        {
            for (int i = series.Close.Count - 1; i >= 0; i--)
            {
                if (time == series.OpenTime[i])
                    return i;
            }
            return -1;
        }
    }
}
