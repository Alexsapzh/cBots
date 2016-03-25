// -------------------------------------------------------------------------------------------------
//
//    This code is a cAlgo API sample.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//
//    The "Sample Trend cBot" will buy when fast period moving average crosses the slow period moving average and sell when 
//    the fast period moving average crosses the slow period moving average. The orders are closed when an opposite signal 
//    is generated. There can only by one Buy or Sell order at any time.
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HMAbot : Robot
    {

        [Parameter("Slow Periods", DefaultValue = 31)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 6)]
        public int FastPeriods { get; set; }

        [Parameter("Quantity (Lots)", DefaultValue = 0.1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Stop Loss", DefaultValue = 40)]
        public int StopLossPips { get; set; }

        [Parameter("Take Profit", DefaultValue = 40)]
        public int TakeProfit { get; set; }

        [Output("HMAslow", Color = Colors.Red)]
        public IndicatorDataSeries HMAslow { get; set; }

        [Output("HMAfast", Color = Colors.Yellow)]
        public IndicatorDataSeries HMAfast { get; set; }

        private HMAslow hmaslow;
        private HMAfast hmafast;
        private const string label = "HMAbot";

        protected override void OnStart()
        {
            hmafast = Indicators.GetIndicator<HMAfast>(FastPeriods);
            hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriods);
        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find(label, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(label, Symbol, TradeType.Sell);

            var currenthmaslow = hmaslow.hmaslow.Last(0);
            var currenthmafast = hmafast.hmafast.Last(0);
            var previoushmaslow = hmaslow.hmaslow.Last(1);
            var previoushmafast = hmafast.hmafast.Last(1);

            if (previoushmaslow > previoushmafast && currenthmaslow <= currenthmafast && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, label, TakeProfit, StopLossPips);
            }
            else if (previoushmaslow < previoushmafast && currenthmaslow >= currenthmafast && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, label, TakeProfit, StopLossPips);
            }
        }

        private long VolumeInUnits
        {
            get { return Symbol.QuantityToVolume(Quantity); }
        }
    }
}
