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
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC)]
    public class ADXRSignal : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Output("ADXR", Color = Colors.Turquoise)]
        public IndicatorDataSeries ADXR { get; set; }

        [Output("DIMINUS", Color = Colors.Red)]
        public IndicatorDataSeries DIMINUS { get; set; }

        [Output("DIPLUS", Color = Colors.Blue)]
        public IndicatorDataSeries DIPLUS { get; set; }

        [Output("ADXRHOUR", Color = Colors.Turquoise)]
        public IndicatorDataSeries ADXRHOUR { get; set; }

        [Output("DIMINUSHOUR", Color = Colors.Red)]
        public IndicatorDataSeries DIMINUSHOUR { get; set; }

        [Output("DIPLUSHOUR", Color = Colors.Blue)]
        public IndicatorDataSeries DIPLUSHOUR { get; set; }

        [Output("ADXR4", Color = Colors.Turquoise)]
        public IndicatorDataSeries ADXRHOUR4 { get; set; }

        [Output("DIMINUS4", Color = Colors.Red)]
        public IndicatorDataSeries DIMINUSHOUR4 { get; set; }

        [Output("DIPLUS4", Color = Colors.Blue)]
        public IndicatorDataSeries DIPLUSHOUR4 { get; set; }

        [Output("ADXRDAILY", Color = Colors.Turquoise)]
        public IndicatorDataSeries ADXRDAILY { get; set; }

        [Output("DIMINUSDAILY", Color = Colors.Red)]
        public IndicatorDataSeries DIMINUSDAILY { get; set; }

        [Output("DIPLUSDAILY", Color = Colors.Blue)]
        public IndicatorDataSeries DIPLUSDAILY { get; set; }





        private MarketSeries series5;
        private MarketSeries series15;
        private MarketSeries serieshour;
        private MarketSeries serieshour4;
        private MarketSeries seriesdaily;


        private ADXR adxr;
        private ADXR adxr5;
        private ADXR adxr15;
        private ADXR adxrhour;
        private ADXR adxrhour4;
        private ADXR adxrdaily;


        protected override void Initialize()
        {
            series5 = MarketData.GetSeries(TimeFrame.Minute5);
            series15 = MarketData.GetSeries(TimeFrame.Minute15);
            serieshour = MarketData.GetSeries(TimeFrame.Hour);
            serieshour4 = MarketData.GetSeries(TimeFrame.Hour4);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

            adxr = Indicators.GetIndicator<ADXR>(Source, interval);
            adxr5 = Indicators.GetIndicator<ADXR>(series5.Close, interval);
            adxr15 = Indicators.GetIndicator<ADXR>(series15.Close, interval);
            adxrhour = Indicators.GetIndicator<ADXR>(serieshour.Close, interval);
            adxrhour4 = Indicators.GetIndicator<ADXR>(serieshour4.Close, interval);
            adxrdaily = Indicators.GetIndicator<ADXR>(seriesdaily.Close, interval);


        }

        public override void Calculate(int index)
        {
            ADXR[index] = adxr.adxr[index];
            DIMINUS[index] = adxr.diminus[index];
            DIPLUS[index] = adxr.diplus[index];

            var adrxindexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (adrxindexhour != -1)
                ADXRHOUR[index] = adxrhour.adxr[adrxindexhour];

            var diminusindexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diminusindexhour != -1)
                DIMINUSHOUR[index] = adxrhour.diminus[diminusindexhour];

            var diplusindexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diplusindexhour != -1)
                DIPLUSHOUR[index] = adxrhour.diplus[diplusindexhour];

            var adrxindexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (adrxindexhour4 != -1)
                ADXRHOUR4[index] = adxrhour4.adxr[adrxindexhour4];

            var diminusindexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diminusindexhour4 != -1)
                DIMINUSHOUR4[index] = adxrhour4.diminus[diminusindexhour4];

            var diplusindexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diplusindexhour4 != -1)
                DIPLUSHOUR4[index] = adxrhour4.diplus[diplusindexhour4];

            var adrxindexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (adrxindexdaily != -1)
                ADXRDAILY[index] = adxrdaily.adxr[adrxindexdaily];

            var diminusindexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diminusindexdaily != -1)
                DIMINUSDAILY[index] = adxrdaily.diminus[diminusindexhour4];

            var diplusindexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (diplusindexdaily != -1)
                DIPLUSDAILY[index] = adxrdaily.diplus[diplusindexdaily];




        }
    }
}
