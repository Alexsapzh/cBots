// -------------------------------------------------------------------------------
//
//    This is a Template used as a guideline to build your own Robot. 
//    Please use the “Feedback” tab to provide us with your suggestions about cAlgo’s API.
//
// -------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using System.Collections.Generic;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class Scalper : Robot
    {
        [Parameter(DefaultValue = "Multi Pos SMA")]
        public string cBotLabel { get; set; }

        [Parameter("BarCount", DefaultValue = 4)]
        public int BarCount { get; set; }

        [Parameter("MaxOrders", DefaultValue = 10)]
        public int MaxOrders { get; set; }

        [Parameter("TakeProfit", DefaultValue = 100)]
        public int TakeProfit { get; set; }

        [Parameter("StopLoss", DefaultValue = 100)]
        public int StopLoss { get; set; }

        [Parameter("Volume", DefaultValue = 10000)]
        public int Volume { get; set; }

        [Parameter("MaxDropDown", DefaultValue = 0)]
        public double MaxDropDown { get; set; }

        [Parameter("MaxProfit", DefaultValue = 0)]
        public double MaxProfit { get; set; }

        [Parameter("Add Position", DefaultValue = 5)]
        public double AddNewPos { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 10)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 10)]
        public int TrailingStop { get; set; }

        [Parameter(DefaultValue = 3)]
        public int MaxPositions { get; set; }

        private int PosOpen = 0;
        private int OpenIndex = 0;
        private double StartBalanse;
        private DateTime dt;

        protected override void OnStart()
        {
            cBotLabel = "Scalper V2 " + Symbol.Code + " " + TimeFrame.ToString() + " / ";
            StartBalanse = Account.Balance;
            dt = Server.Time;
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }




        protected override void OnTick()
        {
            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;

            int last = MarketSeries.Close.Count - 1;
            if (!(MarketSeries.Open[last] == MarketSeries.High[last] && MarketSeries.Open[last] == MarketSeries.Low[last]))
                return;
            if (dt.Date != Server.Time.Date)
            {
                StartBalanse = Account.Balance;
                dt = Server.Time;
            }

            double bp = (StartBalanse - Account.Balance) / (StartBalanse / 100);
            if (bp > 0 && bp >= MaxDropDown && MaxDropDown != 0)
                return;
            if (bp < 0 && Math.Abs(bp) >= MaxProfit && MaxProfit != 0)
                return;
            if (BarCount < 1)
            {
                Print("Мало баров для анализа тренда. BarCount должно быть больше или равно 1");
                return;
            }
            if (PosOpen < MaxOrders)
            {
                if (OpenIndex == 0 || last - OpenIndex > BarCount)
                {
                    if (IsBuy(last))
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
                        PosOpen++;
                        OpenIndex = last;
                    }
                    if (IsSell(last))
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
                        PosOpen++;
                        OpenIndex = last;
                    }

                    // Trailing Stop for all positions
                    SetTrailingStop();
                }
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        protected override void OnPositionClosed(Position position)
        {
            PosOpen--;
            if (PosOpen < 0)
                PosOpen = 0;
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
        private bool IsBuy(int last)
        {

            for (int i = BarCount; i > 0; i--)
            {
                if (MarketSeries.Open[last - i] < MarketSeries.Close[last - i])
                    return false;
                if (i < 2)
                    continue;
                if (MarketSeries.High[last - i] > MarketSeries.High[last - i - 1])
                    return false;
            }
            return true;
        }

        private bool IsSell(int last)
        {

            for (int i = BarCount; i > 0; i--)
            {
                if (MarketSeries.Open[last - i] > MarketSeries.Close[last - i])
                    return false;
                if (i < 2)
                    continue;
                if (MarketSeries.Low[last - i] < MarketSeries.Low[last - i - 1])
                    return false;
            }
            return true;
        }
    }
}
