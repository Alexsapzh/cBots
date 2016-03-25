using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TimeRangeHighlighter : Indicator
    {
        [Parameter(DefaultValue = 0.0)]
        public int StartMinutes { get; set; }

        [Parameter(DefaultValue = 60)]
        public int EndMinutes { get; set; }

        [Parameter(DefaultValue = 50)]
        public int HistogramHeight { get; set; }



        [Output("Shading", PlotType = PlotType.Histogram, Thickness = 2, Color = Colors.Aqua)]
        public IndicatorDataSeries Result { get; set; }


        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...
            var time = this.MarketSeries.OpenTime[index];
            var today = new DateTime(time.Year, time.Month, time.Day);
            var diff = time.Subtract(today).TotalMinutes;
            if (diff >= StartMinutes && diff < EndMinutes)
            {
                Result[index] = this.MarketSeries.Low[index] + HistogramHeight * Symbol.PipSize;
            }

        }
    }
}
