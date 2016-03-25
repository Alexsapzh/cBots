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
    public class ElConejoV4 : Robot
    {

        [Parameter(DefaultValue = "El Conejo V4")]
        public string cBotLabel { get; set; }

        [Parameter("Slow Periods", DefaultValue = 31)]
        public int SlowPeriod { get; set; }

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

        [Parameter("Add Position", DefaultValue = 5, MinValue = 2, MaxValue = 20, Step = 1)]
        public double AddNewPos { get; set; }

        [Parameter(DefaultValue = 3)]
        public int MaxPositions { get; set; }

        [Parameter("Period", DefaultValue = 9, MinValue = 1, MaxValue = 100, Step = 1)]
        public int Period { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26, MinValue = 24, MaxValue = 50, Step = 1)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12, MinValue = 1, MaxValue = 23, Step = 1)]
        public int ShortCycle { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("RSI Periods", DefaultValue = 14, MinValue = 2, MaxValue = 25, Step = 1)]
        public int Periods { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 0, Step = 1)]
        public double BreakEvenGain { get; set; }

        private RelativeStrengthIndex rsi;
        private MacdHistogram _macd;
        private HMAslow _hmaslow;
        private bool _isTrigerred;

        protected override void OnStart()
        {
            _hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriod);
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnTick()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);

            if (rsi.Result.IsRising() && _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising() && _hmaslow.hmaslow.IsRising() && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
            else if (rsi.Result.IsFalling() && _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling() && _hmaslow.hmaslow.IsFalling() && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);

                {
                    // Trailing Stop for all positions
                    SetTrailingStop();
                }
            }
        }

        private Position position
        {

            get { return Positions.FirstOrDefault(pos => (pos.SymbolCode == Symbol.Code)); }
        }

        private void PositionsOnOpened(PositionOpenedEventArgs obj)
        {
            Position openedPosition = obj.Position;
            if (openedPosition.Label != cBotLabel)
                return;

            Print("position opened at {0}", openedPosition.EntryPrice);
        }

        private void PositionsOnClosed(PositionClosedEventArgs obj)
        {
            Position closedPosition = obj.Position;
            if (closedPosition.Label != cBotLabel)
                return;

            Print("position closed with {0} gross profit", closedPosition.GrossProfit);
        }


        private void AddPosition()
        {
            var sellPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Sell);

            foreach (Position position in sellPositions)
            {
                if (position.GrossProfit > AddNewPos)
                    ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }

            var buyPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Buy);

            foreach (Position position in buyPositions)
            {
                if (position.GrossProfit > AddNewPos)
                    ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
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
                if (!_isTrigerred)
                {
                    _isTrigerred = true;
                    Print("Trailing Stop Loss triggered...");
                }
                if (distance < Trigger * Symbol.PipSize)
                    continue;

                double newStopLossPrice = Math.Round(Symbol.Ask + TrailingStop * Symbol.PipSize);

                if (position.StopLoss == null || newStopLossPrice < position.StopLoss)
                    ModifyPosition(position, newStopLossPrice, position.TakeProfit);
            }

            var buyPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Buy);

            foreach (Position position in buyPositions)
            {
                double distance = Symbol.Bid - position.EntryPrice;
                if (!_isTrigerred)
                {
                    _isTrigerred = true;
                    Print("Trailing Stop Loss triggered...");
                }
                if (distance < Trigger * Symbol.PipSize)
                    continue;

                double newStopLossPrice = Math.Round(Symbol.Bid - TrailingStop * Symbol.PipSize);
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
