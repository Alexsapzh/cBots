
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class SignalFibGrid : Indicator
    {
        [Parameter(DefaultValue = 20)]
        public int Period { get; set; }

        [Output("EMA", Color = Colors.Yellow)]
        public IndicatorDataSeries EMA { get; set; }

        [Output("EMA 1m", Color = Colors.Orange)]
        public IndicatorDataSeries EMA1m { get; set; }

        [Output("EMA 5m", Color = Colors.Red)]
        public IndicatorDataSeries EMA5m { get; set; }

        [Output("EMA 15m", Color = Colors.Blue)]
        public IndicatorDataSeries EMA15m { get; set; }

        [Output("EMA 60m", Color = Colors.Purple)]
        public IndicatorDataSeries EMA60m { get; set; }

        [Output("EMA Daily", Color = Colors.Purple)]
        public IndicatorDataSeries EMAdaily { get; set; }

        private MarketSeries series1m;
        private MarketSeries series5m;
        private MarketSeries series15m;
        private MarketSeries series60m;
        private MarketSeries seriesdaily;

        private MovingAverage ema;
        private MovingAverage ema1m;
        private MovingAverage ema5m;
        private MovingAverage ema15m;
        private MovingAverage ema60m;
        private MovingAverage emadaily;

        protected override void Initialize()
        {
            series1m = MarketData.GetSeries(TimeFrame.Minute);
            series5m = MarketData.GetSeries(TimeFrame.Minute5);
            series15m = MarketData.GetSeries(TimeFrame.Minute15);
            series60m = MarketData.GetSeries(TimeFrame.Hour);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

            ema = Indicators.MovingAverage(MarketSeries.Close, 20, MovingAverageType.Exponential);
            ema1m = Indicators.MovingAverage(series1m.Close, 20, MovingAverageType.Exponential);
            ema5m = Indicators.MovingAverage(series5m.Close, 20, MovingAverageType.Exponential);
            ema15m = Indicators.MovingAverage(series15m.Close, 20, MovingAverageType.Exponential);
            ema60m = Indicators.MovingAverage(series60m.Close, 20, MovingAverageType.Exponential);
            emadaily = Indicators.MovingAverage(seriesdaily.Close, 20, MovingAverageType.Exponential);
        }

        public override void Calculate(int index)
        {
            EMA[index] = ema.Result[index];

            var index1m = series1m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index1m != -1)
                EMA1m[index] = ema1m.Result[index1m];

            var index5m = series5m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index5m != -1)
                EMA5m[index] = ema5m.Result[index5m];

            var index15m = series15m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index15m != -1)
                EMA15m[index] = ema15m.Result[index15m];

            var index60m = series60m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index60m != -1)
                EMA60m[index] = ema60m.Result[index60m];

            var indexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index60m != -1)
                EMAdaily[index] = emadaily.Result[indexdaily];
        }
    }
}

