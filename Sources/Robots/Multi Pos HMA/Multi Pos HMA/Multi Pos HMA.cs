using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class MultiPosHMA : Robot
    {

        private HMAslow _hmaslow;

        [Parameter(DefaultValue = "Multi Pos SMA")]
        public string cBotLabel { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("HMA Slow Periods", DefaultValue = 31, MinValue = 2, MaxValue = 150, Step = 1)]
        public int SlowPeriods { get; set; }

        [Parameter(DefaultValue = 1000)]
        public int Volume { get; set; }

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

        [Parameter("Add Position", DefaultValue = 5)]
        public double AddNewPos { get; set; }

        [Parameter(DefaultValue = 3)]
        public int MaxPositions { get; set; }

        protected override void OnStart()
        {
            cBotLabel = "Colonel V1 " + Symbol.Code + " " + TimeFrame.ToString() + " / ";
            _hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriods);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            // Condition to Buy
            if (MarketSeries.Low.LastValue >= _hmaslow.hmaslow.Last(0) && _hmaslow.hmaslow.IsRising())
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
            else if (MarketSeries.High.LastValue <= _hmaslow.hmaslow.Last(0) && _hmaslow.hmaslow.IsFalling())
                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);


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

        private void AddPosition()
        {
            var sellPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Sell);

            foreach (Position position in sellPositions)
            {
                if (position.GrossProfit > AddNewPos)
                    ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
            }

            var buyPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Buy);

            foreach (Position position in buyPositions)
            {
                if (position.GrossProfit > AddNewPos)
                    ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
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
    }
}
