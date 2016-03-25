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

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Data Source")]
        public DataSeries Price { get; set; }


        [Parameter("Exp Fast Periods 1", DefaultValue = 5)]
        public int FastPeriods5 { get; set; }

        [Parameter("Exp Fast Periods 2", DefaultValue = 6)]
        public int FastPeriods6 { get; set; }

        [Parameter("Exp Slow Periods", DefaultValue = 200)]
        public int SlowPeriods { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 0, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 0, Step = 1)]
        public double BreakEvenGain { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Parameter("ADX Trend Strength", DefaultValue = 20, MinValue = 10, MaxValue = 30, Step = 1)]
        public int trend { get; set; }

        private string cBotLabel;
        private ExponentialMovingAverage _emaFast1;
        private ExponentialMovingAverage _emaFast2;
        private ExponentialMovingAverage _emaSlow;
        private ADXR _adx;

        protected override void OnStart()
        {
            cBotLabel = "ADXR " + Symbol.Code + " " + TimeFrame.ToString();
            _adx = Indicators.GetIndicator<ADXR>(Source, interval);
            _emaFast1 = Indicators.ExponentialMovingAverage(Price, 5);
            _emaFast2 = Indicators.ExponentialMovingAverage(Price, 6);
            _emaSlow = Indicators.ExponentialMovingAverage(_emaFast1.Result, SlowPeriods);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnTick()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);


            var currentDiPlus = _adx.diplus.Last(0);
            var currentDiMinus = _adx.diminus.Last(0);
            var previousDiPlus = _adx.diplus.Last(1);
            var previousDiMinus = _adx.diminus.Last(1);

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);

            var currentSlowEMA = _emaFast2.Result.Last(0);
            var currentFastEMA = _emaFast1.Result.Last(0);
            var previousSlowEMA = _emaFast2.Result.Last(1);
            var previousFastEMA = _emaFast1.Result.Last(1);

            var adxrising = _adx.adxr.LastValue >= trend;

            //var emalong = MarketSeries.Close.Last(1) > _emaSlow.Result.LastValue;
            //var emashort = MarketSeries.Close.Last(1) < _emaSlow.Result.LastValue;

            var emalong = MarketSeries.Close.LastValue > _emaSlow.Result.LastValue;
            var emashort = MarketSeries.Close.LastValue < _emaSlow.Result.LastValue;

            var emarising = _emaSlow.Result.IsRising();
            var emafalling = _emaSlow.Result.IsFalling();



            //if (previousSlowEMA > previousFastEMA && currentSlowEMA <= currentFastEMA && currentDiPlus > currentDiMinus && emalong && longPosition == null)
            if (previousDiMinus > previousDiPlus && currentDiMinus <= currentDiPlus && previousSlowEMA > previousFastEMA && currentSlowEMA <= currentFastEMA && adxrising && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, VolumeInUnits, cBotLabel, StopLoss, TakeProfit);
            }
            //else if (previousSlowEMA < previousFastEMA && currentSlowEMA >= currentFastEMA && currentDiPlus < currentDiMinus && emashort && shortPosition == null)
            else if (previousDiMinus < previousDiPlus && currentDiMinus >= currentDiPlus && previousSlowEMA < previousFastEMA && currentSlowEMA >= currentFastEMA && adxrising && shortPosition == null)
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
