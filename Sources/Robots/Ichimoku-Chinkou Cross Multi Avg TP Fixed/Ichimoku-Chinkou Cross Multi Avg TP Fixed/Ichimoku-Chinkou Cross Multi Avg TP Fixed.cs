// -------------------------------------------------------------------------------
//   Trades using Ichimoku Kinko Hyo indicator.
//   Implements Chinkou/Price cross strategy.
//   Chinkou crossing price (close) from below is a bullish signal.
//   Chinkou crossing price (close) from above is a bearish signal.
//   No SL/TP. Positions remain open from signal to signal.
//   Entry confirmed by current price above/below Kumo, latest Chinkou outside Kumo.
//   Copyright 2013-2014, EarnForex.com
//   http://www.earnforex.com
// -------------------------------------------------------------------------------

using System.Linq;
using cAlgo.API;
using cAlgo.API.Requests;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class PersistentAnti : Robot
    {
        // Main input parameters


        public string cBotLabel;

        [Parameter("Stop Loss (pips)", DefaultValue = 10, MinValue = 5, MaxValue = 200, Step = 1)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 10, MinValue = 5, MaxValue = 200, Step = 1)]
        public int TakeProfit { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 5, MinValue = 3, MaxValue = 25, Step = 1)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 5, MinValue = 1, MaxValue = 50, Step = 1)]
        public int TrailingStop { get; set; }

        [Parameter("Add Position", DefaultValue = 5, MinValue = 2, MaxValue = 200, Step = 1)]
        public double AddNewPos { get; set; }

        [Parameter(DefaultValue = 3, MinValue = 3, MaxValue = 100, Step = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Tral_Start", DefaultValue = 50, MinValue = 1)]
        public int Tral_Start { get; set; }

        [Parameter("Tral_Stop", DefaultValue = 50, MinValue = 1)]
        public int Tral_Stop { get; set; }

        [Parameter("Average TP", DefaultValue = 3, MinValue = 1)]
        public int AverageTP { get; set; }

        // Tenkan line period. The fast "moving average".
        [Parameter(DefaultValue = 9, MinValue = 2)]
        public int Tenkan { get; set; }

        // Kijun line period. The slow "moving average".
        [Parameter(DefaultValue = 26, MinValue = 2)]
        public int Kijun { get; set; }

        // Senkou period. Used for Kumo (Cloud) spans.
        [Parameter(DefaultValue = 52, MinValue = 2)]
        public int Senkou { get; set; }

        // Money management
        // Basic position size used with MM = 0.
        [Parameter(DefaultValue = 10000, MinValue = 0)]
        public int Volume { get; set; }

        // Miscellaneous
        [Parameter(DefaultValue = "Ichimoku-Chinkou-Cross")]
        public string Comment { get; set; }

        // Tolerated slippage in brokers' pips.
        [Parameter(DefaultValue = 100, MinValue = 0)]
        public int Slippage { get; set; }

        // Common
        private bool HaveLongPosition;
        private bool HaveShortPosition;
        private Position _position;

        // Entry signals
        private bool ChinkouPriceBull = false;
        private bool ChinkouPriceBear = false;
        private bool KumoBullConfirmation = false;
        private bool KumoBearConfirmation = false;
        private bool KumoChinkouBullConfirmation = false;
        private bool KumoChinkouBearConfirmation = false;

        // Indicator handles
        private IchimokuKinkoHyo Ichimoku;

        protected int PositionsCount
        {
            get { return Positions.Count(position1 => position1.Label == cBotLabel); }
        }

        protected override void OnStart()
        {
            Ichimoku = Indicators.IchimokuKinkoHyo(Tenkan, Kijun, Senkou);
            cBotLabel = "Ichi Multi " + Symbol.Code + " " + TimeFrame.ToString();
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        private Position position
        {

            get { return Positions.FirstOrDefault(pos => ((pos.Label == Comment) && (pos.SymbolCode == Symbol.Code))); }
        }

        protected override void OnTick()
        {
            double bid = Symbol.Bid;
            double ask = Symbol.Ask;
            double point = Symbol.TickSize;

            if (Trade.IsExecuting)
                return;

            foreach (var position1 in Positions.Where(position1 => position1.Label == cBotLabel))
            {
                if (position1.TradeType == TradeType.Buy)
                {
                    if (bid - GetAveragePrice(TradeType.Buy) >= Tral_Start * point)
                        if (bid - Tral_Stop * point >= position1.StopLoss)
                            ModifyPosition(position1, bid - Tral_Stop * point, position1.TakeProfit);
                }
                else
                {
                    if (GetAveragePrice(TradeType.Sell) - ask >= Tral_Start * point)
                        if (ask + Tral_Stop * point <= position1.StopLoss || position1.StopLoss == 0)
                            ModifyPosition(position1, ask + Tral_Stop * point, position1.TakeProfit);
                }
            }
        }


        protected override void OnPositionOpened(Position openedPosition)
        {
            double? stopLossPrice = null;
            double? takeProfitPrice = null;

            // Checks only the positions opened by this robot
            if (Positions.Count(position1 => position1.Label == cBotLabel) == 1)
            {
                _position = openedPosition;

                if (_position.TradeType == TradeType.Buy)
                    takeProfitPrice = _position.EntryPrice + TakeProfit * Symbol.TickSize;

                if (_position.TradeType == TradeType.Sell)
                    takeProfitPrice = _position.EntryPrice - TakeProfit * Symbol.TickSize;
            }
            else
                switch (GetPositionsSide())
                {
                    case 0:
                        takeProfitPrice = GetAveragePrice(TradeType.Buy) + TakeProfit * Symbol.TickSize;
                        break;
                    case 1:
                        takeProfitPrice = GetAveragePrice(TradeType.Sell) - TakeProfit * Symbol.TickSize;
                        break;
                }
            // Checks only the positions opened by this robot
            foreach (var position1 in Positions.Where(position1 => position1.Label == cBotLabel))
            {
                _position = position1;

                if (stopLossPrice != null || takeProfitPrice != null)
                    ModifyPosition(_position, _position.StopLoss, takeProfitPrice);
            }
        }

        private double GetAveragePrice(TradeType typeOfTrade)
        {
            double result = Symbol.Bid;
            double averagePrice = 0;
            long count = 0;

            foreach (var position1 in Positions.Where(position1 => position1.Label == cBotLabel && position1.TradeType == typeOfTrade))
            {
                averagePrice += position1.EntryPrice * position1.Volume;
                count += position1.Volume;
            }

            if (averagePrice > 0 && count > 0)
                result = averagePrice / count;

            return result;
        }

        private int GetPositionsSide()
        {
            int result = -1;

            int buySide = Positions.Count(position1 => position1.Label == cBotLabel && position1.TradeType == TradeType.Buy);
            int sellSide = Positions.Count(position1 => position1.Label == cBotLabel && position1.TradeType == TradeType.Sell);

            if (buySide == PositionsCount)
                result = 0;
            if (sellSide == PositionsCount)
                result = 1;

            return result;
        }

        protected override void OnBar()
        {

            var cBotPositions = Positions.FindAll(cBotLabel);

            if (cBotPositions.Length > MaxPositions)
                return;


            int latest_bar = MarketSeries.Close.Count - 1;
            // Latest bar index
            // Chinkou/Price Cross
            double ChinkouSpanLatest = Ichimoku.ChikouSpan[latest_bar - (Kijun + 1)];
            // Latest closed bar with Chinkou.
            double ChinkouSpanPreLatest = Ichimoku.ChikouSpan[latest_bar - (Kijun + 2)];
            // Bar older than latest closed bar with Chinkou.
            // Bullish entry condition
            if ((ChinkouSpanLatest > MarketSeries.Close[latest_bar - (Kijun + 1)]) && (ChinkouSpanPreLatest <= MarketSeries.Close[latest_bar - (Kijun + 2)]))
            {
                ChinkouPriceBull = true;
                ChinkouPriceBear = false;
            }
            // Bearish entry condition
            else if ((ChinkouSpanLatest < MarketSeries.Close[latest_bar - (Kijun + 1)]) && (ChinkouSpanPreLatest >= MarketSeries.Close[latest_bar - (Kijun + 2)]))
            {
                ChinkouPriceBull = false;
                ChinkouPriceBear = true;
            }
            // Voiding entry conditions if cross is ongoing.
            else if (ChinkouSpanLatest == MarketSeries.Close[latest_bar - (Kijun + 1)])
            {
                ChinkouPriceBull = false;
                ChinkouPriceBear = false;
            }

            // Kumo confirmation. When cross is happening current price (latest close) should be above/below both Senkou Spans, or price should close above/below both Senkou Spans after a cross.
            double SenkouSpanALatestByPrice = Ichimoku.SenkouSpanA[latest_bar - 1];
            // Senkou Span A at time of latest closed price bar.
            double SenkouSpanBLatestByPrice = Ichimoku.SenkouSpanB[latest_bar - 1];
            // Senkou Span B at time of latest closed price bar.
            if ((MarketSeries.Close[latest_bar - 1] > SenkouSpanALatestByPrice) && (MarketSeries.Close[latest_bar - 1] > SenkouSpanBLatestByPrice))
                KumoBullConfirmation = true;
            else
                KumoBullConfirmation = false;
            if ((MarketSeries.Close[latest_bar - 1] < SenkouSpanALatestByPrice) && (MarketSeries.Close[latest_bar - 1] < SenkouSpanBLatestByPrice))
                KumoBearConfirmation = true;
            else
                KumoBearConfirmation = false;

            // Kumo/Chinkou confirmation. When cross is happening Chinkou at its latest close should be above/below both Senkou Spans at that time, or it should close above/below both Senkou Spans after a cross.
            double SenkouSpanALatestByChinkou = Ichimoku.SenkouSpanA[latest_bar - (Kijun + 1)];
            // Senkou Span A at time of latest closed bar of Chinkou span.
            double SenkouSpanBLatestByChinkou = Ichimoku.SenkouSpanB[latest_bar - (Kijun + 1)];
            // Senkou Span B at time of latest closed bar of Chinkou span.
            if ((ChinkouSpanLatest > SenkouSpanALatestByChinkou) && (ChinkouSpanLatest > SenkouSpanBLatestByChinkou))
                KumoChinkouBullConfirmation = true;
            else
                KumoChinkouBullConfirmation = false;
            if ((ChinkouSpanLatest < SenkouSpanALatestByChinkou) && (ChinkouSpanLatest < SenkouSpanBLatestByChinkou))
                KumoChinkouBearConfirmation = true;
            else
                KumoChinkouBearConfirmation = false;

            GetPositionStates();

            if (ChinkouPriceBull)
            {
                if (HaveShortPosition)
                    ClosePrevious();
                if ((KumoBullConfirmation) && (KumoChinkouBullConfirmation))
                {
                    ChinkouPriceBull = false;
                    fBuy();
                }
            }
            else if (ChinkouPriceBear)
            {
                if (HaveLongPosition)
                    ClosePrevious();
                if ((KumoBearConfirmation) && (KumoChinkouBearConfirmation))
                {
                    fSell();
                    ChinkouPriceBear = false;
                }

                // Trailing Stop for all positions
                SetTrailingStop();

            }
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


        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            double? StopLossPrice = null;
            double? TakeProfitPrice = null;

            if (Positions.Count == 1)
            {
                var position = args.Position;

                if (position.TradeType == TradeType.Buy)
                    TakeProfitPrice = position.EntryPrice + TakeProfit * Symbol.TickSize;
                if (position.TradeType == TradeType.Sell)
                    TakeProfitPrice = position.EntryPrice - TakeProfit * Symbol.TickSize;
            }
            else
                switch (GetPositionsSide())
                {
                    case 0:
                        TakeProfitPrice = GetAveragePrice(TradeType.Buy) + TakeProfit * Symbol.TickSize;
                        break;
                    case 1:
                        TakeProfitPrice = GetAveragePrice(TradeType.Sell) - TakeProfit * Symbol.TickSize;
                        break;
                }

            for (int i = 0; i < Positions.Count; i++)
            {
                var position = Positions[i];
                if (StopLossPrice != null || TakeProfitPrice != null)
                    ModifyPosition(position, position.StopLoss, TakeProfitPrice);
            }
        }


        private void GetPositionStates()
        {
            if (position != null)
            {
                if (position.TradeType == TradeType.Buy)
                {
                    HaveLongPosition = true;
                    HaveShortPosition = false;
                    return;
                }
                else if (position.TradeType == TradeType.Sell)
                {
                    HaveLongPosition = false;
                    HaveShortPosition = true;
                    return;
                }
            }
            HaveLongPosition = false;
            HaveShortPosition = false;
        }

        private void ClosePrevious()
        {
            if (position == null)
                return;
            ClosePosition(position);
        }

        private void fBuy()
        {
            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }

        private void fSell()
        {
            ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, cBotLabel, StopLoss, TakeProfit);
        }
    }
}
