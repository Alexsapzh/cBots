using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LaTortugaV4Shift : Robot
    {

        [Parameter(DefaultValue = "La Tortuga V4 Shift")]
        public string cBotLabel { get; set; }

        [Parameter("Slow Periods", DefaultValue = 31, MinValue = 26, MaxValue = 200, Step = 1)]
        public int SlowPeriod { get; set; }

        [Parameter("Fast Periods", DefaultValue = 5, MinValue = 1, MaxValue = 26, Step = 1)]
        public int FastPeriod { get; set; }

        [Parameter("Slow Shift", DefaultValue = -2, MinValue = -100, MaxValue = 500)]
        public int SlowShift { get; set; }

        [Parameter("Fast Shift", DefaultValue = 0, MinValue = -100, MaxValue = 500)]
        public int FastShift { get; set; }

        [Parameter("HTF Shift", DefaultValue = 0, MinValue = -100, MaxValue = 500)]
        public int HTFShift { get; set; }

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

        [Parameter("MACD Period", DefaultValue = 9, MinValue = 1, MaxValue = 100, Step = 1)]
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

        [Parameter("HMA HTF Period", DefaultValue = 1, MinValue = 2, MaxValue = 200, Step = 1)]
        public double HTF_Period { get; set; }


        private MacdHistogram _macd;
        private HMASlowShift _hmaslowshift;
        private HMAFastShift _hmafastshift;
        private RelativeStrengthIndex rsi;

        private const string label = "La Tortuga V4 Shift";

        // HMA Signal
        private MarketSeries HmaDaySeries;
        private HMAHTFSHIFT hmaSignal;

        protected override void OnStart()
        {
            cBotLabel = "La Tortuga V4 Shift" + " " + Symbol.Code;
            _hmafastshift = Indicators.GetIndicator<HMAFastShift>(FastPeriod, FastShift);
            _hmaslowshift = Indicators.GetIndicator<HMASlowShift>(SlowPeriod, SlowShift);
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            HmaDaySeries = MarketData.GetSeries(TimeFrame.Hour4);
            hmaSignal = Indicators.GetIndicator<HMAHTFSHIFT>(HmaDaySeries, 21, 0, false, false, 3, false, 24);


            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }


        protected override void OnBar()
        {


            var cBotPositions = Positions.FindAll(cBotLabel);

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);

            var currenthmaslow = _hmaslowshift.hmaslow.Last(0);
            var currenthmafast = _hmafastshift.hmafast.Last(0);
            var previoushmaslow = _hmaslowshift.hmaslow.Last(1);
            var previoushmafast = _hmafastshift.hmafast.Last(1);

            double i = hmaSignal.hma.LastValue;

            // HMA & MACD
            //if (hmaSignal.IsBullish && _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising() && previoushmaslow > previoushmafast && currenthmaslow <= currenthmafast && longPosition == null)
            // HMA & MACD & RSI
            if (hmaSignal.IsBullish && rsi.Result.LastValue < 40 && _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising() && previoushmaslow > previoushmafast && currenthmaslow <= currenthmafast && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
            // HMA & MACD
            //else if (hmaSignal.IsBearish && _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling() && previoushmaslow < previoushmafast && currenthmaslow >= currenthmafast && shortPosition == null)
            // HMA & MACD & RSI
            else if (hmaSignal.IsBearish && rsi.Result.LastValue > 60 && _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling() && previoushmaslow < previoushmafast && currenthmaslow >= currenthmafast && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }


            {
                // Trailing Stop for all positions
                SetTrailingStop();
            }
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
