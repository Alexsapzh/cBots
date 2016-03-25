using System;
using System.Linq;
using System.Reflection;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using cAlgo.Lib;


namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class MultiTF_MA : Indicator
    {
        [Parameter("KAMA Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("KAMA FastPeriod", DefaultValue = 2)]
        public int FastPeriod { get; set; }

        [Parameter("KAMA SlowPeriod", DefaultValue = 30)]
        public int SlowPeriod { get; set; }

        [Output("KMA", Color = Colors.Yellow)]
        public IndicatorDataSeries KMA { get; set; }

        [Output("KMA5", Color = Colors.Orange)]
        public IndicatorDataSeries KMA5 { get; set; }

        [Output("KMA15", Color = Colors.Red)]
        public IndicatorDataSeries KMA15 { get; set; }

        [Output("KMA Hour", Color = Colors.Orange)]
        public IndicatorDataSeries KMAHOUR { get; set; }

        [Output("KMA Hour4", Color = Colors.Red)]
        public IndicatorDataSeries KMAHOUR4 { get; set; }

        [Output("KMA Daily", Color = Colors.Red)]
        public IndicatorDataSeries KMADAILY { get; set; }

        private MarketSeries series5;
        private MarketSeries series15;
        private MarketSeries serieshour;
        private MarketSeries serieshour4;
        private MarketSeries seriesdaily;

        private kama1 kma;
        private kama1 kma5;
        private kama1 kma15;
        private kama1 kmahour;
        private kama1 kmahour4;
        private kama1 kmadaily;

        protected override void Initialize()
        {
            series5 = MarketData.GetSeries(TimeFrame.Minute5);
            series15 = MarketData.GetSeries(TimeFrame.Minute15);
            serieshour = MarketData.GetSeries(TimeFrame.Hour);
            serieshour4 = MarketData.GetSeries(TimeFrame.Hour4);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

            //_kama = Indicators.GetIndicator<kama1>(Period, FastPeriod, SlowPeriod);
            //kma = Indicators.MovingAverage(MarketSeries.Close, Period, MovingAverageType.Triangular);

            kma = Indicators.GetIndicator<kama1>(MarketSeries.Close, Period, FastPeriod, SlowPeriod);
            kma5 = Indicators.GetIndicator<kama1>(series5.Close, Period, FastPeriod, SlowPeriod);
            kma15 = Indicators.GetIndicator<kama1>(series15.Close, FastPeriod, SlowPeriod);
            kmahour = Indicators.GetIndicator<kama1>(serieshour.Close, FastPeriod, SlowPeriod);
            kmahour4 = Indicators.GetIndicator<kama1>(serieshour4.Close, FastPeriod, SlowPeriod);
            kmadaily = Indicators.GetIndicator<kama1>(seriesdaily.Close, FastPeriod, SlowPeriod);


        }

        public override void Calculate(int index)
        {
            KMA[index] = kma.Result[index];

           var index5 = series5.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index5 != -1)
                KMA5[index] = kma5.Result[index5];

            var index15 = series15.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index15 != -1)
                KMA15[index] = kma15.Result[index15];

            var indexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour != -1)
                KMAHOUR[index] = kmahour.Result[indexhour];

            var indexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour4 != -1)
                KMAHOUR4[index] = kmahour4.Result[indexhour4];

            var indexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexdaily != -1)
                KMADAILY[index] = kmadaily.Result[indexdaily];
        }
    }
}
