using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class ZigZagCycleBot : Robot
    {
        private Position _position;
        private ZigZag _zigzag;
        private double _prevValue;


        [Parameter(DefaultValue = 12)]
        public int ZzDepth { get; set; }

        [Parameter(DefaultValue = 550)]
        public int StopLoss { get; set; }

        [Parameter(DefaultValue = 5)]
        public int ZzDeviation { get; set; }

        [Parameter(DefaultValue = 3)]
        public int ZzBackStep { get; set; }

        [Parameter(DefaultValue = 100000, MinValue = 0)]
        public int Volume { get; set; }


        protected override void OnStart()
        {
            _zigzag = Indicators.GetIndicator<ZigZag>(ZzDepth, ZzDeviation, ZzBackStep, MarketSeries);
        }

        protected override void OnBar()
        {
            if (Trade.IsExecuting)
                return;

            bool isLongPositionOpen = _position != null && _position.TradeType == TradeType.Buy;
            bool isShortPositionOpen = _position != null && _position.TradeType == TradeType.Sell;


            double lastValue = _zigzag.Result.LastValue;

            if (!double.IsNaN(lastValue))
            {

                // Buy                
                if (MarketSeries.Result.Low <= _zigzag.Result.LastValue && _zigzag.Result.IsRising() && !isLongPositionOpen)
                {
                    ClosePosition();
                    Buy();
                }
                // Sell
                else if (_zigzag.Result.IsFalling() && !isShortPositionOpen)
                {
                    ClosePosition();
                    Sell();
                }

                _prevValue = lastValue;
            }
        }

        protected override void OnPositionOpened(Position openedPosition)
        {
            _position = openedPosition;
            Trade.ModifyPosition(openedPosition, GetAbsoluteStopLoss(openedPosition, StopLoss), null);
        }

        private void ClosePosition()
        {
            if (_position == null)
                return;
            Trade.Close(_position);
            _position = null;
        }

        private void Buy()
        {
            Trade.CreateBuyMarketOrder(Symbol, Volume);
        }

        private void Sell()
        {
            Trade.CreateSellMarketOrder(Symbol, Volume);
        }

        private double? GetAbsoluteStopLoss(Position position, int stopLoss)
        {
            return position.TradeType == TradeType.Buy ? position.EntryPrice - Symbol.PipSize * stopLoss : position.EntryPrice + Symbol.PipSize * stopLoss;
        }

    }
}
