using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AutoRescale = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class DailyFiboRetracement : Indicator
    {
        [Parameter("Days To Show", DefaultValue = 5)]
        public int DaysToShow { get; set; }

        [Parameter("Shift (Hours)", DefaultValue = 0)]
        public int ShiftInHours { get; set; }

        private MarketSeries daily;

        protected override void Initialize()
        {
            daily = MarketData.GetSeries(TimeFrame.Daily);
        }

        public override void Calculate(int index)
        {
            for (int i = 0; i < DaysToShow; i++)
            {
                DateTime startOfDay = daily.OpenTime.Last(i).Date.AddDays(1).AddHours(ShiftInHours);
                DateTime endOfDay = startOfDay.AddDays(1);

                var dailyHigh = daily.High.Last(i);
                var dailyLow = daily.Low.Last(i);

                var fib618n = dailyLow - (dailyHigh - dailyLow) * 0.618;
                var fib382 = dailyLow + (dailyHigh - dailyLow) * 0.382;
                var fib500 = dailyLow + (dailyHigh - dailyLow) * 0.5;
                var fib618 = dailyLow + (dailyHigh - dailyLow) * 0.618;
                var fib1618 = dailyLow + (dailyHigh - dailyLow) * 0.618;

                ChartObjects.DrawLine("-61.8%" + i, startOfDay, fib618n, endOfDay, fib618n, Colors.Gray, 1, LineStyle.LinesDots);
                ChartObjects.DrawLine("0.0%" + i, startOfDay, dailyHigh, endOfDay, dailyHigh, Colors.Gray, 1);
                ChartObjects.DrawLine("38.2%" + i, startOfDay, fib382, endOfDay, fib382, Colors.Gray, 1, LineStyle.LinesDots);
                ChartObjects.DrawLine("50.0%" + i, startOfDay, fib500, endOfDay, fib500, Colors.Gray, 1, LineStyle.Solid);
                ChartObjects.DrawLine("61.8%" + i, startOfDay, fib618, endOfDay, fib618, Colors.Gray, 1, LineStyle.LinesDots);
                ChartObjects.DrawLine("100.0%" + i, startOfDay, dailyLow, endOfDay, dailyLow, Colors.Gray, 1);
                ChartObjects.DrawLine("161.8%" + i, startOfDay, fib1618, endOfDay, fib1618, Colors.Gray, 1, LineStyle.LinesDots);
            }
        }
    }
}

