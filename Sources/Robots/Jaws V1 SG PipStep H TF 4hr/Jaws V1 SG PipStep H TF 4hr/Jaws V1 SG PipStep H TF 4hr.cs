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
    public class JawsV1SGPipStepHTF4hr : Robot
    {

        [Parameter(DefaultValue = "Jaws V1")]
        public string cBotLabel { get; set; }

        [Parameter("Start Hour", DefaultValue = 8.0)]
        public double StartTime { get; set; }

        [Parameter("Stop Hour", DefaultValue = 21.0)]
        public double StopTime { get; set; }

        [Parameter("Slow Periods", DefaultValue = 31, MinValue = 26, MaxValue = 200, Step = 1)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 4, MinValue = 1, MaxValue = 200, Step = 1)]
        public int FastPeriods { get; set; }

        [Parameter(DefaultValue = 1000, MinValue = 0)]
        public int Volume { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 100)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 100)]
        public int TakeProfit { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 10)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 10)]
        public int TrailingStop { get; set; }

        [Parameter(DefaultValue = 3, MinValue = 3, MaxValue = 100, Step = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Average TP", DefaultValue = 3, MinValue = 1)]
        public int AverageTP { get; set; }

        [Parameter("Pip Step", DefaultValue = 10, MinValue = 1)]
        public int PipStep { get; set; }

        [Parameter(DefaultValue = "Jaws V1")]
        public string Comment { get; set; }

        [Parameter("HMA HTF Period", DefaultValue = 1, MinValue = 1, MaxValue = 200, Step = 1)]
        public double Period { get; set; }

        private HMAslow hmaslow;
        private HMAfast hmafast;
        private double sp_d;
        private DateTime _startTime;
        private DateTime _stopTime;

        // HMA Signal
        private MarketSeries HmaDaySeries;
        private HMASignals hmaSignal;

        protected override void OnStart()
        {
            cBotLabel = "Jaws V1" + " " + Symbol.Code;
            hmafast = Indicators.GetIndicator<HMAfast>(FastPeriods);
            hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriods);
            HmaDaySeries = MarketData.GetSeries(TimeFrame.Hour4);
            hmaSignal = Indicators.GetIndicator<HMASignals>(HmaDaySeries, 21, false, false, 3, false, 24);
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
            // Start Time is the same day at 08:00:00 Server Time
            _startTime = Server.Time.Date.AddHours(StartTime);

            // Stop Time is the same day at 21:00:00
            _stopTime = Server.Time.Date.AddHours(StopTime);

            Print("Start Time {0},", _startTime);
            Print("Stop Time {0},", _stopTime);


        }

        protected override void OnTick()
        {
            sp_d = (Symbol.Ask - Symbol.Bid) / Symbol.PipSize;
            if (o_tm(TradeType.Buy) > 0)
                f0_86(pnt_12(TradeType.Buy), AverageTP);
            if (o_tm(TradeType.Sell) > 0)
                f0_88(pnt_12(TradeType.Sell), AverageTP);

        }

        private Position position
        {

            get { return Positions.FirstOrDefault(pos => ((pos.Label == Comment) && (pos.SymbolCode == Symbol.Code))); }
        }

        protected override void OnBar()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (Trade.IsExecuting)
                return;

            var currentHours = Server.Time.TimeOfDay.TotalHours;
            bool tradeTime = StartTime < StopTime ? currentHours > StartTime && currentHours < StopTime : currentHours < StopTime || currentHours > StartTime;

            if (!tradeTime)
                return;

            if (cBotPositions.Length > MaxPositions)
                return;

            var longPosition = Positions.Find(cBotLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(cBotLabel, Symbol, TradeType.Sell);

            var currenthmaslow = hmaslow.hmaslow.Last(0);
            var currenthmafast = hmafast.hmafast.Last(0);
            var previoushmaslow = hmaslow.hmaslow.Last(1);
            var previoushmafast = hmafast.hmafast.Last(1);

            double i = hmaSignal.hma.LastValue;

            if (currenthmafast > previoushmafast && hmaSignal.IsBullish)
            {
                fBuy();
            }
            else if (currenthmafast < previoushmafast && hmaSignal.IsBearish)
            {
                fSell();
            }
            {
                // Trailing Stop for all positions
                SetTrailingStop();
            }

            // Some condition to open extra position
            foreach (var position in cBotPositions)
            {
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Buy)
                        if (Math.Round(Symbol.Ask, Symbol.Digits) < Math.Round(D_TD(TradeType.Buy) - PipStep * Symbol.PipSize, Symbol.Digits))
                            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);

                        else if (position.TradeType == TradeType.Sell)
                            if (Math.Round(Symbol.Bid, Symbol.Digits) > Math.Round(U_TD(TradeType.Sell) + PipStep * Symbol.PipSize, Symbol.Digits))
                                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
                }

            }
        }

        private double FindLastPrice(TradeType TypeOfTrade)
        {
            double LastPrice = 0;

            for (int i = 0; i < Positions.Count; i++)
            {
                var position = Positions[i];
                if (TypeOfTrade == TradeType.Buy)
                    if (position.TradeType == TypeOfTrade)
                    {
                        if (LastPrice == 0)
                        {
                            LastPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < LastPrice)
                            LastPrice = position.EntryPrice;
                    }
                if (TypeOfTrade == TradeType.Sell)
                    if (position.TradeType == TypeOfTrade)
                    {
                        if (LastPrice == 0)
                        {
                            LastPrice = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice > LastPrice)
                            LastPrice = position.EntryPrice;
                    }
            }
            return LastPrice;
        }

        private void fBuy()
        {
            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }

        private void fSell()
        {
            ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }


        private void f0_86(double ai_4, int ad_8)
        {
            foreach (var position in Positions)
            {
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        double? li_16 = Math.Round(ai_4 + ad_8 * Symbol.PipSize, Symbol.Digits);
                        if (position.TakeProfit != li_16)
                            ModifyPosition(position, position.StopLoss, li_16);
                    }
                }
            }
        }
        private void f0_88(double ai_4, int ad_8)
        {
            foreach (var position in Positions)
            {
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Sell)
                    {
                        double? li_16 = Math.Round(ai_4 - ad_8 * Symbol.PipSize, Symbol.Digits);
                        if (position.TakeProfit != li_16)
                            ModifyPosition(position, position.StopLoss, li_16);
                    }
                }
            }
        }

        private int o_tm(TradeType TrdTp)
        {
            int TSide = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                var position = Positions[i];
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                        TSide++;
                }
            }
            return TSide;
        }
        private double pnt_12(TradeType TrdTp)
        {
            double Result = 0;
            double AveragePrice = 0;
            long Count = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                var position = Positions[i];
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        AveragePrice += position.EntryPrice * position.Volume;
                        Count += position.Volume;
                    }
                }
            }
            if (AveragePrice > 0 && Count > 0)
                Result = Math.Round(AveragePrice / Count, Symbol.Digits);
            return Result;
        }

        private double D_TD(TradeType TrdTp)
        {
            double D_TD = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                var position = Positions[i];
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (D_TD == 0)
                        {
                            D_TD = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice < D_TD)
                            D_TD = position.EntryPrice;
                    }
                }
            }
            return D_TD;
        }

        private double U_TD(TradeType TrdTp)
        {
            double U_TD = 0;

            for (int i = Positions.Count - 1; i >= 0; i--)
            {
                var position = Positions[i];
                if (position.Label == cBotLabel && position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TrdTp)
                    {
                        if (U_TD == 0)
                        {
                            U_TD = position.EntryPrice;
                            continue;
                        }
                        if (position.EntryPrice > U_TD)
                            U_TD = position.EntryPrice;
                    }
                }
            }
            return U_TD;
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

                double newStopLossPrice = Math.Round(Symbol.Ask + TrailingStop * Symbol.PipSize);

                if (position.StopLoss == null || newStopLossPrice < position.StopLoss)
                    ModifyPosition(position, newStopLossPrice, position.TakeProfit);
            }

            var buyPositions = Positions.FindAll(cBotLabel, Symbol, TradeType.Buy);

            foreach (Position position in buyPositions)
            {
                double distance = Symbol.Bid - position.EntryPrice;

                if (distance < Trigger * Symbol.PipSize)
                    continue;

                double newStopLossPrice = Math.Round(Symbol.Bid - TrailingStop * Symbol.PipSize);
                if (position.StopLoss == null || newStopLossPrice > position.StopLoss)
                    ModifyPosition(position, newStopLossPrice, position.TakeProfit);
            }
        }

    }
}
