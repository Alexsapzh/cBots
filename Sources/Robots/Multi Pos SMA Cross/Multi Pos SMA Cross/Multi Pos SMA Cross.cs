using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class MultiPosSMACross : Robot
    {
        private MovingAverage _fastMa;
        private MovingAverage _slowMa;

        [Parameter(DefaultValue = "Multi Pos SMA")]
        public string cBotLabel { get; set; }

        [Parameter()]
        public DataSeries SourceSeries { get; set; }

        [Parameter("MA Type")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Slow Periods", DefaultValue = 10)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 5)]
        public int FastPeriods { get; set; }

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
            _fastMa = Indicators.MovingAverage(SourceSeries, FastPeriods, MAType);
            _slowMa = Indicators.MovingAverage(SourceSeries, SlowPeriods, MAType);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            var currentSlowMa = _slowMa.Result.Last(0);
            var currentFastMa = _fastMa.Result.Last(0);

            var previousSlowMa = _slowMa.Result.Last(1);
            var previousFastMa = _fastMa.Result.Last(1);

            // Condition to Buy
            if (previousSlowMa > previousFastMa && currentSlowMa <= currentFastMa)
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
            else if (previousSlowMa < previousFastMa && currentSlowMa >= currentFastMa)
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
