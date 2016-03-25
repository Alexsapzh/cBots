using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HMABot : Robot
    {
        // Hull MA Indicator Period
        [Parameter("Hull Period", DefaultValue = 3, MinValue = 1)]
        public int HullPeriod { get; set; }

        [Parameter("MA Type")]
        public HMAType { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow Periods", DefaultValue = 10)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 5)]
        public int FastPeriods { get; set; }

        [Parameter("Quantity (Lots)", DefaultValue = 0.1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        private MovingAverage slowMa;
        private MovingAverage fastMa;
        private const string label = "HMABot";

        protected override void OnStart()
        {

            hull = Indicators.GetIndicator<HMA>(HullPeriod);

            // Identify existing trades from this instance
            foreach (var position in Positions)
            {
                if (position.Label == DragonID)
                    switch (position.TradeType)
                    {
                        case TradeType.Buy:
                            LongPositions++;
                            break;
                        case TradeType.Sell:
                            ShortPositions++;
                            break;
                    }
            }

        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find(label, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(label, Symbol, TradeType.Sell);

            var currentSlowMa = slowMa.Result.Last(0);
            var currentFastMa = fastMa.Result.Last(0);
            var previousSlowMa = slowMa.Result.Last(1);
            var previousFastMa = fastMa.Result.Last(1);

            if (previousSlowMa > previousFastMa && currentSlowMa <= currentFastMa && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, label);
            }
            else if (previousSlowMa < previousFastMa && currentSlowMa >= currentFastMa && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, label);
            }
        }

        private long VolumeInUnits
        {
            get { return Symbol.QuantityToVolume(Quantity); }
        }
    }
}
