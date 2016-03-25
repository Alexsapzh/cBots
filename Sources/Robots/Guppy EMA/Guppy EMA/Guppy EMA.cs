using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ADXROnly : Robot
    {

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

        [Parameter("Data Source")]
        public DataSeries Price { get; set; }

        [Parameter("Exp Fast Periods 13", DefaultValue = 13)]
        public int FastPeriods13 { get; set; }

        [Parameter("Exp Fast Periods 12", DefaultValue = 12)]
        public int FastPeriods12 { get; set; }

        [Parameter("Exp Fast Periods 11", DefaultValue = 11)]
        public int FastPeriods11 { get; set; }

        [Parameter("Exp Fast Periods 10", DefaultValue = 10)]
        public int FastPeriods10 { get; set; }

        [Parameter("Exp Fast Periods 9", DefaultValue = 9)]
        public int FastPeriods9 { get; set; }

        [Parameter("Exp Fast Periods 8", DefaultValue = 8)]
        public int FastPeriods8 { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 0, Step = 1)]
        public double BreakEvenGain { get; set; }

        private string cBotLabel;
        private ExponentialMovingAverage _emaFast13;
        private ExponentialMovingAverage _emaFast12;
        private ExponentialMovingAverage _emaFast11;
        private ExponentialMovingAverage _emaFast10;
        private ExponentialMovingAverage _emaFast9;
        private ExponentialMovingAverage _emaFast8;



        protected override void OnStart()
        {
            cBotLabel = "Guppy EMA " + Symbol.Code + " " + TimeFrame.ToString();
            _emaFast13 = Indicators.ExponentialMovingAverage(Price, 13);
            _emaFast12 = Indicators.ExponentialMovingAverage(Price, 12);
            _emaFast11 = Indicators.ExponentialMovingAverage(Price, 11);
            _emaFast10 = Indicators.ExponentialMovingAverage(Price, 10);
            _emaFast9 = Indicators.ExponentialMovingAverage(Price, 9);
            _emaFast8 = Indicators.ExponentialMovingAverage(Price, 8);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);

            var EMA13 = _emaFast13.Result.LastValue;
            var EMA12 = _emaFast12.Result.LastValue;
            var EMA11 = _emaFast11.Result.LastValue;
            var EMA10 = _emaFast10.Result.LastValue;
            var EMA9 = _emaFast9.Result.LastValue;
            var EMA8 = _emaFast8.Result.LastValue;

            //var emalong = MarketSeries.Close.Last(1) > _emaSlow.Result.LastValue;
            //var emashort = MarketSeries.Close.Last(1) < _emaSlow.Result.LastValue;

            var emalong13 = MarketSeries.Open.LastValue > _emaFast13.Result.LastValue;
            var emalong12 = MarketSeries.Open.LastValue > _emaFast12.Result.LastValue;
            var emalong11 = MarketSeries.Open.LastValue > _emaFast11.Result.LastValue;
            var emalong10 = MarketSeries.Open.LastValue > _emaFast10.Result.LastValue;
            var emalong9 = MarketSeries.Open.LastValue > _emaFast9.Result.LastValue;
            var emalong8 = MarketSeries.Open.LastValue > _emaFast8.Result.LastValue;

            var emashort13 = MarketSeries.Open.LastValue < _emaFast13.Result.LastValue;
            var emashort12 = MarketSeries.Open.LastValue < _emaFast12.Result.LastValue;
            var emashort11 = MarketSeries.Open.LastValue < _emaFast11.Result.LastValue;
            var emashort10 = MarketSeries.Open.LastValue < _emaFast10.Result.LastValue;
            var emashort9 = MarketSeries.Open.LastValue < _emaFast9.Result.LastValue;
            var emashort8 = MarketSeries.Open.LastValue < _emaFast8.Result.LastValue;

            //BUY & SELL LOGIC

            if (emalong13 && emalong12 && emalong11 && emalong10 && emalong9 && emalong8 && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
            else if (emashort13 && emashort12 && emashort11 && emashort10 && emashort9 && emashort8 && shortPosition == null)
            {
                if (shortPosition != null)
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


        //private void CloseTrade()
        //{


        //foreach (Position position in closelong)
        //{
        //if (MarketSeries.Close.Last(0) < _emaFast.Result.Last(0))
        //ClosePosition(closelong);
        //}
        //}



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
