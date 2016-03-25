// Experimental.
// Discover the secrets of the Triple-Trendbox. Visual trend-analysis.
// Based on the logic of the Triple-DailyOHLC indicator.
//
// There are three boxes created.
// The further the trading day evolves, the more relevant the current Triple-TrendBox becomes.
// Each trendline has a possible reversal or continuation potential, 
// more or less significant, depending on your (lower) Timeframe.
//
// The red and green solid trendlines are related to the Triple-DailyOHLC Indicator.
// Therefore they move along with the next high or low that occures during the current day.
// Aditionally, red and green trendlines are projected to the next two days.
// When a new high or low is established, the dotted lines visually provide the trendreversal or -continuation.
//
// Esentially, all the evolving trendlines shown,
// are merely a representation of the price action itself.
//
//               -- Free to use --
//                     MaVe 
// ---- Version:  Monday1Feb2016
//               -----------------
//                    Author          
//                 Mario Verheye  
//               -----------------
using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AutoRescale = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MaVeTripleTrendBox : Indicator
    {
        [Parameter("Boxes to create", DefaultValue = 3, MaxValue = 3, MinValue = 1)]
        public int Boxes { get; set; }
        [Parameter("High/Low Color", DefaultValue = "Yellow")]
        public string DailyHLcolor { get; set; }
        private Colors dailyHLcolor;
        private MarketSeries daily;
// ------------------------------------------------------        
        protected override void Initialize()
        {
            Enum.TryParse(DailyHLcolor, out dailyHLcolor);
            daily = MarketData.GetSeries(TimeFrame.Daily);
        }
// ------------------------------------------------------  
        public override void Calculate(int index)
        {
            DateTime today = MarketSeries.OpenTime[index].Date;
            for (int i = 0; i < Boxes; i++)
            {
                DateTime startOfDay = today.AddDays(-i);
                DateTime endOfDay = today.AddDays(1 - i);
                var high = daily.High.Last(i);
                var high2 = daily.High.Last(i - 1);
                var high3 = daily.High.Last(i - 2);
                var low = daily.Low.Last(i);
                var low2 = daily.Low.Last(i - 1);
                var low3 = daily.Low.Last(i - 2);
                var median = ((low + high) / 2);
// ----- Triple DailyHighLow  
                ChartObjects.DrawLine("High" + i, startOfDay, high, endOfDay, high, dailyHLcolor, 1);
                ChartObjects.DrawLine("Low" + i, startOfDay, low, endOfDay, low, dailyHLcolor, 1);
                ChartObjects.DrawLine("pHigh" + i, endOfDay, high, endOfDay.AddDays(1), high, Colors.Green, 1, LineStyle.Lines);
                ChartObjects.DrawLine("pLow" + i, endOfDay, low, endOfDay.AddDays(1), low, Colors.Red, 1, LineStyle.Lines);
                ChartObjects.DrawLine("ppHigh" + i, endOfDay.AddDays(1), high, endOfDay.AddDays(2), high, Colors.Green, 1, LineStyle.DotsRare);
                ChartObjects.DrawLine("ppLow" + i, endOfDay.AddDays(1), low, endOfDay.AddDays(2), low, Colors.Red, 1, LineStyle.DotsRare);
// ----- Triple Trendlines
                ChartObjects.DrawLine("DownTrend1" + i, startOfDay, high, endOfDay, low, Colors.Red, 1, LineStyle.Solid);
                ChartObjects.DrawLine("DownTrend2" + i, startOfDay, high, endOfDay.AddDays(1), low, Colors.Red, 1, LineStyle.Lines);
                ChartObjects.DrawLine("DownTrend3" + i, startOfDay, high, endOfDay.AddDays(2), low, Colors.Red, 1, LineStyle.DotsRare);
                ChartObjects.DrawLine("UpTrend1" + i, startOfDay, low, endOfDay, high, Colors.Green, 1, LineStyle.Solid);
                ChartObjects.DrawLine("UpTrend2" + i, startOfDay, low, endOfDay.AddDays(1), high, Colors.Green, 1, LineStyle.Lines);
                ChartObjects.DrawLine("UpTrend3" + i, startOfDay, low, endOfDay.AddDays(2), high, Colors.Green, 1, LineStyle.DotsRare);
// ----- TurnLines
                ChartObjects.DrawLine("DownRC-Line1" + i, startOfDay, high, startOfDay.AddDays(2), low2, Colors.Blue, 1, LineStyle.Lines);
                ChartObjects.DrawLine("DownRC-Line2" + i, startOfDay, high, startOfDay.AddDays(3), low2, dailyHLcolor, 1, LineStyle.DotsRare);
                ChartObjects.DrawLine("DownRC-Line3" + i, startOfDay, high, endOfDay.AddDays(2), low3, Colors.White, 1, LineStyle.DotsRare);
                ChartObjects.DrawLine("UpRC-Line1" + i, startOfDay, low, startOfDay.AddDays(2), high2, Colors.Blue, 1, LineStyle.Lines);
                ChartObjects.DrawLine("UpRC-Line2" + i, startOfDay, low, startOfDay.AddDays(3), high2, dailyHLcolor, 1, LineStyle.DotsRare);
                ChartObjects.DrawLine("UpRC-Line3" + i, startOfDay, low, endOfDay.AddDays(2), high3, Colors.White, 1, LineStyle.DotsRare);
// ----- MedianPriceLine
                ChartObjects.DrawLine("Median" + i, startOfDay, median, endOfDay, median, Colors.White, 1);
            }
        }
    }
}
