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
    [Robot()]
    public class PersistentAnti : Robot
    {
        // Main input parameters
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
        }

        private Position position
        {
            get { return Positions.FirstOrDefault(pos => ((pos.Label == Comment) && (pos.SymbolCode == Symbol.Code))); }
        }

        protected override void OnBar()
        {
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
            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, Comment);
        }

        private void fSell()
        {
            ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, Comment);
        }
    }
}
