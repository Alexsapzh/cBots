using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class Ribbon : Robot
    {


        private GuppyRibbon _ema;
        private Position _position;
        private string cBotLabel;

        [Parameter(DefaultValue = 10000)]
        public int Volume { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 100)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 100)]
        public int TakeProfit { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 10)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 10)]
        public int TrailingStop { get; set; }

        [Parameter("Exp Fast Periods 13", DefaultValue = 90)]
        public int FastPeriods13 { get; set; }
        [Parameter("Exp Fast Periods 12", DefaultValue = 80)]
        public int FastPeriods12 { get; set; }
        [Parameter("Exp Fast Periods 11", DefaultValue = 70)]
        public int FastPeriods11 { get; set; }
        [Parameter("Exp Fast Periods 10", DefaultValue = 60)]
        public int FastPeriods10 { get; set; }
        [Parameter("Exp Fast Periods 9", DefaultValue = 50)]
        public int FastPeriods9 { get; set; }
        [Parameter("Exp Fast Periods 8", DefaultValue = 40)]
        public int FastPeriods8 { get; set; }
        [Parameter("Exp Fast Periods 7", DefaultValue = 30)]
        public int FastPeriods7 { get; set; }
        [Parameter("Exp Fast Periods 6", DefaultValue = 20)]
        public int FastPeriods6 { get; set; }
        [Parameter("Exp Fast Periods 5", DefaultValue = 10)]
        public int FastPeriods5 { get; set; }
        [Parameter("Data Source")]
        public DataSeries Price { get; set; }

        protected override void OnStart()
        {
            cBotLabel = "Ribbon V1 " + Symbol.Code + " " + TimeFrame.ToString() + " / ";
            _ema = Indicators.GetIndicator<GuppyRibbon>(Price, FastPeriods13, FastPeriods12, FastPeriods11, FastPeriods10, FastPeriods9, FastPeriods8, FastPeriods7, FastPeriods6, FastPeriods5);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnTick()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            var ema13short = _ema.EMA13.LastValue > _ema.EMA12.LastValue;

            var ema12short = _ema.EMA12.LastValue > _ema.EMA11.LastValue;

            var ema11short = _ema.EMA11.LastValue > _ema.EMA10.LastValue;

            var ema10short = _ema.EMA10.LastValue > _ema.EMA9.LastValue;

            var ema9short = _ema.EMA9.LastValue > _ema.EMA8.LastValue;

            var ema8short = _ema.EMA8.LastValue > _ema.EMA7.LastValue;

            var ema7short = _ema.EMA7.LastValue > _ema.EMA6.LastValue;

            var ema6short = _ema.EMA6.LastValue > _ema.EMA5.LastValue;

            var ema13long = _ema.EMA13.LastValue < _ema.EMA12.LastValue;

            var ema12long = _ema.EMA12.LastValue < _ema.EMA11.LastValue;

            var ema11long = _ema.EMA11.LastValue < _ema.EMA10.LastValue;

            var ema10long = _ema.EMA10.LastValue < _ema.EMA9.LastValue;

            var ema9long = _ema.EMA9.LastValue < _ema.EMA8.LastValue;

            var ema8long = _ema.EMA8.LastValue < _ema.EMA7.LastValue;

            var ema7long = _ema.EMA7.LastValue < _ema.EMA6.LastValue;

            var ema6long = _ema.EMA6.LastValue < _ema.EMA5.LastValue;

            bool isLongPositionOpen = _position != null && _position.TradeType == TradeType.Buy;
            bool isShortPositionOpen = _position != null && _position.TradeType == TradeType.Sell;

            // Condition to Buy
            if (ema13long && ema12long && ema11long && ema10long && ema9long && ema8long && ema7long && ema6long && !isLongPositionOpen)
            {
                ClosePosition();
                Buy();
            }
            else if (ema13short && ema12short && ema11short && ema10short && ema9short && ema8short && ema7short && ema6short && !isShortPositionOpen)
            {
                ClosePosition();
                Sell();
            }

            {
                SetTrailingStop();
            }

        }

        private void ClosePosition()
        {
            if (_position == null)
                return;
            ClosePosition(_position);
            _position = null;
        }

        private void Buy()
        {
            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }

        private void Sell()
        {
            ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
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
