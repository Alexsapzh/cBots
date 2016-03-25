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
        [Parameter("KAMA TimeFrame")]
        public TimeFrame KAMATimeFrame { get; set; }

        [Parameter("KAMA Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("KAMA FastPeriod", DefaultValue = 2)]
        public int FastPeriod { get; set; }

        [Parameter("KAMA SlowPeriod", DefaultValue = 30)]
        public int SlowPeriod { get; set; }

        //[Parameter(DefaultValue = 50)]
        //public int Period { get; set; }

        [Output("KMA", Color = Colors.Blue)]
        public IndicatorDataSeries KMA { get; set; }

        [Output("KAMA5", Color = Colors.Orange)]
        public IndicatorDataSeries KAMA5 { get; set; }

        [Output("KAMA15", Color = Colors.Red)]
        public IndicatorDataSeries KAMA15 { get; set; }

        [Output("KAMAHOUR", Color = Colors.White)]
        public IndicatorDataSeries KAMAHOUR { get; set; }

        

        private MarketSeries series5;
        private MarketSeries series15;
        private MarketSeries serieshour;

        private kama1 kama;
        private kama1 kama5;
        private kama1 kama15;
        private kama1 kamahour;

        protected override void Initialize()
        {
            series5 = MarketData.GetSeries(TimeFrame.Minute5);
            series15 = MarketData.GetSeries(TimeFrame.Minute15);
            serieshour = MarketData.GetSeries(TimeFrame.Hour);

            kama = Indicators.GetIndicator<kama1>(Period, FastPeriod, SlowPeriod);
            kama5 = Indicators.GetIndicator<kama1>(series5, Period, FastPeriod, SlowPeriod);
            kama15 = Indicators.GetIndicator<kama1>(series15, Period, FastPeriod, SlowPeriod);
            kamahour = Indicators.GetIndicator<kama1>(serieshour, Period, FastPeriod, SlowPeriod);


        }

        public override void Calculate(int index)
        {
            KMA[index] = kama.Result[index];

            var index5 = series5.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index5 != -1)
                KAMA5[index] = kama5.Result[index5];

            var index15 = series15.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index15 != -1)
                KAMA15[index] = kama15.Result[index15];

            var indexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour != -1)
                KAMAHOUR[index] = kamahour.Result[indexhour];

        }
    }
}
