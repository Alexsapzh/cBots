using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, ScalePrecision = 0)]
    [Levels(0, 10, 15, 20, 25, 30, 35, 40, 45, 50,
    55, 60, 70, 80, 90, 100, 200, 300)]
    public class PipsATRIndicator : Indicator
    {
        #region Indicator parameters
        [Parameter("Atr TimeFrame")]
        public TimeFrame AtrTimeFrame { get; set; }

        [Parameter("ATR Period", DefaultValue = 20)]
        public int AtrPeriod { get; set; }

        [Parameter("ATR MAType")]
        public MovingAverageType AtrMaType { get; set; }

        [Output("Pips ATR", Color = Colors.SteelBlue)]
        public IndicatorDataSeries Result { get; set; }
        #endregion

        private AverageTrueRange atr;
        MarketSeries AtrSeries;

        protected override void Initialize()
        {
            atr = Indicators.AverageTrueRange(MarketData.GetSeries(AtrTimeFrame), AtrPeriod, AtrMaType);
            AtrSeries = MarketData.GetSeries(AtrTimeFrame);
        }

        public override void Calculate(int index)
        {
            var index1 = GetIndexByDate(AtrSeries, MarketSeries.OpenTime[index]);
            if (index1 != -1)

                Result[index] = atr.Result[index1] / Symbol.PipSize;
        }

        private int GetIndexByDate(MarketSeries series, DateTime time)
        {
            for (int i = series.Close.Count - 1; i > 0; i--)
            {
                if (time == series.OpenTime[i])
                    return i;
            }
            return -1;
        }

    }
}
