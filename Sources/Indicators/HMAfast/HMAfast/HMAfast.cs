using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class HMAfast : Indicator
    {
        [Output("FastHMA", Color = Colors.Orange)]
        public IndicatorDataSeries hmafast { get; set; }

        [Parameter(DefaultValue = 5)]
        public int FastPeriod { get; set; }

        private IndicatorDataSeries diff;
        private WeightedMovingAverage wma1;
        private WeightedMovingAverage wma2;
        private WeightedMovingAverage wma3;

        protected override void Initialize()
        {
            diff = CreateDataSeries();
            wma1 = Indicators.WeightedMovingAverage(MarketSeries.Close, (int)FastPeriod / 2);
            wma2 = Indicators.WeightedMovingAverage(MarketSeries.Close, FastPeriod);
            wma3 = Indicators.WeightedMovingAverage(diff, (int)Math.Sqrt(FastPeriod));

        }

        public override void Calculate(int index)
        {
            double var1 = 2 * wma1.Result[index];
            double var2 = wma2.Result[index];

            diff[index] = var1 - var2;

            hmafast[index] = wma3.Result[index];

        }
    }
}
