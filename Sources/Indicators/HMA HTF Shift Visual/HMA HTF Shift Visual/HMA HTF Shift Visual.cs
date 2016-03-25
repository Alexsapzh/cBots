using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class MultiTF_MA : Indicator
    {

        [Parameter(DefaultValue = 21)]
        public int HTF_Period { get; set; }

        [Parameter(DefaultValue = 0, MinValue = -100, MaxValue = 500)]
        public int HTFShift { get; set; }

        [Output("HTF 4HR", Color = Colors.Blue)]
        public IndicatorDataSeries HTF4hr { get; set; }

        [Output("HTF 1HR", Color = Colors.Yellow)]
        public IndicatorDataSeries HTF1hr { get; set; }

        private MarketSeries HmaHour4Series;
        private MarketSeries HmaHourSeries;
        private HMAHTFSHIFT ma4hr;
        private HMAHTFSHIFT ma1hr;

        protected override void Initialize()
        {
            HmaHour4Series = MarketData.GetSeries(TimeFrame.Hour4);
            HmaHourSeries = MarketData.GetSeries(TimeFrame.Hour);

            ma4hr = Indicators.GetIndicator<HMAHTFSHIFT>(HmaHour4Series, HTF_Period, HTFShift, false, false, 3, false, 24);
            ma1hr = Indicators.GetIndicator<HMAHTFSHIFT>(HmaHourSeries, HTF_Period, HTFShift, false, false, 3, false, 24);

        }

        public override void Calculate(int index)
        {
            //double i = hmaSignal.hma.LastValue;

            var index4hr = HmaHour4Series.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index4hr != -1)
                HTF4hr[index] = ma4hr.hma[index4hr];

            var index1hr = HmaHourSeries.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index1hr != -1)
                HTF1hr[index] = ma1hr.hma[index1hr];
        }
    }
}
