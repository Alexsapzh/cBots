using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HMAbot : Robot
    {


        [Parameter(DefaultValue = 10000, Step = 1000, MinValue = 1000)]
        public int Volume { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableStopLoss { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 1, Step = 1)]
        public double StopLoss { get; set; }

        [Parameter(DefaultValue = false)]
        public bool EnableTrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, Step = 1)]
        public double TrailingStop { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 1, Step = 1)]
        public double TrailingStart { get; set; }

        [Parameter(DefaultValue = true)]
        public bool EnableTakeProfit { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 0)]
        public int TakeProfit { get; set; }

        [Parameter("Data Source")]
        public DataSeries Price { get; set; }

        [Parameter("Exp Fast Periods", DefaultValue = 5, MinValue = 1, MaxValue = 550, Step = 1)]
        public int FastPeriods { get; set; }

        [Parameter("Period", DefaultValue = 9, MinValue = 1, MaxValue = 100, Step = 1)]
        public int Period { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26, MinValue = 1, MaxValue = 100, Step = 1)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12, MinValue = 1, MaxValue = 100, Step = 1)]
        public int ShortCycle { get; set; }

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int interval { get; set; }

        [Parameter("ADX Trend Strength", DefaultValue = 20, MinValue = 10, MaxValue = 30, Step = 1)]
        public int trend { get; set; }

        [Parameter("CCI Period", DefaultValue = 14, MinValue = 1, MaxValue = 100, Step = 1)]
        public int CCI_period { get; set; }

        [Parameter("Period", DefaultValue = 1)]
        public int HeikenPeriod { get; set; }


        #region cBot Variables
        private HeikenAshi2 _heiken;
        private CCI _cci;
        private ADXR _adx;
        private MacdHistogram _macd;
        private ExponentialMovingAverage _emaFast;
        private string _instanceLabel;
        private const int indexOffset = 0;
        private int index;
        #endregion



        protected override void OnStart()
        {
            _instanceLabel = "Heiken CCI Adxr" + " " + Symbol.Code + " " + TimeFrame.ToString();
            _macd = Indicators.MacdHistogram(LongCycle, ShortCycle, Period);
            _emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            _adx = Indicators.GetIndicator<ADXR>(Source, interval);
            _cci = Indicators.GetIndicator<CCI>(CCI_period);
            _heiken = Indicators.GetIndicator<HeikenAshi2>(1);
            index = MarketSeries.Close.Count - 1;


            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;
        }

        protected override void OnTick()
        {

            manageTrailingStops();

            index = MarketSeries.Close.Count - 1;

            var longPosition = Positions.Find(_instanceLabel, Symbol, TradeType.Buy);
            var shortPosition = Positions.Find(_instanceLabel, Symbol, TradeType.Sell);

            bool _macdlong = _macd.Histogram.LastValue < 0.0 && _macd.Signal.IsRising();
            bool _macdshort = _macd.Histogram.LastValue > 0.0 && _macd.Signal.IsFalling();

            //The Best EMA
            //var _emalong = MarketSeries.Open.LastValue > _emaFast.Result.LastValue && _emaFast.Result.IsRising() && MarketSeries.Close.Last(1) > MarketSeries.Close.Last(2);
            //var _emashort = MarketSeries.Open.LastValue < _emaFast.Result.LastValue && _emaFast.Result.IsFalling() && MarketSeries.Close.Last(1) < MarketSeries.Close.Last(2);

            //Heiken EMA
            bool _emalong = _heiken.xOpen[index] > _emaFast.Result.LastValue && _emaFast.Result.IsRising() && _heiken.xClose[index] > _heiken.xOpen[index];
            bool _emashort = _heiken.xOpen[index] < _emaFast.Result.LastValue && _emaFast.Result.IsFalling() && _heiken.xClose[index] < _heiken.xOpen[index];

            bool _ematrendlong = _emaFast.Result.LastValue > _emaFast.Result.Last(2);
            bool _ematrendshort = _emaFast.Result.LastValue < _emaFast.Result.Last(2);
            bool _adxrtrend = _adx.adxr[index] >= trend && _adx.adxr.IsRising();
            bool _adxrlong = _adx.diplus[index] > _adx.diminus[index];
            bool _adxrshort = _adx.diminus[index] > _adx.diplus[index];
            bool _CCIlong = _cci.CCIa[index] >= 0;
            bool _CCIshort = _cci.CCIa[index] <= 0;

            // ADXR Crossover
            //var _adxrlong = _adx.diminus[index - 1] > _adx.diplus[index - 1] && _adx.diminus[index] <= _adx.diplus[index];
            //var _adxrshort = _adx.diminus[index - 1] < _adx.diplus[index - 1] && _adx.diminus[index] >= _adx.diplus[index];

            if (_emalong && _macdlong && _adxrlong && _adxrtrend && _CCIlong && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);
                ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, _instanceLabel, StopLoss, TakeProfit);
            }
            else if (_emashort && _macdshort && _adxrshort && _adxrtrend && _CCIshort && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);
                ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, _instanceLabel, StopLoss, TakeProfit);
            }
        }

        private void PositionsOnOpened(PositionOpenedEventArgs obj)
        {
            Position openedPosition = obj.Position;
            if (openedPosition.Label != _instanceLabel)
                return;

            Print("position opened at {0}", openedPosition.EntryPrice);
        }

        private void PositionsOnClosed(PositionClosedEventArgs obj)
        {
            Position closedPosition = obj.Position;
            if (closedPosition.Label != _instanceLabel)
                return;

            Print("position closed with {0} gross profit", closedPosition.GrossProfit);
        }


        protected void manageTrailingStops()
        {
            if (!EnableTrailingStop)
                return;

            foreach (Position position in Positions.FindAll(_instanceLabel))
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
    }
}
