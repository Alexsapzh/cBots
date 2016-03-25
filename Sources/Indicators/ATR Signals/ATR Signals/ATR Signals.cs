
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC)]
    public class ATRSignals : Indicator
    {
        [Parameter(DefaultValue = 21)]
        public int PeriodAtr { get; set; }

        [Parameter(DefaultValue = 0.002)]
        public double ATRValue { get; set; }

        [Output("ATR", Color = Colors.Yellow)]
        public IndicatorDataSeries ATR { get; set; }

        [Output("ATR 1m", Color = Colors.Orange)]
        public IndicatorDataSeries ATR1m { get; set; }

        [Output("ATR 5m", Color = Colors.Red)]
        public IndicatorDataSeries ATR5m { get; set; }

        [Output("ATR 15m", Color = Colors.Blue)]
        public IndicatorDataSeries ATR15m { get; set; }

        [Output("ATR 60m", Color = Colors.Purple)]
        public IndicatorDataSeries ATR60m { get; set; }

        [Output("ATR Daily", Color = Colors.Purple)]
        public IndicatorDataSeries ATRdaily { get; set; }

        private MarketSeries series1m;
        private MarketSeries series5m;
        private MarketSeries series15m;
        private MarketSeries series60m;
        private MarketSeries seriesdaily;

        private AverageTrueRange atr;
        private AverageTrueRange atr1m;
        private AverageTrueRange atr5m;
        private AverageTrueRange atr15m;
        private AverageTrueRange atr60m;
        private AverageTrueRange atrdaily;

        protected override void Initialize()
        {
            series1m = MarketData.GetSeries(TimeFrame.Minute);
            series5m = MarketData.GetSeries(TimeFrame.Minute5);
            series15m = MarketData.GetSeries(TimeFrame.Minute15);
            series60m = MarketData.GetSeries(TimeFrame.Hour);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

            atr = Indicators.AverageTrueRange(21, MovingAverageType.Exponential);
            atr1m = Indicators.AverageTrueRange(series1m, 21, MovingAverageType.Exponential);
            atr5m = Indicators.AverageTrueRange(series5m, 21, MovingAverageType.Exponential);
            atr15m = Indicators.AverageTrueRange(series15m, 21, MovingAverageType.Exponential);
            atr60m = Indicators.AverageTrueRange(series60m, 21, MovingAverageType.Exponential);
            atrdaily = Indicators.AverageTrueRange(seriesdaily, 21, MovingAverageType.Exponential);
        }

        public override void Calculate(int index)
        {
            ATR[index] = atr.Result[index];

            var index1m = series1m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index1m != -1)
                ATR1m[index] = atr1m.Result[index1m];

            var index5m = series5m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index5m != -1)
                ATR5m[index] = atr5m.Result[index5m];

            var index15m = series15m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index15m != -1)
                ATR15m[index] = atr15m.Result[index15m];

            var index60m = series60m.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index60m != -1)
                ATR60m[index] = atr60m.Result[index60m];

            var indexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index60m != -1)
                ATRdaily[index] = atrdaily.Result[indexdaily];
        }
    }
}

