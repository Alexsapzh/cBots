// -------------------------------------------------------------------------------
//   Trades using Ichimoku Kinko Hyo indicator.
//   Implements Chinkou/Price cross strategy.
//   Chinkou crossing price (close) from below is a bullish signal.
//   Chinkou crossing price (close) from above is a bearish signal.
//   Entry confirmed by current price above/below Kumo, latest Chinkou outside Kumo.
// -------------------------------------------------------------------------------

using System.Linq;
using System;
using cAlgo.API;
using cAlgo.API.Requests;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class IchimokuPipstep : Robot
    {
        // Main input parameters


        public string cBotLabel = "Ichi Multi";

        [Parameter("Start Hour", DefaultValue = 7.0)]
        public double StartTime { get; set; }

        [Parameter("Stop Hour", DefaultValue = 20.0)]
        public double StopTime { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 10, MinValue = 5, MaxValue = 200, Step = 1)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 10, MinValue = 5, MaxValue = 200, Step = 1)]
        public int TakeProfit { get; set; }

        [Parameter("Trigger (pips)", DefaultValue = 5, MinValue = 3, MaxValue = 25, Step = 1)]
        public int Trigger { get; set; }

        [Parameter("Trailing Stop (pips)", DefaultValue = 5, MinValue = 1, MaxValue = 50, Step = 1)]
        public int TrailingStop { get; set; }

        [Parameter("Pip Step", DefaultValue = 10, MinValue = 1)]
        public int PipStep { get; set; }

        [Parameter(DefaultValue = 3, MinValue = 3, MaxValue = 100, Step = 1)]
        public int MaxPositions { get; set; }

        [Parameter("Average TP", DefaultValue = 3, MinValue = 1)]
        public int AverageTP { get; set; }

        [Parameter("Buy", DefaultValue = true)]
        public bool Buy { get; set; }

        [Parameter("Sell", DefaultValue = true)]
        public bool Sell { get; set; }

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


        private double sp_d;
        private DateTime _startTime;
        private DateTime _stopTime;

        // Entry signals
        private bool ChinkouPriceBull = false;
        private bool ChinkouPriceBear = false;
        private bool KumoBullConfirmation = false;
        private bool KumoBearConfirmation = false;
        private bool KumoChinkouBullConfirmation = false;
        private bool KumoChinkouBearConfirmation = false;

        // Indicator handles
        private IchimokuKinkoHyo Ichimoku;


        protected override void OnStart()
        {
            Ichimoku = Indicators.IchimokuKinkoHyo(Tenkan, Kijun, Senkou);
            cBotLabel = "Ichi Multi " + Symbol.Code + " " + TimeFrame.ToString();
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
            // Start Time is the same day at 07:00:00 Server Time
            _startTime = Server.Time.Date.AddHours(StartTime);

            // Stop Time is the next day at 20:00:00
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

            if (Trade.IsExecuting)
                return;

            var currentHours = Server.Time.TimeOfDay.TotalHours;
            bool tradeTime = StartTime < StopTime ? currentHours > StartTime && currentHours < StopTime : currentHours < StopTime || currentHours > StartTime;

            if (!tradeTime)
                return;

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

            {
                RefreshData();
            }
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
