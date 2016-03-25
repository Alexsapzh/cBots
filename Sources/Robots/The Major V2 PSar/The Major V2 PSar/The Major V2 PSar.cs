// HMA Fast / Slow, RSI <20 >80, MACD Rising / Falling, Candlestick Tendency, Sinewave Support / Resistance

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TheMajorV2PSar : Robot
    {

        // general params

        [Parameter()]
        public TimeFrame HighOrderTimeFrame { get; set; }

        [Parameter(DefaultValue = 0.07, MinValue = 0, MaxValue = 2, Step = 0.01)]
        public double Alpha { get; set; }

        [Parameter(DefaultValue = 10000, Step = 1000, MinValue = 1000)]
        public int Volume { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableStopLoss { get; set; }

        [Parameter(DefaultValue = 100, MinValue = 1, MaxValue = 200, Step = 1)]
        public double StopLoss { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableBreakEven { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, MaxValue = 200, Step = 1)]
        public double BreakEvenPips { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 1, MaxValue = 200, Step = 1)]
        public double BreakEvenGain { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableTrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, MaxValue = 200, Step = 1)]
        public double TrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, MaxValue = 200, Step = 1)]
        public double TrailingStart { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableTakeProfit { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 1, MaxValue = 200, Step = 1)]
        public int TakeProfit { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnterOnSyncSignalOnly { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ExitOnOppositeSignal { get; set; }

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

        [Parameter("MFI Period", DefaultValue = 14)]
        public int MFIPeriod { get; set; }

        [Parameter("Min", DefaultValue = 0.02, MinValue = 0.02, MaxValue = 0.02, Step = 0.01)]
        public double minaf { get; set; }

        [Parameter("Max AF", DefaultValue = 0.2, MinValue = 0.2, MaxValue = 0.2, Step = 0.1)]
        public double maxaf { get; set; }

        private SinewaveSupportResistance _SSR;
        private RelativeStrengthIndex rsi;
        private MacdHistogram _macd;
        private ParabolicSAR _parabolic;
        private MoneyFlowIndex _moneyFlow;
        private string label;


        private const int indexOffset = 0;
        private int index;
        private CandlestickTendency tendency;

        public bool globalTendencyWasLong;
        public bool globalTendencyWasShort;
        public bool localTendencyWasLong;
        public bool localTendencyWasShort;

        public Position currentPosition
        {
            get { return Positions.Find(label); }
        }
        public bool inPosition
        {
            get { return currentPosition != null; }
        }
        public bool inShortPosition
        {
            get { return currentPosition != null && currentPosition.TradeType == TradeType.Sell; }
        }
        public bool inLongPosition
        {
            get { return currentPosition != null && currentPosition.TradeType == TradeType.Buy; }
        }

        public bool globalTendencyIsLong
        {
            get { return tendency.HighOrderLine[index] > 0; }
        }
        public bool localTendencyIsLong
        {
            get { return tendency.Line[index] > 0; }
        }
        public bool globalTendencyIsShort
        {
            get { return tendency.HighOrderLine[index] < 0; }
        }
        public bool localTendencyIsShort
        {
            get { return tendency.Line[index] < 0; }
        }
        public bool longSignal
        {
            get { return rsirising && rsilong && macdlong && supportlong && localTendencyIsLong && globalTendencyIsLong && mfilong && psarlong; }
        }
        public bool shortSignal
        {
            get { return rsifalling && rsishort && macdshort && resistanceshort && localTendencyIsShort && globalTendencyIsShort && mfishort && psarshort; }
        }
        public bool closeSignal
        {
            get { return inPosition ? ((currentPosition.TradeType == TradeType.Sell) ? longSignal : shortSignal) : false; }
        }

        public bool rsilong
        {
            get { return rsi.Result.LastValue < 30; }
        }

        public bool rsishort
        {
            get { return rsi.Result.LastValue > 70; }
        }

        public bool rsirising
        {
            get { return rsi.Result.IsRising(); }
        }

        public bool rsifalling
        {
            get { return rsi.Result.IsFalling(); }
        }

        public bool macdlong
        {
            get { return _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising(); }
        }

        public bool macdshort
        {
            get { return _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling(); }
        }

        public bool supportlong
        {
            get { return MarketSeries.Low.LastValue >= _SSR.Support.LastValue; }
        }

        public bool resistanceshort
        {
            get { return MarketSeries.High.LastValue <= _SSR.Resistance.LastValue; }
        }

        public bool psarlong
        {
            get { return _parabolic.Result.Last(0) > _parabolic.Result.Last(1); }
        }

        public bool psarshort
        {
            get { return _parabolic.Result.Last(0) < _parabolic.Result.Last(1); }
        }

        public bool mfilong
        {
            get { return _moneyFlow.Result.IsRising(); }
        }

        public bool mfishort
        {
            get { return _moneyFlow.Result.IsFalling(); }
        }

        protected override void OnStart()
        {

            label = "TheMajorV2PSar" + Symbol.Code + " " + TimeFrame.ToString() + " / " + HighOrderTimeFrame.ToString();
            tendency = Indicators.GetIndicator<CandlestickTendency>(HighOrderTimeFrame);
            index = MarketSeries.Close.Count - 1;
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            _SSR = Indicators.GetIndicator<SinewaveSupportResistance>(MarketSeries, Alpha);
            _moneyFlow = Indicators.MoneyFlowIndex(MFIPeriod);
            _parabolic = Indicators.ParabolicSAR(minaf, maxaf);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }


        protected void UpdateTrailingStops()
        {

            if (!EnableTrailingStop)
                return;

            var positions = Positions.FindAll(label);
            if (positions == null)
                return;

            foreach (var position in positions)
            {
                if (position.Pips >= TrailingStart)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        var newStopLoss = Symbol.Bid - TrailingStop * Symbol.PipSize;
                        if (position.StopLoss < newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                    else if (position.TradeType == TradeType.Sell)
                    {
                        var newStopLoss = Symbol.Ask + TrailingStop * Symbol.PipSize;
                        if (position.StopLoss > newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                }
            }
        }

        protected void MoveToBreakEven()
        {

            if (!EnableBreakEven)
                return;

            var positions = Positions.FindAll(label);
            if (positions == null)
                return;

            foreach (var position in positions)
            {
                if (position.Pips >= BreakEvenPips)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        var newStopLoss = Symbol.Bid - BreakEvenGain * Symbol.PipSize;
                        if (position.StopLoss < newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                    else if (position.TradeType == TradeType.Sell)
                    {
                        var newStopLoss = Symbol.Ask + BreakEvenGain * Symbol.PipSize;
                        if (position.StopLoss > newStopLoss)
                            ModifyPosition(position, newStopLoss, null);
                    }
                }
            }
        }

        protected TradeResult EnterInPosition(TradeType direction)
        {

            if (!EnableStopLoss && EnableTakeProfit)
                return ExecuteMarketOrder(direction, Symbol, Volume, label, null, TakeProfit);

            if (!EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(direction, Symbol, Volume, label, null, null);

            if (EnableStopLoss && !EnableTakeProfit)
                return ExecuteMarketOrder(direction, Symbol, Volume, label, StopLoss, null);

            return ExecuteMarketOrder(direction, Symbol, Volume, label, StopLoss, TakeProfit);
        }

        protected override void OnTick()
        {

            index = MarketSeries.Close.Count - 1;
            UpdateTrailingStops();
            MoveToBreakEven();

        }

        protected override void OnBar()
        {

            index = MarketSeries.Close.Count - 2;

            if (ExitOnOppositeSignal && closeSignal)
                ClosePosition(currentPosition);

            if (!inPosition)
            {

                if (EnterOnSyncSignalOnly)
                {

                    if (localTendencyWasShort && globalTendencyWasShort && localTendencyIsLong && globalTendencyIsLong)
                    {
                        EnterInPosition(TradeType.Buy);
                    }
                    else if (localTendencyWasLong && globalTendencyWasLong && localTendencyIsShort && globalTendencyIsShort)
                    {
                        EnterInPosition(TradeType.Sell);
                    }

                }
                else
                {

                    if (shortSignal)
                    {
                        EnterInPosition(TradeType.Sell);
                    }
                    else if (longSignal)
                    {
                        EnterInPosition(TradeType.Buy);
                    }
                }
            }

            localTendencyWasLong = localTendencyIsLong;
            localTendencyWasShort = localTendencyIsShort;
            globalTendencyWasLong = globalTendencyIsLong;
            globalTendencyWasShort = globalTendencyIsShort;
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


        protected override void OnStop()
        {
        }
    }
}
