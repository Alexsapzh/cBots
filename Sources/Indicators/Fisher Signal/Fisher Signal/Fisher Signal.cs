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
    public class FisherSignal : Indicator
    {
        //[Parameter()]
        //public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 13, MinValue = 2)]
        public int Len { get; set; }

        [Output("FISH15", Color = Colors.Blue)]
        public IndicatorDataSeries FISH15 { get; set; }

        [Output("TRIGGER15", Color = Colors.Red)]
        public IndicatorDataSeries TRIGGER15 { get; set; }

        [Output("FISH", Color = Colors.Blue)]
        public IndicatorDataSeries FISH { get; set; }

        [Output("TRIGGER", Color = Colors.Red)]
        public IndicatorDataSeries TRIGGER { get; set; }

        [Output("FISHHOUR", Color = Colors.Blue)]
        public IndicatorDataSeries FISHHOUR { get; set; }

        [Output("TRIGGERHOUR", Color = Colors.Red)]
        public IndicatorDataSeries TRIGGERHOUR { get; set; }

        [Output("FISHHOUR4", Color = Colors.Blue)]
        public IndicatorDataSeries FISHHOUR4 { get; set; }

        [Output("TRIGGERHOUR4", Color = Colors.Red)]
        public IndicatorDataSeries TRIGGERHOUR4 { get; set; }

        [Output("FISHDAILY", Color = Colors.Blue)]
        public IndicatorDataSeries FISHDAILY { get; set; }

        [Output("TRIGGERDAILY", Color = Colors.Red)]
        public IndicatorDataSeries TRIGGERDAILY { get; set; }





        private MarketSeries series5;
        private MarketSeries series15;
        private MarketSeries serieshour;
        private MarketSeries serieshour4;
        private MarketSeries seriesdaily;


        private FisherTransform ft;
        private FisherTransform ft5;
        private FisherTransform ft15;
        private FisherTransform fthour;
        private FisherTransform fthour4;
        private FisherTransform ftdaily;


        protected override void Initialize()
        {
            series5 = MarketData.GetSeries(TimeFrame.Minute5);
            series15 = MarketData.GetSeries(TimeFrame.Minute15);
            serieshour = MarketData.GetSeries(TimeFrame.Hour);
            serieshour4 = MarketData.GetSeries(TimeFrame.Hour4);
            seriesdaily = MarketData.GetSeries(TimeFrame.Daily);

//Indicators.GetIndicator<FisherTransform>(Len);

            ft = Indicators.GetIndicator<FisherTransform>(Len);
            ft5 = Indicators.GetIndicator<FisherTransform>(series5, Len);
            ft15 = Indicators.GetIndicator<FisherTransform>(series15, Len);
            fthour = Indicators.GetIndicator<FisherTransform>(serieshour, Len);
            fthour4 = Indicators.GetIndicator<FisherTransform>(serieshour4, Len);
            ftdaily = Indicators.GetIndicator<FisherTransform>(seriesdaily, Len);


        }

        public override void Calculate(int index)
        {
            FISH[index] = ft.Fish[index];
            TRIGGER[index] = ft.trigger[index];

            var fishindex15 = series15.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (fishindex15 != -1)
                FISH15[index] = ft15.Fish[fishindex15];

            var triggerindex15 = series15.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (triggerindex15 != -1)
                TRIGGER15[index] = ft15.trigger[triggerindex15];

            var fishindexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (fishindexhour != -1)
                FISHHOUR[index] = fthour.Fish[fishindexhour];

            var triggerindexhour = serieshour.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (triggerindexhour != -1)
                TRIGGERHOUR[index] = fthour.trigger[triggerindexhour];

            var fishindexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (fishindexhour4 != -1)
                FISHHOUR4[index] = fthour4.Fish[fishindexhour4];

            var triggerindexhour4 = serieshour4.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (triggerindexhour4 != -1)
                TRIGGERHOUR4[index] = fthour4.trigger[triggerindexhour4];

            var fishindexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (fishindexdaily != -1)
                FISHDAILY[index] = ftdaily.Fish[fishindexdaily];

            var triggerindexdaily = seriesdaily.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            if (triggerindexdaily != -1)
                TRIGGERDAILY[index] = ftdaily.trigger[triggerindexdaily];

        }


    }
}
