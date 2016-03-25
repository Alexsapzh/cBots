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
    public class KAMASignal : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 2)]
        public int Fast { get; set; }

        [Parameter(DefaultValue = 30)]
        public int Slow { get; set; }

        [Parameter(DefaultValue = 10)]
        public int Period { get; set; }

        [Output("KMA", Color = Colors.Blue)]
        public IndicatorDataSeries KMA { get; set; }

        [Output("KAMA5", Color = Colors.Orange)]
        public IndicatorDataSeries KAMA5 { get; set; }

        [Output("KAMA15", Color = Colors.Red)]
        public IndicatorDataSeries KAMA15 { get; set; }

        [Output("KAMAHOUR", Color = Colors.White)]
        public IndicatorDataSeries KAMAHOUR { get; set; }

        [Output("KAMAHOUR4", Color = Colors.Gold)]
        public IndicatorDataSeries KAMAHOUR4 { get; set; }



        private MarketSeries series5;
        private MarketSeries series15;
        private MarketSeries serieshour;
        private MarketSeries serieshour4;

        private KAMA kama;
        private KAMA kama5;
        private KAMA kama15;
        private KAMA kamahour;
        private KAMA kamahour4;

        protected override void Initialize()
        {
            series5 = MarketData.GetSeries(TimeFrame.Minute5);
            series15 = MarketData.GetSeries(TimeFrame.Minute15);
            serieshour = MarketData.GetSeries(TimeFrame.Hour);
            serieshour4 = MarketData.GetSeries(TimeFrame.Hour4);

            kama = Indicators.GetIndicator<KAMA>(Source, Fast, Slow, Period);
            kama5 = Indicators.GetIndicator<KAMA>(series5.Close, Fast, Slow, Period);
            kama15 = Indicators.GetIndicator<KAMA>(series15.Close, Fast, Slow, Period);
            kamahour = Indicators.GetIndicator<KAMA>(serieshour.Close, Fast, Slow, Period);
            kamahour4 = Indicators.GetIndicator<KAMA>(serieshour4.Close, Fast, Slow, Period);


        }

        public override void Calculate(int index)
        {
            KMA[index] = kama.kama[index];

            var index5 = series5.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index5 != -1)
                KAMA5[index] = kama5.kama[index5];

            var index15 = series15.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (index15 != -1)
                KAMA15[index] = kama15.kama[index15];

            var indexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour != -1)
                KAMAHOUR[index] = kamahour.kama[indexhour];

            var indexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (indexhour4 != -1)
                KAMAHOUR4[index] = kamahour4.kama[indexhour4];

        }
    }
}
