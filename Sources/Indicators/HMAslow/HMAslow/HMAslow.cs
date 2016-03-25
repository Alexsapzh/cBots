using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class HMAslow : Indicator
    {
        [Output("SlowHMA", Color = Colors.Orange)]
        public IndicatorDataSeries hmaslow { get; set; }

        [Parameter(DefaultValue = 31)]
        public int SlowPeriod { get; set; }

        private IndicatorDataSeries diff;
        private WeightedMovingAverage wma1;
        private WeightedMovingAverage wma2;
        private WeightedMovingAverage wma3;

        protected override void Initialize()
        {
            diff = CreateDataSeries();
            wma1 = Indicators.WeightedMovingAverage(MarketSeries.Close, (int)SlowPeriod / 2);
            wma2 = Indicators.WeightedMovingAverage(MarketSeries.Close, SlowPeriod);
            wma3 = Indicators.WeightedMovingAverage(diff, (int)Math.Sqrt(SlowPeriod));

        }

        public override void Calculate(int index)
        {
            double var1 = 2 * wma1.Result[index];
            double var2 = wma2.Result[index];

            diff[index] = var1 - var2;

            hmaslow[index] = wma3.Result[index];

        }
    }
}
