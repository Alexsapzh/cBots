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
    public class ElConejoV3 : Robot
    {

        [Parameter(DefaultValue = "El Conejo")]
        public string cBotLabel { get; set; }

        [Parameter(DefaultValue = 0.07)]
        public double Alpha { get; set; }

        [Parameter("Slow Periods", DefaultValue = 31)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 6)]
        public int FastPeriods { get; set; }

        [Parameter("Quantity (Lots)", DefaultValue = 0.1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 100)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 100)]
        public int TakeProfit { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 10)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 10)]
        public int TrailingStop { get; set; }

        [Parameter("MinBalance", DefaultValue = 5000)]
        public double MinBalance { get; set; }

        [Parameter("MinLoss", DefaultValue = -200.0)]
        public double MinLoss { get; set; }

        [Parameter(DefaultValue = 3)]
        public int MaxPositions { get; set; }

        [Output("HMAslow", Color = Colors.Red)]
        public IndicatorDataSeries HMAslow { get; set; }

        [Output("HMAfast", Color = Colors.Yellow)]
        public IndicatorDataSeries HMAfast { get; set; }

        [Parameter("Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12)]
        public int ShortCycle { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("RSI Periods", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 0, Step = 1)]
        public double BreakEvenGain { get; set; }

        private SinewaveSupportResistance _SSR;
        private RelativeStrengthIndex rsi;
        private MacdHistogram _macd;
        private HMAslow hmaslow;
        private HMAfast hmafast;
        private string label;

        protected override void OnStart()
        {
            label = "Conejo V3 " + Symbol.Code + " " + TimeFrame.ToString();
            hmafast = Indicators.GetIndicator<HMAfast>(FastPeriods);
            hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriods);
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            _SSR = Indicators.GetIndicator<SinewaveSupportResistance>(MarketSeries, 0.07);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnTick()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            var longPosition = Positions.Find(label, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(label, Symbol, TradeType.Sell);

            var currenthmaslow = hmaslow.hmaslow.Last(0);
            var currenthmafast = hmafast.hmafast.Last(0);
            var previoushmaslow = hmaslow.hmaslow.Last(1);
            var previoushmafast = hmafast.hmafast.Last(1);

            if (rsi.Result.LastValue < 20 && _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising() && previoushmaslow > previoushmafast && MarketSeries.Low.LastValue <= _SSR.Support.LastValue && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, label, StopLoss, TakeProfit);
            }
            else if (rsi.Result.LastValue > 80 && _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling() && previoushmaslow < previoushmafast && MarketSeries.High.LastValue >= _SSR.Resistance.LastValue && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, label, StopLoss, TakeProfit);

                // Some condition to close all positions
                if (Account.Balance < MinBalance)
                    foreach (var position in cBotPositions)
                        ClosePosition(position);

                // Some condition to close one position
                foreach (var position in cBotPositions)
                    if (position.GrossProfit < MinLoss)
                        ClosePosition(position);

                // Trailing Stop for all positions
                SetTrailingStop();
            }
        }

        private void PositionsOnOpened(PositionOpenedEventArgs obj)
        {
            Position openedPosition = obj.Position;
            if (openedPosition.Label != label)
                return;

            Print("position opened at {0}", openedPosition.EntryPrice);
        }

        private void PositionsOnClosed(PositionClosedEventArgs obj)
        {
            Position closedPosition = obj.Position;
            if (closedPosition.Label != label)
                return;

            Print("position closed with {0} gross profit", closedPosition.GrossProfit);
        }


        /// <summary>
        /// When the profit in pips is above or equal to Trigger the stop loss will start trailing the spot price.
        /// TrailingStop defines the number of pips the Stop Loss trails the spot price by. 
        /// If Trigger is 0 trailing will begin immediately. 
        /// </summary>
        private void SetTrailingStop()
        {
            var sellPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Sell);

            foreach (Position position in sellPositions)
            {
                double distance = position.EntryPrice - Symbol.Ask;

                if (distance < Trigger * Symbol.PipSize)
                    continue;

                double newStopLossPrice = Symbol.Ask + TrailingStop * Symbol.PipSize;

                if (position.StopLoss == null || newStopLossPrice < position.StopLoss)
                    ModifyPosition(position, newStopLossPrice, position.TakeProfit);
            }

            var buyPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Buy);

            foreach (Position position in buyPositions)
            {
                double distance = Symbol.Bid - position.EntryPrice;

                if (distance < Trigger * Symbol.PipSize)
                    continue;

                double newStopLossPrice = Symbol.Bid - TrailingStop * Symbol.PipSize;
                if (position.StopLoss == null || newStopLossPrice > position.StopLoss)
                    ModifyPosition(position, newStopLossPrice, position.TakeProfit);
            }
        }
        private long VolumeInUnits
        {
            get { return Symbol.QuantityToVolume(Quantity); }
        }
    }
}
