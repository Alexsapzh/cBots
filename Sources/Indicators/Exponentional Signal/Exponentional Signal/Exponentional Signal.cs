
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class ExponentialSignal : Indicator
    {
        [Parameter(DefaultValue = 20)]
        public int Period { get; set; }

        [Output("EMA", Color = Colors.Yellow)]
        public IndicatorDataSeries EMA { get; set; }

        [Output("EMA 1 Hour", Color = Colors.Orange)]
        public IndicatorDataSeries EMAhour { get; set; }

        [Output("EMA 4 Hour", Color = Colors.Red)]
        public IndicatorDataSeries EMAhour4 { get; set; }

        [Output("EMA Daily", Color = Colors.Purple)]
        public IndicatorDataSeries EMAdaily { get; set; }

        private MarketSeries serieshour;
        private MarketSeries serieshour4;
        private MarketSeries seriesdaily;

        private MovingAverage ema;
        private MovingAverage emahour;
        private MovingAverage emahour4;
        private MovingAverage emadaily;

        protected override void Initialize()
        {
            serieshour = MarketData.GetSeries(TimeFrame.Hour);
            serieshour4 = MarketData.GetSeries(TimeFrame.Hour4);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

            ema = Indicators.MovingAverage(MarketSeries.Close, Period, MovingAverageType.Exponential);
            emahour = Indicators.MovingAverage(serieshour.Close, Period, MovingAverageType.Exponential);
            emahour4 = Indicators.MovingAverage(serieshour4.Close, Period, MovingAverageType.Exponential);
            emadaily = Indicators.MovingAverage(seriesdaily.Close, Period, MovingAverageType.Exponential);

        }

        public override void Calculate(int index)
        {
            EMA[index] = ema.Result[index];

            var indexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour != -1)
                EMAhour[index] = emahour.Result[indexhour];

            var indexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour4 != -1)
                EMAhour4[index] = emahour4.Result[indexhour4];

            var indexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexdaily != -1)
                EMAdaily[index] = emadaily.Result[indexdaily];


        }
    }
}

