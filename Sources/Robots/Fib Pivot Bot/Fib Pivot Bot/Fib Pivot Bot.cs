using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FibPivotBot : Robot
    {

        [Parameter(DefaultValue = "Pivot Bot")]
        public string cBotLabel { get; set; }

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

        [Parameter("Period", DefaultValue = 9)]
        public int Period { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12)]
        public int ShortCycle { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 0, Step = 1)]
        public double BreakEvenGain { get; set; }

        [Parameter("Number of Pivots", DefaultValue = 3, MinValue = 1, MaxValue = 3)]
        public int NoPiv { get; set; }

        [Parameter("DrawingWidth", DefaultValue = 50, MaxValue = 100)]
        public int DrawingWidth { get; set; }

        private FiboPivotPointsIntraDay _FPP;
        private RelativeStrengthIndex rsi;
        private MacdHistogram _macd;
        private const string label = "La Tortuga";

        protected override void OnStart()
        {
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            _FPP = Indicators.GetIndicator<FiboPivotPointsIntraDay>(MarketSeries);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);


            //if (MarketSeries.Open.LastValue > _FPP.Pivot.LastValue && longPosition == null)
            if (_macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising() && MarketSeries.Low.LastValue > _FPP.Pivot.LastValue && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
            //else if (MarketSeries.Close.LastValue < _FPP.Pivot.LastValue && shortPosition == null)
            else if (_macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling() && MarketSeries.High.LastValue < _FPP.Pivot.LastValue && shortPosition == null)
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
