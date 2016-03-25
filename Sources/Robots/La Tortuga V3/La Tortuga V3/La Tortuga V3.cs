using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LaTortugaV3 : Robot
    {

        [Parameter(DefaultValue = "La Tortuga V3")]
        public string cBotLabel { get; set; }

        [Parameter("Start Hour", DefaultValue = 7.0)]
        public double StartTime { get; set; }

        [Parameter("Stop Hour", DefaultValue = 20.0)]
        public double StopTime { get; set; }

        [Parameter("Slow Periods", DefaultValue = 31, MinValue = 26, MaxValue = 200, Step = 1)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", DefaultValue = 4, MinValue = 1, MaxValue = 26, Step = 1)]
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

        [Parameter(DefaultValue = "La Tortuga V3")]
        public string Comment { get; set; }

        private HMAslow hmaslow;
        private HMAfast hmafast;
        private double sp_d;
        private DateTime _startTime;
        private DateTime _stopTime;

        protected override void OnStart()
        {
            cBotLabel = "La Tortuga V3" + Symbol.Code + " " + TimeFrame.ToString();
            hmafast = Indicators.GetIndicator<HMAfast>(FastPeriods);
            hmaslow = Indicators.GetIndicator<HMAslow>(SlowPeriods);
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
            // Start Time is the same day at 07:00:00 Server Time
            _startTime = Server.Time.Date.AddHours(StartTime);

            // Stop Time is the same day at 20:00:00
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

            if (previoushmaslow > previoushmafast && currenthmaslow <= currenthmafast)
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
                PipstepBuy();
            }
            else if (previoushmaslow < previoushmafast && currenthmaslow >= currenthmafast)
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
                PipstepSell();

                // Trailing Stop for all positions
                SetTrailingStop();
            }

        }

        private int GetPositionsSide()
        {
            int Result = -1;
            int i, BuySide = 0, SellSide = 0;

            for (i = 0; i < Positions.Count; i++)
            {
                if (Positions[i].TradeType == TradeType.Buy)
                    BuySide++;
                if (Positions[i].TradeType == TradeType.Sell)
                    SellSide++;
            }
            if (BuySide == Positions.Count)
                Result = 0;
            if (SellSide == Positions.Count)
                Result = 1;
            return Result;
        }

        private void PipstepBuy()
        {
            int _pipstep;
            int BarCount = 25;
            int Del = MaxPositions - 1;

            if (PipStep == 0)
                _pipstep = GetDynamicPipstep(BarCount, Del);
            else
                _pipstep = PipStep;

            if (Positions.Count < MaxPositions)
            {
                GetPositionsSide();
            }
            if (Symbol.Ask < FindLastPrice(TradeType.Buy) - _pipstep * Symbol.TickSize)
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }



        private void PipstepSell()
        {
            int _pipstep;
            int BarCount = 25;
            int Del = MaxPositions - 1;

            if (PipStep == 0)
                _pipstep = GetDynamicPipstep(BarCount, Del);
            else
                _pipstep = PipStep;

            if (Positions.Count < MaxPositions)
            {
                GetPositionsSide();
            }
            if (Symbol.Bid > FindLastPrice(TradeType.Sell) + _pipstep * Symbol.TickSize)
                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }

        private int GetDynamicPipstep(int CountOfBars, int Del)
        {
            int Result;
            double HighestPrice = 0, LowestPrice = 0;
            int StartBar = MarketSeries.Close.Count - 2 - CountOfBars;
            int EndBar = MarketSeries.Close.Count - 2;

            for (int i = StartBar; i < EndBar; i++)
            {
                if (HighestPrice == 0 && LowestPrice == 0)
                {
                    HighestPrice = MarketSeries.High[i];
                    LowestPrice = MarketSeries.Low[i];
                    continue;
                }
                if (MarketSeries.High[i] > HighestPrice)
                    HighestPrice = MarketSeries.High[i];
                if (MarketSeries.Low[i] < LowestPrice)
                    LowestPrice = MarketSeries.Low[i];
            }
            Result = (int)((HighestPrice - LowestPrice) / Symbol.TickSize / Del);
            return Result;
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
